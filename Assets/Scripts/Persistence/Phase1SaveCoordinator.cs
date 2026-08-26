using BeyondTheBeat.Missions;
using UnityEngine;

namespace BeyondTheBeat.Persistence
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class Phase1SaveCoordinator : MonoBehaviour
    {
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private Transform vehicleTransform;
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool saveOnApplicationPause = true;
        [SerializeField] private bool saveOnApplicationQuit = true;

        private SavedTransform newGameVehicleTransform;
        private MissionDefinition newGameMission;
        private Rigidbody vehicleBody;
        private bool initialized;

        public SaveManager SaveManager => saveManager;
        public MissionManager MissionManager => missionManager;
        public Transform VehicleTransform => vehicleTransform;
        public bool LoadOnStart => loadOnStart;
        public bool SaveOnApplicationPause => saveOnApplicationPause;
        public bool SaveOnApplicationQuit => saveOnApplicationQuit;

        private void Awake()
        {
            if (vehicleTransform != null)
            {
                newGameVehicleTransform = SavedTransform.Capture(vehicleTransform);
                vehicleBody = vehicleTransform.GetComponent<Rigidbody>();
            }

            if (missionManager != null)
            {
                newGameMission = missionManager.StartingMission;
            }
        }

        private void Start()
        {
            initialized = true;
            if (loadOnStart)
            {
                LoadNow();
            }
        }

        public bool SaveNow()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("[Beyond The Beat] Phase1SaveCoordinator cannot save because required references are missing.");
                return false;
            }

            GameSaveData data = CaptureCurrentState();
            return saveManager.Save(data);
        }

        public bool LoadNow()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("[Beyond The Beat] Phase1SaveCoordinator cannot load because required references are missing.");
                return false;
            }

            SaveLoadResult result = saveManager.Load(out GameSaveData data);
            if (result == SaveLoadResult.Success && ApplyLoadedState(data))
            {
                Debug.Log(
                    $"[Beyond The Beat] Local resume applied. Mission='{data.MissionId}', state={data.MissionState}.");
                return true;
            }

            ApplyNewGameState();
            if (result == SaveLoadResult.Missing)
            {
                Debug.Log("[Beyond The Beat] No local save found; using new-game state.");
            }
            else
            {
                Debug.LogWarning(
                    $"[Beyond The Beat] Local resume fallback applied after load result {result}.");
            }

            return false;
        }

        public bool ResetProgress()
        {
            if (saveManager == null)
            {
                return false;
            }

            bool reset = saveManager.ResetSave();
            ApplyNewGameState();
            return reset;
        }

        private void OnApplicationPause(bool paused)
        {
            if (initialized && paused && saveOnApplicationPause)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            if (initialized && saveOnApplicationQuit)
            {
                SaveNow();
            }
        }

        private GameSaveData CaptureCurrentState()
        {
            return new GameSaveData
            {
                Version = SaveManager.CurrentVersion,
                SceneId = gameObject.scene.name,
                VehicleTransform = SavedTransform.Capture(vehicleTransform),
                MissionId = missionManager.CurrentMissionId,
                MissionState = missionManager.State
            };
        }

        private bool ApplyLoadedState(GameSaveData data)
        {
            if (data == null || data.Version != SaveManager.CurrentVersion)
            {
                return false;
            }

            if (!string.Equals(data.SceneId, gameObject.scene.name, System.StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    $"[Beyond The Beat] Save scene '{data.SceneId}' does not match active scene '{gameObject.scene.name}'.");
                return false;
            }

            if (!missionManager.RestoreMissionState(data.MissionId, data.MissionState))
            {
                return false;
            }

            ApplyVehicleTransform(data.VehicleTransform);
            return true;
        }

        private void ApplyNewGameState()
        {
            if (vehicleTransform != null)
            {
                ApplyVehicleTransform(newGameVehicleTransform);
            }

            if (missionManager == null)
            {
                return;
            }

            if (newGameMission != null)
            {
                missionManager.StartMission(newGameMission);
            }
            else
            {
                missionManager.ClearMission();
            }
        }

        private void ApplyVehicleTransform(SavedTransform savedTransform)
        {
            Vector3 position = savedTransform.Position.ToVector3();
            Quaternion rotation = savedTransform.Rotation.ToQuaternion();

            if (vehicleBody != null)
            {
                vehicleBody.position = position;
                vehicleBody.rotation = rotation;
                vehicleBody.linearVelocity = Vector3.zero;
                vehicleBody.angularVelocity = Vector3.zero;
                return;
            }

            if (vehicleTransform != null)
            {
                vehicleTransform.SetPositionAndRotation(position, rotation);
            }
        }

        private bool HasRequiredReferences()
        {
            return saveManager != null && missionManager != null && vehicleTransform != null;
        }
    }
}
