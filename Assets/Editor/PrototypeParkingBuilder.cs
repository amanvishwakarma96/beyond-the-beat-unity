using System;
using System.Linq;
using BeyondTheBeat.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class PrototypeParkingBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Phase0_Prototype.unity";
        private const string ParkingRootName = "ParkingPrototype";
        private const string ParkingZoneName = "ParkHereZone";
        private const string VehicleName = "PrototypeVehicle";
        private const string ParkingMaterialPath = "Assets/Materials/Prototype_ParkingZone.mat";

        private static readonly Vector3 ParkingPosition = new Vector3(9f, 1.0f, 60f);
        private static readonly Vector3 TriggerSize = new Vector3(4.5f, 2.0f, 7.0f);

        [MenuItem("Beyond The Beat/Phase 0/Build Parking Interaction")]
        private static void BuildParkingInteraction()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Prototype scene not found at {ScenePath}.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject vehicle = FindRootObject(scene, VehicleName);
            if (vehicle == null || !vehicle.TryGetComponent<InteractionController>(out _))
            {
                Debug.LogError("[Beyond The Beat] PrototypeVehicle with InteractionController is required before building parking.");
                return;
            }

            RemoveExistingRoot(scene, ParkingRootName);

            Material parkingMaterial = GetOrCreateMaterial(
                ParkingMaterialPath,
                new Color(0.08f, 0.75f, 0.35f, 0.62f));

            GameObject root = new GameObject(ParkingRootName);

            GameObject zoneObject = new GameObject(ParkingZoneName);
            zoneObject.transform.SetParent(root.transform, false);
            zoneObject.transform.position = ParkingPosition;

            BoxCollider trigger = zoneObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = TriggerSize;

            ParkingZone parkingZone = zoneObject.AddComponent<ParkingZone>();
            InteractionTrigger interactionTrigger = zoneObject.GetComponent<InteractionTrigger>();

            SerializedObject serializedParking = new SerializedObject(parkingZone);
            SetString(serializedParking, "promptText", "Park Here");
            SetFloat(serializedParking, "stopThresholdKph", 2f);
            SetString(serializedParking, "successMessage", "Parked successfully");
            serializedParking.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedTrigger = new SerializedObject(interactionTrigger);
            SetObjectReference(serializedTrigger, "interactable", parkingZone);
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();

            CreateParkingVisual(zoneObject.transform, parkingMaterial);
            CreateParkingMarkers(root.transform, parkingMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = zoneObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Parking interaction created. Drive into the green zone, stop below 2 km/h, then press ACTION or E.");
        }

        [MenuItem("Beyond The Beat/Phase 0/Validate Parking Interaction")]
        private static void ValidateParkingInteraction()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Parking validation FAIL: scene not found at {ScenePath}.");
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
                GameObject root = FindRootObject(validationScene, ParkingRootName);
                Transform zoneTransform = root != null ? root.transform.Find(ParkingZoneName) : null;
                GameObject zoneObject = zoneTransform != null ? zoneTransform.gameObject : null;

                bool vehiclePass = vehicle != null && vehicle.TryGetComponent<InteractionController>(out _);
                bool rootPass = root != null;
                bool zonePass = zoneObject != null && zoneObject.TryGetComponent(out ParkingZone parkingZone);
                bool triggerPass = zoneObject != null &&
                                   zoneObject.TryGetComponent(out BoxCollider trigger) &&
                                   trigger.isTrigger &&
                                   Approximately(trigger.size, TriggerSize) &&
                                   zoneObject.TryGetComponent<InteractionTrigger>(out _);

                bool promptPass = zonePass && parkingZone.PromptText == "Park Here";
                bool thresholdPass = zonePass && Mathf.Abs(parkingZone.StopThresholdKph - 2f) < 0.01f;
                bool feedbackPass = zonePass && parkingZone.SuccessMessage == "Parked successfully";
                bool positionPass = zoneObject != null && Approximately(zoneObject.transform.position, ParkingPosition);

                bool allPass = vehiclePass && rootPass && zonePass && triggerPass && promptPass && thresholdPass && feedbackPass && positionPass;

                string message =
                    "[Beyond The Beat] Phase 0 parking validation\n" +
                    $"Interaction-capable vehicle: {PassFail(vehiclePass)}\n" +
                    $"Parking prototype root: {PassFail(rootPass)}\n" +
                    $"ParkingZone component: {PassFail(zonePass)}\n" +
                    $"Trigger volume 4.5 x 2 x 7: {PassFail(triggerPass)}\n" +
                    $"Prompt 'Park Here': {PassFail(promptPass)}\n" +
                    $"Stop threshold 2 km/h: {PassFail(thresholdPass)}\n" +
                    $"Success feedback configured: {PassFail(feedbackPass)}\n" +
                    $"Parking-zone placement: {PassFail(positionPass)}";

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

        private static void CreateParkingVisual(Transform parent, Material material)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "ParkingSurface";
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = new Vector3(0f, -0.93f, 0f);
            visual.transform.localScale = new Vector3(TriggerSize.x, 0.08f, TriggerSize.z);

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void CreateParkingMarkers(Transform parent, Material material)
        {
            GameObject markers = new GameObject("ParkingMarkers");
            markers.transform.SetParent(parent, false);

            CreateMarker("LeftLine", markers.transform, new Vector3(6.75f, 0.22f, 60f), new Vector3(0.12f, 0.08f, 7f), material);
            CreateMarker("RightLine", markers.transform, new Vector3(11.25f, 0.22f, 60f), new Vector3(0.12f, 0.08f, 7f), material);
            CreateMarker("EndLine", markers.transform, new Vector3(9f, 0.22f, 63.5f), new Vector3(4.5f, 0.08f, 0.12f), material);
        }

        private static void CreateMarker(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = scale;

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible unlit shader was found for the parking prototype.");
            }

            Material material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path),
                color = color
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static void RemoveExistingRoot(Scene scene, string name)
        {
            GameObject existing = FindRootObject(scene, name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static void SetString(SerializedObject target, string propertyName, string value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found.");
            }

            property.stringValue = value;
        }

        private static void SetFloat(SerializedObject target, string propertyName, float value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found.");
            }

            property.floatValue = value;
        }

        private static void SetObjectReference(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found.");
            }

            property.objectReferenceValue = value;
        }

        private static bool Approximately(Vector3 actual, Vector3 expected)
        {
            return Vector3.SqrMagnitude(actual - expected) < 0.0001f;
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
