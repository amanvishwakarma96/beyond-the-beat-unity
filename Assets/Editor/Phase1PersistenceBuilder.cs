using System;
using System.Linq;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase1PersistenceBuilder
    {
        private const string ScenePath = Phase1WorldBuilder.Phase1ScenePath;
        private const string PersistenceRootName = "Phase1Persistence";
        private const string VehicleName = "PrototypeVehicle";
        private const string MissionRootName = "Phase1MissionSystem";

        [MenuItem("Beyond The Beat/Phase 1/Build Local Save Resume")]
        public static void BuildLocalSaveResume()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Persistence build requires Phase 1 scene '{ScenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject vehicle = FindRootObject(scene, VehicleName);
            GameObject missionRoot = FindRootObject(scene, MissionRootName);
            MissionManager missionManager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;

            if (vehicle == null || missionManager == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] Persistence build requires PrototypeVehicle and Phase1MissionSystem/MissionManager. " +
                    "Build the world and Reach Location mission first.");
                return;
            }

            RemoveExistingRoot(scene, PersistenceRootName);

            GameObject persistenceRoot = new GameObject(PersistenceRootName);
            SaveManager saveManager = persistenceRoot.AddComponent<SaveManager>();
            Phase1SaveCoordinator coordinator = persistenceRoot.AddComponent<Phase1SaveCoordinator>();
            ConfigureCoordinator(coordinator, saveManager, missionManager, vehicle.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save persistence setup into '{ScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = persistenceRoot;
            Debug.Log(
                "[Beyond The Beat] Phase 1 local persistence foundation created. " +
                "Vehicle transform and generic mission id/state will load from one versioned local save file.");
        }

        [MenuItem("Beyond The Beat/Phase 1/Validate Local Save Resume")]
        public static void ValidateLocalSaveResume()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Persistence validation FAIL: scene missing at '{ScenePath}'.");
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
                GameObject persistenceRoot = FindRootObject(validationScene, PersistenceRootName);

                MissionManager missionManager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                SaveManager saveManager = persistenceRoot != null ? persistenceRoot.GetComponent<SaveManager>() : null;
                Phase1SaveCoordinator coordinator =
                    persistenceRoot != null ? persistenceRoot.GetComponent<Phase1SaveCoordinator>() : null;

                bool referencePass =
                    vehicle != null &&
                    missionManager != null &&
                    saveManager != null &&
                    coordinator != null &&
                    coordinator.SaveManager == saveManager &&
                    coordinator.MissionManager == missionManager &&
                    coordinator.VehicleTransform == vehicle.transform &&
                    coordinator.LoadOnStart &&
                    coordinator.SaveOnApplicationPause &&
                    coordinator.SaveOnApplicationQuit;

                bool versionPass =
                    SaveManager.CurrentVersion == 1 &&
                    saveManager != null &&
                    string.Equals(saveManager.SaveFileName, "beyond-the-beat-phase1.json", StringComparison.Ordinal);

                GameSaveData sample = new GameSaveData
                {
                    Version = SaveManager.CurrentVersion,
                    SceneId = validationScene.name,
                    VehicleTransform = new SavedTransform(
                        new Vector3(12.5f, 1.25f, -9.75f),
                        Quaternion.Euler(0f, 37f, 0f)),
                    MissionId = missionManager?.StartingMission != null
                        ? missionManager.StartingMission.MissionId
                        : string.Empty,
                    MissionState = MissionState.Active
                };

                string json = SaveManager.SerializeForStorage(sample);
                SaveLoadResult roundTripResult = SaveManager.DeserializeForStorage(json, out GameSaveData roundTrip);
                bool roundTripPass =
                    roundTripResult == SaveLoadResult.Success &&
                    roundTrip != null &&
                    roundTrip.Version == SaveManager.CurrentVersion &&
                    roundTrip.SceneId == validationScene.name &&
                    roundTrip.MissionId == sample.MissionId &&
                    roundTrip.MissionState == MissionState.Active &&
                    Approximately(roundTrip.VehicleTransform.Position.ToVector3(), new Vector3(12.5f, 1.25f, -9.75f));

                GameSaveData incompatible = new GameSaveData
                {
                    Version = SaveManager.CurrentVersion + 1,
                    SceneId = validationScene.name
                };

                bool fallbackPass =
                    SaveManager.DeserializeForStorage(string.Empty, out _) == SaveLoadResult.Corrupt &&
                    SaveManager.DeserializeForStorage(
                        SaveManager.SerializeForStorage(incompatible), out _) == SaveLoadResult.Incompatible;

                bool missionRestorePass = false;
                if (missionManager?.StartingMission != null)
                {
                    string missionId = missionManager.StartingMission.MissionId;
                    bool activeRestored = missionManager.RestoreMissionState(missionId, MissionState.Active);
                    bool activePass = activeRestored &&
                                      missionManager.CurrentMissionId == missionId &&
                                      missionManager.State == MissionState.Active &&
                                      missionManager.HasActiveMission;

                    bool completedRestored = missionManager.RestoreMissionState(missionId, MissionState.Completed);
                    bool completedPass = completedRestored &&
                                         missionManager.CurrentMissionId == missionId &&
                                         missionManager.State == MissionState.Completed &&
                                         !missionManager.HasActiveMission;

                    missionManager.ClearMission();
                    missionRestorePass = activePass && completedPass && missionManager.State == MissionState.Inactive;
                }

                bool allPass = referencePass && versionPass && roundTripPass && fallbackPass && missionRestorePass;

                string message =
                    "[Beyond The Beat] Phase 1 local save/resume validation\n" +
                    $"Central SaveManager/coordinator references: {PassFail(referencePass)}\n" +
                    $"Versioned local save identity: {PassFail(versionPass)}\n" +
                    $"Vehicle + mission JSON round-trip: {PassFail(roundTripPass)}\n" +
                    $"Corrupt/incompatible fallback classification: {PassFail(fallbackPass)}\n" +
                    $"Active/completed mission restore: {PassFail(missionRestorePass)}";

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

        private static void ConfigureCoordinator(
            Phase1SaveCoordinator coordinator,
            SaveManager saveManager,
            MissionManager missionManager,
            Transform vehicleTransform)
        {
            SerializedObject serialized = new SerializedObject(coordinator);
            SetObjectReference(serialized, "saveManager", saveManager);
            SetObjectReference(serialized, "missionManager", missionManager);
            SetObjectReference(serialized, "vehicleTransform", vehicleTransform);
            SetBool(serialized, "loadOnStart", true);
            SetBool(serialized, "saveOnApplicationPause", true);
            SetBool(serialized, "saveOnApplicationQuit", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static void SetBool(SerializedObject target, string propertyName, bool value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized bool field '{propertyName}' could not be resolved.");
            }

            property.boolValue = value;
        }

        private static void RemoveExistingRoot(Scene scene, string name)
        {
            GameObject existing = FindRootObject(scene, name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static bool Approximately(Vector3 actual, Vector3 expected)
        {
            const float tolerance = 0.001f;
            return Mathf.Abs(actual.x - expected.x) <= tolerance &&
                   Mathf.Abs(actual.y - expected.y) <= tolerance &&
                   Mathf.Abs(actual.z - expected.z) <= tolerance;
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
