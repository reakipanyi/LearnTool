# LearningAssistant 需求挖掘文档

> **文档说明**：基于 `/workspace/LearningAssistant` 项目代码梳理的需求挖掘结果。
> **前提假设**：由于需求澄清环节被跳过，以下分析基于合理假设——
> - 目标用户：通用终身学习者（语言学习 + 学科知识记忆为主）
> - 产品阶段：现有产品优化迭代
> - 核心诉求：提升学习效果 + 架构重构
> - 合规要求：个人/开源项目级别

---

## 一、显性需求（已从代码中识别）

| 编号 | 需求分类 | 需求描述 | 来源依据 |
|------|---------|---------|---------|
| R-001 | 核心学习 | 支持多学科（8科）多子类别的卡片式学习 | `Common/Enums.cs` |
| R-002 | 核心学习 | 学习模式：学习模式、快速模式、快速复习模式 | `Common/Enums.cs` |
| R-003 | 记忆算法 | 间隔重复复习（SM2 + FSRS 双算法） | `Services/Learning/SM2Algorithm.cs`、`FSRSAlgorithm.cs` |
| R-004 | AI 增强 | AI 问答、AI 导师、费曼学习法、渐进提示 | `Forms/UserControls/MentorAIPanel.cs`、`FeynmanLearningPanel.cs` |
| R-005 | PDF 学习 | PDF 阅读、高亮、书签、OCR、翻译、TTS | `Forms/PdfReaderFormV2.cs` |
| R-006 | 游戏化 | 等级/XP/金币、成就、徽章、每日挑战、彩纸庆祝 | `Services/Gamification/GamificationService.cs` |
| R-007 | 番茄钟 | 番茄工作法计时、休息提醒、统计 | `Services/Learning/PomodoroService.cs` |
| R-008 | 错题本 | 错题收集、复习、难度分级、掌握度追踪 | `Services/Learning/WrongAnswerService.cs` |
| R-009 | 笔记系统 | 笔记管理、关联学习项、标签分类 | `Services/Learning/NoteService.cs` |
| R-010 | 收藏夹 | 多级文件夹收藏、收藏项管理 | `Services/Favorites/FavoritesService.cs` |
| R-011 | 学习目标 | 每日目标设置、目标追踪、日历视图 | `Services/Learning/LearningGoalService.cs` |
| R-012 | 学习提醒 | 定时提醒、重复提醒、多种提醒类型 | `Services/Learning/SqliteLearningReminderService.cs` |
| R-013 | 学习路径 | 自定义学习路径、进度追踪 | `Services/Learning/LearningPathService.cs` |
| R-014 | 知识图谱 | 知识点关联可视化 | `Services/KnowledgeGraph/KnowledgeGraphService.cs` |
| R-015 | TTS 语音 | 多引擎 TTS（KokoroSharp 本地 / Qwen 云端） | `Services/TTS/KokoroSharpTtsService.cs` |
| R-016 | 数据分析 | 学习统计、图表、学习报告 | `Services/Learning/LearningAnalyticsService.cs` |
| R-017 | 多用户 | 多用户配置切换、用户对比 | `Models/User/UserProfile.cs` |
| R-018 | 数据导入导出 | Excel 导入导出、数据备份 | `Services/Learning/DataImportService.cs` |
| R-019 | 云存储 | 百度网盘备份同步 | `Services/Cloud/BaiduNetdiskService.cs` |
| R-020 | 主题系统 | 亮色/暗色主题、自定义主题 | `Common/ThemeManager.cs` |

---

## 二、隐性需求（从代码推断）

| 编号 | 需求分类 | 需求描述 | 推断依据 |
|------|---------|---------|---------|
| I-001 | 性能 | 大量学习项加载性能优化 | `Services/Cache/CacheService.cs` 缓存服务存在 |
| I-002 | 可靠性 | 崩溃恢复、自动保存 | `Services/Recovery/CrashRecoveryService.cs` |
| I-003 | 数据安全 | 配置加密（API Key 等敏感信息） | `Services/Persistence/ConfigEncryptionHelper.cs` |
| I-004 | 可扩展性 | 插件化 AI 服务（支持多 AI 厂商切换） | `Services/AI/AIServiceFactory.cs` |
| I-005 | 用户体验 | 键盘快捷键、全局热键 | `Services/Hotkeys/HotkeyService.cs` |
| I-006 | 用户体验 | 系统托盘驻留、后台提醒 | `Services/SystemTray/TrayIconService.cs` |
| I-007 | 数据迁移 | JSON 文件存储 → SQLite 数据库迁移 | `Services/Migration/DataMigrationService.cs` |
| I-008 | 并发控制 | 乐观并发控制（RowVersion） | `Data/Database/Entities.cs` - `LearningItemEntity` |
| I-009 | 可访问性 | 高 DPI 适配 | `Program.cs` - `SetHighDpiMode` |
| I-010 | 拖拽交互 | 拖拽导入内容 | `Services/DragDrop/DragDropService.cs` |

---

## 三、待澄清问题（需求缺口）

在深入分析后，以下关键业务信息仍待补充确认：

1. **目标用户群体**：产品主要面向 K12 学生、语言学习者、通用终身学习者，还是职业考试备考者？
2. **产品阶段与目标**：当前是优化迭代、新模块开发，还是以偿还技术债务为主？
3. **商业化与合规**：是否有商业化计划？是否需要满足教育数据安全、未成年人保护、GDPR 等合规要求？
4. **核心优先级**：当前最急需解决的是学习效果、用户体验、AI 能力，还是跨平台/架构重构？
