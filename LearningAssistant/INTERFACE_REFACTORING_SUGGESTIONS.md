# 接口调整建议

## 问题分析

基于学习项模型重构后的统一数据模型，当前项目中大量接口存在以下问题：

### 1. 语言与科目概念混淆

| 问题位置 | 具体问题 | 影响 |
|---------|---------|------|
| `SubjectType` 枚举 | 仅包含 `Chinese`、`English`，实际是语言概念 | 语义混淆 |
| `ILearningView` | 同时存在 `Language` 和 `Subject` 属性 | 冗余且矛盾 |
| `IContentEditorView` | 同时存在 `SelectedLanguage` 和 `SelectedSubject` | 冗余 |
| `IContentLoaderService` | `GetSubCategories(string language)` 和 `GetSubCategoriesBySubject(string subject)` 并存 | 重复方法 |

### 2. 字符串类型滥用

大量接口方法使用 `string` 类型传递语言、科目、子类别等语义明确的值：

| 接口 | 问题方法 | 问题参数 |
|-----|---------|---------|
| `IStudyEngine` | `Initialize()` | `language`, `subCategory` 为 string |
| `IStudyEngine` | `GetProgressSummary()` | `language`, `subCategory` 为 string |
| `IContentLoaderService` | `LoadItems()` | `subCategory` 为 string |
| `IDataPersistenceService` | `GetKnownItems()` | `categoryName` 为 string |
| `IWrongAnswerService` | `GetWrongAnswers()` | `subject`, `category` 为 string |

### 3. 参数重复

多个方法重复接收相同的上下文参数（userId, subject, subCategory）：

```csharp
// 当前模式 - 参数重复
string GetProgressSummary(string userId, string language, string subCategory);
int GetKnownCount(string userId, string subCategory);
int GetUnknownCount(string userId, string subCategory);
double GetAccuracy(string userId, string subCategory);
```

### 4. 学习上下文分散

学习相关的上下文信息（语言、子类别、词库文件等）分散在多个对象中：
- `SessionData` - 保存上次学习状态
- `LearningConfig` - 用户学习配置
- `IStudyEngine.Initialize()` 参数 - 运行时上下文

---

## 前提条件检查

在开始 Phase A 之前，请确认以下前提条件：

| 检查项 | 状态 | 说明 |
|-------|------|------|
| 代码库编译状态 | ✅ 通过 | 只有警告，无错误 |
| LearningModeType 枚举 | ✅ 存在 | 在 `Enums.cs` 中定义 |
| SortOrderType 枚举 | ✅ 存在 | 在 `Enums.cs` 中定义 |
| WrongAnswerItem | ⚠️ 需调整 | Subject/Category 为 string |
| WrongAnswerFilter | ⚠️ 需调整 | Subject/Category 为 string |
| WrongAnswerStats | ⚠️ 需调整 | SubjectStats 为 Dictionary\<string, int\> |
| 学习相关事件 | ⚠️ 需调整 | SubCategory/Language 为 string |
| Constants 类 | ⚠️ 需清理 | 包含大量字符串常量 |

---

## 调整建议

### 建议 1：统一语言/科目模型

**核心原则**：当前系统仅处理语言学习（中文、英语），`SubjectType` 直接表示学习语言/学科，不进行过度抽象。通过显示字符串转换消除语义混淆。

```csharp
// 学科/语言枚举（保持现有，语义明确化）
public enum SubjectType
{
    Chinese,      // 语文（中文）
    English       // 英语
}

// 子类别枚举（保持现有）
public enum SubCategoryType
{
    ChineseCharacter,
    ChinesePhrase,
    ChineseIdiom,
    ChinesePoem,
    ChineseComprehensive,
    EnglishWord,
    EnglishPhrase,
    EnglishSentence,
    EnglishComprehensive
}
```

**关联关系**：
- `SubjectType` → `SubCategoryType`：通过 `SubjectSubCategoryMapping` 获取有效子类别
- `SubjectType` → 显示名称：通过 `EnumExtensions.ToDisplayString()` 获取"语文"/"英语"

### 建议 2：引入统一学习上下文对象

**新增** `LearningContext` 不可变记录类型：

```csharp
public record LearningContext(
    string UserId,
    SubjectType Subject,
    SubCategoryType SubCategory,
    string WordBankFile = "",
    LearningModeType Mode = LearningModeType.Study,
    SortOrderType SortOrder = SortOrderType.Sequential
);
```

**优势**：
- **不可变性**：使用 record 类型，防止传递过程中被意外修改
- **消除参数重复**：统一封装学习上下文
- **值语义**：基于属性值进行相等性比较
- **简洁性**：record 自动生成构造函数、ToString()、Equals() 等方法

#### 2.1 LearningContext 字段使用标注

| 字段 | 用途 | 必需性 |
|-----|------|-------|
| `UserId` | 用户标识 | 所有方法必需 |
| `Subject` | 学科/语言 | 学习引擎初始化、进度查询必需 |
| `SubCategory` | 子类别 | 内容加载、进度查询必需 |
| `WordBankFile` | 词库文件路径 | 内容加载时使用，为空则使用默认 |
| `Mode` | 学习模式 | 学习引擎初始化时使用 |
| `SortOrder` | 排序方式 | 学习引擎初始化时使用 |

#### 2.2 LearningContext 构建者模式

**新增** `LearningContextFactory` 静态类：

```csharp
public static class LearningContextFactory
{
    public static LearningContext FromSessionData(SessionData session, string userId)
    {
        return new LearningContext(
            UserId: userId,
            Subject: session.LastSubject,
            SubCategory: session.LastSubCategory
        );
    }

    public static LearningContext FromLearningConfig(LearningConfig config, string userId)
    {
        return new LearningContext(
            UserId: userId,
            Subject: config.Subject,
            SubCategory: config.SubCategory,
            Mode: config.Mode,
            SortOrder: config.SortOrder
        );
    }

    public static LearningContext FromUiSelection(string userId, SubjectType subject, 
        SubCategoryType subCategory, string wordBankFile = "")
    {
        return new LearningContext(
            UserId: userId,
            Subject: subject,
            SubCategory: subCategory,
            WordBankFile: wordBankFile
        );
    }

    public static LearningContext FromEngineState(StudyEngineState state)
    {
        return new LearningContext(
            UserId: state.UserId,
            Subject: state.Subject,
            SubCategory: state.SubCategory,
            WordBankFile: state.WordBankFile,
            Mode: state.CurrentMode,
            SortOrder: state.CurrentSortOrder
        );
    }

    public static LearningContext WithWordBankFile(this LearningContext context, string wordBankFile)
    {
        return context with { WordBankFile = wordBankFile };
    }

    public static LearningContext WithMode(this LearningContext context, LearningModeType mode)
    {
        return context with { Mode = mode };
    }

    public static LearningContext WithSortOrder(this LearningContext context, SortOrderType sortOrder)
    {
        return context with { SortOrder = sortOrder };
    }
}
```

**使用示例**：

```csharp
// 从Session创建
var context = LearningContextFactory.FromSessionData(session, userId);

// 从配置创建并添加词库文件
var context = LearningContextFactory.FromLearningConfig(config, userId)
    .WithWordBankFile(wordBankFile);

// 从UI选择创建
var context = LearningContextFactory.FromUiSelection(userId, SubjectType.Chinese, SubCategoryType.ChineseCharacter);
```

### 建议 3：接口调整方案

#### 3.1 `IStudyEngine` 接口调整

**当前**：
```csharp
void Initialize(string userId, string language, string subCategory, string wordBankFile, 
                string mode = "Study", string sortOrder = "Sequential", bool continueMode = true);

string GetProgressSummary(string userId, string language, string subCategory);
int GetKnownCount(string userId, string subCategory);
int GetUnknownCount(string userId, string subCategory);
double GetAccuracy(string userId, string subCategory);
```

**调整后**：
```csharp
void Initialize(LearningContext context, bool continueMode = true);

string GetProgressSummary(LearningContext context);
int GetKnownCount(LearningContext context);
int GetUnknownCount(LearningContext context);
double GetAccuracy(LearningContext context);

LearningContext CurrentContext { get; }
```

#### 3.2 `IContentLoaderService` 接口调整

**当前**：
```csharp
List<LearningItem> LoadItems(string subCategory, string wordBankFile = "");
void SaveItems(string subCategory, List<LearningItem> items, string wordBankFile = "");
List<string> GetSubCategories(string language);
List<string> GetSubCategoriesBySubject(string subject);
Type GetItemType(string subCategory);
```

**调整后**：
```csharp
List<LearningItem> LoadItems(LearningContext context);
void SaveItems(LearningContext context, List<LearningItem> items);
List<SubCategoryType> GetSubCategories(SubjectType subject);
// 删除 GetSubCategoriesBySubject - 与 GetSubCategories 语义重复
// 删除 GetItemType - 统一使用 LearningItem，无需获取具体类型
```

#### 3.3 `IDataPersistenceService` 接口调整

**当前**：
```csharp
List<string> GetKnownItems(string userId, string categoryName);
List<string> GetUnknownItems(string userId, string categoryName);
void UpsertLearningItemState(string userId, string categoryName, string content, bool isKnown);
void UpsertLearningItemStates(string userId, string categoryName, IEnumerable<string> contents, bool isKnown);
void DeleteLearningItemState(string userId, string categoryName, string content);
void SyncCategoryProgressToLearningItemStates(string userId, string categoryName, 
                                               List<string> knownItems, List<string> unknownItems);
```

**调整后**：
```csharp
List<string> GetKnownItems(LearningContext context);
List<string> GetUnknownItems(LearningContext context);
void UpsertLearningItemState(LearningContext context, string content, bool isKnown);
void UpsertLearningItemStates(LearningContext context, IEnumerable<string> contents, bool isKnown);
void DeleteLearningItemState(LearningContext context, string content);
void SyncCategoryProgressToLearningItemStates(LearningContext context, 
                                               List<string> knownItems, List<string> unknownItems);
```

#### 3.4 `IWrongAnswerService` 接口调整

**当前**：
```csharp
List<WrongAnswerItem> GetWrongAnswers(string userId, string subject = "", string category = "");
List<WrongAnswerItem> GetBySubjectCategory(string userId, string subject, string category);
List<string> GetSubjects(string userId);
List<string> GetCategories(string userId, string subject);
```

**调整后**：
```csharp
List<WrongAnswerItem> GetWrongAnswers(string userId, LearningContext? context = null);
List<WrongAnswerItem> GetBySubjectCategory(LearningContext context);
List<SubjectType> GetSubjects(string userId);
List<SubCategoryType> GetCategories(string userId, SubjectType subject);
```

#### 3.5 `IDataImportService` 接口调整

**当前**：
```csharp
List<string> GetSupportedContentTypes();
List<string> GetContentTypeFields(string contentType);
```

**调整后**：
```csharp
List<SubCategoryType> GetSupportedSubCategories();
List<string> GetSubCategoryFields(SubCategoryType subCategory);
```

#### 3.6 `ILearningView` 接口调整

**当前**：
```csharp
string Language { get; }
string Subject { get; }
string SubCategory { get; set; }
```

**调整后**：
```csharp
LearningContext Context { get; set; }
// 删除独立的 Language、Subject 属性，通过 Context 访问
```

#### 3.7 `IContentEditorView` 接口调整

**当前**：
```csharp
string SelectedLanguage { get; }
string SelectedSubject { get; }
string SelectedSubCategory { get; }

event EventHandler? LanguageChanged;
event EventHandler? SubjectChanged;
event EventHandler? SubCategoryChanged;

void SetInitialLanguage(string language);
void SetInitialSubject(string subject);
void SetInitialSubCategory(string subCategory);
```

**调整后**：
```csharp
LearningContext SelectedContext { get; }

event EventHandler<ContextChangedEventArgs>? SubjectChanged;
event EventHandler<ContextChangedEventArgs>? SubCategoryChanged;

void SetInitialContext(LearningContext context);
void RefreshSubCategories(IEnumerable<SubCategoryType> subCategories);
```

**新增** `ContextChangedEventArgs`：

```csharp
public class ContextChangedEventArgs : EventArgs
{
    public SubjectType? OldSubject { get; }
    public SubjectType? NewSubject { get; }
    public SubCategoryType? OldSubCategory { get; }
    public SubCategoryType? NewSubCategory { get; }
    
    public ContextChangedEventArgs(SubjectType? oldSubject, SubjectType? newSubject,
        SubCategoryType? oldSubCategory, SubCategoryType? newSubCategory)
    {
        OldSubject = oldSubject;
        NewSubject = newSubject;
        OldSubCategory = oldSubCategory;
        NewSubCategory = newSubCategory;
    }
}
```

**说明**：保留细粒度事件（SubjectChanged、SubCategoryChanged）而非合并为单一 ContextChanged，因为：
- `SubjectChanged` 触发子类别列表刷新
- `SubCategoryChanged` 触发内容加载和数据表刷新
- 每个事件有不同的副作用，合并会强制订阅者进行 diff 判断

#### 3.8 `SessionData` 调整

**当前**：
```csharp
public string Language { get; set; } = string.Empty;
public string SubCategory { get; set; } = string.Empty;
```

**调整后**：
```csharp
public SubjectType LastSubject { get; set; }
public SubCategoryType LastSubCategory { get; set; }
// 删除 Language - 通过 Subject 推导
```

#### 3.9 `WrongAnswerItem` 调整

**当前**：
```csharp
public string Subject { get; set; } = string.Empty;
public string Category { get; set; } = string.Empty;
```

**调整后**：
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public SubjectType Subject { get; set; }

[JsonConverter(typeof(JsonStringEnumConverter))]
public SubCategoryType Category { get; set; }
```

#### 3.10 `WrongAnswerFilter` 调整

**当前**：
```csharp
public string? Subject { get; set; }
public string? Category { get; set; }
```

**调整后**：
```csharp
public SubjectType? Subject { get; set; }
public SubCategoryType? Category { get; set; }
```

#### 3.11 `WrongAnswerStats` 调整

**当前**：
```csharp
public Dictionary<string, int> SubjectStats { get; set; } = new();
```

**调整后**：
```csharp
public Dictionary<SubjectType, int> SubjectStats { get; set; } = new();
```

#### 3.12 学习相关事件调整

**当前**（`LearningSessionStartedEvent`）：
```csharp
public string Language { get; set; } = string.Empty;
public string SubCategory { get; set; } = string.Empty;
```

**调整后**：
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public SubjectType Subject { get; set; }

[JsonConverter(typeof(JsonStringEnumConverter))]
public SubCategoryType SubCategory { get; set; }
```

**当前**（`ItemWrongEvent`, `ItemLearnedEvent`, `LearningSessionCompletedEvent`, `FeynmanCompletedEvent`）：
```csharp
public string SubCategory { get; set; } = string.Empty;
```

**调整后**：
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public SubCategoryType SubCategory { get; set; }
```

**当前**（`SendToPdfSearchEvent`）：
```csharp
public string Language { get; set; } = string.Empty;
```

**调整后**：
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public SubjectType Subject { get; set; }
```

#### 3.13 `Constants` 类清理

**当前**：包含大量字符串常量（Subject、Language、LearningMode、SubCategory、SortOrder）

**调整后**：
- 删除 `Constants.Subject` 类（使用 `SubjectType` 枚举）
- 删除 `Constants.Language` 类（使用 `SubjectType` 枚举）
- 删除 `Constants.LearningMode` 类（使用 `LearningModeType` 枚举）
- 删除 `Constants.SubCategory` 类（使用 `SubCategoryType` 枚举）
- 删除 `Constants.SortOrder` 类（使用 `SortOrderType` 枚举）
- 保留 `Constants.FileName`（文件路径仍需字符串）
- 保留 `Constants.CacheDuration`（时间常量）
- 保留 `Constants.DefaultUserId`

#### 3.14 `LearningConfig` 调整

**当前**：
```csharp
public string Language { get; set; } = string.Empty;
public string SubCategory { get; set; } = string.Empty;
public string Mode { get; set; } = string.Empty;
public string SortOrder { get; set; } = string.Empty;
```

**调整后**：
```csharp
public SubjectType Subject { get; set; }
public SubCategoryType SubCategory { get; set; }
public LearningModeType Mode { get; set; } = LearningModeType.Study;
public SortOrderType SortOrder { get; set; } = SortOrderType.Sequential;
// 删除 Language - 通过 Subject 推导
```

#### 3.15 `IMainView.UpdateDashboardStats` 清理（移除游戏化参数）

**当前**：
```csharp
void UpdateDashboardStats(int todayStudyMinutes, int streakDays, int totalXP,
    int currentLevel, int xpToNextLevel, int completedChallenges, int totalChallenges,
    int noteCount = 0, int todayNewNotes = 0);
```

**调整后**：
```csharp
void UpdateDashboardStats(int todayStudyMinutes, int streakDays, int knownItemsCount = 0, 
    int totalItemsCount = 0);
```

**说明**：移除游戏化相关参数（totalXP、currentLevel、xpToNextLevel、completedChallenges、totalChallenges）和笔记相关参数（noteCount、todayNewNotes），这些属于非核心功能。保留学习时长和连续学习天数作为核心统计指标。

---

## 调整优先级

| 优先级 | 调整项 | 原因 |
|-------|-------|------|
| **P0** | 引入 `LearningContext` | 消除参数重复，统一上下文 |
| **P0** | 接口参数 string → 枚举 | 类型安全，编译时检查 |
| **P1** | `ILearningView` 简化 | 消除语言/科目重复 |
| **P1** | `IContentLoaderService` 合并方法 | 消除语义重复 |
| **P2** | `SessionData` / `LearningConfig` 强类型化 | 数据一致性 |
| **P2** | `IWrongAnswerService` 强类型化 | 类型安全 |

---

## 实施步骤

### 步骤 1：定义基础类型（一次完成）

```
1. 确认 SubjectType 语义（学科/语言）
2. 创建 LearningContext 不可变记录类型
3. 更新 EnumExtensions 确保显示字符串正确（语文/英语）
```

### 步骤 2：调整核心服务接口（按依赖顺序）

```
1. IContentLoaderService - 内容加载是基础
2. IDataPersistenceService - 持久化层
3. IStudyEngine - 学习引擎（依赖前两者）
4. IWrongAnswerService - 错题服务
5. IDataImportService - 导入服务
```

### 步骤 3：调整视图接口

```
1. ILearningView - 学习视图
2. IContentEditorView - 编辑器视图
```

### 步骤 4：调整数据传输对象

```
1. SessionData
2. LearningConfig
```

### 步骤 5：更新实现类

```
按接口调整顺序，逐一更新实现类：
1. ContentLoaderService
2. SqliteDataPersistenceService
3. StudyEngine
4. WrongAnswerService
5. DataImportService
6. LearningForm
7. ContentEditorForm
8. 其他引用位置
```

---

## 兼容性考虑

### 向后兼容策略

1. **字符串-枚举双向转换**：利用现有 `SubjectSubCategoryMapping.TryParseSubject()` 和 `TryParseSubCategory()` 方法
2. **旧数据迁移**：通过 JSON 转换器处理旧格式数据（已有 `LearningItemJsonConverter`）
3. **API 兼容层**：对于对外暴露的接口，可保留 string 重载方法，内部转换为枚举

### 数据迁移示例

```csharp
// SessionData 迁移
public void MigrateSessionData(SessionData session)
{
    if (!string.IsNullOrEmpty(session.Language))
    {
        SubjectSubCategoryMapping.TryParseSubject(session.Language, out var subject);
        session.LastSubject = subject;
    }
    
    if (!string.IsNullOrEmpty(session.SubCategory))
    {
        SubjectSubCategoryMapping.TryParseSubCategory(session.SubCategory, out var subCategory);
        session.LastSubCategory = subCategory;
    }
}
```

---

## 收益评估

| 维度 | 调整前 | 调整后 | 收益 |
|-----|-------|-------|------|
| **类型安全** | 字符串魔法值 | 强类型枚举 | 编译时检查，减少运行时错误 |
| **代码清晰度** | 参数重复 | 统一上下文 | 方法签名简洁，意图明确 |
| **维护成本** | 多处定义 | 单点定义 | 变更只需修改一处 |
| **扩展性** | 字符串判断 | 枚举扩展 | 新增学科/子类别只需添加枚举值 |
| **文档性** | 需注释说明 | 类型自解释 | IDE 自动提示，减少文档负担 |

---

## 实现类调整方案

### 1. `ContentLoaderService` 实现调整

#### 1.1 字段调整

**当前**：
```csharp
private readonly Dictionary<string, string> _categoryFileMap = new Dictionary<string, string>
{
    { Constants.SubCategory.ChineseCharacter, Constants.FileName.ChineseCharacter },
    // ... 其他映射
};
```

**调整后**：
```csharp
private readonly Dictionary<SubCategoryType, string> _categoryFileMap = new Dictionary<SubCategoryType, string>
{
    { SubCategoryType.ChineseCharacter, Constants.FileName.ChineseCharacter },
    { SubCategoryType.ChineseIdiom, Constants.FileName.ChineseIdiom },
    { SubCategoryType.ChinesePhrase, Constants.FileName.ChinesePhrase },
    { SubCategoryType.ChinesePoem, Constants.FileName.ChinesePoem },
    { SubCategoryType.ChineseComprehensive, Constants.FileName.ChineseComprehensive },
    { SubCategoryType.EnglishWord, Constants.FileName.EnglishWord },
    { SubCategoryType.EnglishPhrase, Constants.FileName.EnglishPhrase },
    { SubCategoryType.EnglishSentence, Constants.FileName.EnglishSentence },
    { SubCategoryType.EnglishComprehensive, Constants.FileName.EnglishComprehensive }
};
```

#### 1.2 `LoadItems` 方法调整

**当前**：
```csharp
public List<LearningItem> LoadItems(string subCategory, string wordBankFile = "")
{
    try
    {
        string filePath = GetFilePath(subCategory, wordBankFile);
        // ... 路径安全检查和文件读取
        var json = File.ReadAllText(filePath);
        var items = JsonHelper.DeserializeLearningItems(json);
        
        foreach (var item in items)
        {
            if (item.SubCategory == 0)
            {
                if (Enum.TryParse(subCategory, out SubCategoryType subCategoryType))
                    item.SubCategory = subCategoryType;
            }
        }
        return items;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load items for subCategory: {SubCategory}", subCategory);
        return new List<LearningItem>();
    }
}
```

**调整后**：
```csharp
public List<LearningItem> LoadItems(LearningContext context)
{
    try
    {
        string filePath = GetFilePath(context.SubCategory, context.WordBankFile);
        
        if (!IsPathSafe(filePath))
        {
            _logger.LogWarning("Path traversal detected: {FilePath}", filePath);
            return new List<LearningItem>();
        }
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return new List<LearningItem>();
        }

        var json = File.ReadAllText(filePath);
        var items = JsonHelper.DeserializeLearningItems(json);

        foreach (var item in items)
        {
            if (item.SubCategory == 0)
            {
                item.SubCategory = context.SubCategory;
            }
        }

        return items;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load items for subCategory: {SubCategory}", context.SubCategory);
        return new List<LearningItem>();
    }
}
```

#### 1.3 `SaveItems` 方法调整

**当前**：
```csharp
public void SaveItems(string subCategory, List<LearningItem> items, string wordBankFile = "")
{
    try
    {
        string filePath = GetFilePath(subCategory, wordBankFile);
        JsonHelper.SaveToFile(filePath, items);
        _logger.LogInformation("Saved {Count} items to {FilePath}", items.Count, filePath);
    }
    catch (Exception ex)
    {
        // ...
    }
}
```

**调整后**：
```csharp
public void SaveItems(LearningContext context, List<LearningItem> items)
{
    try
    {
        string filePath = GetFilePath(context.SubCategory, context.WordBankFile);
        JsonHelper.SaveToFile(filePath, items);
        _logger.LogInformation("Saved {Count} items to {FilePath}", items.Count, filePath);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to save items for subCategory: {SubCategory}", context.SubCategory);
    }
}
```

#### 1.4 `GetSubCategories` 方法调整

**当前**：
```csharp
public List<string> GetSubCategories(string language)
{
    // ... 基于字符串语言获取子类别
}

public List<string> GetSubCategoriesBySubject(string subject)
{
    // ... 基于字符串学科获取子类别
}
```

**调整后**：
```csharp
public List<SubCategoryType> GetSubCategories(SubjectType subject)
{
    return SubjectSubCategoryMapping.GetSubCategories(subject);
}
```

---

### 2. `StudyEngine` 实现调整

#### 2.1 状态类调整

**新增** `StudyEngineState` 内部类调整：

```csharp
private class StudyEngineState
{
    public string UserId { get; set; } = string.Empty;
    public SubjectType Subject { get; set; }
    public SubCategoryType SubCategory { get; set; }
    public string WordBankFile { get; set; } = string.Empty;
    public LearningModeType CurrentMode { get; set; } = LearningModeType.Study;
    public SortOrderType CurrentSortOrder { get; set; } = SortOrderType.Sequential;
    public List<string> KnownItems { get; set; } = new List<string>();
    public List<string> UnknownItems { get; set; } = new List<string>();
    public int StudyModeIndex { get; set; }
    public int QuickModeIndex { get; set; }
}
```

#### 2.2 `Initialize` 方法调整

**当前**：
```csharp
public void Initialize(string userId, string language, string subCategory, string wordBankFile, 
                      string mode = Constants.LearningMode.Study, string sortOrder = Constants.SortOrder.Sequential, 
                      bool continueMode = true)
{
    ValidateInitializeParameters(userId, language, subCategory);

    lock (_stateLock)
    {
        _state.UserId = userId;
        _state.Language = language;
        _state.SubCategory = subCategory;
        _state.WordBankFile = wordBankFile;
        _state.CurrentMode = mode == Constants.LearningMode.Quick ? Constants.LearningMode.Quick : Constants.LearningMode.Study;
        _state.CurrentSortOrder = sortOrder;
    }

    LoadAllItems(subCategory, wordBankFile);

    if (continueMode)
    {
        _progressManager.LoadProgress(userId, subCategory);
        SyncProgressState();
    }
    else
    {
        ResetProgress();
    }

    BuildStudyItems();
    ValidateIndex();
}
```

**调整后**：
```csharp
public void Initialize(LearningContext context, bool continueMode = true)
{
    ValidateInitializeParameters(context);

    lock (_stateLock)
    {
        _state.UserId = context.UserId;
        _state.Subject = context.Subject;
        _state.SubCategory = context.SubCategory;
        _state.WordBankFile = context.WordBankFile;
        _state.CurrentMode = context.Mode;
        _state.CurrentSortOrder = context.SortOrder;
    }

    LoadAllItems(context);

    if (continueMode)
    {
        _progressManager.LoadProgress(context.UserId, context.SubCategory.ToString());
        SyncProgressState();
    }
    else
    {
        ResetProgress();
    }

    BuildStudyItems();
    ValidateIndex();
}

private void ValidateInitializeParameters(LearningContext context)
{
    if (string.IsNullOrWhiteSpace(context.UserId))
        throw new ArgumentException("UserId cannot be null or empty", nameof(context.UserId));
}
```

#### 2.3 属性调整

**当前**：
```csharp
public string CurrentMode => _state.CurrentMode;
public string CurrentSortOrder => _state.CurrentSortOrder;
```

**调整后**：
```csharp
public LearningModeType CurrentMode => _state.CurrentMode;
public SortOrderType CurrentSortOrder => _state.CurrentSortOrder;
public LearningContext CurrentContext => new LearningContext(
    _state.UserId,
    _state.Subject,
    _state.SubCategory,
    _state.WordBankFile,
    _state.CurrentMode,
    _state.CurrentSortOrder
);
```

#### 2.4 `LoadAllItems` 方法调整

**当前**：
```csharp
private void LoadAllItems(string subCategory, string wordBankFile)
{
    _allItems.Clear();
    var items = _contentLoaderService.LoadItems(subCategory, wordBankFile);
    foreach (var item in items)
    {
        if (item is LearningItem learningItem)
        {
            _allItems.Add(learningItem);
        }
    }
}
```

**调整后**：
```csharp
private void LoadAllItems(LearningContext context)
{
    _allItems.Clear();
    var items = _contentLoaderService.LoadItems(context);
    _allItems.AddRange(items);
}
```

---

### 3. `SqliteDataPersistenceService` 实现调整

#### 3.1 `GetKnownItems` 方法调整

**当前**：
```csharp
public List<string> GetKnownItems(string userId, string categoryName)
{
    // ... 基于字符串查询
}
```

**调整后**：
```csharp
public List<string> GetKnownItems(LearningContext context)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(context.UserId, nameof(context.UserId));
    
    try
    {
        using var db = _dbContextFactory.CreateDbContext();
        var categoryName = context.SubCategory.ToString();
        return db.LearningItemStates
            .Where(s => s.UserId == context.UserId && 
                        s.CategoryName == categoryName && 
                        s.IsKnown)
            .Select(s => s.Content)
            .ToList();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Failed to get known items for {UserId}, {Category}", 
            context.UserId, context.SubCategory);
        return new List<string>();
    }
}
```

#### 3.2 `GetUnknownItems` 方法调整

**当前**：
```csharp
public List<string> GetUnknownItems(string userId, string categoryName)
{
    // ... 基于字符串查询
}
```

**调整后**：
```csharp
public List<string> GetUnknownItems(LearningContext context)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(context.UserId, nameof(context.UserId));
    
    try
    {
        using var db = _dbContextFactory.CreateDbContext();
        var categoryName = context.SubCategory.ToString();
        return db.LearningItemStates
            .Where(s => s.UserId == context.UserId && 
                        s.CategoryName == categoryName && 
                        !s.IsKnown)
            .Select(s => s.Content)
            .ToList();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Failed to get unknown items for {UserId}, {Category}", 
            context.UserId, context.SubCategory);
        return new List<string>();
    }
}
```

#### 3.3 `UpsertLearningItemState` 方法调整

**当前**：
```csharp
public void UpsertLearningItemState(string userId, string categoryName, string content, bool isKnown)
{
    // ... 基于字符串参数更新
}
```

**调整后**：
```csharp
public void UpsertLearningItemState(LearningContext context, string content, bool isKnown)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(context.UserId, nameof(context.UserId));
    ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
    
    try
    {
        using var db = _dbContextFactory.CreateDbContext();
        var categoryName = context.SubCategory.ToString();
        
        var existing = db.LearningItemStates.FirstOrDefault(
            s => s.UserId == context.UserId && 
                 s.CategoryName == categoryName && 
                 s.Content == content);
        
        if (existing != null)
        {
            existing.IsKnown = isKnown;
            existing.UpdatedAt = DateTime.Now;
        }
        else
        {
            db.LearningItemStates.Add(new LearningItemState
            {
                UserId = context.UserId,
                CategoryName = categoryName,
                Content = content,
                IsKnown = isKnown,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }
        
        db.SaveChanges();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Failed to upsert learning item state for {UserId}, {Category}", 
            context.UserId, context.SubCategory);
    }
}
```

---

### 4. `WrongAnswerService` 实现调整

#### 4.1 `GetWrongAnswers` 方法调整

**当前**：
```csharp
public List<WrongAnswerItem> GetWrongAnswers(string userId, string subject = "", string category = "")
{
    // ... 基于字符串筛选
}
```

**调整后**：
```csharp
public List<WrongAnswerItem> GetWrongAnswers(string userId, LearningContext? context = null)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
    
    try
    {
        var allItems = LoadAllWrongAnswers(userId);
        
        if (context != null)
        {
            var subjectStr = context.Subject.ToString();
            var categoryStr = context.SubCategory.ToString();
            
            return allItems.Where(item => 
                (string.IsNullOrEmpty(subjectStr) || item.Subject == subjectStr) &&
                (string.IsNullOrEmpty(categoryStr) || item.Category == categoryStr))
                .ToList();
        }
        
        return allItems;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get wrong answers for {UserId}", userId);
        return new List<WrongAnswerItem>();
    }
}
```

#### 4.2 `GetSubjects` 和 `GetCategories` 方法调整

**当前**：
```csharp
public List<string> GetSubjects(string userId)
{
    // ... 返回字符串列表
}

public List<string> GetCategories(string userId, string subject)
{
    // ... 返回字符串列表
}
```

**调整后**：
```csharp
public List<SubjectType> GetSubjects(string userId)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
    
    try
    {
        var allItems = LoadAllWrongAnswers(userId);
        var subjectStrings = allItems.Select(i => i.Subject).Distinct().ToList();
        
        var result = new List<SubjectType>();
        foreach (var subjectStr in subjectStrings)
        {
            if (SubjectSubCategoryMapping.TryParseSubject(subjectStr, out var subject))
            {
                result.Add(subject);
            }
        }
        
        return result.Distinct().ToList();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get subjects for {UserId}", userId);
        return new List<SubjectType>();
    }
}

public List<SubCategoryType> GetCategories(string userId, SubjectType subject)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
    
    try
    {
        var allItems = LoadAllWrongAnswers(userId);
        var subjectStr = subject.ToString();
        
        var categoryStrings = allItems
            .Where(i => i.Subject == subjectStr)
            .Select(i => i.Category)
            .Distinct()
            .ToList();
        
        var result = new List<SubCategoryType>();
        foreach (var categoryStr in categoryStrings)
        {
            if (SubjectSubCategoryMapping.TryParseSubCategory(categoryStr, out var category))
            {
                result.Add(category);
            }
        }
        
        return result.Distinct().ToList();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get categories for {UserId}, {Subject}", userId, subject);
        return new List<SubCategoryType>();
    }
}
```

---

### 5. `LearningForm` 实现调整

#### 5.1 属性调整

**当前**：
```csharp
public string Language => _settings.Language;
public string Subject => _settings.Subject;
public string SubCategory { get; set; }
```

**调整后**：
```csharp
private LearningContext? _currentContext;

public LearningContext Context
{
    get => _currentContext ?? new LearningContext(
        _userSessionService?.CurrentUserId ?? "",
        SubjectType.Chinese,
        SubCategoryType.ChineseCharacter
    );
    set
    {
        _currentContext = value;
        UpdateContextDependentUI();
    }
}

private void UpdateContextDependentUI()
{
    if (_currentContext != null)
    {
        _settings.Language = _currentContext.Subject.ToDisplayString();
        _settings.Subject = _currentContext.Subject.ToDisplayString();
        // ... 更新其他依赖上下文的UI
    }
}
```

#### 5.2 `PlayPronunciationAsync` 方法调整

**当前**：
```csharp
public Task PlayPronunciationAsync(string text, string language)
{
    // ... 使用字符串语言
}
```

**调整后**：
```csharp
public Task PlayPronunciationAsync(string text, string language)
{
    return PlayPronunciationAsync(text, Context.Subject);
}

public Task PlayPronunciationAsync(string text, SubjectType subject)
{
    // ... 使用枚举语言
    var langCode = subject == SubjectType.Chinese ? "zh-CN" : "en-US";
    // ...
}
```

---

### 6. `ContentEditorForm` 实现调整

#### 6.1 属性调整

**当前**：
```csharp
public string SelectedLanguage => _comboBoxLanguage.SelectedItem?.ToString() ?? string.Empty;
public string SelectedSubject => _comboBoxSubject.SelectedItem?.ToString() ?? string.Empty;
public string SelectedSubCategory => _comboBoxSubCategory.SelectedItem?.ToString() ?? string.Empty;
```

**调整后**：
```csharp
private LearningContext? _selectedContext;

public LearningContext SelectedContext
{
    get => _selectedContext ?? new LearningContext(
        _userSessionService?.CurrentUserId ?? "",
        SubjectType.Chinese,
        SubCategoryType.ChineseCharacter
    );
    set
    {
        if (_selectedContext != value)
        {
            var oldSubject = _selectedContext?.Subject;
            var oldSubCategory = _selectedContext?.SubCategory;
            
            _selectedContext = value;
            
            if (oldSubject != value.Subject)
            {
                SubjectChanged?.Invoke(this, new ContextChangedEventArgs(
                    oldSubject, value.Subject, oldSubCategory, value.SubCategory));
            }
            
            if (oldSubCategory != value.SubCategory)
            {
                SubCategoryChanged?.Invoke(this, new ContextChangedEventArgs(
                    oldSubject, value.Subject, oldSubCategory, value.SubCategory));
            }
        }
    }
}
```

#### 6.2 事件调整（保留细粒度）

**当前**：
```csharp
public event EventHandler? LanguageChanged;
public event EventHandler? SubjectChanged;
public event EventHandler? SubCategoryChanged;
```

**调整后**：
```csharp
public event EventHandler<ContextChangedEventArgs>? SubjectChanged;
public event EventHandler<ContextChangedEventArgs>? SubCategoryChanged;
```

#### 6.3 初始化方法调整

**当前**：
```csharp
public void SetInitialLanguage(string language)
{
    _comboBoxLanguage.SelectedItem = language;
}

public void SetInitialSubject(string subject)
{
    _comboBoxSubject.SelectedItem = subject;
}

public void SetInitialSubCategory(string subCategory)
{
    _comboBoxSubCategory.SelectedItem = subCategory;
}
```

**调整后**：
```csharp
public void SetInitialContext(LearningContext context)
{
    _selectedContext = context;
    
    _comboBoxSubject.SelectedItem = context.Subject.ToDisplayString();
    _comboBoxSubCategory.SelectedItem = context.SubCategory.ToDisplayString();
    
    RefreshSubCategories(SubjectSubCategoryMapping.GetSubCategories(context.Subject));
}
```

---

### 7. `MainForm` Dashboard 调整

#### 7.1 `UpdateDashboardStats` 方法调整

**当前**：
```csharp
public void UpdateDashboardStats(int todayStudyMinutes, int streakDays, int totalXP,
    int currentLevel, int xpToNextLevel, int completedChallenges, int totalChallenges,
    int noteCount = 0, int todayNewNotes = 0)
{
    // ... 更新游戏化UI
    _labelXP.Text = $"XP: {totalXP}";
    _labelLevel.Text = $"Level: {currentLevel}";
    // ...
}
```

**调整后**：
```csharp
public void UpdateDashboardStats(int todayStudyMinutes, int streakDays, 
    int knownItemsCount = 0, int totalItemsCount = 0)
{
    _labelTodayStudy.Text = $"今日学习: {todayStudyMinutes}分钟";
    _labelStreak.Text = $"连续学习: {streakDays}天";
    _labelKnownItems.Text = $"已掌握: {knownItemsCount}/{totalItemsCount}";
    
    // 移除游戏化相关UI更新代码
}
```

---

### 8. `SessionData` 实现调整

#### 8.1 属性调整

**当前**：
```csharp
public string Language { get; set; } = string.Empty;
public string SubCategory { get; set; } = string.Empty;
```

**调整后**：
```csharp
public SubjectType LastSubject { get; set; }
public SubCategoryType LastSubCategory { get; set; }
```

#### 8.2 数据迁移逻辑

在 `LoadSession()` 方法中添加迁移逻辑：

```csharp
public SessionData LoadSession()
{
    var session = _persistenceService.LoadJsonFile<SessionData>(_sessionFilePath);
    
    if (session == null)
    {
        session = new SessionData();
    }
    else
    {
        MigrateLegacySessionData(session);
    }
    
    return session;
}

private void MigrateLegacySessionData(SessionData session)
{
    if (!string.IsNullOrEmpty(session.Language) && session.LastSubject == 0)
    {
        SubjectSubCategoryMapping.TryParseSubject(session.Language, out session.LastSubject);
    }
    
    if (!string.IsNullOrEmpty(session.SubCategory) && session.LastSubCategory == 0)
    {
        SubjectSubCategoryMapping.TryParseSubCategory(session.SubCategory, out session.LastSubCategory);
    }
}
```

---

### 9. `LearningConfig` 实现调整

#### 9.1 属性调整

**当前**：
```csharp
public string Language { get; set; } = string.Empty;
public string SubCategory { get; set; } = string.Empty;
public string Mode { get; set; } = string.Empty;
public string SortOrder { get; set; } = string.Empty;
```

**调整后**：
```csharp
public SubjectType Subject { get; set; }
public SubCategoryType SubCategory { get; set; }
public LearningModeType Mode { get; set; } = LearningModeType.Study;
public SortOrderType SortOrder { get; set; } = SortOrderType.Sequential;
```

#### 9.2 转换方法

添加与旧格式的转换方法：

```csharp
public static LearningConfig FromLegacy(string language, string subCategory, 
    string mode, string sortOrder)
{
    SubjectSubCategoryMapping.TryParseSubject(language, out var subject);
    SubjectSubCategoryMapping.TryParseSubCategory(subCategory, out var category);
    
    Enum.TryParse<LearningModeType>(mode, true, out var modeType);
    Enum.TryParse<SortOrderType>(sortOrder, true, out var sortOrderType);
    
    return new LearningConfig
    {
        Subject = subject,
        SubCategory = category,
        Mode = modeType != 0 ? modeType : LearningModeType.Study,
        SortOrder = sortOrderType != 0 ? sortOrderType : SortOrderType.Sequential
    };
}
```

---

## 调用链更新示例

### 场景：用户开始学习

**调整前**：
```csharp
// LearningFlowHandler.cs
_studyEngine.Initialize(
    userId: "user001",
    language: "Chinese",
    subCategory: "ChineseCharacter",
    wordBankFile: "",
    mode: "Study",
    sortOrder: "Sequential"
);
```

**调整后**：
```csharp
// LearningFlowHandler.cs
var context = new LearningContext(
    UserId: "user001",
    Subject: SubjectType.Chinese,
    SubCategory: SubCategoryType.ChineseCharacter
);
_studyEngine.Initialize(context);
```

### 场景：加载学习内容

**调整前**：
```csharp
// StudyEngine.cs
var items = _contentLoaderService.LoadItems(subCategory, wordBankFile);
```

**调整后**：
```csharp
// StudyEngine.cs
var items = _contentLoaderService.LoadItems(context);
```

### 场景：获取学习进度

**调整前**：
```csharp
// LearningForm.cs
var progress = _studyEngine.GetProgressSummary(userId, language, subCategory);
```

**调整后**：
```csharp
// LearningForm.cs
var progress = _studyEngine.GetProgressSummary(_currentContext);
```

---

## 接口-实现对照总览

| 接口 | 实现类 | 关键调整 |
|-----|-------|---------|
| `IContentLoaderService` | `ContentLoaderService` | `LoadItems/SaveItems` 参数改为 `LearningContext`，`GetSubCategories` 返回 `List<SubCategoryType>` |
| `IDataPersistenceService` | `SqliteDataPersistenceService` | 所有学习项状态操作方法参数改为 `LearningContext` |
| `IStudyEngine` | `StudyEngine` | `Initialize` 参数改为 `LearningContext`，新增 `CurrentContext` 属性 |
| `IWrongAnswerService` | `WrongAnswerService` | `GetWrongAnswers/GetSubjects/GetCategories` 返回强类型 |
| `ILearningView` | `LearningForm` | 用 `Context` 属性替代 `Language/Subject/SubCategory` |
| `IContentEditorView` | `ContentEditorForm` | 用 `SelectedContext` 属性替代三个独立属性 |
| `IMainView` | `MainForm` | `UpdateDashboardStats` 移除游戏化参数 |
| `ILearningFlowHandler` | `LearningFlowHandler` | `InitializeAsync` 参数改为 `LearningContext` |

---

## 10. Presenter/Handler 层调整方案

### 10.1 `LearningFlowHandler` 调整

#### 10.1.1 接口调整

**当前**：
```csharp
public interface ILearningFlowHandler
{
    Task InitializeAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode = true);
    // ...
}
```

**调整后**：
```csharp
public interface ILearningFlowHandler
{
    Task InitializeAsync(LearningContext context, bool continueMode = true);
    // ...
}
```

#### 10.1.2 字段调整

**当前**：
```csharp
private string _currentUserId = "";
private string _currentSubject = "";
private string _currentSubCategory = "";
```

**调整后**：
```csharp
private LearningContext? _currentContext;
```

#### 10.1.3 `InitializeAsync` 方法调整

**当前**：
```csharp
public async Task InitializeAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode = true)
{
    _currentUserId = userId;
    _currentSubject = language;
    _currentSubCategory = subCategory;
    
    _studyEngine.Initialize(userId, language, subCategory, wordBankFile, 
        _settingsManager.GetLearningMode(), _settingsManager.GetSortOrder(), continueMode);
    // ...
}
```

**调整后**：
```csharp
public async Task InitializeAsync(LearningContext context, bool continueMode = true)
{
    _currentContext = context;
    
    _studyEngine.Initialize(context, continueMode);
    // ...
}
```

#### 10.1.4 其他方法调整

**当前**：
```csharp
private void OnItemWrong(LearningItem item)
{
    var evt = new ItemWrongEvent
    {
        UserId = _currentUserId,
        ItemContent = item.Content,
        CorrectAnswer = item.Meaning,
        UserAnswer = "",
        SubCategory = _currentSubCategory
    };
    _eventBus?.Publish(evt);
}
```

**调整后**：
```csharp
private void OnItemWrong(LearningItem item)
{
    if (_currentContext == null) return;
    
    var evt = new ItemWrongEvent
    {
        UserId = _currentContext.UserId,
        ItemContent = item.Content,
        CorrectAnswer = item.Meaning,
        UserAnswer = "",
        SubCategory = _currentContext.SubCategory.ToString()
    };
    _eventBus?.Publish(evt);
}
```

---

### 10.2 `ContentEditorPresenter` 调整

#### 10.2.1 字段调整

**当前**：
```csharp
private static readonly Dictionary<string, string> CategoryTypeNames = new()
{
    { Constants.SubCategory.ChineseCharacter, "识字" },
    // ...
};
```

**调整后**：
```csharp
private static readonly Dictionary<SubCategoryType, string> CategoryTypeNames = new()
{
    { SubCategoryType.ChineseCharacter, "识字" },
    { SubCategoryType.ChineseIdiom, "成语" },
    { SubCategoryType.ChinesePhrase, "短语" },
    { SubCategoryType.ChinesePoem, "诗词" },
    { SubCategoryType.ChineseComprehensive, "语文综合" },
    { SubCategoryType.EnglishWord, "英语单词" },
    { SubCategoryType.EnglishPhrase, "英语短语" },
    { SubCategoryType.EnglishSentence, "英语句子" },
    { SubCategoryType.EnglishComprehensive, "英语综合" }
};
```

#### 10.2.2 加载内容方法调整

**当前**：
```csharp
private void LoadItems()
{
    var subCategory = _view.SelectedSubCategory;
    var items = _contentLoaderService.LoadItems(subCategory);
    // ...
}
```

**调整后**：
```csharp
private void LoadItems()
{
    var context = _view.SelectedContext;
    var items = _contentLoaderService.LoadItems(context);
    // ...
}
```

#### 10.2.3 保存内容方法调整

**当前**：
```csharp
private void SaveItems()
{
    var subCategory = _view.SelectedSubCategory;
    var items = // ... 从网格获取
    _contentLoaderService.SaveItems(subCategory, items);
}
```

**调整后**：
```csharp
private void SaveItems()
{
    var context = _view.SelectedContext;
    var items = // ... 从网格获取
    _contentLoaderService.SaveItems(context, items);
}
```

---

### 10.3 `MainPresenter` 调整

#### 10.3.1 打开学习窗口方法调整

**当前**：
```csharp
public void OpenLearningWindow(string language, string subCategory, string wordBankFile = "")
{
    var userId = _userSessionService.CurrentUserId;
    
    var learningForm = _windowManager.CreateLearningWindow();
    learningForm.Show();
    
    var flowHandler = _serviceProvider.GetRequiredService<ILearningFlowHandler>();
    flowHandler.InitializeAsync(userId, language, subCategory, wordBankFile);
}
```

**调整后**：
```csharp
public void OpenLearningWindow(LearningContext context)
{
    var learningForm = _windowManager.CreateLearningWindow();
    learningForm.Show();
    
    var flowHandler = _serviceProvider.GetRequiredService<ILearningFlowHandler>();
    flowHandler.InitializeAsync(context);
}
```

#### 10.3.2 更新Dashboard方法调整

**当前**：
```csharp
private void UpdateDashboard()
{
    var userId = _userSessionService.CurrentUserId;
    var stats = _learningAnalyticsService.GetStats(userId);
    
    _view.UpdateDashboardStats(
        stats.TodayStudyMinutes,
        stats.StreakDays,
        stats.TotalXP,
        stats.CurrentLevel,
        stats.XpToNextLevel,
        stats.CompletedChallenges,
        stats.TotalChallenges,
        stats.NoteCount,
        stats.TodayNewNotes
    );
}
```

**调整后**：
```csharp
private void UpdateDashboard()
{
    var userId = _userSessionService.CurrentUserId;
    var stats = _learningAnalyticsService.GetStats(userId);
    
    _view.UpdateDashboardStats(
        stats.TodayStudyMinutes,
        stats.StreakDays,
        stats.KnownItemsCount,
        stats.TotalItemsCount
    );
}
```

---

## 11. JSON 序列化配置

### 11.1 枚举序列化问题

**问题**：`System.Text.Json` 默认将枚举序列化为整数，这会破坏 `SessionData` 和 `LearningConfig` 的 JSON 持久化兼容性。

**解决方案**：添加 `JsonStringEnumConverter` 配置。

#### 11.1.1 全局配置（推荐）

在 `Program.cs` 或 DI 配置中添加：

```csharp
using System.Text.Json.Serialization;

// 在服务注册中配置 JSON 序列化选项
services.AddSingleton<JsonSerializerOptions>(new JsonSerializerOptions
{
    Converters = { new JsonStringEnumConverter() },
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
});
```

#### 11.1.2 属性级配置

在需要序列化的枚举属性上添加特性：

```csharp
public record LearningContext(
    string UserId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SubjectType Subject,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SubCategoryType SubCategory,
    string WordBankFile = "",
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    LearningModeType Mode = LearningModeType.Study,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SortOrderType SortOrder = SortOrderType.Sequential
);
```

#### 11.1.3 SessionData 和 LearningConfig

```csharp
// SessionData
public class SessionData
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubjectType LastSubject { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubCategoryType LastSubCategory { get; set; }
    // ... 其他属性
}

// LearningConfig
public class LearningConfig
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubjectType Subject { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubCategoryType SubCategory { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LearningModeType Mode { get; set; } = LearningModeType.Study;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SortOrderType SortOrder { get; set; } = SortOrderType.Sequential;
}
```

---

### 12. 遗漏方法调整

#### 12.1 `IStudyEngine.AddUnknownItem`

**当前**：
```csharp
void AddUnknownItem(string content, string subCategory);
```

**调整后**：
```csharp
void AddUnknownItem(string content, SubCategoryType subCategory);
```

**实现调整**：
```csharp
public void AddUnknownItem(string content, SubCategoryType subCategory)
{
    if (string.IsNullOrWhiteSpace(content))
        throw new ArgumentException("Content cannot be null or empty", nameof(content));
    
    lock (_stateLock)
    {
        if (!_state.UnknownItems.Contains(content))
        {
            _state.UnknownItems.Add(content);
        }
    }
    
    _persistenceService.UpsertLearningItemState(
        new LearningContext(_state.UserId, _state.Subject, subCategory),
        content,
        false
    );
}
```

#### 12.2 `ContentLoaderService.GetFilePath`

**当前**：
```csharp
private string GetFilePath(string subCategory, string wordBankFile)
{
    // ... 使用字符串 subCategory
}
```

**调整后**：
```csharp
private string GetFilePath(SubCategoryType subCategory, string wordBankFile)
{
    if (!string.IsNullOrEmpty(wordBankFile) && File.Exists(wordBankFile))
    {
        return wordBankFile;
    }
    
    if (_categoryFileMap.TryGetValue(subCategory, out var fileName))
    {
        return Path.Combine(AppPaths.WordBankDir, fileName);
    }
    
    return Path.Combine(AppPaths.WordBankDir, $"{subCategory}.json");
}
```

---

## 13. 数据迁移完整方案

### 13.1 `SessionData` 迁移

**当前格式**：
```json
{
  "Language": "Chinese",
  "SubCategory": "ChineseCharacter"
}
```

**目标格式**：
```json
{
  "LastSubject": "Chinese",
  "LastSubCategory": "ChineseCharacter"
}
```

**迁移逻辑**（在 `LoadSession()` 中）：

```csharp
public SessionData LoadSession()
{
    var session = _persistenceService.LoadJsonFile<SessionData>(_sessionFilePath);
    
    if (session == null)
    {
        session = new SessionData();
    }
    else
    {
        MigrateLegacySessionData(session);
    }
    
    return session;
}

private void MigrateLegacySessionData(SessionData session)
{
    // 迁移旧的 Language 字段到 LastSubject
    if (!string.IsNullOrEmpty(session.Language) && session.LastSubject == 0)
    {
        if (SubjectSubCategoryMapping.TryParseSubject(session.Language, out var subject))
        {
            session.LastSubject = subject;
        }
    }
    
    // 迁移旧的 SubCategory 字段到 LastSubCategory
    if (!string.IsNullOrEmpty(session.SubCategory) && session.LastSubCategory == 0)
    {
        if (SubjectSubCategoryMapping.TryParseSubCategory(session.SubCategory, out var category))
        {
            session.LastSubCategory = category;
        }
    }
}
```

### 13.2 `LearningConfig` 迁移

**当前格式**：
```json
{
  "Language": "Chinese",
  "SubCategory": "ChineseCharacter",
  "Mode": "Study",
  "SortOrder": "Sequential"
}
```

**目标格式**：
```json
{
  "Subject": "Chinese",
  "SubCategory": "ChineseCharacter",
  "Mode": "Study",
  "SortOrder": "Sequential"
}
```

**解决方案**：使用自定义 `JsonConverter` 处理双向兼容，避免字段冲突

**自定义 JsonConverter**：

```csharp
public class LearningConfigConverter : JsonConverter<LearningConfig>
{
    public override LearningConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var config = new LearningConfig();
        bool hasLanguage = false;
        bool hasSubject = false;
        
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }
            
            var propertyName = reader.GetString();
            reader.Read();
            
            switch (propertyName?.ToLowerInvariant())
            {
                case "language":
                    hasLanguage = true;
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var languageStr = reader.GetString();
                        if (SubjectSubCategoryMapping.TryParseSubject(languageStr, out var subject))
                        {
                            config.Subject = subject;
                        }
                    }
                    break;
                    
                case "subject":
                    hasSubject = true;
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var subjectStr = reader.GetString();
                        if (SubjectSubCategoryMapping.TryParseSubject(subjectStr, out var subject))
                        {
                            config.Subject = subject;
                        }
                    }
                    break;
                    
                case "subcategory":
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var categoryStr = reader.GetString();
                        if (SubjectSubCategoryMapping.TryParseSubCategory(categoryStr, out var category))
                        {
                            config.SubCategory = category;
                        }
                    }
                    break;
                    
                case "mode":
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var modeStr = reader.GetString();
                        if (Enum.TryParse<LearningModeType>(modeStr, true, out var mode))
                        {
                            config.Mode = mode;
                        }
                    }
                    break;
                    
                case "sortorder":
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var sortOrderStr = reader.GetString();
                        if (Enum.TryParse<SortOrderType>(sortOrderStr, true, out var sortOrder))
                        {
                            config.SortOrder = sortOrder;
                        }
                    }
                    break;
            }
        }
        
        return config;
    }
    
    public override void Write(Utf8JsonWriter writer, LearningConfig value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Subject", value.Subject.ToString());
        writer.WriteString("SubCategory", value.SubCategory.ToString());
        writer.WriteString("Mode", value.Mode.ToString());
        writer.WriteString("SortOrder", value.SortOrder.ToString());
        writer.WriteEndObject();
    }
}
```

**LearningConfig 类**：

```csharp
[JsonConverter(typeof(LearningConfigConverter))]
public class LearningConfig
{
    public SubjectType Subject { get; set; }
    
    public SubCategoryType SubCategory { get; set; }
    
    public LearningModeType Mode { get; set; } = LearningModeType.Study;
    
    public SortOrderType SortOrder { get; set; } = SortOrderType.Sequential;
}
```

---

## 14. 实施阶段拆分

### Phase A：类型安全重构（核心）

**目标**：消除字符串魔法值，引入强类型枚举和统一上下文对象

**包含内容**：
- 创建 `LearningContext` 记录类型和 `LearningContextFactory`
- 所有接口参数 string → enum
- 所有实现类方法签名更新
- Presenter/Handler 层适配
- SessionData/LearningConfig 数据迁移
- JsonStringEnumConverter 配置

**风险**：中等（编译错误会暴露所有遗漏点）

**验证**：编译通过 + 核心学习流程测试

### Phase B：游戏化移除（独立）

**目标**：移除非核心的游戏化功能

**包含内容**：
- `IMainView.UpdateDashboardStats` 移除游戏化参数
- `MainForm` 移除游戏化 UI 组件和更新逻辑
- `LearningAnalyticsService` 移除游戏化统计字段
- 清理相关游戏化服务引用

**风险**：低（不影响核心学习逻辑）

**验证**：Dashboard 显示正确 + 核心学习流程不受影响

**拆分原因**：
1. 两个任务正交，互不依赖
2. 类型安全重构影响范围广，需要充分测试
3. 游戏化移除风险低，可单独快速完成
4. 分离后更容易定位回归问题

---

## 15. JsonSerializer 使用审计

### 15.1 审计范围

需要确保所有使用 `JsonSerializer` 的地方都能获取到 `JsonStringEnumConverter` 配置。

### 15.2 审计清单

| 文件路径 | 使用方式 | 需要修改 |
|---------|---------|---------|
| `Common/JsonHelper.cs` | `JsonSerializer.Serialize/Deserialize` | 是 - 使用全局配置 |
| `Services/Persistence/SqliteDataPersistenceService.cs` | `LoadJsonFile/SaveJsonFile` | 是 - 使用全局配置 |
| `Services/Learning/ContentLoaderService.cs` | `JsonHelper.DeserializeLearningItems` | 间接 - 依赖 JsonHelper |
| `Services/Learning/DataImportService.cs` | JSON 导入导出 | 是 - 使用全局配置 |
| `Services/Learning/LearningDataExportService.cs` | JSON 导出 | 是 - 使用全局配置 |

### 15.3 统一配置方案

**修改 `JsonHelper.cs`**：

```csharp
public static class JsonHelper
{
    private static readonly JsonSerializerOptions _defaultOptions = new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        IgnoreNullValues = true
    };

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _defaultOptions);
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _defaultOptions);
    }

    public static void SaveToFile<T>(string filePath, T data)
    {
        var json = Serialize(data);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    public static T? LoadFromFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
            return default;
        
        var json = File.ReadAllText(filePath, Encoding.UTF8);
        return Deserialize<T>(json);
    }

    public static List<LearningItem> DeserializeLearningItems(string json)
    {
        return Deserialize<List<LearningItem>>(json) ?? new List<LearningItem>();
    }
}
```

**修改 `SqliteDataPersistenceService.cs`**：

```csharp
public T? LoadJsonFile<T>(string filePath)
{
    return JsonHelper.LoadFromFile<T>(filePath);
}

public void SaveJsonFile<T>(string filePath, T data)
{
    JsonHelper.SaveToFile(filePath, data);
}
```

---

## 16. 实施检查清单

### Phase A - 类型安全重构

#### A1：基础类型定义

- [ ] 创建 `LearningContext` 记录类型
- [ ] 创建 `LearningContextFactory` 静态类
- [ ] 创建 `ContextChangedEventArgs` 事件参数类
- [ ] 更新 `JsonHelper.cs` 添加统一 JSON 序列化配置
- [ ] 更新 `EnumExtensions` 确保显示字符串正确

#### A2：接口调整

- [ ] `IContentLoaderService` - 更新方法签名
- [ ] `IDataPersistenceService` - 更新方法签名
- [ ] `IStudyEngine` - 更新方法签名，新增 `CurrentContext` 属性
- [ ] `IWrongAnswerService` - 更新方法签名
- [ ] `IDataImportService` - 更新方法签名
- [ ] `ILearningView` - 添加 `Context` 属性
- [ ] `IContentEditorView` - 添加 `SelectedContext` 属性和细粒度事件
- [ ] `ILearningFlowHandler` - 更新 `InitializeAsync` 签名

#### A3：实现类调整

- [ ] `ContentLoaderService` - 更新所有方法
- [ ] `SqliteDataPersistenceService` - 更新所有方法
- [ ] `StudyEngine` - 更新所有方法
- [ ] `WrongAnswerService` - 更新所有方法
- [ ] `DataImportService` - 更新所有方法
- [ ] `LearningForm` - 更新属性和方法
- [ ] `ContentEditorForm` - 更新属性和方法
- [ ] `LearningFlowHandler` - 更新所有方法
- [ ] `ContentEditorPresenter` - 更新所有方法
- [ ] `MainPresenter` - 更新相关方法

#### A4：数据迁移

- [ ] `SessionData` - 添加迁移逻辑
- [ ] `LearningConfig` - 添加自定义 `LearningConfigConverter`
- [ ] 验证旧数据能够正确迁移

#### A5：编译与测试

- [ ] 编译项目，确保无错误
- [ ] 运行单元测试
- [ ] 手动测试核心学习流程
- [ ] 验证数据持久化和迁移正确性

---

### Phase B - 游戏化移除

#### B1：接口调整

- [ ] `IMainView` - 更新 `UpdateDashboardStats` 签名

#### B2：实现类调整

- [ ] `MainForm` - 移除游戏化 UI 和更新逻辑
- [ ] `LearningAnalyticsService` - 移除游戏化统计字段

#### B3：编译与测试

- [ ] 编译项目，确保无错误
- [ ] 验证 Dashboard 显示正确
- [ ] 验证核心学习流程不受影响

---

## 17. DI 策略

### 17.1 LearningContext 注入方式

由于 `LearningContext` 是运行时上下文（每次学习会话可能不同），不适合作为 Singleton 注入。推荐以下两种方式：

#### 方式 A：Per-Call 传递（推荐）

**适用场景**：方法级别的上下文需求

```csharp
public class StudyEngine : IStudyEngine
{
    public void Initialize(LearningContext context, bool continueMode = true)
    {
        // 使用 context 参数
    }
    
    public string GetProgressSummary(LearningContext context)
    {
        // 使用 context 参数
    }
}
```

**优势**：
- 明确的依赖关系
- 线程安全（无共享状态）
- 易于测试（可注入任意上下文）

#### 方式 B：Scoped Context Holder

**适用场景**：需要在多个服务间共享同一上下文的场景

```csharp
public interface ILearningContextHolder
{
    LearningContext? CurrentContext { get; set; }
}

public class LearningContextHolder : ILearningContextHolder
{
    public LearningContext? CurrentContext { get; set; }
}

// 注册
services.AddScoped<ILearningContextHolder, LearningContextHolder>();

// 使用
public class StudyEngine : IStudyEngine
{
    private readonly ILearningContextHolder _contextHolder;
    
    public StudyEngine(ILearningContextHolder contextHolder)
    {
        _contextHolder = contextHolder;
    }
    
    public void Initialize(LearningContext context, bool continueMode = true)
    {
        _contextHolder.CurrentContext = context;
        // ...
    }
    
    public string GetProgressSummary()
    {
        var context = _contextHolder.CurrentContext 
            ?? throw new InvalidOperationException("Engine not initialized");
        // ...
    }
}
```

**优势**：
- 减少参数传递
- 适合长生命周期的学习会话

**注意事项**：
- 需要线程安全考虑（同一 Scope 内可能有并发访问）
- 需要明确的初始化顺序

#### 推荐策略

| 服务 | 推荐方式 | 原因 |
|-----|---------|------|
| `IStudyEngine` | Per-Call | 核心服务，需要明确的上下文边界 |
| `IContentLoaderService` | Per-Call | 纯函数式服务，无状态 |
| `IDataPersistenceService` | Per-Call | 持久化操作需要明确的用户/类别上下文 |
| `IWrongAnswerService` | Per-Call | 查询需要明确的筛选条件 |
| `ILearningFlowHandler` | Scoped | 管理整个学习流程，需要共享上下文 |

---

## 18. 测试影响

### 18.1 单元测试影响

所有方法签名变更都会影响现有单元测试：

| 影响类型 | 范围 | 处理方式 |
|---------|------|---------|
| 方法参数变更 | 所有接口实现类 | 更新测试方法调用 |
| 返回类型变更 | `GetSubCategories`、`GetSubjects`、`GetCategories` | 更新断言类型 |
| 新增类型依赖 | `LearningContext`、`ContextChangedEventArgs` | 在测试中构造新类型 |
| JSON 序列化变更 | `SessionData`、`LearningConfig` | 更新序列化测试 |

### 18.2 测试调整流程

#### 步骤 1：审计现有测试

```bash
# 查找所有涉及学习上下文参数的测试
grep -r "GetKnownItems\|GetProgressSummary\|Initialize(" Tests/
```

#### 步骤 2：创建测试辅助方法

```csharp
public static class TestHelper
{
    public static LearningContext CreateTestContext(
        string userId = "test-user",
        SubjectType subject = SubjectType.Chinese,
        SubCategoryType subCategory = SubCategoryType.ChineseCharacter)
    {
        return new LearningContext(userId, subject, subCategory);
    }
    
    public static LearningContext CreateEnglishContext(string userId = "test-user")
    {
        return new LearningContext(userId, SubjectType.English, SubCategoryType.EnglishWord);
    }
}
```

#### 步骤 3：更新测试用例

**调整前**：
```csharp
[Test]
public void GetProgressSummary_ReturnsSummary()
{
    var result = _studyEngine.GetProgressSummary("user001", "Chinese", "ChineseCharacter");
    Assert.IsNotNull(result);
}
```

**调整后**：
```csharp
[Test]
public void GetProgressSummary_ReturnsSummary()
{
    var context = TestHelper.CreateTestContext("user001");
    var result = _studyEngine.GetProgressSummary(context);
    Assert.IsNotNull(result);
}
```

### 18.3 测试检查清单

- [ ] 审计所有单元测试文件，识别受影响的测试用例
- [ ] 创建测试辅助方法（`TestHelper`）
- [ ] 更新所有接口方法调用的测试用例
- [ ] 更新所有返回类型断言的测试用例
- [ ] 更新 JSON 序列化相关的测试用例
- [ ] 添加 `LearningContext` 构造和使用的测试用例
- [ ] 添加数据迁移兼容性测试用例
- [ ] 运行完整测试套件，确保无失败