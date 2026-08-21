using System.Linq;
using BeyondTheBeat.Interaction;
using BeyondTheBeat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class PrototypeInteractionBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Phase0_Prototype.unity";
        private const string VehicleName = "PrototypeVehicle";

        [MenuItem("Beyond The Beat/Phase 0/Build Interaction Foundation")]
        private static void BuildInteractionFoundation()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Prototype scene not found at {ScenePath}.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject vehicle = scene.GetRootGameObjects().FirstOrDefault(root => root.name == VehicleName);
            MobileDrivingInput input = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MobileDrivingInput>(true))
                .FirstOrDefault();

            if (vehicle == null)
            {
                Debug.LogError("[Beyond The Beat] PrototypeVehicle is required before building interaction foundation.");
                return;
            }

            if (input == null)
            {
                Debug.LogError("[Beyond The Beat] MobileDrivingInput is required before building interaction foundation.");
                return;
            }

            InteractionController controller = vehicle.GetComponent<InteractionController>();
            if (controller == null)
            {
                controller = vehicle.AddComponent<InteractionController>();
            }

            controller.SetInputSource(input);

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty actorProperty = serializedController.FindProperty("actor");
            if (actorProperty != null)
            {
                actorProperty.objectReferenceValue = vehicle;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = vehicle;
            Debug.Log("[Beyond The Beat] Interaction foundation wired to PrototypeVehicle and MobileDrivingInput.");
        }

        [MenuItem("Beyond The Beat/Phase 0/Validate Interaction Foundation")]
        private static void ValidateInteractionFoundation()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Interaction validation FAIL: scene not found at {ScenePath}.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject vehicle = validationScene.GetRootGameObjects().FirstOrDefault(root => root.name == VehicleName);
                MobileDrivingInput input = validationScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MobileDrivingInput>(true))
                    .FirstOrDefault();

                InteractionController controller = vehicle != null ? vehicle.GetComponent<InteractionController>() : null;
                bool vehiclePass = vehicle != null;
                bool inputPass = input != null;
                bool controllerPass = controller != null;
                bool wiringPass = controllerPass && ValidateControllerReferences(controller, vehicle, input);
                bool foundationTypesPass =
                    typeof(InteractableObject).IsAbstract &&
                    typeof(InteractionTrigger).IsSealed &&
                    typeof(InteractionController).IsSealed;

                bool allPass = vehiclePass && inputPass && controllerPass && wiringPass && foundationTypesPass;

                string message =
                    "[Beyond The Beat] Phase 0 interaction-foundation validation\n" +
                    $"PrototypeVehicle: {PassFail(vehiclePass)}\n" +
                    $"MobileDrivingInput: {PassFail(inputPass)}\n" +
                    $"InteractionController attached: {PassFail(controllerPass)}\n" +
                    $"Input + actor references: {PassFail(wiringPass)}\n" +
                    $"Reusable interaction types available: {PassFail(foundationTypesPass)}";

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

        private static bool ValidateControllerReferences(
            InteractionController controller,
            GameObject expectedActor,
            MobileDrivingInput expectedInput)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty inputProperty = serializedController.FindProperty("inputSource");
            SerializedProperty actorProperty = serializedController.FindProperty("actor");

            return inputProperty != null &&
                   actorProperty != null &&
                   inputProperty.objectReferenceValue == expectedInput &&
                   actorProperty.objectReferenceValue == expectedActor;
        }

        private static string PassFail(bool value)
        {
            return value ? "PASS" : "FAIL";
        }
    }
}
