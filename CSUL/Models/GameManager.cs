﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CSUL.Models
{
    /// <summary>
    /// 游戏管理类
    /// </summary>
    public class GameManager : IDisposable
    {
        /// <summary>
        /// 配置文件路径
        /// </summary>
        public static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CSUL_Game.config");

        /// <summary>
        /// 得到<see cref="GameManager"/>的实例
        /// </summary>
        public static GameManager Instance { get; } = new();

        /// <summary>
        /// 已安装的游戏版本
        /// </summary>
        public static string? GameVersion => GetGameVersion();

        #region ---构造函数---

        private GameManager()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    this.LoadConfig(ConfigPath);
                }
                catch { }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.SaveConfig(ConfigPath);
        }

        #endregion ---构造函数---

        #region ---公共属性---

        /// <summary>
        /// 启动参数
        /// </summary>
        [Config]
        public string? StartArguemnt { get; set; }

        /// <summary>
        /// 是否以开发模式启动
        /// </summary>
        [Config]
        public bool OpenDeveloper { get; set; }

        /// <summary>
        /// 是否显示Steam提示信息
        /// </summary>
        [Config]
        public bool ShowSteamInfo { get; set; } = true;

        /// <summary>
        /// 是否以Steam正版兼容模式启动
        /// </summary>
        [Config]
        public bool SteamCompatibilityMode { get; set; } = false;

        /// <summary>
        /// 自定义Steam路径
        /// </summary>
        [Config]
        public string? SteamPath { get; set; } = null;

        #endregion ---公共属性---

        #region ---静态方法---

        /// <summary>
        /// 直接启动应用
        /// </summary>
        /// <param name="gamePath">应用路径</param>
        public static void StartGame(string gamePath, string? arguments = null)
        {
            if (string.IsNullOrEmpty(gamePath)) throw new ArgumentNullException(nameof(gamePath));
            if (!File.Exists(gamePath)) throw new FileNotFoundException($"游戏路径{gamePath}不存在，请检查路径设置", gamePath);
            if (arguments is null) Process.Start(gamePath);
            else Process.Start(gamePath, arguments);
        }

        /// <summary>
        /// 获取当前安装的游戏版本
        /// </summary>
        /// <returns></returns>
        public static string? GetGameVersion()
        {
            string gameFilePath = Path.Combine(FileManager.Instance.GameRootDir.FullName, @"Cities2_Data\Managed\Game.dll");
            Assembly gameAssembly = Assembly.LoadFrom(gameFilePath);
            Type? gameVersion = gameAssembly.GetType("Game.Version");
            FieldInfo? fieldInfo1 = gameVersion?.GetField("current");
            object? versionObject = fieldInfo1?.GetValue(null);

            if (versionObject != null)
            {
                string colossalFilePath = Path.Combine(FileManager.Instance.GameRootDir.FullName, @"Cities2_Data\Managed\Colossal.Core.dll");
                Assembly colossaAssembly = Assembly.LoadFrom(colossalFilePath);
                Type? colossalVersion = colossaAssembly.GetType("Colossal.Version");
                PropertyInfo? shortVersionFieldInfo = colossalVersion?.GetProperty("shortVersion");
                PropertyInfo? versionFieldInfo = colossalVersion?.GetProperty("version");
                PropertyInfo? fullVersionFieldInfo = colossalVersion?.GetProperty("fullVersion");
                string? shortVersion = (string?)shortVersionFieldInfo?.GetValue(versionObject);
                string? version = (string?)versionFieldInfo?.GetValue(versionObject);
                string? fullVersion = (string?)fullVersionFieldInfo?.GetValue(versionObject);
                return shortVersion;
            }
            return null;
        }
        #endregion ---静态方法---
    }
}