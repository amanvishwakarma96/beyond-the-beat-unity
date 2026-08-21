using System.Linq;
using BeyondTheBeat.CameraSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class PrototypeCameraBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Phase0_Prototype.unity";
        private const string VehicleName = "PrototypeVehicle";
        private const string CameraName = "GameplayCamera";
        private const string ReferenceCameraName = "PrototypeReferenceCamera";

        [MenuItem("Beyond The Beat/Phase 0/Build Smooth Vehicle Camera")]
        private static void BuildSmoothVehicleCamera()
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
            GameObject vehicle = FindRootObject(scene, VehicleName);
            if (vehicle == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] PrototypeVehicle is missing. " +
                    "Run 'Beyond The Beat > Phase 0 > Build Prototype Vehicle' first.");
                return;
            }

            RemoveExistingPrototypeCameras(scene);

            GameObject cameraObject = new GameObject(CameraName);
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;

            AudioListener listener = cameraObject.AddComponent<AudioListener>();
            listener.enabled = true;

            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
            follow.SetTarget(vehicle.transform, true);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = cameraObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Smooth gameplay camera created and assigned to PrototypeVehicle. " +
                "Enter Play Mode and validate normal driving, steering, braking and reverse visibility.");
        }

        [MenuItem("Beyond The Beat/Phase 0/Validate Smooth Vehicle Camera")]
        private static void ValidateSmoothVehicleCamera()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Camera validation FAIL: scene not found at {ScenePath}.");
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
                GameObject cameraObject = FindRootObject(validationScene, CameraName);
                CameraFollow follow = null;

                bool vehiclePass = vehicle != null;
                bool cameraPass = cameraObject != null && cameraObject.TryGetComponent<Camera>(out _);
                bool followPass = cameraObject != null && cameraObject.TryGetComponent(out follow);
                bool targetPass = followPass && follow != null && follow.Target == vehicle?.transform;
                bool mainCameraPass = cameraObject != null && cameraObject.CompareTag("MainCamera");
                bool audioListenerPass =
                    cameraObject != null &&
                    cameraObject.TryGetComponent<AudioListener>(out AudioListener listener) &&
                    listener.enabled;
                bool referenceCameraRemoved = FindRootObject(validationScene, ReferenceCameraName) == null;
                bool singleEnabledCameraPass = validationScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                    .Count(sceneCamera => sceneCamera.enabled) == 1;

                bool allPass =
                    vehiclePass &&
                    cameraPass &&
                    followPass &&
                    targetPass &&
                    mainCameraPass &&
                    audioListenerPass &&
                    referenceCameraRemoved &&
                    singleEnabledCameraPass;

                string message =
                    "[Beyond The Beat] Phase 0 smooth camera validation\n" +
                    $"PrototypeVehicle present: {PassFail(vehiclePass)}\n" +
                    $"GameplayCamera present: {PassFail(cameraPass)}\n" +
                    $"CameraFollow attached: {PassFail(followPass)}\n" +
                    $"Camera target assigned: {PassFail(targetPass)}\n" +
                    $"MainCamera tag: {PassFail(mainCameraPass)}\n" +
                    $"AudioListener present: {PassFail(audioListenerPass)}\n" +
                    $"Reference camera removed: {PassFail(referenceCameraRemoved)}\n" +
                    $"Exactly one enabled camera: {PassFail(singleEnabledCameraPass)}";

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

        private static void RemoveExistingPrototypeCameras(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == CameraName || root.name == ReferenceCameraName)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
