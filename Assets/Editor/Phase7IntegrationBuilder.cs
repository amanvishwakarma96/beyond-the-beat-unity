using System;
using System.Linq;
using BeyondTheBeat.Economy;
using BeyondTheBeat.Interaction;
using BeyondTheBeat.UI;
using BeyondTheBeat.Vehicle;
using BeyondTheBeat.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class Phase7IntegrationBuilder
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string VehicleName = "PrototypeVehicle";
        private const int StartingCredits = 300;
        private const int FullHealthRepairCost = 300;
        private const int FullTireRepairCost = 200;

        [MenuItem("Beyond The Beat/Phase 7/Build Systemic Vehicle Integration")]
        public static void BuildSystemicIntegration()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException($"Phase 7 integration requires '{ScenePath}'.");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject vehicleObject = FindRootObject(scene, VehicleName);
            VehicleController vehicle = vehicleObject != null ? vehicleObject.GetComponent<VehicleController>() : null;
            RepairableState repairable = vehicleObject != null ? vehicleObject.GetComponent<RepairableState>() : null;
            RepairStation repairStation = FindInScene<RepairStation>(scene);
            CreditWallet wallet = FindInScene<CreditWallet>(scene);
            DrivingHud drivingHud = FindInScene<DrivingHud>(scene);

            if (vehicle == null || repairable == null || repairStation == null || wallet == null || drivingHud == null)
            {
                throw new InvalidOperationException(
                    "Phase 7 requires the integrated VehicleController, RepairableState, RepairStation, CreditWallet and DrivingHud from Phases 0-6.");
            }

            ConfigureStartingCondition(vehicle, repairable);
            ConfigureWallet(wallet);
            ConfigureRepairEconomy(repairStation, vehicle, wallet);
            ConfigureWearZones(scene, vehicleObject, vehicle);
            UpgradeDrivingCluster(drivingHud);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save Phase 7 integration into '{ScenePath}'.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Beyond The Beat] Phase 7 systemic integration built: wear zones, paid repairs, TMP tactical digital cluster and vehicle-condition sources are wired.");
        }

        public static void PrepareAndValidateOrThrow()
        {
            BuildSystemicIntegration();
            ValidateSceneOrThrow();
        }

        public static void ValidateSceneOrThrow()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = scene.IsValid() && scene.isLoaded;
            if (!opened)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject vehicleObject = FindRootObject(scene, VehicleName);
            VehicleController vehicle = vehicleObject != null ? vehicleObject.GetComponent<VehicleController>() : null;
            VehicleWearZoneAdapter adapter = vehicleObject != null ? vehicleObject.GetComponent<VehicleWearZoneAdapter>() : null;
            RepairStation repairStation = FindInScene<RepairStation>(scene);
            CreditWallet wallet = FindInScene<CreditWallet>(scene);
            DrivingHud hud = FindInScene<DrivingHud>(scene);
            HudPanel hudPanel = hud != null && hud.SpeedValueText != null
                ? hud.SpeedValueText.GetComponentInParent<HudPanel>()
                : null;

            bool pass = vehicle != null &&
                        adapter != null &&
                        adapter.VehicleController == vehicle &&
                        adapter.ZoneCount > 0 &&
                        repairStation != null &&
                        repairStation.Vehicle == vehicle &&
                        repairStation.Wallet == wallet &&
                        wallet != null &&
                        wallet.Balance == StartingCredits &&
                        hud != null &&
                        hud.VehicleController == vehicle &&
                        hudPanel != null &&
                        hud.DamageFill != null &&
                        hud.TractionFill != null &&
                        hud.DamageText != null &&
                        hud.TractionText != null &&
                        !hud.DamageFill.raycastTarget &&
                        !hud.TractionFill.raycastTarget &&
                        !hud.DamageText.raycastTarget &&
                        !hud.TractionText.raycastTarget;

            if (!pass)
            {
                throw new InvalidOperationException(
                    $"Phase 7 scene validation failed: vehicle={vehicle != null}, adapter={adapter != null}, zones={adapter?.ZoneCount ?? 0}, " +
                    $"repairStation={repairStation != null}, wallet={wallet != null}, walletBalance={wallet?.Balance ?? -1}, hud={hud != null}, hudPanel={hudPanel != null}.");
            }

            Debug.Log("[Beyond The Beat] Phase 7 scene integration validation PASS.");
        }

        private static void ConfigureStartingCondition(VehicleController vehicle, RepairableState repairable)
        {
            SerializedObject serialized = new SerializedObject(vehicle);
            SerializedProperty health = serialized.FindProperty("healthCondition");
            SerializedProperty wear = serialized.FindProperty("tireWear");
            if (health == null || wear == null)
            {
                throw new InvalidOperationException("VehicleController condition fields are unavailable.");
            }

            health.floatValue = repairable.NeedsRepair ? repairable.Condition01 : 1f;
            wear.floatValue = repairable.NeedsRepair ? 0.20f : 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(vehicle);
            vehicle.ReapplyTuning();
        }

        private static void ConfigureWallet(CreditWallet wallet)
        {
            SerializedObject serialized = new SerializedObject(wallet);
            SerializedProperty balance = serialized.FindProperty("balance");
            if (balance == null)
            {
                throw new InvalidOperationException("CreditWallet.balance is unavailable.");
            }

            balance.intValue = StartingCredits;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wallet);
        }

        private static void ConfigureRepairEconomy(
            RepairStation repairStation,
            VehicleController vehicle,
            CreditWallet wallet)
        {
            SerializedObject serialized = new SerializedObject(repairStation);
            SetObject(serialized, "vehicle", vehicle);
            SetObject(serialized, "wallet", wallet);
            SetInt(serialized, "fullHealthRepairCost", FullHealthRepairCost);
            SetInt(serialized, "fullTireRepairCost", FullTireRepairCost);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(repairStation);
        }

        private static void ConfigureWearZones(Scene scene, GameObject vehicleObject, VehicleController vehicle)
        {
            ZoneContext[] wearZones = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ZoneContext>(true))
                .Where(zone => zone != null &&
                               (zone.ZoneType == WorldZoneType.OffRoad || zone.ZoneType == WorldZoneType.Forest))
                .Distinct()
                .ToArray();

            if (wearZones.Length == 0)
            {
                throw new InvalidOperationException("Phase 7 could not find OffRoad/Forest ZoneContext sources.");
            }

            VehicleWearZoneAdapter adapter = vehicleObject.GetComponent<VehicleWearZoneAdapter>() ??
                                             vehicleObject.AddComponent<VehicleWearZoneAdapter>();
            SerializedObject serialized = new SerializedObject(adapter);
            SetObject(serialized, "vehicleController", vehicle);
            SerializedProperty zones = serialized.FindProperty("zones");
            if (zones == null)
            {
                throw new InvalidOperationException("VehicleWearZoneAdapter.zones is unavailable.");
            }

            zones.arraySize = wearZones.Length;
            for (int i = 0; i < wearZones.Length; i++)
            {
                zones.GetArrayElementAtIndex(i).objectReferenceValue = wearZones[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(adapter);
        }

        private static void UpgradeDrivingCluster(DrivingHud hud)
        {
            if (hud.SpeedValueText == null || hud.SpeedUnitText == null)
            {
                throw new InvalidOperationException("DrivingHud speed references are required before Phase 7 cluster generation.");
            }

            Transform panel = hud.SpeedValueText.transform.parent;
            RectTransform panelRect = panel as RectTransform;
            if (panelRect == null)
            {
                throw new InvalidOperationException("DrivingHud speed panel RectTransform is missing.");
            }

            panelRect.sizeDelta = new Vector2(300f, 205f);
            Image background = panel.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = MobileUiTheme.ChamferedRectSprite;
                background.type = Image.Type.Sliced;
                background.color = MobileUiTheme.Ink;
                background.raycastTarget = false;
            }

            HudPanel hudPanel = panel.GetComponent<HudPanel>() ?? panel.gameObject.AddComponent<HudPanel>();
            Image border = panel.Find("HudPanelBorder")?.GetComponent<Image>();
            hudPanel.ConfigurePresentation(background, border);
            EditorUtility.SetDirty(hudPanel);

            LayoutExistingSpeedReadout(hud, panel);
            RemoveChild(panel, "Phase7DamageMeter");
            RemoveChild(panel, "Phase7TractionMeter");

            MeterRefs damage = CreateMeter(panel, "Phase7DamageMeter", 0.29f, MobileUiTheme.Red);
            MeterRefs traction = CreateMeter(panel, "Phase7TractionMeter", 0.07f, MobileUiTheme.Cyan);

            SerializedObject serialized = new SerializedObject(hud);
            SetObject(serialized, "damageFill", damage.Fill);
            SetObject(serialized, "tractionFill", traction.Fill);
            SetObject(serialized, "damageText", damage.Label);
            SetObject(serialized, "tractionText", traction.Label);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
        }

        private static void LayoutExistingSpeedReadout(DrivingHud hud, Transform panel)
        {
            RectTransform value = hud.SpeedValueText.rectTransform;
            value.anchorMin = new Vector2(0f, 0.55f);
            value.anchorMax = new Vector2(0.72f, 1f);
            value.offsetMin = new Vector2(18f, 0f);
            value.offsetMax = new Vector2(0f, -8f);

            RectTransform unit = hud.SpeedUnitText.rectTransform;
            unit.anchorMin = new Vector2(0.72f, 0.55f);
            unit.anchorMax = new Vector2(1f, 1f);
            unit.offsetMin = new Vector2(2f, 0f);
            unit.offsetMax = new Vector2(-12f, -8f);

            TMP_Text speedLabel = panel.Find("SpeedLabel")?.GetComponent<TMP_Text>();
            if (speedLabel != null)
            {
                RectTransform rect = speedLabel.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.46f);
                rect.anchorMax = new Vector2(1f, 0.57f);
                rect.offsetMin = new Vector2(18f, 0f);
                rect.offsetMax = new Vector2(-12f, 0f);
                speedLabel.color = MobileUiTheme.Amber;
                speedLabel.raycastTarget = false;
            }
        }

        private static MeterRefs CreateMeter(Transform panel, string name, float anchorY, Color fillColor)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(panel, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, anchorY);
            rootRect.anchorMax = new Vector2(1f, anchorY + 0.16f);
            rootRect.offsetMin = new Vector2(MobileUiTheme.BorderPadding + 8f, 0f);
            rootRect.offsetMax = new Vector2(-(MobileUiTheme.BorderPadding + 8f), 0f);

            GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trackObject.transform.SetParent(root.transform, false);
            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0.42f);
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            Image track = trackObject.GetComponent<Image>();
            track.sprite = MobileUiTheme.RoundedRectSprite;
            track.type = Image.Type.Sliced;
            track.color = MobileUiTheme.InkSoft;
            track.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(trackObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            Image fill = fillObject.GetComponent<Image>();
            fill.sprite = MobileUiTheme.RoundedRectSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            fill.color = fillColor;
            fill.raycastTarget = false;

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 13f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.color = MobileUiTheme.White;
            label.raycastTarget = false;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.42f);
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return new MeterRefs(fill, label);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => string.Equals(root.name, name, StringComparison.Ordinal));
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static void SetObject(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized object property '{name}'.");
            property.objectReferenceValue = value;
        }

        private static void SetInt(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized int property '{name}'.");
            property.intValue = value;
        }

        private readonly struct MeterRefs
        {
            public MeterRefs(Image fill, TMP_Text label)
            {
                Fill = fill;
                Label = label;
            }

            public Image Fill { get; }
            public TMP_Text Label { get; }
        }
    }
}
