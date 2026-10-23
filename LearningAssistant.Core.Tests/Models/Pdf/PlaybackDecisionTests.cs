using FluentAssertions;
using LearningAssistant.Models.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Models.Pdf;

public class PlaybackDecisionTests
{
    private const string TrackA = @"C:\audio\p10.mp3";
    private const string TrackB = @"C:\audio\p11.mp3";

    [Fact]
    public void 整篇播放_翻页永不动作()
    {
        PlaybackDecision.Resolve(PlaybackScope.WholeBook, true, PdfPlayerState.Playing, TrackA, TrackB)
            .Should().Be(PlaybackAction.None);
    }

    [Fact]
    public void 关闭跟随翻页_完全不动作()
    {
        PlaybackDecision.Resolve(PlaybackScope.FollowPage, false, PdfPlayerState.Playing, TrackA, TrackB)
            .Should().Be(PlaybackAction.None);
    }

    [Fact]
    public void 目标页无音频_停止并提示()
    {
        PlaybackDecision.Resolve(PlaybackScope.FollowPage, true, PdfPlayerState.Playing, TrackA, null)
            .Should().Be(PlaybackAction.StopWithHint);
    }

    [Fact]
    public void 同一音频_不重启()
    {
        PlaybackDecision.Resolve(PlaybackScope.FollowPage, true, PdfPlayerState.Playing, TrackA, TrackA)
            .Should().Be(PlaybackAction.None);
    }

    [Fact]
    public void 路径忽略大小写_视为同一音频()
    {
        PlaybackDecision.Resolve(PlaybackScope.FollowPage, true, PdfPlayerState.Playing,
                TrackA, TrackA.ToUpperInvariant())
            .Should().Be(PlaybackAction.None);
    }

    [Theory]
    [InlineData(PdfPlayerState.Playing, PlaybackAction.LoadAndPlay)]
    [InlineData(PdfPlayerState.Loading, PlaybackAction.LoadAndPlay)]
    [InlineData(PdfPlayerState.PausedByUser, PlaybackAction.LoadOnly)]
    [InlineData(PdfPlayerState.Ended, PlaybackAction.LoadOnly)]
    [InlineData(PdfPlayerState.Error, PlaybackAction.LoadOnly)]
    [InlineData(PdfPlayerState.Idle, PlaybackAction.None)]
    [InlineData(PdfPlayerState.Previewing, PlaybackAction.None)]
    public void 不同音频时按播放器状态决定动作(PdfPlayerState state, PlaybackAction expected)
    {
        PlaybackDecision.Resolve(PlaybackScope.FollowPage, true, state, TrackA, TrackB)
            .Should().Be(expected);
    }
}
