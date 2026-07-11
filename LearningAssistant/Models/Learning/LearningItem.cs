using LearningAssistant.Common;
using LearningAssistant.Models.Learning.Status;
using LearningAssistant.Models.Learning.ValueObjects;
using LearningAssistant.Services.Learning;
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
                {
                    if (typeof(T) == typeof(string))
                        return (T)(object)(value?.ToString() ?? "");
                    return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"获取扩展属性失败 [key={key}]: {ex.Message}");
            }
            return defaultValue;
        }

        public void SetExtendedProperty(string key, object value)
        {
            var props = JsonConvert.DeserializeObject<Dictionary<string, object>>(ExtendedProperties) 
                        ?? new Dictionary<string, object>();
            props[key] = value;
            ExtendedProperties = JsonConvert.SerializeObject(props);
        }

        public virtual string GetMainContent() => MainContent;

        public virtual string GetDisplayText()
        {
            return LearningItemFormatter.FormatDisplayText(this);
        }

        public virtual string GetPronunciation()
        {
            return Pronunciation?.Main ?? string.Empty;
        }

        public virtual string GetDisplayStruct()
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