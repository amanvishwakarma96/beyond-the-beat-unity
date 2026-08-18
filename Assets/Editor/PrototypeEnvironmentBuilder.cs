using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class PrototypeEnvironmentBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Phase0_Prototype.unity";
        private const string MaterialsFolder = "Assets/Materials";
        private const string GroundMaterialPath = MaterialsFolder + "/Prototype_Ground.mat";
        private const string RoadMaterialPath = MaterialsFolder + "/Prototype_Road.mat";
        private const string ObstacleMaterialPath = MaterialsFolder + "/Prototype_Obstacle.mat";
        private const string MarkerMaterialPath = MaterialsFolder + "/Prototype_Marker.mat";

        private const string EnvironmentRootName = "PrototypeEnvironment";
        private const string GroundName = "Ground_200x200";
        private const string RoadName = "Road_TestStrip";
        private const string SpawnMarkerName = "VehicleSpawnMarker";

        [MenuItem("Beyond The Beat/Phase 0/Build Prototype Environment")]
        private static void BuildPrototypeEnvironment()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Scenes", "Prototype");

            Material groundMaterial = GetOrCreateMaterial(GroundMaterialPath, new Color(0.26f, 0.38f, 0.22f));
            Material roadMaterial = GetOrCreateMaterial(RoadMaterialPath, new Color(0.12f, 0.12f, 0.13f));
            Material obstacleMaterial = GetOrCreateMaterial(ObstacleMaterialPath, new Color(0.82f, 0.32f, 0.08f));
            Material markerMaterial = GetOrCreateMaterial(MarkerMaterialPath, new Color(0.12f, 0.48f, 0.86f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLighting();
            CreateReferenceCamera();

            GameObject root = new GameObject(EnvironmentRootName);

            CreateBox(
                GroundName,
                root.transform,
                Vector3.zero,
                new Vector3(200f, 0.2f, 200f),
                groundMaterial);

            CreateBox(
                RoadName,
                root.transform,
                new Vector3(0f, 0.13f, 0f),
                new Vector3(12f, 0.08f, 160f),
                roadMaterial);

            CreateRoadEdgeMarkers(root.transform, markerMaterial);
            CreateSlalomSection(root.transform, obstacleMaterial);
            CreateBrakingZone(root.transform, obstacleMaterial, markerMaterial);
            CreateCollisionTestObstacles(root.transform, obstacleMaterial);
            CreateSpawnMarker(root.transform, markerMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Beyond The Beat] Phase 0 prototype environment created at {ScenePath}.");
        }

        [MenuItem("Beyond The Beat/Phase 0/Validate Prototype Environment")]
        private static void ValidatePrototypeEnvironment()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Prototype validation FAIL: scene not found at {ScenePath}.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject root = validationScene.GetRootGameObjects()
                    .FirstOrDefault(go => go.name == EnvironmentRootName);

                if (root == null)
                {
                    Debug.LogError("[Beyond The Beat] Prototype validation FAIL: environment root is missing.");
                    return;
                }

                Transform ground = root.transform.Find(GroundName);
                Transform road = root.transform.Find(RoadName);
                Transform spawnMarker = root.transform.Find(SpawnMarkerName);
                Transform slalom = root.transform.Find("SlalomSection");
                Transform brakingZone = root.transform.Find("BrakingZone");
                Transform collisionTests = root.transform.Find("CollisionTestObstacles");

                bool groundPass = ground != null && Approximately(ground.localScale, new Vector3(200f, 0.2f, 200f));
                bool roadPass = road != null && Approximately(road.localScale, new Vector3(12f, 0.08f, 160f));
                bool spawnPass = spawnMarker != null;
                bool slalomPass = slalom != null && slalom.childCount >= 6;
                bool brakingPass = brakingZone != null && brakingZone.childCount >= 3;
                bool collisionPass = collisionTests != null && collisionTests.childCount >= 3;
                bool buildSettingsPass = EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath && scene.enabled);

                bool allPass = groundPass && roadPass && spawnPass && slalomPass && brakingPass && collisionPass && buildSettingsPass;

                string message =
                    "[Beyond The Beat] Phase 0 prototype environment validation\n" +
                    $"Scene asset: PASS ({ScenePath})\n" +
                    $"200x200 ground: {PassFail(groundPass)}\n" +
                    $"Road test strip: {PassFail(roadPass)}\n" +
                    $"Vehicle spawn marker: {PassFail(spawnPass)}\n" +
                    $"Slalom obstacles: {PassFail(slalomPass)}\n" +
                    $"Braking zone: {PassFail(brakingPass)}\n" +
                    $"Collision-test obstacles: {PassFail(collisionPass)}\n" +
                    $"Enabled in Build Settings: {PassFail(buildSettingsPass)}";

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

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f);
        }

        private static void CreateReferenceCamera()
        {
            GameObject cameraObject = new GameObject("PrototypeReferenceCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.transform.position = new Vector3(22f, 24f, -34f);
            camera.transform.rotation = Quaternion.Euler(25f, -28f, 0f);
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;

            // Temporary environment-inspection camera only. Issue #4 replaces gameplay camera behavior.
        }

        private static void CreateRoadEdgeMarkers(Transform parent, Material material)
        {
            GameObject group = new GameObject("RoadEdgeMarkers");
            group.transform.SetParent(parent, false);

            for (int i = -7; i <= 7; i++)
            {
                float z = i * 10f;
                CreateBox($"Marker_L_{i + 7:00}", group.transform, new Vector3(-6.15f, 0.2f, z), new Vector3(0.15f, 0.2f, 4f), material);
                CreateBox($"Marker_R_{i + 7:00}", group.transform, new Vector3(6.15f, 0.2f, z), new Vector3(0.15f, 0.2f, 4f), material);
            }
        }

        private static void CreateSlalomSection(Transform parent, Material material)
        {
            GameObject group = new GameObject("SlalomSection");
            group.transform.SetParent(parent, false);

            for (int i = 0; i < 6; i++)
            {
                float z = -48f + i * 9f;
                float x = i % 2 == 0 ? -2.8f : 2.8f;
                CreateCylinder($"Slalom_{i + 1:00}", group.transform, new Vector3(x, 0.65f, z), new Vector3(0.55f, 0.65f, 0.55f), material);
            }
        }

        private static void CreateBrakingZone(Transform parent, Material obstacleMaterial, Material markerMaterial)
        {
            GameObject group = new GameObject("BrakingZone");
            group.transform.SetParent(parent, false);

            CreateBox("BrakeStart", group.transform, new Vector3(0f, 0.2f, 22f), new Vector3(10f, 0.12f, 0.6f), markerMaterial);
            CreateBox("BrakeTarget", group.transform, new Vector3(0f, 0.2f, 43f), new Vector3(10f, 0.12f, 0.6f), markerMaterial);
            CreateBox("SafetyBarrier", group.transform, new Vector3(0f, 0.75f, 51f), new Vector3(8f, 1.5f, 0.8f), obstacleMaterial);
        }

        private static void CreateCollisionTestObstacles(Transform parent, Material material)
        {
            GameObject group = new GameObject("CollisionTestObstacles");
            group.transform.SetParent(parent, false);

            CreateBox("LowBarrier", group.transform, new Vector3(17f, 0.4f, -12f), new Vector3(7f, 0.8f, 0.8f), material);
            CreateBox("WideBlock", group.transform, new Vector3(20f, 1.0f, 5f), new Vector3(5f, 2f, 5f), material);
            CreateBox("NarrowBlock", group.transform, new Vector3(-18f, 1.5f, 2f), new Vector3(2f, 3f, 2f), material);
        }

        private static void CreateSpawnMarker(Transform parent, Material material)
        {
            GameObject marker = CreateBox(
                SpawnMarkerName,
                parent,
                new Vector3(0f, 0.22f, -68f),
                new Vector3(3.5f, 0.12f, 7f),
                material);

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = scale;
            AssignMaterial(gameObject, material);
            return gameObject;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = scale;
            AssignMaterial(gameObject, material);
            return gameObject;
        }

        private static void AssignMaterial(GameObject gameObject, Material material)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible Lit shader was found for prototype materials.");
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
