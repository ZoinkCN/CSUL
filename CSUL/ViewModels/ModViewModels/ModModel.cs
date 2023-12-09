using CSUL.Models;
using CSUL.UserControls.DragFiles;
using CSUL.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using System.Runtime.Loader;
using System.Windows.Controls;
using Mono.Cecil;

namespace CSUL.ViewModels.ModViewModels
{   //ModModel 构造函数、方法、子类
    public partial class ModModel : BaseViewModel
    {

        #region ---Mod信息类---
        public class ModInfo
        {
            #region ---构造函数---
            public ModInfo(string name)
            {
                Name = name;
            }
            #endregion ---构造函数---

            #region ---公共属性---
            /// <summary>
            /// mod名称
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// mod版本号
            /// </summary>
            public string Version { get; set; } = "?";

            /// <summary>
            /// mod的GUID
            /// </summary>
            public string GUID { get; set; }

            /// <summary>
            /// 主文件路径
            /// </summary>
            public string MainFile { get; private set; } = "";

            /// <summary>
            /// 是否找到主文件
            /// </summary>
            public bool IsMainFileFound { get; private set; } = false;

            /// <summary>
            /// 是否启用
            /// </summary>
            public bool IsEnabled { get; set; } = false;

            /// <summary>
            /// 是否重复
            /// </summary>
            public bool IsDuplicated { get; set; } = false;

            /// <summary>
            /// 模组路径
            /// </summary>
            public string ModPath { get; private set; } = default!;

            /// <summary>
            /// 最后修改时间
            /// </summary>
            public string LastWriteTime { get; set; } = default!;

            /// <summary>
            /// 是否为单文件
            /// </summary>
            public bool IsSingleFile { get; private set; } = false;

            #endregion ---公共属性---

            #region ---公共方法---
            /// <summary>
            /// 设置mod主文件，如果成功，则设置IsMainFileFound为true。
            /// <para>同时会判断该mod是否被启用。</para>
            /// </summary>
            /// <exception cref="FileNotFoundException">所传入文件路径不存在时引发</exception>
            /// <exception cref="ArgumentException">所传入文件后缀名不是dll或dlloff时引发</exception>
            /// <param name="mainFile">主文件路径</param>
            public void SetMainFile(string mainFile)
            {
                if (Path.Exists(mainFile) && (mainFile.ToLower().EndsWith(".dll") || mainFile.ToLower().EndsWith(".dlloff")))
                {
                    MainFile = mainFile;
                    IsMainFileFound = true;
                    IsEnabled = mainFile.ToLower().EndsWith(".dll");
                }
                else
                {
                    if (!Path.Exists(mainFile)) { throw new FileNotFoundException($"\"{mainFile}\" 该文件不存在!"); }
                    else
                    {
                        throw new ArgumentException($"\"{mainFile}\" 该文件不是mod文件!");
                    }
                }
            }

            /// <summary>
            /// 设置mod路径，并判断是否为单文件
            /// </summary>
            /// <param name="modPath">mod路径</param>
            /// <exception cref="ArgumentException">当路径不存在时引发</exception>
            public void SetModPath(string modPath)
            {
                if (File.Exists(modPath))
                {
                    ModPath = modPath;
                    IsSingleFile = true;
                }
                else if (Directory.Exists(modPath))
                {
                    ModPath = modPath;
                    IsSingleFile = false;
                }
                else { throw new ArgumentException($"\"{modPath}\" 该路径不存在!"); }
            }
            #endregion ---公共方法---
        }
        #endregion ---Mod信息类---

        #region ---BepInEx信息条目---

        public class BepItemData
        {
            public string Name { get; set; } = default!;

            public string Version { get; set; } = default!;

            public Brush BackBrush { get; set; } = default!;

            public string Uri { get; set; } = default!;
        }

        #endregion ---BepInEx信息条目---

        #region ---构造函数---

        public ModModel()
        {
            DeleteCommand = new RelayCommand(DeleteModFile);
            AddCommand = new RelayCommand(async (sender) =>
            {
                await InstallFile((sender as DragFilesEventArgs ?? throw new ArgumentNullException()).Paths);
            });
            DownloadCommand = new RelayCommand(async (sender) => await DownloadBepInEx(sender));
            RemoveCommand = new RelayCommand(RemoveBepInEx);
            CheckMods = new RelayCommand(CheckModCompatibility);
            RefreshCommand = new RelayCommand(Refresh);
            DisableCommand = new RelayCommand(Disable);
            EnableCommand = new RelayCommand(Enable);
            RefreshData();
        }

        #endregion ---构造函数---

        #region ---ICommand方法---
        /// <summary>
        /// 禁用mod
        /// </summary>
        /// <param name="sender">Command传入的参数</param>
        private void Disable(object? sender)
        {
            if (sender is not ModInfo data) return;
            if (!data.IsEnabled) { return; }
            FileInfo file = new FileInfo(data.MainFile);
            if (file.Exists && data.MainFile.ToLower().EndsWith(".dll"))
            {
                string newFileName = data.MainFile + "off";
                file.MoveTo(newFileName);
            }
            RefreshData();
        }

        /// <summary>
        /// 启用mod
        /// </summary>
        /// <param name="sender">Command传入的参数</param>
        private void Enable(object? sender)
        {
            if (sender is not ModInfo data) return;
            if (data.IsEnabled) { return; }
            FileInfo file = new FileInfo(data.MainFile);
            if (file.Exists && data.MainFile.ToLower().EndsWith(".dlloff"))
            {
                string newFileName = data.MainFile.Replace("dlloff", "dll");
                file.MoveTo(newFileName);
            }
            RefreshData();
        }

        /// <summary>
        /// 删除模组文件
        /// </summary>
        private void DeleteModFile(object? sender)
        {
            if (sender is not ModInfo data) return;
            StringBuilder sb = new();
            sb.Append($"{LanguageManager.GetString("ModName")}: ").Append(data.Name).AppendLine();
            sb.Append($"{LanguageManager.GetString("LastModifiedTime")}: ").Append(data.LastWriteTime).AppendLine();
            sb.Append($"{LanguageManager.GetString("ModPath")}: ").AppendLine().Append(data.ModPath).AppendLine();
            var ret = LanguageManager.MessageBox(sb.ToString(), LanguageManager.GetString("Msg_Cap_DeleteMod"), MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (ret == MessageBoxResult.OK)
            {
                FileSystemInfo mod = data.IsSingleFile ? new FileInfo(data.ModPath) : new DirectoryInfo(data.ModPath);
                try
                {
                    mod.Delete();
                    LanguageManager.MessageBox(LanguageManager.GetString("Msg_DeleteComlete"));
                }
                catch (Exception ex)
                {

                    LanguageManager.MessageBox(ExceptionManager.GetExMeg(ex), LanguageManager.GetString("Msg_Cap_DeleteFailed"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                RefreshData();
            }
        }

        /// <summary>
        /// 下载并安装BepInEx
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        private async Task DownloadBepInEx(object? sender)
        {
            try
            {
                if (sender is not BepItemData data) return;
                if (LanguageManager.MessageBox(data.Version, LanguageManager.GetString("Msg_Cap_Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                string path = Path.Combine(_tempDirPath, data.Name);
                FileInfo file = new(path);
                if (file.Directory?.Exists is true) file.Directory.Delete(true);
                if (file.Exists) file.Delete();
                if (file.Directory?.Exists is false) file.Directory?.Create();
                await WebManager.DownloadFromUri(data.Uri, path);
                using TempPackage package = new();
                await package.Decompress(path);
                RemoveBepInEx();
                ExFileManager.CopyTo(package.FullName, FileManager.Instance.GameRootDir.FullName, true);
                ShowNoEx = FileManager.Instance.NoBepInEx ? Visibility.Visible : Visibility.Collapsed;
                BepVersion = FileManager.Instance.BepVersion;
                LanguageManager.MessageBox(LanguageManager.GetString("Msg_Cap_InstallComlete"));
            }
            catch (Exception ex)
            {
                LanguageManager.MessageBox(ExceptionManager.GetExMeg(ex), LanguageManager.GetString("Msg_Cap_InstallFailed"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 移除BepInEx
        /// </summary>
        private async void RemoveBepInEx(object? sender)
        {
            MessageBoxResult ret = LanguageManager.MessageBox(
                LanguageManager.GetString("Msg_BepInExDeleteConfirm"),
                LanguageManager.GetString("Msg_Cap_Warning"),
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);
            if (ret == MessageBoxResult.OK)
            {
                try
                {
                    RemoveBepInEx();
                    MessageBoxResult ret2 = LanguageManager.MessageBox(
                        LanguageManager.GetString("Msg_BepInExDeleteComplete"),
                        LanguageManager.GetString("Msg_Cap_Information"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    if (ret2 == MessageBoxResult.Yes)
                    {
                        Process.Start("Explorer.exe", _tempDirPath);
                    }
                }
                catch (Exception e)
                {
                    LanguageManager.MessageBox(
                        ExceptionManager.GetExMeg(e),
                        LanguageManager.GetString("Msg_Cap_RemoveFailed"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    BepData = GetBepDownloadData();
                    await Task.Delay(500);
                    ShowNoEx = FileManager.Instance.NoBepInEx ? Visibility.Visible : Visibility.Collapsed;
                    BepVersion = FileManager.Instance.BepVersion;
                }
            }
        }

        /// <summary>
        /// 检查模组兼容性
        /// </summary>
        private void CheckModCompatibility(object? sender)
        {
            if (ModData is null)
            {
                LanguageManager.MessageBox(
                    LanguageManager.GetString("Msg_GetModInfoFailed"),
                    LanguageManager.GetString("Msg_Cap_Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            if (ModData.Count < 1)
            {
                LanguageManager.MessageBox(
                    LanguageManager.GetString("Msg_NoModInstalled"),
                    LanguageManager.GetString("Msg_Cap_Information"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            Version? knownBepVersion = FileManager.Instance.BepVersion;
            if (knownBepVersion is null)
            {
                LanguageManager.MessageBox(
                    LanguageManager.GetString("Msg_GetBepInExInfoFailed"),
                    LanguageManager.GetString("Msg_Cap_Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            Dictionary<int, (string name, string version)> allData = new();
            List<int> pass = new(), wrong = new(), unknow = new();
            for (int i = 0; i < ModData.Count; i++)
            {
                ModInfo item = ModData[i];
                try
                {
                    //检查单个模组
                    BepInExCheckResult ret = ExFileManager.ChickModBepInExVersioin(item.ModPath,
                        out Version? modVersion, out Version? bepVersion, item.IsSingleFile, knownBepVersion);
                    switch (ret)
                    {
                        case BepInExCheckResult.WrongVersion:
                            wrong.Add(i);
                            allData[i] = (ModData[i].Name, modVersion?.ToString() ?? LanguageManager.GetString("Unknown"));
                            break;

                        case BepInExCheckResult.Passed:
                            pass.Add(i);
                            allData[i] = (ModData[i].Name, modVersion?.ToString() ?? LanguageManager.GetString("Unknown"));
                            break;

                        default: throw new Exception();
                    }
                }
                catch
                {
                    unknow.Add(i);
                    allData[i] = (ModData[i].Name, LanguageManager.GetString("Unknown"));
                }
            }
            ModCompatibilityBox.ShowBox(allData, pass, wrong, unknow);
        }

        private void Refresh(object? sender)
        {
            RefreshData();
        }

        #endregion ---ICommand方法---

        #region ---私有方法---

        /// <summary>
        /// 刷新数据
        /// </summary>
        private void RefreshData()
        {
            List<ModInfo> modInfos = new List<ModInfo>();
            FileInfo[] files = FileManager.Instance.ModDir.GetFiles("*.dll*");
            modInfos.AddRange(FromFiles(files));
            DirectoryInfo[] dirs = FileManager.Instance.ModDir.GetDirectories();
            modInfos.AddRange(FromDirectories(dirs));
            CheckDuplication(modInfos);
            ModData = modInfos;
        }

        /// <summary>
        /// 安装文件
        /// </summary>
        private async Task InstallFile(string[] paths)
        {
            if (!FileManager.Instance.ModDir.Exists) FileManager.Instance.ModDir.Create();
            foreach (string path in paths)
            {
                try
                {
                    using TempPackage package = new();
                    if (path.EndsWith(".dll")) await package.AddFile(path);
                    else await package.Decompress(path);
                    if (package.IsEempty) throw new Exception(LanguageManager.GetString("PackageEmpty"));
                    string name = Path.GetFileName(path);
                    name = name[..name.LastIndexOf('.')];
                    switch (ExFileManager.ChickModBepInExVersioin(package.FullName, out Version? modVersion, out Version? bepVersion))
                    {
                        case BepInExCheckResult.Passed:
                            break;

                        case BepInExCheckResult.UnkownMod:
                            if (LanguageManager.MessageBox(
                                string.Format(LanguageManager.GetString("Msg_GetModBepVersionFailed"), name, bepVersion),
                                LanguageManager.GetString("Msg_Cap_Warning"),
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                            {
                                continue;
                            }
                            break;

                        case BepInExCheckResult.UnknowBepInEx:
                            if (LanguageManager.MessageBox(
                                string.Format(LanguageManager.GetString("Msg_GetBepVersionFailed"), name, modVersion),
                                LanguageManager.GetString("Msg_Cap_Warning"),
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                            {
                                continue;
                            }
                            break;

                        case BepInExCheckResult.WrongVersion:
                            if (LanguageManager.MessageBox(
                                string.Format(LanguageManager.GetString("Msg_GetBepVersionFailed"), name, bepVersion, modVersion),
                                LanguageManager.GetString("Msg_Cap_Warning"),
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                            {
                                continue;
                            }
                            break;

                        default:
                            if (LanguageManager.MessageBox(
                                LanguageManager.GetString("Msg_UnknownCompatibility"),
                                LanguageManager.GetString("Msg_Cap_Warning"),
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                            {
                                continue;
                            }
                            break;
                    }
                    string targetDir = FileManager.Instance.ModDir.FullName;
                    if (package.OnlySingleFile)
                    {
                        if (File.Exists(Path.Combine(targetDir, $"{name}.dll")))
                        {
                            if (LanguageManager.MessageBox(
                                string.Format(LanguageManager.GetString("Msg_ModExists"), name),
                                LanguageManager.GetString("Msg_Cap_Information"),
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Information) == MessageBoxResult.Cancel)
                            {
                                continue;
                            }
                        }
                        File.Copy(path, Path.Combine(targetDir, $"{name}.dll"), true);
                    }
                    else
                    {
                        if (Directory.Exists(Path.Combine(targetDir, name)))
                        {
                            if (LanguageManager.MessageBox(
                                string.Format(LanguageManager.GetString("Msg_ModExists"), name),
                                LanguageManager.GetString("Msg_Cap_Information"),
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Information) == MessageBoxResult.Cancel)
                            {
                                continue;
                            }
                        }
                        Directory.CreateDirectory(Path.Combine(targetDir, name));
                        Directory.Delete(Path.Combine(targetDir, name));
                        ExFileManager.CopyTo(package.FullName, Path.Combine(targetDir, name));
                    }
                    LanguageManager.MessageBox(
                        string.Format(LanguageManager.GetString("Msg_ModInstallComplete"), name),
                        LanguageManager.GetString("Msg_Cap_Information"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception e)
                {
                    LanguageManager.MessageBox(
                        string.Format(LanguageManager.GetString("Msg_ModInstallFailed"), path, ExceptionManager.GetExMeg(e)),
                        LanguageManager.GetString("Msg_Cap_Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            RefreshData();
        }

        /// <summary>
        /// 移除BepInEx
        /// </summary>
        private static void RemoveBepInEx()
        {
            if (!FileManager.Instance.GameRootDir.Exists) return;
            if (FileManager.Instance.ModDir.Exists)
            {   //备份插件 防止误删
                FileManager.Instance.ModDir.CopyTo(Path.Combine(_tempDirPath, FileManager.Instance.ModDir.Name), true);
            }
            if (FileManager.Instance.BepInExDir.Exists)
            {
                FileManager.Instance.BepInExDir.Delete(true);
            }
            FileInfo dll = new(Path.Combine(FileManager.Instance.GameRootDir.FullName, "winhttp.dll"));
            if (dll.Exists) dll.Delete();
        }

        /// <summary>
        /// 获取Bep下载数据
        /// </summary>
        private static List<BepItemData> GetBepDownloadData()
        {
            return (from item in WebManager.GetBepinexInfos()
                    select new BepItemData
                    {
                        Uri = item.Uri,
                        Version = $"{LanguageManager.GetString("Install")} {item.Version} {LanguageManager.GetString((item.IsBeta ? "BetaVer" : "ReleaseVer"))}",
                        Name = item.FileName,
                        BackBrush = new SolidColorBrush(Color.FromRgb(242, 242, 242))
                    }).ToList();
        }
        #endregion ---私有方法---

        #region ---私有字段---
        private static AssemblyDependencyResolver resolver;
        private static AssemblyLoadContext context;
        #endregion ---私有字段---

        #region ---静态方法---
        /// <summary>
        /// 检查GUID对应的mod是否重复
        /// </summary>
        /// <param name="guid">要查重的GUID</param>
        private static void CheckDuplication(IEnumerable<ModInfo> mods)
        {
            List<string> _checked = new List<string>();
            foreach (ModInfo mod in mods)
            {
                string guid = mod.GUID;
                if (_checked.Contains(guid)) continue;
                var fileSearch = mods.Where(s => s.GUID == guid);
                var enabledSearch = fileSearch.Where(s => s.IsEnabled);
                int searchCount = enabledSearch.Count();
                bool duplicated = searchCount > 1;
                foreach (var item in fileSearch)
                {
                    item.IsDuplicated = duplicated;
                }
                _checked.Add(guid);
            }
        }

        /// <summary>
        /// 从dll文件获取Mod信息
        /// </summary>
        /// <param name="file">dll文件路径</param>
        public static ModInfo? FromFile(FileInfo file)
        {
            return GetModFromFile(file);
        }

        /// <summary>
        /// 从多个dll文件获取Mod信息
        /// </summary>
        /// <param name="file">dll文件路径</param>
        public static IEnumerable<ModInfo> FromFiles(IEnumerable<FileInfo> files)
        {
            foreach (FileInfo file in files)
            {
                ModInfo? mod = GetModFromFile(file);
                if (mod != null) yield return mod;
            }
        }

        /// <summary>
        /// 从文件夹获取Mod信息
        /// </summary>
        /// <param name="dir">文件夹路径</param>
        /// <returns></returns>
        public static IEnumerable<ModInfo> FromDirectory(DirectoryInfo dir)
        {
            DirectoryInfo[] directories = dir.GetDirectories();
            foreach (DirectoryInfo directory in directories)
            {
                IEnumerable<ModInfo> mods = FromDirectory(directory);
                foreach (ModInfo mod in mods)
                {
                    yield return mod;
                }
            }
            FileInfo[] files = dir.GetFiles("*.dll*");
            foreach (FileInfo file in files)
            {
                ModInfo? mod = GetModFromFile(file, false);
                if (mod != null)
                {
                    mod.SetModPath(dir.FullName);
                    yield return mod;
                }
            }
        }

        /// <summary>
        /// 从多个文件夹获取Mod信息
        /// </summary>
        /// <param name="directories">文件夹路径</param>
        /// <returns></returns>
        public static IEnumerable<ModInfo> FromDirectories(IEnumerable<DirectoryInfo> directories)
        {
            foreach (DirectoryInfo directory in directories)
            {
                IEnumerable<ModInfo> mods = FromDirectory(directory);
                foreach (ModInfo mod in mods)
                {
                    yield return mod;
                }
            }
        }

        /// <summary>
        /// 读取dll文件信息
        /// </summary>
        /// <param name="file"></param>
        /// <param name="setModPath"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private static ModInfo? GetModFromFile(FileInfo file, bool setModPath = true)
        {
            if (!file.Exists)
            {
                throw new FileNotFoundException(string.Format(LanguageManager.GetString("FileNotExists"), file.FullName));
            }

            using AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(file.FullName);
            TypeDefinition? type = assemblyDefinition.MainModule.Types.FirstOrDefault(s => s.BaseType?.Name == "BaseUnityPlugin");
            if (type != null)
            {
                CustomAttribute? attribute = type.CustomAttributes.FirstOrDefault(s => s.AttributeType.Name == "BepInPlugin");
                if (attribute != null)
                {
                    try
                    {
                        string guid = (string)attribute.ConstructorArguments[0].Value;
                        string name = (string)attribute.ConstructorArguments[1].Value;
                        string version = (string)attribute.ConstructorArguments[2].Value;
                        ModInfo info = new(name);
                        info.GUID = guid;
                        info.Version = version;

                        info.SetMainFile(file.FullName);
                        info.LastWriteTime = file.LastWriteTime.ToString("yyyy-MM-dd-HH:mm:ss");
                        if (setModPath)
                        {
                            info.SetModPath(file.FullName);
                        }
                        return info;
                    }
                    catch { }
                }
            }
            return null;
        }
        #endregion ---静态方法---

    }
}