using System;
using System.Linq;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Survival;
using BeyondTheBeat.UI;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase2MissionBuilder
    {
        private const string ScenePath = Phase2WorldBuilder.Phase2ScenePath;
        private const string MissionRootName = "Phase1MissionSystem";
        private const string SurvivalRootName = "Phase2SurvivalSystem";
        private const string VehicleName = "PrototypeVehicle";
        private const string ForestZoneId = "forest";
        private const string OffRoadZoneId = "off-road";

        private const string MissionAssetPath = "Assets/Settings/Missions/Phase2_ReachAndSurviveForest.asset";
        private const string Phase1ReachMissionAssetPath = "Assets/Settings/Missions/Phase1_ReachOffRoadCheckpoint.asset";
        private const string MissionId = "phase2-reach-and-survive-forest";
        private const string MissionDisplayName = "Reach and Survive the Forest";
        private const string MissionDescription =
            "Reach the forest and remain inside the environmental pressure zone until the survival timer completes.";
        private const float SurvivalDurationSeconds = 8f;

        [MenuItem("Beyond The Beat/Phase 2/Build Reach + Survive Mission")]
        public static void BuildReachAndSurviveMission()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError(
                    $"[Beyond The Beat] Reach + Survive build requires the generated Phase 2 scene at '{ScenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject vehicle = FindRootObject(scene, VehicleName);
            GameObject missionRoot = FindRootObject(scene, MissionRootName);
            GameObject survivalRoot = FindRootObject(scene, SurvivalRootName);
            ZoneContext forestZone = FindZone(scene, ForestZoneId);
            MissionManager manager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
            ForestSurvivalController survivalController =
                survivalRoot != null ? survivalRoot.GetComponent<ForestSurvivalController>() : null;

            if (vehicle == null || manager == null || forestZone == null || survivalController == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] Reach + Survive build requires the inherited MissionManager, PrototypeVehicle, " +
                    "forest ZoneContext, and Phase2SurvivalSystem. Build the previous Phase 2 milestones first.");
                return;
            }

            EnsureFolder("Assets", "Settings");
            EnsureFolder("Assets/Settings", "Missions");

            MissionDefinition mission = GetOrCreateMissionDefinition();
            ZoneContext[] observedZones = FindZoneContexts(scene);
            ConfigureMissionManager(manager, mission, vehicle, observedZones, survivalController);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save Reach + Survive setup into '{ScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = mission;

            Debug.Log(
                "[Beyond The Beat] Phase 2 Reach + Survive mission created. " +
                $"Mission '{MissionId}' targets ZoneContext '{ForestZoneId}' and requires {SurvivalDurationSeconds:0.#} seconds of continuous survival pressure.");
        }

        [MenuItem("Beyond The Beat/Phase 2/Validate Reach + Survive Mission")]
        public static void ValidateReachAndSurviveMission()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            MissionDefinition mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionAssetPath);
            MissionDefinition phase1ReachMission =
                AssetDatabase.LoadAssetAtPath<MissionDefinition>(Phase1ReachMissionAssetPath);

            if (sceneAsset == null || mission == null || phase1ReachMission == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] Reach + Survive validation FAIL: Phase 2 scene or required mission definition is missing.");
                return;
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
                GameObject survivalRoot = FindRootObject(validationScene, SurvivalRootName);
                ZoneContext forestZone = FindZone(validationScene, ForestZoneId);
                ZoneContext offRoadZone = FindZone(validationScene, OffRoadZoneId);
                MissionManager manager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                ForestSurvivalController survivalController =
                    survivalRoot != null ? survivalRoot.GetComponent<ForestSurvivalController>() : null;
                SurvivalResource resource = survivalRoot != null ? survivalRoot.GetComponent<SurvivalResource>() : null;
                MissionHud hud = validationScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MissionHud>(true))
                    .FirstOrDefault();

                bool definitionPass =
                    mission.IsConfigured &&
                    mission.MissionId == MissionId &&
                    mission.DisplayName == MissionDisplayName &&
                    mission.ObjectiveType == MissionObjectiveType.ReachAndSurvive &&
                    mission.TargetZoneId == ForestZoneId &&
                    Approximately(mission.SurvivalDurationSeconds, SurvivalDurationSeconds);

                bool managerPass =
                    manager != null &&
                    manager.StartingMission == mission &&
                    manager.PlayerActor == vehicle &&
                    manager.SurvivalController == survivalController &&
                    manager.ObservedZoneCount >= 4;

                bool reachLocationRegressionPass =
                    phase1ReachMission.IsConfigured &&
                    phase1ReachMission.ObjectiveType == MissionObjectiveType.ReachLocation &&
                    vehicle != null &&
                    offRoadZone != null &&
                    !MissionObjectiveEvaluator.IsSatisfied(phase1ReachMission, forestZone, vehicle, vehicle);

                ZoneContext phase1ReachTarget = FindZone(validationScene, phase1ReachMission.TargetZoneId);
                reachLocationRegressionPass =
                    reachLocationRegressionPass &&
                    phase1ReachTarget != null &&
                    MissionObjectiveEvaluator.IsSatisfied(phase1ReachMission, phase1ReachTarget, vehicle, vehicle);

                bool targetEvaluationPass =
                    vehicle != null &&
                    forestZone != null &&
                    offRoadZone != null &&
                    MissionObjectiveEvaluator.IsTargetZone(mission, forestZone, vehicle, vehicle) &&
                    !MissionObjectiveEvaluator.IsTargetZone(mission, offRoadZone, vehicle, vehicle) &&
                    !MissionObjectiveEvaluator.IsTargetZone(mission, forestZone, missionRoot, vehicle);

                bool timedLifecyclePass = false;
                bool exitResetPass = false;
                bool depletionFailurePass = false;
                bool hudProgressPass = false;

                if (managerPass && survivalController != null && resource != null && forestZone != null)
                {
                    survivalController.ResetResource();
                    bool started = manager.StartMission(mission);
                    bool wrongZoneRejected = !manager.TryProcessZoneEntry(offRoadZone, vehicle);
                    bool pressureEntered = survivalController.TryEnterContext(forestZone, vehicle);
                    bool targetAccepted = manager.TryProcessZoneEntry(forestZone, vehicle);
                    bool earlyCompletionRejected = !manager.TickMission(SurvivalDurationSeconds * 0.5f) &&
                                                   manager.State == MissionState.Active &&
                                                   manager.Progress.TargetContextActive &&
                                                   manager.Progress.SurvivalElapsedSeconds > 0f &&
                                                   manager.Progress.SurvivalElapsedSeconds < SurvivalDurationSeconds;
                    bool completedOnRequirement = manager.TickMission(SurvivalDurationSeconds) &&
                                                  manager.State == MissionState.Completed &&
                                                  !manager.HasActiveMission;
                    timedLifecyclePass =
                        started &&
                        wrongZoneRejected &&
                        pressureEntered &&
                        targetAccepted &&
                        earlyCompletionRejected &&
                        completedOnRequirement;

                    manager.StartMission(mission);
                    survivalController.ResetResource();
                    survivalController.TryEnterContext(forestZone, vehicle);
                    manager.TryProcessZoneEntry(forestZone, vehicle);
                    manager.TickMission(1f);
                    bool exited = manager.TryProcessZoneExit(forestZone, vehicle);
                    exitResetPass =
                        exited &&
                        manager.State == MissionState.Active &&
                        !manager.Progress.TargetContextActive &&
                        Approximately(manager.Progress.SurvivalElapsedSeconds, 0f);

                    manager.StartMission(mission);
                    survivalController.ResetResource();
                    survivalController.TryEnterContext(forestZone, vehicle);
                    manager.TryProcessZoneEntry(forestZone, vehicle);
                    depletionFailurePass =
                        manager.TryProcessSurvivalDepleted() &&
                        manager.State == MissionState.Failed &&
                        !manager.HasActiveMission;

                    manager.StartMission(mission);
                    MissionHudSnapshot reachSnapshot = MissionHud.CreateSnapshot(mission, manager.State, manager.Progress);
                    survivalController.ResetResource();
                    survivalController.TryEnterContext(forestZone, vehicle);
                    manager.TryProcessZoneEntry(forestZone, vehicle);
                    manager.TickMission(SurvivalDurationSeconds * 0.5f);
                    MissionHudSnapshot surviveSnapshot = MissionHud.CreateSnapshot(mission, manager.State, manager.Progress);
                    hudProgressPass =
                        hud != null &&
                        hud.MissionManager == manager &&
                        reachSnapshot.Status == "REACH TARGET ZONE" &&
                        surviveSnapshot.Status.StartsWith("SURVIVING", StringComparison.Ordinal) &&
                        surviveSnapshot.Objective.Contains("/8s");

                    manager.ClearMission();
                    survivalController.ResetResource();
                }

                bool allPass =
                    definitionPass &&
                    managerPass &&
                    reachLocationRegressionPass &&
                    targetEvaluationPass &&
                    timedLifecyclePass &&
                    exitResetPass &&
                    depletionFailurePass &&
                    hudProgressPass;

                string message =
                    "[Beyond The Beat] Phase 2 Reach + Survive mission validation\n" +
                    $"ScriptableObject ReachAndSurvive definition: {PassFail(definitionPass)}\n" +
                    $"MissionManager world/survival wiring: {PassFail(managerPass)}\n" +
                    $"Existing Reach Location regression: {PassFail(reachLocationRegressionPass)}\n" +
                    $"Target/wrong-zone/wrong-actor evaluation: {PassFail(targetEvaluationPass)}\n" +
                    $"Reach then timed survival completion: {PassFail(timedLifecyclePass)}\n" +
                    $"Target exit resets continuous survival progress: {PassFail(exitResetPass)}\n" +
                    $"Survival depletion fails active objective: {PassFail(depletionFailurePass)}\n" +
                    $"Mission HUD reach/survival progress presentation: {PassFail(hudProgressPass)}";

                if (allPass)
                {
                    Debug.Log(message);
                }
                else
                {
                    Debug.LogError(message);
                }
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
                mission.name = "Phase2_ReachAndSurviveForest";
                AssetDatabase.CreateAsset(mission, MissionAssetPath);
            }

            SerializedObject serialized = new SerializedObject(mission);
            SetString(serialized, "missionId", MissionId);
            SetString(serialized, "displayName", MissionDisplayName);
            SetString(serialized, "description", MissionDescription);
            SetString(serialized, "targetZoneId", ForestZoneId);
            SetFloat(serialized, "survivalDurationSeconds", SurvivalDurationSeconds);

            SerializedProperty objectiveType = serialized.FindProperty("objectiveType");
            if (objectiveType == null)
            {
                throw new InvalidOperationException("MissionDefinition objectiveType field could not be resolved.");
            }

            objectiveType.intValue = (int)MissionObjectiveType.ReachAndSurvive;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mission);
            return mission;
        }

        private static void ConfigureMissionManager(
            MissionManager manager,
            MissionDefinition mission,
            GameObject playerActor,
            ZoneContext[] observedZones,
            ForestSurvivalController survivalController)
        {
            SerializedObject serialized = new SerializedObject(manager);
            SetObjectReference(serialized, "startingMission", mission);
            SetObjectReference(serialized, "playerActor", playerActor);
            SetObjectReference(serialized, "survivalController", survivalController);

            SerializedProperty startOnPlay = serialized.FindProperty("startOnPlay");
            SerializedProperty zones = serialized.FindProperty("observedZones");
            if (startOnPlay == null || zones == null)
            {
                throw new InvalidOperationException("MissionManager serialized fields could not be resolved.");
            }

            startOnPlay.boolValue = true;
            zones.arraySize = observedZones.Length;
            for (int i = 0; i < observedZones.Length; i++)
            {
                zones.GetArrayElementAtIndex(i).objectReferenceValue = observedZones[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ZoneContext[] FindZoneContexts(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ZoneContext>(true))
                .OrderBy(zone => zone.ZoneId, StringComparer.Ordinal)
                .ToArray();
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

        private static bool Approximately(float actual, float expected)
        {
            return Mathf.Abs(actual - expected) <= 0.001f;
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
