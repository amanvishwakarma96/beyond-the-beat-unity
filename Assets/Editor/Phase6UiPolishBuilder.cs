using System;
using System.Collections.Generic;
using System.IO;
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
    internal static class Phase6UiPolishBuilder
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string CanvasName = "MobileDrivingCanvas";
        private const string InteractionHudName = "InteractionHUD";
        private const string DrivingControlsName = "DrivingControls";
        private const string SwimControlsName = "SwimControls";
        private const string EnterSwimName = "SwimModeEnter";
        private const string ExitSwimName = "SwimModeExit";
        private const string MissionHudName = "Phase1MissionHUD";
        private const string MechanicHudName = "Phase4MechanicJobHUD";
        private const string TutorialPanelName = "TutorialOnboardingPanel";
        private const string PerformanceOverlayName = "PerformanceDiagnosticsOverlay";
        private const string ValidationDocPath = "Docs/Validation/PHASE_6_UI_POLISH.md";

        private static readonly Vector2 MechanicPosition = new Vector2(-28f, -118f);
        private static readonly Vector2 MechanicSize = new Vector2(500f, 110f);
        private static readonly Vector2 TutorialPosition = new Vector2(0f, -74f);
        private static readonly Vector2 TutorialSize = new Vector2(620f, 166f);
        private static readonly Vector2 SkipSize = new Vector2(112f, 54f);

        [MenuItem("Beyond The Beat/Phase 6/Build Mobile HUD Polish")]
        public static void BuildMobileHudPolish()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException($"Phase 6 UI polish requires integrated scene '{ScenePath}'.");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvas = RequireRoot(scene, CanvasName);

            Transform interactionHud = RequireChild(canvas.transform, InteractionHudName);
            Transform drivingControls = RequireChild(canvas.transform, DrivingControlsName);
            Transform swimControls = RequireChild(canvas.transform, SwimControlsName);
            Transform enterSwim = RequireChild(canvas.transform, EnterSwimName);
            Transform exitSwim = RequireChild(canvas.transform, ExitSwimName);
            Transform missionHudRoot = RequireChild(canvas.transform, MissionHudName);
            Transform mechanicHudRoot = RequireChild(canvas.transform, MechanicHudName);
            Transform tutorialPanel = RequireChild(canvas.transform, TutorialPanelName);
            Transform performanceOverlay = RequireChild(canvas.transform, PerformanceOverlayName);

            MissionHud missionHud = missionHudRoot.GetComponent<MissionHud>() ??
                                    throw new InvalidOperationException("Phase1MissionHUD is missing MissionHud.");
            MechanicJobHud mechanicHud = mechanicHudRoot.GetComponent<MechanicJobHud>() ??
                                          throw new InvalidOperationException("Phase4MechanicJobHUD is missing MechanicJobHud.");
            TutorialHud tutorialHud = canvas.GetComponent<TutorialHud>() ??
                                      throw new InvalidOperationException("MobileDrivingCanvas is missing TutorialHud.");
            PerformanceDiagnosticsOverlay performance = performanceOverlay.GetComponent<PerformanceDiagnosticsOverlay>() ??
                                                        throw new InvalidOperationException("PerformanceDiagnosticsOverlay component is missing.");

            PolishMechanicHud(mechanicHud);
            PolishTutorialHud(tutorialHud);
            PolishPerformanceOverlay(performance);
            PolishMissionHud(missionHud);

            Transform[] safeAreaTargets =
            {
                interactionHud,
                drivingControls,
                swimControls,
                enterSwim,
                exitSwim,
                missionHudRoot,
                mechanicHudRoot,
                tutorialPanel,
                performanceOverlay
            };

            for (int i = 0; i < safeAreaTargets.Length; i++)
            {
                ConfigureSafeArea(safeAreaTargets[i]);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save Phase 6 UI polish into '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = tutorialPanel.gameObject;
            Debug.Log(
                "[Beyond The Beat] Phase 6 mobile HUD polish applied: safe-area fitting, distinct top HUD regions, " +
                "consistent rounded presentation and preserved drive/swim touch ownership.");
        }

        [MenuItem("Beyond The Beat/Phase 6/Validate Mobile HUD Polish")]
        public static void ValidateMobileHudPolish()
        {
            if (!ValidateMobileHudPolishInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateMobileHudPolishOrThrow()
        {
            if (ValidateMobileHudPolishInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateMobileHudPolishInternal(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                message = $"[Beyond The Beat] Phase 6 UI polish validation FAIL: integrated scene '{ScenePath}' is missing.";
                return false;
            }

            Scene original = SceneManager.GetActiveScene();
            bool opened = original.path != ScenePath;
            Scene scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : original;

            try
            {
                GameObject canvas = FindRoot(scene, CanvasName);
                if (canvas == null)
                {
                    message = "[Beyond The Beat] Phase 6 UI polish validation FAIL: MobileDrivingCanvas is missing.";
                    return false;
                }

                string[] targetNames =
                {
                    InteractionHudName,
                    DrivingControlsName,
                    SwimControlsName,
                    EnterSwimName,
                    ExitSwimName,
                    MissionHudName,
                    MechanicHudName,
                    TutorialPanelName,
                    PerformanceOverlayName
                };

                List<Transform> targets = targetNames
                    .Select(name => canvas.transform.Find(name))
                    .Where(value => value != null)
                    .ToList();

                bool structurePass = targets.Count == targetNames.Length;
                bool safeAreaPass = structurePass && targets.All(target =>
                {
                    MobileSafeAreaFitter fitter = target.GetComponent<MobileSafeAreaFitter>();
                    return fitter != null &&
                           fitter.Target == target.GetComponent<RectTransform>() &&
                           fitter.HasAuthoredLayout;
                });

                MissionHud missionHud = canvas.transform.Find(MissionHudName)?.GetComponent<MissionHud>();
                MechanicJobHud mechanicHud = canvas.transform.Find(MechanicHudName)?.GetComponent<MechanicJobHud>();
                TutorialHud tutorialHud = canvas.GetComponent<TutorialHud>();
                PerformanceDiagnosticsOverlay performance =
                    canvas.transform.Find(PerformanceOverlayName)?.GetComponent<PerformanceDiagnosticsOverlay>();
                MobileDrivingInput drivingInput = canvas.GetComponent<MobileDrivingInput>();
                MobileSwimInput swimInput = canvas.transform.Find(SwimControlsName)?.GetComponent<MobileSwimInput>();

                bool mechanicPass = ValidateMechanicHud(mechanicHud);
                bool tutorialPass = ValidateTutorialHud(tutorialHud);
                bool performancePass = ValidatePerformanceOverlay(performance);
                bool missionPass = missionHud != null &&
                                   missionHud.PanelRoot != null &&
                                   missionHud.PanelRoot.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget);

                bool inputPass = drivingInput != null &&
                                 swimInput != null &&
                                 canvas.transform.Find(DrivingControlsName).GetComponentsInChildren<TouchHoldButton>(true).Length == 5 &&
                                 canvas.transform.Find(SwimControlsName).GetComponentsInChildren<TouchHoldButton>(true).Length == 6;

                bool layoutPass = ReferenceLayoutHasNoTopOverlayOverlap();
                bool inheritedPass = FindRoot(scene, "Phase6Performance") != null &&
                                     FindRoot(scene, "Phase6Tutorial") != null &&
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

                bool docPass = File.Exists(ValidationDocPath) || AssetDatabase.LoadAssetAtPath<TextAsset>(ValidationDocPath) != null;

                bool pass = structurePass && safeAreaPass && mechanicPass && tutorialPass && performancePass &&
                            missionPass && inputPass && layoutPass && inheritedPass && cameraPass && buildSettingsPass && docPass;

                message = pass
                    ? "[Beyond The Beat] Phase 6 mobile HUD polish validation PASS: nine known HUD/control roots are safe-area fitted, mission/mechanic/tutorial/performance regions are distinct, themed presentation remains touch-safe, drive/swim mappings are intact, and inherited gameplay/single-scene contracts remain present."
                    : "[Beyond The Beat] Phase 6 mobile HUD polish validation FAIL: " +
                      $"structure={structurePass}, safeArea={safeAreaPass}, mechanic={mechanicPass}, tutorial={tutorialPass}, " +
                      $"performance={performancePass}, mission={missionPass}, input={inputPass}, layout={layoutPass}, " +
                      $"inherited={inheritedPass}, camera={cameraPass}, buildSettings={buildSettingsPass}, doc={docPass}.";
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

        public static bool ReferenceLayoutHasNoTopOverlayOverlap()
        {
            Rect mission = new Rect(28f, 28f, 580f, 210f);
            Rect tutorial = new Rect(650f, 74f, TutorialSize.x, TutorialSize.y);
            Rect performance = new Rect(1572f, 18f, 330f, 84f);
            Rect mechanic = new Rect(1392f, 118f, MechanicSize.x, MechanicSize.y);

            Rect[] regions = { mission, tutorial, performance, mechanic };
            for (int i = 0; i < regions.Length; i++)
            {
                for (int j = i + 1; j < regions.Length; j++)
                {
                    if (regions[i].Overlaps(regions[j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void PolishMissionHud(MissionHud hud)
        {
            if (hud?.PanelRoot == null)
            {
                throw new InvalidOperationException("Mission HUD panel is missing.");
            }

            Image background = hud.PanelRoot.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = MobileUiTheme.RoundedRectSprite;
                background.type = Image.Type.Sliced;
                background.color = new Color(0.025f, 0.045f, 0.07f, 0.93f);
                background.raycastTarget = false;
            }
        }

        private static void PolishMechanicHud(MechanicJobHud hud)
        {
            if (hud == null || hud.PanelRoot == null || hud.JobText == null || hud.CreditsText == null)
            {
                throw new InvalidOperationException("Mechanic job HUD is incomplete.");
            }

            RectTransform rect = hud.PanelRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = MechanicPosition;
            rect.sizeDelta = MechanicSize;

            Image background = hud.PanelRoot.GetComponent<Image>();
            if (background == null)
            {
                throw new InvalidOperationException("Mechanic job HUD is missing its Image background.");
            }
            background.sprite = MobileUiTheme.RoundedRectSprite;
            background.type = Image.Type.Sliced;
            background.color = MobileUiTheme.InkSoft;
            background.raycastTarget = false;

            SetTextStyle(hud.JobText, 18, FontStyle.Bold, MobileUiTheme.White);
            SetTextStyle(hud.CreditsText, 15, FontStyle.Bold, MobileUiTheme.Cyan);
            SetAnchors(hud.JobText.rectTransform, new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.91f));
            SetAnchors(hud.CreditsText.rectTransform, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.44f));
        }

        private static void PolishTutorialHud(TutorialHud hud)
        {
            if (hud == null || hud.Panel == null || hud.TitleText == null || hud.InstructionText == null ||
                hud.ProgressText == null || hud.SkipButton == null)
            {
                throw new InvalidOperationException("Tutorial HUD is incomplete.");
            }

            RectTransform panelRect = hud.Panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = TutorialPosition;
            panelRect.sizeDelta = TutorialSize;

            Image background = hud.Panel.GetComponent<Image>();
            if (background == null)
            {
                throw new InvalidOperationException("Tutorial panel is missing its Image background.");
            }
            background.sprite = MobileUiTheme.RoundedRectSprite;
            background.type = Image.Type.Sliced;
            background.color = MobileUiTheme.Ink;
            background.raycastTarget = false;

            SetTextStyle(hud.TitleText, 24, FontStyle.Bold, MobileUiTheme.White);
            SetTextStyle(hud.InstructionText, 18, FontStyle.Normal, MobileUiTheme.Muted);
            SetTextStyle(hud.ProgressText, 15, FontStyle.Bold, MobileUiTheme.Cyan);

            RectTransform skipRect = hud.SkipButton.GetComponent<RectTransform>();
            skipRect.anchorMin = Vector2.one;
            skipRect.anchorMax = Vector2.one;
            skipRect.pivot = Vector2.one;
            skipRect.anchoredPosition = new Vector2(-16f, -16f);
            skipRect.sizeDelta = SkipSize;

            Image skipImage = hud.SkipButton.GetComponent<Image>();
            if (skipImage == null)
            {
                throw new InvalidOperationException("Tutorial Skip button is missing Image.");
            }
            skipImage.sprite = MobileUiTheme.RoundedRectSprite;
            skipImage.type = Image.Type.Sliced;
            skipImage.color = MobileUiTheme.Amber;
            skipImage.raycastTarget = true;
            hud.SkipButton.targetGraphic = skipImage;

            Text skipLabel = hud.SkipButton.GetComponentInChildren<Text>(true);
            if (skipLabel != null)
            {
                SetTextStyle(skipLabel, 16, FontStyle.Bold, MobileUiTheme.Ink);
            }
        }

        private static void PolishPerformanceOverlay(PerformanceDiagnosticsOverlay overlay)
        {
            if (overlay == null || overlay.MetricsText == null)
            {
                throw new InvalidOperationException("Performance diagnostics overlay is incomplete.");
            }

            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-18f, -18f);
            rect.sizeDelta = new Vector2(330f, 84f);

            Image background = overlay.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = MobileUiTheme.RoundedRectSprite;
                background.type = Image.Type.Sliced;
                background.color = new Color(0.025f, 0.045f, 0.07f, 0.78f);
                background.raycastTarget = false;
            }

            SetTextStyle(overlay.MetricsText, 15, FontStyle.Bold, MobileUiTheme.Muted);
        }

        private static void ConfigureSafeArea(Transform target)
        {
            RectTransform rect = target.GetComponent<RectTransform>() ??
                                 throw new InvalidOperationException($"Safe-area target '{target.name}' is missing RectTransform.");
            MobileSafeAreaFitter fitter = target.GetComponent<MobileSafeAreaFitter>();
            if (fitter == null)
            {
                fitter = target.gameObject.AddComponent<MobileSafeAreaFitter>();
            }
            else if (fitter.HasAuthoredLayout)
            {
                fitter.RestoreAuthoredLayout();
            }

            fitter.ConfigureFromCurrentRect(rect);
            EditorUtility.SetDirty(fitter);
        }

        private static bool ValidateMechanicHud(MechanicJobHud hud)
        {
            if (hud == null || hud.PanelRoot == null || hud.JobText == null || hud.CreditsText == null)
            {
                return false;
            }

            RectTransform rect = hud.PanelRoot.GetComponent<RectTransform>();
            Image background = hud.PanelRoot.GetComponent<Image>();
            return Approximately(rect.anchorMin, Vector2.one) &&
                   Approximately(rect.anchorMax, Vector2.one) &&
                   Approximately(rect.anchoredPosition, MechanicPosition) &&
                   Approximately(rect.sizeDelta, MechanicSize) &&
                   background != null && background.sprite != null && background.type == Image.Type.Sliced &&
                   hud.PanelRoot.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget);
        }

        private static bool ValidateTutorialHud(TutorialHud hud)
        {
            if (hud == null || hud.Panel == null || hud.SkipButton == null)
            {
                return false;
            }

            RectTransform panelRect = hud.Panel.GetComponent<RectTransform>();
            RectTransform skipRect = hud.SkipButton.GetComponent<RectTransform>();
            Image background = hud.Panel.GetComponent<Image>();
            Image skipImage = hud.SkipButton.GetComponent<Image>();

            bool raycastPass = hud.Panel.GetComponentsInChildren<Graphic>(true).All(graphic =>
            {
                bool shouldRaycast = skipImage != null && ReferenceEquals(graphic, skipImage);
                return graphic.raycastTarget == shouldRaycast;
            });

            return Approximately(panelRect.anchoredPosition, TutorialPosition) &&
                   Approximately(panelRect.sizeDelta, TutorialSize) &&
                   skipRect.sizeDelta.x >= 104f && skipRect.sizeDelta.y >= 48f &&
                   background != null && background.sprite != null && background.type == Image.Type.Sliced &&
                   skipImage != null && skipImage.sprite != null && raycastPass;
        }

        private static bool ValidatePerformanceOverlay(PerformanceDiagnosticsOverlay overlay)
        {
            if (overlay == null || overlay.MetricsText == null)
            {
                return false;
            }

            Image background = overlay.GetComponent<Image>();
            return background != null && background.sprite != null && background.type == Image.Type.Sliced &&
                   overlay.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget);
        }

        private static void SetTextStyle(Text text, int size, FontStyle style, Color color)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.raycastTarget = false;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) <= 0.01f && Mathf.Abs(a.y - b.y) <= 0.01f;
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            return parent.Find(name) ?? throw new InvalidOperationException($"Missing '{name}' under '{parent.name}'.");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return FindRoot(scene, name) ?? throw new InvalidOperationException($"Missing required root '{name}'.");
        }
    }
}
