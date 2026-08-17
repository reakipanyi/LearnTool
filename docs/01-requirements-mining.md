# LearningAssistant 需求挖掘文档

> **文档说明**：基于 `LearningAssistant/` 项目**最新代码**梳理的需求挖掘结果（已对照 2026-08 代码现状补全细化）。
> **代码路径基准**：仓库根 `e:\Github\LearnTool\` 下的 `LearningAssistant/` 与 `LearningAssistant.Tests/`。
> **前提假设**：由于需求澄清环节被跳过，以下分析基于合理假设——
> - 目标用户：通用终身学习者（语言学习 + 学科知识记忆为主）
> - 产品阶段：现有产品优化迭代
> - 核心诉求：提升学习效果 + 架构重构 + 数据统计体系完善
> - 合规要求：个人/开源项目级别

---

## 一、显性需求（已从代码中识别）

### 1.1 基础学习需求（原有，已核对）

| 编号 | 需求分类 | 需求描述 | 来源依据 |
|------|---------|---------|---------|
| R-001 | 核心学习 | 支持多学科（8科：中文/英语/数学/物理/化学/历史/地理/生物）卡片式学习，40 个子类别 | `Common/Enums.cs`（`SubjectType`、`SubCategoryType`） |
| R-002 | 核心学习 | 学习模式：学习模式、快速模式、快速复习模式 | `Common/Enums.cs`（`LearningModeType`） |
| R-003 | 记忆算法 | 间隔重复复习（SM2 + FSRS 双算法，FSRS 含 Stability/Difficulty/Retrievability） | `Services/Learning/SM2Algorithm.cs`、`FSRSAlgorithm.cs`、`SqliteSpacedRepetitionService.cs` |
| R-004 | AI 增强 | AI 问答、AI 导师、费曼学习法、渐进提示、AI 侧边栏/历史 | `Services/AI/*`、`Forms/UserControls/MentorAIPanel.cs`、`FeynmanLearningPanel.cs`、`AIAgentSidebarPanel.cs` |
| R-005 | PDF 学习 | PDF 阅读、高亮、书签、OCR、翻译、TTS、夜间模式、PDF→学习打通 | `Forms/PdfReaderFormV2.cs`、`Services/Pdf/*`、`PdfStudyIntegration.cs` |
| R-006 | 游戏化 | 等级/XP/金币、成就、徽章、每日挑战、彩纸庆祝、即时反馈 | `Services/Gamification/GamificationService.cs`、`AchievementService.cs`、`BadgeManager.cs`、`ChallengeManager.cs` |
| R-007 | 番茄钟 | 番茄工作法计时、休息提醒、统计、托盘集成 | `Services/Learning/PomodoroService.cs`、`Forms/UserControls/PomodoroTimer.cs`、`PomodoroTrayIntegration.cs` |
| R-008 | 错题本 | 错题收集、复习、难度分级、掌握度追踪、Markdown 导出 | `Services/Learning/WrongAnswerService.cs`、`Forms/WrongAnswerForm.cs` |
| R-009 | 笔记系统 | 笔记管理、关联学习项、标签分类、复习提醒 | `Services/Learning/NoteService.cs`、`Forms/Notes/NotesForm.cs` |
| R-010 | 收藏夹 | 多级文件夹收藏、收藏项管理、置顶 | `Services/Favorites/FavoritesService.cs`、`BookmarkManagerForm.cs` |
| R-011 | 学习目标 | 每日目标设置、目标追踪、日历视图、进度统计 | `Services/Learning/LearningGoalService.cs`、`GoalCalendarView.cs` |
| R-012 | 学习提醒 | 定时提醒、重复提醒、多种提醒类型、统计触发行为 | `Services/Learning/SqliteLearningReminderService.cs`、`ReminderNotificationForm.cs` |
| R-013 | 学习路径 | 自定义学习路径、进度追踪、预估时长 | `Services/Learning/LearningPathService.cs` |
| R-014 | 知识图谱 | 知识点关联可视化（HTML 前端渲染） | `Services/KnowledgeGraph/KnowledgeGraphService.cs`、`KnowledgeGraphView.cs` |
| R-015 | TTS 语音 | 多引擎 TTS（KokoroSharp 本地 / Qwen 云端）+ 语音协调器 | `Services/TTS/KokoroSharpTtsService.cs`、`QwenTtsService.cs`、`SpeechCoordinator.cs` |
| R-016 | 数据分析 | 学习统计、图表、学习报告（当前为初始版本，见问题清单） | `LearningAnalyticsService.cs`、`LearningReportService.cs`、`LearningChartService.cs`、`LearningManagementForm.cs` |
| R-017 | 多用户 | 多用户配置切换、用户对比（首页下拉切换不灵活，见问题清单） | `Models/User/UserProfile.cs`、`UserSessionService.cs` |
| R-018 | 数据导入导出 | Excel 导入导出、学习项/复习日志/错题导出 | `Services/Learning/DataImportService.cs`、`ExportService.cs`、`LearningDataExportService.cs` |
| R-019 | 云存储 | 百度网盘备份同步 | `Services/Cloud/BaiduNetdiskService.cs` |
| R-020 | 主题系统 | 亮色/暗色主题、自定义主题、IThemeable | `Common/ThemeManager.cs`、`Common/Themes/IThemeable.cs` |

### 1.2 新增需求（相对早期文档，代码中已存在）

| 编号 | 需求分类 | 需求描述 | 来源依据 |
|------|---------|---------|---------|
| R-021 | 学习形式 | 主动回忆、联想学习、闪卡复习（特殊学习形式窗体） | `Forms/ActiveRecallForm.cs`、`AssociationLearningForm.cs`、`FlashcardReviewForm.cs` |
| R-022 | AI 增强 | 渐进提示学习（层层提示引导，独立窗体） | `Forms/ProgressiveHintForm.cs`、`ProgressiveHintStateService.cs` |
| R-023 | 趣味学习 | 趣味游戏化学习：单词消消乐/记忆翻牌/连连看/打地鼠/拼写（WebView2 前端） | `Forms/SpellingGameForm.cs`、`MemoryMatchGameForm.cs`、`LinkMatchGameForm.cs`、`WhackAMoleGameForm.cs`、`WordMatchGameForm.cs`、`WordMatchGameService.cs` |
| R-024 | 学习入口 | 统一学习入口：错题/收藏/学习路径/内容统一接入学习引擎 | `Services/Learning/UnifiedStudyEntryService.cs`、`IUnifiedStudyEntryService.cs` |
| R-025 | 智能推荐 | 个性化学习推荐（结合间隔重复/错题/统计/路径/番茄） | `Services/Learning/LearningRecommendationService.cs` |
| R-026 | PDF 集成 | PDF 与学习系统打通：高亮同步、内容提取入学习队列 | `Services/Learning/HighlightSyncService.cs`、`Services/Pdf/PdfStudyIntegration.cs` |
| R-027 | 云智能 | 百度网盘 AI 分析（文件快照、AI 分析编排） | `Services/PanAnalysis/*`、`Forms/BaiduPanAnalysisForm.cs` |
| R-028 | 数据备份 | 本地备份服务（收藏/统计/目标多 Provider） | `Services/Backup/*`（`BackupService`、`FavoritesBackupProvider`、`StudyStatsBackupProvider`、`LearningGoalsBackupProvider`） |
| R-029 | 内置浏览器 | WebView2 内置浏览器、网页书签管理、网页摘录 | `Forms/WebView2BrowserForm.cs`、`Services/Web/WebBookmarkService.cs`、`WebClippingSaveForm.cs` |
| R-030 | 测验/回忆 | 测验引擎、语音回忆（代码存在，当前**未注册**） | `Services/Quiz/QuizEngineService.cs`、`VoiceRecallService.cs`（`AddLearningEnhancementServices` 中已注释移除） |
| R-031 | 系统集成 | 系统托盘驻留、全局热键、拖拽导入、崩溃恢复、自动保存 | `Services/SystemTray/TrayIconService.cs`、`HotkeyService.cs`、`DragDropService.cs`、`CrashRecoveryService.cs` |

---

## 二、隐性需求（从代码推断）

| 编号 | 需求分类 | 需求描述 | 推断依据 |
|------|---------|---------|---------|
| I-001 | 性能 | 大量学习项加载性能优化 | `Services/Cache/CacheService.cs`、`CacheManagerService.cs`、`LearningStateCacheService.cs` |
| I-002 | 可靠性 | 崩溃恢复、自动保存 | `Services/Recovery/CrashRecoveryService.cs` |
| I-003 | 数据安全 | 配置加密（API Key 等敏感信息） | `Services/Persistence/ConfigEncryptionHelper.cs`、`SecureConfigManager.cs` |
| I-004 | 可扩展性 | 插件化 AI 服务（多 AI 厂商：Deepseek / 豆包 / OpenAI 兼容 / 回退） | `Services/AI/AIServiceFactory.cs`、`DeepseekAIService.cs`、`DoubaoAIService.cs`、`OpenAICompatibleAIService.cs`、`FallbackAIService` |
| I-005 | 用户体验 | 键盘快捷键、全局热键 | `Services/Hotkeys/HotkeyService.cs` |
| I-006 | 用户体验 | 系统托盘驻留、后台提醒、番茄托盘集成 | `Services/SystemTray/TrayIconService.cs`、`PomodoroTrayIntegration.cs` |
| I-007 | 数据迁移 | JSON 文件存储 → SQLite 数据库迁移（当前**已注释停用**，见问题清单） | `Services/Migration/DataMigrationService.cs`、`Program.cs`（L71-L102 注释块） |
| I-008 | 并发控制 | 乐观并发控制（AuditableEntityBase / 基础字段） | `Data/Database/EntityBase.cs` |
| I-009 | 可访问性 | 高 DPI 适配（PerMonitorV2） | `Program.cs` - `SetHighDpiMode` |
| I-010 | 拖拽交互 | 拖拽导入内容 | `Services/DragDrop/DragDropService.cs` |
| I-011 | 事件驱动 | 事件总线驱动跨模块联动（学习/游戏化/收藏/知识图谱事件订阅） | `Common/Events/EventBus.cs`、`LearningEvents.cs`、`FavoritesEventSubscriber.cs`、`LearningEventMediator.cs` |
| I-012 | 语音协调 | 多引擎语音协调（朗读优先级、打断控制） | `Services/TTS/SpeechCoordinator.cs`、`AdvancedSpeechService.cs` |
| I-013 | 统一入口 | 学习入口统一抽象（错题/收藏/路径/内容走同一学习流程） | `IUnifiedStudyEntryService.cs` |

---

## 三、待澄清问题（需求缺口）

在深入分析后，以下关键业务信息仍待补充确认：

1. **目标用户群体**：产品主要面向 K12 学生、语言学习者、通用终身学习者，还是职业考试备考者？
2. **产品阶段与目标**：当前是优化迭代、新模块开发，还是以偿还技术债务为主？（当前已有关卡：统计/图表/报告停留在初始版本）
3. **商业化与合规**：是否有商业化计划？是否需要满足教育数据安全、未成年人保护、GDPR 等合规要求？
4. **核心优先级**：当前最急需解决的是学习效果、用户体验、AI 能力，还是跨平台/架构重构？

---

## 四、已识别的问题清单（衔接《优化改进方案.md》）

> 本部分为需求挖掘中发现的**代码现状问题**，与仓库根 `优化改进方案.md` 保持一致，是后续优化改进的输入。

| 编号 | 归属模块 | 问题描述 | 影响 |
|------|---------|---------|------|
| W-001 | 主界面/用户管理 | 首页用户切换为下拉框（`comboBoxNewLayoutUser`），交互不灵活 | 无法展示头像/等级，切换体验差 |
| W-002 | 主界面/用户管理 | 顶部 ➕ 新增用户按钮与设置窗体用户管理重复 | 功能重复，维护混乱 |
| W-003 | 主界面/用户管理 | 旧布局 `comboBoxUser`/`groupBoxUser` 死代码遗留 | 两套下拉同步易出 BUG |
| W-004 | 数据分析 | 统计/图表/报告为初始版本，与当前项目未有效结合 | 无法支撑数据洞察 |
| W-005 | 数据分析 | `TimeSpentMinutes` 全代码库无赋值点，学习时长恒为 0 | 时长类统计/报告失真 |
| W-006 | 数据分析 | 统计数据多源（JSON + 多个 DB 实体），口径不一 | 同一指标不同界面数值不一致 |
| W-007 | 数据分析 | 统计界面多套并存（`LearningStatsForm`/`LearningManagementForm`/`ResultForm` 统计区） | 维护成本高、语义混乱 |
| W-008 | 数据分析 | 统计入口失效：侧边栏“统计”被注释、`buttonOpenStatistics` 无 Click 事件 | 用户无法进入统计 |
| W-009 | 数据分析 | 图表实现重复：`LearningChartService`（Bitmap）与 `LearningCharts.cs`（ScottPlot）并存 | 风格/主题不统一 |
| W-010 | 数据分析 | 首页 Dashboard StatCard 构造写死初始值（25分钟/7天/120/Lv.5 等） | 空数据展示假数值 |
| W-011 | 数据层 | 数据迁移服务（JSON→SQLite）当前被注释停用 | 旧 JSON 数据无法自动迁移 |
| W-012 | 数据层 | 仓储层（IUserProfileRepository 等）未注册（依赖未解耦） | 架构分层名存实亡 |
| W-013 | 学习增强 | 测验/语音回忆服务文件存在但未注册 | 功能不可用 |
