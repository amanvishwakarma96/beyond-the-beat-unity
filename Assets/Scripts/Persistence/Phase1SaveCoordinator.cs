using System;
using System.Collections.Generic;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Puzzles;
using BeyondTheBeat.Survival;
using BeyondTheBeat.World;
using UnityEngine;

namespace BeyondTheBeat.Persistence
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class Phase1SaveCoordinator : MonoBehaviour
    {
        private static readonly HashSet<string> CompatibleIntegratedSceneIds =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Phase1_MVP",
                "Phase2_Forest",
                "Phase3_RestrictedArea"
            };

        [SerializeField] private SaveManager saveManager;
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private Transform vehicleTransform;
        [SerializeField] private ForestSurvivalController survivalController;
        [SerializeField] private ZoneContext[] persistentZones = Array.Empty<ZoneContext>();
        [SerializeField] private PuzzleStateController[] persistentPuzzles = Array.Empty<PuzzleStateController>();
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
        public int PersistentZoneCount => persistentZones != null ? persistentZones.Length : 0;
        public int PersistentPuzzleCount => persistentPuzzles != null ? persistentPuzzles.Length : 0;
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
            SavedPuzzleState[] puzzleStates = CapturePuzzleStates();
            bool reachAndSolveActiveOrResolved = missionManager.CurrentMission != null &&
                                                missionManager.CurrentMission.ObjectiveType == MissionObjectiveType.ReachAndSolve;

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
                SurvivalRecovering = hasSurvivalState && survivalController.IsRecovering,
                HasPhase3PuzzleState = puzzleStates.Length > 0,
                MissionReachAndSolveTargetContextActive =
                    reachAndSolveActiveOrResolved && missionProgress.TargetContextActive,
                Phase3PuzzleStates = puzzleStates
            };
        }

        private bool ApplyLoadedState(GameSaveData data)
        {
            if (data == null || data.Version != SaveManager.CurrentVersion)
            {
                return false;
            }

            if (!IsCompatibleSceneId(data.SceneId))
            {
                Debug.LogWarning(
                    $"[Beyond The Beat] Save scene '{data.SceneId}' is not compatible with active integrated scene '{gameObject.scene.name}'.");
                return false;
            }

            ApplyVehicleTransform(data.VehicleTransform);

            if (!RestorePuzzleStates(data))
            {
                return false;
            }

            if (!missionManager.RestoreMissionState(data.MissionId, data.MissionState))
            {
                return false;
            }

            if (data.HasPhase2SurvivalState)
            {
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
            }

            if (data.MissionState != MissionState.Active || missionManager.CurrentMission == null)
            {
                return true;
            }

            if (missionManager.CurrentMission.ObjectiveType == MissionObjectiveType.ReachAndSurvive)
            {
                return missionManager.RestoreObjectiveProgress(
                    data.MissionTargetContextActive,
                    data.MissionSurvivalElapsedSeconds);
            }

            if (missionManager.CurrentMission.ObjectiveType == MissionObjectiveType.ReachAndSolve)
            {
                return RestoreReachAndSolveProgress(data.MissionReachAndSolveTargetContextActive);
            }

            return true;
        }

        private bool RestoreReachAndSolveProgress(bool restoredTargetContextActive)
        {
            if (!restoredTargetContextActive)
            {
                return true;
            }

            MissionDefinition mission = missionManager.CurrentMission;
            ZoneContext targetZone = mission != null ? FindPersistentZone(mission.TargetZoneId) : null;
            if (targetZone == null || vehicleTransform == null)
            {
                Debug.LogWarning(
                    "[Beyond The Beat] Reach + Solve resume could not resolve the saved target ZoneContext.");
                return false;
            }

            return missionManager.TryProcessZoneEntry(targetZone, vehicleTransform.gameObject);
        }

        private SavedPuzzleState[] CapturePuzzleStates()
        {
            if (persistentPuzzles == null || persistentPuzzles.Length == 0)
            {
                return Array.Empty<SavedPuzzleState>();
            }

            List<SavedPuzzleState> states = new List<SavedPuzzleState>(persistentPuzzles.Length);
            for (int i = 0; i < persistentPuzzles.Length; i++)
            {
                PuzzleStateController puzzle = persistentPuzzles[i];
                if (puzzle == null || !puzzle.IsConfigured)
                {
                    continue;
                }

                states.Add(new SavedPuzzleState(puzzle.PuzzleId, puzzle.IsSolved));
            }

            return states.ToArray();
        }

        private bool RestorePuzzleStates(GameSaveData data)
        {
            ResetPuzzlesToConfiguredState();
            if (!data.HasPhase3PuzzleState)
            {
                return true;
            }

            SavedPuzzleState[] savedStates = data.Phase3PuzzleStates;
            if (savedStates == null)
            {
                Debug.LogWarning("[Beyond The Beat] Phase 3 save declared puzzle state but contained no puzzle snapshots.");
                return false;
            }

            for (int i = 0; i < savedStates.Length; i++)
            {
                SavedPuzzleState saved = savedStates[i];
                PuzzleStateController puzzle = FindPersistentPuzzle(saved.PuzzleId);
                if (puzzle == null)
                {
                    Debug.LogWarning(
                        $"[Beyond The Beat] Saved puzzle '{saved.PuzzleId}' is not available in the active Phase 3 scene.");
                    return false;
                }

                if (!puzzle.RestorePersistentState(saved.IsSolved))
                {
                    return false;
                }
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
            ResetPuzzlesToConfiguredState();

            if (missionManager == null)
            {
                return;
            }

            if (newGameMission == null)
            {
                missionManager.ClearMission();
                return;
            }

            missionManager.StartMission(newGameMission);
        }

        private void ResetPuzzlesToConfiguredState()
        {
            if (persistentPuzzles == null)
            {
                return;
            }

            for (int i = 0; i < persistentPuzzles.Length; i++)
            {
                persistentPuzzles[i]?.ResetToConfiguredStartState();
            }
        }

        private ZoneContext FindPersistentZone(string zoneId)
        {
            if (persistentZones == null || string.IsNullOrWhiteSpace(zoneId))
            {
                return null;
            }

            for (int i = 0; i < persistentZones.Length; i++)
            {
                ZoneContext zone = persistentZones[i];
                if (zone != null && string.Equals(zone.ZoneId, zoneId, StringComparison.Ordinal))
                {
                    return zone;
                }
            }

            return null;
        }

        private PuzzleStateController FindPersistentPuzzle(string puzzleId)
        {
            if (persistentPuzzles == null || string.IsNullOrWhiteSpace(puzzleId))
            {
                return null;
            }

            for (int i = 0; i < persistentPuzzles.Length; i++)
            {
                PuzzleStateController puzzle = persistentPuzzles[i];
                if (puzzle != null &&
                    puzzle.IsConfigured &&
                    string.Equals(puzzle.PuzzleId, puzzleId, StringComparison.Ordinal))
                {
                    return puzzle;
                }
            }

            return null;
        }

        private bool IsCompatibleSceneId(string savedSceneId)
        {
            if (string.IsNullOrWhiteSpace(savedSceneId))
            {
                return false;
            }

            return string.Equals(savedSceneId, gameObject.scene.name, StringComparison.Ordinal) ||
                   CompatibleIntegratedSceneIds.Contains(savedSceneId);
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
