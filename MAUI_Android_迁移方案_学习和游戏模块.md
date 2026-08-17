# LearningAssistant 学习与游戏模块 MAUI Android 迁移方案

> **文档版本**: v1.0  
> **创建日期**: 2026-08-17  
> **迁移范围**: 学习模块 + 游戏模块（不含 PDF 阅读）  
> **目标平台**: .NET MAUI (Android)

---

## 目录

1. [项目现状概述](#1-项目现状概述)
2. [迁移总体策略](#2-迁移总体策略)
3. [新项目结构设计](#3-新项目结构设计)
4. [技术栈映射](#4-技术栈映射)
5. [各模块详细迁移方案](#5-各模块详细迁移方案)
   - 5.1 [核心层（Models / Common / Data）](#51-核心层models--common--data)
   - 5.2 [服务层（Services）](#52-服务层services)
   - 5.3 [学习主界面（LearningForm）](#53-学习主界面learningform)
   - 5.4 [五大游戏模块](#54-五大游戏模块)
   - 5.5 [复习与主动回忆](#55-复习与主动回忆)
   - 5.6 [游戏化系统（成就/徽章/挑战）](#56-游戏化系统成就徽章挑战)
   - 5.7 [联想学习与费曼技巧](#57-联想学习与费曼技巧)
   - 5.8 [番茄钟](#58-番茄钟)
   - 5.9 [内容管理与错题本](#59-内容管理与错题本)
6. [前端游戏（HTML/JS）适配方案](#6-前端游戏htmljs适配方案)
7. [数据持久化迁移](#7-数据持久化迁移)
8. [Android 平台特有适配](#8-android-平台特有适配)
9. [依赖注入与启动配置](#9-依赖注入与启动配置)
10. [分阶段实施计划](#10-分阶段实施计划)
11. [风险与应对措施](#11-风险与应对措施)

---

## 1. 项目现状概述

### 1.1 当前技术栈

| 层级 | 技术 | 说明 |
|------|------|------|
| **目标框架** | `net10.0-windows7.0` | Windows 专属 |
| **UI 框架** | Windows Forms (WinForms) | 桌面 UI |
| **嵌入式浏览器** | WebView2 (WinForms) | 游戏宿主，依赖 Edge Runtime |
| **数据库** | SQLite + EF Core 10 | `Microsoft.EntityFrameworkCore.Sqlite` |
| **图表** | ScottPlot.WinForms 4.1 | WinForms 图表控件 |
| **TTS** | System.Speech + KokoroSharp + QwenTts | 混合方案 |
| **OCR** | Tesseract 5.2 | 依赖原生库 |
| **PDF** | PdfiumViewerCore + PdfiumViewer.Native | 不迁移 |
| **音频** | NAudio 2.3 | 音频播放 |
| **Office** | EPPlus 8.6 | Excel 导入导出 |
| **DI** | Microsoft.Extensions.DependencyInjection | 可复用 |
| **日志** | Microsoft.Extensions.Logging | 可复用 |
| **绘图** | SkiaSharp 3.119 | 可复用 |
| **JSON** | System.Text.Json + Newtonsoft.Json | 可复用 |

### 1.2 迁移范围内功能清单

#### 学习功能（11项）
1. ✅ 学习卡片主界面（学习/快速模式切换）
2. ✅ 间隔重复复习（SM2/FSRS算法）
3. ✅ 主动回忆训练器
4. ✅ 闪卡复习
5. ✅ 联想学习
6. ✅ 费曼学习法
7. ✅ 渐进式提示
8. ✅ 番茄钟计时
9. ✅ 错题本
10. ✅ 笔记系统
11. ✅ 学习统计与图表

#### 游戏功能（5项，基于WebView2 + HTML/JS）
1. 🧩 **单词消消乐** (WordMatchGame) - 单词↔释义配对消除
2. 🧠 **记忆翻牌** (MemoryMatchGame) - 翻牌配对记忆
3. 🔗 **连连看** (LinkMatchGame) - 路径连接消除
4. ✍️ **单词拼写** (SpellingGame) - 键盘输入拼写
5. 🔨 **打地鼠** (WhackAMoleGame) - 限时快节奏点击

#### 游戏化系统
- 等级/经验值/积分系统
- 成就系统（Achievement）
- 徽章系统（Badge）
- 挑战系统（Challenge）
- 鼓励文案系统（Encouragement）
- 庆祝动画（Confetti）

---

## 2. 迁移总体策略

### 2.1 核心原则

```
分层保留 + UI 替换 + 渐进适配
```

1. **核心层（Models / Data / Services 接口）** → 95% 直接复用，仅调整平台相关引用
2. **服务实现层** → 平台相关实现替换（如 TTS、存储路径、热键等），业务逻辑保留
3. **UI 层** → WinForms → MAUI XAML，完全重写但保持交互逻辑
4. **前端游戏** → WebView2 → MAUI `WebView` / Blazor Hybrid，HTML/JS 资源基本复用

### 2.2 分层迁移映射图

```
┌─────────────────────────────────────────────────────────────┐
│                    UI Layer (重写)                          │
│  WinForms Form/UserControl  →  MAUI ContentPage/ContentView │
│  WinForms 事件/属性绑定     →  MAUI Command/Binding         │
│  WebView2 (WinForms)        →  MAUI BlazorWebView / WebView │
├─────────────────────────────────────────────────────────────┤
│                  Presenter Layer (适配)                     │
│     MVP 模式 Presenter  →  MVVM ViewModel 改造              │
├─────────────────────────────────────────────────────────────┤
│                Services Layer (大部分复用)                  │
│     接口定义 100% 保留                                       │
│     平台相关实现替换 (TTS/文件/通知/系统托盘)               │
├─────────────────────────────────────────────────────────────┤
│              Data / Models Layer (95% 复用)                 │
│     LearningItem / EF Core / Entities 基本不变              │
│     调整: 移除 WinForms 引用 (Color/Point 等)               │
├─────────────────────────────────────────────────────────────┤
│              Common Layer (90% 复用)                        │
│     枚举/常量/JSON/缓存/日志 直接复用                        │
│     移除 WinForms 扩展方法                                   │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. 新项目结构设计

```
LearningAssistant.Maui/
├── LearningAssistant.Maui.sln
├── src/
│   ├── LearningAssistant.Core/                    # 核心层 (可移植类库)
│   │   ├── Common/
│   │   │   ├── Enums.cs                           # SubjectType/SubCategoryType 等
│   │   │   ├── Constants.cs
│   │   │   ├── AppPaths.cs                        # → 抽象为接口
│   │   │   ├── JsonHelper.cs
│   │   │   ├── StringLanguageDetector.cs
│   │   │   ├── SubjectSubCategoryMapping.cs
│   │   │   ├── Events/                            # 事件总线
│   │   │   └── Themes/                            # 主题系统 (MAUI 资源字典)
│   │   ├── Models/
│   │   │   ├── Learning/
│   │   │   ├── Gamification/
│   │   │   ├── Pomodoro/
│   │   │   ├── Quiz/
│   │   │   ├── User/
│   │   │   ├── Config/
│   │   │   └── ValueObjects/
│   │   ├── Data/
│   │   │   └── Database/
│   │   │       ├── AppDbContext.cs
│   │   │       ├── Entities.cs
│   │   │       └── EntityBase.cs
│   │   └── LearningAssistant.Core.csproj          # Target: net8.0
│   │
│   ├── LearningAssistant.Services/                # 服务层 (可移植类库)
│   │   ├── Learning/                              # 学习服务 (大部分直接复用)
│   │   │   ├── IContentLoaderService.cs
│   │   │   ├── ContentLoaderService.cs            # 调整存储路径
│   │   │   ├── StudyEngine.cs
│   │   │   ├── WordMatchGameService.cs            # ✅ 100% 复用
│   │   │   ├── PomodoroService.cs
│   │   │   ├── SqliteSpacedRepetitionService.cs
│   │   │   └── ...
│   │   ├── Gamification/
│   │   │   ├── IGamificationService.cs
│   │   │   ├── GamificationService.cs
│   │   │   └── ...
│   │   ├── AI/
│   │   ├── Feedback/
│   │   ├── Persistence/
│   │   │   ├── IDataPersistenceService.cs
│   │   │   └── SqliteDataPersistenceService.cs    # 调整路径
│   │   ├── TTS/                                   # 平台抽象
│   │   │   └── ITTSService.cs
│   │   └── LearningAssistant.Services.csproj
│   │
│   ├── LearningAssistant.Platforms/Android/       # Android 平台实现
│   │   ├── Services/
│   │   │   ├── AndroidTtsService.cs               # Android TTS 实现
│   │   │   ├── AndroidSoundService.cs             # Android 音频播放
│   │   │   ├── AndroidNotificationService.cs      # Android 通知
│   │   │   └── AndroidAppPaths.cs                 # Android 存储路径
│   │   ├── Renderers/                             # 自定义渲染器
│   │   └── LearningAssistant.Platforms.Android.csproj
│   │
│   └── LearningAssistant.MauiApp/                 # MAUI UI 项目
│       ├── MauiProgram.cs                         # DI + 启动配置
│       ├── App.xaml / App.xaml.cs
│       ├── AppShell.xaml                          # 导航壳
│       ├── Resources/
│       │   ├── Styles/                             # MAUI 主题资源
│       │   ├── Images/
│       │   └── Raw/                                # 游戏 HTML/JS/CSS 资源
│       │       ├── Shared/
│       │       ├── WordMatchGame/
│       │       ├── MemoryMatchGame/
│       │       ├── LinkMatchGame/
│       │       ├── SpellingGame/
│       │       └── WhackAMoleGame/
│       ├── ViewModels/                            # MVVM ViewModels
│       │   ├── LearningViewModel.cs
│       │   ├── Games/
│       │   │   ├── GameViewModelBase.cs
│       │   │   ├── WordMatchGameViewModel.cs
│       │   │   ├── MemoryMatchGameViewModel.cs
│       │   │   └── ...
│       │   ├── ReviewViewModel.cs
│       │   ├── ActiveRecallViewModel.cs
│       │   ├── GamificationViewModel.cs
│       │   └── ...
│       ├── Views/                                 # MAUI Pages
│       │   ├── MainPage.xaml                      # 主页/学习中心
│       │   ├── LearningPage.xaml                  # 学习卡片界面
│       │   ├── Games/
│       │   │   ├── GameHostPage.xaml              # 游戏宿主页 (BlazorWebView)
│       │   │   ├── WordMatchGamePage.xaml
│       │   │   └── ...
│       │   ├── ReviewPage.xaml
│       │   ├── ActiveRecallPage.xaml
│       │   ├── AssociationLearningPage.xaml
│       │   ├── AchievementPage.xaml
│       │   ├── ChallengePage.xaml
│       │   ├── WrongAnswerPage.xaml
│       │   ├── NotesPage.xaml
│       │   ├── ContentEditorPage.xaml
│       │   ├── StatsPage.xaml
│       │   └── SettingsPage.xaml
│       ├── Controls/                              # 自定义控件
│       │   ├── LearningCard.xaml
│       │   ├── CircularProgress.xaml
│       │   ├── LevelBadge.xaml
│       │   ├── PomodoroTimer.xaml
│       │   ├── StatCard.xaml
│       │   └── ConfettiCanvas.xaml                # SkiaSharp 绘制
│       └── LearningAssistant.MauiApp.csproj       # Target: net8.0-android
│
└── tests/
    └── LearningAssistant.Tests/                   # 单元测试 (可复用现有测试)
```

---

## 4. 技术栈映射

| 原 WinForms 技术 | MAUI Android 替代方案 | 备注 |
|-----------------|----------------------|------|
| `System.Windows.Forms.Form` | `Microsoft.Maui.Controls.ContentPage` | 页面 |
| `UserControl` | `ContentView` / `ContentView` + XAML | 自定义控件 |
| `Panel` / `TableLayoutPanel` | `Grid` / `StackLayout` / `VerticalStackLayout` | 布局 |
| `Dock = DockStyle.Fill` | `HorizontalOptions="Fill"` `VerticalOptions="Fill"` | 填充 |
| `Button.Click` 事件 | `Command` 绑定 (MVVM) | 推荐 |
| `ComboBox` | `Picker` | 下拉选择 |
| `CheckBox` / `RadioButton` | `CheckBox` / `RadioButton` | MAUI 内置 |
| `TextBox` | `Entry` / `Editor` | 文本输入 |
| `ListBox` | `CollectionView` | 列表 |
| `ProgressBar` | `ProgressBar` | 直接对应 |
| `System.Windows.Forms.Timer` | `System.Timers.Timer` 或 `IDispatcherTimer` | MAUI `Dispatcher.CreateTimer()` |
| `MessageBox.Show()` | `Page.DisplayAlert()` | 对话框 |
| `ToolTip` | MAUI `Tooltip` 效果 (自定义) | |
| **WebView2** (WinForms) | **`BlazorWebView`** 或 MAUI `WebView` | **游戏宿主核心替换** |
| `CoreWebView2.PostWebMessageAsJson()` | `BlazorWebView` 双向通信 | 见第6节 |
| `ScottPlot.WinForms` | **`ScottPlot.Maui`** 或 `Microcharts.Maui` / `LiveChartsCore.SkiaSharpView.Maui` | 图表替换 |
| `System.Speech.Synthesis` | **`Android.Speech.Tts.TextToSpeech`** | Android 原生 TTS |
| `KokoroSharp` TTS | 保留 (本地推理) / 或仅 QwenTts HTTP | 需验证 Android 原生支持 |
| `NAudio` | **`Plugin.AudioManager`** 或 `MediaElement` | MAUI 音频播放 |
| `ITrayIconService` (系统托盘) | ✅ **Android 通知 + 前台服务** | 对应番茄钟提醒 |
| `IHotkeyService` | ⚠️ **移除或替换** | Android 无全局热键 |
| `Color.FromArgb()` | `Color.FromArgb()` / `Color.Parse()` | SkiaSharp/MAUI 兼容 |
| `Microsoft.Web.WebView2` | `Microsoft.AspNetCore.Components.WebView.Maui` | NuGet 替换 |
| `PdfiumViewer.*` | ❌ **排除 (PDF 不迁移)** | |
| `Tesseract OCR` | 保留或延后 | 需 Android 原生绑定 |
| `EPPlus` (Excel) | ✅ 保留 (.NET Standard 2.0+) | 可直接用 |
| SkiaSharp 3.x | ✅ 升级到 **SkiaSharp.Views.Maui.Controls** | MAUI 内置支持 |

---

## 5. 各模块详细迁移方案

### 5.1 核心层（Models / Common / Data）

#### ✅ 可 100% 直接复用的文件

| 目录 | 文件 | 说明 |
|------|------|------|
| `Common/` | `Enums.cs` | SubjectType/SubCategoryType/LearningModeType/SortOrderType |
| `Common/` | `Constants.cs` | 常量定义 |
| `Common/` | `JsonHelper.cs` | JSON 序列化辅助 |
| `Common/` | `SubjectSubCategoryMapping.cs` | 科目映射 |
| `Common/` | `StringLanguageDetector.cs` | 语言检测 |
| `Common/` | `StringSimilarityHelper.cs` | 字符串相似度 |
| `Common/Events/` | `EventBus.cs`, `LearningEvents.cs`, `IEventBus.cs` | 事件总线（无 WinForms 依赖） |
| `Models/Learning/` | `LearningItem.cs`, `LearningContext.cs`, `LearningStatus.cs` | 核心模型 |
| `Models/Learning/ValueObjects/` | `Meaning.cs`, `Example.cs`, `Pronunciation.cs`, `WordFeatures.cs` | 值对象 |
| `Models/Learning/Progress/` | `StudyStats.cs` | 学习统计 |
| `Models/Gamification/` | 全部 | 成就/徽章/挑战模型 |
| `Models/User/` | `UserProfile.cs`, `Settings.cs` | 用户模型 |
| `Models/Pomodoro/` | 全部 | 番茄钟模型 |
| `Models/Quiz/` | 全部 | 测验模型 |
| `Data/Database/` | `AppDbContext.cs`, `Entities.cs`, `EntityBase.cs` | EF Core 实体（需检查） |

#### ⚠️ 需要调整的文件

| 文件 | 调整内容 |
|------|----------|
| `Common/AppPaths.cs` | 原 `Environment.SpecialFolder.LocalApplicationData` → Android 用 `FileSystem.AppDataDirectory` / `FileSystem.CacheDirectory`。**建议改为接口 `IAppPaths`**，Android 实现注入 |
| `Common/ImageHelper.cs` | 原 `System.Drawing` → MAUI `Microsoft.Maui.Graphics` / SkiaSharp API 替换 |
| `Common/ThemeHelper.cs`, `ThemeManager.cs` | 原 WinForms 颜色主题 → MAUI `ResourceDictionary` 动态资源 |
| `Common/WinFormsExtensions.cs` | ❌ **整文件移除** |
| `Common/MarkdownParser.cs` | 检查是否有 WinForms 引用，一般可保留 |
| `Common/CacheHelper.cs`, `CachePaths.cs` | 路径调整，使用 MAUI `FileSystem` |
| `Common/CategoryConfig.cs` | 如无 UI 依赖可保留 |
| `Models/UI/ConfettiParticle.cs` | `System.Drawing.PointF` → `Microsoft.Maui.Graphics.PointF` |
| `Data/Database/AppDbContext.cs` | 检查 DbContext 配置，MAUI Android 下 SQLite 路径用 `FileSystem.AppDataDirectory` |

#### 🔧 AppPaths 接口改造示例

```csharp
// LearningAssistant.Core/Abstractions/IAppPaths.cs
namespace LearningAssistant.Common.Abstractions;

public interface IAppPaths
{
    string AppDataDir { get; }       // 私有数据目录
    string CacheDir { get; }         // 缓存目录
    string LogsDir { get; }          // 日志目录
    string DatabasePath { get; }     // learning_assistant.db 完整路径
    string ConfigDir { get; }        // 配置目录
    string GetWordBankDir(SubCategoryType category);
}

// LearningAssistant.Platforms.Android/Services/AndroidAppPaths.cs
using Microsoft.Maui.Storage;

namespace LearningAssistant.Platforms.Android.Services;

public class AndroidAppPaths : IAppPaths
{
    public string AppDataDir => FileSystem.AppDataDirectory;
    public string CacheDir => FileSystem.CacheDirectory;
    public string LogsDir => Path.Combine(AppDataDir, "logs");
    public string DatabasePath => Path.Combine(AppDataDir, "learning_assistant.db");
    public string ConfigDir => Path.Combine(AppDataDir, "config");

    public string GetWordBankDir(SubCategoryType category)
        => Path.Combine(AppDataDir, "wordbanks", category.ToString());
}
```

---

### 5.2 服务层（Services）

#### ✅ 可直接复用的核心服务（业务逻辑 + 接口）

| 服务 | 说明 | 迁移状态 |
|------|------|----------|
| `WordMatchGameService` | 游戏取词 + 结果回写 | ✅ 100% 复用（无 UI 依赖） |
| `StudyEngine` + `IStudyEngine` | 学习引擎核心 | ✅ 99% 复用，调整 Progress 路径 |
| `ContentLoaderService` + `IContentLoaderService` | 词库加载/保存 | ✅ 95%，调整文件路径 |
| `GamificationService` + `IGamificationService` | 游戏化 XP/等级/统计 | ✅ 99% 复用 |
| `SqliteDataPersistenceService` | 数据持久化 | ✅ 调整路径 |
| `SqliteSpacedRepetitionService` + `ISpacedRepetitionService` | SM2/FSRS 间隔重复 | ✅ 100% 复用 |
| `PomodoroService` + `IPomodoroService` | 番茄钟逻辑 | ✅ 95%，Timer 换 `IDispatcherTimer` |
| `WrongAnswerService` + `IWrongAnswerService` | 错题本 | ✅ 100% 复用 |
| `NoteService` + `INoteService` | 笔记 | ✅ 100% 复用 |
| `LearningAnalyticsService` | 学习分析 | ✅ 100% 复用 |
| `LearningGoalService` | 学习目标 | ✅ 100% 复用 |
| `AchievementService` | 成就判定 | ✅ 100% 复用 |
| `LearningRecommendationService` | 推荐算法 | ✅ 100% 复用 |
| `FavoritesService` | 收藏夹 | ✅ 100% 复用 |
| `ProgressManager` | 进度管理 | ✅ 调整路径 |
| `CacheService` / `CacheManagerService` | 缓存 | ✅ 调整路径 |
| AI 服务系列 (`IAIService`, `DeepseekAIService` 等) | HTTP 调用 | ✅ 100% 复用 |
| `EncouragementService` | 鼓励文案 | ✅ 100% 复用 |
| `ExportService` / `DataImportService` | 导入导出 (Excel, JSON) | ✅ 路径调整 |
| `KnowledgeGraphService` | 知识图谱 | ✅ 100% 复用 |
| `QuoteService` | 名言 | ✅ 100% 复用 |

#### ⚠️ 需要平台化改造的服务（接口保留，实现替换）

| 原服务接口 | WinForms 实现 | MAUI Android 实现 |
|------------|--------------|-------------------|
| `ITTSService` | `KokoroSharpTtsService` / `QwenTtsService` / `System.Speech` | **`AndroidTtsService`** (Android.Speech.Tts.TextToSpeech) + 可选保留 QwenTts HTTP |
| `ISoundService` | 依赖 NAudio | **`AndroidSoundService`** (`MediaPlayer` / `Plugin.AudioManager`) |
| `INotificationService` / `ICelebrationService` | WinForms Toast / 弹窗 | **`AndroidNotificationService`** (`NotificationManager` + 前台服务) |
| `IThemeService` (原) | 注册 IThemeable + WinForms 颜色 | **MAUI 主题系统** (`AppThemeBinding` + `ResourceDictionary` 切换) |
| `IUserSessionService` | 简单内存实现 | ✅ 可保留逻辑 |
| `IAudioService` | NAudio | `Plugin.AudioManager` 或 `MediaElement` |
| `IWebSpeechService` | WebView2 speech | ✅ BlazorWebView 内 JS Web Speech API 可保留 |
| `ITrayIconService` | 系统托盘图标 | ❌ 移除（Android 用通知 + 桌面小组件代替番茄钟提醒） |
| `IHotkeyService` | 全局热键 | ❌ 移除（Android 无全局热键概念，改用手势和按钮） |

#### 🔧 AndroidTtsService 实现示例

```csharp
// LearningAssistant.Platforms.Android/Services/AndroidTtsService.cs
using Android.Content;
using Android.Runtime;
using Android.Speech.Tts;
using LearningAssistant.Services.TTS;
using Microsoft.Maui.Controls;

namespace LearningAssistant.Platforms.Android.Services;

public class AndroidTtsService : Java.Lang.Object, ITTSService, TextToSpeech.IOnInitListener
{
    private TextToSpeech? _tts;
    private bool _ready;
    private readonly Context _context;

    public AndroidTtsService()
    {
        _context = Android.App.Application.Context;
        _tts = new TextToSpeech(_context, this);
    }

    public void OnInit([GeneratedEnum] OperationResult status)
    {
        if (status == OperationResult.Success)
        {
            _tts!.SetLanguage(new Java.Util.Locale("en", "US"));
            _ready = true;
        }
    }

    public Task SpeakAsync(string text, string lang = "en-US", float rate = 1.0f, CancellationToken ct = default)
    {
        if (!_ready || _tts == null) return Task.CompletedTask;

        var locale = lang.StartsWith("zh") ? Java.Util.Locale.Chinese : Java.Util.Locale.English;
        _tts.SetLanguage(locale);
        _tts.SetSpeechRate(rate);
        _tts.Speak(text, QueueMode.Flush, null, null);
        return Task.CompletedTask;
    }

    public void Stop() => _tts?.Stop();
    public bool IsReady => _ready;

    public new void Dispose()
    {
        _tts?.Shutdown();
        _tts?.Dispose();
        base.Dispose();
    }
}
```

---

### 5.3 学习主界面（LearningForm → LearningPage）

#### 原 LearningForm 组成分析

```
原 LearningForm (WinForms)
├── 顶部: 设置面板 (科目选择、模式、TTS开关、主题切换、速度选择)
├── 左侧: 学习列表 (LearningListView - ListBox)
├── 中间: 学习卡片 (LearningContentView + LearningCard)
├── 下方: 操作按钮 (已知/未知/收藏/显示答案)
├── 右下: 统计面板 (学习时长、分数、今日、连击、进度条)
├── 底部: 番茄钟 (PomodoroTimer)
└── 右上角: 日目标进度环 (CircularProgressControl)
```

#### MAUI 对应布局设计 (LearningPage.xaml)

```xml
<!-- LearningPage.xaml: 使用 Grid + Shell -->
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:LearningAssistant.MauiApp.ViewModels"
             xmlns:ctrl="clr-namespace:LearningAssistant.MauiApp.Controls"
             x:Class="LearningAssistant.MauiApp.Views.LearningPage"
             Title="学习中心">
    <ContentPage.BindingContext>
        <vm:LearningViewModel />
    </ContentPage.BindingContext>

    <Grid RowDefinitions="Auto, Auto, *, Auto"
          ColumnDefinitions="0.3*, 0.7*">
        <!-- Row 0: 顶部设置栏 (横屏/平板显示两列，小屏自适应) -->
        <Grid Grid.Row="0" Grid.ColumnSpan="2" BackgroundColor="{StaticResource Surface}">
            <HorizontalStackLayout Spacing="8" Padding="8,6">
                <Picker Title="科目" ItemsSource="{Binding Subjects}" 
                        SelectedItem="{Binding SelectedSubject}" WidthRequest="100"/>
                <Picker Title="分类" ItemsSource="{Binding SubCategories}" 
                        SelectedItem="{Binding SelectedSubCategory}" WidthRequest="120"/>
                <Picker Title="模式" ItemsSource="{Binding StudyModes}" 
                        SelectedItem="{Binding SelectedMode}"/>
                <Switch IsToggled="{Binding VoiceEnabled}" />
                <Label Text="TTS" VerticalOptions="Center"/>
                <Button Text="切换主题" Command="{Binding ToggleThemeCommand}" Style="{StaticResource GhostButton}"/>
            </HorizontalStackLayout>
        </Grid>

        <!-- Row 1: 统计条 + 番茄钟 -->
        <Grid Grid.Row="1" Grid.ColumnSpan="2" Padding="8,4">
            <Grid ColumnDefinitions="*, Auto">
                <!-- 统计 -->
                <HorizontalStackLayout Spacing="16">
                    <ctrl:StatCard Label="学习时长" Value="{Binding StudyTimeText}" />
                    <ctrl:StatCard Label="分数" Value="{Binding Score}" />
                    <ctrl:StatCard Label="今日" Value="{Binding TodayCount}" />
                    <ctrl:StatCard Label="连击" Value="{Binding StreakDays}" />
                </HorizontalStackLayout>
                <!-- 番茄钟 -->
                <ctrl:PomodoroTimerView Grid.Column="1" 
                                        State="{Binding PomodoroState}"
                                        TimeRemaining="{Binding PomodoroRemaining}"
                                        StartCommand="{Binding StartPomodoroCommand}"
                                        StopCommand="{Binding StopPomodoroCommand}"/>
            </Grid>
        </Grid>

        <!-- Row 2 Col 0: 学习列表 (手机端可切换 Tab) -->
        <CollectionView Grid.Row="2" Grid.Column="0"
                        ItemsSource="{Binding LearningItems}"
                        SelectedItem="{Binding CurrentItem}"
                        ItemTemplate="{StaticResource LearningItemTemplate}"/>

        <!-- Row 2 Col 1: 学习卡片 + 进度环 -->
        <Grid Grid.Row="2" Grid.Column="1" Padding="8">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <!-- 右上角进度环 -->
            <ctrl:CircularProgress Grid.Row="0" 
                                   HorizontalOptions="End"
                                   Progress="{Binding DailyGoalProgress}"
                                   SizeRequest="80"/>
            <!-- 学习卡片 -->
            <ctrl:LearningCardView Grid.Row="1"
                                   Item="{Binding CurrentItem}"
                                   IsAnswerRevealed="{Binding IsAnswerShown}"
                                   RevealAnswerCommand="{Binding ShowAnswerCommand}"
                                   SpeakCommand="{Binding SpeakCommand}"
                                   AddNoteCommand="{Binding AddNoteCommand}"/>
            <!-- 操作按钮 -->
            <HorizontalStackLayout Grid.Row="2" Spacing="8" HorizontalOptions="Center" Padding="0,8">
                <Button Text="⭐ 收藏" Command="{Binding ToggleFavoriteCommand}" 
                        BackgroundColor="{StaticResource Gold}" WidthRequest="100"/>
                <Button Text="❓ 未知" Command="{Binding MarkUnknownCommand}"
                        BackgroundColor="{StaticResource Error}" WidthRequest="100"/>
                <Button Text="✅ 已知" Command="{Binding MarkKnownCommand}"
                        BackgroundColor="{StaticResource Success}" WidthRequest="100"/>
            </HorizontalStackLayout>
        </Grid>

        <!-- Row 3: 进度条 + 鼓励文案 -->
        <VerticalStackLayout Grid.Row="3" Grid.ColumnSpan="2" Padding="8,6">
            <ProgressBar Progress="{Binding TotalProgress}" />
            <Label Text="{Binding EncouragementText}" 
                   HorizontalOptions="Center" FontSize="Caption" TextColor="{StaticResource Tertiary}"/>
        </VerticalStackLayout>
    </Grid>
</ContentPage>
```

#### LearningViewModel 核心映射

```csharp
// ViewModels/LearningViewModel.cs
public partial class LearningViewModel : ObservableObject, IQueryAttributable
{
    // 原 LearningPresenter + LearningForm 逻辑迁移
    private readonly IStudyEngine _studyEngine;
    private readonly IGamificationService _gamification;
    private readonly IPomodoroService _pomodoro;
    private readonly ITTSService _tts;
    private readonly IEncouragementService _encouragement;

    [ObservableProperty] private LearningItem? _currentItem;
    [ObservableProperty] private bool _isAnswerShown;
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _todayCount;
    [ObservableProperty] private int _streakDays;
    [ObservableProperty] private string _studyTimeText = "00:00";
    [ObservableProperty] private double _dailyGoalProgress;
    [ObservableProperty] private double _totalProgress;
    [ObservableProperty] private string _encouragementText = "";

    // 命令 = 原 WinForms Button.Click 处理
    [RelayCommand] private Task MarkKnown() => ProcessAnswer(true);
    [RelayCommand] private Task MarkUnknown() => ProcessAnswer(false);
    [RelayCommand] private void ShowAnswer() => IsAnswerShown = true;
    [RelayCommand] private async Task Speak(string text) => await _tts.SpeakAsync(text);
    [RelayCommand] private void ToggleFavorite() { /* ... */ }

    // 番茄钟
    [ObservableProperty] private PomodoroState _pomodoroState;
    [ObservableProperty] private TimeSpan _pomodoroRemaining;
    [RelayCommand] private void StartPomodoro() => _pomodoro.Start();
    [RelayCommand] private void StopPomodoro() => _pomodoro.Stop();
}
```

---

### 5.4 五大游戏模块

#### 5.4.1 架构决策：BlazorWebView 宿主方案

**推荐方案: MAUI `BlazorWebView` + 现有 HTML/JS 资源**

| 对比项 | MAUI WebView | MAUI BlazorWebView | 纯原生 XAML 重写 |
|--------|-------------|--------------------|------------------|
| 现有 HTML/JS 复用率 | 90% | 95% | 0% |
| C# ↔ JS 通信 | `EvaluateJavaScriptAsync` + 自定义 handler | 内置 `IJSRuntime` + 双向互操作 | 无需 JS |
| 资源打包 | `MauiAsset` (Raw 资产) | `wwwroot` 资产 | - |
| 开发成本 | 中 | 低（本项目最合适） | 极高（5个游戏重写） |
| 调试体验 | Chrome 远程调试 | Chrome 远程调试 + VS Blazor 调试 | MAUI XAML 调试 |
| 性能 | 好 | 好 | 最优 |

**结论**: 游戏模块现有 5 套 HTML/JS/CSS 完整可用，迁移选择 **BlazorWebView 方案**，95% 前端代码直接复用。

#### 5.4.2 BlazorWebView 游戏宿主架构

```
BlazorWebView 方案数据流
────────────────────────────────

  MAUI Services (C#)
        │ 注入
        ▼
  GameViewModelBase (C#)
        │ 1. 取词 BuildItems()
        │ 2. 序列化
        ▼
  Blazor Component (GameHost.razor)
        │ @inject ViewModel
        │ <BlazorWebView HostPage="wwwroot/games/WordMatchGame/index.html">
        │     <script src="BlazorWebView.js"></script>
        │
        │ C# → JS:  DotNet.invokeMethodAsync("InitGame", dataJson)
        ▼
  HTML/JS 游戏层 (几乎不变)
        │ window.DotNet 可用
        │ JS → C#:  DotNet.invokeMethodAsync("GameEnded", resultsJson)
        ▼
  GameViewModelBase (C#)
        │ 3. ApplyResults() 回写学习状态
        │ 4. 触发 XP/成就事件
        ▼
  EF Core / SQLite
```

#### 5.4.3 游戏基类改造（原 WebView2GameFormBase → GameViewModelBase）

```csharp
// ViewModels/Games/GameViewModelBase.cs
namespace LearningAssistant.MauiApp.ViewModels.Games;

/// <summary>
/// MAUI 游戏 ViewModel 基类（对应原 WebView2GameFormBase）
/// 职责：科目选择 → 取词 → 发给前端 → 接收结果 → 回写学习状态
/// </summary>
public abstract class GameViewModelBase : ObservableObject, IAsyncDisposable
{
    protected readonly WordMatchGameService GameService;
    protected readonly IContentLoaderService ContentLoader;
    protected readonly IUserSessionService UserSession;
    protected readonly ILogger Logger;

    [ObservableProperty] private SubjectType _selectedSubject = SubjectType.English;
    [ObservableProperty] private SubCategoryType _selectedSubCategory = SubCategoryType.EnglishWord;
    [ObservableProperty] private List<object> _availableSubCategories = [];
    [ObservableProperty] private bool _isGameReady;
    [ObservableProperty] private string _gameTitle = "";

    private LearningContext? _currentContext;

    // 子类实现：HTML 路径 + 游戏数据构建 + 结果回写
    protected abstract string GameHtmlPath { get; }
    protected abstract object? BuildData(LearningContext context, string theme);
    protected abstract void OnGameEnd(JsonElement gameRoot, LearningContext context);

    // → Blazor 调用：获取初始化 JSON
    [JSInvokable]
    public string? RequestInitData()
    {
        var context = new LearningContext(UserSession.CurrentUserId, SelectedSubject, SelectedSubCategory);
        _currentContext = context;
        var theme = AppInfo.Current.RequestedTheme == AppTheme.Dark ? "dark" : "light";
        var data = BuildData(context, theme);
        return data == null ? null : JsonSerializer.Serialize(new { type = "init", data, theme }, 
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // ← Blazor 调用：前端上报 gameEnd
    [JSInvokable]
    public Task SubmitGameResult(string json)
    {
        if (_currentContext == null) return Task.CompletedTask;
        using var doc = JsonDocument.Parse(json);
        OnGameEnd(doc.RootElement, _currentContext);
        return Task.CompletedTask;
    }

    // ← Blazor 调用：TTS 朗读（对应原 SpeakHost）
    [JSInvokable]
    public Task SpeakHost(string text, string lang = "en-US")
    {
        // 调用 ITTSService
        return Task.CompletedTask;
    }

    // ← Blazor 调用：换一组 (restart)
    [JSInvokable]
    public string? Restart() => RequestInitData();
}
```

#### 5.4.4 五大游戏子类映射表

| 原 WinForms 类 | MAUI ViewModel 类 | BuildItems 参数 | 对应 HTML 资源 |
|---------------|-------------------|-----------------|---------------|
| `WordMatchGameForm` | `WordMatchGameViewModel : GameViewModelBase` | `maxCount: 10, Random` | `wwwroot/games/WordMatchGame/` ✅ 直接复制 |
| `MemoryMatchGameForm` | `MemoryMatchGameViewModel : GameViewModelBase` | `maxCount: 8, WrongFirst` | `wwwroot/games/MemoryMatchGame/` ✅ 直接复制 |
| `LinkMatchGameForm` | `LinkMatchGameViewModel : GameViewModelBase` | `maxCount: 10, WrongFirst` | `wwwroot/games/LinkMatchGame/` ✅ 直接复制 |
| `SpellingGameForm` | `SpellingGameViewModel : GameViewModelBase` | `maxCount: 10, WrongFirst` | `wwwroot/games/SpellingGame/` ✅ 直接复制 |
| `WhackAMoleGameForm` | `WhackAMoleGameViewModel : GameViewModelBase` | `maxCount: 12, Random` | `wwwroot/games/WhackAMoleGame/` ✅ 直接复制 |

> 每个子类的 `BuildData()` 和 `OnGameEnd()` 逻辑与原 WinForms 版本**完全相同**，直接复制即可。

#### 5.4.5 Blazor 游戏宿主组件

```razor
<!-- Components/GameHost.razor -->
@typeparam TViewModel
@implements IAsyncDisposable
@inject TViewModel VM
@inject IJSRuntime JS

<BlazorWebView HostPage="wwwroot/games/index.html">
    <BlazorWebView.RootComponents>
        <RootComponent Selector="#app" ComponentType="typeof(GameBridge{TViewModel})" Parameters="@(new { ViewModel = VM })" />
    </BlazorWebView.RootComponents>
</BlazorWebView>

<!-- GameBridge 接收 JS 事件并转发给 VM -->
```

#### 5.4.6 前端 JS 通信适配（最小改动）

原 WebView2 通信代码：
```js
// 原 shared.js (WebView2)
window.GameUI.sendToHost = function(obj) {
    window.chrome.webview.postMessage(obj);
};
window.chrome.webview.addEventListener('message', e => handleInit(e.data));
```

MAUI BlazorWebView 适配后：
```js
// 新 shared.js (BlazorWebView 兼容层)
// 仅需替换这一个文件，各游戏 game.js 无需修改
(function() {
    "use strict";

    function sendToHost(obj) {
        if (obj.type === "gameEnd") {
            DotNet.invokeMethodAsync("LearningAssistant.MauiApp", "SubmitGameResult", JSON.stringify(obj));
        } else if (obj.type === "restart") {
            DotNet.invokeMethodAsync("LearningAssistant.MauiApp", "Restart")
                .then(data => { if (data) handleInit(JSON.parse(data)); });
        } else if (obj.type === "speak") {
            DotNet.invokeMethodAsync("LearningAssistant.MauiApp", "SpeakHost", obj.text || "", obj.lang || "en-US");
        }
    }

    // Blazor 端初始化后主动调用
    window.GameUI = {
        sendToHost: sendToHost,
        // ...其他方法保持不变
    };

    // Blazor 主动推送 init 数据
    window.receiveInitData = function(jsonStr) {
        handleInit(JSON.parse(jsonStr));
    };
})();
```

> **关键收益**: 5 个游戏的 `game.js`、`index.html`、`style.css` 均 **0 改动**，仅需替换一个 `shared.js` 通信适配层。

---

### 5.5 复习与主动回忆

| 原 WinForms | MAUI 对应 | 迁移要点 |
|------------|-----------|----------|
| `ReviewForm` + `ReviewPanel` | `ReviewPage` + `ReviewViewModel` | 间隔重复日程列表 + 启动复习跳转 |
| `ActiveRecallForm` | `ActiveRecallPage` | 看问题 → 键盘输入答案 → 判分。`System.Windows.Forms.Timer` → `IDispatcherTimer` |
| `FlashcardReviewForm` | `FlashcardReviewPage` | 闪卡翻转动画可使用 MAUI `VisualStateManager` + `RotationY` 动画 |
| `ProgressiveHintForm` | `ProgressiveHintHintPage` | 渐进式提示显示，纯 ViewModel 逻辑可复用 |

#### ActiveRecallPage 核心 XAML

```xml
<ContentPage Title="主动回忆训练">
    <VerticalStackLayout Padding="24" Spacing="16">
        <!-- 进度 -->
        <Grid>
            <ProgressBar Progress="{Binding Progress}" />
            <Label Grid.Column="1" Text="{Binding ProgressText}" HorizontalOptions="End"/>
        </Grid>

        <!-- 问题卡片 -->
        <Frame Style="{StaticResource CardStyle}">
            <VerticalStackLayout Spacing="12">
                <Label Text="❓ 问题" FontSize="Caption" TextColor="{StaticResource Tertiary}"/>
                <Label Text="{Binding CurrentQuestion}" FontSize="Title" FontAttributes="Bold"/>
                <Label Text="{Binding Hint}" FontSize="Body" TextColor="{StaticResource Secondary}"
                       IsVisible="{Binding ShowHint}"/>
            </VerticalStackLayout>
        </Frame>

        <!-- 答题区 -->
        <Entry Placeholder="输入答案..." Text="{Binding UserAnswer}" Completed="OnCheckAnswer"/>
        <Button Text="提交答案" Command="{Binding CheckAnswerCommand}" Style="{StaticResource PrimaryButton}"/>

        <!-- 结果反馈 -->
        <Label Text="{Binding ResultText}" TextColor="{Binding ResultColor}" FontSize="Headline" HorizontalOptions="Center"/>

        <!-- 统计 -->
        <HorizontalStackLayout HorizontalOptions="Center" Spacing="24">
            <Label Text="正确: {Binding CorrectCount}"/>
            <Label Text="正确率: {Binding AccuracyText}"/>
        </HorizontalStackLayout>
    </VerticalStackLayout>
</ContentPage>
```

---

### 5.6 游戏化系统（成就/徽章/挑战）

| 原 WinForms 组件 | MAUI 方案 |
|-----------------|-----------|
| `GamificationService` | ✅ 服务层 100% 复用，事件订阅在 ViewModel 层 |
| `AchievementForm` | `AchievementPage.xaml` → `CollectionView` + `AchievementCard` 控件 |
| `AchievementNotificationForm` | MAUI `Toast` 通知 + 自定义弹出层 (`CommunityToolkit.Maui.Popup`) |
| `BadgeManager` + `Badge` 模型 | ✅ 服务逻辑 100% 复用，UI 用 `LevelBadge` 控件 |
| `ChallengeManager` + `ChallengeForm` | `ChallengePage.xaml` + `ChallengeCard` 控件 |
| `ConfettiManager` + `ConfettiControl` (WinForms GDI) | **`SkiaSharp.Views.Maui.Controls.SKCanvasView`** 重写 Confetti 粒子动画 |
| `EncouragementManager` | ✅ 100% 复用，在 ViewModel 中绑定 `EncouragementText` |

#### Confetti (撒花) MAUI 实现

```xml
<!-- Controls/ConfettiCanvas.xaml -->
<skia:SKCanvasView x:Class="LearningAssistant.MauiApp.Controls.ConfettiCanvas"
                   PaintSurface="OnPaintSurface"
                   HorizontalOptions="Fill" VerticalOptions="Fill"
                   InputTransparent="True"/> <!-- 不拦截触摸 -->
```

```csharp
// Controls/ConfettiCanvas.xaml.cs
private ConfettiParticle[] _particles = new ConfettiParticle[100];
private readonly IDispatcherTimer _timer;

public ConfettiCanvas()
{
    _timer = Dispatcher.CreateTimer();
    _timer.Interval = TimeSpan.FromMilliseconds(16); // 60fps
    _timer.Tick += (_, _) => InvalidateSurface();
}

public void StartBurst(int count = 100)
{
    // 初始化粒子位置/速度/颜色 (原 ConfettiManager 逻辑可复用)
    _timer.Start();
}

private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
{
    var canvas = e.Surface.Canvas;
    canvas.Clear();
    foreach (ref var p in _particles.AsSpan())
    {
        if (!p.Alive) continue;
        p.Update(deltaMs: 16);  // 原物理逻辑 100% 复用
        using var paint = new SKPaint { Color = p.Color.ToSkia() };
        canvas.DrawRect(p.X, p.Y, p.Size, p.Size, paint);
    }
}
```

---

### 5.7 联想学习与费曼技巧

| 原组件 | MAUI 方案 |
|--------|-----------|
| `AssociationLearningForm` (TreeView + AI 联想) | `AssociationLearningPage.xaml`<br>TreeView → MAUI `TreeView` (NET 8 MAUI 内置) 或 `Syncfusion.Maui.TreeView` (第三方)<br>AI 联想 `IAiQuestionService` 调用完全复用 |
| `FeynmanLearningPanel` (用户控件) | `FeynmanLearningView.xaml` (`ContentView`) + `FeynmanViewModel`<br>录音/回放功能: Android `MediaRecorder` + `MediaPlayer` |

#### 联想学习 MAUI TreeView 示例

```xml
<!-- Views/AssociationLearningPage.xaml 片段 -->
<TreeView ItemsSource="{Binding AssociationNodes}">
    <TreeView.ItemTemplate>
        <DataTemplate>
            <TreeViewNode ItemsSource="{Binding Children}" IsExpanded="True">
                <HorizontalStackLayout Spacing="8" VerticalOptions="Center">
                    <Label Text="{Binding Icon}" FontSize="Large"/>
                    <VerticalStackLayout>
                        <Label Text="{Binding Title}" FontAttributes="Bold"/>
                        <Label Text="{Binding Detail}" FontSize="Caption" TextColor="{StaticResource Tertiary}"/>
                    </VerticalStackLayout>
                </HorizontalStackLayout>
            </TreeViewNode>
        </DataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

---

### 5.8 番茄钟

| 原 WinForms | MAUI Android |
|------------|--------------|
| `PomodoroTimer` (UserControl + Timer) | `PomodoroTimerView.xaml` (ContentView) + `IDispatcherTimer` |
| 番茄钟后台运行 | ⚠️ **Android 前台服务** (`ForegroundServiceType = ForegroundService.TypeSpecialUse`)<br>或使用 `Plugin.LocalNotification` + 广播 |
| 系统托盘番茄钟 (`ITrayIconService` / `PomodoroTrayIntegration`) | ❌ Android 无托盘 → 替换为**状态栏通知** (Notification + `PendingIntent`) |

#### Android 前台服务配置（番茄钟必需）

```xml
<!-- Platforms/Android/AndroidManifest.xml 新增 -->
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_SPECIAL_USE" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.WAKE_LOCK" />

<application ...>
    <service android:name=".PomodoroForegroundService"
             android:foregroundServiceType="specialUse"
             android:exported="false">
    </service>
</application>
```

---

### 5.9 内容管理与错题本

| 原组件 | MAUI 方案 |
|--------|-----------|
| `ContentEditorForm` (内容编辑/导入导出) | `ContentEditorPage.xaml`<br>EPPlus Excel 导入可保留 ✅<br>文件选择: MAUI `FilePicker.Default.PickAsync()` |
| `WrongAnswerForm` + `WrongAnswerStatsPanel` | `WrongAnswerPage.xaml` + `StatsPage.xaml`<br>图表: `ScottPlot.Maui` 或 `Microcharts` |
| `NotesForm` + `AddNoteForm` | `NotesPage.xaml` + `NoteEditorPage.xaml` |
| `ResultForm` (学习结果) | `ResultPage.xaml` → 学习结束结算页 |
| `LearningManagementForm` | `ManagementPage.xaml` → 词库/分类管理 |
| `LearningStatsButtonView` + `LearningStatsView` + `Charts` | `StatsPage.xaml` + 图表控件 |

---

## 6. 前端游戏（HTML/JS）适配方案

### 6.1 资源打包方式

**MAUI Android BlazorWebView 要求静态资源放在 `<项目>/wwwroot/` 下**：

```
LearningAssistant.MauiApp/
└── wwwroot/
    ├── games/
    │   ├── Shared/
    │   │   ├── shared.css        # ✅ 直接复制 (除 shared.js 需适配)
    │   │   └── shared.js         # ⚠️ 替换通信适配层 (见 5.4.6)
    │   ├── WordMatchGame/
    │   │   ├── index.html        # ✅ 0 改动复制
    │   │   ├── game.js           # ✅ 0 改动复制
    │   │   └── style.css         # ✅ 0 改动复制
    │   ├── MemoryMatchGame/      # ✅ 同上
    │   ├── LinkMatchGame/        # ✅ 同上
    │   ├── SpellingGame/         # ✅ 同上
    │   └── WhackAMoleGame/       # ✅ 同上
    └── css/app.css
```

`.csproj` 中资源属性由 MAUI 自动处理（`wwwroot` 默认为 `BlazorWebViewAsset`）。

### 6.2 通信协议完全兼容

原 WebView2 ↔ JS 协议结构不变：

```
C# → JS (init 数据)
{
    type: "init",
    data: [ { id, word, meaning, phonetic, example }, ... ],
    theme: "light" | "dark"
}

JS → C# (结果上报)
{
    type: "gameEnd",
    results: [ { id, correct }, ... ]
}

JS → C# (换一组)
{ type: "restart" }

JS → C# (TTS)
{ type: "speak", text: "apple", lang: "en-US" }
```

### 6.3 移动端响应式适配

原游戏已用 `viewport` meta 标签，在手机屏幕上需做以下 CSS 微调（不影响逻辑）：

```css
/* 新增到 shared.css 末尾，Android 小屏适配 */
@media (max-width: 600px) {
    .card { min-height: 60px; font-size: 14px; }
    .board { gap: 6px; padding: 8px; }
    .topbar { flex-direction: column; height: auto; padding: 8px; }
    .stats { font-size: 12px; gap: 8px; }
    .hint { font-size: 12px; padding: 8px; }
}
```

### 6.4 Android 键盘弹出适配（SpellingGame 拼写游戏）

单词拼写游戏需要弹出软键盘。在 MAUI `BlazorWebView` 中：

```xml
<!-- Platforms/Android/AndroidManifest.xml -->
<activity android:name=".MainActivity"
          android:configChanges="orientation|screenSize|keyboard|keyboardHidden"
          android:windowSoftInputMode="adjustResize" />
```

`adjustResize` 会在键盘弹出时自动缩放 WebView 内容区域。

---

## 7. 数据持久化迁移

### 7.1 EF Core SQLite 兼容

原项目使用：
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.8" />
```

MAUI Android 同版本 EF Core 完全兼容 ✅。仅需：

```csharp
// MauiProgram.cs 注册 DbContext
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    var appPaths = builder.Services.BuildServiceProvider().GetRequiredService<IAppPaths>();
    options.UseSqlite($"Data Source={appPaths.DatabasePath}");
});
```

### 7.2 数据库文件迁移（从 WinForms 导入）

提供"迁移数据"功能页：

1. 用户通过 `FilePicker` 选择原 `learning_assistant.db` 文件
2. 复制到 MAUI `FileSystem.AppDataDirectory`
3. 自动执行 EF Core 迁移（如 schema 变化）

```csharp
// IDbMigratorService.cs
public async Task<bool> ImportDatabaseFromFileAsync(string sourceDbPath)
{
    var dest = _appPaths.DatabasePath;
    using var src = File.OpenRead(sourceDbPath);
    using var dst = File.Create(dest);
    await src.CopyToAsync(dst);

    // 应用迁移
    using var db = _dbFactory.CreateDbContext();
    await db.Database.MigrateAsync();
    return true;
}
```

### 7.3 现有词库 JSON 导入导出

原 `IDataImportService` / `IExportService` 基于 JSON/Excel，逻辑 **100% 复用** ✅。
- 导入：`FilePicker.Default.PickAsync()`
- 导出：`FileSystem.AppDataDirectory` + `Share.Default.RequestAsync()` 分享给用户

---

## 8. Android 平台特有适配

### 8.1 权限清单（AndroidManifest.xml）

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <!-- 基础 -->
    <uses-permission android:name="android.permission.INTERNET" />                    <!-- AI/TTS HTTP -->
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />

    <!-- 文件读写 (Android 13+ 细化权限) -->
    <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" android:maxSdkVersion="32" />
    <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" android:maxSdkVersion="28" />
    <uses-permission android:name="android.permission.READ_MEDIA_AUDIO" />
    <uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />

    <!-- 番茄钟 前台服务 + 通知 -->
    <uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
    <uses-permission android:name="android.permission.FOREGROUND_SERVICE_SPECIAL_USE" />
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />      <!-- Android 13+ -->
    <uses-permission android:name="android.permission.SCHEDULE_EXACT_ALARM" />   <!-- 精确提醒 -->
    <uses-permission android:name="android.permission.USE_EXACT_ALARM" />
    <uses-permission android:name="android.permission.WAKE_LOCK" />

    <!-- TTS + 录音 (费曼学习) -->
    <uses-permission android:name="android.permission.RECORD_AUDIO" />
    <uses-permission android:name="android.permission.MODIFY_AUDIO_SETTINGS" />
    <queries>
        <intent>
            <action android:name="android.intent.action.TTS_SERVICE" />
        </intent>
    </queries>
</manifest>
```

### 8.2 运行时权限请求

MAUI 使用 `Permissions` API：

```csharp
// 启动时检查必要权限
async Task EnsurePermissionsAsync()
{
    var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
    if (status != PermissionStatus.Granted)
        status = await Permissions.RequestAsync<Permissions.PostNotifications>();

    if (Permissions.ShouldShowRationale<Permissions.PostNotifications>())
    {
        await Shell.Current.DisplayAlert("权限说明", 
            "通知权限用于番茄钟提醒和成就通知。", "好的");
    }
}
```

### 8.3 生命周期适配

| WinForms 事件 | MAUI / Android 对应 |
|--------------|---------------------|
| `Form_Load` | `Page.OnAppearing()` |
| `Form_FormClosing` | `Page.OnDisappearing()` |
| `Application.ApplicationExit` | `MauiProgram.LifecycleBuilder.OnDestroy` |
| 番茄钟后台继续 | `Android.App.Service` (前台服务) |
| 屏幕旋转自适应 | MAUI `OnSizeAllocated` + 响应式布局 |

---

## 9. 依赖注入与启动配置

### 9.1 MauiProgram.cs 模板

```csharp
// LearningAssistant.MauiApp/MauiProgram.cs
using LearningAssistant.Common.Abstractions;
using LearningAssistant.Platforms.Android.Services;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Gamification;
using Microsoft.EntityFrameworkCore;
using Serilog; // 可选

namespace LearningAssistant.MauiApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
               .UseMauiCommunityToolkit()
               .UseSkiaSharp()
               .ConfigureFonts(fonts =>
               {
                   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
               });

        // ==============================================
        // 1. 配置 (原 ServiceCollectionExtensions.AddConfigurationServices)
        // ==============================================
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        builder.Configuration.AddConfiguration(configBuilder);
        builder.Services.AddSingleton(_ => configBuilder.Get<AppConfig>() ?? new AppConfig());

        // ==============================================
        // 2. 平台实现 (Android 特有)
        // ==============================================
        builder.Services.AddSingleton<IAppPaths, AndroidAppPaths>();
        builder.Services.AddSingleton<ITTSService, AndroidTtsService>();
        builder.Services.AddSingleton<ISoundService, AndroidSoundService>();
        builder.Services.AddSingleton<INotificationService, AndroidNotificationService>();
        builder.Services.AddSingleton<IAudioService, AndroidAudioService>();

        // ==============================================
        // 3. 数据层 (原 ServiceCollectionExtensions.AddDatabaseServices)
        // ==============================================
        builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var paths = sp.GetRequiredService<IAppPaths>();
            options.UseSqlite($"Data Source={paths.DatabasePath}");
        });

        // ==============================================
        // 4. 核心服务 (原 AddCoreServices + AddLearningServices 等)
        // ==============================================
        // 事件总线
        builder.Services.AddSingleton<IEventBus, EventBus>();
        // 持久化
        builder.Services.AddSingleton<IDataPersistenceService, SqliteDataPersistenceService>();
        builder.Services.AddSingleton<ICacheService>(sp =>
        {
            var paths = sp.GetRequiredService<IAppPaths>();
            return new CacheService(Path.Combine(paths.CacheDir, "cache.json"),
                sp.GetService<ILogger<CacheService>>());
        });
        // 内容加载
        builder.Services.AddSingleton<IContentLoaderService, ContentLoaderService>();
        builder.Services.AddSingleton<IUserSessionService, UserSessionService>();
        // 学习引擎
        builder.Services.AddSingleton<IStudyEngine, StudyEngine>();
        builder.Services.AddSingleton<IProgressManager, ProgressManager>();
        // 游戏
        builder.Services.AddSingleton<WordMatchGameService>();
        // 复习
        builder.Services.AddSingleton<ISpacedRepetitionService, SqliteSpacedRepetitionService>();
        // 番茄钟
        builder.Services.AddSingleton<IPomodoroService, PomodoroService>();
        // 错题本 / 笔记
        builder.Services.AddSingleton<IWrongAnswerService, WrongAnswerService>();
        builder.Services.AddSingleton<INoteService, NoteService>();
        // 游戏化
        builder.Services.AddSingleton<IGamificationService, GamificationService>();
        builder.Services.AddSingleton<IEncouragementService, EncouragementService>();
        // AI 服务 (HTTP 调用，完全复用)
        builder.Services.AddHttpClient<IAIService, DeepseekAIService>();
        builder.Services.AddSingleton<IAiQuestionService, AiQuestionService>();
        // 统计分析
        builder.Services.AddSingleton<ILearningAnalyticsService, LearningAnalyticsService>();
        builder.Services.AddSingleton<ILearningGoalService, LearningGoalService>();
        // 导入导出
        builder.Services.AddSingleton<ExportService>();
        builder.Services.AddSingleton<IDataImportService, DataImportService>();
        // ... (完整列表参照原 ServiceCollectionExtensions)

        // ==============================================
        // 5. ViewModels + Pages
        // ==============================================
        builder.Services.AddTransient<LearningViewModel>();
        builder.Services.AddTransient<ReviewViewModel>();
        builder.Services.AddTransient<ActiveRecallViewModel>();
        builder.Services.AddTransient<WordMatchGameViewModel>();
        builder.Services.AddTransient<MemoryMatchGameViewModel>();
        builder.Services.AddTransient<LinkMatchGameViewModel>();
        builder.Services.AddTransient<SpellingGameViewModel>();
        builder.Services.AddTransient<WhackAMoleGameViewModel>();
        builder.Services.AddTransient<GamificationViewModel>();
        // ...

        builder.Services.AddTransient<LearningPage>();
        builder.Services.AddTransient<ReviewPage>();
        builder.Services.AddTransient<GameHostPage>();
        // ...

        // ==============================================
        // 6. 日志 (原 AddLoggingServices)
        // ==============================================
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        return builder.Build();
    }
}
```

### 9.2 AppShell 导航配置

```xml
<!-- AppShell.xaml -->
<Shell>
    <FlyoutItem Title="学习" Icon="book.png">
        <ShellContent ContentTemplate="{DataTemplate views:LearningPage}" Route="Learning"/>
    </FlyoutItem>
    <FlyoutItem Title="游戏中心" Icon="gamepad.png">
        <ShellContent ContentTemplate="{DataTemplate views:GamesHubPage}" Route="Games"/>
    </FlyoutItem>
    <FlyoutItem Title="复习" Icon="review.png">
        <ShellContent ContentTemplate="{DataTemplate views:ReviewPage}" Route="Review"/>
    </FlyoutItem>
    <FlyoutItem Title="统计" Icon="chart.png">
        <ShellContent ContentTemplate="{DataTemplate views:StatsPage}" Route="Stats"/>
    </FlyoutItem>
    <FlyoutItem Title="成就" Icon="trophy.png">
        <ShellContent ContentTemplate="{DataTemplate views:AchievementPage}" Route="Achievement"/>
    </FlyoutItem>
    <FlyoutItem Title="设置" Icon="settings.png">
        <ShellContent ContentTemplate="{DataTemplate views:SettingsPage}" Route="Settings"/>
    </FlyoutItem>
</Shell>
```

---

## 10. 分阶段实施计划

### 总里程碑
| 阶段 | 内容 | 预计工作量 | 交付物 |
|------|------|-----------|--------|
| **P0 骨架期** | 项目创建、核心层迁移、DI 打通 | 3~5 天 | 可编译 + 主壳导航 |
| **P1 学习核心** | 学习卡片 + StudyEngine + 学习列表 | 5~7 天 | 可正常学习/判分 |
| **P2 游戏中心** | BlazorWebView 游戏宿主 + 5 个游戏 | 4~6 天 | 5 游戏全跑通 + 结果回写 |
| **P3 复习增强** | 间隔重复 + 主动回忆 + 闪卡 | 3~4 天 | 复习流程闭环 |
| **P4 游戏化** | 成就/徽章/挑战/撒花/番茄钟 | 4~5 天 | 升级/成就通知可用 |
| **P5 内容管理** | 编辑器 + 错题本 + 笔记 + 导入导出 | 4~5 天 | 词库可管理可导入 |
| **P6 Android 打磨** | 权限 + 通知 + 后台番茄钟 + 性能 | 3~4 天 | APK 可发布 |
| **合计** | | **26~36 人天** | |

### P0: 骨架期 (3~5 天)

1. ✅ 创建 `LearningAssistant.Maui.sln` + 项目结构（见第3节）
2. ✅ 迁移 `LearningAssistant.Core` (Models + Common + Data)
3. ✅ 迁移 `LearningAssistant.Services` 核心服务
4. ✅ 注册 `MauiProgram.cs` DI 容器
5. ✅ `AppShell.xaml` 导航骨架 + 6 个空页面占位
6. ✅ EF Core SQLite 首次连接成功 + 空数据库创建

### P1: 学习核心 (5~7 天)

1. ✅ `LearningViewModel` + 命令绑定
2. ✅ `LearningCardView` 自定义控件
3. ✅ `CircularProgress` (日目标环) + `StatCard`
4. ✅ 科目/分类选择逻辑
5. ✅ 已知/未知状态回写
6. ✅ 学习统计面板 + 鼓励文案
7. ✅ Android TTS 接入

### P2: 游戏中心 (4~6 天)

1. ✅ `BlazorWebView` 游戏宿主通用框架
2. ✅ `GameViewModelBase` + `shared.js` 通信适配
3. ✅ 5 个游戏逐个接入（各 ~0.5 天）
4. ✅ `WordMatchGameService` 注入 + 取词测试
5. ✅ 游戏结果回写学习状态验证

### P3: 复习增强 (3~4 天)

1. ✅ `ReviewPage` (间隔重复日程)
2. ✅ `ActiveRecallPage` (主动回忆训练)
3. ✅ `FlashcardReviewPage` (闪卡翻转动画)
4. ✅ `ProgressiveHintPage` (渐进提示)

### P4: 游戏化 (4~5 天)

1. ✅ `GamificationViewModel` + XP/等级联动
2. ✅ `AchievementPage` + 成就解锁通知 (Toast/Popup)
3. ✅ `ChallengePage` + 挑战卡片
4. ✅ SkiaSharp Confetti 撒花动画
5. ✅ `PomodoroTimerView` + 前台服务 + 通知

### P5: 内容管理 (4~5 天)

1. ✅ `ContentEditorPage` (增删改词条)
2. ✅ `WrongAnswerPage` (错题本 + 统计)
3. ✅ `NotesPage` + `NoteEditorPage`
4. ✅ Excel/JSON 导入导出 (EPPlus 保留)
5. ✅ FilePicker 文件选择

### P6: Android 打磨 (3~4 天)

1. ✅ 运行时权限弹窗 (通知/录音/存储)
2. ✅ 番茄钟前台服务保活
3. ✅ 通知渠道 (Android 8.0+ NotificationChannel)
4. ✅ 小屏适配 (360dp / 600dp 断点)
5. ✅ 键盘弹出布局调整
6. ✅ 性能测试 (低端机流畅度)
7. ✅ APK 签名 + 发布配置

---

## 11. 风险与应对措施

| 风险 | 概率 | 影响 | 应对措施 |
|------|------|------|----------|
| **BlazorWebView 游戏性能不如 WebView2** | 中 | 中 | Chrome 远程调优 CSS 动画；极端方案降级为 MAUI `WebView` + `evaluateJavascript` |
| **Android TTS 音质不如 KokoroSharp** | 高 | 低 | 默认 Android TTS，可在设置中切换「云端 Qwen TTS」（HTTP 方案） |
| **EF Core SQLite 在 Android 上锁表现** | 低 | 中 | 确保 `AddDbContextFactory` 而非 Scoped；所有写操作统一通过 Service 层 |
| **番茄钟后台被杀** | 高 | 中 | 前台服务 + `WakeLock` + `AlarmManager` 精确闹钟；国内 ROM 引导用户加白名单 |
| **ScottPlot.WinForms 替换工作量** | 中 | 中 | 先上 `Microcharts.Maui` (轻量)，统计页后续再补 ScottPlot.Maui |
| **HTML/JS 游戏在小屏触控不佳** | 中 | 中 | 6.3 节响应式 CSS 适配；卡片最小触摸尺寸 48x48dp |
| **EPPlus 读取 Excel 兼容性** | 低 | 低 | EPPlus 8.6 支持 netstandard2.0，MAUI 兼容；可选升级到最新稳定版 |
| **数据模型 System.Drawing 引用残留** | 低 | 高 | 全局 Grep `System.Drawing` / `Color.FromArgb` / `PointF`，全部替换为 MAUI Graphics/SkiaSharp 类型 |

---

## 附录：NuGet 包清单（MAUI 新项目）

```xml
<!-- LearningAssistant.MauiApp.csproj 核心包 -->
<ItemGroup>
    <!-- MAUI + Blazor -->
    <PackageReference Include="Microsoft.Maui.Controls" Version="8.0.40" />
    <PackageReference Include="Microsoft.Maui.Controls.Compatibility" Version="8.0.40" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="8.0.40" />
    
    <!-- MAUI 社区工具包 -->
    <PackageReference Include="CommunityToolkit.Maui" Version="9.0.0" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" /> <!-- ObservableObject + RelayCommand -->
    
    <!-- EF Core + SQLite -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.8" />
    
    <!-- 图表 (二选一或全装) -->
    <PackageReference Include="ScottPlot.Maui" Version="5.0.0-beta" />
    <PackageReference Include="Microcharts.Maui" Version="1.0.0" />
    
    <!-- SkiaSharp -->
    <PackageReference Include="SkiaSharp.Views.Maui.Controls" Version="3.119.4" />
    
    <!-- Excel (导入导出) -->
    <PackageReference Include="EPPlus" Version="8.6.0" />
    
    <!-- 通知 (番茄钟) -->
    <PackageReference Include="Plugin.LocalNotification" Version="11.0.0" />
    
    <!-- 音频播放 -->
    <PackageReference Include="Plugin.AudioManager" Version="1.7.0" />
    
    <!-- DI + 配置 + 日志 (同原项目) -->
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
    
    <!-- JSON + HTTP -->
    <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
    <PackageReference Include="System.Text.Json" Version="8.0.5" />
</ItemGroup>
```

---

> **文档结束**  
> 本方案覆盖学习模块 + 游戏模块全部 16 项核心功能的 MAUI Android 迁移细节，不含 PDF 阅读与百度网盘分析模块。
