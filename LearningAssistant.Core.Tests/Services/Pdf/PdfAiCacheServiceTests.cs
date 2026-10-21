using FluentAssertions;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfAiCacheServiceTests
{
    [Fact]
    public void Key_对页码排序去重_不同模型或功能生成不同Key()
    {
        var cache = new PdfAiCacheService(CreateTempRoot());
        var key1 = cache.BuildKey("fp", new[] { 3, 1, 3 }, "summarize", "zh", "v1", "model-a");
        var key2 = cache.BuildKey("fp", new[] { 1, 3 }, "summarize", "zh", "v1", "model-a");
        var key3 = cache.BuildKey("fp", new[] { 1, 3 }, "outline", "zh", "v1", "model-a");
        var key4 = cache.BuildKey("fp", new[] { 1, 3 }, "summarize", "zh", "v1", "model-b");

        key1.Should().Be(key2);
        key3.Should().NotBe(key1);
        key4.Should().NotBe(key1);
    }

    [Fact]
    public void 写入后可跨实例读取()
    {
        var root = CreateTempRoot();
        var first = new PdfAiCacheService(root);
        first.Set("key1", "缓存内容");

        var second = new PdfAiCacheService(root);
        second.TryGet("key1", out var content).Should().BeTrue();
        content.Should().Be("缓存内容");
        second.Count.Should().Be(1);
    }

    [Fact]
    public void 超出容量_淘汰最久未访问条目()
    {
        var root = CreateTempRoot();
        var cache = new PdfAiCacheService(root, maxEntries: 2);

        cache.Set("a", "A");
        cache.Set("b", "B");
        // 访问 a，使 b 成为最久未使用
        cache.TryGet("a", out _).Should().BeTrue();
        cache.Set("c", "C");

        cache.Count.Should().Be(2);
        cache.TryGet("b", out _).Should().BeFalse();
        cache.TryGet("a", out _).Should().BeTrue();
        cache.TryGet("c", out _).Should().BeTrue();
    }

    [Fact]
    public void 缓存文件损坏按未命中处理()
    {
        var root = CreateTempRoot();
        var cache = new PdfAiCacheService(root);
        cache.Set("broken", "内容");
        File.WriteAllText(Path.Combine(root, "broken.json"), "{ not json");

        var reloaded = new PdfAiCacheService(root);
        reloaded.TryGet("broken", out _).Should().BeFalse();
    }

    private static string CreateTempRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pdfaicache_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
