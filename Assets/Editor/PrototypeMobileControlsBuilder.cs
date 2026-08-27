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
        private const string ControlsRootName = "DrivingControls";

        [MenuItem("Beyond The Beat/Phase 0/Build Mobile Driving Controls")]
        private static void BuildMobileDrivingControls()
        {
            if (Application.isBatchMode)
            {
                AssetDatabase.SaveAssets();
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

            GameObject controlsRoot = new GameObject(ControlsRootName, typeof(RectTransform));
            controlsRoot.transform.SetParent(canvasObject.transform, false);
            StretchFullScreen(controlsRoot.GetComponent<RectTransform>());

            CreateControlDock(
                controlsRoot.transform,
                "SteeringDock",
                new Vector2(0f, 0f),
                new Vector2(270f, 150f),
                new Vector2(500f, 250f));

            CreateControlDock(
                controlsRoot.transform,
                "PedalDock",
                new Vector2(1f, 0f),
                new Vector2(-280f, 150f),
                new Vector2(520f, 250f));

            TouchHoldButton left = CreateCircleControl(
                controlsRoot.transform,
                "SteerLeft",
                "‹",
                new Vector2(155f, 145f),
                178f,
                Anchor.BottomLeft,
                new Color(0.10f, 0.18f, 0.26f, 0.94f),
                new Color(0.14f, 0.46f, 0.56f, 1f));

            TouchHoldButton right = CreateCircleControl(
                controlsRoot.transform,
                "SteerRight",
                "›",
                new Vector2(365f, 145f),
                178f,
                Anchor.BottomLeft,
                new Color(0.10f, 0.18f, 0.26f, 0.94f),
                new Color(0.14f, 0.46f, 0.56f, 1f));

            TouchHoldButton reverse = CreateCircleControl(
                controlsRoot.transform,
                "BrakeReverse",
                "BRAKE\nREV",
                new Vector2(-365f, 145f),
                182f,
                Anchor.BottomRight,
                new Color(0.28f, 0.08f, 0.08f, 0.96f),
                new Color(0.70f, 0.13f, 0.12f, 1f));

            TouchHoldButton accelerate = CreateCircleControl(
                controlsRoot.transform,
                "Accelerate",
                "GO",
                new Vector2(-155f, 145f),
                198f,
                Anchor.BottomRight,
                new Color(0.05f, 0.25f, 0.29f, 0.96f),
                new Color(0.08f, 0.62f, 0.68f, 1f));

            TouchHoldButton interact = CreatePillControl(
                controlsRoot.transform,
                "Interact",
                "ACTION",
                new Vector2(-190f, 350f),
                new Vector2(220f, 92f),
                Anchor.BottomRight,
                new Color(0.30f, 0.19f, 0.04f, 0.94f),
                new Color(0.88f, 0.48f, 0.08f, 1f));

            SerializedObject serializedInput = new SerializedObject(input);
            SetObjectReference(serializedInput, "vehicleController", controller);
            SetObjectReference(serializedInput, "steerLeftButton", left);
            SetObjectReference(serializedInput, "steerRightButton", right);
            SetObjectReference(serializedInput, "accelerateButton", accelerate);
            SetObjectReference(serializedInput, "brakeReverseButton", reverse);
            SetObjectReference(serializedInput, "interactButton", interact);

            SerializedProperty directFallback = serializedInput.FindProperty("enableDirectTouchFallback");
            if (directFallback == null)
            {
                throw new InvalidOperationException("MobileDrivingInput direct-touch fallback field was not found.");
            }
            directFallback.boolValue = true;
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

            Selection.activeGameObject = controlsRoot;
            Debug.Log(
                "[Beyond The Beat] Mobile driving controls created with direct Input System multi-touch fallback, " +
                "authored circular/pill presentation, and visible press feedback.");
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
                TouchHoldButton[] buttons = canvasPass
                    ? canvasObject.GetComponentsInChildren<TouchHoldButton>(true)
                    : Array.Empty<TouchHoldButton>();
                bool fiveButtonsPass = buttons.Length == 5;
                bool authoredVisualsPass = fiveButtonsPass && buttons.All(button =>
                {
                    Image image = button.GetComponent<Image>();
                    Transform label = button.transform.Find("Label");
                    return image != null && image.sprite != null && image.raycastTarget && label != null;
                });
                bool eventSystemPass = eventSystem != null && eventSystem.GetComponent<InputSystemUIInputModule>() != null;
                bool directTouchPass = inputPass && input != null && input.DirectTouchFallbackEnabled;

                bool deterministicMappingPass = false;
                if (input != null)
                {
                    input.EvaluateButtonStatesForValidation(
                        leftPressed: true,
                        rightPressed: false,
                        acceleratePressed: true,
                        brakeReversePressed: false,
                        interact: true,
                        out float steering,
                        out float throttle,
                        out float brake,
                        out bool interact);

                    bool comboPass = Mathf.Approximately(steering, -1f) &&
                                     Mathf.Approximately(throttle, 1f) &&
                                     Mathf.Approximately(brake, 0f) &&
                                     interact;

                    input.EvaluateButtonStatesForValidation(
                        leftPressed: false,
                        rightPressed: true,
                        acceleratePressed: true,
                        brakeReversePressed: true,
                        interact: false,
                        out steering,
                        out throttle,
                        out brake,
                        out interact);

                    bool conflictPass = Mathf.Approximately(steering, 1f) &&
                                        Mathf.Approximately(throttle, 0f) &&
                                        Mathf.Approximately(brake, 1f) &&
                                        !interact;
                    deterministicMappingPass = comboPass && conflictPass;
                }

                bool debugAdapterPass = vehiclePass &&
                                        (!vehicle.TryGetComponent(out VehicleDebugInput debugInput) || !debugInput.enabled);

                bool allPass = vehiclePass &&
                               canvasPass &&
                               inputPass &&
                               controlsPass &&
                               fiveButtonsPass &&
                               authoredVisualsPass &&
                               eventSystemPass &&
                               directTouchPass &&
                               deterministicMappingPass &&
                               debugAdapterPass;

                string message =
                    "[Beyond The Beat] Phase 0 mobile-controls validation\n" +
                    $"VehicleController available: {PassFail(vehiclePass)}\n" +
                    $"Landscape HUD canvas: {PassFail(canvasPass)}\n" +
                    $"MobileDrivingInput attached: {PassFail(inputPass)}\n" +
                    $"All input references assigned: {PassFail(controlsPass)}\n" +
                    $"Five touch controls present: {PassFail(fiveButtonsPass)}\n" +
                    $"Authored control sprites/hit targets: {PassFail(authoredVisualsPass)}\n" +
                    $"Input System UI module present: {PassFail(eventSystemPass)}\n" +
                    $"Direct Touchscreen fallback enabled: {PassFail(directTouchPass)}\n" +
                    $"Deterministic multitouch mapping: {PassFail(deterministicMappingPass)}\n" +
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

        private static void CreateControlDock(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject dock = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dock.transform.SetParent(parent, false);

            RectTransform rect = dock.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor.x > 0.5f ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = dock.GetComponent<Image>();
            image.sprite = MobileUiTheme.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.02f, 0.035f, 0.055f, 0.34f);
            image.raycastTarget = false;
        }

        private static TouchHoldButton CreateCircleControl(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            float size,
            Anchor anchor,
            Color normalColor,
            Color pressedColor)
        {
            return CreateControl(
                parent,
                name,
                label,
                anchoredPosition,
                new Vector2(size, size),
                anchor,
                MobileUiTheme.CircleSprite,
                normalColor,
                pressedColor,
                label.Length <= 2 ? 72 : 27);
        }

        private static TouchHoldButton CreatePillControl(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            Anchor anchor,
            Color normalColor,
            Color pressedColor)
        {
            return CreateControl(
                parent,
                name,
                label,
                anchoredPosition,
                size,
                anchor,
                MobileUiTheme.RoundedRectSprite,
                normalColor,
                pressedColor,
                25);
        }

        private static TouchHoldButton CreateControl(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            Anchor anchor,
            Sprite sprite,
            Color normalColor,
            Color pressedColor,
            int fontSize)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(TouchHoldButton));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            Vector2 anchorPoint = anchor == Anchor.BottomLeft ? Vector2.zero : new Vector2(1f, 0f);
            rect.anchorMin = anchorPoint;
            rect.anchorMax = anchorPoint;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite == MobileUiTheme.RoundedRectSprite ? Image.Type.Sliced : Image.Type.Simple;
            image.color = normalColor;
            image.raycastTarget = true;

            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.32f, 0.86f, 0.94f, 0.24f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 10f);
            labelRect.offsetMax = new Vector2(-12f, -10f);

            Text text = labelObject.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = MobileUiTheme.White;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Shadow shadow = labelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(2f, -2f);

            TouchHoldButton button = buttonObject.GetComponent<TouchHoldButton>();
            button.ConfigureVisual(image, normalColor, pressedColor);
            return button;
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

        private static void StretchFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
