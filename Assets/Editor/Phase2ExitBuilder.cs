using System;
using System.Linq;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Persistence;
using BeyondTheBeat.Survival;
using BeyondTheBeat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase2ExitBuilder
    {
        private const string ScenePath = Phase2WorldBuilder.Phase2ScenePath;
        private const string PersistenceRootName = "Phase1Persistence";
        private const string SurvivalRootName = "Phase2SurvivalSystem";
        private const string MissionRootName = "Phase1MissionSystem";
        private const string VehicleName = "PrototypeVehicle";

        [MenuItem("Beyond The Beat/Phase 2/Build Exit Integration")]
        public static void BuildExitIntegration()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Phase 2 exit integration requires scene '{ScenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject persistenceRoot = FindRootObject(scene, PersistenceRootName);
            GameObject survivalRoot = FindRootObject(scene, SurvivalRootName);
            Phase1SaveCoordinator coordinator =
                persistenceRoot != null ? persistenceRoot.GetComponent<Phase1SaveCoordinator>() : null;
            ForestSurvivalController survivalController =
                survivalRoot != null ? survivalRoot.GetComponent<ForestSurvivalController>() : null;

            if (coordinator == null || survivalController == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] Phase 2 exit integration requires Phase1Persistence and Phase2SurvivalSystem. " +
                    "Build the preceding Phase 1/2 milestones first.");
                return;
            }

            SerializedObject serialized = new SerializedObject(coordinator);
            SerializedProperty survivalProperty = serialized.FindProperty("survivalController");
            if (survivalProperty == null)
            {
                Debug.LogError("[Beyond The Beat] Phase1SaveCoordinator survivalController field could not be resolved.");
                return;
            }

            survivalProperty.objectReferenceValue = survivalController;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save Phase 2 exit integration into '{ScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Beyond The Beat] Phase 2 exit integration built. Central persistence now owns optional survival/resource resume state.");
        }

        [MenuItem("Beyond The Beat/Phase 2/Validate Exit Integration")]
        public static void ValidateExitIntegration()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Phase 2 exit validation FAIL: scene missing at '{ScenePath}'.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject persistenceRoot = FindRootObject(validationScene, PersistenceRootName);
                GameObject survivalRoot = FindRootObject(validationScene, SurvivalRootName);
                GameObject missionRoot = FindRootObject(validationScene, MissionRootName);
                GameObject vehicle = FindRootObject(validationScene, VehicleName);

                Phase1SaveCoordinator coordinator =
                    persistenceRoot != null ? persistenceRoot.GetComponent<Phase1SaveCoordinator>() : null;
                SaveManager saveManager =
                    persistenceRoot != null ? persistenceRoot.GetComponent<SaveManager>() : null;
                ForestSurvivalController survivalController =
                    survivalRoot != null ? survivalRoot.GetComponent<ForestSurvivalController>() : null;
                MissionManager missionManager =
                    missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                MissionHud hud = validationScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MissionHud>(true))
                    .FirstOrDefault();

                bool wiringPass =
                    coordinator != null &&
                    saveManager != null &&
                    survivalController != null &&
                    missionManager != null &&
                    vehicle != null &&
                    coordinator.SaveManager == saveManager &&
                    coordinator.MissionManager == missionManager &&
                    coordinator.VehicleTransform == vehicle.transform &&
                    coordinator.SurvivalController == survivalController;

                GameSaveData sample = new GameSaveData
                {
                    Version = SaveManager.CurrentVersion,
                    SceneId = validationScene.name,
                    VehicleTransform = SavedTransform.Capture(vehicle != null ? vehicle.transform : null),
                    MissionId = missionManager != null && missionManager.StartingMission != null
                        ? missionManager.StartingMission.MissionId
                        : string.Empty,
                    MissionState = MissionState.Active,
                    HasPhase2SurvivalState = true,
                    MissionTargetContextActive = true,
                    MissionSurvivalElapsedSeconds = 3.25f,
                    SurvivalResourceValue = 64f,
                    SurvivalPressureActive = true,
                    SurvivalRecovering = false
                };

                string json = SaveManager.SerializeForStorage(sample);
                SaveLoadResult roundTripResult = SaveManager.DeserializeForStorage(json, out GameSaveData roundTrip);
                bool roundTripPass =
                    roundTripResult == SaveLoadResult.Success &&
                    roundTrip != null &&
                    roundTrip.HasPhase2SurvivalState &&
                    roundTrip.MissionTargetContextActive &&
                    Approximately(roundTrip.MissionSurvivalElapsedSeconds, 3.25f) &&
                    Approximately(roundTrip.SurvivalResourceValue, 64f) &&
                    roundTrip.SurvivalPressureActive &&
                    !roundTrip.SurvivalRecovering;

                bool backwardCompatibilityPass = false;
                string phase1StyleJson =
                    $"{{\"Version\":{SaveManager.CurrentVersion},\"SceneId\":\"{validationScene.name}\",\"MissionId\":\"legacy\",\"MissionState\":0}}";
                if (SaveManager.DeserializeForStorage(phase1StyleJson, out GameSaveData legacy) == SaveLoadResult.Success)
                {
                    backwardCompatibilityPass = legacy != null && !legacy.HasPhase2SurvivalState;
                }

                bool restorePass = false;
                bool hudPass = false;
                if (missionManager != null &&
                    missionManager.StartingMission != null &&
                    missionManager.StartingMission.ObjectiveType == MissionObjectiveType.ReachAndSurvive &&
                    survivalController != null &&
                    survivalController.Resource != null)
                {
                    survivalController.ResetResource();
                    bool missionRestored = missionManager.RestoreMissionState(
                        missionManager.StartingMission.MissionId,
                        MissionState.Active);
                    bool survivalRestored = survivalController.RestorePersistentState(64f, true, false);
                    bool progressRestored = missionManager.RestoreObjectiveProgress(true, 3.25f);

                    restorePass =
                        missionRestored &&
                        survivalRestored &&
                        progressRestored &&
                        missionManager.State == MissionState.Active &&
                        missionManager.Progress.TargetContextActive &&
                        Approximately(missionManager.Progress.SurvivalElapsedSeconds, 3.25f) &&
                        Approximately(survivalController.Resource.CurrentValue, 64f) &&
                        survivalController.IsPressureActive;

                    if (hud != null)
                    {
                        hud.Refresh();
                        hudPass =
                            hud.MissionManager == missionManager &&
                            hud.StatusText != null &&
                            hud.StatusText.text.IndexOf("RESOURCE 64%", StringComparison.Ordinal) >= 0;
                    }

                    missionManager.ClearMission();
                    survivalController.ResetResource();
                }

                bool allPass = wiringPass && roundTripPass && backwardCompatibilityPass && restorePass && hudPass;
                string message =
                    "[Beyond The Beat] Phase 2 exit integration validation\n" +
                    $"Central persistence/survival wiring: {PassFail(wiringPass)}\n" +
                    $"Phase 2 save JSON round-trip: {PassFail(roundTripPass)}\n" +
                    $"Phase 1 save backward compatibility: {PassFail(backwardCompatibilityPass)}\n" +
                    $"Active Reach + Survive/resource restore: {PassFail(restorePass)}\n" +
                    $"Mission HUD resource status: {PassFail(hudPass)}";

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

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static bool Approximately(float actual, float expected)
        {
            return Mathf.Abs(actual - expected) <= 0.001f;
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
