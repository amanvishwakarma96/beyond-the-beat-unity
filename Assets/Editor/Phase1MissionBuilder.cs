using System;
using System.Linq;
using BeyondTheBeat.Missions;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase1MissionBuilder
    {
        private const string ScenePath = Phase1WorldBuilder.Phase1ScenePath;
        private const string MissionRootName = "Phase1MissionSystem";
        private const string TargetZoneName = "ReachLocationTargetZone";
        private const string TargetMarkerName = "ReachLocationTargetMarker";
        private const string MissionAssetPath = "Assets/Settings/Missions/Phase1_ReachOffRoadCheckpoint.asset";
        private const string MissionTargetMaterialPath = "Assets/Materials/Phase1_MissionTarget.mat";

        private const string MissionId = "phase1-reach-offroad-checkpoint";
        private const string MissionDisplayName = "Reach the Off-road Checkpoint";
        private const string MissionDescription = "Drive from the urban road into the marked off-road checkpoint.";
        private const string TargetZoneId = "phase1-offroad-checkpoint";

        private static readonly Vector3 TargetZonePosition = new Vector3(62f, 2f, 42f);
        private static readonly Vector3 TargetZoneSize = new Vector3(12f, 4f, 12f);

        [MenuItem("Beyond The Beat/Phase 1/Build Reach Location Mission")]
        public static void BuildReachLocationMission()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError(
                    $"[Beyond The Beat] Phase 1 mission build requires the generated MVP scene at '{ScenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject vehicle = FindRootObject(scene, "PrototypeVehicle");
            ZoneContext offRoadContext = FindZoneById(scene, "off-road");

            if (vehicle == null || offRoadContext == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] Phase 1 mission build requires PrototypeVehicle and the off-road ZoneContext. " +
                    "Build/validate the Phase 1 world foundation first.");
                return;
            }

            EnsureFolder("Assets", "Settings");
            EnsureFolder("Assets/Settings", "Missions");
            EnsureFolder("Assets", "Materials");

            MissionDefinition mission = GetOrCreateMissionDefinition();
            Material targetMaterial = GetOrCreateMaterial(
                MissionTargetMaterialPath,
                new Color(0.12f, 0.78f, 0.92f));

            RemoveExistingRoot(scene, MissionRootName);

            GameObject missionRoot = new GameObject(MissionRootName);
            ZoneContext targetZone = CreateTargetZone(missionRoot.transform);
            CreateTargetMarker(missionRoot.transform, targetMaterial);

            ZoneContext[] observedZones = FindZoneContexts(scene);
            MissionManager manager = missionRoot.AddComponent<MissionManager>();
            ConfigureMissionManager(manager, mission, vehicle, observedZones);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save Phase 1 mission setup into '{ScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = missionRoot;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Phase 1 Reach Location mission created. " +
                $"Mission '{MissionId}' targets ZoneContext '{targetZone.ZoneId}' and starts automatically in Play Mode.");
        }

        [MenuItem("Beyond The Beat/Phase 1/Validate Reach Location Mission")]
        public static void ValidateReachLocationMission()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            MissionDefinition mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionAssetPath);

            if (sceneAsset == null || mission == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] Phase 1 mission validation FAIL: generated scene or mission definition is missing.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject vehicle = FindRootObject(validationScene, "PrototypeVehicle");
                GameObject missionRoot = FindRootObject(validationScene, MissionRootName);
                MissionManager manager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                Transform targetZoneTransform = missionRoot != null ? missionRoot.transform.Find(TargetZoneName) : null;
                ZoneContext targetZone = targetZoneTransform != null
                    ? targetZoneTransform.GetComponent<ZoneContext>()
                    : null;
                ZoneContext broadOffRoadZone = FindZoneById(validationScene, "off-road");

                bool definitionPass =
                    mission.IsConfigured &&
                    mission.MissionId == MissionId &&
                    mission.DisplayName == MissionDisplayName &&
                    mission.ObjectiveType == MissionObjectiveType.ReachLocation &&
                    mission.TargetZoneId == TargetZoneId;

                bool managerPass =
                    manager != null &&
                    manager.StartingMission == mission &&
                    manager.PlayerActor == vehicle &&
                    manager.ObservedZoneCount >= 3;

                bool targetZonePass =
                    targetZone != null &&
                    targetZone.ZoneId == TargetZoneId &&
                    targetZone.ZoneType == WorldZoneType.OffRoad &&
                    targetZone.TryGetComponent(out BoxCollider trigger) &&
                    trigger.isTrigger &&
                    Approximately(trigger.size, TargetZoneSize) &&
                    Approximately(targetZone.transform.position, TargetZonePosition);

                bool targetMarkerPass = missionRoot != null && missionRoot.transform.Find(TargetMarkerName) != null;

                bool evaluatorPass =
                    vehicle != null &&
                    targetZone != null &&
                    broadOffRoadZone != null &&
                    MissionObjectiveEvaluator.IsSatisfied(mission, targetZone, vehicle, vehicle) &&
                    !MissionObjectiveEvaluator.IsSatisfied(mission, broadOffRoadZone, vehicle, vehicle) &&
                    !MissionObjectiveEvaluator.IsSatisfied(mission, targetZone, missionRoot, vehicle);

                bool lifecyclePass = false;
                if (managerPass)
                {
                    bool started = manager.StartMission(mission);
                    lifecyclePass = started && manager.State == MissionState.Active && manager.HasActiveMission;
                    manager.ClearMission();
                    lifecyclePass = lifecyclePass && manager.State == MissionState.Inactive && !manager.HasActiveMission;
                }

                bool allPass =
                    definitionPass &&
                    managerPass &&
                    targetZonePass &&
                    targetMarkerPass &&
                    evaluatorPass &&
                    lifecyclePass;

                string message =
                    "[Beyond The Beat] Phase 1 Reach Location mission validation\n" +
                    $"ScriptableObject mission definition: {PassFail(definitionPass)}\n" +
                    $"MissionManager data/world references: {PassFail(managerPass)}\n" +
                    $"Dedicated target ZoneContext: {PassFail(targetZonePass)}\n" +
                    $"Visible target marker: {PassFail(targetMarkerPass)}\n" +
                    $"Reach Location objective evaluation: {PassFail(evaluatorPass)}\n" +
                    $"Mission start/clear lifecycle: {PassFail(lifecyclePass)}";

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

        private static MissionDefinition GetOrCreateMissionDefinition()
        {
            MissionDefinition mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionAssetPath);
            if (mission == null)
            {
                mission = ScriptableObject.CreateInstance<MissionDefinition>();
                mission.name = "Phase1_ReachOffRoadCheckpoint";
                AssetDatabase.CreateAsset(mission, MissionAssetPath);
            }

            SerializedObject serialized = new SerializedObject(mission);
            SetString(serialized, "missionId", MissionId);
            SetString(serialized, "displayName", MissionDisplayName);
            SetString(serialized, "description", MissionDescription);

            SerializedProperty objectiveType = serialized.FindProperty("objectiveType");
            if (objectiveType == null)
            {
                throw new InvalidOperationException("MissionDefinition objectiveType field could not be resolved.");
            }

            objectiveType.enumValueIndex = (int)MissionObjectiveType.ReachLocation;
            SetString(serialized, "targetZoneId", TargetZoneId);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mission);

            return mission;
        }

        private static ZoneContext CreateTargetZone(Transform parent)
        {
            GameObject targetObject = new GameObject(TargetZoneName);
            targetObject.transform.SetParent(parent, false);
            targetObject.transform.position = TargetZonePosition;

            BoxCollider trigger = targetObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = TargetZoneSize;

            ZoneContext zone = targetObject.AddComponent<ZoneContext>();
            SerializedObject serialized = new SerializedObject(zone);
            SetString(serialized, "zoneId", TargetZoneId);

            SerializedProperty zoneType = serialized.FindProperty("zoneType");
            if (zoneType == null)
            {
                throw new InvalidOperationException("ZoneContext zoneType field could not be resolved.");
            }

            zoneType.enumValueIndex = (int)WorldZoneType.OffRoad;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return zone;
        }

        private static void CreateTargetMarker(Transform parent, Material material)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = TargetMarkerName;
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(TargetZonePosition.x, 0.3f, TargetZonePosition.z);
            marker.transform.localScale = new Vector3(5f, 0.06f, 5f);

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

        private static void ConfigureMissionManager(
            MissionManager manager,
            MissionDefinition mission,
            GameObject playerActor,
            ZoneContext[] observedZones)
        {
            SerializedObject serialized = new SerializedObject(manager);
            SetObjectReference(serialized, "startingMission", mission);
            SetObjectReference(serialized, "playerActor", playerActor);

            SerializedProperty startOnPlay = serialized.FindProperty("startOnPlay");
            SerializedProperty zones = serialized.FindProperty("observedZones");
            if (startOnPlay == null || zones == null)
            {
                throw new InvalidOperationException("MissionManager serialized fields could not be resolved.");
            }

            startOnPlay.boolValue = true;
            zones.arraySize = observedZones.Length;
            for (int i = 0; i < observedZones.Length; i++)
            {
                zones.GetArrayElementAtIndex(i).objectReferenceValue = observedZones[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ZoneContext[] FindZoneContexts(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ZoneContext>(true))
                .OrderBy(zone => zone.ZoneId, StringComparer.Ordinal)
                .ToArray();
        }

        private static ZoneContext FindZoneById(Scene scene, string zoneId)
        {
            return FindZoneContexts(scene)
                .FirstOrDefault(zone => string.Equals(zone.ZoneId, zoneId, StringComparison.Ordinal));
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

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible Unlit shader was found for the mission target marker.");
            }

            Material material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path),
                color = color
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void SetString(SerializedObject target, string propertyName, string value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized string field '{propertyName}' could not be resolved.");
            }

            property.stringValue = value;
        }

        private static void SetObjectReference(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized object field '{propertyName}' could not be resolved.");
            }

            property.objectReferenceValue = value;
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
