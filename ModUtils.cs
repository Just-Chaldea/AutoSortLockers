using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Common.Mod
{
    internal static class ModUtils
    {
        private static MonoBehaviour coroutineObject;

        public static void LoadSaveData<SaveDataT>(string fileName, Action<SaveDataT> onSuccess) where SaveDataT : new()
        {
            StartCoroutine(LoadInternal<SaveDataT>(fileName, onSuccess));
        }

        private static IEnumerator LoadInternal<SaveDataT>(string fileName, Action<SaveDataT> onSuccess)
        {
            UserStorage userStorage = PlatformUtils.main.GetUserStorage();
            var files = new List<string> { fileName };
            UserStorageUtils.LoadOperation loadOperation = userStorage.LoadFilesAsync(SaveLoadManager.main.GetCurrentSlot(), files);
            yield return loadOperation;
            if (loadOperation.GetSuccessful())
            {
                string stringData = Encoding.ASCII.GetString(loadOperation.files[fileName]);
                SaveDataT saveData = JsonConvert.DeserializeObject<SaveDataT>(stringData);
                onSuccess(saveData);
            }
            else
            {
                Console.WriteLine("Load Failed: " + loadOperation.errorMessage);
            }
        }

        public static void Save<SaveDataT>(SaveDataT newSaveData, string fileName, Action onSaveComplete = null)
        {
            if (newSaveData != null)
            {
                string saveDataJson = JsonConvert.SerializeObject(newSaveData);
                StartCoroutine(SaveInternal(saveDataJson, fileName, onSaveComplete));
            }
        }

        private static IEnumerator SaveInternal(string saveData, string fileName, Action onSaveComplete = null)
        {
            UserStorage userStorage = PlatformUtils.main.GetUserStorage();
            var saveFileMap = new Dictionary<string, byte[]>
            {
                { fileName, Encoding.ASCII.GetBytes(saveData) }
            };
            SaveLoadManager.main.GetCurrentSlot();
            UserStorageUtils.SaveOperation saveOp = userStorage.SaveFilesAsync(SaveLoadManager.main.GetCurrentSlot(), saveFileMap);
            yield return saveOp;
            if (saveOp.GetSuccessful())
            {
                onSaveComplete?.Invoke();
            }
        }

        public static Coroutine StartCoroutine(IEnumerator coroutine)
        {
            if (coroutineObject == null)
            {
                var go = new GameObject();
                coroutineObject = go.AddComponent<ModSaver>();
            }

            return coroutineObject.StartCoroutine(coroutine);
        }
    }
}
