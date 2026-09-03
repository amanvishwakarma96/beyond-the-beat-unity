using System;
using System.Linq;
using System.Reflection;
using BeyondTheBeat.Performance;
using BeyondTheBeat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6PerformanceBuilder
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string RootName = "Phase6Performance";
        private const string CanvasName = "MobileDrivingCanvas";
        private const string OverlayName = "PerformanceDiagnosticsOverlay";
        private const string BudgetAssetPath = "Assets/Settings/Performance/Phase6_MobilePerformanceBudget.asset";
        private const string ValidationDocPath = "Docs/Validation/PHASE_6_PERFORMANCE_FOUNDATION.md";

        [MenuItem("Beyond The Beat/Phase 6/Build Performance Foundation")]
        public static void BuildPerformanceFoundation()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException($"Phase 6 performance build requires integrated scene '{ScenePath}'.");
            }

            EnsureFolder("Assets/Settings", "Performance");
            MobilePerformanceBudget budget = CreateOrUpdateBudget();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveRoot(scene, RootName);

            GameObject canvas = RequireRoot(scene, CanvasName);
            Transform previousOverlay = canvas.transform.Find(OverlayName);
            if (previousOverlay != null)
            {
                UnityEngine.Object.DestroyImmediate(previousOverlay.gameObject);
            }

            GameObject root = new GameObject(RootName, typeof(MobilePerformanceMonitor));
            MobilePerformanceMonitor monitor = root.GetComponent<MobilePerformanceMonitor>();
            SerializedObject monitorSerialized = new SerializedObject(monitor);
            SetObjectReference(monitorSerialized, "budget", budget);
            SetBool(monitorSerialized, "applyFrameRatePolicy", true);
            monitorSerialized.ApplyModifiedPropertiesWithoutUndo();

            PerformanceDiagnosticsOverlay overlay = CreateOverlay(canvas.transform, monitor);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save Phase 6 performance integration into '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = overlay.gameObject;
            Debug.Log("[Beyond The Beat] Phase 6 mobile performance budget, sampled monitor and development diagnostics overlay created.");
        }

        [MenuItem("Beyond The Beat/Phase 6/Validate Performance Foundation")]
        public static void ValidatePerformanceFoundation()
        {
            if (!ValidatePerformanceFoundationInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }
            Debug.Log(message);
        }

        public static bool ValidatePerformanceFoundationOrThrow()
        {
            if (ValidatePerformanceFoundationInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }
            throw new InvalidOperationException(message);
        }

        private static bool ValidatePerformanceFoundationInternal(out string message)
        {
            MobilePerformanceBudget budget = AssetDatabase.LoadAssetAtPath<MobilePerformanceBudget>(BudgetAssetPath);
            if (budget == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                message = "[Beyond The Beat] Phase 6 performance validation FAIL: budget asset or integrated scene is missing.";
                return false;
            }

            Scene original = SceneManager.GetActiveScene();
            bool opened = original.path != ScenePath;
            Scene scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : original;

            try
            {
                GameObject performanceRoot = FindRoot(scene, RootName);
                GameObject canvas = FindRoot(scene, CanvasName);
                MobilePerformanceMonitor[] monitors = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MobilePerformanceMonitor>(true))
                    .ToArray();
                PerformanceDiagnosticsOverlay[] overlays = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<PerformanceDiagnosticsOverlay>(true))
                    .ToArray();

                MobilePerformanceMonitor monitor = monitors.Length == 1 ? monitors[0] : null;
                PerformanceDiagnosticsOverlay overlay = overlays.Length == 1 ? overlays[0] : null;

                bool budgetPass = budget.IsConfigured &&
                                  budget.BaselineTargetFps == 30 &&
                                  budget.StretchTargetFps == 60 &&
                                  Mathf.Approximately(budget.WarningFrameTimeMilliseconds, 37f) &&
                                  budget.WarningAllocatedMemoryMb == 1024 &&
                                  budget.MaximumApkSizeMb == 200 &&
                                  Mathf.Approximately(budget.SampleIntervalSeconds, 1f);

                bool wiringPass = performanceRoot != null && monitor != null && monitor.Budget == budget &&
                                  monitor.ApplyFrameRatePolicy && canvas != null && overlay != null &&
                                  overlay.Monitor == monitor && overlay.MetricsText != null;

                bool touchSafePass = overlay != null &&
                                     overlay.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget);

                bool samplingPass = ValidateClassification(budget) &&
                                    typeof(PerformanceDiagnosticsOverlay).GetMethod(
                                        "Update",
                                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null;

                bool inheritedPass = FindRoot(scene, "Phase5OceanArea") != null &&
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

                bool pass = budgetPass && wiringPass && touchSafePass && samplingPass && inheritedPass &&
                            cameraPass && buildSettingsPass && docPass;

                message = pass
                    ? "[Beyond The Beat] Phase 6 performance foundation validation PASS: 30/60 FPS targets, 37 ms warning frame budget, 1024 MB memory warning, 200 MB APK ceiling, sampled monitor, non-raycasting development overlay, inherited Phase 5 gameplay and single-scene build contract are intact."
                    : "[Beyond The Beat] Phase 6 performance foundation validation FAIL: " +
                      $"budget={budgetPass}, wiring={wiringPass}, touchSafe={touchSafePass}, sampling={samplingPass}, " +
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

        private static bool ValidateClassification(MobilePerformanceBudget budget)
        {
            MobilePerformanceSnapshot healthy = MobilePerformanceMonitor.EvaluateSnapshot(
                budget, 31f, 32f, 512L * 1024L * 1024L);
            MobilePerformanceSnapshot lowFps = MobilePerformanceMonitor.EvaluateSnapshot(
                budget, 24f, 42f, 512L * 1024L * 1024L);
            MobilePerformanceSnapshot highMemory = MobilePerformanceMonitor.EvaluateSnapshot(
                budget, 30f, 33f, 1200L * 1024L * 1024L);

            return healthy.IsWithinBudget &&
                   (lowFps.WarningFlags & PerformanceWarningFlags.LowFps) != 0 &&
                   (lowFps.WarningFlags & PerformanceWarningFlags.HighFrameTime) != 0 &&
                   (highMemory.WarningFlags & PerformanceWarningFlags.HighAllocatedMemory) != 0;
        }

        private static MobilePerformanceBudget CreateOrUpdateBudget()
        {
            MobilePerformanceBudget budget = AssetDatabase.LoadAssetAtPath<MobilePerformanceBudget>(BudgetAssetPath);
            if (budget == null)
            {
                budget = ScriptableObject.CreateInstance<MobilePerformanceBudget>();
                AssetDatabase.CreateAsset(budget, BudgetAssetPath);
            }

            SerializedObject serialized = new SerializedObject(budget);
            SetInt(serialized, "baselineTargetFps", 30);
            SetInt(serialized, "stretchTargetFps", 60);
            SetFloat(serialized, "warningFrameTimeMilliseconds", 37f);
            SetInt(serialized, "warningAllocatedMemoryMb", 1024);
            SetInt(serialized, "maximumApkSizeMb", 200);
            SetFloat(serialized, "sampleIntervalSeconds", 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(budget);
            return budget;
        }

        private static PerformanceDiagnosticsOverlay CreateOverlay(Transform canvas, MobilePerformanceMonitor monitor)
        {
            GameObject overlayObject = new GameObject(
                OverlayName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(PerformanceDiagnosticsOverlay));
            overlayObject.transform.SetParent(canvas, false);

            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -18f);
            rect.sizeDelta = new Vector2(330f, 84f);

            Image background = overlayObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Metrics", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(overlayObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 6f);
            textRect.offsetMax = new Vector2(-10f, -6f);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = "FPS --  FRAME -- ms\nMEM -- MB";
            text.raycastTarget = false;

            PerformanceDiagnosticsOverlay overlay = overlayObject.GetComponent<PerformanceDiagnosticsOverlay>();
            SerializedObject overlaySerialized = new SerializedObject(overlay);
            SetObjectReference(overlaySerialized, "monitor", monitor);
            SetObjectReference(overlaySerialized, "metricsText", text);
            overlaySerialized.ApplyModifiedPropertiesWithoutUndo();
            return overlay;
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

        private static void SetObjectReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            property.objectReferenceValue = value;
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            property.boolValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            property.intValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            property.floatValue = value;
        }
    }
}
