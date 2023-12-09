using CSUL.Models;
using CSUL.UserControls.DragFiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CSUL.ViewModels.MapViewModels
{
    /// <summary>
    /// MapView的ViewModel
    /// </summary>
    public class MapModel : BaseViewModel
    {
        public MapModel()
        {
            //获取初始数据
            RefreshData();

            //设定命令处理方法
            DeleteCommand = new RelayCommand((sender) =>
            {
                if (sender is not GameDataFileInfo data) return;
                StringBuilder sb = new();
                sb.Append($"{LanguageManager.GetString(data.DataType == Models.Enums.GameDataFileType.Map ? "MapName" : "SaveName")}: ").Append(data.Name).AppendLine();
                sb.Append($"{LanguageManager.GetString(data.DataType == Models.Enums.GameDataFileType.Map ? "MapID" : "SaveID")}: ").Append(data.Cid).AppendLine();
                sb.Append($"{LanguageManager.GetString("LastWriteTime")}: ").Append(data.LastWriteTime).AppendLine();
                sb.Append($"{LanguageManager.GetString(data.DataType == Models.Enums.GameDataFileType.Map ? "MapPath" : "SavePath")}: ").AppendLine().Append(data.FilePath).AppendLine();
                var ret = LanguageManager.MessageBox(sb.ToString(), LanguageManager.GetString(data.DataType == Models.Enums.GameDataFileType.Map ? "DeleteMap" : "DeleteSave"), MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (ret == MessageBoxResult.OK)
                {
                    try
                    {
                        data.Delete();
                        LanguageManager.MessageBox(LanguageManager.GetString("Msg_DeleteComlete"));
                    }
                    catch (Exception ex)
                    {
                        LanguageManager.MessageBox(
                            ExceptionManager.GetExMeg(ex),
                            LanguageManager.GetString("Msg_Cap_DeleteFailed"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    RefreshData();
                }
            });
            AddCommand = new RelayCommand(async (sender) =>
            {
                await InstallFile((sender as DragFilesEventArgs ?? throw new ArgumentNullException()).Paths);
            });
            Refresh = new RelayCommand(sender => RefreshData());
        }

        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand OpenFolder { get; } = new RelayCommand((sender) => Process.Start("Explorer.exe", FileManager.Instance.MapDir.FullName));
        public ICommand Refresh { get; }

        private IEnumerable<GameDataFileInfo> gameData = default!;

        public IEnumerable<GameDataFileInfo> GameData
        {
            get => gameData;
            set
            {
                if (gameData == value) return;
                gameData = value;
                OnPropertyChanged();
            }
        }

        #region ---私有方法---

        /// <summary>
        /// 刷新地图数据
        /// </summary>
        private void RefreshData() => GameData = from cid in FileManager.Instance.MapDir.GetAllFiles()
                                                 where cid.Name.EndsWith(".cok")
                                                 let data = new GameDataFileInfo(cid.FullName)
                                                 select data;

        /// <summary>
        /// 安装文件
        /// </summary>
        /// <param name="paths"></param>
        private async Task InstallFile(string[] paths)
        {
            foreach (string path in paths)
            {
                try
                {
                    Models.Structs.InstalledGameDataFiles ret = await FileManager.Instance.InstallGameDataFile(path);
                    StringBuilder builder = new();
                    builder.Append(string.Format(LanguageManager.GetString("Msg_FileAnalyzeComplete"), Path.GetFileName(path))).AppendLine();
                    builder.Append($"{string.Format(LanguageManager.GetString("Msg_MapImported"), ret.MapNames.Count)}: ").AppendLine();
                    ret.MapNames.ForEach(x => builder.AppendLine(x));
                    builder.AppendLine();
                    builder.Append($"{string.Format(LanguageManager.GetString("Msg_SaveImported"), ret.SaveNames.Count)}: ").AppendLine();
                    ret.SaveNames.ForEach(x => builder.AppendLine(x));
                    LanguageManager.MessageBox(builder.ToString(), LanguageManager.GetString("Msg_Cap_Information"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LanguageManager.MessageBox(
                        ExceptionManager.GetExMeg(ex, string.Format(LanguageManager.GetString("Msg_FileImportFailed"), Path.GetFileName(path))),
                        LanguageManager.GetString("Msg_Cap_Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            RefreshData();
        }

        #endregion ---私有方法---
    }
}