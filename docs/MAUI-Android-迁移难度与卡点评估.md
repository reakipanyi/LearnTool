# LearningAssistant → MAUI Android 版本迁移：难度与卡点评估

> 本文档针对现有 **LearningAssistant** WinForms 桌面应用迁移到 **.NET MAUI Android** 目标框架进行系统性评估，量化迁移难度、识别核心卡点，并给出分阶段策略。

---

## 一、总体难度评估

| 维度 | 难度 | 说明 |
| --- | --- | --- |
| **总体** | **🔴 P0 极高** | 单文件 UI（InitializeComponent 手写）、大量 GDI+/PInvoke、10+ 原生/Windows 专属三方库、多模态 ShowDialog 交互模型、静态类全局路径，所有问题叠加 |
| UI 重写 | **🔴 100%** | 60+ Form / UserControl 全部手写初始化（无设计器 `.Designer.cs`，均在 `InitializeComponent()` 中 `new`），无法映射到 MAUI XAML |
| 三方库替换 | **🔴 60%** | WebView2、PdfiumViewerCore、KokoroSharp、ScottPlot.WinForms、NAudio、System.Speech、Tesseract（Windows 原生）均需寻找 Android 替代或重写 |
| 服务层可复用 | **🟡 ~60%** | Presenter / Services / Models / EF Core / 事件总线 / 缓存 / SQLite EF Core 可直接复用，但需修 `AppPaths`、`BeginInvoke`、`MessageBox` 等平台耦合 |
| 工作量估算 | **🔴 大** | 若全量迁移，估算 **3~6 人月**；若仅保留「学习 + 错题 + 统计 + AI」核心闭环，**1.5~3 人月** |

### 1.1 迁移策略建议

| 策略 | 说明 | 工作量 | 推荐度 |
| --- | --- | --- | --- |
| **A. 全量迁移** | 所有模块都在 MAUI Android 重写 UI | 极大（6 人月+） | ⭐ |
| **B. 核心闭环迁移**（首选） | 迁移：学习画布 + 列表 + 间隔复习 + 错题 + 统计 + 编辑器 + AI；保留桌面端：PDF OCR、托盘、番茄钟快捷键、百度网盘 | 中等（1.5~3 人月） | ⭐⭐⭐⭐⭐ |
| **C. 方案 B + Hybrid WebView** | PDF、浏览器、知识图谱用 MAUI Blazor / HybridWebView 承载 Web 渲染 | 中等 | ⭐⭐⭐⭐ |
| **D. 仅移动版"学习伴侣"App** | 独立轻量化 MAUI App，仅做卡片学习 + 复习 + 错题，数据通过网络与桌面同步 | 较小（1~1.5 人月） | ⭐⭐⭐ |

> **推荐路径 B**：先把 MVP 中 Presenter / Services 抽成 RCL（.NET 8+ Razor Class Library 或普通 class library，目标 `net10.0`），WinForms 与 MAUI 双端都引用该库；UI 端 MAUI 重写；保留桌面端 PDF / OCR 等重量级 Windows 专属功能。

---

## 二、技术栈对比（桌面 vs MAUI Android）

| 类别 | 桌面（现状） | MAUI Android | 迁移评价 |
| --- | --- | --- | --- |
| 框架 | `net10.0-windows7.0` + `UseWindowsForms` | `net10.0-android` + `UseMaui` | 多目标 TFM，可通过 `#if ANDROID` / Condition 隔离 |
| UI | Form + UserControl + `InitializeComponent()` 手写 | XAML + Code-behind / MVVM CommunityToolkit | 100% 重写 |
| 布局 | `TableLayoutPanel` / `FlowLayoutPanel` / `SplitContainer` / `Dock=Fill` | `Grid` / `StackLayout` / `VerticalStackLayout` / `GridLength(*,Auto)` | 思路可迁移但语法不同 |
| 表格 | `DataGridView` + `DataTable` | 无内置表格；可用 `CollectionView` / `DataGrid`（CommunityToolkit / DevExpress）或自定义 `BindableLayout` | 无一一映射；编辑器表格是**最大单点难点** |
| 树形 | `TreeView` | 无内置；需 `Syncfusion.TreeView.Maui` 或自定义多级 `CollectionView` | 难；`AssociationLearningForm`、Pdf 书签树受影响 |
| Tab | `TabControl` + `TabPage` | `TabbedPage` / `Shell` / `TabView`（CommunityToolkit） | 可行 |
| 控件 | ComboBox / ListBox / ListView / ProgressBar / ToolTip | Picker / CollectionView / ProgressBar / Tooltip（Platform-Specific） | 均有等价，但属性不同 |
| 绘制 | `System.Drawing`（GDI+） `OnPaint` `e.Graphics.Draw*` | `Microsoft.Maui.Graphics`（SkiaSharp） `ICanvas` | 必须重写：自定义绘制控件（`CircularProgressControl`、`ProgressRingControl`、`ProgressBarEx`、`ChartControl`、`MiniLineChart`、`FeynmanLearningPanel` 部分、`ConfettiControl`、`LevelBadge`、`PomodoroTimer` 圆形等） |
| 字体 | `new Font("微软雅黑", ...)` / `new Font("Segoe UI Emoji", ...)` | `FontFamily` 通过 `RegisterFont` 注册；Emoji 依赖系统字体 | 中文字体需内嵌资源（Noto Sans SC） |
| 对话框 | `ShowDialog()` 阻塞、`MessageBox.Show`、`OpenFileDialog`、`SaveFileDialog`、`Microsoft.VisualBasic.Interaction.InputBox` | `Shell.Current.DisplayAlert` / `FilePicker.Default` / `FolderPicker` | 全部异步；代码中同步阻塞调用需改 async |
| 主题 | `IThemeable` + `ThemeService` + `ThemeHelper.Colors` | `AppThemeBinding` / `ResourceDictionary` / `IPlatformTheme` | 思路可保留，实现要重构 |
| 通知 / 托盘 | `NotifyIcon`、`TrayIconService`、Toast/气泡 | Android Notification（`NotificationCompat.Builder`） / MAUI `LocalNotification` Plugin；无托盘 | 功能需降级或映射 |
| 全局快捷键 | `user32.dll` `RegisterHotkey`（`HotkeyService`） | `Dispatcher.Start/Stop` + 物理键覆写 `OnKeyDown` / `OptionsMenu` | 不可实现（安卓无全局热键概念） |
| 音频播放 | `NAudio.WaveOutEvent` + `AudioFileReader` / `System.Speech.SpeechSynthesizer` | `Plugin.AudioManager` / `MediaElement`（MAUI） / Android `TextToSpeech` Framework | 需重写；`SoundService`、`BaseTtsService` 全部重写 |
| TTS | KokoroSharp（本地） + QwenTts（云端） | 云端 QwenTts 可用；本地 KokoroSharp 无 Android 支持 | 本地 TTS 在 Android 不可用；或接入 Android 内置 `TextToSpeech` 引擎 |
| PDF | PdfiumViewerCore（PdfiumViewer.Native.x86_64） + PdfiumPdfService | `Maui.PdfViewer`（Syncfusion / DevExpress 付费）或 Android `PdfRenderer` API | 无免费 PDFium Android Binding，成本高 |
| 浏览器 | `Microsoft.Web.WebView2` | MAUI 内置 `WebView`（基于 Android `WebView`） | 控件替换，`NavigateToString` / `CoreWebView2` 部分 API 需适配 |
| OCR | `Tesseract 5.2` + Windows 原生 tessdata | `TesseractOCR.Maui` 插件或 Android `ML Kit Text Recognition` | Tesseract .NET 封装多为 Windows；Android 端一般走 ML Kit |
| 图表 | `ScottPlot.WinForms 4.1.74` | `ScottPlot.Maui`（预览版）或 `Microcharts.Maui` / `Syncfusion.Xamarin.SfChart` | 可替换；但 `LearningCharts` + `WeakPointsChart` 需重绘 |
| Excel | `EPPlus 8.6.0` | `EPPlus` 支持 `netstandard2.0`，Android 可用 | ✅ 可复用，只需文件权限 |
| SkiaSharp | `SkiaSharp 3.119.4`（仅部分用） | `SkiaSharp.Views.Maui.Controls` 完整支持 | ✅ 可复用，可用来替换 GDI+ 绘制 |
| 绘图图像处理 | `new Bitmap(...)`、`Graphics.FromImage`、`Marshal.Copy` 像素翻转（`PdfReaderNightModeManager`） | `SKBitmap.Decode` / `SKCanvas` / `SKPixmap` | 需改用 SkiaSharp；夜间模式反色代码要重写 |
| 配置 | `appsettings.json` + `AppData/config/appsettings.json`（`IConfiguration`） | 继续用 `IConfiguration`，物理路径变 | 配置系统可复用，路径改 |
| 网络 | `HttpClient` 单例，Timeout 绑定到 AI Config | `HttpClient` 完全一样；需 `INTERNET` / `ACCESS_NETWORK_STATE` 权限 | ✅ 可复用；AI / 翻译 / 网盘 HTTP 层无改动 |
| 数据库 | EF Core 10 Sqlite + `Database.EnsureCreated` + `RepairSchema()`（`AppDbContext`） | EF Core 10 Sqlite 完全兼容 Android | ✅ 可复用，只需改 `AppPaths.DatabasePath` 为 `Context.FilesDir` |
| 存储 | `AppDomain.CurrentDomain.BaseDirectory` + `AppData/`（可写） | 应用私有目录：`Context.FilesDir`；外部：`Android/ApplicationData`；只读资源：`Raw` / `Assets` | 最大卡点之一；`AppPaths` 静态类 100% 基于桌面路径 |
| 权限 | 无需 | `INTERNET` / `READ_MEDIA_IMAGES` / `POST_NOTIFICATIONS` / `WAKE_LOCK`（番茄钟） / `RECORD_AUDIO`（若加语音回忆） | 需在 `AndroidManifest.xml` 声明 + 运行时请求 |

---

## 三、第三方库卡点详解（逐个评估）

对应 [LearningAssistant.csproj](file:///e:/Github/LearnTool/LearningAssistant/LearningAssistant.csproj) 中的 13 个 PackageReference：

| 包名 | 版本 | Android 可用性 | 卡点 / 替代方案 |
| --- | --- | --- | --- |
| **KokoroSharp** | 0.6.7 | ❌ **Windows Only**（原生 CPU dll，x86_64） | 🟥 无法在 ARM Android 上运行；保留云端 `QwenTtsService`，或接入 Android 系统 `TextToSpeech` Java API |
| **KokoroSharp.CPU** | 0.6.7 | ❌ Windows Only | 同上 |
| **Microsoft.Web.WebView2** | 1.0.2646.30 | ❌ Win 专属 | 🟨 MAUI 内置 `WebView` 控件替代；`WebView2BrowserForm` 改 ContentPage + WebView；`CoreWebView2` 专属 API（DevTools、CookieManager、ScriptDialog）需查 MAUI WebView Handler 扩展 |
| **EPPlus / EPPlus.Interfaces** | 8.6.0 / 8.4.0 | ✅ netstandard 2.0，Android OK | 🟩 直接复用；仅需 `FilePicker` 获取导出路径 |
| **Microsoft.Extensions.\*（DI、Configuration、Logging、HttpClient、Options）** | 10.0.8 | ✅ 官方 MAUI 内置 | 🟩 完全复用；MAUI `MauiApp.CreateBuilder` 有相同 `Services`/`Configuration` 扩展点 |
| **System.Speech** | 10.0.8 | ❌ Windows Only | 🟥 废弃；MAUI 端用 `PlatformTextToSpeech`（MAUI Essentials `ITextToSpeech`）替代发音播放；或保留云端 TTS 流式输出 + MediaElement |
| **System.Text.Json** | 10.0.8 | ✅ | 🟩 |
| **Tesseract**（Charles Weld 版） | 5.2.0 | ❌ 绑定 Windows 原生 dll | 🟥 卡点；方案：① 接入 Google ML Kit Text Recognition（Android 官方 OCR，不需 tessdata）；② 找 `Tesseract4Android` Java Binding 再包一层；③ MAUI 插件如 `TesseractOcr.Maui`（需验证） |
| **NAudio** | 2.3.0 | ❌ Windows / WASAPI / DirectSound Only | 🟥 替代：① MAUI `MediaElement` 播放 wav/mp3；② `Plugin.AudioManager`；③ 直接 Android `MediaPlayer` |
| **Newtonsoft.Json** | 13.0.4 | ✅ netstandard | 🟩 直接复用 |
| **SkiaSharp** | 3.119.4 | ✅ MAUI 官方包 `SkiaSharp.Views.Maui.Controls` | 🟩 可大幅复用；同时用它**替代 System.Drawing 自定义绘制**（14+ 自定义控件） |
| **PdfiumViewer.Native.x86_64.v8-xfa** | 2018.4.8.256 | ❌ x86_64 Windows 原生 | 🟥 卡点；`PdfiumPdfService`、`PdfRenderer`、`PdfOcrService` 等全链路依赖；替代：① Android `PdfRenderer`（API 21+）Java Binding；② Syncfusion `Maui.PdfViewer`（付费）；③ 降级为「打开 PDF 用外部 App」 |
| **PdfiumViewerCore** | 1.0.0 | ❌ Windows / x86_64 | 同上 |
| **ScottPlot.WinForms** | 4.1.74 | ❌ WinForms 专属 | 🟨 替代：① ScottPlot 5 有 `ScottPlot.Maui` 实验包；② `Microcharts.Maui`；③ `LiveChartsCore.SkiaSharpView.Maui`；④ 重写为 `SkiaSharp` 自绘（若图表量少） |
| **Microsoft.EntityFrameworkCore + Sqlite** | 10.0.8 | ✅ | 🟩 直接复用；只需把 `AppPaths.DatabasePath` 改成 `Path.Combine(FileSystem.AppDataDirectory, "learning_assistant.db")`；`RepairSchema()` 可继续用 |

### 3.1 结论：必须替换的 4 类「死卡点」

1. **🔴 本地 TTS（KokoroSharp + System.Speech + NAudio）**：三个都不可用 → 保留 QwenTts 云端 + Android 系统 TTS（`ITextToSpeech` Essentials）+ `MediaElement` 播放
2. **🔴 PDF（PdfiumViewerCore + Pdfium Native）**：x86_64 → 要么付费 Syncfusion，要么用 Android `PdfRenderer` Java Binding，要么降级/放弃 Android PDF
3. **🔴 自定义绘制（GDI+）**：~15 个自定义控件 `OnPaint` → `SkiaSharp.Views.Maui.Controls.SKCanvasView` 重写
4. **🟡 OCR（Tesseract .NET）**：→ 改 ML Kit（Android 官方）或 Java Binding Tesseract4Android

---

## 四、代码层面卡点清单（按严重度排序）

### 🔴 P0：架构级 / 不可直接运行

1. **`AppPaths` 静态类全写死桌面路径**
   - 位置：[AppPaths.cs](file:///e:/Github/LearnTool/LearningAssistant/Common/AppPaths.cs)
   - 所有路径都基于 `AppDomain.CurrentDomain.BaseDirectory`（桌面可写目录），Android `BaseDirectory` 是只读 APK 内部路径，写必抛 `UnauthorizedAccessException`。
   - 影响：**25+ 子目录属性**（`DataRoot/ConfigDir/DatabaseDir/UsersDir/CacheDir/ExportsDir/...`）+ 数据库、缓存、TTS、PDF 标注、书签、缩略图、日志。
   - 改造：把 `AppPaths` 改为**接口 `IAppPaths`**，WinForms 与 MAUI 各自实现；MAUI 用 `FileSystem.AppDataDirectory` + `FileSystem.CacheDirectory`；tessdata、SubjectTemplates.json、encouragement.json 等只读资源改为 `Assets/` 或 `Raw` 内嵌。

2. **60+ Form / UserControl 全部手写 `InitializeComponent()`**
   - 现状：项目**没有任何 `.Designer.cs`**，所有控件（`MainForm`、`LearningForm`、`PdfReaderFormV2`、`ContentEditorForm`、30+ UserControl）都是在 `InitializeComponent()` 中 `new Button/Label/Panel/TabControl/SplitContainer` + 手写坐标/尺寸。
   - 卡点：无法「一键转换」；必须手工改 XAML；50 个 InitializeComponent + 每个 InitializeComponent 平均 150~800 行。
   - 参考 `LearningForm.cs#L1005-L1113`：仅布局就有主表 3 列、中间表 5 行、子视图 5 个 + 字体 + 颜色 + 事件订阅，工作量巨大。

3. **同步 ShowDialog 阻塞交互模型**
   - 位置：[WindowManager.cs](file:///e:/Github/LearnTool/LearningAssistant/Managers/WindowManager.cs) 全部方法都是 `ShowDialog()`；`ThinkingStimulator` / `SettingPresenter` / `MainPresenter` / `ContentEditorPresenter` 均假设对话框阻塞后续代码。
   - MAUI 中 `Shell.Current.GoToAsync` / `Navigation.PushModalAsync` 全部异步。
   - 改造：`WindowManager`（MVP 协调层）改为 `Task OpenXxxAsync()`；下游 Presenter 流程重构为 async/await。

4. **`MessageBox.Show`、`OpenFileDialog`、`SaveFileDialog`、`Microsoft.VisualBasic.Interaction.InputBox`**
   - 出现次数：`MessageBox.Show` 约 30 次，`Open/SaveFileDialog` 5 次，`InputBox` 2 次。
   - `InputBox`（[SettingPresenter.cs#149](file:///e:/Github/LearnTool/LearningAssistant/Presenters/SettingPresenter.cs#L149)、[MainPresenter.cs#386](file:///e:/Github/LearnTool/LearningAssistant/Presenters/MainPresenter.cs#L386)）在 Android 根本不存在，必须改自定义 `Entry` + `DisplayPromptAsync`。

5. **`BeginInvoke` / `Invoke`（WinForms UI 线程模型）**
   - 出现次数：例如 [FlashcardReviewForm.cs#L540](file:///e:/Github/LearnTool/LearningAssistant/Forms/FlashcardReviewForm.cs#L540)
   - MAUI 用 `MainThread.BeginInvokeOnMainThread` / `Dispatcher.Dispatch`。

### 🟠 P1：功能级 / 需要重写 / 重选型

6. **P/Invoke：`user32.dll`、`gdi32.dll`**
   - `HotkeyService`：`RegisterHotkey(user32)`、`SetWindowPos` → 安卓无全局热键概念，功能删除。
   - `AchievementNotificationForm`：`gdi32.dll` CreateRoundRectRgn 画圆角（CS L254）→ 用 MAUI `Frame.CornerRadius` 即可。
   - `PomodoroTrayIntegration`：`FlashWindowEx(user32)` 闪烁窗口 → 改 `HapticFeedback` / 状态栏通知。

7. **大量 GDI+ `new Bitmap()` / `Graphics.DrawImage` / `Marshal.Copy` 像素级操作**
   - 位置：`PdfReaderNightModeManager`（241/253 反色）、`PdfPresenter.cs`（458 绘图裁剪）、`PdfReaderHighlightManager.cs`（460 DrawImage）、`PdfReaderNavigationManager.cs`（多处 new Bitmap）、`WebView2BrowserForm.cs`（1194 Image.FromStream）
   - 全部改为 `SkiaSharp SKBitmap + SKCanvas`；`Marshal.Copy` 对应 `SKPixmap.GetPixelSpan`。

8. **硬编码「微软雅黑 / Segoe UI Emoji」字体**
   - 约 30+ 处：`new Font("微软雅黑", ...)`（ChallengeForm、AchievementForm、BadgeManager、ThinkingStimulator、AIPanelPopupService、各种 LearningForm Label）
   - Android 系统**默认不包含**这几个字体，会回退为 Droid Sans Fallback，中文会变形。
   - 方案：在 MAUI 项目 `Resources/Fonts/` 放 `NotoSansSC-Regular.otf`（开源中文）+ `seguiemj.ttf`（Segoe UI Emoji，Apache 协议可分发），通过 `MauiProgram.ConfigureFonts` 注册，再在 ResourceDictionary 里映射为 `StaticResource` 全局 Key。

9. **14+ 自绘自定义控件**（`IThemeable` 或直接继承 `Control` 重写 `OnPaint`）
   - 清单：`CircularProgressControl`、`ProgressRingControl`、`ProgressBarEx`、`ChartControl`、`MiniLineChart`、`LearningCharts`（部分）、`WeakPointsChart`、`ConfettiControl`、`FloatingText`、`LevelBadge`、`StatCard`（部分）、`RecommendationCard`（部分）、`PomodoroTimer`（圆形进度）、`AchievementCard`（圆角）
   - 每一个都要改 `SKCanvasView`（SkiaSharp.Maui）重写 `OnPaintSurface`。若用 Syncfusion / DevExpress MAUI 则部分可直接替换。

10. **DataGridView 编辑器网格**（`ContentEditorForm`）
    - 现状：[ContentEditorForm.cs#L639](file:///e:/Github/LearnTool/LearningAssistant/Forms/ContentEditorForm.cs#L639) `new DataGridView()` + `DataTable` 绑定；`ContentEditorPresenter` 把 `LearningItem` 转 `DataTable`（[ContentEditorPresenter.cs#L148](file:///e:/Github/LearnTool/LearningAssistant/Presenters/ContentEditorPresenter.cs#L148)），支持中文列名动态生成、行内编辑、批量删除、JSON 同步。
    - MAUI 无内置 DataGrid。可选：① `CommunityToolkit.Maui.DataGrid`（功能较弱）；② DevExpress.Maui.DataGrid / Telerik UI（付费）；③ 自定义 `CollectionView` + `Entry` 双向绑定（按 SubjectTemplates.json 动态生成列）。
    - 这是**工作台编辑器迁移的最大单点难点**。

11. **TreeView（联想学习 + PDF 书签树）**
    - `AssociationLearningForm.cs#L142`：多级联想树；`PdfReaderFormV2.cs#L56`：文件树。
    - MAUI 无内置 TreeView。社区常见方案：① `Syncfusion.Maui.TreeView`（付费）；② 嵌套 `CollectionView` + 展开折叠；③ 改列表展示（扁平化）。

12. **`System.Speech` 合成语音 + NAudio 播放链路**
    - `BaseTtsService.cs#L50-L166` 全部用 `WaveOutEvent` + `AudioFileReader` + `WaveFileReader` + NAudio 变速 `VarispeedWaveStream`。
    - 替代：① 云端 QwenTts（`QwenTtsClient` 只有 HTTP 下载，不依赖 NAudio）→ MAUI `MediaElement` 播放；② 本地走 `TextToSpeech.Default.SpeakAsync`（MAUI Essentials，限系统发音）；③ 变速走 `MediaElement.Speed`。

13. **托盘 / 全局快捷键**
    - `ITrayIconService`、`TrayIconService`、`PomodoroTrayIntegration` → Android 无托盘，改为 Foreground Service + Notification；`HotkeyService`（[HotkeyService.cs#L462](file:///e:/Github/LearnTool/LearningAssistant/Services/Hotkeys/HotkeyService.cs#L462)）直接删除。

### 🟡 P2：模块级 / 选型 / 改调用方式

14. **`ThemeService` / `IThemeable` / `ThemeHelper.Colors` 主题系统**
    - 思路好但实现绑定 WinForms：`IThemeable.ApplyTheme(ThemeColors)` 接受 `Color`，遍历每个控件改 `BackColor/ForeColor`；`ThemeHelper.GetHoverColor` 基于 GDI Color 调亮度。
    - 改 MAUI：① 颜色调色板保留（`ThemeColors` 模型可复用）；② 改为 `AppThemeBinding + ResourceDictionary{Styles.xaml}` 切换；③ `GetHoverColor` 调 SkiaSharp HSL/HLSL 等效实现。

15. **SplitContainer（可拖拽分割条）**
    - MainForm、PDF Reader、Notes Form 多处 `SplitContainer` → MAUI 用 `Grid` 行高/列宽 + 手势 `PanGestureRecognizer` 改 `GridLength`，或 `Syncfusion.SfPopupLayout` / 社区 `DraggableSplitter` 控件。

16. **ToastNotification（无 Win32 气泡 API）**
    - `ToastNotification` / `ReminderNotificationForm` / `AchievementNotificationForm`：改用 MAUI `CommunityToolkit.Maui.Toast` / `Snackbar`；成就用 `INotificationManager` 插件或本地通知。

17. **`EncouragementService` 用 NAudio 播放鼓励音**
    - `EncouragementService.cs#L128`：`AudioFileReader + WaveOutEvent` 播放本地 wav → 改 `MediaElement` 或 `Plugin.AudioManager` 播放 Android 资源音频。

18. **`SoundService`（`ISoundService`）**
    - 同上。

19. **知识点图谱 `kg-visualization.html`**
    - 目前在 WinForms 上跑但功能注释为已移除；Android 可直接用 `BlobUrl` / `Raw` 资源 + MAUI WebView 承载，反而更顺畅（WebView 是官方控件）。

20. **权限问题汇总（AndroidManifest.xml 必须声明）**
    - `INTERNET`：AI、翻译、网盘、TTS 云端
    - `ACCESS_NETWORK_STATE`：检测网络
    - `POST_NOTIFICATIONS`（API 33+）：成就提醒、番茄钟
    - `WAKE_LOCK` + `FOREGROUND_SERVICE`：番茄钟后台计时
    - `READ_MEDIA_IMAGES` / `READ_EXTERNAL_STORAGE`：导入导出 JSON、PDF 打开、剪藏图片
    - `SCHEDULE_EXACT_ALARM` 或 `USE_EXACT_ALARM`：`SqliteLearningReminderService` 定时提醒
    - `RECORD_AUDIO`（可选）：未来语音回忆功能

### 🟢 P3：可复用 / 少量改动

21. **服务层（`Services/` 下约 80 个文件）除 P0/P1 标记外，大部分可复用**
    - MVP 中的 Services：`StudyEngine`、`ProgressManager`、`StudyListProcessor`、`ContentLoaderService`、`FSRSAlgorithm`、`SM2Algorithm`、`LearningAnalyticsService`、`LearningReportService`、`LearningChartService`（只输出数据，不绑定 UI）、`LearningGoalService`、`WrongAnswerService`、`NoteService`、`FavoritesService`、`LearningPathService`、`UnifiedStudyEntryService`、`LearningRecommendationService`、`PomodoroService`、`GamificationService`、`AchievementService`、`BackupService`、`CrashRecoveryService`、`CacheService`、`UserSessionService`、`SqliteSpacedRepetitionService`、`SqliteLearningReminderService`、`EventBus`、`IEventBus` 全部是纯 C#，无 UI 依赖，可直接复用。

22. **Presenter 层（Presenters/ 共 9 个）可复用约 70%**
    - `MainPresenter`：事件绑定、用户切换、对比数据、推荐调用 → 可复用；UI 调用点改 async 即可
    - `LearningPresenter` / `LearningFlowHandler`：学习流程核心，高度依赖 `ILearningView` → 只要 MAUI 端实现 `ILearningView`，逻辑基本不动（这正是 MVP 的威力！）
    - `ContentEditorPresenter`：高度依赖 `IContentEditorView`、`DataTable`、`Open/SaveFileDialog` → 可复用模型/校验/去重/导入导出 JSON；`DataTable→DataGridView` 交互部分换 MAUI 网格实现
    - `PdfPresenter`：PDF 服务整合 + OCR + 翻译 + TTS → 若 PDF 方案选型落地，可复用 60%
    - `SettingPresenter`：UI 绑定（`InputBox` 要改），逻辑可复用
    - `ResultPresenter` + `AudioPresenter`：逻辑简单

23. **Models 层 100% 可复用**（`LearningItem`、`LearningPath`、`WrongAnswerItem`、`UserProfile`、`Achievement/Badge/Challenge`、`Pomodoro`、`PDF`、`Note`、`Quiz`、`AI/Mentor*`、各类 ValueObjects）。

24. **`EF Core AppDbContext` + `RepairSchema()` 100% 可复用**（仅改 `DatabasePath` 路径解析）。

25. **AI 全链路 100% 可复用**（`AIServiceFactory`、`FallbackAIService`、`AiQuestionService`、`AIPromptService`、`ConversationContextService`、`PromptTemplateService`、3 个 AI Provider 实现）—— 全部走 HTTP，无 UI 绑定。

26. **百度翻译 `BaiduTranslationService` 100% 可复用**。

27. **`SubjectTemplateService`、`CategoryConfig`、`SubjectSubCategoryMapping`、`Constants`、`Enums`、`JsonHelper`、`StringSimilarityHelper`、`StringLanguageDetector`、`MarkdownParser`（Markdig）**等工具类全部可复用。

---

## 五、模块级迁移难度评估表

| 模块 | P0 可复用度 | 关键卡点 | 难度 |
| --- | --- | --- | --- |
| 学习引擎（StudyEngine/FSRS/SM2/SpacedRepetition） | 100% | 无 | 🟩 低 |
| 用户/学习进度/学习记录（UserProfile + EF） | 100% | 无 | 🟩 低 |
| 游戏化（Gamification/Achievement/Badge/Challenge） | 100% | 无 | 🟩 低 |
| 推荐 / 分析 / 报告 / 图表数据 | 100% | 无 | 🟩 低 |
| 错题本 / 笔记 / 收藏 / 路径 / 目标 / 番茄钟（服务层） | 100% | 无 | 🟩 低 |
| 学习上下文（LearningContext + 枚举 + 学科映射） | 100% | 无 | 🟩 低 |
| AI 全链路（LLM + 对话上下文 + Prompt + 问答） | 100% | 无 | 🟩 低 |
| 翻译 + 网盘 HTTP 层 | 100% | 无 | 🟩 低 |
| 缓存 / 日志 / 配置 / DI / 事件总线 | 95% | 路径 | 🟩 低 |
| 数据库（SQLite + EF Core + RepairSchema） | 95% | 路径 | 🟩 低 |
| 学习画布 UI（卡片翻面 + TTS + 按钮） | 30% | 重写 XAML + 自定义控件 SkiaSharp 画进度环 | 🟧 中高 |
| 学习列表 UI（LearningListView） | 20% | 重写 CollectionView | 🟧 中 |
| 仪表盘首页（DashboardView + StatCard/FeatureCard） | 20% | 5 类卡片 SkiaSharp 重绘；布局改 XAML Grid | 🟧 中 |
| 学习统计图表 UI（ResultForm/LearningCharts） | 20% | ScottPlot.WinForms → ScottPlot.Maui / Microcharts；弱点点图自绘 | 🟧 中高 |
| 错题本 UI（WrongAnswerForm） | 30% | 列表 + 详情改 XAML | 🟧 中 |
| 每日挑战 UI（ChallengeForm） | 25% | TabControl→TabbedPage；进度条 SkiaSharp | 🟧 中 |
| 成就徽章 UI（AchievementForm） | 25% | 卡片布局 + 解锁动效 | 🟧 中 |
| 学习管理 UI（LearningManagementForm） | 20% | 提醒/报告/目标集成改 XAML | 🟧 中 |
| 设置 UI（SettingForm） | 30% | InputBox 改自定义 Entry；Hotkey 面板删除 | 🟧 中 |
| 学习法（费曼/主动回忆/联想/渐进提示）UI | 10% | ThinkingStimulator 关联 4 个 Form 全改；TreeView 是硬卡点 | 🔴 高 |
| 内容编辑器（ContentEditorForm + Presenter） | 5% | DataGridView + DataTable 动态列网格 → MAUI 无对应；中文列名模板 | 🔴 极高 |
| PDF 阅读器（PdfReaderFormV2 + 全套服务） | 5% | PdfiumViewer 不可用；Android 无免费 Pdfium Binding；高亮/标注/反色服务全绑 GDI+ | 🔴 极高 |
| 浏览器 + 剪藏（WebView2BrowserForm + SaveForm） | 60% | WebView2→MAUI WebView；CoreWebView2 专属 API（剪藏 DOM 注入）改 Handler | 🔴 高 |
| 系统托盘 + 全局快捷键 | 0% | 无对应；改通知 + Foreground Service | 🔴 高（且效果降级） |
| 自定义控件 14 个（圆形、图表、进度、徽章） | 0% | GDI+ → SkiaSharp.Maui 逐个重写 | 🔴 高 |
| TTS（本地 + 云端） | 60% | 云端可用；KokoroSharp/NAudio/System.Speech 全部 Windows Only | 🟧 中 |
| 音频播放（ISoundService + 鼓励音） | 0% | NAudio → MediaElement/Plugin.AudioManager | 🟧 中 |
| OCR（TesseractOcrService） | 0% | Tesseract .NET Windows Only → ML Kit / Java Binding | 🔴 高 |
| 知识库图谱 WebView | 80% | Windows WebView2 → MAUI WebView，差异较小 | 🟩 低 |
| 导入导出 JSON/Excel（DataImportService/ExportService + EPPlus） | 90% | Open/SaveFileDialog → FilePicker；EPPlus 本身兼容 | 🟨 中 |
| 主题系统（IThemeable + ThemeService） | 40% | 模型可复用；WinForms 遍历 Set BackColor/ForeColor → MAUI AppThemeBinding | 🟧 中 |

---

## 六、落地实施路径

### 阶段 0：架构准备（1~2 周）—— 决定成败的前置
1. **拆分 Shared Core 项目**：新建 `LearningAssistant.Core.csproj`（`net10.0`），把以下目录从 WinForms 项目**物理迁出**并引用：
   - `Common/`（除 `WinFormsExtensions.cs`、`BrowserHelper.cs`）
   - `Models/` 全量
   - `Data/` 全量
   - `Services/` 全量（除 `SystemTray/`、`Hotkeys/`、`Feedback/SoundService.cs` 中 NAudio 播放部分——改用接口分离）
   - `Managers/`（除 `*Tray*`、`WindowManager` 中 `ShowDialog` 行为）
   - `Presenters/` 全量
   - `Views/`（契约接口，不改）
   - `Resources/`（JSON 资源）
2. **移除静态 `AppPaths`**：
   - 改为 `interface IAppPaths`（`GetDatabasePath / GetCacheDir / GetConfigDir / EnsureInitialized` 等 20 个方法）
   - WinForms 端 `WindowsAppPaths` 维持现有 `AppDomain.CurrentDomain.BaseDirectory` 语义
   - MAUI 端 `MauiAndroidAppPaths` 使用 `FileSystem.AppDataDirectory` / `FileSystem.CacheDirectory`
   - `AppPaths.SetCurrentUserId` 改为 `IUserSessionService.CurrentUserId`（避免全局静态可变状态）
3. **把 NAudio / KokoroSharp 播放链路改为接口**：`IAudioPlayer` + `ITtsAudioPlayer`，桌面实现 NAudio，Android 实现 `MediaElement` 或 Java `AudioTrack`。
4. **配置系统**：`Program.cs` 中 `appsettings.json` 改为 MAUI `MauiApp.CreateBuilder.Configuration`，`SubjectTemplates.json`、`encouragement.json` 标记为 `MauiAsset` / `Embedded Resource`，在 Core 中提供 `IResourceLoader` 抽象读取。
5. **启动 MAUI 新项目**（`LearningAssistant.Maui.sln` + `LearningAssistant.Maui.csproj`，TFM `net10.0-android`），引用 Core 项目，跑通 DI + 数据库 + 一个空 HomePage。

> **卡点预警**：`PdfiumViewer.Native.x86_64.v8-xfa`、`ScottPlot.WinForms`、`Microsoft.Web.WebView2` 这 3 个 PackageReference **不能放到 Core 项目**，必须 `Condition="'$(TargetFramework)' == 'net10.0-windows7.0'"` 绑定 WinForms TFM。MAUI 端将用不同实现。

### 阶段 1：MVP 最小闭环（4~6 周）
- **目标**：在 Android 手机上实现「选学科 → 卡片学习 → 标记已掌握 / 待学 → 统计数据落库 → 查看统计」流程。
- 迁移：
  1. `IMainView` / `ILearningView` MAUI 端实现（HomePage + LearningPage XAML）
  2. 自绘控件：`CircularProgressControl`、`LevelBadge`、`ProgressBarEx` → `SkiaSharp.Maui`
  3. `MainPresenter`、`LearningPresenter`、`LearningFlowHandler` 接入
  4. 本地 TTS 改用 MAUI Essentials `TextToSpeech.SpeakAsync`，或保留云端 QwenTts + `MediaElement`
  5. 配置迁移 + EF Core 数据库打通（`AppPaths.DatabasePath`）
  6. `ScottPlot.WinForms` → `ScottPlot.Maui` 或 `Microcharts.Maui` 基础图表
- **不做**：PDF、编辑器、浏览器、OCR、番茄钟托盘、全局快捷键

### 阶段 2：编辑器 + 错题 + 游戏化（4~6 周）
- 内容编辑器网格：`CollectionView` + 动态列（按 `SubjectTemplates.json` 生成 `DataTemplate`），或引入 DevExpress / Telerik MAUI DataGrid 付费授权
- 错题本 UI + 错题复习循环（`WrongAnswerService` 已经就绪）
- 每日挑战 / 成就徽章 UI（`GamificationService` 已就绪，通知改 `LocalNotification` Plugin）
- 番茄钟前台服务（`Android.ForegroundServiceType.SpecialUse` + 通知）
- 学习目标面板

### 阶段 3：AI 助手 + 学习法（3~5 周）
- AI 助手常驻侧栏（MAUI Shell Flyout 或 `TabBar` + `ContentPage`），复用 `ConversationContextService`
- 费曼学习面板 + 主动回忆（`ThinkingStimulator` 去除 `ShowDialog` 改为 PushModalAsync）
- 联想学习 TreeView：付费 Syncfusion.TreeView.Maui 或改扁平化列表
- 渐进提示面板
- 学习路径面板

### 阶段 4：PDF + 浏览器 + 剪藏（4~8 周，可选 / 付费依赖）
- **PDF 选型决策**：
  - ① Syncfusion `Maui.PdfViewer`（$999/年起）→ 可快速完成高亮、书签、夜间模式、选中文本加入学习
  - ② Android `PdfRenderer` Java Binding（免费但工作量大，渲染 + 点击坐标映射手写）
  - ③ 降级为 ACTION_VIEW 调用外部 PDF App
- 浏览器 + 剪藏：MAUI `WebView` + Handler 扩展注入 JS（`EvaluateJavaScriptAsync`），实现 `WebClippingSaveForm`
- OCR：ML Kit `TextRecognition`（Google Play Services）替代 Tesseract

### 阶段 5：打磨 + 发布（2~3 周）
- 主题系统（AppThemeBinding 深/浅色）+ 字体内嵌 Noto Sans SC
- AOT / 裁剪 / `AndroidAppBundle` 发布包瘦身
- 权限声明 + 运行时请求
- 崩溃上报（AppCenter / Sentry.Maui）
- 签名、渠道包

---

## 七、不可实现 / 需降级的功能清单

| 功能 | 原因 | 推荐降级方案 |
| --- | --- | --- |
| 系统托盘（TrayIcon） | Android 无系统托盘概念 | Foreground Service + 状态栏通知 |
| 全局快捷键（HotkeyService） | Android 无 user32 / 全局热键 | 物理键覆写 `OnKeyDown` + 无障碍服务（体验差，建议移除） |
| 窗口闪烁（FlashWindowEx） | 无 Win32 | HapticFeedback + 通知 + MediaPlayer 提示音 |
| 窗口最大化 / 句柄操作 | 移动端不适用 | Shell 导航化，移除 |
| KokoroSharp 本地 TTS（x86_64 Windows dll） | Android ARM 无法运行 | 保留云端 QwenTts；或 Android 系统 TTS（音色降级） |
| PdfiumViewerCore（x86_64 Windows） | 原生 dll 不可用 | 付费 Syncfusion.PdfViewer.Maui；或 Android PdfRenderer Java Binding；或打开外部 App |
| Tesseract .NET（Windows Native） | tessdata + pinvoke | ML Kit Text Recognition（推荐）或 Tesseract4Android Java Binding |
| NAudio 音频播放（WASAPI / WaveOut） | 仅限 Windows | MediaElement / Plugin.AudioManager |
| System.Speech.SpeechSynthesizer | Windows Only | Essentials `TextToSpeech.SpeakAsync` |
| ScottPlot.WinForms 控件 | WinForms 专属 | ScottPlot.Maui / Microcharts / LiveCharts |
| Microsoft.VisualBasic.Interaction.InputBox | VB 运行时 / Windows | `Shell.Current.DisplayPromptAsync` + `Entry` 自定义弹窗 |
| Segoe UI Emoji 字体版权 | Windows 专属字体，安卓无 | 改用 `Noto Color Emoji`（Google 免费）或 `seguiemj.ttf` 需确认微软协议（个人使用一般 OK，分发需谨慎） |
| 「微软雅黑」字体版权 | 微软授权仅限 Windows 系统使用 | 改用「思源黑体 Noto Sans SC」（SIL Open Font License，可商用分发） |
| `MessageBox.Show` 有模式阻塞语义 | MAUI `DisplayAlert` 是异步 | 所有调用方加 async/await |
| `ShowDialog` 阻塞 | MAUI 导航全异步 | `WindowManager.OpenXxx` 全改 `Task OpenXxxAsync()`；下游代码重构；特别注意 `ThinkingStimulator` 中 3 个串联对话框 |

---

## 八、结论

### 8.1 总体判断

- **若目标是「1:1 全功能迁移到 Android 单机 App」**：不建议做。卡点过多（PDF、OCR、TTS 本地、Pdfium Native、DataGridView、TreeView、全局快捷键、托盘、14 个 GDI+ 控件），部分功能 Android 平台本身就没有等价物，工程风险高。
- **若目标是「做一个核心学习闭环的 MAUI Android App，与桌面 WinForms 共存、共享 Core 库」**（方案 B）：**可行、且性价比高**。服务层 / 算法层 / Presenter 层复用度达 60%~80%，只需重写 UI 与少量平台专属。
- **最大技术风险点排序**：
  1. 🟥 PDF 模块选型与工作量
  2. 🟥 内容编辑器动态列 DataGrid（无免费一等公民）
  3. 🟥 `AppPaths` 静态类改造（贯穿所有持久化逻辑，错则全盘坏）
  4. 🟧 14 个 GDI+ 自定义控件 → SkiaSharp
  5. 🟧 同步 ShowDialog / MessageBox → 全异步改造

### 8.2 推荐路线

1. **先做阶段 0**（拆分 Core 库 + `IAppPaths` + `IAudioPlayer` + 启动 MAUI 空壳）。此阶段无 UI 风险，是架构地基；**如果阶段 0 没做，所有迁移都是在沙上建塔**。
2. 再做阶段 1（MVP 闭环）验证「服务能跑、数据库能写、卡片能学」，此时投入 1~1.5 个月即可拿到可演示的 Android App。
3. 阶段 2 编辑器 / 错题 / 游戏化落地，形成可用产品。
4. PDF / 浏览器 / OCR 作为阶段 4 的**可选项**，根据阶段 2 用户反馈决定是否做（很多学习场景甚至不一定需要 Android 端 PDF）。

### 8.3 最终难度评级

```
可复用代码量（Service + Presenter + Model + EF Core + AI）:   ≈ 65%
必须新写 / 重写（UI XAML + SkiaSharp 自绘 + 平台音视频）:    ≈ 35%
必须替换的第三方库（13 个包中 7 个不可用）：                   ≈ 54%
综合难度（含风险与不确定性）：                                  🔴 高
建议团队配置：                                                  1 名资深 MAUI + 1 名后端 / WinForms
```

文档结束。
