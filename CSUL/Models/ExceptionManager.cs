using System;
using System.Text;

namespace CSUL.Models
{
    /// <summary>
    /// 异常处理器
    /// </summary>
    public class ExceptionManager
    {
        /// <summary>
        /// 得到异常信息
        /// </summary>
        public static string GetExMeg(Exception? ex, string? meg = null)
        {
            StringBuilder builder = new();
            builder.AppendLine($"{LanguageManager.GetString("ExceptionTime")}: {DateTime.Now:yyyy_MM_dd HH:mm:ss:ffff}");
            if (ex is not null)
            {
                builder.AppendLine($"{LanguageManager.GetString("ExceptionObject")}: {ex.Source}");
                builder.AppendLine($"{LanguageManager.GetString("ExceptionTargetSite")}: {ex.TargetSite?.Name}");
                builder.AppendLine($"{LanguageManager.GetString("ExceptionStackTrace")}: \n{ex.StackTrace}");
                builder.AppendLine($"{LanguageManager.GetString("ExceptionMessage")}: {ex.Message}");
            }
            if (meg is not null)
            {
                builder.AppendLine($"{LanguageManager.GetString("ExceptionExtraMessage")}: \n{meg}");
            }
            return builder.ToString();
        }
    }
}