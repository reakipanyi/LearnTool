
using Microsoft.Extensions.Logging;
using LearningAssistant.Common;
using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    public class ContentLoaderService : IContentLoaderService
    {
        private readonly ILogger<ContentLoaderService> _logger;
        private readonly Dictionary<string, Type> _categoryTypeMap = new Dictionary<string, Type>
        {
            { Constants.SubCategory.ChineseCharacter, typeof(ChineseCharacter) },
            { Constants.SubCategory.ChinesePhrase, typeof(ChinesePhrase) },
            { Constants.SubCategory.ChineseIdiom, typeof(ChineseIdiom) },
            { Constants.SubCategory.ChinesePoem, typeof(ChinesePoem) },
            { Constants.SubCategory.ChineseComprehensive, typeof(ChineseComprehensive) },
            { Constants.SubCategory.EnglishWord, typeof(EnglishWord) },
            { Constants.SubCategory.EnglishPhrase, typeof(EnglishPhrase) },
            { Constants.SubCategory.EnglishSentence, typeof(EnglishSentence) },
            { Constants.SubCategory.EnglishComprehensive, typeof(EnglishComprehensive) }
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
            { Constants.SubCategory.EnglishComprehensive, Constants.FileName.EnglishComprehensive }
        };

        public ContentLoaderService(ILogger<ContentLoaderService> logger)
        {
            _logger = logger;
        }

        public List<object> LoadItems(string subCategory, string wordBankFile = "")
        {
            try
            {
                string filePath = GetFilePath(subCategory, wordBankFile);
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                    return new List<object>();
                }

                var itemType = GetItemType(subCategory);
                var json = File.ReadAllText(filePath);

                // 直接反序列化为具体类型列表，而不是 List<object>
                var listType = typeof(List<>).MakeGenericType(itemType);
                var items = System.Text.Json.JsonSerializer.Deserialize(json, listType,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                if (items == null)
                {
                    _logger.LogWarning("No items loaded from file: {FilePath}", filePath);
                    return new List<object>();
                }

                return ((System.Collections.IList)items).Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load items for subCategory: {SubCategory}", subCategory);
                return new List<object>();
            }
        }

        public void SaveItems(string subCategory, List<object> items, string wordBankFile = "")
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
                    Constants.SubCategory.ChineseIdiom,
                    Constants.SubCategory.ChinesePhrase,
                    Constants.SubCategory.ChinesePoem,
                    Constants.SubCategory.ChineseComprehensive
                };
            }
            else
            {
                return new List<string>
                {
                    Constants.SubCategory.EnglishWord,
                    Constants.SubCategory.EnglishPhrase,
                    Constants.SubCategory.EnglishSentence,
                    Constants.SubCategory.EnglishComprehensive
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
                Constants.SubCategory.ChinesePhrase => "短语",
                Constants.SubCategory.ChineseIdiom => "成语",
                Constants.SubCategory.ChinesePoem => "诗词",
                Constants.SubCategory.ChineseComprehensive => "语文综合",
                Constants.SubCategory.EnglishWord => "英语单词",
                Constants.SubCategory.EnglishPhrase => "英语短语",
                Constants.SubCategory.EnglishSentence => "英语句子",
                Constants.SubCategory.EnglishComprehensive => "英语综合",
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

        public void SaveUserContent(UserContent content)
        {
            try
            {
                var userContentDir = Path.Combine(FileHelper.GetDataDirectory(), "UserContent");
                Directory.CreateDirectory(userContentDir);
                
                var filePath = Path.Combine(userContentDir, $"{content.UserId}_{content.Id}.json");
                JsonHelper.SaveToFile(filePath, content);
                _logger.LogInformation("Saved user content: {Title}", content.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save user content");
            }
        }

        private string GetFilePath(string subCategory, string wordBankFile)
        {
            if (!string.IsNullOrWhiteSpace(wordBankFile))
            {
                return Path.Combine(FileHelper.GetDataDirectory(), wordBankFile);
            }
            return Path.Combine(FileHelper.GetDataDirectory(), _categoryFileMap.GetValueOrDefault(subCategory, "data.json"));
        }

    }
}

