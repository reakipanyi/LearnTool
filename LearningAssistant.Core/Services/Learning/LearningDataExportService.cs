using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习数据导出服务
    /// </summary>
    public class LearningDataExportService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<LearningDataExportService>? _logger;

        public LearningDataExportService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<LearningDataExportService>? logger = null)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        /// <summary>
        /// 导出复习日志为 CSV 格式
        /// </summary>
        public void ExportReviewLogsToCsv(string userId, string filePath, int days = 30)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var startDate = DateTime.Now.AddDays(-days);

                var logs = db.ReviewLogs
                    .Where(r => r.UserId == userId && r.ReviewTime >= startDate)
                    .OrderByDescending(r => r.ReviewTime)
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine("日期,时间,内容ID,评分,间隔(天),易度因子,稳定性,难度,算法,耗时(秒)");

                foreach (var log in logs)
                {
                    sb.AppendLine($"{log.ReviewTime:yyyy-MM-dd},{log.ReviewTime:HH:mm:ss},{log.ContentId}," +
                                  $"{log.Rating},{log.Interval},{log.EaseFactor:F2},{log.Stability:F2}," +
                                  $"{log.Difficulty:F2},{log.AlgorithmType ?? "SM-2"},{log.Duration / 1000.0:F1}");
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                _logger?.LogInformation("复习日志导出成功: {FilePath}, 共 {Count} 条记录", filePath, logs.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出复习日志失败");
                throw;
            }
        }

        /// <summary>
        /// 导出复习日志为 Excel 兼容格式
        /// </summary>
        public void ExportReviewLogsToExcel(string userId, string filePath, int days = 30)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var startDate = DateTime.Now.AddDays(-days);

                var logs = db.ReviewLogs
                    .Where(r => r.UserId == userId && r.ReviewTime >= startDate)
                    .OrderByDescending(r => r.ReviewTime)
                    .ToList();

                var items = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId && i.IsActive)
                    .ToDictionary(i => i.Id, i => i.Content);

                var sb = new StringBuilder();
                sb.AppendLine("日期\t时间\t内容\t评分\t间隔(天)\t易度因子\t稳定性\t难度\t算法\t耗时(秒)");

                foreach (var log in logs)
                {
                    string content = items.GetValueOrDefault(log.ContentId, "未知");
                    content = content.Replace("\t", " ").Replace("\n", " ").Replace("\r", "");

                    sb.AppendLine($"{log.ReviewTime:yyyy-MM-dd}\t{log.ReviewTime:HH:mm:ss}\t{content}\t" +
                                  $"{log.Rating}\t{log.Interval}\t{log.EaseFactor:F2}\t{log.Stability:F2}\t" +
                                  $"{log.Difficulty:F2}\t{log.AlgorithmType ?? "SM-2"}\t{log.Duration / 1000.0:F1}");
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                _logger?.LogInformation("复习日志导出成功(Excel格式): {FilePath}, 共 {Count} 条记录", filePath, logs.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出复习日志失败");
                throw;
            }
        }

        /// <summary>
        /// 导出学习项为 CSV 格式
        /// </summary>
        public void ExportLearningItemsToCsv(string userId, string filePath)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                var items = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId && i.IsActive)
                    .OrderBy(i => i.Category)
                    .ThenBy(i => i.Content)
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine("分类,内容,答案,当前间隔(天),重复次数,易度因子,稳定性,难度,学习阶段,下次复习日期,最后复习日期");

                foreach (var item in items)
                {
                    sb.AppendLine($"\"{item.Category}\",\"{item.Content}\",\"{item.Answer}\"," +
                                  $"{item.Interval},{item.Repetitions},{item.EFactor:F2}," +
                                  $"{item.Stability:F2},{item.Difficulty:F2}," +
                                  $"{item.LearningStage},{item.NextReviewDate:yyyy-MM-dd}," +
                                  $"{item.LastReviewDate:yyyy-MM-dd}");
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                _logger?.LogInformation("学习项导出成功: {FilePath}, 共 {Count} 条记录", filePath, items.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出学习项失败");
                throw;
            }
        }

        /// <summary>
        /// 导出统计数据摘要
        /// </summary>
        public string ExportStatisticsSummary(string userId, int days = 30)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var startDate = DateTime.Now.AddDays(-days);

                var logs = db.ReviewLogs
                    .Where(r => r.UserId == userId && r.ReviewTime >= startDate)
                    .ToList();

                var items = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId && i.IsActive)
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine($"# 学习统计报告 ({DateTime.Now:yyyy-MM-dd})");
                sb.AppendLine();
                sb.AppendLine("## 概述");
                sb.AppendLine($"- 统计周期: 最近 {days} 天");
                sb.AppendLine($"- 总复习次数: {logs.Count}");
                sb.AppendLine($"- 活跃学习项: {items.Count}");
                sb.AppendLine();

                if (logs.Count > 0)
                {
                    var correctCount = logs.Count(l => l.Rating >= 3);
                    var accuracy = (double)correctCount / logs.Count * 100;

                    var sm2Logs = logs.Where(l => l.AlgorithmType != "FSRS").ToList();
                    var fsrsLogs = logs.Where(l => l.AlgorithmType == "FSRS").ToList();

                    sb.AppendLine("## 复习统计");
                    sb.AppendLine($"- 总复习次数: {logs.Count}");
                    sb.AppendLine($"- 正确次数: {correctCount} ({accuracy:F1}%)");
                    sb.AppendLine($"- 错误次数: {logs.Count - correctCount}");
                    sb.AppendLine();

                    sb.AppendLine("## 算法使用分布");
                    sb.AppendLine($"- SM-2: {sm2Logs.Count} 次 ({sm2Logs.Count * 100.0 / logs.Count:F1}%)");
                    sb.AppendLine($"- FSRS: {fsrsLogs.Count} 次 ({fsrsLogs.Count * 100.0 / logs.Count:F1}%)");
                    sb.AppendLine();

                    var avgInterval = logs.Where(l => l.Interval > 0).Average(l => l.Interval);
                    var avgStability = logs.Where(l => l.Stability.HasValue).Average(l => l.Stability!.Value);
                    var avgDifficulty = logs.Where(l => l.Difficulty.HasValue).Average(l => l.Difficulty!.Value);

                    sb.AppendLine("## 学习项状态");
                    sb.AppendLine($"- 平均间隔: {avgInterval:F1} 天");
                    sb.AppendLine($"- 平均稳定性: {avgStability:F2}");
                    sb.AppendLine($"- 平均难度: {avgDifficulty:F2}");
                    sb.AppendLine();

                    var byRating = logs.GroupBy(l => l.Rating).OrderBy(g => g.Key);
                    sb.AppendLine("## 评分分布");
                    foreach (var group in byRating)
                    {
                        sb.AppendLine($"- {group.Key}分: {group.Count()} 次 ({group.Count() * 100.0 / logs.Count:F1}%)");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成统计摘要失败");
                throw;
            }
        }

        /// <summary>
        /// 备份所有数据到 JSON 文件
        /// </summary>
        public void BackupAllData(string userId, string filePath)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                var items = db.SpacedRepetitionItems
                    .Where(i => i.UserId == userId)
                    .ToList();

                var logs = db.ReviewLogs
                    .Where(r => r.UserId == userId)
                    .ToList();

                var itemStates = db.LearningItemStates
                    .Where(s => s.UserId == userId)
                    .ToList();

                var backup = new
                {
                    BackupDate = DateTime.Now,
                    UserId = userId,
                    Version = "1.0",
                    LearningItems = items,
                    ReviewLogs = logs,
                    LearningItemStates = itemStates
                };

                var json = System.Text.Json.JsonSerializer.Serialize(backup, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json, Encoding.UTF8);
                _logger?.LogInformation("数据备份成功: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "数据备份失败");
                throw;
            }
        }
    }
}
