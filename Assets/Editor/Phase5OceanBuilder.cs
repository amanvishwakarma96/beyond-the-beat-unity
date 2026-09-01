using System;
using System.Reflection;
using BeyondTheBeat.Water;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase5OceanBuilder
    {
        public const string Phase5ScenePath = "Assets/Scenes/Phase5/Phase5_Ocean.unity";

        private const string OceanRootName = "Phase5OceanArea";
        private const string OceanVolumeName = "OceanVolume";
        private const string OceanSurfaceName = "OceanSurface";
        private const string ShorelineName = "OceanShoreline";
        private const string OceanZoneId = "ocean";
        private const string MaterialPath = "Assets/Materials/Phase5/OceanSurface.mat";
        private const float SurfaceY = -0.25f;
        private const float MaxDepth = 12f;

        // Existing Forest content is centered around x=130,z=0 and Restricted content around x=205,z=0.
        // Keep the first ocean north of those systems so Phase 5 does not rewrite or overlap earlier gameplay space.
        private static readonly Vector3 OceanCenter = new Vector3(130f, SurfaceY, 150f);
        private static readonly Vector3 OceanSize = new Vector3(140f, MaxDepth, 80f);

        [MenuItem("Beyond The Beat/Phase 5/Build Ocean Foundation")]
        public static void BuildOceanFoundation()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase4CookingBuilder.Phase4ScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Phase 5 ocean build requires the integrated Phase 4 scene at '{Phase4CookingBuilder.Phase4ScenePath}'.");
            }

            EnsureFolder("Assets/Scenes", "Phase5");
            EnsureFolder("Assets/Materials", "Phase5");

            Scene sourceScene = EditorSceneManager.OpenScene(Phase4CookingBuilder.Phase4ScenePath, OpenSceneMode.Single);
            if (sourceScene.isDirty && !EditorSceneManager.SaveScene(sourceScene))
            {
                throw new InvalidOperationException("Unable to save the Phase 4 source scene before creating Phase 5.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase5ScenePath) != null &&
                !AssetDatabase.DeleteAsset(Phase5ScenePath))
            {
                throw new InvalidOperationException($"Unable to replace existing Phase 5 scene at '{Phase5ScenePath}'.");
            }

            if (!AssetDatabase.CopyAsset(Phase4CookingBuilder.Phase4ScenePath, Phase5ScenePath))
            {
                throw new InvalidOperationException($"Unable to copy Phase 4 scene to '{Phase5ScenePath}'.");
            }

            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.OpenScene(Phase5ScenePath, OpenSceneMode.Single);
            RemoveRoot(scene, OceanRootName);

            GameObject oceanRoot = new GameObject(OceanRootName);
            WaterVolume waterVolume = CreateOceanVolume(oceanRoot.transform);
            CreateOceanSurface(oceanRoot.transform);
            CreateShoreline(oceanRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save generated Phase 5 scene at '{Phase5ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Phase5ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = waterVolume.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Phase 5 ocean foundation created north of the existing world. Water context/depth data is isolated from future swim controls and rendering uses one static opaque mobile-friendly material with no reflection/refraction pass.");
        }

        [MenuItem("Beyond The Beat/Phase 5/Validate Ocean Foundation")]
        public static void ValidateOceanFoundation()
        {
            if (!ValidateOceanFoundationInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateOceanFoundationOrThrow()
        {
            if (ValidateOceanFoundationInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateOceanFoundationInternal(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase5ScenePath) == null)
            {
                message = $"[Beyond The Beat] Phase 5 ocean validation FAIL: scene not found at '{Phase5ScenePath}'.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != Phase5ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(Phase5ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject oceanRoot = FindRootObject(validationScene, OceanRootName);
                Transform volumeTransform = oceanRoot != null ? oceanRoot.transform.Find(OceanVolumeName) : null;
                Transform surfaceTransform = oceanRoot != null ? oceanRoot.transform.Find(OceanSurfaceName) : null;
                Transform shorelineTransform = oceanRoot != null ? oceanRoot.transform.Find(ShorelineName) : null;

                WaterVolume waterVolume = volumeTransform != null ? volumeTransform.GetComponent<WaterVolume>() : null;
                ZoneContext oceanContext = volumeTransform != null ? volumeTransform.GetComponent<ZoneContext>() : null;
                BoxCollider volumeCollider = volumeTransform != null ? volumeTransform.GetComponent<BoxCollider>() : null;
                Renderer surfaceRenderer = surfaceTransform != null ? surfaceTransform.GetComponent<Renderer>() : null;

                int oceanContextCount = 0;
                ZoneContext[] contexts = UnityEngine.Object.FindObjectsByType<ZoneContext>(FindObjectsSortMode.None);
                for (int i = 0; i < contexts.Length; i++)
                {
                    ZoneContext context = contexts[i];
                    if (context != null && context.gameObject.scene == validationScene && context.ZoneType == WorldZoneType.Ocean)
                    {
                        oceanContextCount++;
                    }
                }

                bool enumPass =
                    (int)WorldZoneType.Urban == 0 &&
                    (int)WorldZoneType.OffRoad == 1 &&
                    (int)WorldZoneType.Forest == 2 &&
                    (int)WorldZoneType.Restricted == 3 &&
                    (int)WorldZoneType.Ocean == 4;

                bool structurePass =
                    oceanRoot != null &&
                    waterVolume != null &&
                    waterVolume.IsConfigured &&
                    oceanContext != null &&
                    oceanContext.ZoneType == WorldZoneType.Ocean &&
                    string.Equals(oceanContext.ZoneId, OceanZoneId, StringComparison.Ordinal) &&
                    oceanContextCount == 1 &&
                    waterVolume.ZoneContext == oceanContext &&
                    waterVolume.VolumeCollider == volumeCollider &&
                    volumeCollider != null &&
                    volumeCollider.isTrigger &&
                    Mathf.Approximately(waterVolume.SurfaceY, SurfaceY) &&
                    Mathf.Approximately(waterVolume.MaxDepth, MaxDepth) &&
                    volumeTransform.position.z > 100f &&
                    surfaceTransform != null &&
                    shorelineTransform != null;

                bool queryPass = ValidateWaterQueries(waterVolume, volumeCollider);
                bool noPerFrameLoop = typeof(WaterVolume).GetMethod(
                    "Update",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null;

                bool mobileSurfacePass =
                    surfaceRenderer != null &&
                    surfaceRenderer.sharedMaterial != null &&
                    surfaceRenderer.sharedMaterial.shader != null &&
                    surfaceRenderer.sharedMaterial.renderQueue < (int)RenderQueue.Transparent &&
                    oceanRoot.GetComponentsInChildren<Camera>(true).Length == 0 &&
                    oceanRoot.GetComponentsInChildren<Light>(true).Length == 0;

                bool inheritedPhase4Pass =
                    FindRootObject(validationScene, "Phase4FreeRoamActivities") != null &&
                    FindRootObject(validationScene, "ParkingPrototype") != null &&
                    FindRootObject(validationScene, "Phase1MissionSystem") != null &&
                    FindRootObject(validationScene, "Phase3RestrictedArea") != null &&
                    FindRootObject(validationScene, "Phase4MechanicJobSystem") != null &&
                    FindRootObject(validationScene, "MobileDrivingCanvas") != null;

                bool buildSettingsPass =
                    EditorBuildSettings.scenes.Length == 1 &&
                    EditorBuildSettings.scenes[0].enabled &&
                    string.Equals(EditorBuildSettings.scenes[0].path, Phase5ScenePath, StringComparison.Ordinal);

                bool pass = enumPass && structurePass && queryPass && noPerFrameLoop &&
                            mobileSurfacePass && inheritedPhase4Pass && buildSettingsPass;

                message = pass
                    ? "[Beyond The Beat] Phase 5 ocean foundation validation PASS: additive Ocean context, reusable depth queries, non-overlapping north-edge placement, static mobile-friendly water surface, inherited Phase 4 gameplay and single-scene build contract are intact. Physical Android water-area validation remains required."
                    : "[Beyond The Beat] Phase 5 ocean foundation validation FAIL: " +
                      $"enum={enumPass}, structure={structurePass}, queries={queryPass}, noUpdate={noPerFrameLoop}, " +
                      $"mobileSurface={mobileSurfacePass}, inheritedPhase4={inheritedPhase4Pass}, buildSettings={buildSettingsPass}.";
                return pass;
            }
            finally
            {
                if (openedForValidation && validationScene.IsValid() && validationScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(validationScene, true);
                }
            }
        }

        private static bool ValidateWaterQueries(WaterVolume waterVolume, BoxCollider volumeCollider)
        {
            if (waterVolume == null || volumeCollider == null)
            {
                return false;
            }

            Vector3 center = volumeCollider.bounds.center;
            Vector3 belowSurface = new Vector3(center.x, SurfaceY - 3f, center.z);
            Vector3 aboveSurface = new Vector3(center.x, SurfaceY + 1f, center.z);
            Vector3 outside = new Vector3(volumeCollider.bounds.max.x + 5f, SurfaceY - 3f, center.z);
            Vector3 deep = new Vector3(center.x, SurfaceY - MaxDepth * 2f, center.z);

            return waterVolume.ContainsHorizontalPosition(belowSurface) &&
                   Mathf.Approximately(waterVolume.GetDepthAt(belowSurface), 3f) &&
                   Mathf.Approximately(waterVolume.GetDepthAt(aboveSurface), 0f) &&
                   Mathf.Approximately(waterVolume.GetDepthAt(outside), 0f) &&
                   Mathf.Approximately(waterVolume.GetDepthAt(deep), MaxDepth) &&
                   Mathf.Approximately(waterVolume.GetNormalizedDepthAt(deep), 1f);
        }

        private static WaterVolume CreateOceanVolume(Transform parent)
        {
            GameObject volumeObject = new GameObject(OceanVolumeName);
            volumeObject.transform.SetParent(parent, false);
            volumeObject.transform.position = OceanCenter;

            BoxCollider collider = volumeObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = OceanSize;
            collider.center = new Vector3(0f, -MaxDepth * 0.5f, 0f);

            ZoneContext context = volumeObject.AddComponent<ZoneContext>();
            SerializedObject serializedContext = new SerializedObject(context);
            SetString(serializedContext, "zoneId", OceanZoneId);
            SetInt(serializedContext, "zoneType", (int)WorldZoneType.Ocean);
            serializedContext.ApplyModifiedPropertiesWithoutUndo();

            WaterVolume waterVolume = volumeObject.AddComponent<WaterVolume>();
            SerializedObject serializedWater = new SerializedObject(waterVolume);
            SetObjectReference(serializedWater, "zoneContext", context);
            SetObjectReference(serializedWater, "volumeCollider", collider);
            SetFloat(serializedWater, "surfaceY", SurfaceY);
            SetFloat(serializedWater, "maxDepth", MaxDepth);
            serializedWater.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(context);
            EditorUtility.SetDirty(waterVolume);
            return waterVolume;
        }

        private static void CreateOceanSurface(Transform parent)
        {
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = OceanSurfaceName;
            surface.transform.SetParent(parent, false);
            surface.transform.position = new Vector3(OceanCenter.x, SurfaceY - 0.06f, OceanCenter.z);
            surface.transform.localScale = new Vector3(OceanSize.x, 0.12f, OceanSize.z);

            Collider collider = surface.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            surface.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateOceanMaterial();
        }

        private static void CreateShoreline(Transform parent)
        {
            GameObject shore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shore.name = ShorelineName;
            shore.transform.SetParent(parent, false);
            shore.transform.position = new Vector3(OceanCenter.x, SurfaceY + 0.15f, OceanCenter.z - OceanSize.z * 0.5f - 5f);
            shore.transform.localScale = new Vector3(OceanSize.x, 0.4f, 10f);
        }

        private static Material CreateOrUpdateOceanMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Color") ??
                            Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("Unable to resolve a lightweight shader for the Phase 5 ocean surface.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "OceanSurface" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            Color color = new Color(0.05f, 0.30f, 0.43f, 1f);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, name, StringComparison.Ordinal))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            GameObject existing = FindRootObject(scene, name);
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

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing serialized string property '{propertyName}'.");
            property.stringValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing serialized int property '{propertyName}'.");
            property.intValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing serialized float property '{propertyName}'.");
            property.floatValue = value;
        }

        private static void SetObjectReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing serialized object property '{propertyName}'.");
            property.objectReferenceValue = value;
        }
    }
}
