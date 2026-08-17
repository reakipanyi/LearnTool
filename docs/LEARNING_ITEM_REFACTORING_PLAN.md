# 学习项模型重构方案

## 一、当前状态分析

### 1.1 现有模型结构

```
LearningItem (抽象基类)
    ├── ChineseCharacter
    ├── ChinesePhrase
    ├── ChineseIdiom
    ├── ChinesePoem
    ├── ChineseComprehensive
    ├── EnglishWord
    ├── EnglishPhrase
    ├── EnglishSentence
    ├── EnglishComprehensive
    ├── GrammarRule
    └── GeneralSubjectItem
```

### 1.2 问题清单

| 问题 | 严重程度 | 影响范围 |
|------|---------|---------|
| 贫血模型：学习项仅包含数据，无业务行为 | 高 | 所有学习相关Service |
| 属性冗余：各子类重复定义Meaning、Example等属性 | 中 | 所有学习项子类 |
| 方法重复：GetDisplayText/GetDisplayStruct逻辑分散 | 中 | 所有学习项子类 |
| 枚举与类重复：SubCategoryType有9个值，对应9个具体类 | 中 | ContentLoaderService, LearningItemJsonConverter |
| 关系无映射：SubjectType和SubCategoryType无关联验证 | 中 | 所有使用地方 |
| 通用类脱节：GeneralSubjectItem使用字符串存储科目 | 低 | 学科学习功能 |
| JSON序列化复杂：LearningItemJsonConverter维护成本高 | 中 | 数据持久化、导入导出 |

### 1.3 关联文件清单（40个文件）

**核心模型文件：**
- `Models/Learning/LearningItem.cs` - 基类
- `Models/Learning/ChineseCharacter.cs` - 汉字
- `Models/Learning/ChinesePhrase.cs` - 中文短语
- `Models/Learning/ChineseIdiom.cs` - 成语
- `Models/Learning/ChinesePoem.cs` - 诗词
- `Models/Learning/ChineseComprehensive.cs` - 语文综合
- `Models/Learning/EnglishWord.cs` - 英语单词
- `Models/Learning/EnglishPhrase.cs` - 英语短语
- `Models/Learning/EnglishSentence.cs` - 英语句子
- `Models/Learning/EnglishComprehensive.cs` - 英语综合
- `Models/Learning/GeneralSubjectItem.cs` - 通用学科
- `Models/Learning/GrammarRule.cs` - 语法规则

**序列化与转换：**
- `Common/LearningItemJsonConverter.cs` - JSON转换器（核心）
- `Common/Enums.cs` - 枚举定义
- `Common/Constants.cs` - 常量定义

**服务层：**
- `Services/Learning/ContentLoaderService.cs` - 内容加载
- `Services/Learning/StudyEngine.cs` - 学习引擎
- `Services/Learning/ProgressManager.cs` - 进度管理
- `Services/Learning/DataImportService.cs` - 数据导入
- `Services/Learning/LearningDataExportService.cs` - 数据导出
- `Services/Persistence/SqliteDataPersistenceService.cs` - 数据持久化

**表现层：**
- `Presenters/LearningFlowHandler.cs` - 学习流程处理
- `Presenters/ContentEditorPresenter.cs` - 内容编辑
- `Forms/LearningForm.cs` - 学习表单
- `Forms/ContentEditorForm.cs` - 内容编辑表单

---

## 二、重构方案

### 2.1 目标模型结构

```
LearningItem (统一模型，不再抽象)
    ├── 属性：
        ├── 基础属性：Id, CreatedAt, UpdatedAt
        ├── 分类属性：Subject (SubjectType), SubCategory (SubCategoryType)
        ├── 核心内容：MainContent, Meaning, Example
        ├── 发音属性：Pronunciation, UkPhonetic, UsPhonetic
        ├── 汉字属性：StrokeCount, Radical, Structure
        ├── 英语属性：PartOfSpeech, WordForms
        └── 扩展属性：ExtendedProperties (JSON)
    └── 行为：
        ├── Review(bool isCorrect)
        ├── MarkAsKnown()
        ├── MarkAsUnknown()
        ├── UpdateContent()
        └── UpdateMeaning()

LearningUnit (聚合根，可选)
    ├── LearningItem Item
    ├── LearningProgress Progress
    └── List<LearningRecord> Records

值对象：
    ├── Pronunciation
    ├── Meaning
    ├── Example
    ├── CharacterFeatures
    └── LearningProgress
```

### 2.2 新增文件

| 文件路径 | 说明 |
|---------|------|
| `Models/Learning/ValueObjects/Pronunciation.cs` | 发音值对象 |
| `Models/Learning/ValueObjects/Meaning.cs` | 释义值对象 |
| `Models/Learning/ValueObjects/Example.cs` | 例句值对象 |
| `Models/Learning/ValueObjects/CharacterFeatures.cs` | 汉字特征值对象 |
| `Models/Learning/ValueObjects/LearningProgress.cs` | 学习进度值对象 |
| `Models/Learning/ValueObjects/WordFeatures.cs` | 单词特征值对象 |
| `Models/Learning/Status/LearningStatus.cs` | 学习状态模式 |
| `Models/Learning/LearningUnit.cs` | 学习单元聚合根 |
| `Models/Learning/LearningRecord.cs` | 学习记录 |
| `Common/SubjectSubCategoryMapping.cs` | 科目-子类别映射 |
| `Services/Learning/LearningItemFormatter.cs` | 学习项显示格式化器 |
| `Services/Learning/SubCategoryPropertyConfigService.cs` | 属性配置服务 |
| `Resources/SubCategoryProperties.json` | 属性配置文件 |

### 2.3 删除文件

| 文件路径 | 说明 |
|---------|------|
| `Models/Learning/ChineseCharacter.cs` | 合并到统一模型 |
| `Models/Learning/ChinesePhrase.cs` | 合并到统一模型 |
| `Models/Learning/ChineseIdiom.cs` | 合并到统一模型 |
| `Models/Learning/ChinesePoem.cs` | 合并到统一模型 |
| `Models/Learning/ChineseComprehensive.cs` | 合并到统一模型 |
| `Models/Learning/EnglishWord.cs` | 合并到统一模型 |
| `Models/Learning/EnglishPhrase.cs` | 合并到统一模型 |
| `Models/Learning/EnglishSentence.cs` | 合并到统一模型 |
| `Models/Learning/EnglishComprehensive.cs` | 合并到统一模型 |

### 2.4 修改文件

| 文件路径 | 修改内容 |
|---------|---------|
| `Models/Learning/LearningItem.cs` | 从抽象类改为具体类，添加分类属性和领域行为 |
| `Models/Learning/GeneralSubjectItem.cs` | 合并到统一模型或保留作为特例 |
| `Models/Learning/GrammarRule.cs` | 评估是否保留 |
| `Common/LearningItemJsonConverter.cs` | 简化为单一类型序列化 |
| `Common/Enums.cs` | 添加科目-子类别映射扩展方法 |
| `Common/Constants.cs` | 更新子类别常量 |
| `Services/Learning/ContentLoaderService.cs` | 使用统一模型加载数据 |
| `Services/Learning/StudyEngine.cs` | 适配新模型 |
| `Services/Learning/ProgressManager.cs` | 适配新模型 |
| `Services/Learning/DataImportService.cs` | 适配新模型 |
| `Services/Learning/LearningDataExportService.cs` | 适配新模型 |
| `Services/Persistence/SqliteDataPersistenceService.cs` | 适配新模型 |
| `Presenters/LearningFlowHandler.cs` | 适配新模型 |
| `Presenters/ContentEditorPresenter.cs` | 适配新模型 |
| `Forms/LearningForm.cs` | 适配新模型 |
| `Forms/ContentEditorForm.cs` | 适配新模型 |

---

## 三、详细实现

### 3.1 新增：值对象基类

**文件：** `Models/Learning/ValueObjects/ValueObject.cs`

```csharp
namespace LearningAssistant.Models.Learning.ValueObjects
{
    public abstract class ValueObject
    {
        protected static bool EqualOperator(ValueObject left, ValueObject right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
                return false;
            return ReferenceEquals(left, null) || left.Equals(right);
        }

        protected static bool NotEqualOperator(ValueObject left, ValueObject right)
        {
            return !EqualOperator(left, right);
        }

        protected abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            var other = (ValueObject)obj;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x != null ? x.GetHashCode() : 0)
                .Aggregate((x, y) => x ^ y);
        }
    }
}
```

### 3.2 新增：发音值对象

**文件：** `Models/Learning/ValueObjects/Pronunciation.cs`

```csharp
namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class Pronunciation : ValueObject
    {
        public string Main { get; set; } = string.Empty;
        public string? UkPhonetic { get; set; }
        public string? UsPhonetic { get; set; }

        public Pronunciation() { }

        public Pronunciation(string main, string? ukPhonetic = null, string? usPhonetic = null)
        {
            Main = main ?? throw new ArgumentNullException(nameof(main));
            UkPhonetic = ukPhonetic;
            UsPhonetic = usPhonetic;
        }

        public static Pronunciation Create(string main, string? ukPhonetic = null, string? usPhonetic = null)
        {
            if (string.IsNullOrWhiteSpace(main))
                throw new ArgumentException("发音不能为空", nameof(main));
            return new Pronunciation(main, ukPhonetic, usPhonetic);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Main;
            yield return UkPhonetic ?? string.Empty;
            yield return UsPhonetic ?? string.Empty;
        }
    }
}
```

### 3.3 新增：释义值对象

**文件：** `Models/Learning/ValueObjects/Meaning.cs`

```csharp
namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class Meaning : ValueObject
    {
        public string Content { get; set; } = string.Empty;

        public Meaning() { }

        public Meaning(string content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public static Meaning Create(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("释义不能为空", nameof(content));
            return new Meaning(content);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Content;
        }
    }
}
```

### 3.4 新增：例句值对象

**文件：** `Models/Learning/ValueObjects/Example.cs`

```csharp
namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class Example : ValueObject
    {
        public string Content { get; set; } = string.Empty;
        public string? Translation { get; set; }

        public Example() { }

        public Example(string content, string? translation = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Translation = translation;
        }

        public static Example Create(string content, string? translation = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("例句不能为空", nameof(content));
            return new Example(content, translation);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Content;
            yield return Translation ?? string.Empty;
        }
    }
}
```

### 3.5 新增：汉字特征值对象

**文件：** `Models/Learning/ValueObjects/CharacterFeatures.cs`

```csharp
namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class CharacterFeatures : ValueObject
    {
        public string StrokeCount { get; set; } = string.Empty;
        public string Radical { get; set; } = string.Empty;
        public string Structure { get; set; } = string.Empty;

        public CharacterFeatures() { }

        public CharacterFeatures(string strokeCount, string radical, string structure)
        {
            StrokeCount = strokeCount;
            Radical = radical;
            Structure = structure;
        }

        public static CharacterFeatures Create(string strokeCount, string radical, string structure)
            => new(strokeCount, radical, structure);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return StrokeCount;
            yield return Radical;
            yield return Structure;
        }
    }
}
```

### 3.6 新增：单词特征值对象

**文件：** `Models/Learning/ValueObjects/WordFeatures.cs`

```csharp
namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class WordFeatures : ValueObject
    {
        public string PartOfSpeech { get; set; } = string.Empty;
        public string WordForms { get; set; } = string.Empty;
        public string Collocations { get; set; } = string.Empty;
        public string SyllableBreakdown { get; set; } = string.Empty;

        public WordFeatures() { }

        public WordFeatures(string partOfSpeech, string wordForms, string collocations, string syllableBreakdown)
        {
            PartOfSpeech = partOfSpeech;
            WordForms = wordForms;
            Collocations = collocations;
            SyllableBreakdown = syllableBreakdown;
        }

        public static WordFeatures Create(string partOfSpeech = "", string wordForms = "", 
                                          string collocations = "", string syllableBreakdown = "")
            => new(partOfSpeech, wordForms, collocations, syllableBreakdown);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return PartOfSpeech;
            yield return WordForms;
            yield return Collocations;
            yield return SyllableBreakdown;
        }
    }
}
```

### 3.7 新增：学习进度值对象

**文件：** `Models/Learning/ValueObjects/LearningProgress.cs`

```csharp
namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class LearningProgress : ValueObject
    {
        public int TotalReviewCount { get; set; }
        public int CorrectCount { get; set; }
        public double AccuracyRate => TotalReviewCount > 0 ? (double)CorrectCount / TotalReviewCount : 0;
        public DateTime? LastReviewDate { get; set; }
        public int Streak { get; set; }

        public LearningProgress() { }

        public LearningProgress(int totalReviewCount, int correctCount, DateTime? lastReviewDate, int streak)
        {
            TotalReviewCount = totalReviewCount;
            CorrectCount = correctCount;
            LastReviewDate = lastReviewDate;
            Streak = streak;
        }

        public static LearningProgress Create()
            => new(0, 0, null, 0);

        public LearningProgress Update(bool isCorrect)
            => new(
                TotalReviewCount + 1,
                CorrectCount + (isCorrect ? 1 : 0),
                DateTime.Now,
                isCorrect ? Streak + 1 : 0
            );

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return TotalReviewCount;
            yield return CorrectCount;
            yield return LastReviewDate ?? DateTime.MinValue;
            yield return Streak;
        }
    }
}
```

### 3.8 新增：学习状态枚举（替代状态模式）

**文件：** `Models/Learning/Status/LearningStatus.cs`

```csharp
namespace LearningAssistant.Models.Learning.Status
{
    public enum LearningStatus
    {
        New = 0,
        Learning = 1,
        Known = 2,
        Mastered = 3
    }

    public static class LearningStatusExtensions
    {
        public static LearningStatus Promote(this LearningStatus status)
        {
            return status switch
            {
                LearningStatus.New => LearningStatus.Learning,
                LearningStatus.Learning => LearningStatus.Known,
                LearningStatus.Known => LearningStatus.Mastered,
                LearningStatus.Mastered => LearningStatus.Mastered,
                _ => status
            };
        }

        public static LearningStatus Demote(this LearningStatus status)
        {
            return status switch
            {
                LearningStatus.New => LearningStatus.New,
                LearningStatus.Learning => LearningStatus.New,
                LearningStatus.Known => LearningStatus.Learning,
                LearningStatus.Mastered => LearningStatus.Known,
                _ => status
            };
        }
    }
}
```

### 3.9 修改：统一学习项模型

**文件：** `Models/Learning/LearningItem.cs`

```csharp
using LearningAssistant.Common;
using LearningAssistant.Models.Learning.Status;
using LearningAssistant.Models.Learning.ValueObjects;
using Newtonsoft.Json;

namespace LearningAssistant.Models.Learning
{
    public class LearningItem
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public SubjectType Subject { get; set; }
        public SubCategoryType SubCategory { get; set; }

        public string MainContent { get; set; } = string.Empty;
        public Meaning? Meaning { get; set; }
        public Example? Example { get; set; }

        public Pronunciation? Pronunciation { get; set; }
        public CharacterFeatures? CharacterFeatures { get; set; }
        public WordFeatures? WordFeatures { get; set; }

        public string ExtendedProperties { get; set; } = "{}";

        [JsonProperty("Status")]
        public LearningStatus Status { get; set; } = LearningStatus.New;

        [JsonProperty("ReviewCount")]
        public int ReviewCount { get; set; }

        [JsonProperty("LastReviewedAt")]
        public DateTime? LastReviewedAt { get; set; }

        public void Review(bool isCorrect)
        {
            ReviewCount++;
            LastReviewedAt = DateTime.Now;
            Status = isCorrect ? Status.Promote() : Status.Demote();
        }

        public void MarkAsKnown()
        {
            Status = LearningStatus.Known;
        }

        public void MarkAsUnknown()
        {
            Status = LearningStatus.New;
        }

        public void UpdateContent(string newContent)
        {
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("内容不能为空", nameof(newContent));
            MainContent = newContent;
            UpdatedAt = DateTime.Now;
        }

        public void UpdateMeaning(string newMeaning)
        {
            Meaning = Meaning.Create(newMeaning);
            UpdatedAt = DateTime.Now;
        }

        public T GetExtendedProperty<T>(string key, T defaultValue = default)
        {
            try
            {
                var props = JsonConvert.DeserializeObject<Dictionary<string, object>>(ExtendedProperties);
                if (props?.TryGetValue(key, out var value) == true)
                    return JsonConvert.DeserializeObject<T>(value.ToString() ?? "");
            }
            catch { }
            return defaultValue;
        }

        public void SetExtendedProperty(string key, object value)
        {
            var props = JsonConvert.DeserializeObject<Dictionary<string, object>>(ExtendedProperties) 
                        ?? new Dictionary<string, object>();
            props[key] = value;
            ExtendedProperties = JsonConvert.SerializeObject(props);
        }

        public string GetMainContent() => MainContent;

        public string GetDisplayText()
        {
            return LearningItemFormatter.FormatDisplayText(this);
        }

        public string GetPronunciation()
        {
            return Pronunciation?.Main ?? string.Empty;
        }

        public string GetDisplayStruct()
        {
            return LearningItemFormatter.FormatDisplayStruct(this);
        }

        public static LearningItem Create(SubjectType subject, SubCategoryType subCategory, 
                                          string mainContent, string meaning)
        {
            ValidateSubjectSubCategory(subject, subCategory);

            return new LearningItem
            {
                Id = Guid.NewGuid().ToString(),
                Subject = subject,
                SubCategory = subCategory,
                MainContent = mainContent,
                Meaning = Meaning.Create(meaning),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        private static void ValidateSubjectSubCategory(SubjectType subject, SubCategoryType subCategory)
        {
            var validSubCategories = SubjectSubCategoryMapping.GetSubCategories(subject);
            if (!validSubCategories.Contains(subCategory))
                throw new ArgumentException($"子类别 {subCategory} 不属于科目 {subject}");
        }
    }
}
```

### 3.10 新增：科目-子类别映射（含字符串-枚举转换）

**文件：** `Common/SubjectSubCategoryMapping.cs`

```csharp
namespace LearningAssistant.Common
{
    public static class SubjectSubCategoryMapping
    {
        private static readonly Dictionary<SubjectType, List<SubCategoryType>> _mapping = new()
        {
            { SubjectType.Chinese, new List<SubCategoryType> 
                { SubCategoryType.ChineseCharacter, SubCategoryType.ChinesePhrase, 
                  SubCategoryType.ChineseIdiom, SubCategoryType.ChinesePoem, 
                  SubCategoryType.ChineseComprehensive } },
            { SubjectType.English, new List<SubCategoryType> 
                { SubCategoryType.EnglishWord, SubCategoryType.EnglishPhrase, 
                  SubCategoryType.EnglishSentence, SubCategoryType.EnglishComprehensive } }
        };

        private static readonly Dictionary<string, SubjectType> _subjectStringMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Chinese", SubjectType.Chinese },
            { "English", SubjectType.English },
            { "中文", SubjectType.Chinese },
            { "英语", SubjectType.English },
            { "语文", SubjectType.Chinese }
        };

        private static readonly Dictionary<string, SubCategoryType> _subCategoryStringMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ChineseCharacter", SubCategoryType.ChineseCharacter },
            { "ChinesePhrase", SubCategoryType.ChinesePhrase },
            { "ChineseIdiom", SubCategoryType.ChineseIdiom },
            { "ChinesePoem", SubCategoryType.ChinesePoem },
            { "ChineseComprehensive", SubCategoryType.ChineseComprehensive },
            { "EnglishWord", SubCategoryType.EnglishWord },
            { "EnglishPhrase", SubCategoryType.EnglishPhrase },
            { "EnglishSentence", SubCategoryType.EnglishSentence },
            { "EnglishComprehensive", SubCategoryType.EnglishComprehensive },
            { "识字", SubCategoryType.ChineseCharacter },
            { "短语", SubCategoryType.ChinesePhrase },
            { "成语", SubCategoryType.ChineseIdiom },
            { "诗词", SubCategoryType.ChinesePoem },
            { "语文综合", SubCategoryType.ChineseComprehensive },
            { "英语单词", SubCategoryType.EnglishWord },
            { "英语短语", SubCategoryType.EnglishPhrase },
            { "英语句子", SubCategoryType.EnglishSentence },
            { "英语综合", SubCategoryType.EnglishComprehensive }
        };

        public static List<SubCategoryType> GetSubCategories(SubjectType subject)
            => _mapping.TryGetValue(subject, out var list) ? list : new List<SubCategoryType>();

        public static SubjectType GetSubject(SubCategoryType subCategory)
            => _mapping.FirstOrDefault(kv => kv.Value.Contains(subCategory)).Key;

        public static bool IsValidSubCategory(SubjectType subject, SubCategoryType subCategory)
            => GetSubCategories(subject).Contains(subCategory);

        public static SubjectType ParseSubject(string subjectString)
        {
            if (Enum.TryParse<SubjectType>(subjectString, true, out var result))
                return result;

            return _subjectStringMap.TryGetValue(subjectString, out var mapped) 
                ? mapped 
                : SubjectType.Chinese;
        }

        public static SubCategoryType ParseSubCategory(string subCategoryString)
        {
            if (Enum.TryParse<SubCategoryType>(subCategoryString, true, out var result))
                return result;

            return _subCategoryStringMap.TryGetValue(subCategoryString, out var mapped) 
                ? mapped 
                : SubCategoryType.ChineseCharacter;
        }

        public static bool TryParseSubject(string subjectString, out SubjectType subject)
        {
            if (Enum.TryParse<SubjectType>(subjectString, true, out subject))
                return true;

            return _subjectStringMap.TryGetValue(subjectString, out subject);
        }

        public static bool TryParseSubCategory(string subCategoryString, out SubCategoryType subCategory)
        {
            if (Enum.TryParse<SubCategoryType>(subCategoryString, true, out subCategory))
                return true;

            return _subCategoryStringMap.TryGetValue(subCategoryString, out subCategory);
        }
    }
}
```

### 3.11 新增：学习项显示格式化器

**文件：** `Services/Learning/LearningItemFormatter.cs`

```csharp
using LearningAssistant.Common;
using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    public static class LearningItemFormatter
    {
        public static string FormatDisplayText(LearningItem item)
        {
            var parts = new List<string>();

            switch (item.SubCategory)
            {
                case SubCategoryType.ChineseCharacter:
                    AddIfNotEmpty(parts, "拼音", item.Pronunciation?.Main);
                    AddIfNotEmpty(parts, "释义", item.Meaning?.Content);
                    AddIfNotEmpty(parts, "笔画", item.CharacterFeatures?.StrokeCount + "画");
                    AddIfNotEmpty(parts, "部首", item.CharacterFeatures?.Radical);
                    AddIfNotEmpty(parts, "结构", item.CharacterFeatures?.Structure);
                    AddIfNotEmpty(parts, "组词", item.GetExtendedProperty<string>("Words"));
                    AddIfNotEmpty(parts, "例句", item.Example?.Content);
                    break;

                case SubCategoryType.ChinesePhrase:
                case SubCategoryType.ChineseIdiom:
                    AddIfNotEmpty(parts, "拼音", item.Pronunciation?.Main);
                    AddIfNotEmpty(parts, "释义", item.Meaning?.Content);
                    AddIfNotEmpty(parts, "例句", item.Example?.Content);
                    break;

                case SubCategoryType.ChinesePoem:
                    AddIfNotEmpty(parts, "作者", item.GetExtendedProperty<string>("Author"));
                    AddIfNotEmpty(parts, "朝代", item.GetExtendedProperty<string>("Dynasty"));
                    AddIfNotEmpty(parts, "内容", item.GetExtendedProperty<string>("Content"));
                    break;

                case SubCategoryType.EnglishWord:
                    AddIfNotEmpty(parts, "词性", item.WordFeatures?.PartOfSpeech);
                    AddIfNotEmpty(parts, "音标", item.Pronunciation?.Main);
                    AddIfNotEmpty(parts, "英式", item.Pronunciation?.UkPhonetic);
                    AddIfNotEmpty(parts, "美式", item.Pronunciation?.UsPhonetic);
                    AddIfNotEmpty(parts, "拼读", item.WordFeatures?.SyllableBreakdown);
                    AddIfNotEmpty(parts, "释义", item.Meaning?.Content);
                    AddIfNotEmpty(parts, "词形", item.WordFeatures?.WordForms);
                    AddIfNotEmpty(parts, "搭配", item.WordFeatures?.Collocations);
                    AddIfNotEmpty(parts, "例句", item.Example?.Content);
                    AddIfNotEmpty(parts, "例句翻译", item.Example?.Translation);
                    break;

                case SubCategoryType.EnglishPhrase:
                    AddIfNotEmpty(parts, "音标", item.Pronunciation?.Main);
                    AddIfNotEmpty(parts, "释义", item.Meaning?.Content);
                    AddIfNotEmpty(parts, "例句", item.Example?.Content);
                    break;

                case SubCategoryType.EnglishSentence:
                    AddIfNotEmpty(parts, "翻译", item.Meaning?.Content);
                    break;

                case SubCategoryType.ChineseComprehensive:
                case SubCategoryType.EnglishComprehensive:
                    AddIfNotEmpty(parts, "内容", item.MainContent);
                    AddIfNotEmpty(parts, "释义", item.Meaning?.Content);
                    break;
            }

            return string.Join("\n", parts);
        }

        public static string FormatDisplayStruct(LearningItem item)
        {
            var parts = new List<string>();

            switch (item.SubCategory)
            {
                case SubCategoryType.ChineseCharacter:
                    AddIfNotEmpty(parts, "拼音:?");
                    AddIfNotEmpty(parts, "释义:?");
                    AddIfNotEmpty(parts, "笔画:?");
                    AddIfNotEmpty(parts, "部首:?");
                    AddIfNotEmpty(parts, "结构:?");
                    AddIfNotEmpty(parts, "组词:?");
                    AddIfNotEmpty(parts, "例句:?");
                    break;

                case SubCategoryType.ChinesePhrase:
                case SubCategoryType.ChineseIdiom:
                    AddIfNotEmpty(parts, "拼音:?");
                    AddIfNotEmpty(parts, "释义:?");
                    AddIfNotEmpty(parts, "例句:?");
                    break;

                case SubCategoryType.EnglishWord:
                    AddIfNotEmpty(parts, "词性:?");
                    AddIfNotEmpty(parts, "音标:?");
                    AddIfNotEmpty(parts, "释义:?");
                    AddIfNotEmpty(parts, "词形:?");
                    AddIfNotEmpty(parts, "搭配:?");
                    AddIfNotEmpty(parts, "例句:?");
                    break;

                case SubCategoryType.EnglishPhrase:
                    AddIfNotEmpty(parts, "音标:?");
                    AddIfNotEmpty(parts, "释义:?");
                    AddIfNotEmpty(parts, "例句:?");
                    break;

                case SubCategoryType.EnglishSentence:
                    AddIfNotEmpty(parts, "翻译:?");
                    break;
            }

            return string.Join("\n", parts);
        }

        private static void AddIfNotEmpty(List<string> parts, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add($"{label}: {value}");
        }

        private static void AddIfNotEmpty(List<string> parts, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add(value);
        }
    }
}
```

### 3.12 修改：JSON转换器

**文件：** `Common/LearningItemJsonConverter.cs`

```csharp
using LearningAssistant.Models.Learning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LearningAssistant.Common
{
    public class LearningItemJsonConverter : JsonConverter<LearningItem>
    {
        public override LearningItem? ReadJson(JsonReader reader, Type objectType, LearningItem? existingValue, 
                                                bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException($"Expected StartObject token, got {reader.TokenType}");

            JObject jsonObject = JObject.Load(reader);
            var item = new LearningItem();

            item.Id = jsonObject["Id"]?.ToString() ?? Guid.NewGuid().ToString();
            item.CreatedAt = jsonObject["CreatedAt"]?.ToObject<DateTime>() ?? DateTime.Now;
            item.UpdatedAt = jsonObject["UpdatedAt"]?.ToObject<DateTime>() ?? DateTime.Now;

            if (Enum.TryParse(jsonObject["Subject"]?.ToString(), out SubjectType subject))
                item.Subject = subject;

            if (Enum.TryParse(jsonObject["SubCategory"]?.ToString(), out SubCategoryType subCategory))
                item.SubCategory = subCategory;

            item.MainContent = jsonObject["MainContent"]?.ToString() ?? 
                              jsonObject["Word"]?.ToString() ?? 
                              jsonObject["Character"]?.ToString() ?? 
                              jsonObject["Phrase"]?.ToString() ?? 
                              jsonObject["Sentence"]?.ToString() ?? string.Empty;

            var meaningContent = jsonObject["Meaning"]?.ToString() ?? 
                                jsonObject["ChineseMeaning"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(meaningContent))
                item.Meaning = Models.Learning.ValueObjects.Meaning.Create(meaningContent);

            var exampleContent = jsonObject["Example"]?.ToString() ?? string.Empty;
            var exampleTranslation = jsonObject["ExampleTranslation"]?.ToString();
            if (!string.IsNullOrWhiteSpace(exampleContent))
                item.Example = Models.Learning.ValueObjects.Example.Create(exampleContent, exampleTranslation);

            var pronunciation = jsonObject["Phonetic"]?.ToString() ?? jsonObject["Pinyin"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(pronunciation))
            {
                item.Pronunciation = Models.Learning.ValueObjects.Pronunciation.Create(
                    pronunciation,
                    jsonObject["UkPhonetic"]?.ToString(),
                    jsonObject["UsPhonetic"]?.ToString()
                );
            }

            if (item.SubCategory == SubCategoryType.ChineseCharacter)
            {
                item.CharacterFeatures = Models.Learning.ValueObjects.CharacterFeatures.Create(
                    jsonObject["StrokeCount"]?.ToString() ?? string.Empty,
                    jsonObject["Radical"]?.ToString() ?? string.Empty,
                    jsonObject["Structure"]?.ToString() ?? string.Empty
                );
            }

            if (item.SubCategory == SubCategoryType.EnglishWord)
            {
                item.WordFeatures = Models.Learning.ValueObjects.WordFeatures.Create(
                    jsonObject["PartOfSpeech"]?.ToString() ?? string.Empty,
                    jsonObject["WordForms"]?.ToString() ?? string.Empty,
                    jsonObject["Collocations"]?.ToString() ?? string.Empty,
                    jsonObject["SyllableBreakdown"]?.ToString() ?? string.Empty
                );
            }

            var extendedProps = new Dictionary<string, object>();
            foreach (var prop in jsonObject)
            {
                if (!IsStandardProperty(prop.Key))
                    extendedProps[prop.Key] = prop.Value.ToObject<object>() ?? string.Empty;
            }
            item.ExtendedProperties = JsonConvert.SerializeObject(extendedProps);

            return item;
        }

        private static bool IsStandardProperty(string key)
        {
            var standardProps = new[] { "Id", "CreatedAt", "UpdatedAt", "Subject", "SubCategory", 
                                       "MainContent", "Meaning", "Example", "Phonetic", "Pinyin",
                                       "UkPhonetic", "UsPhonetic", "StrokeCount", "Radical", 
                                       "Structure", "PartOfSpeech", "WordForms", "Collocations", 
                                       "SyllableBreakdown", "Word", "Character", "Phrase", "Sentence",
                                       "ChineseMeaning", "ExampleTranslation", "$type" };
            return standardProps.Contains(key, StringComparer.OrdinalIgnoreCase);
        }

        public override void WriteJson(JsonWriter writer, LearningItem? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("Id");
            writer.WriteValue(value.Id);
            writer.WritePropertyName("CreatedAt");
            writer.WriteValue(value.CreatedAt);
            writer.WritePropertyName("UpdatedAt");
            writer.WriteValue(value.UpdatedAt);
            writer.WritePropertyName("Subject");
            writer.WriteValue(value.Subject.ToString());
            writer.WritePropertyName("SubCategory");
            writer.WriteValue(value.SubCategory.ToString());
            writer.WritePropertyName("MainContent");
            writer.WriteValue(value.MainContent);

            if (value.Meaning != null)
            {
                writer.WritePropertyName("Meaning");
                writer.WriteValue(value.Meaning.Content);
            }

            if (value.Example != null)
            {
                writer.WritePropertyName("Example");
                writer.WriteValue(value.Example.Content);
                if (!string.IsNullOrWhiteSpace(value.Example.Translation))
                {
                    writer.WritePropertyName("ExampleTranslation");
                    writer.WriteValue(value.Example.Translation);
                }
            }

            if (value.Pronunciation != null)
            {
                writer.WritePropertyName("Phonetic");
                writer.WriteValue(value.Pronunciation.Main);
                if (!string.IsNullOrWhiteSpace(value.Pronunciation.UkPhonetic))
                {
                    writer.WritePropertyName("UkPhonetic");
                    writer.WriteValue(value.Pronunciation.UkPhonetic);
                }
                if (!string.IsNullOrWhiteSpace(value.Pronunciation.UsPhonetic))
                {
                    writer.WritePropertyName("UsPhonetic");
                    writer.WriteValue(value.Pronunciation.UsPhonetic);
                }
            }

            if (value.CharacterFeatures != null)
            {
                writer.WritePropertyName("StrokeCount");
                writer.WriteValue(value.CharacterFeatures.StrokeCount);
                writer.WritePropertyName("Radical");
                writer.WriteValue(value.CharacterFeatures.Radical);
                writer.WritePropertyName("Structure");
                writer.WriteValue(value.CharacterFeatures.Structure);
            }

            if (value.WordFeatures != null)
            {
                writer.WritePropertyName("PartOfSpeech");
                writer.WriteValue(value.WordFeatures.PartOfSpeech);
                writer.WritePropertyName("WordForms");
                writer.WriteValue(value.WordFeatures.WordForms);
                writer.WritePropertyName("Collocations");
                writer.WriteValue(value.WordFeatures.Collocations);
                writer.WritePropertyName("SyllableBreakdown");
                writer.WriteValue(value.WordFeatures.SyllableBreakdown);
            }

            writer.WritePropertyName("ExtendedProperties");
            writer.WriteRawValue(value.ExtendedProperties);

            writer.WriteEndObject();
        }
    }
}
```

### 3.13 修改：内容加载服务

**文件：** `Services/Learning/ContentLoaderService.cs`

```csharp
using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LearningAssistant.Services.Learning
{
    public class ContentLoaderService : IContentLoaderService
    {
        private readonly ILogger<ContentLoaderService> _logger;

        private static readonly JsonSerializerSettings _serializerSettings = new()
        {
            Converters = { new LearningItemJsonConverter() }
        };

        private readonly Dictionary<string, string> _categoryFileMap = new Dictionary<string, string>
        {
            { Constants.SubCategory.ChineseCharacter, Constants.FileName.ChineseCharacter },
            { Constants.SubCategory.ChineseIdiom, Constants.FileName.ChineseIdiom },
            { Constants.SubCategory.ChinesePhrase, Constants.FileName.ChinesePhrase },
            { Constants.SubCategory.ChinesePoem, Constants.FileName.ChinesePoem },
            { Constants.SubCategory.ChineseComprehensive, Constants.FileName.ChineseComprehensive },
            { Constants.SubCategory.EnglishWord, Constants.FileName.EnglishWord },
            { Constants.SubCategory.EnglishPhrase, Constants.FileName.EnglishPhrase },
            { Constants.SubCategory.EnglishSentence, Constants.FileName.EnglishSentence },
            { Constants.SubCategory.EnglishComprehensive, Constants.FileName.EnglishComprehensive },
            { Constants.SubCategory.MathFormula, Constants.FileName.MathFormula },
            { Constants.SubCategory.MathExample, Constants.FileName.MathExample },
            { Constants.SubCategory.MathConcept, Constants.FileName.MathConcept },
            { Constants.SubCategory.MathComprehensive, Constants.FileName.MathComprehensive },
            { Constants.SubCategory.PhysicsLaw, Constants.FileName.PhysicsLaw },
            { Constants.SubCategory.PhysicsExperiment, Constants.FileName.PhysicsExperiment },
            { Constants.SubCategory.PhysicsDerivation, Constants.FileName.PhysicsDerivation },
            { Constants.SubCategory.PhysicsComprehensive, Constants.FileName.PhysicsComprehensive },
            { Constants.SubCategory.ChemistryEquation, Constants.FileName.ChemistryEquation },
            { Constants.SubCategory.ChemistryElement, Constants.FileName.ChemistryElement },
            { Constants.SubCategory.ChemistryExperiment, Constants.FileName.ChemistryExperiment },
            { Constants.SubCategory.ChemistryComprehensive, Constants.FileName.ChemistryComprehensive },
            { Constants.SubCategory.HistoryEvent, Constants.FileName.HistoryEvent },
            { Constants.SubCategory.HistoryPerson, Constants.FileName.HistoryPerson },
            { Constants.SubCategory.HistoryTimeline, Constants.FileName.HistoryTimeline },
            { Constants.SubCategory.HistoryComprehensive, Constants.FileName.HistoryComprehensive },
            { Constants.SubCategory.GeographyKnowledge, Constants.FileName.GeographyKnowledge },
            { Constants.SubCategory.GeographyMap, Constants.FileName.GeographyMap },
            { Constants.SubCategory.GeographyClimate, Constants.FileName.GeographyClimate },
            { Constants.SubCategory.GeographyComprehensive, Constants.FileName.GeographyComprehensive },
            { Constants.SubCategory.BiologyConcept, Constants.FileName.BiologyConcept },
            { Constants.SubCategory.BiologyExperiment, Constants.FileName.BiologyExperiment },
            { Constants.SubCategory.BiologyPhenomenon, Constants.FileName.BiologyPhenomenon },
            { Constants.SubCategory.BiologyComprehensive, Constants.FileName.BiologyComprehensive }
        };

        public ContentLoaderService(ILogger<ContentLoaderService> logger)
        {
            _logger = logger;
        }

        public List<LearningItem> LoadItems(string subCategory, string wordBankFile = "")
        {
            try
            {
                string filePath = GetFilePath(subCategory, wordBankFile);
                
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

                string content = File.ReadAllText(filePath);
                var items = JsonConvert.DeserializeObject<List<LearningItem>>(content, _serializerSettings) ?? new List<LearningItem>();

                foreach (var item in items)
                {
                    item.SubCategory = ParseSubCategory(subCategory);
                    item.Subject = GetSubjectFromSubCategory(subCategory);
                }

                _logger.LogInformation("Loaded {Count} items from {FilePath}", items.Count, filePath);
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load items from subcategory: {SubCategory}", subCategory);
                return new List<LearningItem>();
            }
        }

        private SubCategoryType ParseSubCategory(string subCategory)
        {
            if (Enum.TryParse<SubCategoryType>(subCategory, true, out var result))
                return result;
            
            return subCategory switch
            {
                "ChineseCharacter" or "识字" => SubCategoryType.ChineseCharacter,
                "ChinesePhrase" or "短语" => SubCategoryType.ChinesePhrase,
                "ChineseIdiom" or "成语" => SubCategoryType.ChineseIdiom,
                "ChinesePoem" or "诗词" => SubCategoryType.ChinesePoem,
                "ChineseComprehensive" or "语文综合" => SubCategoryType.ChineseComprehensive,
                "EnglishWord" or "英语单词" => SubCategoryType.EnglishWord,
                "EnglishPhrase" or "英语短语" => SubCategoryType.EnglishPhrase,
                "EnglishSentence" or "英语句子" => SubCategoryType.EnglishSentence,
                "EnglishComprehensive" or "英语综合" => SubCategoryType.EnglishComprehensive,
                _ => SubCategoryType.ChineseCharacter
            };
        }

        private SubjectType GetSubjectFromSubCategory(string subCategory)
        {
            return subCategory.StartsWith("Chinese", StringComparison.OrdinalIgnoreCase) ||
                   subCategory.StartsWith("语文", StringComparison.OrdinalIgnoreCase)
                ? SubjectType.Chinese
                : SubjectType.English;
        }

        private string GetFilePath(string subCategory, string wordBankFile)
        {
            if (!string.IsNullOrEmpty(wordBankFile) && File.Exists(wordBankFile))
                return wordBankFile;

            if (_categoryFileMap.TryGetValue(subCategory, out var fileName))
                return Path.Combine(AppPaths.DataDir, fileName);

            return Path.Combine(AppPaths.DataDir, $"{subCategory}.json");
        }

        private bool IsPathSafe(string path)
        {
            string normalizedPath = Path.GetFullPath(path);
            string dataDir = Path.GetFullPath(AppPaths.DataDir);
            return normalizedPath.StartsWith(dataDir, StringComparison.OrdinalIgnoreCase);
        }
    }
}
```

---

## 四、数据迁移策略

### 4.1 向后兼容方案

1. **读取旧格式**：`LearningItemJsonConverter` 已支持从旧格式（如 `EnglishWord`、`ChineseCharacter`）自动转换
2. **写入新格式**：统一写入新格式的 `LearningItem`
3. **版本标记**：在JSON中添加 `$schemaVersion` 字段

### 4.2 迁移步骤

```
Step 1: 部署新代码（支持双向兼容）
Step 2: 用户首次打开应用时，自动迁移数据库中的旧格式数据
Step 3: 清理旧的学习项文件（可选）
Step 4: 验证数据完整性
```

### 4.3 迁移代码示例

```csharp
public class DataMigrationService : IDataMigrationService
{
    public void MigrateLearningItems()
    {
        var oldItems = LoadOldFormatItems();
        var newItems = oldItems.Select(ConvertToNewFormat).ToList();
        SaveNewFormatItems(newItems);
    }

    private LearningItem ConvertToNewFormat(object oldItem)
    {
        var json = JsonConvert.SerializeObject(oldItem);
        var serializer = new JsonSerializer();
        serializer.Converters.Add(new LearningItemJsonConverter());
        using var reader = new JsonTextReader(new StringReader(json));
        return serializer.Deserialize<LearningItem>(reader)!;
    }
}
```

---

## 五、验证检查清单

### 5.1 编译验证

- [ ] `dotnet build` 无错误
- [ ] 所有项目文件都能正确编译
- [ ] 无警告（可选）

### 5.2 功能验证

- [ ] 学习项加载正常
- [ ] 学习引擎正常工作
- [ ] 进度保存和恢复正常
- [ ] 导入导出功能正常
- [ ] UI显示正常

### 5.3 数据验证

- [ ] 旧格式数据能正确迁移
- [ ] 新格式数据能正确保存
- [ ] 数据完整性检查通过

### 5.4 性能验证

- [ ] 启动时间对比（优化前 vs 优化后）
- [ ] 学习项加载时间对比
- [ ] 内存占用对比

---

## 六、实施顺序

### Phase 1：基础建设（低风险）

1. 创建值对象基类和各值对象
2. 创建科目-子类别映射
3. 创建学习项显示格式化器
4. 修改 `LearningItem` 为具体类，添加领域行为

### Phase 2：核心服务适配（中等风险）

5. 修改 `LearningItemJsonConverter` 支持双向兼容
6. 修改 `ContentLoaderService` 使用统一模型
7. 修改 `StudyEngine` 适配新模型
8. 修改 `ProgressManager` 适配新模型

### Phase 3：数据持久化适配（中等风险）

9. 修改 `SqliteDataPersistenceService`
10. 修改 `DataImportService` 和 `LearningDataExportService`

### Phase 4：UI适配（中等风险）

11. 修改 `LearningFlowHandler`
12. 修改 `ContentEditorPresenter`
13. 修改 `LearningForm` 和 `ContentEditorForm`

### Phase 5：清理（低风险）

14. 删除旧的学习项子类文件
15. 运行测试验证
16. 部署发布

---

## 七、风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|------|-----|------|---------|
| 数据迁移失败 | 低 | 高 | 备份数据，支持回滚 |
| 序列化兼容性问题 | 中 | 高 | 双向兼容设计 |
| UI显示异常 | 中 | 中 | 充分测试 |
| 性能回归 | 低 | 中 | 性能测试对比 |
| 第三方依赖问题 | 低 | 中 | 锁定依赖版本 |

---

## 八、预期收益

| 指标 | 优化前 | 优化后 |
|------|-------|-------|
| 学习项类数量 | 11个 | 1个 |
| 代码重复率 | 高 | 低 |
| 扩展新类型难度 | 需要新建类 | 修改配置即可 |
| 数据一致性 | 依赖事务 | 聚合保证 |
| 测试难度 | 高 | 低（领域行为可独立测试） |
| 维护成本 | 高 | 低 |

---

## 九、特殊类型处理

### 9.1 GeneralSubjectItem（通用学科项）处理策略

**当前状态**：`GeneralSubjectItem` 使用字符串存储科目（Subject）和分类（Category），与枚举体系无关。

**处理方案**：合并到统一模型，通过扩展属性存储学科特定数据。

```csharp
// 迁移规则：GeneralSubjectItem -> LearningItem
// Subject: "Math" -> SubjectType.English（非语言类统一映射为English，或扩展SubjectType枚举）
// SubCategory: "Formula" -> 通过扩展属性存储
// Title -> MainContent
// Content -> Meaning.Content
// Category -> ExtendedProperties["Category"]
```

**建议**：如果需要保留数学、物理等学科，考虑扩展 `SubjectType` 枚举：

```csharp
public enum SubjectType
{
    Chinese = 0,
    English = 1,
    Math = 2,
    Physics = 3,
    Chemistry = 4,
    History = 5,
    Geography = 6,
    Biology = 7
}
```

### 9.2 GrammarRule（语法规则）处理策略

**当前状态**：`GrammarRule` 是特殊的学习项类型，包含规则描述和示例。

**处理方案**：合并到统一模型，使用 `SubCategoryType.EnglishComprehensive` 或创建新的子类别。

```csharp
// 迁移规则：GrammarRule -> LearningItem
// Rule -> MainContent
// Explanation -> Meaning.Content
// Examples -> Example（多个示例存储在ExtendedProperties中）
```

### 9.3 枚举扩展建议

如需支持通用学科，扩展 `SubCategoryType`：

```csharp
public enum SubCategoryType
{
    // 中文
    ChineseCharacter = 0,
    ChinesePhrase = 1,
    ChineseIdiom = 2,
    ChinesePoem = 3,
    ChineseComprehensive = 4,
    
    // 英语
    EnglishWord = 10,
    EnglishPhrase = 11,
    EnglishSentence = 12,
    EnglishComprehensive = 13,
    
    // 数学
    MathFormula = 20,
    MathExample = 21,
    MathConcept = 22,
    MathComprehensive = 23,
    
    // 物理
    PhysicsLaw = 30,
    PhysicsExperiment = 31,
    PhysicsDerivation = 32,
    PhysicsComprehensive = 33,
    
    // 其他学科...
}
```

---

## 十、参考资料

- [领域驱动设计：值对象](https://martinfowler.com/bliki/ValueObject.html)
- [状态模式](https://refactoring.guru/design-patterns/state)
- [聚合模式](https://martinfowler.com/bliki/DDD_Aggregate.html)
