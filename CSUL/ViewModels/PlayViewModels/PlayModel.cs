using CSUL.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CSUL.ViewModels.PlayViewModels
{
    /// <summary>
    /// PlayView的ViewModel
    /// </summary>
    public class PlayModel : BaseViewModel
    {
        private Window? window = null;

        public PlayModel()
        {
            PlayGameCommand = new RelayCommand(
                async (sender) =>
                {
                    ButtonEnabled = false;
                    await Task.Delay(500);
                    if (GameManager.Instance.ShowSteamInfo)
                    {
                        string steamInfo = LanguageManager.GetString("Msg_SteamCaution");
                        MessageBoxResult ret = LanguageManager.MessageBox(steamInfo, LanguageManager.GetString("Msg_Cap_SteamCaution"), MessageBoxButton.OKCancel, MessageBoxImage.Information);
                        if (ret == MessageBoxResult.Cancel) GameManager.Instance.ShowSteamInfo = false;
                    }
                    if (window is not null) window.WindowState = WindowState.Minimized;
                    try
                    {
                        string arg = $"{(OpenDeveloper ? "-developerMode " : null)}{GameManager.Instance.StartArguemnt}";
                        if (GameManager.Instance.SteamCompatibilityMode)
                        {   //Steam正版兼容模式
                            string? steamPath = null;
                            if (GameManager.Instance.SteamPath is string path && File.Exists(path)) steamPath = path;
                            SteamManager.StartApp(949230, arg, steamPath);
                        }
                        else GameManager.StartGame(FileManager.Instance.GamePath!, arg);
                    }
                    catch (Exception ex)
                    {
                        LanguageManager.MessageBox(ExceptionManager.GetExMeg(ex), LanguageManager.GetString("Msg_Cap_GameStartError"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    await Task.Delay(500);
                    ButtonEnabled = true;
                });
        }

        public ICommand PlayGameCommand { get; }

        public bool OpenDeveloper
        {
            get => GameManager.Instance.OpenDeveloper;
            set
            {
                GameManager.Instance.OpenDeveloper = value;
                OnPropertyChanged();
            }
        }

        public bool SteamCompatibilityMode
        {
            get => GameManager.Instance.SteamCompatibilityMode;
            set
            {
                GameManager.Instance.SteamCompatibilityMode = value;
                OnPropertyChanged();
            }
        }

        private bool buttonEnabled = true;

        public bool ButtonEnabled
        {
            get => buttonEnabled;
            set
            {
                if (buttonEnabled == value) return;
                buttonEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 设定当前窗口
        /// </summary>
        /// <param name="window"></param>
        public void SetWindow(Window window) => this.window = window;
    }
}