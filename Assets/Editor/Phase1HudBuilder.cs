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
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(620f, 190f);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.035f, 0.045f, 0.065f, 0.88f);
            panelImage.raycastTarget = false;

            Text title = CreateText(
                panel.transform,
                TitleName,
                new Vector2(18f, -56f),
                new Vector2(-18f, -14f),
                30,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);

            Text objective = CreateText(
                panel.transform,
                ObjectiveName,
                new Vector2(18f, -142f),
                new Vector2(-18f, -58f),
                24,
                FontStyle.Normal,
                TextAnchor.UpperLeft);

            Text status = CreateText(
                panel.transform,
                StatusName,
                new Vector2(18f, -176f),
                new Vector2(-18f, -146f),
                20,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);

            MissionHud hud = hudRoot.GetComponent<MissionHud>();
            SerializedObject serialized = new SerializedObject(hud);
            SetObjectReference(serialized, "missionManager", missionManager);
            SetObjectReference(serialized, "panelRoot", panel);
            SetObjectReference(serialized, "titleText", title);
            SetObjectReference(serialized, "objectiveText", objective);
            SetObjectReference(serialized, "statusText", status);
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
                "[Beyond The Beat] Phase 1 Mission HUD created. " +
                "Mission state is event-driven and completed/failed states explicitly release the player to free roam.");
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
                    hud.StatusText != null;

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
                    complete.Status.IndexOf("FREE ROAM", StringComparison.Ordinal) >= 0 &&
                    failed.Status.IndexOf("FREE ROAM", StringComparison.Ordinal) >= 0;

                bool nonBlockingPass = structurePass &&
                                       hud.PanelRoot.TryGetComponent(out Image panelImage) &&
                                       !panelImage.raycastTarget;

                bool allPass = structurePass && referencesPass && freeRoamPass && activePass && completionPass && nonBlockingPass;

                string message =
                    "[Beyond The Beat] Phase 1 Mission HUD validation\n" +
                    $"HUD structure/text references: {PassFail(structurePass)}\n" +
                    $"MissionManager source reference: {PassFail(referencesPass)}\n" +
                    $"Free-roam view state: {PassFail(freeRoamPass)}\n" +
                    $"Active mission view state: {PassFail(activePass)}\n" +
                    $"Completed/failed free-roam state: {PassFail(completionPass)}\n" +
                    $"HUD does not block touch input: {PassFail(nonBlockingPass)}";

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

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 offsetMin,
            Vector2 offsetMax,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
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
    }
}
