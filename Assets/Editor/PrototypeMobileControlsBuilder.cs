using System;
using System.Linq;
using BeyondTheBeat.UI;
using BeyondTheBeat.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class PrototypeMobileControlsBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Phase0_Prototype.unity";
        private const string CanvasName = "MobileDrivingCanvas";
        private const string EventSystemName = "EventSystem";
        private const string VehicleName = "PrototypeVehicle";

        [MenuItem("Beyond The Beat/Phase 0/Build Mobile Driving Controls")]
        private static void BuildMobileDrivingControls()
        {
            if (Application.isBatchMode)
            {
                if (!EditorSceneManager.SaveOpenScenes())
                {
                    Debug.LogError("[Beyond The Beat] Failed to save open scenes before building mobile controls in batch mode.");
                    return;
                }
            }
            else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
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
            if (vehicle == null || !vehicle.TryGetComponent(out VehicleController controller))
            {
                Debug.LogError("[Beyond The Beat] PrototypeVehicle with VehicleController is required before building mobile controls.");
                return;
            }

            RemoveExistingRoot(scene, CanvasName);
            EnsureEventSystem(scene);

            GameObject canvasObject = new GameObject(CanvasName);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            MobileDrivingInput input = canvasObject.AddComponent<MobileDrivingInput>();

            TouchHoldButton left = CreateControlButton(canvasObject.transform, "SteerLeft", "◀", new Vector2(160f, 155f), new Vector2(175f, 175f), Anchor.BottomLeft);
            TouchHoldButton right = CreateControlButton(canvasObject.transform, "SteerRight", "▶", new Vector2(360f, 155f), new Vector2(175f, 175f), Anchor.BottomLeft);
            TouchHoldButton reverse = CreateControlButton(canvasObject.transform, "BrakeReverse", "BRAKE\nREV", new Vector2(-365f, 155f), new Vector2(190f, 175f), Anchor.BottomRight);
            TouchHoldButton accelerate = CreateControlButton(canvasObject.transform, "Accelerate", "GAS", new Vector2(-155f, 155f), new Vector2(190f, 175f), Anchor.BottomRight);
            TouchHoldButton interact = CreateControlButton(canvasObject.transform, "Interact", "ACTION", new Vector2(-155f, 380f), new Vector2(190f, 135f), Anchor.BottomRight);

            SerializedObject serializedInput = new SerializedObject(input);
            SetObjectReference(serializedInput, "vehicleController", controller);
            SetObjectReference(serializedInput, "steerLeftButton", left);
            SetObjectReference(serializedInput, "steerRightButton", right);
            SetObjectReference(serializedInput, "accelerateButton", accelerate);
            SetObjectReference(serializedInput, "brakeReverseButton", reverse);
            SetObjectReference(serializedInput, "interactButton", interact);
            serializedInput.ApplyModifiedPropertiesWithoutUndo();

            VehicleDebugInput debugInput = vehicle.GetComponent<VehicleDebugInput>();
            if (debugInput != null)
            {
                debugInput.enabled = false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = canvasObject;
            Debug.Log("[Beyond The Beat] Mobile driving controls created. Use touch in a device build or W/A/S/D + Space + E in the Editor.");
        }

        [MenuItem("Beyond The Beat/Phase 0/Validate Mobile Driving Controls")]
        private static void ValidateMobileDrivingControls()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Mobile-controls validation FAIL: scene not found at {ScenePath}.");
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
                GameObject canvasObject = FindRootObject(validationScene, CanvasName);
                EventSystem eventSystem = validationScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
                    .FirstOrDefault();
                MobileDrivingInput input = null;

                bool vehiclePass = vehicle != null && vehicle.TryGetComponent<VehicleController>(out _);
                bool canvasPass = canvasObject != null &&
                                  canvasObject.TryGetComponent<Canvas>(out Canvas canvas) &&
                                  canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                                  canvasObject.TryGetComponent<CanvasScaler>(out _) &&
                                  canvasObject.TryGetComponent<GraphicRaycaster>(out _);

                bool inputPass = canvasPass && canvasObject.TryGetComponent(out input);
                bool controlsPass = inputPass && input != null && ValidateInputReferences(input);
                bool fiveButtonsPass = canvasPass && canvasObject.GetComponentsInChildren<TouchHoldButton>(true).Length == 5;
                bool eventSystemPass = eventSystem != null && eventSystem.GetComponent<InputSystemUIInputModule>() != null;

                bool debugAdapterPass = vehiclePass &&
                                        (!vehicle.TryGetComponent(out VehicleDebugInput debugInput) || !debugInput.enabled);

                bool allPass = vehiclePass && canvasPass && inputPass && controlsPass && fiveButtonsPass && eventSystemPass && debugAdapterPass;

                string message =
                    "[Beyond The Beat] Phase 0 mobile-controls validation\n" +
                    $"VehicleController available: {PassFail(vehiclePass)}\n" +
                    $"Landscape HUD canvas: {PassFail(canvasPass)}\n" +
                    $"MobileDrivingInput attached: {PassFail(inputPass)}\n" +
                    $"All input references assigned: {PassFail(controlsPass)}\n" +
                    $"Five touch controls present: {PassFail(fiveButtonsPass)}\n" +
                    $"Input System UI module: {PassFail(eventSystemPass)}\n" +
                    $"Legacy debug adapter disabled: {PassFail(debugAdapterPass)}";

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

        private static TouchHoldButton CreateControlButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            Anchor anchor)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TouchHoldButton));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            Vector2 anchorPoint = anchor == Anchor.BottomLeft ? Vector2.zero : new Vector2(1f, 0f);
            rect.anchorMin = anchorPoint;
            rect.anchorMax = anchorPoint;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.10f, 0.14f, 0.72f);
            image.raycastTarget = true;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text text = labelObject.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 30;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 36;
            text.color = Color.white;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return buttonObject.GetComponent<TouchHoldButton>();
        }

        private static void EnsureEventSystem(Scene scene)
        {
            EventSystem eventSystem = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
                .FirstOrDefault();

            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject(EventSystemName);
                eventObject.AddComponent<EventSystem>();
                eventObject.AddComponent<InputSystemUIInputModule>();
                return;
            }

            BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
            foreach (BaseInputModule module in modules)
            {
                if (!(module is InputSystemUIInputModule))
                {
                    UnityEngine.Object.DestroyImmediate(module);
                }
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static bool ValidateInputReferences(MobileDrivingInput input)
        {
            SerializedObject serializedInput = new SerializedObject(input);
            string[] names =
            {
                "vehicleController",
                "steerLeftButton",
                "steerRightButton",
                "accelerateButton",
                "brakeReverseButton",
                "interactButton"
            };

            return names.All(name =>
            {
                SerializedProperty property = serializedInput.FindProperty(name);
                return property != null && property.objectReferenceValue != null;
            });
        }

        private static void SetObjectReference(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"MobileDrivingInput property '{propertyName}' was not found.");
            }

            property.objectReferenceValue = value;
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

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";

        private enum Anchor
        {
            BottomLeft,
            BottomRight
        }
    }
}
