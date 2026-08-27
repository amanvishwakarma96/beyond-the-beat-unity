using System;
using System.Linq;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase2WorldBuilder
    {
        public const string Phase2ScenePath = "Assets/Scenes/Phase2/Phase2_Forest.unity";

        private const string Phase2RootName = "Phase2ForestBiome";
        private const string ForestZoneRootName = "ForestZone";
        private const string ForestContextName = "ForestZoneContext";
        private const string ForestTreesName = "ForestTrees";
        private const string ForestGroundName = "ForestGround";
        private const string ForestTrailName = "ForestTrail";
        private const string ForestConnectorName = "OffRoadToForestConnector";

        private const string ForestZoneId = "forest";
        private const string ForestGroundMaterialPath = "Assets/Materials/Phase2_ForestGround.mat";
        private const string ForestTrunkMaterialPath = "Assets/Materials/Phase2_ForestTrunk.mat";
        private const string ForestCanopyMaterialPath = "Assets/Materials/Phase2_ForestCanopy.mat";
        private const string RoadMaterialPath = "Assets/Materials/Prototype_Road.mat";

        private static readonly Vector3 ForestCenter = new Vector3(130f, 0f, 0f);
        private static readonly Vector3 ForestZoneSize = new Vector3(70f, 4f, 140f);

        [MenuItem("Beyond The Beat/Phase 2/Build Forest Biome Foundation")]
        public static void BuildForestBiomeFoundation()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset phase1SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase1WorldBuilder.Phase1ScenePath);
            if (phase1SceneAsset == null)
            {
                Debug.LogError(
                    $"[Beyond The Beat] Phase 2 forest build requires the integrated Phase 1 scene at '{Phase1WorldBuilder.Phase1ScenePath}'.");
                return;
            }

            EnsureFolder("Assets/Scenes", "Phase2");
            EnsureFolder("Assets", "Materials");

            Scene phase1Scene = EditorSceneManager.OpenScene(Phase1WorldBuilder.Phase1ScenePath, OpenSceneMode.Single);
            if (phase1Scene.isDirty && !EditorSceneManager.SaveScene(phase1Scene))
            {
                Debug.LogError("[Beyond The Beat] Unable to save Phase 1 source scene before creating Phase 2.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase2ScenePath) != null &&
                !AssetDatabase.DeleteAsset(Phase2ScenePath))
            {
                Debug.LogError($"[Beyond The Beat] Unable to replace existing Phase 2 scene at '{Phase2ScenePath}'.");
                return;
            }

            if (!AssetDatabase.CopyAsset(Phase1WorldBuilder.Phase1ScenePath, Phase2ScenePath))
            {
                Debug.LogError($"[Beyond The Beat] Unable to copy Phase 1 scene to '{Phase2ScenePath}'.");
                return;
            }

            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.OpenScene(Phase2ScenePath, OpenSceneMode.Single);
            RemoveExistingRoot(scene, Phase2RootName);

            Material groundMaterial = GetOrCreateMaterial(ForestGroundMaterialPath, new Color(0.16f, 0.31f, 0.15f));
            Material trunkMaterial = GetOrCreateMaterial(ForestTrunkMaterialPath, new Color(0.28f, 0.16f, 0.08f));
            Material canopyMaterial = GetOrCreateMaterial(ForestCanopyMaterialPath, new Color(0.10f, 0.38f, 0.16f));
            Material roadMaterial = AssetDatabase.LoadAssetAtPath<Material>(RoadMaterialPath) ?? groundMaterial;

            GameObject phase2Root = new GameObject(Phase2RootName);
            GameObject forestRoot = new GameObject(ForestZoneRootName);
            forestRoot.transform.SetParent(phase2Root.transform, false);

            CreateForestGround(forestRoot.transform, groundMaterial, roadMaterial);
            CreateForestTrees(forestRoot.transform, trunkMaterial, canopyMaterial);
            CreateForestConnector(phase2Root.transform, roadMaterial);
            CreateForestContext(forestRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save generated Phase 2 forest scene at '{Phase2ScenePath}'.");
                return;
            }

            AddSceneToBuildSettings(Phase2ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = phase2Root;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Phase 2 forest biome foundation created. " +
                "The integrated Phase 1 MVP is preserved and a drivable forest ZoneContext is available for later survival systems.");
        }

        [MenuItem("Beyond The Beat/Phase 2/Validate Forest Biome Foundation")]
        public static void ValidateForestBiomeFoundation()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase2ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Phase 2 forest validation FAIL: scene not found at '{Phase2ScenePath}'.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != Phase2ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(Phase2ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject phase1World = FindRootObject(validationScene, "Phase1World");
                GameObject missionRoot = FindRootObject(validationScene, "Phase1MissionSystem");
                GameObject persistenceRoot = FindRootObject(validationScene, "Phase1Persistence");
                GameObject mobileCanvas = FindRootObject(validationScene, "MobileDrivingCanvas");
                GameObject parkingRoot = FindRootObject(validationScene, "ParkingPrototype");
                GameObject phase2Root = FindRootObject(validationScene, Phase2RootName);

                Transform forestRoot = phase2Root != null ? phase2Root.transform.Find(ForestZoneRootName) : null;
                Transform forestContextTransform = forestRoot != null ? forestRoot.Find(ForestContextName) : null;
                ZoneContext forestContext = forestContextTransform != null
                    ? forestContextTransform.GetComponent<ZoneContext>()
                    : null;

                bool inheritedPhase1Pass =
                    phase1World != null &&
                    missionRoot != null &&
                    persistenceRoot != null &&
                    mobileCanvas != null &&
                    parkingRoot != null &&
                    mobileCanvas.transform.Find("Phase1MissionHUD") != null;

                bool forestRootPass = phase2Root != null && forestRoot != null;
                bool forestGroundPass = forestRoot != null && forestRoot.Find(ForestGroundName) != null;
                bool forestTrailPass = forestRoot != null && forestRoot.Find(ForestTrailName) != null;
                bool forestTreesPass = forestRoot != null &&
                                       forestRoot.Find(ForestTreesName) != null &&
                                       forestRoot.Find(ForestTreesName).childCount >= 12;
                bool connectorPass = phase2Root != null && phase2Root.transform.Find(ForestConnectorName) != null;
                bool forestZonePass = ValidateForestZone(forestContext);
                bool uniqueForestZonePass = validationScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<ZoneContext>(true))
                    .Count(zone => string.Equals(zone.ZoneId, ForestZoneId, StringComparison.Ordinal)) == 1;
                bool buildSettingsPass = EditorBuildSettings.scenes.Any(item => item.path == Phase2ScenePath && item.enabled);

                bool allPass =
                    inheritedPhase1Pass &&
                    forestRootPass &&
                    forestGroundPass &&
                    forestTrailPass &&
                    forestTreesPass &&
                    connectorPass &&
                    forestZonePass &&
                    uniqueForestZonePass &&
                    buildSettingsPass;

                string message =
                    "[Beyond The Beat] Phase 2 forest biome foundation validation\n" +
                    $"Inherited Phase 1 world/mission/save/HUD/parking: {PassFail(inheritedPhase1Pass)}\n" +
                    $"Phase2ForestBiome/ForestZone roots: {PassFail(forestRootPass)}\n" +
                    $"Drivable forest ground: {PassFail(forestGroundPass)}\n" +
                    $"Forest trail visual: {PassFail(forestTrailPass)}\n" +
                    $"Deterministic tree cluster: {PassFail(forestTreesPass)}\n" +
                    $"Off-road to forest connector: {PassFail(connectorPass)}\n" +
                    $"Forest ZoneContext id/type/trigger: {PassFail(forestZonePass)}\n" +
                    $"Unique forest zone id: {PassFail(uniqueForestZonePass)}\n" +
                    $"Phase 2 scene enabled in Build Settings: {PassFail(buildSettingsPass)}";

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

        private static void CreateForestGround(Transform parent, Material groundMaterial, Material roadMaterial)
        {
            CreateBox(
                ForestGroundName,
                parent,
                new Vector3(ForestCenter.x, 0.16f, ForestCenter.z),
                new Vector3(ForestZoneSize.x, 0.08f, ForestZoneSize.z),
                groundMaterial,
                true);

            CreateBox(
                ForestTrailName,
                parent,
                new Vector3(ForestCenter.x, 0.23f, ForestCenter.z),
                new Vector3(9f, 0.03f, 140f),
                roadMaterial,
                false);

            CreateBox(
                "ForestEntryMarker",
                parent,
                new Vector3(98.5f, 0.24f, 0f),
                new Vector3(0.8f, 0.12f, 11f),
                roadMaterial,
                false);
        }

        private static void CreateForestConnector(Transform parent, Material roadMaterial)
        {
            CreateBox(
                ForestConnectorName,
                parent,
                new Vector3(90.5f, 0.16f, 0f),
                new Vector3(14f, 0.08f, 9f),
                roadMaterial,
                true);
        }

        private static void CreateForestTrees(Transform parent, Material trunkMaterial, Material canopyMaterial)
        {
            GameObject trees = new GameObject(ForestTreesName);
            trees.transform.SetParent(parent, false);

            Vector3[] positions =
            {
                new Vector3(106f, 0f, -55f), new Vector3(117f, 0f, -42f),
                new Vector3(144f, 0f, -56f), new Vector3(155f, 0f, -38f),
                new Vector3(107f, 0f, -20f), new Vector3(118f, 0f, -8f),
                new Vector3(143f, 0f, -18f), new Vector3(155f, 0f, -4f),
                new Vector3(106f, 0f, 16f), new Vector3(117f, 0f, 30f),
                new Vector3(144f, 0f, 15f), new Vector3(155f, 0f, 34f),
                new Vector3(106f, 0f, 51f), new Vector3(119f, 0f, 60f),
                new Vector3(143f, 0f, 49f), new Vector3(155f, 0f, 61f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                float trunkHeight = 4.5f + (i % 3) * 0.45f;
                float canopySize = 3.8f + (i % 2) * 0.45f;
                CreateTree(trees.transform, i + 1, positions[i], trunkHeight, canopySize, trunkMaterial, canopyMaterial);
            }
        }

        private static void CreateTree(
            Transform parent,
            int index,
            Vector3 position,
            float trunkHeight,
            float canopySize,
            Material trunkMaterial,
            Material canopyMaterial)
        {
            GameObject tree = new GameObject($"Tree_{index:00}");
            tree.transform.SetParent(parent, false);

            CreateBox(
                "Trunk",
                tree.transform,
                new Vector3(position.x, 0.2f + trunkHeight * 0.5f, position.z),
                new Vector3(0.85f, trunkHeight, 0.85f),
                trunkMaterial,
                true);

            CreateBox(
                "Canopy",
                tree.transform,
                new Vector3(position.x, trunkHeight + canopySize * 0.4f, position.z),
                new Vector3(canopySize, canopySize * 0.8f, canopySize),
                canopyMaterial,
                false);
        }

        private static void CreateForestContext(Transform parent)
        {
            GameObject contextObject = new GameObject(ForestContextName);
            contextObject.transform.SetParent(parent, false);
            contextObject.transform.position = new Vector3(ForestCenter.x, 2f, ForestCenter.z);

            BoxCollider trigger = contextObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = ForestZoneSize;

            ZoneContext zone = contextObject.AddComponent<ZoneContext>();
            SerializedObject serialized = new SerializedObject(zone);
            SerializedProperty zoneId = serialized.FindProperty("zoneId");
            SerializedProperty zoneType = serialized.FindProperty("zoneType");
            if (zoneId == null || zoneType == null)
            {
                throw new InvalidOperationException("ZoneContext serialized fields could not be resolved for Phase 2 forest setup.");
            }

            zoneId.stringValue = ForestZoneId;
            zoneType.enumValueIndex = (int)WorldZoneType.Forest;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool ValidateForestZone(ZoneContext zone)
        {
            return zone != null &&
                   zone.ZoneId == ForestZoneId &&
                   zone.ZoneType == WorldZoneType.Forest &&
                   zone.TryGetComponent(out BoxCollider trigger) &&
                   trigger.isTrigger &&
                   Approximately(trigger.size, ForestZoneSize) &&
                   Approximately(zone.transform.position, new Vector3(ForestCenter.x, 2f, ForestCenter.z));
        }

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool keepCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;

            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!keepCollider)
            {
                Collider collider = box.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            return box;
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Color");
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible shader was found for Phase 2 forest materials.");
            }

            Material material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path),
                color = color
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            if (current.Any(item => item.path == scenePath))
            {
                EditorBuildSettings.scenes = current
                    .Select(item => item.path == scenePath ? new EditorBuildSettingsScene(scenePath, true) : item)
                    .ToArray();
                return;
            }

            EditorBuildSettings.scenes = current
                .Concat(new[] { new EditorBuildSettingsScene(scenePath, true) })
                .ToArray();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void RemoveExistingRoot(Scene scene, string name)
        {
            GameObject existing = FindRootObject(scene, name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static bool Approximately(Vector3 actual, Vector3 expected)
        {
            const float tolerance = 0.01f;
            return Mathf.Abs(actual.x - expected.x) <= tolerance &&
                   Mathf.Abs(actual.y - expected.y) <= tolerance &&
                   Mathf.Abs(actual.z - expected.z) <= tolerance;
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
