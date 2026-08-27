using System;
using System.IO;
using UnityEngine;

namespace BeyondTheBeat.Persistence
{
    public enum SaveLoadResult
    {
        Success = 0,
        Missing = 1,
        Corrupt = 2,
        Incompatible = 3,
        IoError = 4
    }

    [DisallowMultipleComponent]
    public sealed class SaveManager : MonoBehaviour
    {
        public const int CurrentVersion = 1;

        [SerializeField] private string saveFileName = "beyond-the-beat-phase1.json";

        public string SaveFileName => saveFileName;
        public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
        public SaveLoadResult LastLoadResult { get; private set; } = SaveLoadResult.Missing;

        public bool Save(GameSaveData data)
        {
            if (data == null || data.Version != CurrentVersion)
            {
                Debug.LogError("[Beyond The Beat] SaveManager rejected save data with an invalid version.");
                return false;
            }

            string path = SavePath;
            string tempPath = path + ".tmp";

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(tempPath, SerializeForStorage(data));

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tempPath, path);
                Debug.Log($"[Beyond The Beat] Local save written: {path}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Beyond The Beat] Local save failed: {exception.Message}");
                TryDelete(tempPath);
                return false;
            }
        }

        public SaveLoadResult Load(out GameSaveData data)
        {
            string path = SavePath;
            data = null;

            if (!File.Exists(path))
            {
                LastLoadResult = SaveLoadResult.Missing;
                return LastLoadResult;
            }

            try
            {
                string json = File.ReadAllText(path);
                LastLoadResult = DeserializeForStorage(json, out data);

                if (LastLoadResult != SaveLoadResult.Success)
                {
                    Debug.LogWarning(
                        $"[Beyond The Beat] Local save could not be used ({LastLoadResult}); new-game state will be used.");
                }

                return LastLoadResult;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Beyond The Beat] Local save read failed: {exception.Message}");
                LastLoadResult = SaveLoadResult.IoError;
                data = null;
                return LastLoadResult;
            }
        }

        public bool ResetSave()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                TryDelete(SavePath + ".tmp");
                LastLoadResult = SaveLoadResult.Missing;
                Debug.Log("[Beyond The Beat] Local save reset.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Beyond The Beat] Unable to reset local save: {exception.Message}");
                return false;
            }
        }

        public static string SerializeForStorage(GameSaveData data)
        {
            return JsonUtility.ToJson(data, true);
        }

        public static SaveLoadResult DeserializeForStorage(string json, out GameSaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return SaveLoadResult.Corrupt;
            }

            try
            {
                GameSaveData parsed = JsonUtility.FromJson<GameSaveData>(json);
                if (parsed == null)
                {
                    return SaveLoadResult.Corrupt;
                }

                if (parsed.Version != CurrentVersion)
                {
                    return SaveLoadResult.Incompatible;
                }

                if (string.IsNullOrWhiteSpace(parsed.SceneId))
                {
                    return SaveLoadResult.Corrupt;
                }

                data = parsed;
                return SaveLoadResult.Success;
            }
            catch (Exception)
            {
                return SaveLoadResult.Corrupt;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Cleanup must not replace the original save/reset failure.
            }
        }
    }
}
