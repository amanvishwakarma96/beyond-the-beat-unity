using System;
using System.Linq;
using BeyondTheBeat.Puzzles;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase3RestrictedAreaBuilder
    {
        public const string Phase3ScenePath = "Assets/Scenes/Phase3/Phase3_RestrictedArea.unity";

        private const string RootName = "Phase3RestrictedArea";
        private const string CompoundName = "RestrictedCompound";
        private const string ZoneName = "RestrictedZoneContext";
        private const string GateMechanismName = "RestrictedGateMechanism";
        private const string GatePanelName = "GatePanel";
        private const string PuzzleRootName = "PressurePlatePuzzle";
        private const string PressurePlateName = "PressurePlate";
        private const string PuzzleCrateName = "PuzzleCrate";
        private const string ConnectorName = "ForestToRestrictedConnector";
        private const string PuzzleApronName = "PuzzleApron";
        private const string RestrictedZoneId = "restricted-yard";

        private const string GroundMaterialPath = "Assets/Materials/Phase3_RestrictedGround.mat";
        private const string WallMaterialPath = "Assets/Materials/Phase3_RestrictedWall.mat";
        private const string GateMaterialPath = "Assets/Materials/Phase3_Gate.mat";
        private const string PlateMaterialPath = "Assets/Materials/Phase3_PressurePlate.mat";
        private const string CrateMaterialPath = "Assets/Materials/Phase3_PuzzleCrate.mat";
        private const string RoadMaterialPath = "Assets/Materials/Prototype_Road.mat";

        private static readonly Vector3 RestrictedCenter = new Vector3(205f, 0f, 0f);
        private static readonly Vector3 RestrictedZoneSize = new Vector3(48f, 4f, 58f);
        private static readonly Vector3 GateRootPosition = new Vector3(180.5f, 0f, 0f);
        private static readonly Vector3 GateClosedLocalPosition = new Vector3(0f, 2.5f, 0f);
        private static readonly Vector3 GateOpenOffset = new Vector3(0f, 6f, 0f);
        private const float RequiredPuzzleMass = 4f;

        [MenuItem("Beyond The Beat/Phase 3/Build Restricted Area Foundation")]
        public static void BuildRestrictedAreaFoundation()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset phase2SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase2WorldBuilder.Phase2ScenePath);
            if (phase2SceneAsset == null)
            {
                Debug.LogError(
                    $"[Beyond The Beat] Phase 3 restricted-area build requires the integrated Phase 2 scene at '{Phase2WorldBuilder.Phase2ScenePath}'.");
                return;
            }

            EnsureFolder("Assets/Scenes", "Phase3");
            EnsureFolder("Assets", "Materials");

            Scene phase2Scene = EditorSceneManager.OpenScene(Phase2WorldBuilder.Phase2ScenePath, OpenSceneMode.Single);
            if (phase2Scene.isDirty && !EditorSceneManager.SaveScene(phase2Scene))
            {
                Debug.LogError("[Beyond The Beat] Unable to save Phase 2 source scene before creating Phase 3.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase3ScenePath) != null &&
                !AssetDatabase.DeleteAsset(Phase3ScenePath))
            {
                Debug.LogError($"[Beyond The Beat] Unable to replace existing Phase 3 scene at '{Phase3ScenePath}'.");
                return;
            }

            if (!AssetDatabase.CopyAsset(Phase2WorldBuilder.Phase2ScenePath, Phase3ScenePath))
            {
                Debug.LogError($"[Beyond The Beat] Unable to copy Phase 2 scene to '{Phase3ScenePath}'.");
                return;
            }

            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.OpenScene(Phase3ScenePath, OpenSceneMode.Single);
            RemoveExistingRoot(scene, RootName);

            Material groundMaterial = GetOrCreateMaterial(GroundMaterialPath, new Color(0.24f, 0.25f, 0.24f));
            Material wallMaterial = GetOrCreateMaterial(WallMaterialPath, new Color(0.38f, 0.40f, 0.42f));
            Material gateMaterial = GetOrCreateMaterial(GateMaterialPath, new Color(0.16f, 0.18f, 0.20f));
            Material plateMaterial = GetOrCreateMaterial(PlateMaterialPath, new Color(0.72f, 0.52f, 0.12f));
            Material crateMaterial = GetOrCreateMaterial(CrateMaterialPath, new Color(0.42f, 0.24f, 0.10f));
            Material roadMaterial = AssetDatabase.LoadAssetAtPath<Material>(RoadMaterialPath) ?? groundMaterial;

            GameObject root = new GameObject(RootName);
            CreateConnector(root.transform, roadMaterial, groundMaterial);
            GameObject compound = CreateCompound(root.transform, groundMaterial, wallMaterial);
            CreateRestrictedZone(compound.transform);
            RestrictedGateController gate = CreateGate(root.transform, gateMaterial);
            CreatePuzzle(root.transform, plateMaterial, crateMaterial, gate);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save generated Phase 3 scene at '{Phase3ScenePath}'.");
                return;
            }

            AddSceneToBuildSettings(Phase3ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Phase 3 restricted-area foundation created. " +
                "The full Phase 2 vertical slice is preserved, and the new pressure-plate puzzle controls a reusable restricted gate without mission-specific logic.");
        }

        [MenuItem("Beyond The Beat/Phase 3/Validate Restricted Area Foundation")]
        public static void ValidateRestrictedAreaFoundation()
        {
            if (!ValidateRestrictedAreaFoundationInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateRestrictedAreaFoundationOrThrow()
        {
            if (ValidateRestrictedAreaFoundationInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateRestrictedAreaFoundationInternal(out string message)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase3ScenePath);
            if (sceneAsset == null)
            {
                message = $"[Beyond The Beat] Phase 3 restricted-area validation FAIL: scene not found at '{Phase3ScenePath}'.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != Phase3ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(Phase3ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject root = FindRootObject(validationScene, RootName);
                GameObject phase2Forest = FindRootObject(validationScene, "Phase2ForestBiome");
                GameObject phase2Survival = FindRootObject(validationScene, "Phase2SurvivalSystem");
                GameObject phase1Mission = FindRootObject(validationScene, "Phase1MissionSystem");
                GameObject phase1Persistence = FindRootObject(validationScene, "Phase1Persistence");
                GameObject mobileCanvas = FindRootObject(validationScene, "MobileDrivingCanvas");
                GameObject parkingRoot = FindRootObject(validationScene, "ParkingPrototype");

                bool inheritedPhase2Pass = phase2Forest != null &&
                                           phase2Survival != null &&
                                           phase1Mission != null &&
                                           phase1Persistence != null &&
                                           mobileCanvas != null &&
                                           parkingRoot != null;

                Transform compound = root != null ? root.transform.Find(CompoundName) : null;
                ZoneContext restrictedZone = compound != null
                    ? compound.GetComponentInChildren<ZoneContext>(true)
                    : null;
                bool restrictedZonePass = restrictedZone != null &&
                                          restrictedZone.ZoneId == RestrictedZoneId &&
                                          restrictedZone.ZoneType == WorldZoneType.Restricted &&
                                          restrictedZone.TryGetComponent(out BoxCollider restrictedTrigger) &&
                                          restrictedTrigger.isTrigger &&
                                          Approximately(restrictedTrigger.size, RestrictedZoneSize);

                int restrictedZoneCount = validationScene.GetRootGameObjects()
                    .SelectMany(item => item.GetComponentsInChildren<ZoneContext>(true))
                    .Count(item => string.Equals(item.ZoneId, RestrictedZoneId, StringComparison.Ordinal));
                bool uniqueZonePass = restrictedZoneCount == 1;

                Transform gateRoot = root != null ? root.transform.Find(GateMechanismName) : null;
                RestrictedGateController gate = gateRoot != null
                    ? gateRoot.GetComponent<RestrictedGateController>()
                    : null;
                Transform puzzleRoot = root != null ? root.transform.Find(PuzzleRootName) : null;
                PuzzleStateController puzzleState = puzzleRoot != null
                    ? puzzleRoot.GetComponent<PuzzleStateController>()
                    : null;
                PuzzleGateBinding binding = puzzleRoot != null
                    ? puzzleRoot.GetComponent<PuzzleGateBinding>()
                    : null;
                PhysicsPressurePlate plate = puzzleRoot != null
                    ? puzzleRoot.GetComponentInChildren<PhysicsPressurePlate>(true)
                    : null;
                Rigidbody crate = puzzleRoot != null
                    ? puzzleRoot.GetComponentsInChildren<Rigidbody>(true).FirstOrDefault(body => body.gameObject.name == PuzzleCrateName)
                    : null;

                bool gateStructurePass = gate != null &&
                                         gate.GateTransform != null &&
                                         gate.GateTransform.name == GatePanelName &&
                                         gate.GateTransform.TryGetComponent(out Collider gateCollider) &&
                                         !gateCollider.isTrigger &&
                                         gate.StartLocked;
                bool puzzleStructurePass = puzzleState != null &&
                                           binding != null &&
                                           binding.PuzzleState == puzzleState &&
                                           binding.Gate == gate &&
                                           binding.RelockWhenPuzzleResets &&
                                           plate != null &&
                                           plate.PuzzleState == puzzleState &&
                                           plate.TryGetComponent(out Collider plateCollider) &&
                                           plateCollider.isTrigger &&
                                           crate != null &&
                                           crate.mass >= RequiredPuzzleMass;
                bool massContractPass = plate != null &&
                                        plate.MeetsRequirement(RequiredPuzzleMass) &&
                                        !plate.MeetsRequirement(Mathf.Max(0f, RequiredPuzzleMass - 0.5f));
                bool connectorPass = root != null &&
                                     root.transform.Find(ConnectorName) != null &&
                                     root.transform.Find(PuzzleApronName) != null;

                bool behaviorPass = ValidatePuzzleGateBehavior(puzzleState, binding, gate);
                bool buildSettingsPass = EditorBuildSettings.scenes.Any(item => item.path == Phase3ScenePath && item.enabled);

                bool allPass = inheritedPhase2Pass &&
                               root != null &&
                               compound != null &&
                               restrictedZonePass &&
                               uniqueZonePass &&
                               gateStructurePass &&
                               puzzleStructurePass &&
                               massContractPass &&
                               behaviorPass &&
                               connectorPass &&
                               buildSettingsPass;

                message =
                    "[Beyond The Beat] Phase 3 restricted-area foundation validation\n" +
                    $"Inherited Phase 2 world/survival/mission/save/HUD/parking: {PassFail(inheritedPhase2Pass)}\n" +
                    $"Phase3RestrictedArea/RestrictedCompound roots: {PassFail(root != null && compound != null)}\n" +
                    $"Restricted ZoneContext id/type/trigger: {PassFail(restrictedZonePass)}\n" +
                    $"Unique restricted zone id: {PassFail(uniqueZonePass)}\n" +
                    $"Locked gate structure/collider: {PassFail(gateStructurePass)}\n" +
                    $"Pressure plate + crate + generic binding structure: {PassFail(puzzleStructurePass)}\n" +
                    $"Pressure-plate mass threshold contract: {PassFail(massContractPass)}\n" +
                    $"Puzzle solve unlock/open + reset relock/close: {PassFail(behaviorPass)}\n" +
                    $"Drivable connector + puzzle apron: {PassFail(connectorPass)}\n" +
                    $"Phase 3 scene enabled in Build Settings: {PassFail(buildSettingsPass)}";

                return allPass;
            }
            finally
            {
                if (openedForValidation && validationScene.IsValid())
                {
                    EditorSceneManager.CloseScene(validationScene, true);
                }
            }
        }

        private static GameObject CreateCompound(Transform parent, Material groundMaterial, Material wallMaterial)
        {
            GameObject compound = new GameObject(CompoundName);
            compound.transform.SetParent(parent, false);

            CreateBox("RestrictedGround", compound.transform, new Vector3(205f, 0.16f, 0f), new Vector3(50f, 0.08f, 60f), groundMaterial, true);
            CreateBox("NorthWall", compound.transform, new Vector3(205f, 1.5f, 30f), new Vector3(50f, 3f, 1f), wallMaterial, true);
            CreateBox("SouthWall", compound.transform, new Vector3(205f, 1.5f, -30f), new Vector3(50f, 3f, 1f), wallMaterial, true);
            CreateBox("EastWall", compound.transform, new Vector3(230f, 1.5f, 0f), new Vector3(1f, 3f, 60f), wallMaterial, true);
            CreateBox("WestWallNorth", compound.transform, new Vector3(180f, 1.5f, 17.5f), new Vector3(1f, 3f, 25f), wallMaterial, true);
            CreateBox("WestWallSouth", compound.transform, new Vector3(180f, 1.5f, -17.5f), new Vector3(1f, 3f, 25f), wallMaterial, true);

            return compound;
        }

        private static void CreateConnector(Transform parent, Material roadMaterial, Material groundMaterial)
        {
            CreateBox(ConnectorName, parent, new Vector3(172.5f, 0.16f, 0f), new Vector3(15f, 0.08f, 10f), roadMaterial, true);
            CreateBox(PuzzleApronName, parent, new Vector3(172.5f, 0.15f, 10f), new Vector3(15f, 0.08f, 12f), groundMaterial, true);
        }

        private static void CreateRestrictedZone(Transform parent)
        {
            GameObject zoneObject = new GameObject(ZoneName);
            zoneObject.transform.SetParent(parent, false);
            zoneObject.transform.position = new Vector3(205f, 2f, 0f);

            BoxCollider trigger = zoneObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = RestrictedZoneSize;

            ZoneContext zone = zoneObject.AddComponent<ZoneContext>();
            SerializedObject serialized = new SerializedObject(zone);
            SerializedProperty zoneId = serialized.FindProperty("zoneId");
            SerializedProperty zoneType = serialized.FindProperty("zoneType");
            if (zoneId == null || zoneType == null)
            {
                throw new InvalidOperationException("ZoneContext serialized fields could not be resolved for Phase 3 restricted-area setup.");
            }

            zoneId.stringValue = RestrictedZoneId;
            zoneType.enumValueIndex = (int)WorldZoneType.Restricted;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RestrictedGateController CreateGate(Transform parent, Material gateMaterial)
        {
            GameObject gateRoot = new GameObject(GateMechanismName);
            gateRoot.transform.SetParent(parent, false);
            gateRoot.transform.position = GateRootPosition;

            GameObject panel = CreateBox(
                GatePanelName,
                gateRoot.transform,
                Vector3.zero,
                new Vector3(1f, 5f, 10f),
                gateMaterial,
                true,
                false);
            panel.transform.localPosition = GateClosedLocalPosition;

            RestrictedGateController gate = gateRoot.AddComponent<RestrictedGateController>();
            SerializedObject serialized = new SerializedObject(gate);
            SetObjectReference(serialized, "gateTransform", panel.transform);
            SetVector(serialized, "closedLocalPosition", GateClosedLocalPosition);
            SetVector(serialized, "openLocalOffset", GateOpenOffset);
            SetFloat(serialized, "transitionDuration", 0.35f);
            SetBool(serialized, "startLocked", true);
            SetBool(serialized, "openWhenUnlocked", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            gate.SetLocked(true);
            gate.SnapToCurrentState();
            return gate;
        }

        private static void CreatePuzzle(
            Transform parent,
            Material plateMaterial,
            Material crateMaterial,
            RestrictedGateController gate)
        {
            GameObject puzzleRoot = new GameObject(PuzzleRootName);
            puzzleRoot.transform.SetParent(parent, false);

            PuzzleStateController puzzleState = puzzleRoot.AddComponent<PuzzleStateController>();

            GameObject plateObject = CreateBox(
                PressurePlateName,
                puzzleRoot.transform,
                new Vector3(173f, 0.35f, 10f),
                new Vector3(4f, 0.3f, 4f),
                plateMaterial,
                true);
            BoxCollider plateCollider = plateObject.GetComponent<BoxCollider>();
            plateCollider.isTrigger = true;
            PhysicsPressurePlate plate = plateObject.AddComponent<PhysicsPressurePlate>();
            SerializedObject plateSerialized = new SerializedObject(plate);
            SetObjectReference(plateSerialized, "puzzleState", puzzleState);
            SetFloat(plateSerialized, "requiredMass", RequiredPuzzleMass);
            SetBool(plateSerialized, "resetWhenBelowRequirement", true);
            plateSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject crate = CreateBox(
                PuzzleCrateName,
                puzzleRoot.transform,
                new Vector3(168f, 1.1f, 10f),
                new Vector3(2f, 2f, 2f),
                crateMaterial,
                true);
            Rigidbody crateBody = crate.AddComponent<Rigidbody>();
            crateBody.mass = 5f;
            crateBody.linearDamping = 1.25f;
            crateBody.angularDamping = 1.5f;

            PuzzleGateBinding binding = puzzleRoot.AddComponent<PuzzleGateBinding>();
            SerializedObject bindingSerialized = new SerializedObject(binding);
            SetObjectReference(bindingSerialized, "puzzleState", puzzleState);
            SetObjectReference(bindingSerialized, "gate", gate);
            SetBool(bindingSerialized, "relockWhenPuzzleResets", true);
            bindingSerialized.ApplyModifiedPropertiesWithoutUndo();
            binding.Rebind();
            binding.Synchronize();
        }

        private static bool ValidatePuzzleGateBehavior(
            PuzzleStateController puzzleState,
            PuzzleGateBinding binding,
            RestrictedGateController gate)
        {
            if (puzzleState == null || binding == null || gate == null || gate.GateTransform == null)
            {
                return false;
            }

            puzzleState.ResetPuzzle();
            binding.Rebind();
            binding.Synchronize();
            bool startsLocked = gate.IsLocked &&
                                !gate.IsOpen &&
                                Approximately(gate.GateTransform.localPosition, gate.ClosedLocalPosition);

            puzzleState.SetSolved(true);
            bool unlocks = !gate.IsLocked &&
                           gate.IsOpen &&
                           Approximately(gate.GateTransform.localPosition, gate.OpenLocalPosition);

            puzzleState.ResetPuzzle();
            bool resets = gate.IsLocked &&
                          !gate.IsOpen &&
                          Approximately(gate.GateTransform.localPosition, gate.ClosedLocalPosition);

            return startsLocked && unlocks && resets;
        }

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool keepCollider,
            bool worldPosition = true)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            if (worldPosition)
            {
                box.transform.position = position;
            }
            else
            {
                box.transform.localPosition = position;
            }
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
                throw new InvalidOperationException("No compatible shader was found for Phase 3 restricted-area materials.");
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

        private static void SetObjectReference(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to resolve serialized field '{name}'.");
            }

            property.objectReferenceValue = value;
        }

        private static void SetVector(SerializedObject serialized, string name, Vector3 value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to resolve serialized field '{name}'.");
            }

            property.vector3Value = value;
        }

        private static void SetFloat(SerializedObject serialized, string name, float value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to resolve serialized field '{name}'.");
            }

            property.floatValue = value;
        }

        private static void SetBool(SerializedObject serialized, string name, bool value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to resolve serialized field '{name}'.");
            }

            property.boolValue = value;
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
