using System;
using BeyondTheBeat.Economy;
using BeyondTheBeat.Interaction;
using BeyondTheBeat.Jobs;
using BeyondTheBeat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class Phase4MechanicJobBuilder
    {
        private const string ScenePath = Phase4CookingBuilder.Phase4ScenePath;
        private const string ActivitiesRootName = "Phase4FreeRoamActivities";
        private const string RepairBayName = "VehicleRepairBay";
        private const string VehicleName = "PrototypeVehicle";
        private const string CanvasName = "MobileDrivingCanvas";
        private const string SystemRootName = "Phase4MechanicJobSystem";
        private const string HudRootName = "Phase4MechanicJobHUD";
        private const string JobAssetPath = "Assets/Data/Phase4/PrototypeMechanicJob.asset";
        private const string JobId = "prototype-mechanic-job";
        private const string TargetRepairableId = "prototype-vehicle";
        private const int RewardCredits = 125;

        [MenuItem("Beyond The Beat/Phase 4/Build Mechanic Job")]
        public static void BuildMechanicJob()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException($"Phase 4 mechanic job requires scene '{ScenePath}'.");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject activitiesRoot = FindRootObject(scene, ActivitiesRootName);
            GameObject vehicle = FindRootObject(scene, VehicleName);
            GameObject canvas = FindRootObject(scene, CanvasName);
            Transform repairBay = activitiesRoot != null ? activitiesRoot.transform.Find(RepairBayName) : null;
            RepairableState target = vehicle != null ? vehicle.GetComponent<RepairableState>() : null;
            RepairStation repairStation = repairBay != null ? repairBay.GetComponent<RepairStation>() : null;

            if (activitiesRoot == null || vehicle == null || canvas == null || target == null || repairStation == null)
            {
                throw new InvalidOperationException(
                    "Phase 4 mechanic job requires the inherited free-roam activities, PrototypeVehicle, MobileDrivingCanvas, RepairableState and VehicleRepairBay.");
            }

            MechanicJobDefinition definition = CreateOrUpdateDefinition();
            RemoveRoot(scene, SystemRootName);
            RemoveChild(canvas.transform, HudRootName);

            GameObject systemRoot = new GameObject(SystemRootName);
            CreditWallet wallet = systemRoot.AddComponent<CreditWallet>();
            MechanicJobManager manager = systemRoot.AddComponent<MechanicJobManager>();
            ConfigureWallet(wallet, 0);
            ConfigureManager(manager, definition, repairStation, target, wallet, true);
            manager.RebindSources();

            MechanicJobHud hud = CreateHud(canvas.transform, manager, wallet);
            hud.Refresh();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save Phase 4 mechanic job integration into '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = systemRoot;
            Debug.Log(
                "[Beyond The Beat] Phase 4 mechanic job created. The data-driven job consumes RepairStation completion, awards credits through CreditWallet, and exposes state through a non-blocking event-driven HUD.");
        }

        [MenuItem("Beyond The Beat/Phase 4/Validate Mechanic Job")]
        public static void ValidateMechanicJob()
        {
            if (!ValidateMechanicJobInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateMechanicJobOrThrow()
        {
            if (ValidateMechanicJobInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateMechanicJobInternal(out string message)
        {
            MechanicJobDefinition definition = AssetDatabase.LoadAssetAtPath<MechanicJobDefinition>(JobAssetPath);
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null || definition == null)
            {
                message = $"[Beyond The Beat] Phase 4 mechanic job validation FAIL: scene/job asset missing. scene={sceneAsset != null}, jobAsset={definition != null}.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject systemRoot = FindRootObject(validationScene, SystemRootName);
                GameObject activitiesRoot = FindRootObject(validationScene, ActivitiesRootName);
                GameObject vehicle = FindRootObject(validationScene, VehicleName);
                GameObject canvas = FindRootObject(validationScene, CanvasName);
                Transform repairBay = activitiesRoot != null ? activitiesRoot.transform.Find(RepairBayName) : null;
                Transform cookingStation = activitiesRoot != null ? activitiesRoot.transform.Find("CookingStation") : null;
                Transform hudTransform = canvas != null ? canvas.transform.Find(HudRootName) : null;

                CreditWallet wallet = systemRoot != null ? systemRoot.GetComponent<CreditWallet>() : null;
                MechanicJobManager manager = systemRoot != null ? systemRoot.GetComponent<MechanicJobManager>() : null;
                RepairableState target = vehicle != null ? vehicle.GetComponent<RepairableState>() : null;
                RepairStation station = repairBay != null ? repairBay.GetComponent<RepairStation>() : null;
                MechanicJobHud hud = hudTransform != null ? hudTransform.GetComponent<MechanicJobHud>() : null;

                bool dataPass =
                    definition.IsConfigured &&
                    string.Equals(definition.JobId, JobId, StringComparison.Ordinal) &&
                    string.Equals(definition.TargetRepairableId, TargetRepairableId, StringComparison.Ordinal) &&
                    definition.RewardCredits == RewardCredits;

                bool wiringPass =
                    manager != null &&
                    wallet != null &&
                    target != null &&
                    station != null &&
                    manager.StartingJob == definition &&
                    manager.RepairStation == station &&
                    manager.Target == target &&
                    manager.Wallet == wallet &&
                    station.Target == target &&
                    string.Equals(target.RepairableId, TargetRepairableId, StringComparison.Ordinal);

                bool hudPass =
                    hud != null &&
                    hud.JobManager == manager &&
                    hud.Wallet == wallet &&
                    hud.PanelRoot != null &&
                    hud.JobText != null &&
                    hud.CreditsText != null &&
                    !hud.JobText.raycastTarget &&
                    !hud.CreditsText.raycastTarget &&
                    hud.PanelRoot.GetComponentsInChildren<Image>(true).Length > 0 &&
                    Array.TrueForAll(hud.PanelRoot.GetComponentsInChildren<Image>(true), image => !image.raycastTarget);

                bool inheritedPass =
                    cookingStation != null &&
                    FindRootObject(validationScene, "ParkingPrototype") != null &&
                    FindRootObject(validationScene, "Phase1MissionSystem") != null &&
                    FindRootObject(validationScene, "Phase3RestrictedArea") != null;

                bool behaviorPass = ValidateJobBehavior(definition, out string behaviorDetail);
                bool buildSettingsPass =
                    EditorBuildSettings.scenes.Length == 1 &&
                    EditorBuildSettings.scenes[0].enabled &&
                    string.Equals(EditorBuildSettings.scenes[0].path, ScenePath, StringComparison.Ordinal);

                bool pass = dataPass && wiringPass && hudPass && inheritedPass && behaviorPass && buildSettingsPass;
                message = pass
                    ? "[Beyond The Beat] Phase 4 mechanic job validation PASS: ScriptableObject job data, matching repair completion, cancel/unrelated protection, one-time rewards, restart cycle, non-raycasting HUD and inherited gameplay regressions are intact. Physical Android validation remains required."
                    : "[Beyond The Beat] Phase 4 mechanic job validation FAIL: " +
                      $"data={dataPass}, wiring={wiringPass}, hud={hudPass}, inherited={inheritedPass}, behavior={behaviorPass} ({behaviorDetail}), buildSettings={buildSettingsPass}.";
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

        private static bool ValidateJobBehavior(MechanicJobDefinition definition, out string detail)
        {
            GameObject targetObject = null;
            GameObject unrelatedTargetObject = null;
            GameObject stationObject = null;
            GameObject unrelatedStationObject = null;
            GameObject systemObject = null;
            GameObject actor = null;

            try
            {
                targetObject = new GameObject("MechanicJobValidationTarget");
                RepairableState target = targetObject.AddComponent<RepairableState>();
                ConfigureRepairable(target, TargetRepairableId, 0.6f);

                unrelatedTargetObject = new GameObject("MechanicJobValidationUnrelatedTarget");
                RepairableState unrelatedTarget = unrelatedTargetObject.AddComponent<RepairableState>();
                ConfigureRepairable(unrelatedTarget, "unrelated-target", 0.5f);

                stationObject = CreateValidationStation("MechanicJobValidationStation", target, out RepairStation station);
                unrelatedStationObject = CreateValidationStation("MechanicJobValidationUnrelatedStation", unrelatedTarget, out RepairStation unrelatedStation);

                systemObject = new GameObject("MechanicJobValidationSystem");
                CreditWallet wallet = systemObject.AddComponent<CreditWallet>();
                MechanicJobManager manager = systemObject.AddComponent<MechanicJobManager>();
                ConfigureWallet(wallet, 0);
                ConfigureManager(manager, definition, station, target, wallet, false);
                manager.RebindSources();

                actor = new GameObject("MechanicJobValidationActor");
                int initialBalance = wallet.Balance;
                float initialDamage = target.Damage01;

                bool started = manager.StartJob(definition) && manager.State == MechanicJobState.Active;
                bool repairStarted = station.RequestInteraction(actor);
                station.AdvanceActivity(station.DurationSeconds * 0.4f);
                bool cancelled = station.CancelInteraction(actor) &&
                                 manager.State == MechanicJobState.Active &&
                                 wallet.Balance == initialBalance &&
                                 Mathf.Approximately(target.Damage01, initialDamage);

                bool unrelatedStarted = unrelatedStation.RequestInteraction(actor);
                bool unrelatedCompleted = unrelatedStarted && unrelatedStation.AdvanceActivity(unrelatedStation.DurationSeconds + 0.1f);
                bool unrelatedIgnored = unrelatedCompleted &&
                                        manager.State == MechanicJobState.Active &&
                                        wallet.Balance == initialBalance;

                bool restartedRepair = station.RequestInteraction(actor);
                bool completedRepair = restartedRepair && station.AdvanceActivity(station.DurationSeconds + 0.1f);
                bool paidOnce = completedRepair &&
                                manager.State == MechanicJobState.Completed &&
                                wallet.Balance == initialBalance + definition.RewardCredits;

                int afterFirstReward = wallet.Balance;
                target.SetDamage01(0.3f);
                bool postCompleteRepair = station.RequestInteraction(actor) && station.AdvanceActivity(station.DurationSeconds + 0.1f);
                bool noDoublePay = postCompleteRepair && wallet.Balance == afterFirstReward;

                manager.ClearJob();
                bool fullTargetRejectsJob = !manager.StartJob(definition);
                bool redamaged = target.SetDamage01(0.4f) && target.NeedsRepair;
                bool restartedJob = redamaged && manager.StartJob(definition);
                bool secondRepair = station.RequestInteraction(actor) && station.AdvanceActivity(station.DurationSeconds + 0.1f);
                bool secondReward = secondRepair &&
                                    manager.State == MechanicJobState.Completed &&
                                    wallet.Balance == afterFirstReward + definition.RewardCredits;

                bool pass = started && repairStarted && cancelled && unrelatedIgnored && restartedRepair &&
                            paidOnce && noDoublePay && fullTargetRejectsJob && redamaged && restartedJob && secondReward;
                detail =
                    $"started={started}, repairStarted={repairStarted}, cancelled={cancelled}, unrelatedIgnored={unrelatedIgnored}, " +
                    $"restartedRepair={restartedRepair}, paidOnce={paidOnce}, noDoublePay={noDoublePay}, " +
                    $"fullTargetRejectsJob={fullTargetRejectsJob}, redamaged={redamaged}, restartedJob={restartedJob}, secondReward={secondReward}";
                return pass;
            }
            finally
            {
                DestroyImmediateSafe(actor);
                DestroyImmediateSafe(systemObject);
                DestroyImmediateSafe(unrelatedStationObject);
                DestroyImmediateSafe(stationObject);
                DestroyImmediateSafe(unrelatedTargetObject);
                DestroyImmediateSafe(targetObject);
            }
        }

        private static MechanicJobDefinition CreateOrUpdateDefinition()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "Phase4");

            MechanicJobDefinition definition = AssetDatabase.LoadAssetAtPath<MechanicJobDefinition>(JobAssetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<MechanicJobDefinition>();
                AssetDatabase.CreateAsset(definition, JobAssetPath);
            }

            SerializedObject serialized = new SerializedObject(definition);
            SetString(serialized, "jobId", JobId);
            SetString(serialized, "displayName", "Repair the prototype vehicle");
            SetString(serialized, "targetRepairableId", TargetRepairableId);
            SetInt(serialized, "rewardCredits", RewardCredits);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static MechanicJobHud CreateHud(Transform canvas, MechanicJobManager manager, CreditWallet wallet)
        {
            GameObject hudObject = new GameObject(HudRootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MechanicJobHud));
            hudObject.transform.SetParent(canvas, false);

            RectTransform rootRect = hudObject.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.02f, 0.78f);
            rootRect.anchorMax = new Vector2(0.42f, 0.94f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image background = hudObject.GetComponent<Image>();
            background.color = new Color(0.03f, 0.04f, 0.06f, 0.78f);
            background.raycastTarget = false;

            Text jobText = CreateText("Job", hudObject.transform, new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.94f), 16, FontStyle.Bold);
            Text creditsText = CreateText("Credits", hudObject.transform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.45f), 15, FontStyle.Normal);

            MechanicJobHud hud = hudObject.GetComponent<MechanicJobHud>();
            SerializedObject serialized = new SerializedObject(hud);
            SetObjectReference(serialized, "jobManager", manager);
            SetObjectReference(serialized, "wallet", wallet);
            SetObjectReference(serialized, "panelRoot", hudObject);
            SetObjectReference(serialized, "jobText", jobText);
            SetObjectReference(serialized, "creditsText", creditsText);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
            return hud;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, int fontSize, FontStyle style)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }

        private static GameObject CreateValidationStation(string name, RepairableState target, out RepairStation station)
        {
            GameObject stationObject = new GameObject(name);
            BoxCollider collider = stationObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            station = stationObject.AddComponent<RepairStation>();
            ConfigureRepairStation(station, target);
            return stationObject;
        }

        private static void ConfigureManager(
            MechanicJobManager manager,
            MechanicJobDefinition definition,
            RepairStation station,
            RepairableState target,
            CreditWallet wallet,
            bool startOnPlay)
        {
            SerializedObject serialized = new SerializedObject(manager);
            SetObjectReference(serialized, "startingJob", definition);
            SetBool(serialized, "startStartingJobOnPlay", startOnPlay);
            SetObjectReference(serialized, "repairStation", station);
            SetObjectReference(serialized, "target", target);
            SetObjectReference(serialized, "wallet", wallet);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        private static void ConfigureWallet(CreditWallet wallet, int balance)
        {
            SerializedObject serialized = new SerializedObject(wallet);
            SetInt(serialized, "balance", Mathf.Max(0, balance));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wallet);
        }

        private static void ConfigureRepairable(RepairableState target, string id, float damage01)
        {
            SerializedObject serialized = new SerializedObject(target);
            SetString(serialized, "repairableId", id);
            SetFloat(serialized, "damage01", Mathf.Clamp01(damage01));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void ConfigureRepairStation(RepairStation station, RepairableState target)
        {
            SerializedObject serialized = new SerializedObject(station);
            SetString(serialized, "promptText", "REPAIR");
            SetBool(serialized, "allowRepeatInteraction", true);
            SetFloat(serialized, "durationSeconds", 3f);
            SetBool(serialized, "resetProgressOnCancel", true);
            SetObjectReference(serialized, "target", target);
            SetString(serialized, "activityLabel", "Repair vehicle");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(station);
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, name, StringComparison.Ordinal))
                {
                    return root;
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

        private static void RemoveChild(Transform parent, string name)
        {
            Transform existing = parent != null ? parent.Find(name) : null;
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
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

        private static void DestroyImmediateSafe(UnityEngine.Object value)
        {
            if (value != null)
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException($"Unable to find serialized string property '{propertyName}'.");
            property.stringValue = value;
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException($"Unable to find serialized bool property '{propertyName}'.");
            property.boolValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException($"Unable to find serialized int property '{propertyName}'.");
            property.intValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException($"Unable to find serialized float property '{propertyName}'.");
            property.floatValue = value;
        }

        private static void SetObjectReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException($"Unable to find serialized object property '{propertyName}'.");
            property.objectReferenceValue = value;
        }
    }
}
