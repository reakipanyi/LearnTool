using FluentAssertions;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Audio;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Audio;

public class AnalysisCoveragePlanTests
{
    private static AudioSentenceSpan Span(int startMs, int endMs)
        => new() { StartMs = startMs, EndMs = endMs };

    [Fact]
    public void 无切片_整轨分析()
    {
        var plan = AnalysisCoveragePlan.Plan(null, null, 60000);

        plan.Mode.Should().Be(AnalysisCoverageMode.None);
        plan.Coverage.Should().Be(0);
        plan.Holes.Should().BeEmpty();
    }

    [Fact]
    public void 零时长_整轨分析且覆盖为零()
    {
        var plan = AnalysisCoveragePlan.Plan(new[] { Span(0, 1000) }, null, 0);

        plan.Mode.Should().Be(AnalysisCoverageMode.None);
        plan.Coverage.Should().Be(0);
    }

    [Fact]
    public void 覆盖率低于三成_整轨分析()
    {
        var plan = AnalysisCoveragePlan.Plan(new[] { Span(0, 15000) }, null, 60000);

        plan.Mode.Should().Be(AnalysisCoverageMode.None);
        plan.Coverage.Should().BeApproximately(0.25, 1e-9);
    }

    [Fact]
    public void 覆盖率介于三成与九成_只分析空洞()
    {
        var plan = AnalysisCoveragePlan.Plan(
            new[] { Span(0, 20000), Span(40000, 55000) },
            null,
            60000);

        plan.Mode.Should().Be(AnalysisCoverageMode.Partial);
        plan.Coverage.Should().BeApproximately(35.0 / 60.0, 1e-9);
        plan.Holes.Should().Equal(new AudioTimeRange(20000, 40000), new AudioTimeRange(55000, 60000));
    }

    [Fact]
    public void 覆盖率达标_跳过VAD()
    {
        var plan = AnalysisCoveragePlan.Plan(
            new[] { Span(0, 30000), Span(30000, 57000) },
            null,
            60000);

        plan.Mode.Should().Be(AnalysisCoverageMode.Full);
        plan.Coverage.Should().BeApproximately(0.95, 1e-9);
        plan.Holes.Should().BeEmpty();
    }

    [Fact]
    public void 重叠区间只算一次()
    {
        var plan = AnalysisCoveragePlan.Plan(
            new[] { Span(0, 12000), Span(5000, 17000) },
            null,
            60000);

        plan.Coverage.Should().BeApproximately(17.0 / 60.0, 1e-9);
    }

    [Fact]
    public void 越界与乱序切片被裁剪并合并()
    {
        var plan = AnalysisCoveragePlan.Plan(
            new[] { Span(-1000, 5000), Span(60000, 70000), Span(3000, 8000) },
            null,
            60000);

        plan.Coverage.Should().BeApproximately(8.0 / 60.0, 1e-9);
        plan.Mode.Should().Be(AnalysisCoverageMode.None);
    }

    [Fact]
    public void 手动修正与切片共同计入覆盖()
    {
        var plan = AnalysisCoveragePlan.Plan(
            new[] { Span(0, 10000) },
            new[] { Span(10000, 40000) },
            50000);

        plan.Mode.Should().Be(AnalysisCoverageMode.Partial);
        plan.Coverage.Should().BeApproximately(0.8, 1e-9);
        plan.Holes.Should().Equal(new AudioTimeRange(40000, 50000));
    }

    [Fact]
    public void 字幕覆盖达标也跳过VAD()
    {
        var plan = AnalysisCoveragePlan.Plan(
            null,
            null,
            60000,
            new[] { Span(0, 58000) });

        plan.Mode.Should().Be(AnalysisCoverageMode.Full);
        plan.Coverage.Should().BeApproximately(58.0 / 60.0, 1e-9);
        plan.Holes.Should().BeEmpty();
    }

    [Fact]
    public void 字幕与切片重叠_空洞按合并后的覆盖计算()
    {
        var plan = AnalysisCoveragePlan.Plan(
            new[] { Span(0, 10000) },
            null,
            60000,
            new[] { Span(8000, 40000) });

        plan.Mode.Should().Be(AnalysisCoverageMode.Partial);
        plan.Coverage.Should().BeApproximately(40.0 / 60.0, 1e-9);
        plan.Holes.Should().Equal(new AudioTimeRange(40000, 60000));
    }
}
