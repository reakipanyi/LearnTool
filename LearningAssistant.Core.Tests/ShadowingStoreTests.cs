using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Shadowing;
using Xunit;

namespace LearningAssistant.Core.Tests;

public class ShadowingStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsManifest()
    {
        string root = CreateTempRoot();
        try
        {
            var store = new ShadowingStore(root, "tester");
            var manifest = new ShadowingManifest
            {
                PdfPath = Path.Combine(root, "book.pdf"),
                PageIndex = 2,
                SegmenterVersion = SentenceSegmenter.Version,
                Sentences =
                {
                    new ShadowingSentence { Id = "abc", Index = 0, StartOffset = 0, Length = 10, Text = "Hello there." }
                }
            };

            store.Save(manifest);
            var loaded = store.Load(manifest.PdfPath, 2);

            Assert.NotNull(loaded);
            Assert.Equal(2, loaded!.PageIndex);
            Assert.Equal(SentenceSegmenter.Version, loaded.SegmenterVersion);
            Assert.Single(loaded.Sentences);
            Assert.Equal("abc", loaded.Sentences[0].Id);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void ManifestPath_IsolatesByUserAndPage()
    {
        string root = CreateTempRoot();
        try
        {
            string pdf = Path.Combine(root, "book.pdf");
            var userA = new ShadowingStore(root, "a");
            var userB = new ShadowingStore(root, "b");

            string page0 = userA.GetManifestPath(pdf, 0);
            string page1 = userA.GetManifestPath(pdf, 1);
            string otherUser = userB.GetManifestPath(pdf, 0);

            Assert.NotEqual(page0, page1);
            Assert.NotEqual(page0, otherUser);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void SentenceRecordings_RoundTripAndIsolatedByPage()
    {
        string root = CreateTempRoot();
        try
        {
            string pdf = Path.Combine(root, "book.pdf");
            var store = new ShadowingStore(root, "tester");

            store.SaveSentenceRecordings(pdf, 0, new Dictionary<string, string>
            {
                ["sentence-a"] = Path.Combine(root, "a.wav"),
                ["sentence-b"] = "  "
            });

            var loaded = store.LoadSentenceRecordings(pdf, 0);
            Assert.Single(loaded);
            Assert.Equal(Path.Combine(root, "a.wav"), loaded["sentence-a"]);

            Assert.Empty(store.LoadSentenceRecordings(pdf, 1));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void SentenceScores_RoundTripFiltersOutOfRange()
    {
        string root = CreateTempRoot();
        try
        {
            string pdf = Path.Combine(root, "book.pdf");
            var store = new ShadowingStore(root, "tester");

            store.SaveSentenceScores(pdf, 0, new Dictionary<string, int>
            {
                ["sentence-a"] = 88,
                ["sentence-b"] = 120,
                ["sentence-c"] = -1,
                [" "] = 50
            });

            var loaded = store.LoadSentenceScores(pdf, 0);
            Assert.Single(loaded);
            Assert.Equal(88, loaded["sentence-a"]);
            Assert.Empty(store.LoadSentenceScores(pdf, 1));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void Delete_RemovesSentenceScores()
    {
        string root = CreateTempRoot();
        try
        {
            var store = new ShadowingStore(root, "tester");
            string pdf = Path.Combine(root, "book.pdf");
            store.SaveSentenceScores(pdf, 0, new Dictionary<string, int> { ["s"] = 70 });

            Assert.True(store.Delete(pdf, 0));
            Assert.Empty(store.LoadSentenceScores(pdf, 0));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void Delete_RemovesExistingManifest()
    {
        string root = CreateTempRoot();
        try
        {
            var store = new ShadowingStore(root, "tester");
            string pdf = Path.Combine(root, "book.pdf");
            store.Save(new ShadowingManifest { PdfPath = pdf, PageIndex = 0 });

            Assert.True(store.Delete(pdf, 0));
            Assert.False(store.Delete(pdf, 0));
            Assert.Null(store.Load(pdf, 0));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static string CreateTempRoot()
    {
        string path = Path.Combine(Path.GetTempPath(), "shadowing-tests", Guid.NewGuid().ToString("N"));
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
