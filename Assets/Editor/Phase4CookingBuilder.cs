using System;
using BeyondTheBeat.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase4CookingBuilder
    {
        public const string Phase4ScenePath = "Assets/Scenes/Phase4/Phase4_FreeRoam.unity";

        private const string RootName = "Phase4FreeRoamActivities";
        private const string StationName = "CookingStation";
        private const string ParkingRootName = "ParkingPrototype";
        private const string MissionRootName = "Phase1MissionSystem";

        [MenuItem("Beyond The Beat/Phase 4/Build Cooking Interaction")]
        public static void BuildCookingInteraction()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset phase3Scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase3RestrictedAreaBuilder.Phase3ScenePath);
            if (phase3Scene == null)
            {
                throw new InvalidOperationException(
                    $"Phase 4 cooking build requires the integrated Phase 3 scene at '{Phase3RestrictedAreaBuilder.Phase3ScenePath}'.");
            }

            EnsureFolder("Assets/Scenes", "Phase4");

            Scene sourceScene = EditorSceneManager.OpenScene(Phase3RestrictedAreaBuilder.Phase3ScenePath, OpenSceneMode.Single);
            if (sourceScene.isDirty && !EditorSceneManager.SaveScene(sourceScene))
            {
                throw new InvalidOperationException("Unable to save the Phase 3 source scene before creating Phase 4.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase4ScenePath) != null &&
                !AssetDatabase.DeleteAsset(Phase4ScenePath))
            {
                throw new InvalidOperationException($"Unable to replace the existing Phase 4 scene at '{Phase4ScenePath}'.");
            }

            if (!AssetDatabase.CopyAsset(Phase3RestrictedAreaBuilder.Phase3ScenePath, Phase4ScenePath))
            {
                throw new InvalidOperationException($"Unable to copy Phase 3 scene to '{Phase4ScenePath}'.");
            }

            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.OpenScene(Phase4ScenePath, OpenSceneMode.Single);
            RemoveExistingRoot(scene, RootName);

            GameObject root = new GameObject(RootName);
            CookingStation station = CreateCookingStation(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save generated Phase 4 scene at '{Phase4ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Phase4ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = station.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Phase 4 cooking interaction created. Cooking reuses the existing shared interaction trigger/controller path and adds a reusable timed-activity layer without changing parking.");
        }

        [MenuItem("Beyond The Beat/Phase 4/Validate Cooking Interaction")]
        public static void ValidateCookingInteraction()
        {
            if (!ValidateCookingInteractionInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateCookingInteractionOrThrow()
        {
            if (ValidateCookingInteractionInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateCookingInteractionInternal(out string message)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase4ScenePath);
            if (sceneAsset == null)
            {
                message = $"[Beyond The Beat] Phase 4 cooking validation FAIL: scene not found at '{Phase4ScenePath}'.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != Phase4ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(Phase4ScenePath, OpenSceneMode.Additive)
                : originalScene;

            GameObject validationActor = null;
            try
            {
                GameObject root = FindRootObject(validationScene, RootName);
                Transform stationTransform = root != null ? root.transform.Find(StationName) : null;
                CookingStation cooking = stationTransform != null ? stationTransform.GetComponent<CookingStation>() : null;
                InteractionTrigger trigger = stationTransform != null ? stationTransform.GetComponent<InteractionTrigger>() : null;
                BoxCollider triggerCollider = stationTransform != null ? stationTransform.GetComponent<BoxCollider>() : null;

                bool structurePass = root != null &&
                                     cooking != null &&
                                     trigger != null &&
                                     triggerCollider != null &&
                                     triggerCollider.isTrigger &&
                                     cooking is TimedActivityInteractable &&
                                     cooking is InteractableObject &&
                                     string.Equals(cooking.PromptText, "COOK", StringComparison.Ordinal) &&
                                     string.Equals(cooking.RecipeId, "camp-meal", StringComparison.Ordinal);

                bool triggerWiringPass = false;
                if (trigger != null && cooking != null)
                {
                    SerializedObject serializedTrigger = new SerializedObject(trigger);
                    SerializedProperty interactableProperty = serializedTrigger.FindProperty("interactable");
                    triggerWiringPass = interactableProperty != null && interactableProperty.objectReferenceValue == cooking;
                }

                bool inheritedWorldPass = FindRootObject(validationScene, ParkingRootName) != null &&
                                          FindRootObject(validationScene, MissionRootName) != null &&
                                          FindRootObject(validationScene, "Phase3RestrictedArea") != null;

                bool behaviorPass = false;
                string behaviorDetail = "not-run";
                if (cooking != null)
                {
                    validationActor = new GameObject("Phase4CookingValidationActor");
                    cooking.ResetCompletion();

                    int initialMeals = cooking.MealsPrepared;
                    bool started = cooking.RequestInteraction(validationActor);
                    cooking.AdvanceActivity(cooking.DurationSeconds * 0.4f);
                    bool progressed = cooking.IsInteracting && cooking.Progress01 > 0f && cooking.Progress01 < 1f;
                    bool cancelled = cooking.CancelInteraction(validationActor) &&
                                     !cooking.IsInteracting &&
                                     !cooking.HasCompleted &&
                                     Mathf.Approximately(cooking.Progress01, 0f) &&
                                     cooking.MealsPrepared == initialMeals;

                    bool restarted = cooking.RequestInteraction(validationActor);
                    bool completed = cooking.AdvanceActivity(cooking.DurationSeconds + 0.1f) &&
                                     !cooking.IsInteracting &&
                                     cooking.HasCompleted &&
                                     Mathf.Approximately(cooking.Progress01, 1f) &&
                                     cooking.MealsPrepared == initialMeals + 1;

                    int afterFirstCompletion = cooking.MealsPrepared;
                    cooking.AdvanceActivity(cooking.DurationSeconds + 1f);
                    bool completesOnce = cooking.MealsPrepared == afterFirstCompletion;

                    bool repeatStarted = cooking.RequestInteraction(validationActor);
                    bool repeatCompleted = cooking.AdvanceActivity(cooking.DurationSeconds + 0.1f) &&
                                           cooking.MealsPrepared == initialMeals + 2;

                    behaviorPass = started && progressed && cancelled && restarted && completed &&
                                   completesOnce && repeatStarted && repeatCompleted;
                    behaviorDetail =
                        $"started={started}, progressed={progressed}, cancelled={cancelled}, restarted={restarted}, " +
                        $"completed={completed}, completesOnce={completesOnce}, repeatStarted={repeatStarted}, repeatCompleted={repeatCompleted}";
                }

                bool pass = structurePass && triggerWiringPass && inheritedWorldPass && behaviorPass;
                message = pass
                    ? "[Beyond The Beat] Phase 4 cooking validation PASS: shared timed activity, Cook completion/cancel/repeat behavior, trigger wiring, and inherited parking/mission/restricted-area regressions are intact. Physical Android ACTION/device validation remains required."
                    : "[Beyond The Beat] Phase 4 cooking validation FAIL: " +
                      $"structure={structurePass}, triggerWiring={triggerWiringPass}, inheritedWorld={inheritedWorldPass}, behavior={behaviorPass} ({behaviorDetail}).";
                return pass;
            }
            finally
            {
                if (validationActor != null)
                {
                    UnityEngine.Object.DestroyImmediate(validationActor);
                }

                if (openedForValidation && validationScene.IsValid() && validationScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(validationScene, true);
                }
            }
        }

        private static CookingStation CreateCookingStation(Transform parent)
        {
            GameObject stationObject = new GameObject(StationName);
            stationObject.transform.SetParent(parent, false);
            stationObject.transform.position = new Vector3(42f, 0.75f, -16f);

            BoxCollider triggerCollider = stationObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(7f, 3f, 7f);

            CookingStation cooking = stationObject.AddComponent<CookingStation>();
            InteractionTrigger trigger = stationObject.GetComponent<InteractionTrigger>() ?? stationObject.AddComponent<InteractionTrigger>();

            SerializedObject serializedCooking = new SerializedObject(cooking);
            SetString(serializedCooking, "promptText", "COOK");
            SetBool(serializedCooking, "allowRepeatInteraction", true);
            SetFloat(serializedCooking, "durationSeconds", 2.5f);
            SetBool(serializedCooking, "resetProgressOnCancel", true);
            SetString(serializedCooking, "recipeId", "camp-meal");
            SetString(serializedCooking, "activityLabel", "Cook meal");
            serializedCooking.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedTrigger = new SerializedObject(trigger);
            SerializedProperty interactableProperty = serializedTrigger.FindProperty("interactable");
            if (interactableProperty == null)
            {
                throw new InvalidOperationException("Unable to configure CookingStation InteractionTrigger.interactable reference.");
            }

            interactableProperty.objectReferenceValue = cooking;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();

            CreateVisualPrimitive("Counter", PrimitiveType.Cube, stationObject.transform, new Vector3(0f, -0.25f, 0f), new Vector3(3.6f, 1f, 2.2f));
            CreateVisualPrimitive("CookTop", PrimitiveType.Cube, stationObject.transform, new Vector3(0f, 0.38f, 0f), new Vector3(2.2f, 0.18f, 1.5f));
            CreateVisualPrimitive("Pot", PrimitiveType.Cylinder, stationObject.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.75f, 0.25f, 0.75f));

            return cooking;
        }

        private static void CreateVisualPrimitive(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            GameObject visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, name, StringComparison.Ordinal))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static void RemoveExistingRoot(Scene scene, string name)
        {
            GameObject existing = FindRootObject(scene, name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to find serialized string property '{propertyName}'.");
            }

            property.stringValue = value;
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to find serialized bool property '{propertyName}'.");
            }

            property.boolValue = value;
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to find serialized float property '{propertyName}'.");
            }

            property.floatValue = value;
        }
    }
}
