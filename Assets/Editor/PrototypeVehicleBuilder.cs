using System;
using System.Linq;
using BeyondTheBeat.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class PrototypeVehicleBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Phase0_Prototype.unity";
        private const string PrefabPath = "Assets/Prefabs/Vehicles/PrototypeVehicle.prefab";
        private const string VehicleMaterialPath = "Assets/Materials/Prototype_Vehicle.mat";
        private const string WheelMaterialPath = "Assets/Materials/Prototype_Wheel.mat";
        private const string GlassMaterialPath = "Assets/Materials/Prototype_VehicleGlass.mat";
        private const string TrimMaterialPath = "Assets/Materials/Prototype_VehicleTrim.mat";
        private const string RimMaterialPath = "Assets/Materials/Prototype_VehicleRim.mat";
        private const string HeadlightMaterialPath = "Assets/Materials/Prototype_Headlight.mat";
        private const string TailLightMaterialPath = "Assets/Materials/Prototype_TailLight.mat";
        private const string VehicleName = "PrototypeVehicle";
        private const string SpawnMarkerName = "VehicleSpawnMarker";

        [MenuItem("Beyond The Beat/Phase 0/Build Prototype Vehicle")]
        private static void BuildPrototypeVehicle()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError(
                    $"[Beyond The Beat] Prototype scene not found at {ScenePath}. " +
                    "Run 'Beyond The Beat > Phase 0 > Build Prototype Environment' first.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform spawnMarker = FindTransformByName(scene, SpawnMarkerName);
            if (spawnMarker == null)
            {
                Debug.LogError($"[Beyond The Beat] {SpawnMarkerName} is missing from the prototype scene.");
                return;
            }

            GameObject existingVehicle = FindRootObject(scene, VehicleName);
            if (existingVehicle != null)
            {
                UnityEngine.Object.DestroyImmediate(existingVehicle);
            }

            Material bodyMaterial = GetOrCreateMaterial(VehicleMaterialPath, new Color(0.035f, 0.31f, 0.48f), 0.42f, 0.08f);
            Material wheelMaterial = GetOrCreateMaterial(WheelMaterialPath, new Color(0.025f, 0.028f, 0.034f), 0.14f, 0f);
            Material glassMaterial = GetOrCreateMaterial(GlassMaterialPath, new Color(0.025f, 0.09f, 0.13f), 0.72f, 0.02f);
            Material trimMaterial = GetOrCreateMaterial(TrimMaterialPath, new Color(0.028f, 0.035f, 0.045f), 0.32f, 0.12f);
            Material rimMaterial = GetOrCreateMaterial(RimMaterialPath, new Color(0.48f, 0.55f, 0.62f), 0.62f, 0.52f);
            Material headlightMaterial = GetOrCreateMaterial(HeadlightMaterialPath, new Color(0.86f, 0.94f, 1f), 0.78f, 0.08f);
            Material tailLightMaterial = GetOrCreateMaterial(TailLightMaterialPath, new Color(0.88f, 0.06f, 0.045f), 0.50f, 0.04f);

            GameObject vehicle = new GameObject(VehicleName);
            vehicle.transform.position = spawnMarker.position + new Vector3(0f, 0.35f, 0f);
            vehicle.transform.rotation = spawnMarker.rotation;

            Rigidbody body = vehicle.AddComponent<Rigidbody>();
            body.mass = 1250f;

            BoxCollider chassisCollider = vehicle.AddComponent<BoxCollider>();
            chassisCollider.center = new Vector3(0f, 0.45f, 0f);
            chassisCollider.size = new Vector3(1.8f, 0.65f, 3.6f);

            CreateBodyVisual(
                vehicle.transform,
                bodyMaterial,
                glassMaterial,
                trimMaterial,
                headlightMaterial,
                tailLightMaterial);

            WheelAssembly frontLeft = CreateWheel(vehicle.transform, "FrontLeft", new Vector3(-0.82f, 0f, 1.2f), wheelMaterial, rimMaterial);
            WheelAssembly frontRight = CreateWheel(vehicle.transform, "FrontRight", new Vector3(0.82f, 0f, 1.2f), wheelMaterial, rimMaterial);
            WheelAssembly rearLeft = CreateWheel(vehicle.transform, "RearLeft", new Vector3(-0.82f, 0f, -1.2f), wheelMaterial, rimMaterial);
            WheelAssembly rearRight = CreateWheel(vehicle.transform, "RearRight", new Vector3(0.82f, 0f, -1.2f), wheelMaterial, rimMaterial);

            VehicleController controller = vehicle.AddComponent<VehicleController>();
            vehicle.AddComponent<VehicleDebugInput>();

            SerializedObject serializedController = new SerializedObject(controller);
            SetObjectReference(serializedController, "frontLeftCollider", frontLeft.Collider);
            SetObjectReference(serializedController, "frontRightCollider", frontRight.Collider);
            SetObjectReference(serializedController, "rearLeftCollider", rearLeft.Collider);
            SetObjectReference(serializedController, "rearRightCollider", rearRight.Collider);
            SetObjectReference(serializedController, "frontLeftVisual", frontLeft.VisualRoot);
            SetObjectReference(serializedController, "frontRightVisual", frontRight.VisualRoot);
            SetObjectReference(serializedController, "rearLeftVisual", rearLeft.VisualRoot);
            SetObjectReference(serializedController, "rearRightVisual", rearRight.VisualRoot);
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            controller.ReapplyTuning();

            EnsureFolder("Assets/Prefabs", "Vehicles");
            PrefabUtility.SaveAsPrefabAssetAndConnect(vehicle, PrefabPath, InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = vehicle;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Prototype vehicle created with unchanged physics and an authored low-poly visual shell. " +
                "Body, cabin, glass, lights, bumpers, and wheel rims are presentation-only children without colliders.");
        }

        [MenuItem("Beyond The Beat/Phase 0/Validate Prototype Vehicle")]
        private static void ValidatePrototypeVehicle()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Vehicle validation FAIL: scene not found at {ScenePath}.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject vehicle = FindRootObject(validationScene, VehicleName);
                VehicleController controller = null;

                bool vehiclePass = vehicle != null;
                bool rigidbodyPass = vehiclePass && vehicle.TryGetComponent(out Rigidbody body) && Mathf.Abs(body.mass - 1250f) < 0.1f;
                bool controllerPass = vehiclePass && vehicle.TryGetComponent(out controller);
                bool debugInputPass = vehiclePass && vehicle.TryGetComponent<VehicleDebugInput>(out _);
                bool wheelCountPass = vehiclePass && vehicle.GetComponentsInChildren<WheelCollider>(true).Length == 4;
                bool prefabPass = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
                bool referencesPass = controllerPass && controller != null && ValidateControllerReferences(controller);
                bool tuningPass = controllerPass && controller != null && ValidateCandidateTuning(controller);

                Transform bodyVisual = vehiclePass ? vehicle.transform.Find("BodyVisual") : null;
                int rimCount = vehiclePass
                    ? vehicle.GetComponentsInChildren<Transform>(true).Count(transform => transform.name == "Rim")
                    : 0;
                bool authoredVisualPass =
                    bodyVisual != null &&
                    bodyVisual.childCount >= 12 &&
                    rimCount == 4 &&
                    bodyVisual.GetComponentsInChildren<Collider>(true).Length == 0;

                bool allPass =
                    vehiclePass &&
                    rigidbodyPass &&
                    controllerPass &&
                    debugInputPass &&
                    wheelCountPass &&
                    prefabPass &&
                    referencesPass &&
                    tuningPass &&
                    authoredVisualPass;

                string message =
                    "[Beyond The Beat] Phase 0 vehicle validation\n" +
                    $"Vehicle in scene: {PassFail(vehiclePass)}\n" +
                    $"Rigidbody mass 1250 kg: {PassFail(rigidbodyPass)}\n" +
                    $"VehicleController attached: {PassFail(controllerPass)}\n" +
                    $"Debug input adapter attached: {PassFail(debugInputPass)}\n" +
                    $"Four WheelColliders: {PassFail(wheelCountPass)}\n" +
                    $"Controller wheel references: {PassFail(referencesPass)}\n" +
                    $"Candidate final tuning baseline: {PassFail(tuningPass)}\n" +
                    $"Authored body/glass/lights/rims visual shell: {PassFail(authoredVisualPass)}\n" +
                    $"Prototype vehicle prefab: {PassFail(prefabPass)}";

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

        private static void CreateBodyVisual(
            Transform parent,
            Material bodyMaterial,
            Material glassMaterial,
            Material trimMaterial,
            Material headlightMaterial,
            Material tailLightMaterial)
        {
            GameObject visualRoot = new GameObject("BodyVisual");
            visualRoot.transform.SetParent(parent, false);

            CreateVisualBox("LowerBody", visualRoot.transform, new Vector3(0f, 0.44f, 0f), new Vector3(1.78f, 0.48f, 3.52f), Quaternion.identity, bodyMaterial);
            CreateVisualBox("Hood", visualRoot.transform, new Vector3(0f, 0.76f, 1.03f), new Vector3(1.64f, 0.24f, 1.16f), Quaternion.Euler(-4f, 0f, 0f), bodyMaterial);
            CreateVisualBox("RearDeck", visualRoot.transform, new Vector3(0f, 0.75f, -1.28f), new Vector3(1.62f, 0.22f, 0.72f), Quaternion.Euler(3f, 0f, 0f), bodyMaterial);
            CreateVisualBox("Cabin", visualRoot.transform, new Vector3(0f, 1.05f, -0.18f), new Vector3(1.48f, 0.60f, 1.48f), Quaternion.identity, bodyMaterial);
            CreateVisualBox("Roof", visualRoot.transform, new Vector3(0f, 1.39f, -0.22f), new Vector3(1.28f, 0.11f, 1.08f), Quaternion.identity, trimMaterial);

            CreateVisualBox("Windshield", visualRoot.transform, new Vector3(0f, 1.08f, 0.58f), new Vector3(1.36f, 0.48f, 0.055f), Quaternion.Euler(-24f, 0f, 0f), glassMaterial);
            CreateVisualBox("RearGlass", visualRoot.transform, new Vector3(0f, 1.08f, -0.93f), new Vector3(1.34f, 0.42f, 0.055f), Quaternion.Euler(24f, 0f, 0f), glassMaterial);
            CreateVisualBox("SideGlass_L", visualRoot.transform, new Vector3(-0.755f, 1.08f, -0.18f), new Vector3(0.045f, 0.44f, 0.92f), Quaternion.identity, glassMaterial);
            CreateVisualBox("SideGlass_R", visualRoot.transform, new Vector3(0.755f, 1.08f, -0.18f), new Vector3(0.045f, 0.44f, 0.92f), Quaternion.identity, glassMaterial);

            CreateVisualBox("FrontBumper", visualRoot.transform, new Vector3(0f, 0.42f, 1.80f), new Vector3(1.72f, 0.18f, 0.12f), Quaternion.identity, trimMaterial);
            CreateVisualBox("RearBumper", visualRoot.transform, new Vector3(0f, 0.42f, -1.80f), new Vector3(1.72f, 0.18f, 0.12f), Quaternion.identity, trimMaterial);
            CreateVisualBox("SideSkirt_L", visualRoot.transform, new Vector3(-0.86f, 0.34f, -0.05f), new Vector3(0.10f, 0.16f, 2.45f), Quaternion.identity, trimMaterial);
            CreateVisualBox("SideSkirt_R", visualRoot.transform, new Vector3(0.86f, 0.34f, -0.05f), new Vector3(0.10f, 0.16f, 2.45f), Quaternion.identity, trimMaterial);

            CreateVisualBox("Headlight_L", visualRoot.transform, new Vector3(-0.57f, 0.64f, 1.79f), new Vector3(0.43f, 0.18f, 0.07f), Quaternion.identity, headlightMaterial);
            CreateVisualBox("Headlight_R", visualRoot.transform, new Vector3(0.57f, 0.64f, 1.79f), new Vector3(0.43f, 0.18f, 0.07f), Quaternion.identity, headlightMaterial);
            CreateVisualBox("TailLight_L", visualRoot.transform, new Vector3(-0.58f, 0.63f, -1.79f), new Vector3(0.38f, 0.16f, 0.07f), Quaternion.identity, tailLightMaterial);
            CreateVisualBox("TailLight_R", visualRoot.transform, new Vector3(0.58f, 0.63f, -1.79f), new Vector3(0.38f, 0.16f, 0.07f), Quaternion.identity, tailLightMaterial);
        }

        private static WheelAssembly CreateWheel(
            Transform parent,
            string name,
            Vector3 localPosition,
            Material wheelMaterial,
            Material rimMaterial)
        {
            GameObject colliderObject = new GameObject(name + "WheelCollider");
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.localPosition = localPosition;

            WheelCollider wheelCollider = colliderObject.AddComponent<WheelCollider>();
            wheelCollider.radius = 0.34f;
            wheelCollider.mass = 28f;
            wheelCollider.suspensionDistance = 0.22f;

            GameObject visualRoot = new GameObject(name + "WheelVisual");
            visualRoot.transform.SetParent(parent, false);
            visualRoot.transform.localPosition = localPosition;

            GameObject visualMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visualMesh.name = "Tire";
            visualMesh.transform.SetParent(visualRoot.transform, false);
            visualMesh.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            visualMesh.transform.localScale = new Vector3(0.34f, 0.12f, 0.34f);
            AssignMaterial(visualMesh, wheelMaterial);
            RemoveCollider(visualMesh);

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(visualRoot.transform, false);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rim.transform.localScale = new Vector3(0.205f, 0.128f, 0.205f);
            AssignMaterial(rim, rimMaterial);
            RemoveCollider(rim);

            return new WheelAssembly(wheelCollider, visualRoot.transform);
        }

        private static GameObject CreateVisualBox(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.transform.localRotation = localRotation;
            AssignMaterial(item, material);
            RemoveCollider(item);
            return item;
        }

        private static bool ValidateControllerReferences(VehicleController controller)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            string[] referenceNames =
            {
                "frontLeftCollider",
                "frontRightCollider",
                "rearLeftCollider",
                "rearRightCollider",
                "frontLeftVisual",
                "frontRightVisual",
                "rearLeftVisual",
                "rearRightVisual"
            };

            return referenceNames.All(name =>
            {
                SerializedProperty property = serializedController.FindProperty(name);
                return property != null && property.objectReferenceValue != null;
            });
        }

        private static bool ValidateCandidateTuning(VehicleController controller)
        {
            Vector3 centerOfMass = controller.CenterOfMassOffset;

            return Approximately(controller.MotorTorque, 1700f) &&
                   Approximately(controller.BrakeTorque, 3800f) &&
                   Approximately(controller.MaxSteerAngle, 30f) &&
                   Approximately(controller.SteeringResponse, 6f) &&
                   Approximately(controller.HighSpeedSteerStartKph, 5f) &&
                   Approximately(controller.HighSpeedSteerFullKph, 50f) &&
                   Approximately(controller.HighSpeedSteerMultiplier, 0.38f) &&
                   Approximately(controller.SuspensionSpring, 35000f) &&
                   Approximately(controller.SuspensionDamper, 5000f) &&
                   Approximately(controller.ForwardFrictionStiffness, 1.4f) &&
                   Approximately(controller.SidewaysFrictionStiffness, 1.6f) &&
                   Approximately(centerOfMass.x, 0f) &&
                   Approximately(centerOfMass.y, -0.5f) &&
                   Approximately(centerOfMass.z, 0f) &&
                   Approximately(controller.DownforceCoefficient, 20f);
        }

        private static bool Approximately(float actual, float expected)
        {
            return Mathf.Abs(actual - expected) < 0.001f;
        }

        private static void SetObjectReference(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"VehicleController property '{propertyName}' was not found.");
            }

            property.objectReferenceValue = value;
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static Transform FindTransformByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                Transform match = transforms.FirstOrDefault(item => item.name == name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Material GetOrCreateMaterial(
            string path,
            Color color,
            float smoothness,
            float metallic)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("No compatible Lit shader was found for the prototype vehicle.");
                }

                material = new Material(shader)
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            SetColorIfPresent(material, "_BaseColor", color);
            SetFloatIfPresent(material, "_Smoothness", smoothness);
            SetFloatIfPresent(material, "_Glossiness", smoothness);
            SetFloatIfPresent(material, "_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetColorIfPresent(Material material, string propertyName, Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void AssignMaterial(GameObject gameObject, Material material)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string fullPath = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";

        private readonly struct WheelAssembly
        {
            public WheelAssembly(WheelCollider collider, Transform visualRoot)
            {
                Collider = collider;
                VisualRoot = visualRoot;
            }

            public WheelCollider Collider { get; }
            public Transform VisualRoot { get; }
        }
    }
}
