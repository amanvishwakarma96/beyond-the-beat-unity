using System;
using System.Linq;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Puzzles;
using BeyondTheBeat.UI;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase3MissionBuilder
    {
        private const string ScenePath = Phase3RestrictedAreaBuilder.Phase3ScenePath;
        private const string MissionRootName = "Phase1MissionSystem";
        private const string VehicleName = "PrototypeVehicle";
        private const string RestrictedZoneId = "restricted-yard";
        private const string ForestZoneId = "forest";
        private const string RestrictedPuzzleId = "restricted-pressure-plate";

        private const string MissionAssetPath = "Assets/Settings/Missions/Phase3_ReachAndSolveRestrictedArea.asset";
        private const string Phase1ReachMissionAssetPath = "Assets/Settings/Missions/Phase1_ReachOffRoadCheckpoint.asset";
        private const string Phase2SurvivalMissionAssetPath = "Assets/Settings/Missions/Phase2_ReachAndSurviveForest.asset";
        private const string MissionId = "phase3-reach-and-solve-restricted-area";
        private const string MissionDisplayName = "Unlock the Restricted Area";
        private const string MissionDescription =
            "Solve the pressure-plate access puzzle, then enter the restricted compound through the unlocked gate.";

        [MenuItem("Beyond The Beat/Phase 3/Build Reach + Solve Mission")]
        public static void BuildReachAndSolveMission()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Reach + Solve build requires Phase 3 scene '{ScenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject vehicle = FindRootObject(scene, VehicleName);
            GameObject missionRoot = FindRootObject(scene, MissionRootName);
            MissionManager manager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
            ZoneContext restrictedZone = FindZone(scene, RestrictedZoneId);
            PuzzleStateController puzzle = FindSinglePuzzle(scene);

            if (vehicle == null || manager == null || restrictedZone == null || puzzle == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] Reach + Solve build requires the inherited MissionManager, PrototypeVehicle, " +
                    "restricted ZoneContext, and the Phase 3 puzzle foundation. Build the previous milestone first.");
                return;
            }

            EnsureFolder("Assets", "Settings");
            EnsureFolder("Assets/Settings", "Missions");

            ConfigurePuzzleIdentity(puzzle);
            MissionDefinition mission = GetOrCreateMissionDefinition();
            ConfigureMissionManager(
                manager,
                mission,
                vehicle,
                FindZoneContexts(scene),
                FindPuzzleSources(scene));
            manager.RebindPuzzleSources();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save Reach + Solve setup into '{ScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = mission;

            Debug.Log(
                "[Beyond The Beat] Phase 3 Reach + Solve mission created. " +
                $"Mission '{MissionId}' requires ZoneContext '{RestrictedZoneId}' and puzzle '{RestrictedPuzzleId}'.");
        }

        [MenuItem("Beyond The Beat/Phase 3/Validate Reach + Solve Mission")]
        public static void ValidateReachAndSolveMission()
        {
            if (ValidateReachAndSolveMissionInternal(out string message))
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public static bool ValidateReachAndSolveMissionOrThrow()
        {
            if (ValidateReachAndSolveMissionInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateReachAndSolveMissionInternal(out string message)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            MissionDefinition mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionAssetPath);
            MissionDefinition phase1Mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(Phase1ReachMissionAssetPath);
            MissionDefinition phase2Mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(Phase2SurvivalMissionAssetPath);

            if (sceneAsset == null || mission == null || phase1Mission == null || phase2Mission == null)
            {
                message = "[Beyond The Beat] Reach + Solve validation FAIL: scene or required mission asset is missing.";
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
                MissionManager manager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                ZoneContext restrictedZone = FindZone(validationScene, RestrictedZoneId);
                ZoneContext forestZone = FindZone(validationScene, ForestZoneId);
                PuzzleStateController puzzle = FindPuzzle(validationScene, RestrictedPuzzleId);
                MissionHud hud = validationScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MissionHud>(true))
                    .FirstOrDefault();

                bool definitionPass = mission.IsConfigured &&
                                      mission.MissionId == MissionId &&
                                      mission.DisplayName == MissionDisplayName &&
                                      mission.ObjectiveType == MissionObjectiveType.ReachAndSolve &&
                                      mission.TargetZoneId == RestrictedZoneId &&
                                      mission.TargetPuzzleId == RestrictedPuzzleId &&
                                      Mathf.Approximately(mission.SurvivalDurationSeconds, 0f);

                bool puzzleIdentityPass = puzzle != null &&
                                          puzzle.IsConfigured &&
                                          puzzle.PuzzleId == RestrictedPuzzleId &&
                                          FindPuzzleSources(validationScene)
                                              .Count(item => item.PuzzleId == RestrictedPuzzleId) == 1;

                bool managerPass = manager != null &&
                                   manager.StartingMission == mission &&
                                   manager.PlayerActor == vehicle &&
                                   manager.ObservedZoneCount >= 5 &&
                                   manager.ObservedPuzzleCount >= 1;

                bool previousMissionRegressionPass = phase1Mission.IsConfigured &&
                                                     phase1Mission.ObjectiveType == MissionObjectiveType.ReachLocation &&
                                                     phase2Mission.IsConfigured &&
                                                     phase2Mission.ObjectiveType == MissionObjectiveType.ReachAndSurvive;

                bool targetEvaluationPass = vehicle != null &&
                                            restrictedZone != null &&
                                            forestZone != null &&
                                            MissionObjectiveEvaluator.IsTargetZone(mission, restrictedZone, vehicle, vehicle) &&
                                            !MissionObjectiveEvaluator.IsTargetZone(mission, forestZone, vehicle, vehicle) &&
                                            !MissionObjectiveEvaluator.IsTargetZone(mission, restrictedZone, missionRoot, vehicle);

                bool reachThenSolvePass = false;
                bool solveThenReachPass = false;
                bool resetRetryPass = false;
                bool hudPass = false;

                if (managerPass && puzzle != null && restrictedZone != null && forestZone != null && vehicle != null)
                {
                    manager.RebindPuzzleSources();
                    puzzle.ResetPuzzle();

                    bool started = manager.StartMission(mission);
                    bool wrongZoneRejected = !manager.TryProcessZoneEntry(forestZone, vehicle);
                    bool targetAccepted = manager.TryProcessZoneEntry(restrictedZone, vehicle);
                    bool waitsForPuzzle = manager.State == MissionState.Active &&
                                          manager.Progress.TargetContextActive &&
                                          !manager.Progress.PuzzleSolved;
                    puzzle.SetSolved(true);
                    reachThenSolvePass = started &&
                                         wrongZoneRejected &&
                                         targetAccepted &&
                                         waitsForPuzzle &&
                                         manager.State == MissionState.Completed &&
                                         !manager.HasActiveMission;

                    puzzle.ResetPuzzle();
                    manager.StartMission(mission);
                    puzzle.SetSolved(true);
                    bool solvedOutside = manager.State == MissionState.Active &&
                                         !manager.Progress.TargetContextActive &&
                                         manager.Progress.PuzzleSolved;
                    bool completedOnArrival = manager.TryProcessZoneEntry(restrictedZone, vehicle) &&
                                              manager.State == MissionState.Completed;
                    solveThenReachPass = solvedOutside && completedOnArrival;

                    puzzle.ResetPuzzle();
                    manager.StartMission(mission);
                    puzzle.SetSolved(true);
                    bool firstSolveObserved = manager.Progress.PuzzleSolved && manager.State == MissionState.Active;
                    puzzle.ResetPuzzle();
                    bool resetObserved = !manager.Progress.PuzzleSolved && manager.State == MissionState.Active;
                    manager.TryProcessZoneEntry(restrictedZone, vehicle);
                    bool stillBlockedAfterReset = manager.State == MissionState.Active &&
                                                  manager.Progress.TargetContextActive &&
                                                  !manager.Progress.PuzzleSolved;
                    puzzle.SetSolved(true);
                    resetRetryPass = firstSolveObserved &&
                                     resetObserved &&
                                     stillBlockedAfterReset &&
                                     manager.State == MissionState.Completed;

                    puzzle.ResetPuzzle();
                    manager.StartMission(mission);
                    MissionHudSnapshot initialSnapshot = MissionHud.CreateSnapshot(mission, manager.State, manager.Progress);
                    puzzle.SetSolved(true);
                    MissionHudSnapshot solvedSnapshot = MissionHud.CreateSnapshot(mission, manager.State, manager.Progress);
                    hudPass = hud != null &&
                              hud.MissionManager == manager &&
                              initialSnapshot.Status == "SOLVE PUZZLE • REACH AREA" &&
                              solvedSnapshot.Status == "PUZZLE SOLVED • ENTER AREA" &&
                              manager.Progress.NormalizedProgress >= 0.49f &&
                              manager.Progress.NormalizedProgress <= 0.51f;

                    manager.ClearMission();
                    puzzle.ResetPuzzle();
                }

                bool allPass = definitionPass &&
                               puzzleIdentityPass &&
                               managerPass &&
                               previousMissionRegressionPass &&
                               targetEvaluationPass &&
                               reachThenSolvePass &&
                               solveThenReachPass &&
                               resetRetryPass &&
                               hudPass;

                message =
                    "[Beyond The Beat] Phase 3 Reach + Solve mission validation\n" +
                    $"ScriptableObject ReachAndSolve definition: {PassFail(definitionPass)}\n" +
                    $"Stable/unique puzzle identity: {PassFail(puzzleIdentityPass)}\n" +
                    $"MissionManager zone/puzzle wiring: {PassFail(managerPass)}\n" +
                    $"Reach Location + ReachAndSurvive regression: {PassFail(previousMissionRegressionPass)}\n" +
                    $"Target/wrong-zone/wrong-actor evaluation: {PassFail(targetEvaluationPass)}\n" +
                    $"Reach first then solve completion: {PassFail(reachThenSolvePass)}\n" +
                    $"Solve first then reach completion: {PassFail(solveThenReachPass)}\n" +
                    $"Puzzle reset/retry mission state: {PassFail(resetRetryPass)}\n" +
                    $"Mission HUD two-step progress: {PassFail(hudPass)}";

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

        private static MissionDefinition GetOrCreateMissionDefinition()
        {
            MissionDefinition mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionAssetPath);
            if (mission == null)
            {
                mission = ScriptableObject.CreateInstance<MissionDefinition>();
                mission.name = "Phase3_ReachAndSolveRestrictedArea";
                AssetDatabase.CreateAsset(mission, MissionAssetPath);
            }

            SerializedObject serialized = new SerializedObject(mission);
            SetString(serialized, "missionId", MissionId);
            SetString(serialized, "displayName", MissionDisplayName);
            SetString(serialized, "description", MissionDescription);
            SetString(serialized, "targetZoneId", RestrictedZoneId);
            SetString(serialized, "targetPuzzleId", RestrictedPuzzleId);
            SetFloat(serialized, "survivalDurationSeconds", 0f);

            SerializedProperty objectiveType = serialized.FindProperty("objectiveType");
            if (objectiveType == null)
            {
                throw new InvalidOperationException("MissionDefinition objectiveType field could not be resolved.");
            }

            objectiveType.intValue = (int)MissionObjectiveType.ReachAndSolve;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mission);
            return mission;
        }

        private static void ConfigurePuzzleIdentity(PuzzleStateController puzzle)
        {
            SerializedObject serialized = new SerializedObject(puzzle);
            SetString(serialized, "puzzleId", RestrictedPuzzleId);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(puzzle);
        }

        private static void ConfigureMissionManager(
            MissionManager manager,
            MissionDefinition mission,
            GameObject playerActor,
            ZoneContext[] observedZones,
            PuzzleStateController[] observedPuzzles)
        {
            SerializedObject serialized = new SerializedObject(manager);
            SetObjectReference(serialized, "startingMission", mission);
            SetObjectReference(serialized, "playerActor", playerActor);

            SerializedProperty startOnPlay = serialized.FindProperty("startOnPlay");
            SerializedProperty zones = serialized.FindProperty("observedZones");
            SerializedProperty puzzles = serialized.FindProperty("observedPuzzles");
            if (startOnPlay == null || zones == null || puzzles == null)
            {
                throw new InvalidOperationException("MissionManager serialized fields could not be resolved for Phase 3 mission setup.");
            }

            startOnPlay.boolValue = true;
            zones.arraySize = observedZones.Length;
            for (int i = 0; i < observedZones.Length; i++)
            {
                zones.GetArrayElementAtIndex(i).objectReferenceValue = observedZones[i];
            }

            puzzles.arraySize = observedPuzzles.Length;
            for (int i = 0; i < observedPuzzles.Length; i++)
            {
                puzzles.GetArrayElementAtIndex(i).objectReferenceValue = observedPuzzles[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
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
                .OrderBy(puzzle => puzzle.PuzzleId, StringComparer.Ordinal)
                .ToArray();
        }

        private static PuzzleStateController FindSinglePuzzle(Scene scene)
        {
            PuzzleStateController[] puzzles = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PuzzleStateController>(true))
                .ToArray();
            return puzzles.Length == 1 ? puzzles[0] : null;
        }

        private static PuzzleStateController FindPuzzle(Scene scene, string puzzleId)
        {
            return FindPuzzleSources(scene)
                .FirstOrDefault(puzzle => string.Equals(puzzle.PuzzleId, puzzleId, StringComparison.Ordinal));
        }

        private static ZoneContext FindZone(Scene scene, string zoneId)
        {
            return FindZoneContexts(scene)
                .FirstOrDefault(zone => string.Equals(zone.ZoneId, zoneId, StringComparison.Ordinal));
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void SetString(SerializedObject target, string propertyName, string value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized string field '{propertyName}' could not be resolved.");
            }

            property.stringValue = value;
        }

        private static void SetFloat(SerializedObject target, string propertyName, float value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized float field '{propertyName}' could not be resolved.");
            }

            property.floatValue = value;
        }

        private static void SetObjectReference(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized object field '{propertyName}' could not be resolved.");
            }

            property.objectReferenceValue = value;
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
