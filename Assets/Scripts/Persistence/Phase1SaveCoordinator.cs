using BeyondTheBeat.Missions;
using BeyondTheBeat.Survival;
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
        [SerializeField] private ForestSurvivalController survivalController;
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
        public ForestSurvivalController SurvivalController => survivalController;
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
            bool hasSurvivalState = survivalController != null && survivalController.Resource != null;
            MissionProgressSnapshot missionProgress = missionManager.Progress;

            return new GameSaveData
            {
                Version = SaveManager.CurrentVersion,
                SceneId = gameObject.scene.name,
                VehicleTransform = SavedTransform.Capture(vehicleTransform),
                MissionId = missionManager.CurrentMissionId,
                MissionState = missionManager.State,
                HasPhase2SurvivalState = hasSurvivalState,
                MissionTargetContextActive = hasSurvivalState && missionProgress.TargetContextActive,
                MissionSurvivalElapsedSeconds = hasSurvivalState ? missionProgress.SurvivalElapsedSeconds : 0f,
                SurvivalResourceValue = hasSurvivalState ? survivalController.Resource.CurrentValue : 0f,
                SurvivalPressureActive = hasSurvivalState && survivalController.IsPressureActive,
                SurvivalRecovering = hasSurvivalState && survivalController.IsRecovering
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

            ApplyVehicleTransform(data.VehicleTransform);

            if (!missionManager.RestoreMissionState(data.MissionId, data.MissionState))
            {
                return false;
            }

            if (!data.HasPhase2SurvivalState)
            {
                return true;
            }

            if (survivalController == null || survivalController.Resource == null)
            {
                Debug.LogWarning("[Beyond The Beat] Phase 2 save contains survival state but no survival controller is configured.");
                return false;
            }

            if (!survivalController.RestorePersistentState(
                    data.SurvivalResourceValue,
                    data.SurvivalPressureActive,
                    data.SurvivalRecovering))
            {
                return false;
            }

            if (data.MissionState == MissionState.Active &&
                missionManager.CurrentMission != null &&
                missionManager.CurrentMission.ObjectiveType == MissionObjectiveType.ReachAndSurvive)
            {
                return missionManager.RestoreObjectiveProgress(
                    data.MissionTargetContextActive,
                    data.MissionSurvivalElapsedSeconds);
            }

            return true;
        }

        private void ApplyNewGameState()
        {
            if (vehicleTransform != null)
            {
                ApplyVehicleTransform(newGameVehicleTransform);
            }

            survivalController?.ResetResource();

            if (missionManager == null)
            {
                return;
            }

            if (newGameMission == null)
            {
                missionManager.ClearMission();
                return;
            }

            bool alreadyAtNewGameMission =
                missionManager.State == MissionState.Active &&
                missionManager.CurrentMissionId == newGameMission.MissionId;

            if (!alreadyAtNewGameMission)
            {
                missionManager.StartMission(newGameMission);
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
