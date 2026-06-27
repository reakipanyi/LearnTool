using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习数据导入导出服务实现
    /// </summary>
    public class DataImportService : IDataImportService
    {
        private readonly ILogger<DataImportService>? _logger;

        public DataImportService(ILogger<DataImportService>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
                var items = JsonSerializer.Deserialize<List<LearningItem>>(json);

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

        /// <inheritdoc/>
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

                var firstItem = items[0];
                var fields = GetItemFields(firstItem);

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

        /// <inheritdoc/>
        public bool ExportToJson(string filePath, List<LearningItem> items, ExportOptions options)
        {
            try
            {
                var json = JsonSerializer.Serialize(items, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(filePath, json);
                _logger?.LogInformation($"JSON导出完成: {items.Count}条 -> {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "JSON导出失败");
                return false;
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        #region 私有方法

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
                "general" or "generalsubject" => ParseGeneralSubject(values, headers, options),
                _ => ParseGeneralSubject(values, headers, options)
            };
        }

        private EnglishWord ParseEnglishWord(string[] values, string[] headers, ImportOptions options)
        {
            var word = new EnglishWord();

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
                            word.Word = value;
                            break;
                        case "phonetic":
                        case "音标":
                            word.Phonetic = value;
                            break;
                        case "meaning":
                        case "释义":
                        case "意思":
                            word.Meaning = value;
                            break;
                        case "example":
                        case "例句":
                            word.Example = value;
                            break;
                        case "partofspeech":
                        case "词性":
                            word.PartOfSpeech = value;
                            break;
                    }
                }
            }
            else if (values.Length >= 2)
            {
                word.Word = values[0].Trim();
                word.Meaning = values[1].Trim();
                if (values.Length >= 3)
                    word.Phonetic = values[2].Trim();
                if (values.Length >= 4)
                    word.Example = values[3].Trim();
            }

            return word;
        }

        private ChineseCharacter ParseChineseCharacter(string[] values, string[] headers, ImportOptions options)
        {
            var character = new ChineseCharacter();

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
                            character.Character = value;
                            break;
                        case "pinyin":
                        case "拼音":
                            character.Pinyin = value;
                            break;
                        case "meaning":
                        case "释义":
                        case "意思":
                            character.Meaning = value;
                            break;
                        case "strokecount":
                        case "笔画":
                        case "笔画数":
                            character.StrokeCount = value;
                            break;
                        case "radical":
                        case "部首":
                            character.Radical = value;
                            break;
                        case "words":
                        case "组词":
                            character.Words = value;
                            break;
                    }
                }
            }
            else if (values.Length >= 2)
            {
                character.Character = values[0].Trim();
                character.Pinyin = values[1].Trim();
                if (values.Length >= 3)
                    character.Meaning = values[2].Trim();
                if (values.Length >= 4)
                    character.Words = values[3].Trim();
            }

            return character;
        }

        private GeneralSubjectItem ParseGeneralSubject(string[] values, string[] headers, ImportOptions options)
        {
            var item = new GeneralSubjectItem
            {
                Subject = options.Subject,
                Category = options.Category
            };

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
                                item.Topic = value;
                                break;
                            case "content":
                            case "内容":
                                item.Content = value;
                                break;
                            case "keypoints":
                            case "要点":
                            case "知识点":
                                item.KeyPoints = value;
                                break;
                            case "example":
                            case "例子":
                            case "例题":
                                item.Example = value;
                                break;
                            case "question":
                            case "问题":
                                item.Question = value;
                                break;
                            case "answer":
                            case "答案":
                                item.Answer = value;
                                break;
                            case "analysis":
                            case "解析":
                                item.Analysis = value;
                                break;
                            case "note":
                            case "备注":
                                item.Note = value;
                                break;
                            case "timeperiod":
                            case "时间":
                            case "时期":
                            case "年代":
                                item.TimePeriod = value;
                                break;
                            case "relatedpeople":
                            case "人物":
                            case "相关人物":
                                item.RelatedPeople = value;
                                break;
                            case "relatedplaces":
                            case "地点":
                            case "相关地点":
                                item.RelatedPlaces = value;
                                break;
                            case "background":
                            case "背景":
                                item.Background = value;
                                break;
                            case "impact":
                            case "影响":
                            case "意义":
                                item.Impact = value;
                                break;
                            case "principle":
                            case "原理":
                                item.Principle = value;
                                break;
                            case "experimentsteps":
                            case "实验步骤":
                                item.ExperimentSteps = value;
                                break;
                            case "applications":
                            case "应用":
                                item.Applications = value;
                                break;
                            case "furtherreading":
                            case "延伸阅读":
                                item.FurtherReading = value;
                                break;
                            case "funfact":
                            case "趣味知识":
                            case "冷知识":
                                item.FunFact = value;
                                break;
                            case "imagedescription":
                            case "图片描述":
                                item.ImageDescription = value;
                                break;
                            case "tags":
                            case "标签":
                                item.Tags = value;
                                break;
                        }
                }
            }
            else if (values.Length >= 2)
            {
                item.Topic = values[0].Trim();
                item.Content = values[1].Trim();
                if (values.Length >= 3)
                    item.KeyPoints = values[2].Trim();
            }

            return item;
        }

        private LearningItem? CreateLearningItemFromAnki(string front, string back, ImportOptions options)
        {
            var contentType = options.ContentType.ToLower();

            return contentType switch
            {
                "englishword" => new EnglishWord
                {
                    Word = front,
                    Meaning = back
                },
                "chinesecharacter" => new ChineseCharacter
                {
                    Character = front,
                    Meaning = back
                },
                _ => new GeneralSubjectItem
                {
                    Topic = front,
                    Content = back,
                    Subject = options.Subject,
                    Category = options.Category
                }
            };
        }

        private Dictionary<string, Func<LearningItem, string>> GetItemFields(LearningItem item)
        {
            var fields = new Dictionary<string, Func<LearningItem, string>>();

            if (item is EnglishWord word)
            {
                fields["Word"] = i => ((EnglishWord)i).Word;
                fields["Phonetic"] = i => ((EnglishWord)i).Phonetic;
                fields["Meaning"] = i => ((EnglishWord)i).Meaning;
                fields["Example"] = i => ((EnglishWord)i).Example;
                fields["PartOfSpeech"] = i => ((EnglishWord)i).PartOfSpeech;
            }
            else if (item is ChineseCharacter character)
            {
                fields["Character"] = i => ((ChineseCharacter)i).Character;
                fields["Pinyin"] = i => ((ChineseCharacter)i).Pinyin;
                fields["Meaning"] = i => ((ChineseCharacter)i).Meaning;
                fields["StrokeCount"] = i => ((ChineseCharacter)i).StrokeCount;
                fields["Radical"] = i => ((ChineseCharacter)i).Radical;
                fields["Words"] = i => ((ChineseCharacter)i).Words ?? string.Empty;
            }
            else if (item is GeneralSubjectItem general)
            {
                fields["Topic"] = i => ((GeneralSubjectItem)i).Topic;
                fields["Content"] = i => ((GeneralSubjectItem)i).Content;
                fields["KeyPoints"] = i => ((GeneralSubjectItem)i).KeyPoints;
                fields["Example"] = i => ((GeneralSubjectItem)i).Example;
                fields["Question"] = i => ((GeneralSubjectItem)i).Question;
                fields["Answer"] = i => ((GeneralSubjectItem)i).Answer;
                fields["Analysis"] = i => ((GeneralSubjectItem)i).Analysis;
                fields["Note"] = i => ((GeneralSubjectItem)i).Note;
                fields["Subject"] = i => ((GeneralSubjectItem)i).Subject;
                fields["Category"] = i => ((GeneralSubjectItem)i).Category;
                fields["TimePeriod"] = i => ((GeneralSubjectItem)i).TimePeriod;
                fields["RelatedPeople"] = i => ((GeneralSubjectItem)i).RelatedPeople;
                fields["RelatedPlaces"] = i => ((GeneralSubjectItem)i).RelatedPlaces;
                fields["Background"] = i => ((GeneralSubjectItem)i).Background;
                fields["Impact"] = i => ((GeneralSubjectItem)i).Impact;
                fields["Principle"] = i => ((GeneralSubjectItem)i).Principle;
                fields["ExperimentSteps"] = i => ((GeneralSubjectItem)i).ExperimentSteps;
                fields["Applications"] = i => ((GeneralSubjectItem)i).Applications;
                fields["FurtherReading"] = i => ((GeneralSubjectItem)i).FurtherReading;
                fields["FunFact"] = i => ((GeneralSubjectItem)i).FunFact;
                fields["ImageDescription"] = i => ((GeneralSubjectItem)i).ImageDescription;
                fields["Tags"] = i => ((GeneralSubjectItem)i).Tags;
            }
            else
            {
                fields["MainContent"] = i => i.GetMainContent();
                fields["DisplayText"] = i => i.GetDisplayText();
            }

            return fields;
        }

        private void SaveImportedItems(List<LearningItem> items, ImportOptions options)
        {
            // 这里可以添加将导入的数据保存到用户内容库的逻辑
            // 目前先记录日志
            _logger?.LogInformation($"已导入 {items.Count} 条 {options.ContentType} 数据");
        }

        #endregion
    }
}
