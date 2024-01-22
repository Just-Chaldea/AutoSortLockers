using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Common.Mod;
using HarmonyLib;
using Nautilus.Handlers;
using Newtonsoft.Json;
using UnityEngine;

namespace AutosortLockers
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class Plugin : BaseUnityPlugin
	{
		public const string SaveDataFilename = "AutosortLockerSMLSaveData.json";
        public new static ManualLogSource Logger { get; private set; }

        private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        public static Config config = new();
		public static SaveData saveData;
		public static List<Color> colors = new();

		private static ModSaver saveObject;

		public static event Action<SaveData> OnDataLoaded;

        private void Awake()
        {
            // set project-scoped logger instance
            Logger = base.Logger;

            Plugin.config = OptionsPanelHandler.RegisterModOptions<Config>();

			// Load colors
            var serializedColors = JsonConvert.DeserializeObject<List<SerializableColor>>(File.ReadAllText(GetAssetPath("colors.json")));
            foreach (var sColor in serializedColors)
            {
                colors.Add(sColor.ToColor());
            }

            AddBuildables();

            // register harmony patches, if there are any
            Harmony.CreateAndPatchAll(Assembly);
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

		public static void AddBuildables()
		{
			AutosortLocker.AddBuildable();
			AutosortTarget.AddBuildable();
        }

        public static string GetAssetPath(string filename)
        {
            return Path.Combine(Path.Combine(GetModPath(), "Assets"), filename);
        }

        private static string GetModPath()
        {
            return Path.GetDirectoryName(Assembly.Location);
        }

        public static SaveData GetSaveData()
		{
			return saveData ?? new SaveData();
		}

		public static SaveDataEntry GetSaveData(string id)
		{
			var saveData = GetSaveData();
			foreach (var entry in saveData.Entries)
			{
				if (entry.Id == id)
				{
					return entry;
				}
			}
			return new SaveDataEntry() { Id = id };
		}

		public static void Save()
		{
			if (!IsSaving())
			{
				saveObject = new GameObject().AddComponent<ModSaver>();

				SaveData newSaveData = new SaveData();
				var targets = GameObject.FindObjectsOfType<AutosortTarget>();
				foreach (var target in targets)
				{
					target.Save(newSaveData);
				}
				saveData = newSaveData;
				ModUtils.Save<SaveData>(saveData, SaveDataFilename, OnSaveComplete);
			}
		}

		public static void OnSaveComplete()
		{
			saveObject.StartCoroutine(SaveCoroutine());
		}

		private static IEnumerator SaveCoroutine()
		{
			while (SaveLoadManager.main != null && SaveLoadManager.main.isSaving)
			{
				yield return null;
			}
			GameObject.DestroyImmediate(saveObject.gameObject);
			saveObject = null;
		}

		public static bool IsSaving()
		{
			return saveObject != null;
		}

		public static void LoadSaveData()
		{
			Logger.LogInfo("Loading Save Data...");
			ModUtils.LoadSaveData<SaveData>(SaveDataFilename, (data) =>
			{
				saveData = data;
				Logger.LogInfo("Save Data Loaded");
				OnDataLoaded?.Invoke(saveData);
			});
		}
	}
}