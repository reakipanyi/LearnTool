using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Shadowing;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class ShadowingSegmentStoreTests
{
    [Fact]
    public async Task Segments_RoundTripRelativePathsAndTakes()
    {
        string root = CreateTempRoot();
        try
        {
            string pdf = Path.Combine(root, "book.pdf");
            var store = new ShadowingSegmentStore(root, "tester");
            var manifest = new ShadowingSegmentManifest
            {
                PageIndex = 11,
                Source = new ShadowingSegmentSource
                {
                    TrackPath = Path.Combine(root, "audio", "Lesson_12.mp3"),
                    Fingerprint = "9f3c1a",
                    DurationMs = 203400
                },
                Session = new ShadowingSegmentSession
                {
                    Id = "ss_20261024_1520",
                    CaptureSpeed = 1.0,
                    LockedPage = true
                },
                Segments =
                {
                    new ShadowingSegment
                    {
                        Id = "sg_011_7c3a9e21",
                        StartMs = 0,
                        EndMs = 35700,
                        Text = "First, listen to the dialogue.",
                        TextSource = "srt",
                        SlicePath = "slices/sg_011_7c3a9e21.wav",
                        Takes =
                        {
                            new ShadowingTake
                            {
                                Id = "tk_011_7c3a9e21",
                                Path = "takes/sg_011_7c3a9e21_20261024_153012.wav",
                                DurationMs = 33800,
                                Score = 86,
                                ScoreKind = "pronunciation",
                                Chosen = true
                            }
                        }
                    }
                }
            };

            await store.SaveSegmentsAsync(pdf, manifest);
            var loaded = await store.LoadSegmentsAsync(pdf, 11);

            Assert.NotNull(loaded);
            Assert.Equal(3, loaded!.SchemaVersion);
            Assert.Equal(11, loaded.PageIndex);
            Assert.Equal(203400, loaded.Source.DurationMs);
            Assert.Equal(1.0, loaded.Session.CaptureSpeed);
            Assert.Single(loaded.Segments);
            Assert.Equal("slices/sg_011_7c3a9e21.wav", loaded.Segments[0].SlicePath);
            Assert.Single(loaded.Segments[0].Takes);
            Assert.Equal(86, loaded.Segments[0].Takes[0].Score);
            Assert.True(loaded.Segments[0].Takes[0].Chosen);

            string resolved = store.ResolvePath(loaded.Segments[0].SlicePath);
            Assert.Equal(Path.Combine(root, "slices", "sg_011_7c3a9e21.wav"), resolved);
            Assert.Equal(loaded.Segments[0].SlicePath, store.ToRelativePath(resolved));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void SegmentsPath_IsolatesByUserAndPage()
    {
        string root = CreateTempRoot();
        try
        {
            string pdf = Path.Combine(root, "book.pdf");
            var userA = new ShadowingSegmentStore(root, "a");
            var userB = new ShadowingSegmentStore(root, "b");

            Assert.NotEqual(userA.GetSegmentsPath(pdf, 0), userA.GetSegmentsPath(pdf, 1));
            Assert.NotEqual(userA.GetSegmentsPath(pdf, 0), userB.GetSegmentsPath(pdf, 0));
            Assert.Equal(
                Path.GetDirectoryName(userA.GetProgressPath(pdf)),
                Path.GetDirectoryName(userA.GetSegmentsPath(pdf, 0)));
            Assert.Equal("p011.segments.json", Path.GetFileName(userA.GetSegmentsPath(pdf, 11)));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task SaveSegments_MergesSameIdAndKeepsUnmentionedSegments()
    {
        string root = CreateTempRoot();
        try
        {
            string pdf = Path.Combine(root, "book.pdf");
            var store = new ShadowingSegmentStore(root, "tester");
            var first = new ShadowingSegmentManifest
            {
                PageIndex = 3,
                Source = new ShadowingSegmentSource { TrackPath = "audio/a.mp3" },
                Segments =
                {
                    new ShadowingSegment { Id = "sg_a", StartMs = 0, EndMs = 1000 },
                    new ShadowingSegment { Id = "sg_b", StartMs = 1000, EndMs = 2000 }
                }
            };
            await store.SaveSegmentsAsync(pdf, first);

            var second = new ShadowingSegmentManifest
            {
                PageIndex = 3,
                Source = new ShadowingSegmentSource { TrackPath = "audio/a.mp3" },
                Segments =
                {
                    new ShadowingSegment { Id = "sg_a", StartMs = 0, EndMs = 1500 },
                    new ShadowingSegment { Id = "sg_c", StartMs = 2000, EndMs = 3000 }
                }
            };
            await store.SaveSegmentsAsync(pdf, second);

            var loaded = await store.LoadSegmentsAsync(pdf, 3);
            Assert.NotNull(loaded);
            Assert.Equal(3, loaded!.Segments.Count);
            Assert.Equal(1500, loaded.Segments.Single(s => s.Id == "sg_a").EndMs);
            Assert.Contains(loaded.Segments, s => s.Id == "sg_b");
            Assert.Contains(loaded.Segments, s => s.Id == "sg_c");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task SaveSegments_PreserveTakes_KeepsTakesOnRealignment()
    {
        string root = CreateTempRoot();
        try
        {
            string pdf = Path.Combine(root, "book.pdf");
            var store = new ShadowingSegmentStore(root, "tester");
            var take = new ShadowingTake
            {
                Id = "tk_003_aaaaaaaa",
                Path = "takes/tk_003_aaaaaaaa.wav",
                DurationMs = 1200,
                Score = 88,
                Chosen = true
            };
            var first = new ShadowingSegmentManifest
            {
                PageIndex = 3,
                Segments =
                {
                    new ShadowingSegment
                    {
                        Id = "sg_003_aaaaaaaa",
                        StartMs = 0,
                        EndMs = 1200,
                        Text = "First version.",
                        Takes = { take }
                    }
                }
            };
            await store.SaveSegmentsAsync(pdf, first);

            var realigned = new ShadowingSegmentManifest
            {
                PageIndex = 3,
                Segments =
                {
                    new ShadowingSegment
                    {
                        Id = "sg_003_aaaaaaaa",
                        StartMs = 100,
                        EndMs = 1500,
                        Text = "Realigned text."
                    }
                }
            };
            await store.SaveSegmentsAsync(pdf, realigned, preserveExistingTakes: true);

            var loaded = await store.LoadSegmentsAsync(pdf, 3);
            Assert.NotNull(loaded);
            var segment = Assert.Single(loaded!.Segments);
            Assert.Equal(100, segment.StartMs);
            Assert.Equal(1500, segment.EndMs);
            Assert.Single(segment.Takes);
            Assert.Equal("tk_003_aaaaaaaa", segment.Takes[0].Id);
            Assert.True(segment.Takes[0].Chosen);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void CreateAnchoredSegmentId_IsDeterministicAndStable()
    {
        string first = ShadowingSegmentStore.CreateAnchoredSegmentId(11, "hl:11:abcd:0|s1");
        string second = ShadowingSegmentStore.CreateAnchoredSegmentId(11, "hl:11:abcd:0|s1");
        string other = ShadowingSegmentStore.CreateAnchoredSegmentId(11, "hl:11:abcd:0|s2");

        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
        Assert.Matches("^sg_011_[0-9a-f]{8}$", first);
    }

    [Fact]
    public async Task Progress_SavePage_MergesOtherPages()
    {
        string root = CreateTempRoot();
        try
        {
            string pdf = Path.Combine(root, "book.pdf");
            var store = new ShadowingSegmentStore(root, "tester");

            await store.SaveProgressPageAsync(pdf, 11, "sg_011_8d1f02aa", "single-loop");
            await store.SaveProgressPageAsync(pdf, 12, "sg_012_1a2b3c4d", "continuous");

            var progress = await store.LoadProgressAsync(pdf);
            Assert.NotNull(progress);
            Assert.Equal(1, progress!.SchemaVersion);
            Assert.Equal(2, progress.Pages.Count);
            Assert.Equal("sg_011_8d1f02aa", progress.Pages["11"].LastSegmentId);
            Assert.Equal("single-loop", progress.Pages["11"].Mode);
            Assert.Equal("continuous", progress.Pages["12"].Mode);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void CreateIds_FollowDesignFormatAndAreUnique()
    {
        string segmentA = ShadowingSegmentStore.CreateSegmentId(11);
        string segmentB = ShadowingSegmentStore.CreateSegmentId(11);

        Assert.Matches("^sg_011_[0-9a-f]{8}$", segmentA);
        Assert.NotEqual(segmentA, segmentB);
        Assert.Matches("^tk_011_[0-9a-f]{8}$", ShadowingSegmentStore.CreateTakeId(11));
        Assert.Matches("^ss_\\d{8}_\\d{4}$", ShadowingSegmentStore.CreateSessionId(new DateTime(2026, 10, 24, 15, 20, 0)));
    }

    [Fact]
    public void AllocateTakeRelativePath_AvoidsNameCollision()
    {
        string root = CreateTempRoot();
        try
        {
            var store = new ShadowingSegmentStore(root, "tester");
            var time = new DateTime(2026, 10, 24, 15, 30, 12);
            string first = store.AllocateTakeRelativePath("sg_011_7c3a9e21", time);
            Directory.CreateDirectory(Path.GetDirectoryName(store.ResolvePath(first))!);
            File.WriteAllBytes(store.ResolvePath(first), Array.Empty<byte>());

            string second = store.AllocateTakeRelativePath("sg_011_7c3a9e21", time);
            Assert.Equal("takes/sg_011_7c3a9e21_20261024_153012.wav", first);
            Assert.Equal("takes/sg_011_7c3a9e21_20261024_153012_2.wav", second);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static string CreateTempRoot()
    {
        string path = Path.Combine(Path.GetTempPath(), "shadowing-segment-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempRoot(string root)
    {
        try
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
        catch
        {
        }
    }
}
