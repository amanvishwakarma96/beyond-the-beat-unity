using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BeyondTheBeat.Performance;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6MobileQualityBuilder
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string PerformanceRootName = "Phase6Performance";
        private const string GameplayCameraName = "GameplayCamera";
        private const string OceanRootName = "Phase5OceanArea";
        private const string OceanSurfaceName = "OceanSurface";
        private const string ExplorationRootName = "Phase5ExplorationCheckpoints";
        private const string ExplorationMarkerName = "Marker";
        private const string ProfileAssetPath = "Assets/Settings/Performance/Phase6_MobileQualityProfile.asset";
        private const string ValidationDocPath = "Docs/Validation/PHASE_6_MOBILE_QUALITY_OPTIMIZATION.md";

        [MenuItem("Beyond The Beat/Phase 6/Build Mobile Quality Optimization")]
        public static void BuildMobileQualityOptimization()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException($"Phase 6 quality optimization requires integrated scene '{ScenePath}'.");
            }

            MobileQualityProfile profile = CreateOrUpdateProfile();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject performanceRoot = RequireRoot(scene, PerformanceRootName);
            Camera gameplayCamera = RequireRoot(scene, GameplayCameraName).GetComponent<Camera>() ??
                                    throw new InvalidOperationException("GameplayCamera root is missing Camera component.");

            MobileQualityBootstrap[] existing = performanceRoot.GetComponents<MobileQualityBootstrap>();
            for (int i = 1; i < existing.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(existing[i]);
            }

            MobileQualityBootstrap bootstrap = existing.Length > 0
                ? existing[0]
                : performanceRoot.AddComponent<MobileQualityBootstrap>();

            SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
            SetObjectReference(serializedBootstrap, "profile", profile);
            SetObjectReference(serializedBootstrap, "gameplayCamera", gameplayCamera);
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            // Persist the camera-side settings in the scene as well as applying them again at runtime.
            gameplayCamera.allowHDR = profile.CameraHdr;
            gameplayCamera.allowMSAA = profile.CameraMsaa;

            Renderer[] decorativeRenderers = GetDecorativeRenderers(scene);
            if (decorativeRenderers.Length != 4)
            {
                throw new InvalidOperationException(
                    $"Expected exactly four known decorative renderers (OceanSurface + 3 exploration markers), found {decorativeRenderers.Length}.");
            }

            for (int i = 0; i < decorativeRenderers.Length; i++)
            {
                OptimizeDecorativeRenderer(decorativeRenderers[i]);
            }

            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(gameplayCamera);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save Phase 6 quality optimization into '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = performanceRoot;
            Debug.Log(
                "[Beyond The Beat] Phase 6 mobile quality optimization applied: data-driven shadow/MSAA/LOD profile, gameplay HDR disabled, and probe/shadow work removed only from OceanSurface + exploration markers.");
        }

        [MenuItem("Beyond The Beat/Phase 6/Validate Mobile Quality Optimization")]
        public static void ValidateMobileQualityOptimization()
        {
            if (!ValidateMobileQualityOptimizationInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateMobileQualityOptimizationOrThrow()
        {
            if (ValidateMobileQualityOptimizationInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateMobileQualityOptimizationInternal(out string message)
        {
            MobileQualityProfile profile = AssetDatabase.LoadAssetAtPath<MobileQualityProfile>(ProfileAssetPath);
            if (profile == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                message = "[Beyond The Beat] Phase 6 mobile quality validation FAIL: profile asset or integrated scene is missing.";
                return false;
            }

            Scene original = SceneManager.GetActiveScene();
            bool opened = original.path != ScenePath;
            Scene scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : original;

            try
            {
                GameObject performanceRoot = FindRoot(scene, PerformanceRootName);
                GameObject cameraRoot = FindRoot(scene, GameplayCameraName);
                Camera gameplayCamera = cameraRoot != null ? cameraRoot.GetComponent<Camera>() : null;
                MobileQualityBootstrap[] bootstraps = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MobileQualityBootstrap>(true))
                    .ToArray();
                MobilePerformanceMonitor[] monitors = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MobilePerformanceMonitor>(true))
                    .ToArray();
                Renderer[] decorativeRenderers = GetDecorativeRenderers(scene);

                bool profilePass = profile.IsConfigured &&
                                   Mathf.Approximately(profile.ShadowDistance, 35f) &&
                                   profile.ShadowCascades == 2 &&
                                   profile.ShadowQuality == ShadowQuality.HardOnly &&
                                   profile.ShadowResolution == ShadowResolution.Medium &&
                                   profile.AntiAliasing == 2 &&
                                   Mathf.Approximately(profile.LodBias, 0.8f) &&
                                   !profile.RealtimeReflectionProbes &&
                                   !profile.SoftParticles &&
                                   !profile.CameraHdr &&
                                   profile.CameraMsaa;

                MobileQualityBootstrap bootstrap = bootstraps.Length == 1 ? bootstraps[0] : null;
                bool wiringPass = performanceRoot != null &&
                                  bootstrap != null &&
                                  bootstrap.gameObject == performanceRoot &&
                                  bootstrap.Profile == profile &&
                                  bootstrap.GameplayCamera == gameplayCamera &&
                                  monitors.Length == 1;

                bool oneShotPass = typeof(MobileQualityBootstrap).GetMethod(
                                           "Update",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null &&
                                       typeof(MobileQualityBootstrap).GetMethod(
                                           "LateUpdate",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null &&
                                       typeof(MobileQualityBootstrap).GetMethod(
                                           "FixedUpdate",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null;

                int enabledCameraCount = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                    .Count(camera => camera.enabled);
                bool cameraPass = gameplayCamera != null && enabledCameraCount == 1 &&
                                  !gameplayCamera.allowHDR && gameplayCamera.allowMSAA;

                bool decorativePass = decorativeRenderers.Length == 4 && decorativeRenderers.All(IsDecorativeRendererOptimized);

                bool inheritedPass = FindRoot(scene, "Phase5OceanArea") != null &&
                                     FindRoot(scene, "Phase5SwimPrototype") != null &&
                                     FindRoot(scene, "Phase5ExplorationCheckpoints") != null &&
                                     FindRoot(scene, "PrototypeVehicle") != null &&
                                     FindRoot(scene, "Phase3RestrictedArea") != null &&
                                     FindRoot(scene, "Phase1MissionSystem") != null;

                bool buildSettingsPass = EditorBuildSettings.scenes.Length == 1 &&
                                         EditorBuildSettings.scenes[0].enabled &&
                                         string.Equals(EditorBuildSettings.scenes[0].path, ScenePath, StringComparison.Ordinal);
                bool docPass = AssetDatabase.LoadAssetAtPath<TextAsset>(ValidationDocPath) != null || File.Exists(ValidationDocPath);

                bool pass = profilePass && wiringPass && oneShotPass && cameraPass && decorativePass &&
                            inheritedPass && buildSettingsPass && docPass;

                message = pass
                    ? "[Beyond The Beat] Phase 6 mobile quality validation PASS: 35m hard-shadow/2-cascade profile, 2x MSAA, 0.8 LOD bias, HDR/reflection/soft-particle reductions, one-shot bootstrap and scoped decorative-renderer optimization are intact. Physical Android FPS/thermal evidence remains required."
                    : "[Beyond The Beat] Phase 6 mobile quality validation FAIL: " +
                      $"profile={profilePass}, wiring={wiringPass}, oneShot={oneShotPass}, camera={cameraPass}, " +
                      $"decorative={decorativePass}, inherited={inheritedPass}, buildSettings={buildSettingsPass}, doc={docPass}.";
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

        private static MobileQualityProfile CreateOrUpdateProfile()
        {
            MobileQualityProfile profile = AssetDatabase.LoadAssetAtPath<MobileQualityProfile>(ProfileAssetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<MobileQualityProfile>();
                AssetDatabase.CreateAsset(profile, ProfileAssetPath);
            }

            SerializedObject serialized = new SerializedObject(profile);
            SetFloat(serialized, "shadowDistance", 35f);
            SetInt(serialized, "shadowCascades", 2);
            SetInt(serialized, "shadowQuality", (int)ShadowQuality.HardOnly);
            SetInt(serialized, "shadowResolution", (int)ShadowResolution.Medium);
            SetInt(serialized, "antiAliasing", 2);
            SetFloat(serialized, "lodBias", 0.8f);
            SetBool(serialized, "realtimeReflectionProbes", false);
            SetBool(serialized, "softParticles", false);
            SetBool(serialized, "cameraHdr", false);
            SetBool(serialized, "cameraMsaa", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static Renderer[] GetDecorativeRenderers(Scene scene)
        {
            List<Renderer> renderers = new List<Renderer>(4);

            GameObject oceanRoot = FindRoot(scene, OceanRootName);
            Transform oceanSurface = oceanRoot != null ? oceanRoot.transform.Find(OceanSurfaceName) : null;
            Renderer oceanRenderer = oceanSurface != null ? oceanSurface.GetComponent<Renderer>() : null;
            if (oceanRenderer != null)
            {
                renderers.Add(oceanRenderer);
            }

            GameObject explorationRoot = FindRoot(scene, ExplorationRootName);
            if (explorationRoot != null)
            {
                Renderer[] markerRenderers = explorationRoot.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => string.Equals(renderer.gameObject.name, ExplorationMarkerName, StringComparison.Ordinal))
                    .OrderBy(renderer => renderer.transform.parent != null ? renderer.transform.parent.name : renderer.name, StringComparer.Ordinal)
                    .ToArray();
                renderers.AddRange(markerRenderers);
            }

            return renderers.ToArray();
        }

        private static void OptimizeDecorativeRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            EditorUtility.SetDirty(renderer);
        }

        private static bool IsDecorativeRendererOptimized(Renderer renderer)
        {
            return renderer != null &&
                   renderer.shadowCastingMode == ShadowCastingMode.Off &&
                   !renderer.receiveShadows &&
                   renderer.lightProbeUsage == LightProbeUsage.Off &&
                   renderer.reflectionProbeUsage == ReflectionProbeUsage.Off;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => string.Equals(root.name, name, StringComparison.Ordinal));
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return FindRoot(scene, name) ?? throw new InvalidOperationException($"Missing required root '{name}'.");
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
