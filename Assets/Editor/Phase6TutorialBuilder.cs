using System;
using System.Linq;
using BeyondTheBeat.Tutorial;
using BeyondTheBeat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6TutorialBuilder
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string CanvasName = "MobileDrivingCanvas";
        private const string TutorialRootName = "Phase6Tutorial";
        private const string TutorialPanelName = "TutorialOnboardingPanel";
        private const string ProfileAssetPath = "Assets/Settings/Tutorial/Phase6_CoreControlsTutorial.asset";
        private const string ValidationDocPath = "Docs/Validation/PHASE_6_TUTORIAL_ONBOARDING.md";

        [MenuItem("Beyond The Beat/Phase 6/Build Tutorial Onboarding")]
        public static void BuildTutorialOnboarding()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException($"Phase 6 tutorial build requires integrated scene '{ScenePath}'.");
            }

            EnsureFolder("Assets/Settings", "Tutorial");
            TutorialProfile profile = CreateOrUpdateProfile();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveRoot(scene, TutorialRootName);

            GameObject canvas = RequireRoot(scene, CanvasName);
            Transform previousPanel = canvas.transform.Find(TutorialPanelName);
            if (previousPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(previousPanel.gameObject);
            }

            MobileDrivingInput input = canvas.GetComponent<MobileDrivingInput>() ??
                                       throw new InvalidOperationException($"'{CanvasName}' is missing MobileDrivingInput.");

            TutorialHud existingHud = canvas.GetComponent<TutorialHud>();
            if (existingHud != null)
            {
                UnityEngine.Object.DestroyImmediate(existingHud);
            }

            GameObject tutorialRoot = new GameObject(TutorialRootName, typeof(TutorialController));
            TutorialController controller = tutorialRoot.GetComponent<TutorialController>();
            controller.Configure(profile, input, true, true);

            GameObject panel = CreateTutorialPanel(canvas.transform, controller, out TutorialHud hud);

            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save Phase 6 tutorial integration into '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = panel;
            Debug.Log("[Beyond The Beat] Phase 6 tutorial/onboarding profile, observer controller and touch-safe HUD created.");
        }

        [MenuItem("Beyond The Beat/Phase 6/Validate Tutorial Onboarding")]
        public static void ValidateTutorialOnboarding()
        {
            if (!ValidateTutorialOnboardingInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }
            Debug.Log(message);
        }

        public static bool ValidateTutorialOnboardingOrThrow()
        {
            if (ValidateTutorialOnboardingInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }
            throw new InvalidOperationException(message);
        }

        private static bool ValidateTutorialOnboardingInternal(out string message)
        {
            TutorialProfile profile = AssetDatabase.LoadAssetAtPath<TutorialProfile>(ProfileAssetPath);
            if (profile == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                message = "[Beyond The Beat] Phase 6 tutorial validation FAIL: tutorial profile or integrated scene is missing.";
                return false;
            }

            Scene original = SceneManager.GetActiveScene();
            bool opened = original.path != ScenePath;
            Scene scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : original;

            try
            {
                GameObject tutorialRoot = FindRoot(scene, TutorialRootName);
                GameObject canvas = FindRoot(scene, CanvasName);
                TutorialController[] controllers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<TutorialController>(true))
                    .ToArray();
                TutorialHud[] huds = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<TutorialHud>(true))
                    .ToArray();

                TutorialController controller = controllers.Length == 1 ? controllers[0] : null;
                TutorialHud hud = huds.Length == 1 ? huds[0] : null;
                MobileDrivingInput input = canvas != null ? canvas.GetComponent<MobileDrivingInput>() : null;
                Transform panelTransform = canvas != null ? canvas.transform.Find(TutorialPanelName) : null;
                GameObject panel = panelTransform != null ? panelTransform.gameObject : null;

                bool profilePass = profile.IsConfigured &&
                                   profile.StepCount == 4 &&
                                   profile.AllowSkip &&
                                   profile.Steps[0].Signal == TutorialSignal.Steering &&
                                   profile.Steps[1].Signal == TutorialSignal.Accelerate &&
                                   profile.Steps[2].Signal == TutorialSignal.BrakeOrReverse &&
                                   profile.Steps[3].Signal == TutorialSignal.Interaction;

                bool wiringPass = tutorialRoot != null && controller != null && controller.Profile == profile &&
                                  controller.InputSource == input && hud != null && hud.Controller == controller &&
                                  hud.Panel == panel && hud.TitleText != null && hud.InstructionText != null &&
                                  hud.ProgressText != null && hud.SkipButton != null;

                bool touchSafePass = panel != null && panel.GetComponentsInChildren<Graphic>(true).All(graphic =>
                {
                    bool isSkipGraphic = graphic.GetComponent<Button>() != null;
                    return isSkipGraphic ? graphic.raycastTarget : !graphic.raycastTarget;
                });

                bool inheritedPass = FindRoot(scene, "Phase6Performance") != null &&
                                     FindRoot(scene, "Phase5OceanArea") != null &&
                                     FindRoot(scene, "Phase5SwimPrototype") != null &&
                                     FindRoot(scene, "Phase5ExplorationCheckpoints") != null &&
                                     FindRoot(scene, "Phase4FreeRoamActivities") != null &&
                                     FindRoot(scene, "Phase3RestrictedArea") != null &&
                                     FindRoot(scene, "Phase1MissionSystem") != null;

                bool cameraPass = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                    .Count(camera => camera.enabled) == 1;

                bool buildSettingsPass = EditorBuildSettings.scenes.Length == 1 &&
                                         EditorBuildSettings.scenes[0].enabled &&
                                         string.Equals(EditorBuildSettings.scenes[0].path, ScenePath, StringComparison.Ordinal);

                bool docPass = AssetDatabase.LoadAssetAtPath<TextAsset>(ValidationDocPath) != null ||
                               System.IO.File.Exists(ValidationDocPath);

                bool pass = profilePass && wiringPass && touchSafePass && inheritedPass && cameraPass &&
                            buildSettingsPass && docPass;

                message = pass
                    ? "[Beyond The Beat] Phase 6 tutorial/onboarding validation PASS: ordered steer/accelerate/brake-or-reverse/ACTION steps, observer-only input wiring, explicit Skip raycast, inherited gameplay and single-scene build contract are intact."
                    : "[Beyond The Beat] Phase 6 tutorial/onboarding validation FAIL: " +
                      $"profile={profilePass}, wiring={wiringPass}, touchSafe={touchSafePass}, inherited={inheritedPass}, " +
                      $"camera={cameraPass}, buildSettings={buildSettingsPass}, doc={docPass}.";
                return pass;
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static TutorialProfile CreateOrUpdateProfile()
        {
            TutorialProfile profile = AssetDatabase.LoadAssetAtPath<TutorialProfile>(ProfileAssetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<TutorialProfile>();
                AssetDatabase.CreateAsset(profile, ProfileAssetPath);
            }

            profile.Configure(
                "phase6-core-controls",
                true,
                new[]
                {
                    new TutorialStep("steer", "Steer", "Hold LEFT or RIGHT briefly to steer your vehicle.", TutorialSignal.Steering, 0.5f, 0.35f),
                    new TutorialStep("accelerate", "Move", "Hold GO briefly to accelerate.", TutorialSignal.Accelerate, 0.5f, 0.35f),
                    new TutorialStep("brake-reverse", "Brake / Reverse", "Hold REV to slow down or reverse.", TutorialSignal.BrakeOrReverse, 0.5f, 0.35f),
                    new TutorialStep("action", "Interact", "Press ACTION when a world prompt appears.", TutorialSignal.Interaction, 0.1f, 0f)
                });
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static GameObject CreateTutorialPanel(Transform canvas, TutorialController controller, out TutorialHud hud)
        {
            GameObject panel = new GameObject(
                TutorialPanelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.transform.SetParent(canvas, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -82f);
            panelRect.sizeDelta = new Vector2(620f, 154f);

            Image background = panel.GetComponent<Image>();
            background.color = new Color(0.025f, 0.035f, 0.055f, 0.88f);
            background.raycastTarget = false;

            Text title = CreateText(panel.transform, "Title", new Vector2(20f, -12f), new Vector2(-112f, -48f), 22, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text instruction = CreateText(panel.transform, "Instruction", new Vector2(20f, -50f), new Vector2(-20f, -112f), 18, TextAnchor.UpperLeft, FontStyle.Normal);
            Text progress = CreateText(panel.transform, "Progress", new Vector2(20f, -116f), new Vector2(-20f, -140f), 15, TextAnchor.MiddleLeft, FontStyle.Normal);

            GameObject skipObject = new GameObject("Skip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            skipObject.transform.SetParent(panel.transform, false);
            RectTransform skipRect = skipObject.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(1f, 1f);
            skipRect.anchorMax = new Vector2(1f, 1f);
            skipRect.pivot = new Vector2(1f, 1f);
            skipRect.anchoredPosition = new Vector2(-14f, -14f);
            skipRect.sizeDelta = new Vector2(92f, 40f);
            Image skipImage = skipObject.GetComponent<Image>();
            skipImage.color = new Color(1f, 1f, 1f, 0.12f);
            skipImage.raycastTarget = true;
            Button skipButton = skipObject.GetComponent<Button>();

            Text skipText = CreateText(skipObject.transform, "Label", Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform skipTextRect = skipText.GetComponent<RectTransform>();
            skipTextRect.anchorMin = Vector2.zero;
            skipTextRect.anchorMax = Vector2.one;
            skipTextRect.offsetMin = Vector2.zero;
            skipTextRect.offsetMax = Vector2.zero;
            skipText.text = "SKIP";

            hud = canvas.gameObject.AddComponent<TutorialHud>();
            hud.Configure(controller, panel, title, instruction, progress, skipButton);
            panel.SetActive(false);
            return panel;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 offsetMin,
            Vector2 offsetMax,
            int fontSize,
            TextAnchor alignment,
            FontStyle style)
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
            text.alignment = alignment;
            text.fontStyle = style;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return FindRoot(scene, name) ?? throw new InvalidOperationException($"Missing required root '{name}'.");
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            GameObject existing = FindRoot(scene, name);
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
    }
}
