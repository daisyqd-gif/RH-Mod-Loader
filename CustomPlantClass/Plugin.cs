// ===== System =====
global using System;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Reflection;
global using System.Diagnostics;
global using System.Collections;
global using System.Collections.Generic;
global using System.Security.Cryptography;
global using System.Text.Json;
global using System.Net.Http;
global using System.Threading.Tasks;
// ===== BepInEx =====
global using BepInEx;
global using BepInEx.Logging;
global using BepInEx.Unity.IL2CPP;
// ===== Unity =====
global using UnityEngine;
global using UnityEngine.Rendering;
global using UnityEngine.UI;
global using Unity.VisualScripting;
// ==== Il2cppInterop ====
global using Il2CppInterop.Runtime.Injection;
// ===== Other =====
global using CustomizeLib.BepInEx;
global using HarmonyLib;
global using TMPro;
// == Static Aliases ==
global using static Zombie;
global using static Plant;
// ===== Aliases =====
global using Random = UnityEngine.Random;
global using Object = UnityEngine.Object;
global using File = System.IO.File;
global using System.Text.RegularExpressions;

[assembly : CustomPlantClass.CustomMod(CustomPlantClass.MyPluginInfo.PluginName)]

namespace CustomPlantClass
{
    [BepInPlugin(MyPluginInfo.PluginGuid, MyPluginInfo.PluginName, MyPluginInfo.PluginVersion)]
    public class Core : BasePlugin
    {
        public static ManualLogSource Logger;
        public static AssetBundle assetBundle;
        public override void Load()
        {
            Logger = Log;
            Tools.InitMod();
            assetBundle=AssetMgr.LoadBundleFromResource(Assembly.GetExecutingAssembly(),"datamgr",false);

            DataMgr.StartUpMessages.Add($"Thank you for using {MyPluginInfo.PluginName} {MyPluginInfo.PluginVersion}!");
            DataMgr.GameStartActions.Add(() =>
            {
                foreach(var level in DataMgr.LoadedCustomLevels)
                {
                    if(level.ScenePrefab != null)
                    {
                        var scenePrefab = level.ScenePrefab;
                        scenePrefab.transform.FindChild("bg")?.AddComponent<GiveFertilize>();
                        scenePrefab.transform.FindChild("checklose")?.AddComponent<GameLose>();
                        // Add FloorMgr to all "floor" children
                        foreach (Transform child in scenePrefab.transform)
                        {
                            if (Regex.IsMatch(child.name, "floor", RegexOptions.IgnoreCase))
                                child.gameObject.AddComponent<FloorMgr>();
                        }
                        GameAPP.resourcesManager.backgroundPrefabs[level.SceneType] = scenePrefab;
                    }
                    if(level.MusicAudio!=null) if(!GameAPP.soundManager.musics.TryAdd(level.MusicType,level.MusicAudio)) ModLogger.LogWarn($"MusicType {level.MusicType} already exists! Using original.");
                }
            });
            Log.LogInfo($"{MyPluginInfo.PluginName} {MyPluginInfo.PluginVersion} loaded.");
        }
    }
    public static class MyPluginInfo
    {
        public const string PluginGuid = "PVZDataMgr.Bepinex";
        public const string PluginName = "PVZDataMgr";
        public const string PluginVersion = "1.0.0";
    }
    public static class ModLogger
    {
        public static void LogInfo(string mod, string msg)
        {
            Core.Logger.LogInfo($"[{mod}] Info — {msg}");
            DataMgr.StartUpMessages.Add($"[{mod}] Info — {msg}");
        }

        public static void LogWarn(string mod, string msg)
        {
            Core.Logger.LogWarning($"[{mod}] Warning — {msg}");
            DataMgr.StartUpWarnings.Add($"[{mod}] Warning — {msg}");
        }

        public static void LogError(string mod, string msg)
        {
            Core.Logger.LogError($"[{mod}] Error — {msg}");
            DataMgr.StartUpErrors.Add($"[{mod}] Error — {msg}");
        }

        public static void LogInfo(string msg)
        {
            Core.Logger.LogInfo($"[{MyPluginInfo.PluginName}] Info — {msg}");
            DataMgr.StartUpMessages.Add($"[{MyPluginInfo.PluginName}] Info — {msg}");
        }

        public static void LogWarn(string msg)
        {
            Core.Logger.LogWarning($"[{MyPluginInfo.PluginName}] Warning — {msg}");
            DataMgr.StartUpWarnings.Add($"[{MyPluginInfo.PluginName}] Warning — {msg}");
        }

        public static void LogError(string msg)
        {
            Core.Logger.LogError($"[{MyPluginInfo.PluginName}] Error — {msg}");
            DataMgr.StartUpErrors.Add($"[{MyPluginInfo.PluginName}] Error — {msg}");
        }

        public static void LogInfo(Assembly asm, string msg)
        {
            Core.Logger.LogInfo($"[{AttributeMgr.GetModName(asm)}] Info — {msg}");
            DataMgr.StartUpMessages.Add($"[{AttributeMgr.GetModName(asm)}] Info — {msg}");
        }

        public static void LogWarn(Assembly asm, string msg)
        {
            Core.Logger.LogWarning($"[{AttributeMgr.GetModName(asm)}] Warning — {msg}");
            DataMgr.StartUpWarnings.Add($"[{AttributeMgr.GetModName(asm)}] Warning — {msg}");
        }

        public static void LogError(Assembly asm, string msg)
        {
            Core.Logger.LogError($"[{AttributeMgr.GetModName(asm)}] Error — {msg}");
            DataMgr.StartUpErrors.Add($"[{AttributeMgr.GetModName(asm)}] Error — {msg}");
        }
    }
}