using System;
using System.IO;
using UnityEngine;

namespace PawSlayers
{
    public class RunSaveManager : MonoBehaviour
    {
        public RunManager runManager;

        public string SavePath => Path.Combine(Application.persistentDataPath, "paw_slayers_run_save.json");

        public void Initialize(RunManager manager)
        {
            runManager = manager;
            Debug.Log("Run save path: " + SavePath);
        }

        public bool HasSavedRun()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("Has saved run: false");
                return false;
            }

            RunSaveData data = LoadCurrentRun();
            bool hasRun = data != null && data.hasActiveRun && !data.runWon && !data.runLost;
            Debug.Log("Has saved run: " + hasRun);
            return hasRun;
        }

        public void SaveCurrentRun(string sceneNameOverride = null)
        {
            if (runManager == null)
            {
                Debug.LogWarning("RunSaveManager missing RunManager.");
                return;
            }

            RunSaveData data = BuildSaveDataFromRunState(sceneNameOverride);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log("Save created: " + SavePath);
            Debug.Log("Current run deck count saved: " + data.currentRunDeck.Count);
            Debug.Log("Relic count saved: " + data.ownedRelics.Count);
            Debug.Log("Gold saved: " + data.gold);
            Debug.Log("Selected heroes saved: " + string.Join(", ", data.selectedHeroIds));
        }

        public RunSaveData LoadCurrentRun()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("Run save file missing: " + SavePath);
                return null;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                RunSaveData data = JsonUtility.FromJson<RunSaveData>(json);
                Debug.Log("Save loaded: " + SavePath);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to load run save: " + exception.Message);
                return null;
            }
        }

        public bool ContinueSavedRun()
        {
            RunSaveData data = LoadCurrentRun();
            if (data == null || !data.hasActiveRun || runManager == null)
            {
                return false;
            }

            ApplySaveDataToRunState(data);
            string sceneToLoad = string.IsNullOrWhiteSpace(data.currentSceneName) ? runManager.mapSceneName : data.currentSceneName;
            if (sceneToLoad == runManager.battleSceneName)
            {
                Debug.Log("Saved battle run restored to MapScene for prototype safety.");
                sceneToLoad = runManager.mapSceneName;
            }

            Debug.Log("Continued saved run.");
            runManager.LoadSceneByName(sceneToLoad);
            return true;
        }

        public void DeleteCurrentRunSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("Save deleted: " + SavePath);
            }
        }

        public RunSaveData BuildSaveDataFromRunState(string sceneNameOverride = null)
        {
            return runManager == null ? new RunSaveData() : runManager.BuildRunSaveData(sceneNameOverride);
        }

        public void ApplySaveDataToRunState(RunSaveData data)
        {
            runManager?.ApplyRunSaveData(data);
        }
    }
}
