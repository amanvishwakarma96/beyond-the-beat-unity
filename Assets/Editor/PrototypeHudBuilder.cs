using System;
using System.Linq;
using BeyondTheBeat.Interaction;
using BeyondTheBeat.UI;
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

        [MenuItem("Beyond The Beat/Phase 0/Build Minimal HUD")]
        private static void BuildMinimalHud()
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
            GameObject canvasObject = FindRootObject(scene, CanvasName);
            GameObject vehicle = FindRootObject(scene, VehicleName);

            if (canvasObject == null || !canvasObject.TryGetComponent<Canvas>(out _))
            {
                Debug.LogError("[Beyond The Beat] MobileDrivingCanvas is required. Build mobile driving controls first.");
                return;
            }

            if (vehicle == null || !vehicle.TryGetComponent(out InteractionController interactionController))
            {
                Debug.LogError("[Beyond The Beat] PrototypeVehicle with InteractionController is required.");
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

            GameObject hudRoot = new GameObject(HudRootName, typeof(RectTransform), typeof(UIManager));
            hudRoot.transform.SetParent(canvasObject.transform, false);
            StretchFullScreen(hudRoot.GetComponent<RectTransform>());

            PanelRefs prompt = CreatePanel(
                hudRoot.transform,
                PromptPanelName,
                new Vector2(0.5f, 0f),
                new Vector2(0f, 92f),
                new Vector2(680f, 86f),
                new Color(0.04f, 0.05f, 0.07f, 0.82f),
                34,
                TextAnchor.MiddleCenter);

            PanelRefs feedback = CreatePanel(
                hudRoot.transform,
                FeedbackPanelName,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -92f),
                new Vector2(620f, 82f),
                new Color(0.05f, 0.28f, 0.12f, 0.88f),
                34,
                TextAnchor.MiddleCenter);

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
            serializedUi.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = hudRoot;
            Debug.Log("[Beyond The Beat] Minimal Phase 0 HUD created. Prompt and parking-success feedback are event-driven.");
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
                UIManager uiManager = null;

                bool canvasPass = canvasObject != null && canvasObject.TryGetComponent<Canvas>(out _);
                bool controllerPass = vehicle != null && vehicle.TryGetComponent(out controller);
                bool hudPass = hudRoot != null && hudRoot.TryGetComponent(out uiManager);
                bool promptPass = hudPass && hudRoot.transform.Find(PromptPanelName) != null;
                bool feedbackPass = hudPass && hudRoot.transform.Find(FeedbackPanelName) != null;
                bool referencesPass = hudPass && uiManager != null && controller != null && ValidateUiReferences(uiManager, controller);
                bool initiallyHiddenPass = promptPass && feedbackPass &&
                                           !hudRoot.transform.Find(PromptPanelName).gameObject.activeSelf &&
                                           !hudRoot.transform.Find(FeedbackPanelName).gameObject.activeSelf;

                bool allPass = canvasPass && controllerPass && hudPass && promptPass && feedbackPass && referencesPass && initiallyHiddenPass;

                string message =
                    "[Beyond The Beat] Phase 0 minimal HUD validation\n" +
                    $"Mobile canvas available: {PassFail(canvasPass)}\n" +
                    $"InteractionController available: {PassFail(controllerPass)}\n" +
                    $"UIManager attached: {PassFail(hudPass)}\n" +
                    $"Interaction prompt panel: {PassFail(promptPass)}\n" +
                    $"Success feedback panel: {PassFail(feedbackPass)}\n" +
                    $"UI source/view references: {PassFail(referencesPass)}\n" +
                    $"Prompt/feedback initially hidden: {PassFail(initiallyHiddenPass)}";

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

        private static PanelRefs CreatePanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Color backgroundColor,
            int fontSize,
            TextAnchor alignment)
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
            image.color = backgroundColor;
            image.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(root.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(22f, 10f);
            textRect.offsetMax = new Vector2(-22f, -10f);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 22;
            text.resizeTextMaxSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;

            return new PanelRefs(root, text);
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
                throw new InvalidOperationException($"UIManager property '{propertyName}' was not found.");
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
    }
}
