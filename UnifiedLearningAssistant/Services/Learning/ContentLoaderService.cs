
using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Learning;

namespace UnifiedLearningAssistant.Services.Learning
{
    public class ContentLoaderService : IContentLoaderService
    {
        private readonly ILogger&lt;ContentLoaderService&gt; _logger;
        private readonly Dictionary&lt;string, Type&gt; _categoryTypeMap = new Dictionary&lt;string, Type&gt;
        {
            { Constants.SubCategory.ChineseCharacter, typeof(ChineseCharacter) },
            { Constants.SubCategory.ChineseWordCombination, typeof(ChineseWordCombination) },
            { Constants.SubCategory.ChinesePhrase, typeof(ChinesePhrase) },
            { Constants.SubCategory.ChineseIdiom, typeof(ChineseIdiom) },
            { Constants.SubCategory.ChinesePoem, typeof(ChinesePoem) },
            { Constants.SubCategory.ChineseComprehensive, typeof(ChineseComprehensive) },
            { Constants.SubCategory.EnglishWord, typeof(EnglishWord) },
            { Constants.SubCategory.EnglishPhrase, typeof(EnglishPhrase) },
            { Constants.SubCategory.EnglishSentence, typeof(EnglishSentence) },
            { Constants.SubCategory.EnglishComprehensive, typeof(EnglishComprehensive) }
        };

        private readonly Dictionary&lt;string, string&gt; _categoryFileMap = new Dictionary&lt;string, string&gt;
        {
            { Constants.SubCategory.ChineseCharacter, Constants.FileName.ChineseCharacter },
            { Constants.SubCategory.ChineseWordCombination, Constants.FileName.ChineseWordCombination },
            { Constants.SubCategory.ChineseIdiom, Constants.FileName.ChineseIdiom },
            { Constants.SubCategory.ChinesePhrase, Constants.FileName.ChinesePhrase },
            { Constants.SubCategory.ChinesePoem, Constants.FileName.ChinesePoem },
            { Constants.SubCategory.ChineseComprehensive, Constants.FileName.ChineseComprehensive },
            { Constants.SubCategory.EnglishWord, Constants.FileName.EnglishWord },
            { Constants.SubCategory.EnglishPhrase, Constants.FileName.EnglishPhrase },
            { Constants.SubCategory.EnglishSentence, Constants.FileName.EnglishSentence },
            { Constants.SubCategory.EnglishComprehensive, Constants.FileName.EnglishComprehensive }
        };

        public ContentLoaderService(ILogger&lt;ContentLoaderService&gt; logger)
        {
            _logger = logger;
        }

        public List&lt;object&gt; LoadItems(string subCategory, string wordBankFile = "")
        {
            try
            {
                string filePath = GetFilePath(subCategory, wordBankFile);
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                    return new List&lt;object&gt;();
                }

                var itemType = GetItemType(subCategory);
                var json = File.ReadAllText(filePath);

                // 直接反序列化为具体类型列表，而不是 List&lt;object&gt;
                var listType = typeof(List&lt;&gt;).MakeGenericType(itemType);
                var items = System.Text.Json.JsonSerializer.Deserialize(json, listType,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                if (items == null)
                {
                    _logger.LogWarning("No items loaded from file: {FilePath}", filePath);
                    return new List&lt;object&gt;();
                }

                return ((System.Collections.IList)items).Cast&lt;object&gt;().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load items for subCategory: {SubCategory}", subCategory);
                return new List&lt;object&gt;();
            }
        }

        public void SaveItems(string subCategory, List&lt;object&gt; items, string wordBankFile = "")
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

        public List&lt;string&gt; GetSubCategories(string language)
        {
            if (language == Constants.Language.Chinese)
            {
                return new List&lt;string&gt;
                {
                    Constants.SubCategory.ChineseCharacter,
                    Constants.SubCategory.ChineseWordCombination,
                    Constants.SubCategory.ChineseIdiom,
                    Constants.SubCategory.ChinesePhrase,
                    Constants.SubCategory.ChinesePoem,
                    Constants.SubCategory.ChineseComprehensive
                };
            }
            else
            {
                return new List&lt;string&gt;
                {
                    Constants.SubCategory.EnglishWord,
                    Constants.SubCategory.EnglishPhrase,
                    Constants.SubCategory.EnglishSentence,
                    Constants.SubCategory.EnglishComprehensive
                };
            }
        }

        public List&lt;string&gt; GetWordBankFiles(string subCategory)
        {
            try
            {
                var dataDir = FileHelper.GetDataDirectory();
                var defaultFile = _categoryFileMap.GetValueOrDefault(subCategory, "");

                var categoryPrefix = GetCategoryFilePrefix(subCategory);

                var files = Directory.EnumerateFiles(dataDir, "*.json")
                                   .Select(Path.GetFileName)
                                   .Where(file =&gt; file.StartsWith(categoryPrefix, StringComparison.OrdinalIgnoreCase))
                                   .ToList();

                if (!string.IsNullOrWhiteSpace(defaultFile) &amp;&amp; !files.Contains(defaultFile))
                {
                    files.Add(defaultFile);
                }

                files.Sort();
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get word bank files for subCategory: {SubCategory}", subCategory);
                return new List&lt;string&gt;();
            }
        }

        private string GetCategoryFilePrefix(string subCategory)
        {
            return subCategory switch
            {
                Constants.SubCategory.ChineseCharacter =&gt; "识字",
                Constants.SubCategory.ChineseWordCombination =&gt; "组词",
                Constants.SubCategory.ChinesePhrase =&gt; "短语",
                Constants.SubCategory.ChineseIdiom =&gt; "成语",
                Constants.SubCategory.ChinesePoem =&gt; "诗词",
                Constants.SubCategory.ChineseComprehensive =&gt; "语文综合",
                Constants.SubCategory.EnglishWord =&gt; "英语单词",
                Constants.SubCategory.EnglishPhrase =&gt; "英语短语",
                Constants.SubCategory.EnglishSentence =&gt; "英语句子",
                Constants.SubCategory.EnglishComprehensive =&gt; "英语综合",
                _ =&gt; ""
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

    }
}

