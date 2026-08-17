# 统一执行计划：实体聚合重构 + 学习卡片交互优化

> 整合来源：
> - [ENTITY_AND_AGGREGATE_ANALYSIS.md](file:///e:/Github/LearnTool/LearningAssistant/ENTITY_AND_AGGREGATE_ANALYSIS.md) — 实体与聚合架构分析及优化建议
> - [LEARNING_CARD_CONTENT_INTERACTION_IMPROVEMENT.md](file:///e:/Github/LearnTool/LearningAssistant/LEARNING_CARD_CONTENT_INTERACTION_IMPROVEMENT.md) — 学习卡片字段级交互优化方案
> 
> 核心原则：每个 Phase 可独立编译、测试、部署；共享修改点在任意 Phase 中只能被一个任务修改。

---

## 一、共享修改点归属表

| 文件/组件 | LEARNING_CARD | ENTITY | INTERFACE_REFACTORING | 归属 Phase |
|-----------|---------------|--------|----------------------|------------|
| **LearningItem** | 结构不变（目标11） | 创建 `LearningItemEntity`（9.6） | 参数化改造 | Phase 7 |
| **ValueObject 基类** | 依赖 `Meaning`/`Example`/`Pronunciation` 结构 | 合并两个基类（9.1） | 无 | Phase 6 |
| **LearningFlowHandler** | 移除 `_pronunciationQueue`，引入 `ISpeechCoordinator`（6.6） | 引入领域服务层（10.4）+ 用户聚合拆分（9.2） | 参数化改造 | Phase 2 + Phase 5 |
| **ILearningView** | 新增三个字段发音事件（6.5） | 无 | `LearningContext` 参数化 | Phase 4a + Phase 4b |
| **LearningItemFormatter** | 新增 `BuildFields`（6.2） | 无 | 无 | Phase 1 |
| **LearningCard** | 容器化改造（6.4） | 无 | 无 | Phase 3 |
| **ContentField** | 新增 `record`（6.1） | 无 | 无 | Phase 1 |
| **ISpeechCoordinator** | 新增（6.6） | 依赖 | 无 | Phase 2 |
| **AuditableEntityBase** | 无 | 添加 `RowVersion`（10.3.1） | 无 | Phase 8（调整后） |
| **仓储模式** | 无 | 为每个聚合根创建仓储（10.2.1） | 无 | Phase 8（调整后） |
| **用户聚合拆分** | 无 | 拆分为用户、提醒、目标三个聚合（9.2） | 无 | Phase 5 |

---

## 二、执行 Phase 序列

### Phase 0：准备阶段

**来源**：新增

**任务清单**：
1. 建立基线测试：运行现有单元测试，确保全部通过，记录当前状态
2. 创建分支策略：为每个 Phase 创建独立分支模板
3. 验证 CI/CD 流程：确保当前 CI/CD 能正常构建和测试
4. 备份当前代码：创建 tag `before-unified-plan`
5. 初始化测试数据：准备覆盖各子类别的测试学习项

**涉及文件**：无（环境准备）

**前置依赖**：无

**完成标准**：
- 所有现有单元测试通过
- 分支策略文档创建完成
- CI/CD 构建成功
- Git tag 创建完成

**回归测试范围**：全量现有测试

---

### Phase 1：结构化字段输出（LEARNING_CARD P1）

**来源**：LEARNING_CARD 第六章 6.1-6.2

**任务清单**：
1. 新增 `record ContentField`（`Services/Learning/ContentField.cs`）
2. 改造 `LearningItemFormatter`：新增 `BuildFields` 方法，覆盖全部 `SubCategoryType`
3. 原 `FormatDisplayText`/`FormatDisplayStruct` 改为派生自 `BuildFields`（保持向后兼容）
4. 新增 `LearningItemFormatterTests` 单元测试

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Services/Learning/ContentField.cs` | 新增 |
| `Services/Learning/LearningItemFormatter.cs` | 修改 |
| `LearningAssistant.Tests/LearningItemFormatterTests.cs` | 新增 |

**前置依赖**：Phase 0 完成

**完成标准**：
- 编译通过，无错误
- `BuildFields` 覆盖全部 `SubCategoryType` 枚举值
- 单元测试全部通过
- 旧路径（`FormatDisplayText`）行为不变

**回归测试范围**：
- 学习卡片显示（各子类别）
- 答题模式揭示逻辑
- 已有 `LearningItemFormatter` 调用点

---

### Phase 2：统一发音协调层（LEARNING_CARD P2）

**来源**：LEARNING_CARD 第六章 6.6

**任务清单**：
1. 新增 `SpeakStateChangedEventArgs`（含 `SpeakKey` 播放来源标识）
2. 新增 `ISpeechCoordinator` 接口
3. 新增 `SpeechCoordinator` 实现（吸收 `_pronunciationQueue`）
4. 新增 `IDomainService` 空接口（预留注入点，Phase 5 填充）
5. `LearningFlowHandler` 全局发音切换到 `ISpeechCoordinator`
6. `LearningFlowHandler` 注入 `IDomainService`（当前为空实现）
7. 注册到 DI（`ServiceCollectionExtensions`）
8. 新增 `SpeechCoordinatorTests` 单元测试

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Services/TTS/SpeakStateChangedEventArgs.cs` | 新增 |
| `Services/TTS/ISpeechCoordinator.cs` | 新增 |
| `Services/TTS/SpeechCoordinator.cs` | 新增 |
| `Services/Learning/IDomainService.cs` | 新增（空接口，预留） |
| `Presenters/LearningFlowHandler.cs` | 修改 |
| `Common/ServiceCollectionExtensions.cs` | 修改 |
| `LearningAssistant.Tests/SpeechCoordinatorTests.cs` | 新增 |

**前置依赖**：Phase 1 完成

**完成标准**：
- 编译通过，无错误
- `_pronunciationQueue` 已移除，由 `SpeechCoordinator` 内部队列替代
- `IDomainService` 空接口已注入，不影响当前逻辑
- 全局发音/自动播放/预缓存行为无回归
- 单元测试全部通过（串行化、`StopAsync` 中断、缓存命中、`SpeakStateChanged` 含 `SpeakKey`）

**回归测试范围**：
- 全局发音按钮（`buttonPronounce`/Space）
- 自动播放逻辑
- 预缓存机制

---

### Phase 3：卡片控件改造 + 字段行渲染（LEARNING_CARD P3）

**来源**：LEARNING_CARD 第六章 6.3-6.4

**任务清单**：
1. 新增 `ContentFieldRow` 行控件（播放状态反馈、复制按钮、播放来源追踪、TTS 不可用禁用）
2. 改造 `LearningCard`（明细区容器化、`SetFields`、行复用、删除 `Content` 属性、`AutoSize`）
3. `LearningCard` 传递 `ISpeechCoordinator` 给行控件

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Forms/UserControls/ContentFieldRow.cs` | 新增 |
| `Forms/UserControls/LearningCard.cs` | 修改 |

**前置依赖**：Phase 2 完成（`ISpeechCoordinator` 已就位）

**完成标准**：
- 编译通过，无错误
- 卡片明细区按字段行渲染
- 🔊 按钮显示/隐藏正确（`SpeakText != null`）
- 播放状态反馈正确（🔊→⏸+高亮）
- 复制按钮功能正常（📋→✓）
- TTS 不可用时按钮禁用
- 播放来源追踪正确（多行快速切换不混乱）
- 行复用逻辑正确（不重建控件树）

**回归测试范围**：
- 学习卡片视觉效果（各子类别）
- 答题模式揭示行为
- 整卡点击行为（不被按钮点击干扰）
- DPI 适配（100%/125%/150%）
- 暗色模式

---

### Phase 4a：视图集成 + 键盘快捷键（LEARNING_CARD P4）

**来源**：LEARNING_CARD 第六章 6.5-6.7

**任务清单**：
1. 改造 `LearningForm`：`UpdateLearningCard`/`UpdateDetailState` 改用 `SetFields`
2. 新增 `UpdateKeyboardShortcutsMapping`（同步 `Alt+1..5` 映射）
3. `ProcessCmdKey` 加 `Alt+1..5` 快捷键
4. 转发三个字段事件（`FieldSpeakRequested`/`FieldStopRequested`/`FieldCopyRequested`）
5. `FieldCopyRequested` 直接处理 `Clipboard.SetText`
6. `ILearningView` 新增三个字段发音事件
7. `LearningFlowHandler` 订阅字段发音/停止（传递 `speakKey`）
8. Dispose 中清理新增事件

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Forms/LearningForm.cs` | 修改 |
| `Views/ILearningView.cs` | 修改 |
| `Presenters/LearningFlowHandler.cs` | 修改 |

**前置依赖**：Phase 3 完成

**完成标准**：
- 编译通过，无错误
- 字段级 🔊/📋 可用，走 Presenter
- 播放状态同步正确
- 来源追踪正确（快速切换多行）
- 与全局发音不叠加
- `Alt+1..Alt+5` 快捷键工作正常
- 事件清理正确（无内存泄漏）

**回归测试范围**：
- 端到端字段发音
- 复制功能
- 键盘快捷键（`Alt+1..Alt+5`、Space、Tab）
- 全局发音与字段发音互斥
- 播放来源追踪（快速切换多行）
- 事件生命周期（Dispose 后无内存泄漏）

---

### Phase 4b：ILearningView 参数化改造（INTERFACE_REFACTORING）

**来源**：INTERFACE_REFACTORING_SUGGESTIONS

**任务清单**：
1. `ILearningView` 参数化改造（`LearningContext` 替代分散参数）
2. 更新 `LearningForm` 实现
3. 更新 `LearningFlowHandler` 调用
4. 更新其他 `ILearningView` 实现类

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Views/ILearningView.cs` | 修改 |
| `Forms/LearningForm.cs` | 修改 |
| `Presenters/LearningFlowHandler.cs` | 修改 |

**前置依赖**：Phase 4a 完成

**完成标准**：
- 编译通过，无错误
- `ILearningView` 参数化完成（`LearningContext`）
- 字段发音事件保留且正常工作
- 功能无回归

**回归测试范围**：
- 学习引擎初始化
- 进度查询
- 字段发音功能

---

### Phase 5：领域服务层引入 + 用户聚合拆分（ENTITY 9.2 + 10.4）

**来源**：ENTITY 第九章 9.2 + 第十章 10.4

**任务清单**：
1. 创建 `LearningDomainService` 实现 `IDomainService` 接口
2. 识别跨聚合业务场景（完成学习项、完成挑战、添加笔记、完成复习、完成费曼）
3. 领域服务协调跨聚合操作，发布领域事件
4. **用户聚合拆分**：将 `Reminder`/`ReminderRepeatDay` 和 `LearningGoal`/`DailyGoalRecord` 拆分为独立聚合
5. 创建 `ReminderRepository` 和 `LearningGoalRepository`
6. `LearningFlowHandler` 中跨聚合逻辑迁移到领域服务
7. 领域服务调用 `ISpeechCoordinator` 完成发音操作

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Services/Learning/LearningDomainService.cs` | 新增（实现 `IDomainService`） |
| `Services/Learning/IDomainService.cs` | 修改（添加方法签名） |
| `Services/Repositories/IReminderRepository.cs` | 新增 |
| `Services/Repositories/ILearningGoalRepository.cs` | 新增 |
| `Presenters/LearningFlowHandler.cs` | 修改 |
| `Common/ServiceCollectionExtensions.cs` | 修改（注册领域服务和仓储） |
| `Data/Database/Entities.cs` | 修改（调整实体关系） |

**前置依赖**：Phase 4b 完成（`ISpeechCoordinator` 和字段事件已就位）

**完成标准**：
- 编译通过，无错误
- 跨聚合业务操作由领域服务协调
- 领域事件正确发布（`ItemLearnedEvent`、`ChallengeCompletedEvent` 等）
- 用户聚合已拆分为三个独立聚合
- 发音操作通过 `ISpeechCoordinator` 执行
- 业务规则不变（XP 奖励、挑战进度等）

**回归测试范围**：
- 完成学习项流程（XP+10、加入复习队列、更新挑战进度）
- 完成挑战流程（解锁徽章、更新 XP）
- 添加笔记流程（XP+15、更新目标进度）
- 完成复习流程（更新记忆强度、移除错题）
- 提醒功能
- 目标管理功能

---

### Phase 6：ValueObject 基类统一（ENTITY 9.1）

**来源**：ENTITY 第九章 9.1

**任务清单**：
1. 保留 `Models/ValueObjects/ValueObject.cs` 作为统一基类
2. 删除 `Models/Learning/ValueObjects/ValueObject.cs`
3. 将 `Meaning`/`Example`/`Pronunciation`/`CharacterFeatures`/`WordFeatures`/`LearningProgress` 的基类改为统一基类
4. 保持字段结构不变，仅修改继承关系

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Models/Learning/ValueObjects/ValueObject.cs` | 删除 |
| `Models/Learning/ValueObjects/Meaning.cs` | 修改（继承） |
| `Models/Learning/ValueObjects/Example.cs` | 修改（继承） |
| `Models/Learning/ValueObjects/Pronunciation.cs` | 修改（继承） |
| `Models/Learning/ValueObjects/CharacterFeatures.cs` | 修改（继承） |
| `Models/Learning/ValueObjects/WordFeatures.cs` | 修改（继承） |
| `Models/Learning/ValueObjects/LearningProgress.cs` | 修改（继承） |

**前置依赖**：Phase 4b 完成（LEARNING_CARD 全部 UI 改造完成）

**完成标准**：
- 编译通过，无错误
- 值对象字段结构不变
- `LearningItemFormatter.BuildFields` 正常工作
- LEARNING_CARD 功能无回归

**回归测试范围**：
- 所有学习卡片显示（各子类别）
- 字段发音功能
- 答题模式揭示

---

### Phase 7：添加乐观并发控制（ENTITY 10.3.1）

**来源**：ENTITY 第十章 10.3.1

**任务清单**：
1. `AuditableEntityBase` 添加 `RowVersion` 字段
2. EF Core 配置 `IsRowVersion`
3. 添加并发冲突处理逻辑

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Data/Database/EntityBase.cs` | 修改（添加 `RowVersion`） |
| `Data/Database/AppDbContext.cs` | 修改（配置 `IsRowVersion`） |

**前置依赖**：Phase 6 完成

**完成标准**：
- 编译通过，无错误
- `RowVersion` 字段正确添加
- EF Core 配置正确
- 并发冲突可被捕获和处理

**回归测试范围**：
- 所有数据持久化操作
- 并发场景测试（模拟多用户同时修改）

---

### Phase 8：LearningItem 持久化迁移（ENTITY 9.6）

**来源**：ENTITY 第九章 9.6

**任务清单**：
1. 创建 `LearningItemEntity` 数据库实体（自带 `RowVersion`）
2. 更新 `AppDbContext` 注册实体
3. 更新 `DbModelConverter` 添加转换方法
4. 创建数据迁移工具（`DataMigrationService`）
5. 保留旧的文件系统加载路径作为兼容层
6. 通过配置开关控制数据来源

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Data/Database/Entities.cs` | 修改（新增 `LearningItemEntity`） |
| `Data/Database/AppDbContext.cs` | 修改（注册实体） |
| `Data/Database/DbModelConverter.cs` | 修改（添加转换） |
| `Services/Data/DataMigrationService.cs` | 新增 |
| `Common/ServiceCollectionExtensions.cs` | 修改（注册迁移服务） |

**前置依赖**：Phase 7 完成（`RowVersion` 已添加，新实体自带并发控制）

**完成标准**：
- 编译通过，无错误
- `LearningItemEntity` 正确映射 `LearningItem`，自带 `RowVersion`
- 数据迁移工具可将文件系统数据迁移到数据库
- 配置开关可切换数据来源
- LEARNING_CARD 功能无回归（输入仍是 `LearningItem` 对象）

**回归测试范围**：
- 学习内容加载（文件系统 + 数据库两种模式）
- 学习卡片显示
- 字段发音功能
- 数据持久化验证（重启后数据保留）

---

### Phase 9：基础设施优化（ENTITY 10.2）

**来源**：ENTITY 第十章 10.2

**任务清单**：
1. 为每个聚合根创建仓储接口和实现
2. 注册到 DI

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Services/Repositories/IUserProfileRepository.cs` | 新增 |
| `Services/Repositories/ISpacedRepetitionRepository.cs` | 新增 |
| `Services/Repositories/IWrongAnswerRepository.cs` | 新增 |
| `Services/Repositories/IFavoritesRepository.cs` | 新增 |
| `Services/Repositories/IMentorSessionRepository.cs` | 新增 |
| `Services/Repositories/IKnowledgeGraphRepository.cs` | 新增 |
| `Services/Repositories/IQuizRepository.cs` | 新增 |
| `Common/ServiceCollectionExtensions.cs` | 修改（注册仓储） |

**前置依赖**：Phase 8 完成

**完成标准**：
- 编译通过，无错误
- 所有聚合根有对应的仓储接口和实现
- 现有数据访问功能无回归

**回归测试范围**：
- 所有数据持久化操作
- 现有 Service 层功能

---

### Phase 10：清理收尾（LEARNING_CARD P5）

**来源**：LEARNING_CARD 第六章 6.7

**任务清单**：
1. 移除 `FormatDisplayText`/`FormatDisplayStruct` 旧方法及 `LearningCard.Content` 残留引用
2. 移除 `LearningContentView` 中 `_labelContent`/`_listBoxDisplay` 死代码

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Services/Learning/LearningItemFormatter.cs` | 修改（移除旧方法） |
| `Forms/UserControls/LearningCard.cs` | 修改（清理残留引用） |
| `Forms/UserControls/LearningContentView.cs` | 修改（移除死代码） |

**前置依赖**：Phase 9 完成

**完成标准**：
- 编译通过，无错误
- 旧代码路径完全移除
- 功能无回归

**回归测试范围**：
- 全量回归测试

---

### Phase 11：废弃 JSON 字段（ENTITY 9.3）

**来源**：ENTITY 第九章 9.3

> **注意**：此 Phase 需在 Phase 8 上线后运行 **2-4 周验证期**，确认 `LearningItemStateEntity` 在生产中完全可靠后再执行。

**任务清单**：
1. 废弃 `CategoryProgressEntity` 中的 `KnownItemsJson` 和 `UnknownItemsJson` 字段
2. 完全使用 `LearningItemStateEntity` 存储学习项状态
3. 数据迁移：将现有 JSON 数据迁移到 `LearningItemStateEntity`

**涉及文件**：
| 文件 | 改动类型 |
|------|----------|
| `Data/Database/Entities.cs` | 修改（废弃 JSON 字段） |
| `Services/Data/DataMigrationService.cs` | 修改（添加 JSON 迁移） |

**前置依赖**：Phase 10 完成 + 验证期结束

**完成标准**：
- 编译通过，无错误
- `CategoryProgressEntity` 不再使用 JSON 字段
- 数据迁移完成，无数据丢失
- 功能无回归

**回归测试范围**：
- 学习进度统计
- 学习项状态管理
- 全量回归测试

---

## 三、关键路径

```
Phase 0 ──► Phase 1 ──► Phase 2 ──► Phase 3 ──► Phase 4a ──► Phase 4b ──► Phase 5 ──► Phase 6 ──► Phase 7 ──► Phase 8 ──► Phase 9 ──► Phase 10 ──► [2-4周验证期] ──► Phase 11
              │               │               │               │               │               │               │               │               │
              │               └───────────────┴───────────────┴───────────────┴───────────────┘               │               │
              │                                                     │                        │               │
              └─────────────────────────────────────────────────────┘                        │               │
                                                                                             │               │
                                      关键路径（任何延期会阻塞后续所有 Phase）                  │               │
                                                                                             │               │
                                          Phase 4a + 4b 是最大瓶颈：                           │               │
                                          - ILearningView 接口改造（字段事件 + 参数化）          │               │
                                          - LearningForm 集成                                │               │
                                          - LearningFlowHandler 改造                        │               │
```

**关键路径分析**：
- **Phase 0-4b**：LEARNING_CARD 核心改造，必须顺序执行
- **Phase 5**：依赖 Phase 4b 的 `ISpeechCoordinator` 和字段事件，同时完成用户聚合拆分
- **Phase 6**：依赖 Phase 4b 完成（值对象结构不再变动）
- **Phase 7**：依赖 Phase 6 完成（实体基类稳定）
- **Phase 8**：依赖 Phase 7 完成（`RowVersion` 已添加）
- **Phase 9**：依赖 Phase 8 完成（数据库实体稳定）
- **Phase 10**：依赖所有前面的 Phase 完成
- **Phase 11**：依赖 Phase 10 完成 + 2-4 周验证期

---

## 四、风险矩阵

| 风险 | 概率 | 影响 | 缓解措施 | 关联 Phase |
|------|------|------|----------|------------|
| LEARNING_CARD 字段映射错误 | 低 | 高 | `LearningItemFormatterTests` 覆盖全部子类别 | Phase 1 |
| `ISpeechCoordinator` 串行化失败 | 中 | 高 | `SpeechCoordinatorTests` 测试多并发场景 | Phase 2 |
| 播放来源追踪状态混乱 | 中 | 中 | `SpeakKey` 唯一性保证（`Label + ":" + SpeakText`） | Phase 3-4a |
| ILearningView 接口改造回归 | 高 | 中 | 拆分为 Phase 4a（字段事件）和 Phase 4b（参数化），降低单次风险 | Phase 4a, 4b |
| LearningFlowHandler 两次改造冲突 | 中 | 高 | Phase 2 预留 `IDomainService` 空接口注入点，Phase 5 只填充实现 | Phase 2, 5 |
| 用户聚合拆分数据迁移失败 | 中 | 高 | 创建 `ReminderRepository` 和 `LearningGoalRepository`，保留旧数据作为兼容层 | Phase 5 |
| 值对象基类统一导致字段映射失效 | 低 | 中 | 仅修改继承关系，保持字段结构不变 | Phase 6 |
| LearningItem 持久化迁移数据丢失 | 低 | 高 | 保留旧路径作为兼容层，添加数据校验；新实体自带 `RowVersion` | Phase 8 |
| 并发冲突未正确处理 | 中 | 中 | `RowVersion` + `DbUpdateConcurrencyException` 处理 | Phase 7 |
| JSON 字段废弃后数据不一致 | 低 | 高 | 设置 2-4 周验证期，确保 `LearningItemStateEntity` 完全可靠 | Phase 11 |

---

## 五、资源需求评估

| Phase | 预估工时 | 关键技能 | 依赖资源 |
|-------|----------|----------|----------|
| Phase 0 | 2h | Git、CI/CD | 项目环境 |
| Phase 1 | 4h | C#、单元测试 | `LearningItem` 结构理解 |
| Phase 2 | 8h | C#、TTS、异步编程 | `ITTSService` 理解 |
| Phase 3 | 12h | WinForms、UI 设计 | `LearningCard` 当前代码 |
| Phase 4a | 10h | WinForms、MVP 模式 | `LearningForm`、`ILearningView` |
| Phase 4b | 6h | WinForms、接口重构 | `LearningContext` 理解 |
| Phase 5 | 12h | DDD、领域事件、EF Core | `IEventBus`、各 Service、实体关系 |
| Phase 6 | 2h | C#、继承重构 | 值对象当前代码 |
| Phase 7 | 4h | EF Core、并发控制 | `EntityBase` |
| Phase 8 | 10h | EF Core、数据迁移 | `AppDbContext` |
| Phase 9 | 8h | Repository Pattern | 数据库实体理解 |
| Phase 10 | 4h | 代码清理、回归测试 | 全量代码理解 |
| Phase 11 | 4h | 数据迁移、回归测试 | `CategoryProgressEntity` |
| **合计** | **86h** | | |

---

## 六、交付物清单

| Phase | 交付物 | 验收标准 |
|-------|--------|----------|
| Phase 0 | 基线测试报告、分支策略文档、Git tag | 测试通过，tag 创建完成 |
| Phase 1 | `ContentField.cs`、`LearningItemFormatterTests.cs` | 编译通过，测试通过 |
| Phase 2 | `ISpeechCoordinator.cs`、`SpeechCoordinator.cs`、`IDomainService.cs`、`SpeechCoordinatorTests.cs` | 全局发音无回归 |
| Phase 3 | `ContentFieldRow.cs`、改造后的 `LearningCard.cs` | 卡片字段行渲染正常 |
| Phase 4a | 改造后的 `LearningForm.cs`、`ILearningView.cs`、`LearningFlowHandler.cs` | 字段级 🔊/📋 可用 |
| Phase 4b | 参数化改造后的 `ILearningView.cs` | `LearningContext` 参数化完成 |
| Phase 5 | `LearningDomainService.cs`、用户聚合拆分 | 跨聚合操作协调正常，三个独立聚合就位 |
| Phase 6 | 统一后的 ValueObject 基类 | 值对象结构不变，编译通过 |
| Phase 7 | `RowVersion` 字段、并发冲突处理 | 并发控制生效 |
| Phase 8 | `LearningItemEntity.cs`、`DataMigrationService.cs` | 数据可从文件系统迁移到数据库 |
| Phase 9 | 各仓储接口和实现 | 仓储模式就位 |
| Phase 10 | 清理后的代码库 | 旧代码路径完全移除 |
| Phase 11 | JSON 字段废弃、数据迁移完成 | 数据一致，功能无回归 |

---

## 七、执行策略

### 7.1 每日检查点

每个 Phase 执行期间，每天结束前检查：
1. 编译是否通过
2. 已有单元测试是否通过
3. 关键功能是否可用（手动验证）

### 7.2 代码审查

每个 Phase 完成后，进行代码审查：
1. 代码风格符合项目规范
2. 新增代码有单元测试覆盖
3. 无内存泄漏风险（事件清理）
4. 异常处理完善

### 7.3 回滚策略

每个 Phase 开始前，创建 git 分支：
```bash
git checkout -b phase-0-preparation
git checkout -b phase-1-content-field
git checkout -b phase-2-speech-coordinator
# ...
```

若 Phase 失败，直接回滚到上一 Phase 的稳定分支。

### 7.4 进度追踪

使用项目管理工具追踪每个 Phase 的进度：
- 任务完成率
- 阻塞问题
- 风险状态

### 7.5 验证期管理（Phase 11 前）

Phase 8 完成后，进入验证期：
1. 部署到测试环境
2. 运行 2-4 周验证
3. 监控 `LearningItemStateEntity` 的使用情况
4. 确认无数据不一致问题
5. 验证期结束后执行 Phase 11

---

## 八、注意事项

1. **不要跳过 Phase**：每个 Phase 有明确的依赖关系，跳过会导致后续 Phase 无法进行
2. **保持向后兼容**：过渡期保留旧方法，逐步移除
3. **测试先行**：每个 Phase 的单元测试应与代码同步完成
4. **文档同步**：代码变更后，同步更新相关文档
5. **用户通知**：涉及用户体验变化的 Phase（Phase 3-4a）完成后，通知用户测试新功能
6. **验证期必须执行**：Phase 11（JSON 字段废弃）必须在验证期确认 `LearningItemStateEntity` 可靠后才能执行
7. **预留注入点**：Phase 2 预留的 `IDomainService` 空接口在 Phase 5 才填充实现，减少二次侵入