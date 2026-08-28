using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase2PresentationBuilder
    {
        private const string ScenePath = Phase2WorldBuilder.Phase2ScenePath;
        private const string RootName = "Phase2Presentation";
        private const string SkyboxMaterialPath = "Assets/Materials/BTB_ProceduralSky.mat";
        private const string WhiteLineMaterialPath = "Assets/Materials/BTB_RoadLine_White.mat";
        private const string AmberLineMaterialPath = "Assets/Materials/BTB_RoadLine_Amber.mat";
        private const string SignDarkMaterialPath = "Assets/Materials/BTB_Sign_Dark.mat";
        private const string SignUrbanMaterialPath = "Assets/Materials/BTB_Sign_Urban.mat";
        private const string SignOffRoadMaterialPath = "Assets/Materials/BTB_Sign_OffRoad.mat";
        private const string SignForestMaterialPath = "Assets/Materials/BTB_Sign_Forest.mat";

        [MenuItem("Beyond The Beat/Phase 2/Build Authored Presentation")]
        public static void BuildPresentation()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Presentation build requires Phase 2 scene '{ScenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveExistingRoot(scene, RootName);
            EnsureFolder("Assets", "Materials");

            ConfigureAtmosphere(scene);
            TuneWorldMaterials();

            Material whiteLine = GetOrCreateMaterial(WhiteLineMaterialPath, new Color(0.88f, 0.92f, 0.96f));
            Material amberLine = GetOrCreateMaterial(AmberLineMaterialPath, new Color(1f, 0.58f, 0.12f));
            Material signDark = GetOrCreateMaterial(SignDarkMaterialPath, new Color(0.025f, 0.04f, 0.06f));
            Material signUrban = GetOrCreateMaterial(SignUrbanMaterialPath, new Color(0.10f, 0.66f, 0.82f));
            Material signOffRoad = GetOrCreateMaterial(SignOffRoadMaterialPath, new Color(0.95f, 0.48f, 0.10f));
            Material signForest = GetOrCreateMaterial(SignForestMaterialPath, new Color(0.14f, 0.62f, 0.30f));

            GameObject root = new GameObject(RootName);
            CreateMainRoadMarkings(root.transform, whiteLine, amberLine);
            CreateConnectorMarkings(root.transform, whiteLine);
            CreateRoadsideReflectors(root.transform, whiteLine, amberLine);
            CreateZoneSign(root.transform, "UrbanSign", "URBAN", new Vector3(-8.5f, 2.0f, -62f), Quaternion.Euler(0f, 18f, 0f), signDark, signUrban);
            CreateZoneSign(root.transform, "OffRoadSign", "OFF ROAD", new Vector3(27f, 2.0f, 38f), Quaternion.Euler(0f, 90f, 0f), signDark, signOffRoad);
            CreateZoneSign(root.transform, "ForestSign", "FOREST", new Vector3(100f, 2.0f, -7f), Quaternion.Euler(0f, 90f, 0f), signDark, signForest);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save authored presentation into '{ScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;

            Debug.Log(
                "[Beyond The Beat] Phase 2 authored presentation created: atmospheric lighting/fog, tuned biome palette, " +
                "road markings, reflectors, and world-space zone signage.");
        }

        [MenuItem("Beyond The Beat/Phase 2/Validate Authored Presentation")]
        public static void ValidatePresentation()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Presentation validation FAIL: scene missing at '{ScenePath}'.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject root = FindRootObject(validationScene, RootName);
                Transform markings = root != null ? root.transform.Find("RoadMarkings") : null;
                Transform reflectors = root != null ? root.transform.Find("RoadsideReflectors") : null;
                Transform signs = root != null ? root.transform.Find("ZoneSigns") : null;
                Light sun = validationScene.GetRootGameObjects()
                    .SelectMany(item => item.GetComponentsInChildren<Light>(true))
                    .FirstOrDefault(light => light.type == LightType.Directional);

                bool rootPass = root != null;
                bool atmospherePass = RenderSettings.fog && RenderSettings.skybox != null && sun != null && sun.shadows != LightShadows.None;
                bool markingsPass = markings != null && markings.childCount >= 20;
                bool reflectorsPass = reflectors != null && reflectors.childCount >= 12;
                bool signsPass = signs != null && signs.childCount == 3 &&
                                 signs.Cast<Transform>().All(sign => sign.GetComponentInChildren<TextMesh>(true) != null);
                bool materialPass =
                    ValidateMaterialColor("Assets/Materials/Prototype_Road.mat", new Color(0.055f, 0.065f, 0.08f), 0.08f) &&
                    ValidateMaterialColor("Assets/Materials/Phase2_ForestGround.mat", new Color(0.08f, 0.20f, 0.10f), 0.08f);

                bool allPass = rootPass && atmospherePass && markingsPass && reflectorsPass && signsPass && materialPass;
                string message =
                    "[Beyond The Beat] Phase 2 authored presentation validation\n" +
                    $"Presentation root: {PassFail(rootPass)}\n" +
                    $"Sky/fog/sun atmosphere: {PassFail(atmospherePass)}\n" +
                    $"Road lane/edge markings: {PassFail(markingsPass)}\n" +
                    $"Roadside reflectors: {PassFail(reflectorsPass)}\n" +
                    $"Urban/off-road/forest signage: {PassFail(signsPass)}\n" +
                    $"Tuned road/forest material palette: {PassFail(materialPass)}";

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

        private static void ConfigureAtmosphere(Scene scene)
        {
            Light sun = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Light>(true))
                .FirstOrDefault(light => light.type == LightType.Directional);

            if (sun != null)
            {
                sun.color = new Color(1f, 0.86f, 0.70f);
                sun.intensity = 1.18f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.82f;
                sun.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            }

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
                if (skybox == null)
                {
                    skybox = new Material(skyShader) { name = "BTB_ProceduralSky" };
                    AssetDatabase.CreateAsset(skybox, SkyboxMaterialPath);
                }

                SetColorIfPresent(skybox, "_SkyTint", new Color(0.30f, 0.48f, 0.68f));
                SetColorIfPresent(skybox, "_GroundColor", new Color(0.19f, 0.20f, 0.18f));
                SetFloatIfPresent(skybox, "_AtmosphereThickness", 0.82f);
                SetFloatIfPresent(skybox, "_Exposure", 1.18f);
                RenderSettings.skybox = skybox;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.36f, 0.48f, 0.60f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.27f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.12f, 0.11f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.36f, 0.45f, 0.48f);
            RenderSettings.fogStartDistance = 105f;
            RenderSettings.fogEndDistance = 270f;
        }

        private static void TuneWorldMaterials()
        {
            TuneMaterial("Assets/Materials/Prototype_Ground.mat", new Color(0.12f, 0.24f, 0.14f), 0.03f, 0f);
            TuneMaterial("Assets/Materials/Prototype_Road.mat", new Color(0.055f, 0.065f, 0.08f), 0.14f, 0f);
            TuneMaterial("Assets/Materials/Prototype_Obstacle.mat", new Color(0.86f, 0.31f, 0.08f), 0.18f, 0f);
            TuneMaterial("Assets/Materials/Prototype_Marker.mat", new Color(0.12f, 0.70f, 0.88f), 0.30f, 0f);
            TuneMaterial("Assets/Materials/Phase1_Urban.mat", new Color(0.20f, 0.27f, 0.36f), 0.22f, 0.02f);
            TuneMaterial("Assets/Materials/Phase1_OffRoad.mat", new Color(0.34f, 0.22f, 0.11f), 0.02f, 0f);
            TuneMaterial("Assets/Materials/Phase1_ZoneMarker.mat", new Color(1.0f, 0.58f, 0.10f), 0.25f, 0f);
            TuneMaterial("Assets/Materials/Phase2_ForestGround.mat", new Color(0.08f, 0.20f, 0.10f), 0.02f, 0f);
            TuneMaterial("Assets/Materials/Phase2_ForestTrunk.mat", new Color(0.22f, 0.11f, 0.045f), 0.06f, 0f);
            TuneMaterial("Assets/Materials/Phase2_ForestCanopy.mat", new Color(0.045f, 0.27f, 0.10f), 0.02f, 0f);
        }

        private static void CreateMainRoadMarkings(Transform parent, Material white, Material amber)
        {
            GameObject group = new GameObject("RoadMarkings");
            group.transform.SetParent(parent, false);

            for (int i = -7; i <= 7; i++)
            {
                float z = i * 10f;
                CreateBox($"CenterDash_{i + 7:00}", group.transform, new Vector3(0f, 0.285f, z), new Vector3(0.18f, 0.025f, 4.2f), white, false);
            }

            CreateBox("EdgeLine_L", group.transform, new Vector3(-5.45f, 0.282f, 0f), new Vector3(0.13f, 0.02f, 154f), amber, false);
            CreateBox("EdgeLine_R", group.transform, new Vector3(5.45f, 0.282f, 0f), new Vector3(0.13f, 0.02f, 154f), amber, false);
        }

        private static void CreateConnectorMarkings(Transform parent, Material white)
        {
            Transform group = parent.Find("RoadMarkings");
            if (group == null)
            {
                return;
            }

            for (int i = 0; i < 6; i++)
            {
                float x = 8f + i * 8f;
                CreateBox($"OffRoadConnectorDash_{i:00}", group, new Vector3(x, 0.285f, 45f), new Vector3(3.2f, 0.02f, 0.16f), white, false);
            }

            for (int i = 0; i < 4; i++)
            {
                float x = 82f + i * 5f;
                CreateBox($"ForestConnectorDash_{i:00}", group, new Vector3(x, 0.245f, 0f), new Vector3(2.2f, 0.02f, 0.16f), white, false);
            }
        }

        private static void CreateRoadsideReflectors(Transform parent, Material white, Material amber)
        {
            GameObject group = new GameObject("RoadsideReflectors");
            group.transform.SetParent(parent, false);

            for (int i = -6; i <= 6; i += 2)
            {
                float z = i * 10f;
                CreateReflector($"Reflector_L_{i + 6:00}", group.transform, new Vector3(-6.4f, 0.55f, z), white, amber);
                CreateReflector($"Reflector_R_{i + 6:00}", group.transform, new Vector3(6.4f, 0.55f, z), white, amber);
            }
        }

        private static void CreateReflector(string name, Transform parent, Vector3 position, Material bodyMaterial, Material capMaterial)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            CreateBox("Post", root.transform, position, new Vector3(0.12f, 0.65f, 0.12f), bodyMaterial, false);
            CreateBox("Reflector", root.transform, position + new Vector3(0f, 0.27f, 0f), new Vector3(0.16f, 0.14f, 0.16f), capMaterial, false);
        }

        private static void CreateZoneSign(
            Transform parent,
            string name,
            string label,
            Vector3 position,
            Quaternion rotation,
            Material darkMaterial,
            Material accentMaterial)
        {
            Transform signs = parent.Find("ZoneSigns");
            if (signs == null)
            {
                GameObject signsObject = new GameObject("ZoneSigns");
                signsObject.transform.SetParent(parent, false);
                signs = signsObject.transform;
            }

            GameObject root = new GameObject(name);
            root.transform.SetParent(signs, false);
            root.transform.position = position;
            root.transform.rotation = rotation;

            CreateBox("Post", root.transform, new Vector3(0f, 1.0f, 0f), new Vector3(0.12f, 2.0f, 0.12f), darkMaterial, false, local: true);
            CreateBox("Panel", root.transform, new Vector3(0f, 2.0f, 0f), new Vector3(2.9f, 0.85f, 0.12f), darkMaterial, false, local: true);
            CreateBox("Accent", root.transform, new Vector3(-1.34f, 2.0f, -0.08f), new Vector3(0.12f, 0.72f, 0.06f), accentMaterial, false, local: true);

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = new Vector3(0.12f, 2.0f, -0.09f);
            textObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = 0.09f;
            text.color = Color.white;
        }

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider,
            bool local = false)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.SetParent(parent, false);
            if (local)
            {
                item.transform.localPosition = position;
            }
            else
            {
                item.transform.position = position;
            }
            item.transform.localScale = scale;

            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!collider)
            {
                Collider existing = item.GetComponent<Collider>();
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                }
            }

            return item;
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.color = color;
                EditorUtility.SetDirty(material);
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible Lit shader was found for authored presentation materials.");
            }

            material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path),
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void TuneMaterial(string path, Color color, float smoothness, float metallic)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                return;
            }

            material.color = color;
            SetColorIfPresent(material, "_BaseColor", color);
            SetFloatIfPresent(material, "_Smoothness", smoothness);
            SetFloatIfPresent(material, "_Glossiness", smoothness);
            SetFloatIfPresent(material, "_Metallic", metallic);
            EditorUtility.SetDirty(material);
        }

        private static bool ValidateMaterialColor(string path, Color expected, float tolerance)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                return false;
            }

            Color actual = material.color;
            return Mathf.Abs(actual.r - expected.r) <= tolerance &&
                   Mathf.Abs(actual.g - expected.g) <= tolerance &&
                   Mathf.Abs(actual.b - expected.b) <= tolerance;
        }

        private static void SetColorIfPresent(Material material, string property, Color value)
        {
            if (material != null && material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material != null && material.HasProperty(property))
            {
                material.SetFloat(property, value);
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

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
