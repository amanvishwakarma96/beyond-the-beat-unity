using System;
using System.Linq;
using BeyondTheBeat.Missions;
using BeyondTheBeat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class Phase1HudBuilder
    {
        private const string ScenePath = Phase1WorldBuilder.Phase1ScenePath;
        private const string CanvasName = "MobileDrivingCanvas";
        private const string MissionRootName = "Phase1MissionSystem";
        private const string HudRootName = "Phase1MissionHUD";
        private const string PanelName = "MissionPanel";
        private const string TitleName = "MissionTitle";
        private const string ObjectiveName = "MissionObjective";
        private const string StatusName = "MissionStatus";
        private const string ProgressRootName = "MissionProgress";

        [MenuItem("Beyond The Beat/Phase 1/Build Mission HUD")]
        public static void BuildMissionHud()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Mission HUD build requires Phase 1 scene '{ScenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvasObject = FindRootObject(scene, CanvasName);
            GameObject missionRoot = FindRootObject(scene, MissionRootName);
            MissionManager missionManager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;

            if (canvasObject == null || !canvasObject.TryGetComponent<Canvas>(out _) || missionManager == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] Mission HUD build requires MobileDrivingCanvas and Phase1MissionSystem/MissionManager.");
                return;
            }

            Transform existing = canvasObject.transform.Find(HudRootName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject hudRoot = new GameObject(HudRootName, typeof(RectTransform), typeof(MissionHud));
            hudRoot.transform.SetParent(canvasObject.transform, false);
            StretchFullScreen(hudRoot.GetComponent<RectTransform>());

            GameObject panel = new GameObject(PanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(hudRoot.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(28f, -28f);
            panelRect.sizeDelta = new Vector2(580f, 210f);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.sprite = MobileUiTheme.RoundedRectSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.025f, 0.045f, 0.07f, 0.93f);
            panelImage.raycastTarget = false;

            CreateAccentBar(panel.transform);

            Text eyebrow = CreateText(panel.transform, "MissionEyebrow", 14, FontStyle.Bold, TextAnchor.MiddleLeft);
            eyebrow.text = "CURRENT OBJECTIVE";
            eyebrow.color = MobileUiTheme.Cyan;
            SetAnchors(eyebrow.rectTransform, new Vector2(0.055f, 0.82f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);

            Text title = CreateText(panel.transform, TitleName, 29, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetAnchors(title.rectTransform, new Vector2(0.055f, 0.60f), new Vector2(0.95f, 0.84f), Vector2.zero, Vector2.zero);

            Text objective = CreateText(panel.transform, ObjectiveName, 19, FontStyle.Normal, TextAnchor.UpperLeft);
            objective.color = MobileUiTheme.Muted;
            SetAnchors(objective.rectTransform, new Vector2(0.055f, 0.30f), new Vector2(0.95f, 0.62f), Vector2.zero, Vector2.zero);

            Text status = CreateText(panel.transform, StatusName, 17, FontStyle.Bold, TextAnchor.MiddleLeft);
            status.color = MobileUiTheme.Cyan;
            SetAnchors(status.rectTransform, new Vector2(0.055f, 0.15f), new Vector2(0.95f, 0.31f), Vector2.zero, Vector2.zero);

            ProgressRefs progress = CreateProgressBar(panel.transform);
            progress.Root.SetActive(false);

            MissionHud hud = hudRoot.GetComponent<MissionHud>();
            SerializedObject serialized = new SerializedObject(hud);
            SetObjectReference(serialized, "missionManager", missionManager);
            SetObjectReference(serialized, "panelRoot", panel);
            SetObjectReference(serialized, "titleText", title);
            SetObjectReference(serialized, "objectiveText", objective);
            SetObjectReference(serialized, "statusText", status);
            SetObjectReference(serialized, "progressRoot", progress.Root);
            SetObjectReference(serialized, "progressFill", progress.Fill);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            MissionHudSnapshot initial = MissionHud.CreateSnapshot(null, MissionState.Inactive);
            title.text = initial.Title;
            objective.text = initial.Objective;
            status.text = initial.Status;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save Phase 1 Mission HUD into '{ScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = hudRoot;

            Debug.Log(
                "[Beyond The Beat] Authored mission HUD created with compact hierarchy and reusable survival-progress bar.");
        }

        [MenuItem("Beyond The Beat/Phase 1/Validate Mission HUD")]
        public static void ValidateMissionHud()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Mission HUD validation FAIL: scene missing at '{ScenePath}'.");
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
                GameObject missionRoot = FindRootObject(validationScene, MissionRootName);
                MissionManager manager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                Transform hudTransform = canvasObject != null ? canvasObject.transform.Find(HudRootName) : null;
                MissionHud hud = hudTransform != null ? hudTransform.GetComponent<MissionHud>() : null;
                MissionDefinition mission = manager != null ? manager.StartingMission : null;

                bool structurePass =
                    canvasObject != null &&
                    canvasObject.TryGetComponent<Canvas>(out _) &&
                    hud != null &&
                    hud.PanelRoot != null &&
                    hud.TitleText != null &&
                    hud.ObjectiveText != null &&
                    hud.StatusText != null &&
                    hud.ProgressRoot != null &&
                    hud.ProgressFill != null;

                bool referencesPass = structurePass && hud.MissionManager == manager && manager != null;

                MissionHudSnapshot freeRoam = MissionHud.CreateSnapshot(null, MissionState.Inactive);
                MissionHudSnapshot active = MissionHud.CreateSnapshot(mission, MissionState.Active);
                MissionHudSnapshot complete = MissionHud.CreateSnapshot(mission, MissionState.Completed);
                MissionHudSnapshot failed = MissionHud.CreateSnapshot(mission, MissionState.Failed);

                bool freeRoamPass =
                    freeRoam.Title == "FREE ROAM" &&
                    freeRoam.Status == "NO ACTIVE MISSION";

                bool activePass =
                    mission != null &&
                    active.Title == mission.DisplayName &&
                    active.Status == "MISSION ACTIVE" &&
                    !string.IsNullOrWhiteSpace(active.Objective);

                bool completionPass =
                    complete.Title == "MISSION COMPLETE" &&
                    complete.Status.IndexOf("COMPLETE", StringComparison.Ordinal) >= 0 &&
                    failed.Status.IndexOf("FAILED", StringComparison.Ordinal) >= 0;

                bool progressPass = structurePass &&
                                    hud.ProgressFill.type == Image.Type.Filled &&
                                    hud.ProgressFill.fillMethod == Image.FillMethod.Horizontal &&
                                    !hud.ProgressRoot.activeSelf;

                bool nonBlockingPass = structurePass &&
                                       hud.PanelRoot.GetComponentsInChildren<Image>(true).All(image => !image.raycastTarget);

                bool authoredVisualsPass = structurePass &&
                                           hud.PanelRoot.TryGetComponent(out Image authoredPanel) &&
                                           authoredPanel.sprite != null;

                bool allPass = structurePass &&
                               referencesPass &&
                               freeRoamPass &&
                               activePass &&
                               completionPass &&
                               progressPass &&
                               nonBlockingPass &&
                               authoredVisualsPass;

                string message =
                    "[Beyond The Beat] Phase 1 authored Mission HUD validation\n" +
                    $"HUD structure/text/progress references: {PassFail(structurePass)}\n" +
                    $"MissionManager source reference: {PassFail(referencesPass)}\n" +
                    $"Free-roam view state: {PassFail(freeRoamPass)}\n" +
                    $"Active mission view state: {PassFail(activePass)}\n" +
                    $"Completed/failed view state: {PassFail(completionPass)}\n" +
                    $"Survival progress presentation: {PassFail(progressPass)}\n" +
                    $"HUD does not block touch input: {PassFail(nonBlockingPass)}\n" +
                    $"Authored rounded presentation: {PassFail(authoredVisualsPass)}";

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

        private static ProgressRefs CreateProgressBar(Transform parent)
        {
            GameObject root = new GameObject(ProgressRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetAnchors(rootRect, new Vector2(0.055f, 0.055f), new Vector2(0.95f, 0.115f), Vector2.zero, Vector2.zero);

            GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trackObject.transform.SetParent(root.transform, false);
            StretchFullScreen(trackObject.GetComponent<RectTransform>());
            Image track = trackObject.GetComponent<Image>();
            track.sprite = MobileUiTheme.RoundedRectSprite;
            track.type = Image.Type.Sliced;
            track.color = new Color(0.08f, 0.13f, 0.18f, 1f);
            track.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(root.transform, false);
            StretchFullScreen(fillObject.GetComponent<RectTransform>());
            Image fill = fillObject.GetComponent<Image>();
            fill.sprite = MobileUiTheme.RoundedRectSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            fill.color = MobileUiTheme.Cyan;
            fill.raycastTarget = false;

            return new ProgressRefs(root, fill);
        }

        private static void CreateAccentBar(Transform parent)
        {
            GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(parent, false);
            RectTransform rect = accent.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.12f);
            rect.anchorMax = new Vector2(0f, 0.88f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(9f, 0f);
            rect.sizeDelta = new Vector2(6f, 0f);

            Image image = accent.GetComponent<Image>();
            image.sprite = MobileUiTheme.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = MobileUiTheme.Cyan;
            image.raycastTarget = false;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = MobileUiTheme.White;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void StretchFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetObjectReference(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"MissionHud serialized field '{propertyName}' could not be resolved.");
            }

            property.objectReferenceValue = value;
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";

        private readonly struct ProgressRefs
        {
            public ProgressRefs(GameObject root, Image fill)
            {
                Root = root;
                Fill = fill;
            }

            public GameObject Root { get; }
            public Image Fill { get; }
        }
    }
}
