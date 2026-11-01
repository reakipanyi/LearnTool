using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfReadingCheckInPreferenceStoreTests : IDisposable
{
    private readonly string _root;

    public PdfReadingCheckInPreferenceStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pdf_checkin_pref_tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch
        {
            // 临时目录清理失败不影响测试结论
        }
    }

    [Fact]
    public void 保存后重新加载_筛选偏好往返一致()
    {
        string directory = Path.Combine(_root, "user");
        var store = new PdfReadingCheckInPreferenceStore(directory);
        store.Save(new PdfReadingCheckInPreferences
        {
            MinSeconds = 120,
            DisplayScope = PdfCheckInDisplayScope.AllIncludingMissing,
            DisplayMode = PdfCheckInCellDisplayMode.Minutes,
            ExpandedFolderKeys = new List<string> { "RAZ", "", PdfCheckInLayoutRules.MissingGroupKey }
        });

        var reloaded = new PdfReadingCheckInPreferenceStore(directory).Load();

        reloaded.MinSeconds.Should().Be(120);
        reloaded.DisplayScope.Should().Be(PdfCheckInDisplayScope.AllIncludingMissing);
        reloaded.DisplayMode.Should().Be(PdfCheckInCellDisplayMode.Minutes);
        reloaded.ExpandedFolderKeys.Should().BeEquivalentTo(
            new[] { "RAZ", "", PdfCheckInLayoutRules.MissingGroupKey });
    }

    [Fact]
    public void 按当前目录懒解析_切换用户后互不影响()
    {
        string userA = Path.Combine(_root, "userA");
        string userB = Path.Combine(_root, "userB");
        string current = userA;
        var store = new PdfReadingCheckInPreferenceStore(() => current);

        store.Save(new PdfReadingCheckInPreferences
        {
            MinSeconds = 300,
            DisplayScope = PdfCheckInDisplayScope.All
        });

        current = userB;
        store.Load().MinSeconds.Should().Be(PdfReadingCheckInPreferences.DefaultMinSeconds);

        store.Save(new PdfReadingCheckInPreferences { MinSeconds = 30 });
        store.Load().MinSeconds.Should().Be(30);

        current = userA;
        store.Load().MinSeconds.Should().Be(300);
        store.Load().DisplayScope.Should().Be(PdfCheckInDisplayScope.All);
    }

    [Fact]
    public void 偏好文件损坏_回退默认值()
    {
        string directory = Path.Combine(_root, "broken");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, PdfReadingCheckInPreferenceStore.FileName), "{ 这不是合法 JSON");

        var preferences = new PdfReadingCheckInPreferenceStore(directory).Load();

        preferences.MinSeconds.Should().Be(PdfReadingCheckInPreferences.DefaultMinSeconds);
        preferences.DisplayScope.Should().Be(PdfCheckInDisplayScope.RecordedOnly);
        preferences.DisplayMode.Should().Be(PdfCheckInCellDisplayMode.CheckMark);
        preferences.ExpandedFolderKeys.Should().BeEmpty();
    }

    [Fact]
    public void 非法阈值与未知枚举_归一化为默认值()
    {
        var preferences = new PdfReadingCheckInPreferences
        {
            MinSeconds = 99999,
            DisplayScope = (PdfCheckInDisplayScope)99,
            DisplayMode = (PdfCheckInCellDisplayMode)99,
            ExpandedFolderKeys = new List<string> { "  RAZ  ", "raz", "", "   ", "Grade 1" }
        }.Normalize();

        preferences.MinSeconds.Should().Be(PdfReadingCheckInPreferences.MaxAllowedSeconds);
        preferences.DisplayScope.Should().Be(PdfCheckInDisplayScope.RecordedOnly);
        preferences.DisplayMode.Should().Be(PdfCheckInCellDisplayMode.CheckMark);
        // 根层目录的空串键需要保留，用于记住根目录行的展开状态
        preferences.ExpandedFolderKeys.Should().BeEquivalentTo(new[] { "RAZ", "", "Grade 1" });
    }

    [Fact]
    public void 阈值低于下限_夹取到最小值()
    {
        var normalized = new PdfReadingCheckInPreferences { MinSeconds = -30 }.Normalize();
        normalized.MinSeconds.Should().Be(PdfReadingCheckInPreferences.MinAllowedSeconds);
    }
}
