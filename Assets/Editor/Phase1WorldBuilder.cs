using System;
using System.Linq;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase1WorldBuilder
    {
        public const string Phase0ScenePath = "Assets/Scenes/Prototype/Phase0_Prototype.unity";
        public const string Phase1ScenePath = "Assets/Scenes/Phase1/Phase1_MVP.unity";

        private const string WorldRootName = "Phase1World";
        private const string UrbanRootName = "UrbanZone";
        private const string OffRoadRootName = "OffRoadZone";
        private const string UrbanContextName = "UrbanZoneContext";
        private const string OffRoadContextName = "OffRoadZoneContext";

        private const string UrbanMaterialPath = "Assets/Materials/Phase1_Urban.mat";
        private const string OffRoadMaterialPath = "Assets/Materials/Phase1_OffRoad.mat";
        private const string ZoneMarkerMaterialPath = "Assets/Materials/Phase1_ZoneMarker.mat";
        private const string RoadMaterialPath = "Assets/Materials/Prototype_Road.mat";

        [MenuItem("Beyond The Beat/Phase 1/Build MVP World Foundation")]
        public static void BuildMvpWorldFoundation()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset phase0SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase0ScenePath);
            if (phase0SceneAsset == null)
            {
                Debug.LogError(
                    $"[Beyond The Beat] Phase 0 scene is required at '{Phase0ScenePath}'. " +
                    "Generate the complete Phase 0 prototype before creating the Phase 1 world.");
                return;
            }

            EnsureFolder("Assets/Scenes", "Phase1");
            EnsureFolder("Assets", "Materials");

            Scene phase0Scene = EditorSceneManager.OpenScene(Phase0ScenePath, OpenSceneMode.Single);
            if (phase0Scene.isDirty && !EditorSceneManager.SaveScene(phase0Scene))
            {
                Debug.LogError("[Beyond The Beat] Unable to save the Phase 0 source scene before creating Phase 1.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase1ScenePath) != null &&
                !AssetDatabase.DeleteAsset(Phase1ScenePath))
            {
                Debug.LogError($"[Beyond The Beat] Unable to replace existing Phase 1 scene at '{Phase1ScenePath}'.");
                return;
            }

            if (!AssetDatabase.CopyAsset(Phase0ScenePath, Phase1ScenePath))
            {
                Debug.LogError($"[Beyond The Beat] Unable to copy Phase 0 scene to '{Phase1ScenePath}'.");
                return;
            }

            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.OpenScene(Phase1ScenePath, OpenSceneMode.Single);

            RemoveExistingRoot(scene, WorldRootName);

            Material urbanMaterial = GetOrCreateMaterial(UrbanMaterialPath, new Color(0.28f, 0.34f, 0.42f));
            Material offRoadMaterial = GetOrCreateMaterial(OffRoadMaterialPath, new Color(0.42f, 0.31f, 0.18f));
            Material zoneMarkerMaterial = GetOrCreateMaterial(ZoneMarkerMaterialPath, new Color(0.86f, 0.67f, 0.12f));
            Material roadMaterial = AssetDatabase.LoadAssetAtPath<Material>(RoadMaterialPath) ?? urbanMaterial;

            GameObject worldRoot = new GameObject(WorldRootName);
            GameObject urbanRoot = new GameObject(UrbanRootName);
            urbanRoot.transform.SetParent(worldRoot.transform, false);
            GameObject offRoadRoot = new GameObject(OffRoadRootName);
            offRoadRoot.transform.SetParent(worldRoot.transform, false);

            CreateUrbanArea(urbanRoot.transform, urbanMaterial, zoneMarkerMaterial);
            CreateOffRoadArea(offRoadRoot.transform, offRoadMaterial, zoneMarkerMaterial);
            CreateConnectorRoad(worldRoot.transform, roadMaterial);

            CreateZoneContext(
                UrbanContextName,
                urbanRoot.transform,
                "urban-road",
                WorldZoneType.Urban,
                new Vector3(0f, 2f, 0f),
                new Vector3(40f, 4f, 180f));

            CreateZoneContext(
                OffRoadContextName,
                offRoadRoot.transform,
                "off-road",
                WorldZoneType.OffRoad,
                new Vector3(54f, 2f, 0f),
                new Vector3(64f, 4f, 150f));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save generated Phase 1 scene at '{Phase1ScenePath}'.");
                return;
            }

            AddSceneToBuildSettings(Phase1ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = worldRoot;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Phase 1 MVP world foundation created. " +
                "The Phase 0 drive/camera/mobile/parking loop is preserved in a separate Phase 1 scene with Urban and Off-road contexts.");
        }

        [MenuItem("Beyond The Beat/Phase 1/Validate MVP World Foundation")]
        public static void ValidateMvpWorldFoundation()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase1ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Phase 1 world validation FAIL: scene not found at '{Phase1ScenePath}'.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != Phase1ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(Phase1ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject worldRoot = FindRootObject(validationScene, WorldRootName);
                GameObject vehicle = FindRootObject(validationScene, "PrototypeVehicle");
                GameObject gameplayCamera = FindRootObject(validationScene, "GameplayCamera");
                GameObject mobileCanvas = FindRootObject(validationScene, "MobileDrivingCanvas");
                GameObject parkingRoot = FindRootObject(validationScene, "ParkingPrototype");

                Transform urbanRoot = worldRoot != null ? worldRoot.transform.Find(UrbanRootName) : null;
                Transform offRoadRoot = worldRoot != null ? worldRoot.transform.Find(OffRoadRootName) : null;
                Transform urbanContextTransform = urbanRoot != null ? urbanRoot.Find(UrbanContextName) : null;
                Transform offRoadContextTransform = offRoadRoot != null ? offRoadRoot.Find(OffRoadContextName) : null;

                ZoneContext urbanContext = urbanContextTransform != null
                    ? urbanContextTransform.GetComponent<ZoneContext>()
                    : null;
                ZoneContext offRoadContext = offRoadContextTransform != null
                    ? offRoadContextTransform.GetComponent<ZoneContext>()
                    : null;

                bool inheritedLoopPass =
                    vehicle != null &&
                    gameplayCamera != null &&
                    mobileCanvas != null &&
                    parkingRoot != null;

                bool worldRootPass = worldRoot != null;
                bool urbanVisualPass = urbanRoot != null && urbanRoot.Find("UrbanBuildings")?.childCount >= 6;
                bool offRoadVisualPass = offRoadRoot != null && offRoadRoot.Find("OffRoadTerrain") != null;
                bool urbanZonePass = ValidateZoneContext(urbanContext, "urban-road", WorldZoneType.Urban, new Vector3(40f, 4f, 180f));
                bool offRoadZonePass = ValidateZoneContext(offRoadContext, "off-road", WorldZoneType.OffRoad, new Vector3(64f, 4f, 150f));
                bool buildSettingsPass = EditorBuildSettings.scenes.Any(item => item.path == Phase1ScenePath && item.enabled);

                bool allPass =
                    inheritedLoopPass &&
                    worldRootPass &&
                    urbanVisualPass &&
                    offRoadVisualPass &&
                    urbanZonePass &&
                    offRoadZonePass &&
                    buildSettingsPass;

                string message =
                    "[Beyond The Beat] Phase 1 MVP world foundation validation\n" +
                    $"Inherited drive/camera/mobile/parking loop: {PassFail(inheritedLoopPass)}\n" +
                    $"Phase1World root: {PassFail(worldRootPass)}\n" +
                    $"Urban area/buildings: {PassFail(urbanVisualPass)}\n" +
                    $"Off-road area: {PassFail(offRoadVisualPass)}\n" +
                    $"Urban ZoneContext: {PassFail(urbanZonePass)}\n" +
                    $"Off-road ZoneContext: {PassFail(offRoadZonePass)}\n" +
                    $"Phase 1 scene enabled in Build Settings: {PassFail(buildSettingsPass)}";

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

        private static void CreateUrbanArea(Transform parent, Material buildingMaterial, Material markerMaterial)
        {
            GameObject buildings = new GameObject("UrbanBuildings");
            buildings.transform.SetParent(parent, false);

            float[] zPositions = { -55f, -25f, 5f, 35f, 65f };
            for (int i = 0; i < zPositions.Length; i++)
            {
                float heightLeft = 7f + (i % 3) * 2f;
                float heightRight = 9f + ((i + 1) % 3) * 2f;

                CreateBox(
                    $"Building_L_{i + 1:00}",
                    buildings.transform,
                    new Vector3(-15f, 0.2f + heightLeft * 0.5f, zPositions[i]),
                    new Vector3(7f, heightLeft, 11f),
                    Quaternion.identity,
                    buildingMaterial);

                CreateBox(
                    $"Building_R_{i + 1:00}",
                    buildings.transform,
                    new Vector3(15f, 0.2f + heightRight * 0.5f, zPositions[i]),
                    new Vector3(7f, heightRight, 11f),
                    Quaternion.identity,
                    buildingMaterial);
            }

            CreateBox(
                "UrbanEntryMarker",
                parent,
                new Vector3(0f, 0.24f, -78f),
                new Vector3(11f, 0.12f, 0.8f),
                Quaternion.identity,
                markerMaterial);
        }

        private static void CreateOffRoadArea(Transform parent, Material terrainMaterial, Material markerMaterial)
        {
            CreateBox(
                "OffRoadTerrain",
                parent,
                new Vector3(54f, 0.16f, 0f),
                new Vector3(64f, 0.08f, 150f),
                Quaternion.identity,
                terrainMaterial);

            GameObject bumps = new GameObject("OffRoadBumps");
            bumps.transform.SetParent(parent, false);

            CreateBox("Bump_01", bumps.transform, new Vector3(42f, 0.45f, -45f), new Vector3(9f, 0.7f, 12f), Quaternion.Euler(0f, 0f, 5f), terrainMaterial);
            CreateBox("Bump_02", bumps.transform, new Vector3(62f, 0.55f, -12f), new Vector3(11f, 0.9f, 14f), Quaternion.Euler(0f, 0f, -6f), terrainMaterial);
            CreateBox("Bump_03", bumps.transform, new Vector3(46f, 0.4f, 25f), new Vector3(10f, 0.6f, 10f), Quaternion.Euler(4f, 0f, 0f), terrainMaterial);
            CreateBox("Bump_04", bumps.transform, new Vector3(67f, 0.5f, 52f), new Vector3(12f, 0.8f, 13f), Quaternion.Euler(-5f, 0f, 0f), terrainMaterial);

            CreateBox(
                "OffRoadEntryMarker",
                parent,
                new Vector3(25f, 0.24f, 45f),
                new Vector3(0.8f, 0.12f, 8f),
                Quaternion.identity,
                markerMaterial);
        }

        private static void CreateConnectorRoad(Transform parent, Material roadMaterial)
        {
            CreateBox(
                "UrbanToOffRoadConnector",
                parent,
                new Vector3(27f, 0.22f, 45f),
                new Vector3(54f, 0.08f, 8f),
                Quaternion.identity,
                roadMaterial);
        }

        private static void CreateZoneContext(
            string name,
            Transform parent,
            string zoneId,
            WorldZoneType zoneType,
            Vector3 position,
            Vector3 size)
        {
            GameObject zoneObject = new GameObject(name);
            zoneObject.transform.SetParent(parent, false);
            zoneObject.transform.position = position;

            BoxCollider trigger = zoneObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = size;

            ZoneContext zoneContext = zoneObject.AddComponent<ZoneContext>();
            SerializedObject serialized = new SerializedObject(zoneContext);
            SerializedProperty idProperty = serialized.FindProperty("zoneId");
            SerializedProperty typeProperty = serialized.FindProperty("zoneType");

            if (idProperty == null || typeProperty == null)
            {
                throw new InvalidOperationException("ZoneContext serialized fields could not be resolved.");
            }

            idProperty.stringValue = zoneId;
            typeProperty.enumValueIndex = (int)zoneType;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool ValidateZoneContext(
            ZoneContext zoneContext,
            string expectedId,
            WorldZoneType expectedType,
            Vector3 expectedSize)
        {
            if (zoneContext == null ||
                zoneContext.ZoneId != expectedId ||
                zoneContext.ZoneType != expectedType ||
                !zoneContext.TryGetComponent(out BoxCollider trigger) ||
                !trigger.isTrigger)
            {
                return false;
            }

            return Approximately(trigger.size, expectedSize);
        }

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.rotation = rotation;
            gameObject.transform.localScale = scale;

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return gameObject;
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

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible Lit shader was found for the Phase 1 world materials.");
            }

            Material material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path),
                color = color
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
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

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            if (existingScenes.Any(scene => scene.path == scenePath))
            {
                EditorBuildSettings.scenes = existingScenes
                    .Select(scene => scene.path == scenePath ? new EditorBuildSettingsScene(scenePath, true) : scene)
                    .ToArray();
                return;
            }

            EditorBuildSettings.scenes = existingScenes
                .Concat(new[] { new EditorBuildSettingsScene(scenePath, true) })
                .ToArray();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string fullPath = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
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
