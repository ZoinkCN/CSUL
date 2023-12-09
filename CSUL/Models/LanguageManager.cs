using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFCustomMessageBox;

namespace CSUL.Models
{
    /// <summary>
    /// 界面语言管理类
    /// </summary>
    public class LanguageManager : IDisposable
    {
        /// <summary>
        /// 配置文件路径
        /// </summary>
        public static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CSUL_Lang.config");

        /// <summary>
        /// 可用语言字典，与语言选择下拉框绑定
        /// <para>Key:语言代码，添加新的语言是请将文件命名为正确的语言代码</para>
        /// <para>Value:语言名称，下拉框将显示此项</para>
        /// </summary>
        public static ReadOnlyDictionary<string, string> Languages = GetLanguages();

        public static LanguageManager Instance { get; } = new();

        #region ---构造函数---
        private LanguageManager()
        {
            bool loaded = false;
            if (File.Exists(ConfigPath))
            {
                try
                {
                    this.LoadConfig(ConfigPath);
                    loaded = true;
                }
                catch { }
            }
            if (!loaded)
            {
                CurrentLanguage = CultureInfo.CurrentCulture.Name.ToLower().Replace("-", "_");
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.SaveConfig(ConfigPath);
        }
        #endregion ---构造函数---

        #region ---私有字段---
        private string currentLanguage;
        #endregion ---私有字段---

        #region ---公共属性---
        [Config]
        public string CurrentLanguage
        {
            get => currentLanguage;
            set
            {
                if (value == currentLanguage) return;
                currentLanguage = value;
                SetLanguage(value);
            }
        }
        #endregion ---公共属性---

        #region ---私有方法---
        private void SetLanguage(string language)
        {
            if (Application.LoadComponent(new Uri($@"Languages\{language}.xaml", UriKind.Relative)) is ResourceDictionary rd)
            {
                ResourceDictionary resources = Application.Current.Resources.MergedDictionaries[0];
                foreach (object? key in rd.Keys)
                {
                    resources[key] = rd[key];
                }
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language.Replace("_", "-"));
                Trace.WriteLine(CultureInfo.CurrentUICulture.Name);
            }
        }
        #endregion ---私有方法---

        #region ---静态方法---
        /// <summary>
        /// 读取
        /// </summary>
        /// <returns></returns>
        public static ReadOnlyDictionary<string, string> GetLanguages()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            string langDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");
            DirectoryInfo directory = new DirectoryInfo(langDir);
            FileInfo[] files = directory.GetFiles("*.xaml");
            foreach (FileInfo file in files)
            {
                string lang = Path.GetFileNameWithoutExtension(file.Name);
                lang = lang.ToLower();
                try
                {
                    CultureInfo culture = CultureInfo.GetCultureInfo(lang.Replace("_", "-"));
                    dict.Add(lang, culture.NativeName);
                }
                catch { }
            }
            return new ReadOnlyDictionary<string, string>(dict);
        }

        /// <summary>
        /// 获取语言词典中的词条，当前语言中没有定义此词条时会从默认语言(en-US)中寻找。
        /// <para>当前语言与默认语言中均未定义此词条时，将返回null。</para>
        /// </summary>
        /// <param name="key">词条的Key，大小写敏感</param>
        /// <returns>词条内容</returns>
        public static string GetString(string key)
        {
            ResourceDictionary resources = Application.Current.Resources.MergedDictionaries[0];
            if (resources.Keys.Cast<string>().Contains(key))
            {
                return (string)resources[key];
            }
            else
            {
                if (Application.LoadComponent(new Uri(@"Languages\en_us.xaml", UriKind.Relative)) is ResourceDictionary defaultResources)
                {
                    if (defaultResources.Keys.Cast<string>().Contains(key))
                    {
                        return (string)resources[key];
                    }
                }
            }
            return "";
        }

        public static MessageBoxResult MessageBox(string message, string caption, MessageBoxButton button, MessageBoxImage image)
        {
            return button switch
            {
                MessageBoxButton.OK => CustomMessageBox.ShowOK(message, caption, GetString("Msg_Btn_OK"), image),
                MessageBoxButton.OKCancel => CustomMessageBox.ShowOKCancel(message, caption, GetString("Msg_Btn_OK"), GetString("Msg_Btn_Cancel"), image),
                MessageBoxButton.YesNoCancel => CustomMessageBox.ShowYesNoCancel(message, caption, GetString("Msg_Btn_Yes"), GetString("Msg_Btn_No"), GetString("Msg_Btn_Cancel"), image),
                MessageBoxButton.YesNo => CustomMessageBox.ShowYesNo(message, caption, GetString("Msg_Btn_Yes"), GetString("Msg_Btn_No"), image),
                _ => default,
            };
        }

        public static MessageBoxResult MessageBox(string message, string caption, MessageBoxButton button)
        {
            return button switch
            {
                MessageBoxButton.OK => CustomMessageBox.ShowOK(message, caption, GetString("Msg_Btn_OK")),
                MessageBoxButton.OKCancel => CustomMessageBox.ShowOKCancel(message, caption, GetString("Msg_Btn_OK"), GetString("Msg_Btn_Cancel")),
                MessageBoxButton.YesNoCancel => CustomMessageBox.ShowYesNoCancel(message, caption, GetString("Msg_Btn_Yes"), GetString("Msg_Btn_No"), GetString("Msg_Btn_Cancel")),
                MessageBoxButton.YesNo => CustomMessageBox.ShowYesNo(message, caption, GetString("Msg_Btn_Yes"), GetString("Msg_Btn_No")),
                _ => default,
            };
        }

        public static MessageBoxResult MessageBox(string message)
        {
            return CustomMessageBox.ShowOK(message, "", GetString("Msg_Btn_OK"));
        }
        #endregion ---静态方法---
    }
}
