using System;
using System.Diagnostics;

namespace LearningAssistant.Common
{
    /// <summary>
    /// 浏览器辅助类 - 统一处理外部URL打开
    /// </summary>
    public static class BrowserHelper
    {
        /// <summary>
        /// 使用系统默认浏览器打开URL
        /// </summary>
        /// <param name="url">要打开的URL</param>
        /// <returns>是否成功打开</returns>
        public static bool OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"BrowserHelper.OpenUrl 打开URL失败: {url}, 异常: {ex}");
                return false;
            }
        }
    }
}
