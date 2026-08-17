# LearningAssistant 方案设计文档

> **文档说明**：输出 LearningAssistant 的系统分层架构、模块拆分、核心数据实体、功能清单和验收标准（已对照 2026-08 最新代码补全细化）。

---

## 一、系统分层架构

```mermaid
graph TB
    subgraph "表现层 (Presentation)"
        UI1[主窗体 MainForm]
        UI2[学习窗体 LearningForm / LearningHubForm]
        UI3[PDF阅读器 PdfReaderFormV2]
        UI4[设置窗体 SettingForm]
        UI5[内容编辑器 ContentEditorForm]
        UI6[学习数据中心 LearningManagementForm]
        UI7[错题本 WrongAnswerForm]
        UI8[挑战 ChallengeForm]
        UI9[结果/统计 ResultForm]
        UI10[趣味游戏 WebView2GameFormBase + 5款游戏]
        UI11[内置浏览器 WebView2BrowserForm]
        UI12[百度网盘AI分析 BaiduPanAnalysisForm]
        UC[用户控件库 UserControls: 卡片/图表/面板/Dashboard]
    end

    subgraph "主持者层 (Presenter)"
        P1[MainPresenter]
        P2[LearningPresenter]
        P3[LearningFlowHandler]
        P4[PdfPresenter]
        P5[SettingPresenter]
        P6[ResultPresenter]
        P7[ContentEditorPresenter]
        P8[LearningEventMediator]
    end

    subgraph "领域服务层 (Domain Service)"
        DS1[学习领域服务 LearningDomainService]
        DS2[游戏化领域服务 GamificationService]
        DS3[AI 领域服务 AiQuestionService]
        DS4[统一学习入口 UnifiedStudyEntryService]
        DS5[智能推荐 LearningRecommendationService]
    end

    subgraph "应用服务层 (Application Service)"
        S1[学习引擎 StudyEngine]
        S2[间隔重复 SqliteSpacedRepetitionService]
        S3[学习推荐 RecommendationService]
        S4[学习分析 LearningAnalyticsService + 统计聚合(规划)]
        S5[错题服务 WrongAnswerService]
        S6[笔记服务 NoteService]
        S7[收藏服务 FavoritesService]
        S8[番茄钟 PomodoroService]
        S9[目标服务 LearningGoalService]
        S10[提醒服务 SqliteLearningReminderService]
        S11[学习路径 LearningPathService]
        S12[知识图谱 KnowledgeGraphService]
        S13[PDF学习打通 PdfStudyIntegration + HighlightSyncService]
        S14[备份服务 BackupService]
        S15[测验/语音回忆 QuizEngine/VoiceRecall(待启用)]
        S16[百度网盘AI分析 PanAnalysis 编排器]
    end

    subgraph "基础设施层 (Infrastructure)"
        INF1[数据持久化 SqliteDataPersistenceService]
        INF2[缓存服务 CacheService/CacheManagerService]
        INF3[TTS 服务 + 语音协调 SpeechCoordinator]
        INF4[AI 服务 AIServiceFactory + 多厂商回退]
        INF5[PDF 服务 PdfiumPdfService]
        INF6[事件总线 EventBus/LearningEvents]
        INF7[云存储 CloudStorageService]
        INF8[崩溃恢复 CrashRecoveryService]
        INF9[主题系统 ThemeService/ThemeManager]
        INF10[热键/托盘/拖拽/系统集成]
    end

    subgraph "数据层 (Data)"
        DB[(SQLite 数据库 AppDbContext)]
        FS[(文件系统 JSON/配置/资源)]
    end

    UI1 --> P1
    UI2 --> P2
    UI3 --> P4
    P2 --> P3
    P3 --> DS1
    P3 --> S1
    DS1 --> S2
    DS1 --> S3
    DS1 --> S5
    DS1 --> S7
    S1 --> INF1
    INF1 --> DB
    INF2 --> FS
    P4 --> INF5
    P4 --> DS3
    P4 --> S13
```

### 1.1 依赖注入组织（`Common/ServiceCollectionExtensions.cs`）

`Program.BuildServiceProvider()` 按块注册：配置 / 日志 / 核心业务 / AI / PDF / 学习 / 学习增强 / 数据库 / 百度网盘分析 / 窗体。

> ⚠️ **现状提示**：
> - 仓储层（`IUserProfileRepository` 等）当前**未注册**（依赖 `AppDbContext` 而容器只注册了 `IDbContextFactory`），分层名存实亡。
> - 数据迁移服务（`DataMigrationService`/`LearningItemMigrationService`）与启动迁移块当前**被注释停用**。
> - 测验/语音回忆（`QuizEngineService`/`VoiceRecallService`）文件存在但未注册。

---

## 二、模块拆分

| 模块名称 | 模块职责 | 核心服务/组件 | 依赖模块 |
|---------|---------|-------------|---------|
| **核心学习模块** | 学习引擎、卡片展示、学习流程控制 | StudyEngine, LearningFlowHandler, LearningCard | 无（核心） |
| **记忆算法模块** | 间隔重复算法、复习调度 | SM2Algorithm, FSRSAlgorithm, SqliteSpacedRepetitionService | 核心学习 |
| **统一学习入口模块** | 错题/收藏/路径/内容统一接入学习引擎 | UnifiedStudyEntryService, StudyListProcessor | 核心学习 |
| **AI 增强模块** | AI 问答、导师、费曼、渐进提示、AI 侧边栏 | AiQuestionService, MentorAIPanel, FeynmanLearningPanel, AIAgentSidebarPanel | 核心学习 |
| **PDF 学习模块** | PDF 阅读、标注、OCR、翻译、TTS、学习打通 | PdfiumPdfService, PdfOcrService, PdfStudyIntegration, HighlightSyncService | 核心学习 |
| **游戏化模块** | 等级/XP、成就、徽章、挑战、庆祝、鼓励 | GamificationService, AchievementService, BadgeManager, ChallengeManager, EncouragementService | 核心学习 |
| **趣味学习模块** | 5 款 WebView2 小游戏巩固知识 | WordMatchGameService, 5 个游戏窗体 | 核心学习 |
| **错题本模块** | 错题收集、分类、复习、掌握度、导出 | WrongAnswerService, WrongAnswerForm | 核心学习 |
| **笔记模块** | 笔记管理、关联学习项、标签、复习 | NoteService, NotesForm | 核心学习 |
| **收藏模块** | 多级收藏夹、收藏项管理、置顶 | FavoritesService, BookmarkManagerForm | 核心学习 |
| **番茄钟模块** | 番茄计时、休息提醒、统计、托盘集成 | PomodoroService, PomodoroTimer, PomodoroTrayIntegration | 无（独立） |
| **学习目标模块** | 目标设置、追踪、日历、进度统计 | LearningGoalService, GoalCalendarView | 核心学习 |
| **提醒模块** | 学习提醒、定时提醒、触发统计 | SqliteLearningReminderService, ReminderNotificationForm | 学习目标 |
| **学习路径模块** | 自定义路径、进度追踪、预估时长 | LearningPathService, LearningPathItem | 核心学习 |
| **知识图谱模块** | 知识点关联、可视化 | KnowledgeGraphService, KnowledgeGraphView | 核心学习 |
| **数据分析模块** | 学习统计、图表、报告（初始版本，待重构） | LearningAnalyticsService, LearningChartService, LearningReportService, LearningManagementForm | 核心学习 |
| **TTS 语音模块** | 语音合成、发音协调 | KokoroSharpTtsService, QwenTtsService, SpeechCoordinator | 无（基础设施） |
| **用户管理模块** | 多用户切换、用户档案（切换交互待优化） | UserSessionService, UserProfile | 无（基础设施） |
| **内容管理模块** | 内容导入导出、编辑器、PDF 提炼 | DataImportService, ExportService, ContentEditorForm, PdfContentLinkService | 核心学习 |
| **内置浏览器模块** | WebView2 浏览器、网页书签、网页摘录 | WebView2BrowserForm, WebBookmarkService, WebClippingSaveForm | 无 |
| **云智能分析模块** | 百度网盘 AI 分析 | BaiduPanAnalysisOrchestrator, BaiduPanAnalysisForm | 无 |
| **数据备份模块** | 本地备份（收藏/统计/目标） | BackupService + 各 BackupProvider | 数据层 |
| **系统基础设施** | 缓存、配置、日志、主题、热键、托盘、拖拽、崩溃恢复 | CacheService, ThemeManager, HotkeyService, TrayIconService, CrashRecoveryService | 无（基础） |

---

## 三、核心数据实体

### 3.1 实体关系图

```mermaid
erDiagram
    USER_PROFILE ||--o{ CATEGORY_PROGRESS : has
    USER_PROFILE ||--o{ LEARNING_RECORD : has
    USER_PROFILE ||--o{ REMINDER : has
    USER_PROFILE ||--o{ BADGE_UNLOCK : has
    USER_PROFILE ||--o{ STUDY_STATS : has
    USER_PROFILE ||--o{ DAILY_CHALLENGE : has
    USER_PROFILE ||--o{ CHALLENGE_HISTORY : has
    USER_PROFILE ||--o{ LEARNING_GOAL : has
    USER_PROFILE ||--o{ DAILY_GOAL_RECORD : has
    USER_PROFILE ||--o{ FAVORITE_FOLDER : has
    USER_PROFILE ||--o{ FAVORITE_ITEM : has
    USER_PROFILE ||--o{ POMODORO_SETTINGS : has
    USER_PROFILE ||--o{ POMODORO_RECORD : has
    USER_PROFILE ||--o{ WRONG_ANSWER : has
    USER_PROFILE ||--o{ NOTE : has
    USER_PROFILE ||--o{ LEARNING_PATH : has
    USER_PROFILE ||--o{ SPACED_REPETITION_ITEM : has
    USER_PROFILE ||--o{ LEARNING_ITEM_STATE : has

    SPACED_REPETITION_ITEM ||--o{ REVIEW_LOG : has
    REMINDER ||--o{ REMINDER_REPEAT_DAY : contains
    LEARNING_PATH ||--o{ LEARNING_PATH_ITEM : contains
    FAVORITE_FOLDER ||--o{ FAVORITE_ITEM : contains

    LEARNING_ITEM_STATE }o--|| CATEGORY_PROGRESS : tracks

    USER_PROFILE {
        string UserId PK
        string UserName
        string AvatarPath
        DateTime LastLoginTime
        DateTime LastStudyDate
        int ConsecutiveStudyDays
        int TotalStudyTimeMinutes
        int TodayStudyTimeMinutes
        int TodayItemsStudied
        int XP / TotalXP
        int Level / Coins
        int TotalItemsStudied / StudyDays / LongestStreak
    }

    LEARNING_ITEM {
        string Id PK
        string Subject / SubCategory
        string MainContent
        string MeaningJson / ExampleJson
        string PronunciationJson
        string CharacterFeaturesJson / WordFeaturesJson
        string Status
        int ReviewCount
        DateTime LastReviewedAt
    }

    SPACED_REPETITION_ITEM {
        Guid Id PK
        string Content / Answer
        int Interval / Repetitions
        double EFactor / Stability / Difficulty / Retrievability
        DateTime NextReviewDate
        int WrongCount / CorrectCount / ReviewCount / CorrectStreak
        int LearningStage
        string AlgorithmType
    }

    REVIEW_LOG {
        int Id PK
        Guid ContentId FK
        int Rating / Interval / Duration
        double EaseFactor / Stability / Difficulty
        DateTime ReviewTime
        string AlgorithmType
    }

    WRONG_ANSWER {
        string Id PK
        string Subject / Category / Tags
        string Question / CorrectAnswer / UserAnswer / Explanation
        int WrongCount / CorrectCount / ReviewCount
        double Difficulty
        int MasteryLevel
        DateTime FirstWrongAt / LastWrongAt / NextReviewAt
    }

    NOTE {
        string Id PK
        string Title / Content / Category / Tags
        string RelatedType / RelatedItemId / RelatedItemTitle
        int Importance / ReviewCount
        bool IsFavorite
        string Color / Source
    }

    LEARNING_GOAL {
        int Id PK
        string GoalType
        int TargetValue
        string Unit
        bool Enabled
    }

    DAILY_GOAL_RECORD {
        int Id PK
        DateTime Date
        string ProgressJson / CompletedJson
        bool AllCompleted
    }

    POMODORO_RECORD {
        string Id PK
        DateTime StartTime / EndTime
        string Type / Task
        int DurationSeconds / PlannedDurationSeconds
        bool Completed
        int InterruptionCount
    }

    LEARNING_PATH {
        string Id PK
        string Name / Description / Goal
        string PathType / Domain / Level
        int TotalEstimatedMinutes
        bool IsActive
        DateTime StartDate / TargetDate
    }

    LEARNING_PATH_ITEM {
        string Id PK
        string PathId FK
        string Title / Description
        string ContentType / ContentIds / Prerequisites
        int EstimatedMinutes / DifficultyLevel / Order / Progress
        bool IsCompleted
    }

    REMINDER {
        Guid Id PK
        string Type / Title / Description / RepeatType
        TimeSpan Time
        bool Enabled
        int TriggerCount / OpenCount / SnoozeCount / DismissCount
    }
```

### 3.2 补充实体（数据分析/系统级）

| 实体名称 | 用途 | 说明 |
|---------|------|------|
| `LearningItemStateEntity` | 学习项状态（替代 CategoryProgress 的 JSON 存储） | 按用户+分类记录 IsKnown |
| `AppSessionEntity` | 应用会话状态持久化 | SessionKey + JSON |
| `MigrationCheckpointEntity` | 迁移断点续传 | StepId + Status + DetailJson |
| `ReminderRepeatDayEntity` | 提醒重复星期（替代 RepeatDaysJson） | ReminderId + DayOfWeek |
| `PomodoroSettingsEntity` | 番茄钟设置 | 学习/短休/长休时长等 |

> **数据权威源**：核心数据均入库（SQLite `AppDbContext`）。学习分析（`LearningAnalyticsService`）仍保留每用户 JSON（`UserAnalyticsData`）与 DB 并存，为优化点之一（见《优化改进方案.md》）。

---

## 四、功能清单（按优先级）

### 4.1 P0 - 核心功能（必须）

| 功能编号 | 功能名称 | 功能描述 | 验收要点 |
|---------|---------|---------|---------|
| F-001 | 多学科卡片学习 | 8 大学科、40 个子分类的卡片式学习 | 所有子类别卡片正确展示、认识/不认识标记 |
| F-002 | 间隔重复复习 | 基于 SM2/FSRS 算法的智能复习调度 | 复习间隔计算正确、到期自动提醒 |
| F-003 | TTS 语音朗读 | 本地/云端双引擎语音合成 + 协调 | 发音清晰、自动播放、字段级发音 |
| F-004 | 学习数据持久化 | SQLite 存储用户数据 | 数据不丢失、崩溃可恢复 |
| F-005 | 多用户切换 | 支持多用户独立学习数据 | 用户数据隔离、切换正常 |

### 4.2 P1 - 重要功能（应该有）

| 功能编号 | 功能名称 | 功能描述 | 验收要点 |
|---------|---------|---------|---------|
| F-006 | AI 问答助手 | 基于 AI 的知识点问答讲解 | AI 回答准确、上下文连贯 |
| F-007 | PDF 学习集成 | PDF 阅读、标注、内容提取、学习打通 | 正常打开PDF、高亮/书签可用、可入学习队列 |
| F-008 | 游戏化系统 | 等级/XP/成就/徽章/挑战/鼓励 | 奖励计算正确、成就触发正常 |
| F-009 | 错题本 | 错题收集、分类、复习、导出 | 错题自动记录、复习有效 |
| F-010 | 学习统计分析 | 学习数据可视化图表 | 数据准确、图表清晰（**待重构**） |
| F-011 | 番茄钟 | 番茄工作法计时、统计、托盘 | 计时准确、提醒正常 |
| F-012 | 学习目标 | 每日目标设置与追踪、日历 | 目标可设置、进度可追踪 |
| F-013 | 收藏夹 | 多级文件夹收藏管理 | 收藏/取消/分类正常 |
| F-014 | 笔记系统 | 学习笔记管理、关联学习项 | 笔记可增删改查、可关联学习项 |
| F-015 | 统一学习入口 | 错题/收藏/路径/内容统一学习 | 不同来源进入同一学习流程 |
| F-016 | 趣味游戏 | 5 款 WebView2 巩固游戏 | 可玩、错题回写、XP 结算 |

### 4.3 P2 - 增强功能（可以有）

| 功能编号 | 功能名称 | 功能描述 | 验收要点 |
|---------|---------|---------|---------|
| F-017 | 费曼学习法 | 用户输出理解 + AI 评估 | AI 评估合理、反馈有价值 |
| F-018 | AI 导师 | 对话式深度讲解 | 引导式提问、个性化讲解 |
| F-019 | 渐进提示 | 层层递进的提示引导 | 提示分级合理、不直接给答案 |
| F-020 | 知识图谱 | 知识点关联可视化 | 图谱生成合理、交互流畅 |
| F-021 | 学习路径 | 自定义学习路径规划 | 路径可创建、进度可追踪 |
| F-022 | 智能推荐 | 个性化学习推荐 | 推荐结合薄弱点、有针对性 |
| F-023 | 每日挑战 | 每日学习任务挑战 | 挑战生成合理、奖励正确 |
| F-024 | 学习提醒 | 定时学习提醒 | 提醒准时、可配置 |
| F-025 | 内容导入导出 | Excel 批量导入导出、复习日志/错题导出 | 导入不丢失、导出格式正确 |
| F-026 | 云存储备份 | 百度网盘同步 + 本地备份 | 备份正常、恢复可用 |
| F-027 | OCR 文字识别 | PDF/图片文字识别 | 识别准确、支持中英文 |
| F-028 | 翻译功能 | 中英文互译、PDF 取词 | 翻译准确 |
| F-029 | 内置浏览器 | 网页浏览、书签、摘录 | 浏览正常、摘录可入学习 |
| F-030 | 百度网盘 AI 分析 | 网盘文件 AI 分析 | 分析结果合理、可导入 |

### 4.4 优化改进项（衔接《优化改进方案.md》）

| 编号 | 模块 | 优化项 | 优先级 |
|------|------|--------|--------|
| O-001 | 数据分析 | 统一统计聚合服务 + 时长写入 + JSON 迁移 | P0 |
| O-002 | 数据分析 | 首页 Dashboard 真实数据 + 空状态 | P0 |
| O-003 | 数据分析 | 统一"学习数据中心" Tab 化 + 恢复统计入口 | P1 |
| O-004 | 数据分析 | 统一 ScottPlot 图表层，删除 Bitmap 旧图表 | P1 |
| O-005 | 报告 | 报告结构化 + 导出 + AI 总结 + 日报提醒 | P2 |
| O-006 | 用户管理 | 首页用户切换改版（浮层面板）、删除重复 ➕、清理死代码 | P2（可并行） |

---

## 五、验收标准

### 5.1 功能验收标准

| 验收维度 | 验收标准 | 度量方法 |
|---------|---------|---------|
| **学习功能** | 40+ 子类别学习卡片展示正常，认识/不认识标记正确 | 全类别遍历测试 |
| **算法正确性** | 间隔重复算法计算结果与标准算法一致 | 单元测试覆盖率 > 90% |
| **数据完整性** | 学习数据持久化不丢失，崩溃可恢复 | 异常退出测试 + 数据校验 |
| **AI 可用性** | AI 问答响应时间 < 5s（网络正常时），多厂商可回退 | 性能测试 + 人工评估 |
| **PDF 兼容性** | 支持常见 PDF 文件，渲染无错乱，学习打通可用 | 多种 PDF 样本测试 |
| **语音质量** | TTS 发音清晰可懂，无明显卡顿 | 人工试听 + 响应时间测试 |
| **统计准确性** | 时长/数量/正确率/连击等口径正确、可配置 | 聚合服务单元测试（见优化方案） |

### 5.2 性能验收标准

| 性能指标 | 验收标准 | 测试条件 |
|---------|---------|---------|
| 应用启动时间 | < 3 秒 | 冷启动，常规配置机器 |
| 学习项加载 | 1万条数据 < 1 秒 | 标准测试数据集 |
| 卡片切换 | < 200ms | 连续快速切换 |
| 内存占用 | 常规使用 < 500MB | 1万条数据量下 |
| TTS 响应 | 本地 TTS < 500ms，云端 < 2s | 正常网络环境 |
| PDF 打开 | 100页以内 < 2s | 标准 PDF 文档 |
| 统计中心打开 | 打开 + 聚合结果 < 1s（含缓存命中） | 常规数据量 |

### 5.3 兼容性验收标准

| 兼容性维度 | 验收标准 |
|---------|---------|
| **操作系统** | Windows 7 SP1 及以上全版本兼容（含 WebView2 运行时） |
| **.NET 版本** | 依赖 .NET 10 Desktop Runtime |
| **高 DPI** | 100%/125%/150%/200% 缩放无错乱 |
| **数据兼容** | 旧版 JSON 数据可平滑迁移到 SQLite（幂等、可断点续传） |
| **分辨率** | 最小支持 1366×768 分辨率 |

### 5.4 安全验收标准

| 安全维度 | 验收标准 |
|---------|---------|
| **配置安全** | API Key 等敏感配置加密存储 |
| **数据安全** | 用户学习数据本地存储，不上传（除非用户主动备份） |
| **异常安全** | 异常退出不损坏数据文件 |
| **输入安全** | 用户输入内容做长度/格式校验，防止注入 |
