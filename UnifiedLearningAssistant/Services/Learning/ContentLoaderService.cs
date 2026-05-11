using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Learning;

namespace UnifiedLearningAssistant.Services.Learning
{
    public class ContentLoaderService : IContentLoaderService
    {
        private readonly ILogger<ContentLoaderService> _logger;
        private readonly Dictionary<string, Type> _categoryTypeMap = new Dictionary<string, Type>
        {
            { Constants.SubCategory.ChineseCharacter, typeof(ChineseCharacter) },
            { Constants.SubCategory.ChineseWordCombination, typeof(ChineseWordCombination) },
            { Constants.SubCategory.ChinesePhrase, typeof(ChinesePhrase) },
            { Constants.SubCategory.ChineseIdiom, typeof(ChineseIdiom) },
            { Constants.SubCategory.ChinesePoem, typeof(ChinesePoem) },
            { Constants.SubCategory.EnglishWord, typeof(EnglishWord) },
            { Constants.SubCategory.EnglishPhrase, typeof(EnglishPhrase) },
            { Constants.SubCategory.EnglishSentence, typeof(EnglishSentence) }
        };

        private readonly Dictionary<string, string> _categoryFileMap = new Dictionary<string, string>
        {
            { Constants.SubCategory.ChineseCharacter, Constants.FileName.ChineseCharacter },
            { Constants.SubCategory.ChineseWordCombination, Constants.FileName.ChineseWordCombination },
            { Constants.SubCategory.ChineseIdiom, Constants.FileName.ChineseIdiom },
            { Constants.SubCategory.ChinesePhrase, Constants.FileName.ChinesePhrase },
            { Constants.SubCategory.ChinesePoem, Constants.FileName.ChinesePoem },
            { Constants.SubCategory.EnglishWord, Constants.FileName.EnglishWord },
            { Constants.SubCategory.EnglishPhrase, Constants.FileName.EnglishPhrase },
            { Constants.SubCategory.EnglishSentence, Constants.FileName.EnglishSentence }
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
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                    return new List<LearningItem>();
                }

                var itemType = GetItemType(subCategory);
                var items = JsonHelper.LoadFromFile<List<object>>(filePath);

                if (items == null)
                {
                    _logger.LogWarning("No items loaded from file: {FilePath}", filePath);
                    return new List<LearningItem>();
                }

                return items.Select(obj => ConvertToLearningItem(obj, itemType))
                           .Where(item => item != null)
                           .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load items for subCategory: {SubCategory}", subCategory);
                return new List<LearningItem>();
            }
        }

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
                _logger.LogError(ex, "Failed to save items for subCategory: {SubCategory}", subCategory);
            }
        }

        public List<string> GetSubCategories(string language)
        {
            if (language == Constants.Language.Chinese)
            {
                return new List<string>
                {
                    Constants.SubCategory.ChineseCharacter,
                    Constants.SubCategory.ChineseWordCombination,
                    Constants.SubCategory.ChineseIdiom,
                    Constants.SubCategory.ChinesePhrase,
                    Constants.SubCategory.ChinesePoem
                };
            }
            else
            {
                return new List<string>
                {
                    Constants.SubCategory.EnglishWord,
                    Constants.SubCategory.EnglishPhrase,
                    Constants.SubCategory.EnglishSentence
                };
            }
        }

        public List<string> GetWordBankFiles(string subCategory)
        {
            try
            {
                var dataDir = FileHelper.GetDataDirectory();
                var defaultFile = _categoryFileMap.GetValueOrDefault(subCategory, "");
                
                var categoryPrefix = GetCategoryFilePrefix(subCategory);
                
                var files = Directory.EnumerateFiles(dataDir, "*.json")
                                   .Select(Path.GetFileName)
                                   .Where(file => file.StartsWith(categoryPrefix, StringComparison.OrdinalIgnoreCase))
                                   .ToList();
                
                if (!string.IsNullOrWhiteSpace(defaultFile) && !files.Contains(defaultFile))
                {
                    files.Add(defaultFile);
                }
                
                files.Sort();
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get word bank files for subCategory: {SubCategory}", subCategory);
                return new List<string>();
            }
        }

        private string GetCategoryFilePrefix(string subCategory)
        {
            return subCategory switch
            {
                Constants.SubCategory.ChineseCharacter => "识字",
                Constants.SubCategory.ChineseWordCombination => "组词",
                Constants.SubCategory.ChinesePhrase => "短语",
                Constants.SubCategory.ChineseIdiom => "成语",
                Constants.SubCategory.ChinesePoem => "诗词",
                Constants.SubCategory.EnglishWord => "英语单词",
                Constants.SubCategory.EnglishPhrase => "英语短语",
                Constants.SubCategory.EnglishSentence => "英语句子",
                _ => ""
            };
        }

        public string GetDefaultWordBankFile(string subCategory)
        {
            return _categoryFileMap.GetValueOrDefault(subCategory, "");
        }

        public Type GetItemType(string subCategory)
        {
            return _categoryTypeMap.GetValueOrDefault(subCategory, typeof(LearningItem));
        }

        private string GetFilePath(string subCategory, string wordBankFile)
        {
            if (!string.IsNullOrWhiteSpace(wordBankFile))
            {
                return Path.Combine(FileHelper.GetDataDirectory(), wordBankFile);
            }
            return Path.Combine(FileHelper.GetDataDirectory(), _categoryFileMap.GetValueOrDefault(subCategory, "data.json"));
        }

        private LearningItem? ConvertToLearningItem(object obj, Type targetType)
        {
            try
            {
                var json = JsonHelper.Serialize(obj);
                return (LearningItem?)System.Text.Json.JsonSerializer.Deserialize(json, targetType,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert object to {TargetType}", targetType.Name);
                return null;
            }
        }
    }
}
