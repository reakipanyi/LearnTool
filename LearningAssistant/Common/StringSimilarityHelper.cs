using System;

namespace LearningAssistant.Common
{
    /// <summary>
    /// 字符串相似度计算工具类
    /// 
    /// 提供字符串相似度计算和答案验证功能，用于支持主动回忆和渐进式提示等学习功能。
    /// 主要算法基于 Levenshtein 编辑距离，实现模糊匹配和答案验证。
    /// </summary>
    public static class StringSimilarityHelper
    {
        /// <summary>
        /// 计算两个字符串的相似度
        /// 
        /// 相似度范围为 0 到 1，0 表示完全不同，1 表示完全相同。
        /// 使用 Levenshtein 编辑距离算法计算相似度。
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="target">目标字符串</param>
        /// <returns>相似度值 (0-1)</returns>
        /// <example>
        /// <code>
        /// double similarity = StringSimilarityHelper.CalculateSimilarity("学习", "学习");
        /// // 返回 1.0（完全相同）
        /// 
        /// similarity = StringSimilarityHelper.CalculateSimilarity("学习", "学");
        /// // 返回较高相似度（部分匹配）
        /// </code>
        /// </example>
        public static double CalculateSimilarity(string source, string target)
        {
            // 空字符串检查
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                return 0;

            // 统一转为小写进行比较，忽略大小写差异
            source = source.ToLower();
            target = target.ToLower();

            // 完全匹配直接返回最高相似度
            if (source.Equals(target))
                return 1.0;

            // 计算编辑距离并转换为相似度
            int distance = LevenshteinDistance(source, target);
            int maxLength = Math.Max(source.Length, target.Length);

            // 相似度 = 1 - (编辑距离 / 最大长度)
            return 1.0 - (double)distance / maxLength;
        }

        /// <summary>
        /// 计算编辑距离 (Levenshtein Distance)
        /// 
        /// 编辑距离是指将一个字符串转换为另一个字符串所需的最少单字符编辑操作次数。
        /// 操作包括：插入、删除、替换。
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="target">目标字符串</param>
        /// <returns>编辑距离</returns>
        /// <remarks>
        /// 使用动态规划算法实现，时间复杂度 O(n*m)，空间复杂度 O(n*m)，
        /// 其中 n 和 m 分别是两个字符串的长度。
        /// </remarks>
        public static int LevenshteinDistance(string source, string target)
        {
            // 边界条件处理
            if (string.IsNullOrEmpty(source))
                return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target))
                return source.Length;

            // 创建动态规划表
            int[,] d = new int[source.Length + 1, target.Length + 1];

            // 初始化边界条件
            for (int i = 0; i <= source.Length; i++)
                d[i, 0] = i;  // 从空字符串到 source 的编辑距离
            for (int j = 0; j <= target.Length; j++)
                d[0, j] = j;  // 从空字符串到 target 的编辑距离

            // 动态规划计算
            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    // 计算替换成本：字符相同为 0，不同为 1
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    
                    // 取插入、删除、替换中的最小值
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1,      // 删除操作
                                 d[i, j - 1] + 1),     // 插入操作
                        d[i - 1, j - 1] + cost);      // 替换操作
                }
            }

            // 返回最终的编辑距离
            return d[source.Length, target.Length];
        }

        /// <summary>
        /// 检查答案是否正确
        /// 
        /// 支持三种匹配方式：完全匹配、包含匹配和相似度匹配。
        /// 按顺序进行匹配，任一匹配成功即认为答案正确。
        /// </summary>
        /// <param name="userAnswer">用户输入的答案</param>
        /// <param name="correctAnswer">正确答案</param>
        /// <param name="similarityThreshold">相似度阈值，默认 0.6</param>
        /// <returns>答案是否正确</returns>
        /// <example>
        /// <code>
        /// bool isCorrect = StringSimilarityHelper.CheckAnswer("学习", "学习");
        /// // 返回 true（完全匹配）
        /// 
        /// isCorrect = StringSimilarityHelper.CheckAnswer("学", "学习");
        /// // 返回 true（包含匹配）
        /// 
        /// isCorrect = StringSimilarityHelper.CheckAnswer("学习中", "学习");
        /// // 返回 true（包含匹配）
        /// 
        /// isCorrect = StringSimilarityHelper.CheckAnswer("学西", "学习");
        /// // 返回 true（相似度匹配，超过阈值）
        /// </code>
        /// </example>
        public static bool CheckAnswer(string userAnswer, string correctAnswer, double similarityThreshold = 0.6)
        {
            // 用户答案为空直接返回错误
            if (string.IsNullOrEmpty(userAnswer))
                return false;

            // 统一格式：小写并去除首尾空格
            userAnswer = userAnswer.ToLower().Trim();
            correctAnswer = correctAnswer.ToLower().Trim();

            // 1. 完全匹配检查
            if (userAnswer == correctAnswer)
                return true;

            // 2. 包含匹配检查（用户答案是正确答案的子集或反之）
            if (correctAnswer.Contains(userAnswer) || userAnswer.Contains(correctAnswer))
                return true;

            // 3. 相似度匹配检查（基于编辑距离的模糊匹配）
            double similarity = CalculateSimilarity(userAnswer, correctAnswer);
            return similarity > similarityThreshold;
        }
    }
}
