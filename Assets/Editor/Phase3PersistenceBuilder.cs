using System;
using System.Linq;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Persistence;
using BeyondTheBeat.Puzzles;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase3PersistenceBuilder
    {
        private const string ScenePath = Phase3RestrictedAreaBuilder.Phase3ScenePath;
        private const string PersistenceRootName = "Phase1Persistence";
        private const string MissionRootName = "Phase1MissionSystem";
        private const string VehicleName = "PrototypeVehicle";
        private const string RestrictedZoneId = "restricted-yard";
        private const string RestrictedPuzzleId = "restricted-pressure-plate";
        private const string Phase3MissionAssetPath = "Assets/Settings/Missions/Phase3_ReachAndSolveRestrictedArea.asset";

        [MenuItem("Beyond The Beat/Phase 3/Build Persistence Resume")]
        public static void BuildPersistenceResume()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Phase 3 persistence build requires scene '{ScenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject persistenceRoot = FindRootObject(scene, PersistenceRootName);
            Phase1SaveCoordinator coordinator =
                persistenceRoot != null ? persistenceRoot.GetComponent<Phase1SaveCoordinator>() : null;

            if (coordinator == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] Phase 3 persistence requires the inherited Phase1Persistence/Phase1SaveCoordinator.");
                return;
            }

            ConfigureCoordinator(coordinator, FindZoneContexts(scene), FindPuzzleSources(scene));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save Phase 3 persistence wiring into '{ScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = persistenceRoot;

            Debug.Log(
                "[Beyond The Beat] Phase 3 persistence wiring created. " +
                "Reach + Solve target context and stable puzzle states are now included in the additive local-save contract.");
        }

        [MenuItem("Beyond The Beat/Phase 3/Validate Persistence Resume")]
        public static void ValidatePersistenceResume()
        {
            if (ValidatePersistenceResumeInternal(out string message))
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public static bool ValidatePersistenceResumeOrThrow()
        {
            if (ValidatePersistenceResumeInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidatePersistenceResumeInternal(out string message)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            MissionDefinition mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(Phase3MissionAssetPath);
            if (sceneAsset == null || mission == null)
            {
                message = "[Beyond The Beat] Phase 3 persistence validation FAIL: scene or Reach + Solve mission asset is missing.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject vehicle = FindRootObject(validationScene, VehicleName);
                GameObject missionRoot = FindRootObject(validationScene, MissionRootName);
                GameObject persistenceRoot = FindRootObject(validationScene, PersistenceRootName);
                MissionManager manager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                SaveManager saveManager = persistenceRoot != null ? persistenceRoot.GetComponent<SaveManager>() : null;
                Phase1SaveCoordinator coordinator =
                    persistenceRoot != null ? persistenceRoot.GetComponent<Phase1SaveCoordinator>() : null;
                ZoneContext restrictedZone = FindZone(validationScene, RestrictedZoneId);
                PuzzleStateController puzzle = FindPuzzle(validationScene, RestrictedPuzzleId);

                bool wiringPass = vehicle != null &&
                                  manager != null &&
                                  saveManager != null &&
                                  coordinator != null &&
                                  coordinator.SaveManager == saveManager &&
                                  coordinator.MissionManager == manager &&
                                  coordinator.VehicleTransform == vehicle.transform &&
                                  coordinator.PersistentZoneCount >= 5 &&
                                  coordinator.PersistentPuzzleCount >= 1 &&
                                  restrictedZone != null &&
                                  puzzle != null;

                GameSaveData roundTripSource = new GameSaveData
                {
                    Version = SaveManager.CurrentVersion,
                    SceneId = validationScene.name,
                    VehicleTransform = new SavedTransform(new Vector3(201f, 1f, 2f), Quaternion.Euler(0f, 90f, 0f)),
                    MissionId = mission.MissionId,
                    MissionState = MissionState.Active,
                    HasPhase3PuzzleState = true,
                    MissionReachAndSolveTargetContextActive = true,
                    Phase3PuzzleStates = new[]
                    {
                        new SavedPuzzleState(RestrictedPuzzleId, false)
                    }
                };

                string json = SaveManager.SerializeForStorage(roundTripSource);
                SaveLoadResult roundTripResult = SaveManager.DeserializeForStorage(json, out GameSaveData roundTrip);
                bool roundTripPass = roundTripResult == SaveLoadResult.Success &&
                                     roundTrip != null &&
                                     roundTrip.HasPhase3PuzzleState &&
                                     roundTrip.MissionReachAndSolveTargetContextActive &&
                                     roundTrip.Phase3PuzzleStates != null &&
                                     roundTrip.Phase3PuzzleStates.Length == 1 &&
                                     roundTrip.Phase3PuzzleStates[0].PuzzleId == RestrictedPuzzleId &&
                                     !roundTrip.Phase3PuzzleStates[0].IsSolved;

                const string legacyPhase2Json =
                    "{\"Version\":1,\"SceneId\":\"Phase2_Forest\",\"MissionId\":\"phase2-reach-and-survive-forest\",\"MissionState\":1,\"HasPhase2SurvivalState\":true,\"MissionTargetContextActive\":true,\"MissionSurvivalElapsedSeconds\":3.5}";
                SaveLoadResult legacyResult = SaveManager.DeserializeForStorage(legacyPhase2Json, out GameSaveData legacyData);
                bool legacyCompatibilityPass = legacyResult == SaveLoadResult.Success &&
                                               legacyData != null &&
                                               !legacyData.HasPhase3PuzzleState &&
                                               !legacyData.MissionReachAndSolveTargetContextActive &&
                                               (legacyData.Phase3PuzzleStates == null || legacyData.Phase3PuzzleStates.Length == 0) &&
                                               legacyData.HasPhase2SurvivalState;

                bool unsolvedResumePass = false;
                bool solvedResumePass = false;
                bool completedResumePass = false;
                bool resetRetryPass = false;

                if (wiringPass)
                {
                    manager.RebindPuzzleSources();

                    puzzle.RestorePersistentState(false);
                    bool activeRestored = manager.RestoreMissionState(mission.MissionId, MissionState.Active);
                    bool targetRestored = manager.TryProcessZoneEntry(restrictedZone, vehicle);
                    bool blockedUntilSolve = manager.State == MissionState.Active &&
                                             manager.Progress.TargetContextActive &&
                                             !manager.Progress.PuzzleSolved;
                    puzzle.SetSolved(true);
                    unsolvedResumePass = activeRestored &&
                                         targetRestored &&
                                         blockedUntilSolve &&
                                         manager.State == MissionState.Completed;

                    puzzle.RestorePersistentState(true);
                    bool solvedActiveRestored = manager.RestoreMissionState(mission.MissionId, MissionState.Active);
                    bool solvedStateRestored = manager.State == MissionState.Active &&
                                               !manager.Progress.TargetContextActive &&
                                               manager.Progress.PuzzleSolved;
                    bool completedOnArrival = manager.TryProcessZoneEntry(restrictedZone, vehicle) &&
                                              manager.State == MissionState.Completed;
                    solvedResumePass = solvedActiveRestored && solvedStateRestored && completedOnArrival;

                    puzzle.RestorePersistentState(true);
                    bool completedRestored = manager.RestoreMissionState(mission.MissionId, MissionState.Completed);
                    completedResumePass = completedRestored &&
                                          manager.State == MissionState.Completed &&
                                          !manager.HasActiveMission &&
                                          puzzle.IsSolved;

                    puzzle.RestorePersistentState(true);
                    puzzle.ResetToConfiguredStartState();
                    bool configuredReset = puzzle.IsSolved == puzzle.SolvedOnStart;
                    bool retryStarted = manager.StartMission(mission);
                    bool enteredUnsolved = manager.TryProcessZoneEntry(restrictedZone, vehicle);
                    bool retryBlocked = manager.State == MissionState.Active &&
                                        manager.Progress.TargetContextActive &&
                                        !manager.Progress.PuzzleSolved;
                    resetRetryPass = configuredReset && retryStarted && enteredUnsolved && retryBlocked;

                    manager.ClearMission();
                    puzzle.ResetToConfiguredStartState();
                }

                bool allPass = wiringPass &&
                               roundTripPass &&
                               legacyCompatibilityPass &&
                               unsolvedResumePass &&
                               solvedResumePass &&
                               completedResumePass &&
                               resetRetryPass;

                message =
                    "[Beyond The Beat] Phase 3 persistence/resume validation\n" +
                    $"Coordinator zone/puzzle wiring: {PassFail(wiringPass)}\n" +
                    $"Phase 3 save JSON round-trip: {PassFail(roundTripPass)}\n" +
                    $"Older Phase 2 JSON additive compatibility: {PassFail(legacyCompatibilityPass)}\n" +
                    $"Unsolved target-context resume then solve: {PassFail(unsolvedResumePass)}\n" +
                    $"Solved puzzle resume then target entry: {PassFail(solvedResumePass)}\n" +
                    $"Completed Reach + Solve state restore: {PassFail(completedResumePass)}\n" +
                    $"Configured puzzle reset + mission retry: {PassFail(resetRetryPass)}";

                return allPass;
            }
            finally
            {
                if (openedForValidation && validationScene.IsValid())
                {
                    EditorSceneManager.CloseScene(validationScene, true);
                }
            }
        }

        private static void ConfigureCoordinator(
            Phase1SaveCoordinator coordinator,
            ZoneContext[] zones,
            PuzzleStateController[] puzzles)
        {
            SerializedObject serialized = new SerializedObject(coordinator);
            SerializedProperty persistentZones = serialized.FindProperty("persistentZones");
            SerializedProperty persistentPuzzles = serialized.FindProperty("persistentPuzzles");
            if (persistentZones == null || persistentPuzzles == null)
            {
                throw new InvalidOperationException(
                    "Phase1SaveCoordinator Phase 3 persistence fields could not be resolved.");
            }

            persistentZones.arraySize = zones.Length;
            for (int i = 0; i < zones.Length; i++)
            {
                persistentZones.GetArrayElementAtIndex(i).objectReferenceValue = zones[i];
            }

            persistentPuzzles.arraySize = puzzles.Length;
            for (int i = 0; i < puzzles.Length; i++)
            {
                persistentPuzzles.GetArrayElementAtIndex(i).objectReferenceValue = puzzles[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(coordinator);
        }

        private static ZoneContext[] FindZoneContexts(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ZoneContext>(true))
                .OrderBy(zone => zone.ZoneId, StringComparer.Ordinal)
                .ToArray();
        }

        private static PuzzleStateController[] FindPuzzleSources(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PuzzleStateController>(true))
                .Where(puzzle => puzzle != null && puzzle.IsConfigured)
                .OrderBy(puzzle => puzzle.PuzzleId, StringComparer.Ordinal)
                .ToArray();
        }

        private static ZoneContext FindZone(Scene scene, string zoneId)
        {
            return FindZoneContexts(scene)
                .FirstOrDefault(zone => string.Equals(zone.ZoneId, zoneId, StringComparison.Ordinal));
        }

        private static PuzzleStateController FindPuzzle(Scene scene, string puzzleId)
        {
            return FindPuzzleSources(scene)
                .FirstOrDefault(puzzle => string.Equals(puzzle.PuzzleId, puzzleId, StringComparison.Ordinal));
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
