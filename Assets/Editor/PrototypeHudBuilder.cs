using System;
using System.Linq;
using BeyondTheBeat.Interaction;
using BeyondTheBeat.UI;
using BeyondTheBeat.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class PrototypeHudBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Phase0_Prototype.unity";
        private const string CanvasName = "MobileDrivingCanvas";
        private const string VehicleName = "PrototypeVehicle";
        private const string HudRootName = "InteractionHUD";
        private const string PromptPanelName = "InteractionPrompt";
        private const string FeedbackPanelName = "SuccessFeedback";
        private const string SpeedPanelName = "SpeedPanel";

        [MenuItem("Beyond The Beat/Phase 0/Build Minimal HUD")]
        private static void BuildMinimalHud()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
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
            GameObject canvasObject = FindRootObject(scene, CanvasName);
            GameObject vehicle = FindRootObject(scene, VehicleName);

            if (canvasObject == null || !canvasObject.TryGetComponent<Canvas>(out _))
            {
                Debug.LogError("[Beyond The Beat] MobileDrivingCanvas is required. Build mobile driving controls first.");
                return;
            }

            if (vehicle == null ||
                !vehicle.TryGetComponent(out InteractionController interactionController) ||
                !vehicle.TryGetComponent(out VehicleController vehicleController))
            {
                Debug.LogError("[Beyond The Beat] PrototypeVehicle with InteractionController and VehicleController is required.");
                return;
            }

            ParkingZone parkingZone = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ParkingZone>(true))
                .FirstOrDefault();

            if (parkingZone == null)
            {
                Debug.LogError("[Beyond The Beat] ParkingZone is required. Build the parking interaction first.");
                return;
            }

            Transform existingHud = canvasObject.transform.Find(HudRootName);
            if (existingHud != null)
            {
                UnityEngine.Object.DestroyImmediate(existingHud.gameObject);
            }

            GameObject hudRoot = new GameObject(HudRootName, typeof(RectTransform), typeof(UIManager), typeof(DrivingHud));
            hudRoot.transform.SetParent(canvasObject.transform, false);
            StretchFullScreen(hudRoot.GetComponent<RectTransform>());

            PanelRefs prompt = CreatePanel(
                hudRoot.transform,
                PromptPanelName,
                new Vector2(0.5f, 0f),
                new Vector2(0f, 48f),
                new Vector2(520f, 66f),
                new Color(0.025f, 0.045f, 0.07f, 0.94f),
                27,
                TextAnchor.MiddleCenter,
                MobileUiTheme.Cyan);

            PanelRefs feedback = CreatePanel(
                hudRoot.transform,
                FeedbackPanelName,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -34f),
                new Vector2(480f, 62f),
                new Color(0.04f, 0.25f, 0.17f, 0.96f),
                26,
                TextAnchor.MiddleCenter,
                new Color(0.28f, 0.95f, 0.63f, 1f));

            SpeedRefs speed = CreateSpeedPanel(hudRoot.transform);

            prompt.Root.SetActive(false);
            feedback.Root.SetActive(false);

            UIManager uiManager = hudRoot.GetComponent<UIManager>();
            SerializedObject serializedUi = new SerializedObject(uiManager);
            SetObjectReference(serializedUi, "interactionController", interactionController);
            SetObjectReference(serializedUi, "parkingZone", parkingZone);
            SetObjectReference(serializedUi, "promptRoot", prompt.Root);
            SetObjectReference(serializedUi, "promptText", prompt.Text);
            SetObjectReference(serializedUi, "feedbackRoot", feedback.Root);
            SetObjectReference(serializedUi, "feedbackText", feedback.Text);
            serializedUi.FindProperty("feedbackDuration").floatValue = 2f;
            SerializedProperty promptPrefix = serializedUi.FindProperty("promptPrefix");
            if (promptPrefix != null)
            {
                promptPrefix.stringValue = "ACTION  •  ";
            }
            serializedUi.ApplyModifiedPropertiesWithoutUndo();

            DrivingHud drivingHud = hudRoot.GetComponent<DrivingHud>();
            SerializedObject serializedDriving = new SerializedObject(drivingHud);
            SetObjectReference(serializedDriving, "vehicleController", vehicleController);
            SetObjectReference(serializedDriving, "speedValueText", speed.Value);
            SetObjectReference(serializedDriving, "speedUnitText", speed.Unit);
            serializedDriving.ApplyModifiedPropertiesWithoutUndo();
            speed.Value.text = "000";
            speed.Unit.text = "KM/H";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = hudRoot;
            Debug.Log(
                "[Beyond The Beat] Authored driving HUD created: compact speed telemetry, interaction pill, " +
                "and success feedback. All presentation graphics are non-raycasting.");
        }

        [MenuItem("Beyond The Beat/Phase 0/Validate Minimal HUD")]
        private static void ValidateMinimalHud()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] HUD validation FAIL: scene not found at {ScenePath}.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject canvasObject = FindRootObject(validationScene, CanvasName);
                GameObject vehicle = FindRootObject(validationScene, VehicleName);
                Transform hudTransform = canvasObject != null ? canvasObject.transform.Find(HudRootName) : null;
                GameObject hudRoot = hudTransform != null ? hudTransform.gameObject : null;
                InteractionController controller = null;
                VehicleController vehicleController = null;
                UIManager uiManager = null;
                DrivingHud drivingHud = null;

                bool canvasPass = canvasObject != null && canvasObject.TryGetComponent<Canvas>(out _);
                bool controllerPass = vehicle != null &&
                                      vehicle.TryGetComponent(out controller) &&
                                      vehicle.TryGetComponent(out vehicleController);
                bool hudPass = hudRoot != null &&
                               hudRoot.TryGetComponent(out uiManager) &&
                               hudRoot.TryGetComponent(out drivingHud);
                bool promptPass = hudPass && hudRoot.transform.Find(PromptPanelName) != null;
                bool feedbackPass = hudPass && hudRoot.transform.Find(FeedbackPanelName) != null;
                bool speedPass = hudPass &&
                                 hudRoot.transform.Find(SpeedPanelName) != null &&
                                 drivingHud != null &&
                                 drivingHud.VehicleController == vehicleController &&
                                 drivingHud.SpeedValueText != null &&
                                 drivingHud.SpeedUnitText != null;
                bool referencesPass = hudPass && uiManager != null && controller != null && ValidateUiReferences(uiManager, controller);
                bool initiallyHiddenPass = promptPass && feedbackPass &&
                                           !hudRoot.transform.Find(PromptPanelName).gameObject.activeSelf &&
                                           !hudRoot.transform.Find(FeedbackPanelName).gameObject.activeSelf;
                bool authoredVisualsPass = hudPass && hudRoot.GetComponentsInChildren<Image>(true).All(image =>
                    !image.raycastTarget && image.sprite != null);

                bool allPass = canvasPass &&
                               controllerPass &&
                               hudPass &&
                               promptPass &&
                               feedbackPass &&
                               speedPass &&
                               referencesPass &&
                               initiallyHiddenPass &&
                               authoredVisualsPass;

                string message =
                    "[Beyond The Beat] Phase 0 authored HUD validation\n" +
                    $"Mobile canvas available: {PassFail(canvasPass)}\n" +
                    $"Vehicle/interaction sources available: {PassFail(controllerPass)}\n" +
                    $"UIManager + DrivingHud attached: {PassFail(hudPass)}\n" +
                    $"Interaction prompt pill: {PassFail(promptPass)}\n" +
                    $"Success feedback pill: {PassFail(feedbackPass)}\n" +
                    $"Speed telemetry panel: {PassFail(speedPass)}\n" +
                    $"UI source/view references: {PassFail(referencesPass)}\n" +
                    $"Prompt/feedback initially hidden: {PassFail(initiallyHiddenPass)}\n" +
                    $"Authored non-raycasting presentation sprites: {PassFail(authoredVisualsPass)}";

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

        private static SpeedRefs CreateSpeedPanel(Transform parent)
        {
            GameObject root = new GameObject(SpeedPanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-28f, -28f);
            rect.sizeDelta = new Vector2(230f, 126f);

            Image image = root.GetComponent<Image>();
            image.sprite = MobileUiTheme.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.025f, 0.045f, 0.07f, 0.92f);
            image.raycastTarget = false;

            CreateAccentBar(root.transform, new Color(0.11f, 0.74f, 0.82f, 1f));

            Text value = CreateText(root.transform, "SpeedValue", 58, FontStyle.Bold, TextAnchor.MiddleRight);
            RectTransform valueRect = value.rectTransform;
            valueRect.anchorMin = new Vector2(0f, 0.28f);
            valueRect.anchorMax = new Vector2(0.72f, 1f);
            valueRect.offsetMin = new Vector2(18f, 0f);
            valueRect.offsetMax = new Vector2(0f, -8f);

            Text unit = CreateText(root.transform, "SpeedUnit", 19, FontStyle.Bold, TextAnchor.MiddleLeft);
            RectTransform unitRect = unit.rectTransform;
            unitRect.anchorMin = new Vector2(0.72f, 0.28f);
            unitRect.anchorMax = new Vector2(1f, 1f);
            unitRect.offsetMin = new Vector2(2f, 0f);
            unitRect.offsetMax = new Vector2(-12f, -8f);
            unit.color = MobileUiTheme.Muted;

            Text label = CreateText(root.transform, "SpeedLabel", 15, FontStyle.Bold, TextAnchor.MiddleLeft);
            label.text = "SPEED";
            label.color = MobileUiTheme.Cyan;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.30f);
            labelRect.offsetMin = new Vector2(18f, 5f);
            labelRect.offsetMax = new Vector2(-12f, 0f);

            return new SpeedRefs(root, value, unit);
        }

        private static PanelRefs CreatePanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Color backgroundColor,
            int fontSize,
            TextAnchor alignment,
            Color accentColor)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor.y > 0.5f ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = root.GetComponent<Image>();
            image.sprite = MobileUiTheme.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = backgroundColor;
            image.raycastTarget = false;

            CreateAccentBar(root.transform, accentColor);

            Text text = CreateText(root.transform, "Text", fontSize, FontStyle.Bold, alignment);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(26f, 8f);
            textRect.offsetMax = new Vector2(-18f, -8f);

            return new PanelRefs(root, text);
        }

        private static void CreateAccentBar(Transform parent, Color color)
        {
            GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(parent, false);
            RectTransform rect = accent.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.18f);
            rect.anchorMax = new Vector2(0f, 0.82f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(8f, 0f);
            rect.sizeDelta = new Vector2(5f, 0f);

            Image image = accent.GetComponent<Image>();
            image.sprite = MobileUiTheme.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            int fontSize,
            FontStyle style,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = MobileUiTheme.White;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void StretchFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static bool ValidateUiReferences(UIManager manager, InteractionController expectedController)
        {
            SerializedObject serialized = new SerializedObject(manager);
            SerializedProperty controller = serialized.FindProperty("interactionController");
            SerializedProperty zone = serialized.FindProperty("parkingZone");
            SerializedProperty promptRoot = serialized.FindProperty("promptRoot");
            SerializedProperty promptText = serialized.FindProperty("promptText");
            SerializedProperty feedbackRoot = serialized.FindProperty("feedbackRoot");
            SerializedProperty feedbackText = serialized.FindProperty("feedbackText");

            return controller != null && controller.objectReferenceValue == expectedController &&
                   zone != null && zone.objectReferenceValue is ParkingZone &&
                   promptRoot != null && promptRoot.objectReferenceValue != null &&
                   promptText != null && promptText.objectReferenceValue != null &&
                   feedbackRoot != null && feedbackRoot.objectReferenceValue != null &&
                   feedbackText != null && feedbackText.objectReferenceValue != null;
        }

        private static void SetObjectReference(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"HUD property '{propertyName}' was not found.");
            }

            property.objectReferenceValue = value;
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";

        private readonly struct PanelRefs
        {
            public PanelRefs(GameObject root, Text text)
            {
                Root = root;
                Text = text;
            }

            public GameObject Root { get; }
            public Text Text { get; }
        }

        private readonly struct SpeedRefs
        {
            public SpeedRefs(GameObject root, Text value, Text unit)
            {
                Root = root;
                Value = value;
                Unit = unit;
            }

            public GameObject Root { get; }
            public Text Value { get; }
            public Text Unit { get; }
        }
    }
}
