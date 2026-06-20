
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
            { Constants.SubCategory.EnglishComprehensive, typeof(EnglishComprehensive) },
            { Constants.SubCategory.MathFormula, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.MathExample, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.MathConcept, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.MathComprehensive, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.PhysicsLaw, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.PhysicsExperiment, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.PhysicsDerivation, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.PhysicsComprehensive, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.ChemistryEquation, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.ChemistryElement, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.ChemistryExperiment, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.ChemistryComprehensive, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.HistoryEvent, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.HistoryPerson, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.HistoryTimeline, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.HistoryComprehensive, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.GeographyKnowledge, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.GeographyMap, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.GeographyClimate, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.GeographyComprehensive, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.BiologyConcept, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.BiologyExperiment, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.BiologyPhenomenon, typeof(GeneralSubjectItem) },
            { Constants.SubCategory.BiologyComprehensive, typeof(GeneralSubjectItem) }
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

                var itemType = GetItemType(subCategory);
                var json = File.ReadAllText(filePath);

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
                    return new List<LearningItem>();
                }

                return ((System.Collections.IList)items).Cast<LearningItem>().ToList();
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

        public List<string> GetSubCategoriesBySubject(string subject)
        {
            return subject switch
            {
                Constants.Subject.Chinese => new List<string>
                {
                    Constants.SubCategory.ChineseCharacter,
                    Constants.SubCategory.ChineseIdiom,
                    Constants.SubCategory.ChinesePhrase,
                    Constants.SubCategory.ChinesePoem,
                    Constants.SubCategory.ChineseComprehensive
                },
                Constants.Subject.English => new List<string>
                {
                    Constants.SubCategory.EnglishWord,
                    Constants.SubCategory.EnglishPhrase,
                    Constants.SubCategory.EnglishSentence,
                    Constants.SubCategory.EnglishComprehensive
                },
                Constants.Subject.Math => new List<string>
                {
                    Constants.SubCategory.MathFormula,
                    Constants.SubCategory.MathExample,
                    Constants.SubCategory.MathConcept,
                    Constants.SubCategory.MathComprehensive
                },
                Constants.Subject.Physics => new List<string>
                {
                    Constants.SubCategory.PhysicsLaw,
                    Constants.SubCategory.PhysicsExperiment,
                    Constants.SubCategory.PhysicsDerivation,
                    Constants.SubCategory.PhysicsComprehensive
                },
                Constants.Subject.Chemistry => new List<string>
                {
                    Constants.SubCategory.ChemistryEquation,
                    Constants.SubCategory.ChemistryElement,
                    Constants.SubCategory.ChemistryExperiment,
                    Constants.SubCategory.ChemistryComprehensive
                },
                Constants.Subject.History => new List<string>
                {
                    Constants.SubCategory.HistoryEvent,
                    Constants.SubCategory.HistoryPerson,
                    Constants.SubCategory.HistoryTimeline,
                    Constants.SubCategory.HistoryComprehensive
                },
                Constants.Subject.Geography => new List<string>
                {
                    Constants.SubCategory.GeographyKnowledge,
                    Constants.SubCategory.GeographyMap,
                    Constants.SubCategory.GeographyClimate,
                    Constants.SubCategory.GeographyComprehensive
                },
                Constants.Subject.Biology => new List<string>
                {
                    Constants.SubCategory.BiologyConcept,
                    Constants.SubCategory.BiologyExperiment,
                    Constants.SubCategory.BiologyPhenomenon,
                    Constants.SubCategory.BiologyComprehensive
                },
                _ => new List<string>()
            };
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

        public List<string> GetWordBankFiles(string subCategory)
        {
            try
            {
                var dataDir = AppPaths.DataDir;
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

        private string GetFilePath(string subCategory, string wordBankFile)
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

