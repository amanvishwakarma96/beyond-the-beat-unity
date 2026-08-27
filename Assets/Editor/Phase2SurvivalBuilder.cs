using System;
using System.Linq;
using BeyondTheBeat.Survival;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase2SurvivalBuilder
    {
        private const string ScenePath = Phase2WorldBuilder.Phase2ScenePath;
        private const string SurvivalRootName = "Phase2SurvivalSystem";
        private const string VehicleName = "PrototypeVehicle";
        private const string ForestZoneId = "forest";
        private const string OffRoadZoneId = "off-road";

        private const float MaxResource = 100f;
        private const float StartingResource = 100f;
        private const float DrainPerSecond = 4f;
        private const float RecoveryPerSecond = 12f;

        [MenuItem("Beyond The Beat/Phase 2/Build Forest Survival Resource")]
        public static void BuildForestSurvivalResource()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Forest survival build requires Phase 2 scene '{ScenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject vehicle = FindRootObject(scene, VehicleName);
            ZoneContext forestZone = FindZone(scene, ForestZoneId);

            if (vehicle == null || forestZone == null || forestZone.ZoneType != WorldZoneType.Forest)
            {
                Debug.LogError(
                    "[Beyond The Beat] Forest survival build requires PrototypeVehicle and the Phase 2 'forest' ZoneContext.");
                return;
            }

            RemoveExistingRoot(scene, SurvivalRootName);

            GameObject survivalRoot = new GameObject(SurvivalRootName);
            SurvivalResource resource = survivalRoot.AddComponent<SurvivalResource>();
            ForestSurvivalController controller = survivalRoot.AddComponent<ForestSurvivalController>();

            ConfigureResource(resource);
            ConfigureController(controller, forestZone, vehicle, resource);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save forest survival setup into '{ScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = survivalRoot;

            Debug.Log(
                "[Beyond The Beat] Phase 2 forest survival resource created. " +
                "Environmental pressure is activated only by the configured Forest ZoneContext and recovers after exit.");
        }

        [MenuItem("Beyond The Beat/Phase 2/Validate Forest Survival Resource")]
        public static void ValidateForestSurvivalResource()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Forest survival validation FAIL: scene missing at '{ScenePath}'.");
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
                GameObject survivalRoot = FindRootObject(validationScene, SurvivalRootName);
                ZoneContext forestZone = FindZone(validationScene, ForestZoneId);
                ZoneContext offRoadZone = FindZone(validationScene, OffRoadZoneId);
                SurvivalResource resource = survivalRoot != null ? survivalRoot.GetComponent<SurvivalResource>() : null;
                ForestSurvivalController controller =
                    survivalRoot != null ? survivalRoot.GetComponent<ForestSurvivalController>() : null;

                bool referencesPass =
                    vehicle != null &&
                    forestZone != null &&
                    offRoadZone != null &&
                    resource != null &&
                    controller != null &&
                    controller.ForestZone == forestZone &&
                    controller.PlayerActor == vehicle &&
                    controller.Resource == resource;

                bool configurationPass =
                    referencesPass &&
                    forestZone.ZoneType == WorldZoneType.Forest &&
                    Approximately(resource.MaxValue, MaxResource) &&
                    Approximately(resource.StartingValue, StartingResource) &&
                    Approximately(controller.DrainPerSecond, DrainPerSecond) &&
                    Approximately(controller.RecoveryPerSecond, RecoveryPerSecond) &&
                    controller.ExitMode == SurvivalExitMode.RecoverOverTime;

                bool wrongZonePass = false;
                bool wrongActorPass = false;
                bool forestDrainPass = false;
                bool exitRecoveryPass = false;
                bool disableStopsPass = false;
                bool resourceEventsPass = false;

                if (referencesPass)
                {
                    controller.ResetResource();
                    float initialValue = resource.CurrentValue;

                    wrongZonePass =
                        !controller.TryEnterContext(offRoadZone, vehicle) &&
                        !controller.IsPressureActive &&
                        Approximately(resource.CurrentValue, initialValue);

                    wrongActorPass =
                        !controller.TryEnterContext(forestZone, survivalRoot) &&
                        !controller.IsPressureActive;

                    bool enteredForest = controller.TryEnterContext(forestZone, vehicle) &&
                                         controller.IsPressureActive &&
                                         !controller.IsRecovering;
                    controller.Tick(2f);
                    float expectedAfterDrain = initialValue - (DrainPerSecond * 2f);
                    forestDrainPass = enteredForest && Approximately(resource.CurrentValue, expectedAfterDrain);

                    bool exitedForest = controller.TryExitContext(forestZone, vehicle) &&
                                        !controller.IsPressureActive &&
                                        controller.IsRecovering;
                    float valueAtExit = resource.CurrentValue;
                    controller.Tick(1f);
                    bool recoveredWithoutFurtherDrain =
                        resource.CurrentValue > valueAtExit &&
                        resource.CurrentValue <= resource.MaxValue;
                    controller.Tick(100f);
                    exitRecoveryPass =
                        exitedForest &&
                        recoveredWithoutFurtherDrain &&
                        Approximately(resource.CurrentValue, resource.MaxValue) &&
                        !controller.IsRecovering;

                    controller.ResetResource();
                    controller.TryEnterContext(forestZone, vehicle);
                    controller.Tick(1f);
                    float valueBeforeDisable = resource.CurrentValue;
                    controller.enabled = false;
                    controller.Tick(10f);
                    disableStopsPass =
                        Approximately(resource.CurrentValue, valueBeforeDisable) &&
                        !controller.IsPressureActive &&
                        !controller.IsRecovering;
                    controller.enabled = true;
                    controller.ResetResource();

                    int valueChangedCount = 0;
                    int depletedCount = 0;
                    int recoveredCount = 0;
                    Action<float, float> valueChanged = (_, _) => valueChangedCount++;
                    Action depleted = () => depletedCount++;
                    Action recovered = () => recoveredCount++;

                    resource.ValueChanged += valueChanged;
                    resource.Depleted += depleted;
                    resource.Recovered += recovered;
                    resource.Drain(resource.MaxValue * 2f);
                    resource.Recover(1f);
                    resource.ValueChanged -= valueChanged;
                    resource.Depleted -= depleted;
                    resource.Recovered -= recovered;

                    resourceEventsPass =
                        valueChangedCount == 2 &&
                        depletedCount == 1 &&
                        recoveredCount == 1;

                    controller.ResetResource();
                }

                bool allPass =
                    referencesPass &&
                    configurationPass &&
                    wrongZonePass &&
                    wrongActorPass &&
                    forestDrainPass &&
                    exitRecoveryPass &&
                    disableStopsPass &&
                    resourceEventsPass;

                string message =
                    "[Beyond The Beat] Phase 2 forest survival validation\n" +
                    $"Forest/resource/controller references: {PassFail(referencesPass)}\n" +
                    $"Configured resource/drain/recovery contract: {PassFail(configurationPass)}\n" +
                    $"Non-forest zone cannot activate pressure: {PassFail(wrongZonePass)}\n" +
                    $"Non-player actor cannot activate pressure: {PassFail(wrongActorPass)}\n" +
                    $"Forest entry drains deterministically: {PassFail(forestDrainPass)}\n" +
                    $"Forest exit stops drain and recovers/clamps: {PassFail(exitRecoveryPass)}\n" +
                    $"Disabled controller cannot continue draining: {PassFail(disableStopsPass)}\n" +
                    $"Value/depleted/recovered events: {PassFail(resourceEventsPass)}";

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

        private static void ConfigureResource(SurvivalResource resource)
        {
            SerializedObject serialized = new SerializedObject(resource);
            SetFloat(serialized, "maxValue", MaxResource);
            SetFloat(serialized, "startingValue", StartingResource);
            SetFloat(serialized, "currentValue", StartingResource);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureController(
            ForestSurvivalController controller,
            ZoneContext forestZone,
            GameObject playerActor,
            SurvivalResource resource)
        {
            SerializedObject serialized = new SerializedObject(controller);
            SetObjectReference(serialized, "forestZone", forestZone);
            SetObjectReference(serialized, "playerActor", playerActor);
            SetObjectReference(serialized, "resource", resource);
            SetFloat(serialized, "drainPerSecond", DrainPerSecond);
            SetFloat(serialized, "recoveryPerSecond", RecoveryPerSecond);

            SerializedProperty exitMode = serialized.FindProperty("exitMode");
            if (exitMode == null)
            {
                throw new InvalidOperationException("ForestSurvivalController field 'exitMode' could not be resolved.");
            }

            exitMode.intValue = (int)SurvivalExitMode.RecoverOverTime;
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

        private static void SetFloat(SerializedObject target, string propertyName, float value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized float field '{propertyName}' could not be resolved.");
            }

            property.floatValue = value;
        }

        private static ZoneContext FindZone(Scene scene, string zoneId)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ZoneContext>(true))
                .FirstOrDefault(zone => string.Equals(zone.ZoneId, zoneId, StringComparison.Ordinal));
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static void RemoveExistingRoot(Scene scene, string name)
        {
            GameObject existing = FindRootObject(scene, name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static bool Approximately(float actual, float expected)
        {
            return Mathf.Abs(actual - expected) <= 0.001f;
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
