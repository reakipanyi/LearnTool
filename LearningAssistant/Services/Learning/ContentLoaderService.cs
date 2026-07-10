using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    public class ContentLoaderService : IContentLoaderService
    {
        private readonly ILogger<ContentLoaderService> _logger;

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
            { SubCategoryType.EnglishComprehensive, Constants.FileName.EnglishComprehensive },
            { SubCategoryType.MathFormula, Constants.FileName.MathFormula },
            { SubCategoryType.MathExample, Constants.FileName.MathExample },
            { SubCategoryType.MathConcept, Constants.FileName.MathConcept },
            { SubCategoryType.MathComprehensive, Constants.FileName.MathComprehensive },
            { SubCategoryType.PhysicsLaw, Constants.FileName.PhysicsLaw },
            { SubCategoryType.PhysicsExperiment, Constants.FileName.PhysicsExperiment },
            { SubCategoryType.PhysicsDerivation, Constants.FileName.PhysicsDerivation },
            { SubCategoryType.PhysicsComprehensive, Constants.FileName.PhysicsComprehensive },
            { SubCategoryType.ChemistryEquation, Constants.FileName.ChemistryEquation },
            { SubCategoryType.ChemistryElement, Constants.FileName.ChemistryElement },
            { SubCategoryType.ChemistryExperiment, Constants.FileName.ChemistryExperiment },
            { SubCategoryType.ChemistryComprehensive, Constants.FileName.ChemistryComprehensive },
            { SubCategoryType.HistoryEvent, Constants.FileName.HistoryEvent },
            { SubCategoryType.HistoryPerson, Constants.FileName.HistoryPerson },
            { SubCategoryType.HistoryTimeline, Constants.FileName.HistoryTimeline },
            { SubCategoryType.HistoryComprehensive, Constants.FileName.HistoryComprehensive },
            { SubCategoryType.GeographyKnowledge, Constants.FileName.GeographyKnowledge },
            { SubCategoryType.GeographyMap, Constants.FileName.GeographyMap },
            { SubCategoryType.GeographyClimate, Constants.FileName.GeographyClimate },
            { SubCategoryType.GeographyComprehensive, Constants.FileName.GeographyComprehensive },
            { SubCategoryType.BiologyConcept, Constants.FileName.BiologyConcept },
            { SubCategoryType.BiologyExperiment, Constants.FileName.BiologyExperiment },
            { SubCategoryType.BiologyPhenomenon, Constants.FileName.BiologyPhenomenon },
            { SubCategoryType.BiologyComprehensive, Constants.FileName.BiologyComprehensive }
        };

        public ContentLoaderService(ILogger<ContentLoaderService> logger)
        {
            _logger = logger;
        }

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

        public List<SubCategoryType> GetSubCategories(SubjectType subject)
        {
            return SubjectSubCategoryMapping.GetSubCategories(subject);
        }

        public List<string> GetAllSubjects()
        {
            return new List<string>
            {
                Constants.Subject.Chinese,
                Constants.Subject.English,
                Constants.Subject.Math,
                Constants.Subject.Physics,
                Constants.Subject.Chemistry,
                Constants.Subject.History,
                Constants.Subject.Geography,
                Constants.Subject.Biology
            };
        }

        public List<string> GetWordBankFiles(SubCategoryType subCategory)
        {
            try
            {
                var dataDir = AppPaths.DataDir;
                var defaultFile = _categoryFileMap.GetValueOrDefault(subCategory, "");
                var subCategoryStr = subCategory.ToString();

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

        private string GetCategoryFilePrefix(SubCategoryType subCategory)
        {
            return subCategory switch
            {
                SubCategoryType.ChineseCharacter => "识字",
                SubCategoryType.ChinesePhrase => "短语",
                SubCategoryType.ChineseIdiom => "成语",
                SubCategoryType.ChinesePoem => "诗词",
                SubCategoryType.ChineseComprehensive => "语文综合",
                SubCategoryType.EnglishWord => "英语单词",
                SubCategoryType.EnglishPhrase => "英语短语",
                SubCategoryType.EnglishSentence => "英语句子",
                SubCategoryType.EnglishComprehensive => "英语综合",
                SubCategoryType.MathFormula => "数学公式",
                SubCategoryType.MathExample => "数学例题",
                SubCategoryType.MathConcept => "数学概念",
                SubCategoryType.MathComprehensive => "数学综合",
                SubCategoryType.PhysicsLaw => "物理定律",
                SubCategoryType.PhysicsExperiment => "物理实验",
                SubCategoryType.PhysicsDerivation => "物理推导",
                SubCategoryType.PhysicsComprehensive => "物理综合",
                SubCategoryType.ChemistryEquation => "化学方程式",
                SubCategoryType.ChemistryElement => "化学元素",
                SubCategoryType.ChemistryExperiment => "化学实验",
                SubCategoryType.ChemistryComprehensive => "化学综合",
                SubCategoryType.HistoryEvent => "历史事件",
                SubCategoryType.HistoryPerson => "历史人物",
                SubCategoryType.HistoryTimeline => "历史时间线",
                SubCategoryType.HistoryComprehensive => "历史综合",
                SubCategoryType.GeographyKnowledge => "地理知识",
                SubCategoryType.GeographyMap => "地理地图",
                SubCategoryType.GeographyClimate => "地理气候",
                SubCategoryType.GeographyComprehensive => "地理综合",
                SubCategoryType.BiologyConcept => "生物概念",
                SubCategoryType.BiologyExperiment => "生物实验",
                SubCategoryType.BiologyPhenomenon => "生物现象",
                SubCategoryType.BiologyComprehensive => "生物综合",
                _ => ""
            };
        }

        public string GetDefaultWordBankFile(SubCategoryType subCategory)
        {
            return _categoryFileMap.GetValueOrDefault(subCategory, "");
        }

        public void SaveUserContent(UserContent content)
        {
            try
            {
                var userContentDir = Path.Combine(AppPaths.DataDir, "UserContent");
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

        private string GetFilePath(SubCategoryType subCategory, string wordBankFile)
        {
            if (!string.IsNullOrWhiteSpace(wordBankFile))
            {
                return Path.Combine(AppPaths.DataDir, wordBankFile);
            }
            return Path.Combine(AppPaths.DataDir, _categoryFileMap.GetValueOrDefault(subCategory, "data.json"));
        }

        private bool IsPathSafe(string filePath)
        {
            try
            {
                var dataDir = new DirectoryInfo(AppPaths.DataDir).FullName;
                var fullPath = new FileInfo(filePath).FullName;
                return fullPath.StartsWith(dataDir, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}