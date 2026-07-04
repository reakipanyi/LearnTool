using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Learning.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;

namespace LearningAssistant.Services.Learning
{
    public class DataImportService : IDataImportService
    {
        private readonly ILogger<DataImportService>? _logger;

        public DataImportService(ILogger<DataImportService>? logger = null)
        {
            _logger = logger;
        }

        public ImportResult ImportFromCsv(string filePath, ImportOptions options)
        {
            var result = new ImportResult
            {
                ContentType = options.ContentType,
                Success = false
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Errors.Add($"文件不存在: {filePath}");
                    return result;
                }

                var encoding = GetEncoding(options.Encoding);
                var lines = File.ReadAllLines(filePath, encoding);

                if (lines.Length == 0)
                {
                    result.Errors.Add("文件为空");
                    return result;
                }

                int startIndex = options.HasHeaderRow ? 1 : 0;
                result.TotalCount = lines.Length - startIndex;

                var importedItems = new List<LearningItem>();
                var headers = options.HasHeaderRow
                    ? ParseCsvLine(lines[0], options.CsvDelimiter)
                    : Array.Empty<string>();

                for (int i = startIndex; i < lines.Length; i++)
                {
                    try
                    {
                        var values = ParseCsvLine(lines[i], options.CsvDelimiter);
                        var item = ParseLearningItem(values, headers, options);

                        if (item != null)
                        {
                            importedItems.Add(item);
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                            result.Warnings.Add($"第 {i + 1} 行: 数据格式不正确，已跳过");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"第 {i + 1} 行: {ex.Message}");
                    }
                }

                SaveImportedItems(importedItems, options);
                result.Success = true;
                _logger?.LogInformation($"CSV导入完成: 成功{result.SuccessCount}条, 失败{result.FailedCount}条, 跳过{result.SkippedCount}条");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"导入失败: {ex.Message}");
                _logger?.LogError(ex, "CSV导入失败");
            }

            return result;
        }

        public ImportResult ImportFromAnki(string filePath, ImportOptions options)
        {
            var result = new ImportResult
            {
                ContentType = options.ContentType,
                Success = false
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Errors.Add($"文件不存在: {filePath}");
                    return result;
                }

                var encoding = GetEncoding(options.Encoding);
                var lines = File.ReadAllLines(filePath, encoding);

                if (lines.Length == 0)
                {
                    result.Errors.Add("文件为空");
                    return result;
                }

                result.TotalCount = lines.Length;
                var importedItems = new List<LearningItem>();

                for (int i = 0; i < lines.Length; i++)
                {
                    try
                    {
                        var line = lines[i];
                        var parts = line.Split('\t');

                        if (parts.Length < 2)
                        {
                            result.SkippedCount++;
                            continue;
                        }

                        var front = parts[0].Trim();
                        var back = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                        if (string.IsNullOrWhiteSpace(front))
                        {
                            result.SkippedCount++;
                            continue;
                        }

                        var item = CreateLearningItemFromAnki(front, back, options);
                        if (item != null)
                        {
                            importedItems.Add(item);
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"第 {i + 1} 行: {ex.Message}");
                    }
                }

                SaveImportedItems(importedItems, options);
                result.Success = true;
                _logger?.LogInformation($"Anki导入完成: 成功{result.SuccessCount}条, 失败{result.FailedCount}条, 跳过{result.SkippedCount}条");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"导入失败: {ex.Message}");
                _logger?.LogError(ex, "Anki导入失败");
            }

            return result;
        }

        public ImportResult ImportFromJson(string filePath, ImportOptions options)
        {
            var result = new ImportResult
            {
                ContentType = options.ContentType,
                Success = false
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Errors.Add($"文件不存在: {filePath}");
                    return result;
                }

                var json = File.ReadAllText(filePath);
                var items = JsonHelper.DeserializeLearningItems(json);

                if (items == null || items.Count == 0)
                {
                    result.Errors.Add("JSON文件中没有有效数据");
                    return result;
                }

                result.TotalCount = items.Count;
                result.SuccessCount = items.Count;

                SaveImportedItems(items, options);
                result.Success = true;
                _logger?.LogInformation($"JSON导入完成: 成功{result.SuccessCount}条");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"导入失败: {ex.Message}");
                _logger?.LogError(ex, "JSON导入失败");
            }

            return result;
        }

        public bool ExportToCsv(string filePath, List<LearningItem> items, ExportOptions options)
        {
            try
            {
                var encoding = GetEncoding(options.Encoding);
                var lines = new List<string>();

                if (items.Count == 0)
                {
                    File.WriteAllText(filePath, string.Empty, encoding);
                    return true;
                }

                var fields = GetItemFields();

                if (options.IncludeHeader)
                {
                    lines.Add(string.Join(options.CsvDelimiter, fields.Select(f => EscapeCsvValue(f.Key))));
                }

                foreach (var item in items)
                {
                    var values = fields.Select(f =>
                    {
                        var value = f.Value(item);
                        return EscapeCsvValue(value ?? string.Empty);
                    });
                    lines.Add(string.Join(options.CsvDelimiter, values));
                }

                File.WriteAllLines(filePath, lines, encoding);
                _logger?.LogInformation($"CSV导出完成: {items.Count}条 -> {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CSV导出失败");
                return false;
            }
        }

        public bool ExportToJson(string filePath, List<LearningItem> items, ExportOptions options)
        {
            try
            {
                JsonHelper.SaveToFile(filePath, items);
                _logger?.LogInformation($"JSON导出完成: {items.Count}条 -> {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "JSON导出失败");
                return false;
            }
        }

        public List<string[]> PreviewCsv(string filePath, int rowCount = 5)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new List<string[]>();

                var lines = File.ReadLines(filePath).Take(rowCount).ToList();
                var result = new List<string[]>();

                foreach (var line in lines)
                {
                    result.Add(ParseCsvLine(line, ","));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "预览CSV失败");
                return new List<string[]>();
            }
        }

        public List<string> GetSupportedContentTypes()
        {
            return new List<string>
            {
                "EnglishWord",
                "ChineseCharacter",
                "ChinesePhrase",
                "EnglishPhrase",
                "ChineseIdiom",
                "ChinesePoem",
                "GrammarRule",
                "GeneralSubject"
            };
        }

        public List<string> GetContentTypeFields(string contentType)
        {
            return contentType switch
            {
                "EnglishWord" => new List<string> { "Word", "Phonetic", "Meaning", "Example", "PartOfSpeech" },
                "ChineseCharacter" => new List<string> { "Character", "Pinyin", "Meaning", "StrokeCount", "Radical", "Words" },
                "ChinesePhrase" => new List<string> { "Phrase", "Pinyin", "Meaning", "Example" },
                "EnglishPhrase" => new List<string> { "Phrase", "Meaning", "Example" },
                "ChineseIdiom" => new List<string> { "Idiom", "Pinyin", "Meaning", "Story", "Example" },
                "ChinesePoem" => new List<string> { "Title", "Author", "Dynasty", "Verses", "Translation" },
                "GrammarRule" => new List<string> { "Title", "Category", "Definition", "Example1", "Example2" },
                "GeneralSubject" => new List<string> { "Topic", "Content", "KeyPoints", "Example" },
                _ => new List<string> { "Title", "Content" }
            };
        }

        #region Private Methods

        private string[] ParseCsvLine(string line, string delimiter)
        {
            if (string.IsNullOrEmpty(line))
                return Array.Empty<string>();

            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (delimiter.Contains(c))
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private Encoding GetEncoding(string encodingName)
        {
            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        private LearningItem? ParseLearningItem(string[] values, string[] headers, ImportOptions options)
        {
            var contentType = options.ContentType.ToLower();

            return contentType switch
            {
                "englishword" => ParseEnglishWord(values, headers, options),
                "chinesecharacter" => ParseChineseCharacter(values, headers, options),
                _ => ParseGeneralSubject(values, headers, options)
            };
        }

        private LearningItem ParseEnglishWord(string[] values, string[] headers, ImportOptions options)
        {
            var item = new LearningItem
            {
                Id = Guid.NewGuid().ToString(),
                Subject = SubjectType.English,
                SubCategory = SubCategoryType.EnglishWord,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            string word = string.Empty;
            string phonetic = string.Empty;
            string meaning = string.Empty;
            string example = string.Empty;
            string partOfSpeech = string.Empty;

            if (headers.Length > 0 && values.Length > 0)
            {
                for (int i = 0; i < Math.Min(headers.Length, values.Length); i++)
                {
                    var header = headers[i].Trim().ToLower();
                    var value = values[i].Trim();

                    switch (header)
                    {
                        case "word":
                        case "单词":
                            word = value;
                            break;
                        case "phonetic":
                        case "音标":
                            phonetic = value;
                            break;
                        case "meaning":
                        case "释义":
                        case "意思":
                            meaning = value;
                            break;
                        case "example":
                        case "例句":
                            example = value;
                            break;
                        case "partofspeech":
                        case "词性":
                            partOfSpeech = value;
                            break;
                    }
                }
            }
            else if (values.Length >= 2)
            {
                word = values[0].Trim();
                meaning = values[1].Trim();
                if (values.Length >= 3)
                    phonetic = values[2].Trim();
                if (values.Length >= 4)
                    example = values[3].Trim();
            }

            item.MainContent = word;
            item.Meaning = Meaning.Create(meaning);
            item.Pronunciation = Pronunciation.Create(phonetic);
            item.WordFeatures = WordFeatures.Create(partOfSpeech);

            if (!string.IsNullOrWhiteSpace(example))
                item.Example = Example.Create(example);

            return item;
        }

        private LearningItem ParseChineseCharacter(string[] values, string[] headers, ImportOptions options)
        {
            var item = new LearningItem
            {
                Id = Guid.NewGuid().ToString(),
                Subject = SubjectType.Chinese,
                SubCategory = SubCategoryType.ChineseCharacter,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            string character = string.Empty;
            string pinyin = string.Empty;
            string meaning = string.Empty;
            string strokeCount = string.Empty;
            string radical = string.Empty;
            string words = string.Empty;

            if (headers.Length > 0 && values.Length > 0)
            {
                for (int i = 0; i < Math.Min(headers.Length, values.Length); i++)
                {
                    var header = headers[i].Trim().ToLower();
                    var value = values[i].Trim();

                    switch (header)
                    {
                        case "character":
                        case "汉字":
                        case "字":
                            character = value;
                            break;
                        case "pinyin":
                        case "拼音":
                            pinyin = value;
                            break;
                        case "meaning":
                        case "释义":
                        case "意思":
                            meaning = value;
                            break;
                        case "strokecount":
                        case "笔画":
                        case "笔画数":
                            strokeCount = value;
                            break;
                        case "radical":
                        case "部首":
                            radical = value;
                            break;
                        case "words":
                        case "组词":
                            words = value;
                            break;
                    }
                }
            }
            else if (values.Length >= 2)
            {
                character = values[0].Trim();
                pinyin = values[1].Trim();
                if (values.Length >= 3)
                    meaning = values[2].Trim();
                if (values.Length >= 4)
                    words = values[3].Trim();
            }

            item.MainContent = character;
            item.Meaning = Meaning.Create(meaning);
            item.Pronunciation = Pronunciation.Create(pinyin);
            item.CharacterFeatures = CharacterFeatures.Create(strokeCount, radical, "");

            if (!string.IsNullOrWhiteSpace(words))
                item.SetExtendedProperty("Words", words);

            return item;
        }

        private LearningItem ParseGeneralSubject(string[] values, string[] headers, ImportOptions options)
        {
            var item = new LearningItem
            {
                Id = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            if (Enum.TryParse(options.Subject, out SubjectType subject))
                item.Subject = subject;
            if (Enum.TryParse(options.Category, out SubCategoryType subCategory))
                item.SubCategory = subCategory;

            string topic = string.Empty;
            string content = string.Empty;

            if (headers.Length > 0 && values.Length > 0)
            {
                for (int i = 0; i < Math.Min(headers.Length, values.Length); i++)
                {
                    var header = headers[i].Trim().ToLower();
                    var value = values[i].Trim();

                    switch (header)
                    {
                        case "topic":
                        case "主题":
                        case "标题":
                            topic = value;
                            break;
                        case "content":
                        case "内容":
                            content = value;
                            break;
                        case "keypoints":
                        case "要点":
                        case "知识点":
                            item.SetExtendedProperty("KeyPoints", value);
                            break;
                        case "example":
                        case "例子":
                        case "例题":
                            item.Example = Example.Create(value);
                            break;
                        case "question":
                        case "问题":
                            item.SetExtendedProperty("Question", value);
                            break;
                        case "answer":
                        case "答案":
                            item.SetExtendedProperty("Answer", value);
                            break;
                        case "analysis":
                        case "解析":
                            item.SetExtendedProperty("Analysis", value);
                            break;
                        case "note":
                        case "备注":
                            item.SetExtendedProperty("Note", value);
                            break;
                        case "timeperiod":
                        case "时间":
                        case "时期":
                        case "年代":
                            item.SetExtendedProperty("TimePeriod", value);
                            break;
                        case "relatedpeople":
                        case "人物":
                        case "相关人物":
                            item.SetExtendedProperty("RelatedPeople", value);
                            break;
                        case "relatedplaces":
                        case "地点":
                        case "相关地点":
                            item.SetExtendedProperty("RelatedPlaces", value);
                            break;
                        case "background":
                        case "背景":
                            item.SetExtendedProperty("Background", value);
                            break;
                        case "impact":
                        case "影响":
                        case "意义":
                            item.SetExtendedProperty("Impact", value);
                            break;
                        case "principle":
                        case "原理":
                            item.SetExtendedProperty("Principle", value);
                            break;
                        case "experimentsteps":
                        case "实验步骤":
                            item.SetExtendedProperty("ExperimentSteps", value);
                            break;
                        case "applications":
                        case "应用":
                            item.SetExtendedProperty("Applications", value);
                            break;
                        case "furtherreading":
                        case "延伸阅读":
                            item.SetExtendedProperty("FurtherReading", value);
                            break;
                        case "funfact":
                        case "趣味知识":
                        case "冷知识":
                            item.SetExtendedProperty("FunFact", value);
                            break;
                        case "imagedescription":
                        case "图片描述":
                            item.SetExtendedProperty("ImageDescription", value);
                            break;
                        case "tags":
                        case "标签":
                            item.SetExtendedProperty("Tags", value);
                            break;
                    }
                }
            }
            else if (values.Length >= 2)
            {
                topic = values[0].Trim();
                content = values[1].Trim();
            }

            item.MainContent = topic;
            item.Meaning = Meaning.Create(content);

            return item;
        }

        private LearningItem? CreateLearningItemFromAnki(string front, string back, ImportOptions options)
        {
            var contentType = options.ContentType.ToLower();

            var item = new LearningItem
            {
                Id = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            switch (contentType)
            {
                case "englishword":
                    item.Subject = SubjectType.English;
                    item.SubCategory = SubCategoryType.EnglishWord;
                    item.MainContent = front;
                    item.Meaning = Meaning.Create(back);
                    break;
                case "chinesecharacter":
                    item.Subject = SubjectType.Chinese;
                    item.SubCategory = SubCategoryType.ChineseCharacter;
                    item.MainContent = front;
                    item.Meaning = Meaning.Create(back);
                    break;
                default:
                    if (Enum.TryParse(options.Subject, out SubjectType subject))
                        item.Subject = subject;
                    if (Enum.TryParse(options.Category, out SubCategoryType subCategory))
                        item.SubCategory = subCategory;
                    item.MainContent = front;
                    item.Meaning = Meaning.Create(back);
                    break;
            }

            return item;
        }

        private Dictionary<string, Func<LearningItem, string>> GetItemFields()
        {
            var fields = new Dictionary<string, Func<LearningItem, string>>();

            fields["MainContent"] = i => i.MainContent;
            fields["Meaning"] = i => i.Meaning?.Content ?? string.Empty;
            fields["Pronunciation"] = i => i.Pronunciation?.Main ?? string.Empty;
            fields["Example"] = i => i.Example?.Content ?? string.Empty;
            fields["ExampleTranslation"] = i => i.Example?.Translation ?? string.Empty;
            fields["PartOfSpeech"] = i => i.WordFeatures?.PartOfSpeech ?? string.Empty;
            fields["WordForms"] = i => i.WordFeatures?.WordForms ?? string.Empty;
            fields["StrokeCount"] = i => i.CharacterFeatures?.StrokeCount ?? string.Empty;
            fields["Radical"] = i => i.CharacterFeatures?.Radical ?? string.Empty;
            fields["Structure"] = i => i.CharacterFeatures?.Structure ?? string.Empty;
            fields["Subject"] = i => i.Subject.ToString();
            fields["SubCategory"] = i => i.SubCategory.ToString();

            return fields;
        }

        private void SaveImportedItems(List<LearningItem> items, ImportOptions options)
        {
            _logger?.LogInformation($"已导入 {items.Count} 条 {options.ContentType} 数据");
        }

        #endregion
    }
}