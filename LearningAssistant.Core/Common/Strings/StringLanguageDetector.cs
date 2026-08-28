using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions; 
namespace LearningAssistant.Common
{

    public class StringLanguageDetector
    {
        /// <summary>
        /// 判断字符串的语言类型
        /// </summary>
        /// <param name="input">要判断的字符串</param>
        /// <returns>返回语言类型：Chinese, English, Mixed</returns>
        public static LanguageType DetectLanguage(string input)
        {
            if (string.IsNullOrEmpty(input))
                return LanguageType.Unknown;

            bool hasChinese = false;
            bool hasEnglish = false;

            foreach (char c in input)
            {
                if (IsChinese(c))
                    hasChinese = true;
                else if (IsEnglish(c))
                    hasEnglish = true;

                // 如果同时包含中英文，可以提前返回
                if (hasChinese && hasEnglish)
                    return LanguageType.Mixed;
            }

            if (hasChinese && !hasEnglish)
                return LanguageType.Chinese;
            else if (!hasChinese && hasEnglish)
                return LanguageType.English;
            else
                return LanguageType.Unknown;
        }

        /// <summary>
        /// 判断字符是否为中文
        /// </summary>
        private static bool IsChinese(char c)
        {
            // 中文字符的Unicode范围
            return (c >= 0x4E00 && c <= 0x9FFF) ||  // 常用汉字
                   (c >= 0x3400 && c <= 0x4DBF) ||  // 扩展A
                   (c >= 0x20000 && c <= 0x2A6DF) || // 扩展B
                   (c >= 0x2A700 && c <= 0x2B73F) || // 扩展C
                   (c >= 0x2B740 && c <= 0x2B81F) || // 扩展D
                   (c >= 0x2B820 && c <= 0x2CEAF) || // 扩展E
                   (c >= 0xF900 && c <= 0xFAFF) ||   // 兼容汉字
                   (c >= 0x2F800 && c <= 0x2FA1F);   // 兼容汉字补充
        }

        /// <summary>
        /// 判断字符是否为英文字母
        /// </summary>
        private static bool IsEnglish(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        /// <summary>
        /// 使用正则表达式的版本
        /// </summary>
        public static LanguageType DetectLanguageRegex(string input)
        {
            if (string.IsNullOrEmpty(input))
                return LanguageType.Unknown;

            bool hasChinese = Regex.IsMatch(input, @"[\u4e00-\u9fff\u3400-\u4dbf]");
            bool hasEnglish = Regex.IsMatch(input, @"[a-zA-Z]");

            if (hasChinese && hasEnglish)
                return LanguageType.Mixed;
            else if (hasChinese)
                return LanguageType.Chinese;
            else if (hasEnglish)
                return LanguageType.English;
            else
                return LanguageType.Unknown;
        }

        /// <summary>
        /// 详细的检测结果，包含统计信息
        /// </summary>
        public static DetectionResult DetailedDetect(string input)
        {
            var result = new DetectionResult
            {
                OriginalText = input,
                ChineseCount = 0,
                EnglishCount = 0,
                OtherCount = 0
            };

            if (string.IsNullOrEmpty(input))
                return result;

            foreach (char c in input)
            {
                if (IsChinese(c))
                    result.ChineseCount++;
                else if (IsEnglish(c))
                    result.EnglishCount++;
                else
                    result.OtherCount++;
            }

            // 确定语言类型
            if (result.ChineseCount > 0 && result.EnglishCount > 0)
                result.LanguageType = LanguageType.Mixed;
            else if (result.ChineseCount > 0)
                result.LanguageType = LanguageType.Chinese;
            else if (result.EnglishCount > 0)
                result.LanguageType = LanguageType.English;
            else
                result.LanguageType = LanguageType.Unknown;

            return result;
        }
    }

    /// <summary>
    /// 语言类型枚举
    /// </summary>
    public enum LanguageType
    {
        Unknown,    // 未知
        Chinese,    // 纯中文
        English,    // 纯英文
        Mixed       // 中英文混合
    }

    /// <summary>
    /// 检测结果类
    /// </summary>
    public class DetectionResult
    {
        public string OriginalText { get; set; }
        public LanguageType LanguageType { get; set; }
        public int ChineseCount { get; set; }
        public int EnglishCount { get; set; }
        public int OtherCount { get; set; }

        public override string ToString()
        {
            return $"文本: '{OriginalText}', 类型: {LanguageType}, " +
                   $"中文: {ChineseCount}, 英文: {EnglishCount}, 其他: {OtherCount}";
        }
    } 
}
