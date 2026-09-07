using System;
using BeyondTheBeat.Economy;
using BeyondTheBeat.Interaction;
using BeyondTheBeat.Persistence;
using BeyondTheBeat.Vehicle;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    public static class Phase7IntegrationFastValidation
    {
        public static void Validate()
        {
            // Preserve every already-green Phase 5/6 contract before validating the new systemic loop.
            Phase6PerformanceFastValidation.Validate();

            ValidateVehicleDegradationMath();
            ValidatePaidRepairFlow();
            ValidatePersistenceRoundTrip();

            Debug.Log(
                "[Beyond The Beat] FAST PR VALIDATION PASS: Phase 7 vehicle wear/degradation, partial/full credit-funded repairs and vehicle-condition persistence passed with all Phase 5/6 contracts.");
        }

        private static void ValidateVehicleDegradationMath()
        {
            float healthyAcceleration = VehicleController.EvaluateAccelerationMultiplier(1f);
            float damagedAcceleration = VehicleController.EvaluateAccelerationMultiplier(0.25f);
            float healthyBrakes = VehicleController.EvaluateBrakeMultiplier(1f, 0f);
            float wornBrakes = VehicleController.EvaluateBrakeMultiplier(0.35f, 0.8f);
            float freshTraction = VehicleController.EvaluateTractionMultiplier(0f);
            float wornTraction = VehicleController.EvaluateTractionMultiplier(0.8f);
            float belowThresholdWear = VehicleController.EvaluateOffRoadWearIntensity(40f, 45f);
            float highSpeedWear = VehicleController.EvaluateOffRoadWearIntensity(90f, 45f);
            float mildCollision = VehicleController.EvaluateCollisionHealthDamage(6f, 8f, 0.018f);
            float severeCollision = VehicleController.EvaluateCollisionHealthDamage(20f, 8f, 0.018f);

            bool pass = healthyAcceleration > damagedAcceleration &&
                        healthyBrakes > wornBrakes &&
                        freshTraction > wornTraction &&
                        Mathf.Approximately(belowThresholdWear, 0f) &&
                        highSpeedWear > 0f &&
                        Mathf.Approximately(mildCollision, 0f) &&
                        severeCollision > 0f;

            if (!pass)
            {
                throw new InvalidOperationException(
                    "Phase 7 degradation math validation failed: " +
                    $"accel={healthyAcceleration:0.###}/{damagedAcceleration:0.###}, brakes={healthyBrakes:0.###}/{wornBrakes:0.###}, " +
                    $"traction={freshTraction:0.###}/{wornTraction:0.###}, offRoad={belowThresholdWear:0.###}/{highSpeedWear:0.###}, " +
                    $"collision={mildCollision:0.###}/{severeCollision:0.###}.");
            }
        }

        private static void ValidatePaidRepairFlow()
        {
            GameObject vehicleObject = null;
            GameObject stationObject = null;
            GameObject walletObject = null;
            GameObject actor = null;

            try
            {
                vehicleObject = new GameObject("Phase7ValidationVehicle");
                vehicleObject.AddComponent<Rigidbody>();
                VehicleController vehicle = vehicleObject.AddComponent<VehicleController>();
                RepairableState repairable = vehicleObject.AddComponent<RepairableState>();
                vehicle.RestoreCondition(0.5f, 0.5f);
                repairable.SetDamage01(vehicle.ServiceNeed01);

                walletObject = new GameObject("Phase7ValidationWallet");
                CreditWallet wallet = walletObject.AddComponent<CreditWallet>();
                wallet.SetBalance(100);

                stationObject = new GameObject("Phase7ValidationRepairStation");
                BoxCollider collider = stationObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                stationObject.AddComponent<InteractionTrigger>();
                RepairStation station = stationObject.AddComponent<RepairStation>();

                SerializedObject serialized = new SerializedObject(station);
                SetObject(serialized, "target", repairable);
                SetObject(serialized, "vehicle", vehicle);
                SetObject(serialized, "wallet", wallet);
                SetInt(serialized, "fullHealthRepairCost", 300);
                SetInt(serialized, "fullTireRepairCost", 200);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                actor = new GameObject("Phase7ValidationActor");
                int fullRepairEvents = 0;
                station.RepairCompleted += (source, completedTarget, completedActor, repairCount) => fullRepairEvents++;

                int initialCost = station.CurrentRepairCost;
                bool firstStarted = station.RequestInteraction(actor);
                bool firstCompleted = firstStarted && station.AdvanceActivity(station.DurationSeconds + 0.1f);
                bool partialPass = initialCost == 250 &&
                                   firstCompleted &&
                                   wallet.Balance == 0 &&
                                   Mathf.Abs(vehicle.HealthCondition - 0.7f) < 0.001f &&
                                   Mathf.Abs(vehicle.TireWear - 0.3f) < 0.001f &&
                                   vehicle.NeedsService &&
                                   fullRepairEvents == 0;

                wallet.AddCredits(150);
                int remainingCost = station.CurrentRepairCost;
                bool secondStarted = station.RequestInteraction(actor);
                bool secondCompleted = secondStarted && station.AdvanceActivity(station.DurationSeconds + 0.1f);
                bool fullPass = remainingCost == 150 &&
                                secondCompleted &&
                                wallet.Balance == 0 &&
                                Mathf.Approximately(vehicle.HealthCondition, 1f) &&
                                Mathf.Approximately(vehicle.TireWear, 0f) &&
                                !vehicle.NeedsService &&
                                !repairable.NeedsRepair &&
                                fullRepairEvents == 1;

                if (!partialPass || !fullPass)
                {
                    throw new InvalidOperationException(
                        "Phase 7 paid repair validation failed: " +
                        $"initialCost={initialCost}, partial={partialPass}, remainingCost={remainingCost}, full={fullPass}, " +
                        $"balance={wallet.Balance}, health={vehicle.HealthCondition:0.###}, tireWear={vehicle.TireWear:0.###}, events={fullRepairEvents}.");
                }
            }
            finally
            {
                DestroyImmediateSafe(actor);
                DestroyImmediateSafe(stationObject);
                DestroyImmediateSafe(walletObject);
                DestroyImmediateSafe(vehicleObject);
            }
        }

        private static void ValidatePersistenceRoundTrip()
        {
            GameSaveData source = new GameSaveData
            {
                Version = SaveManager.CurrentVersion,
                SceneId = "Phase5_Ocean",
                HasPhase7VehicleConditionState = true,
                VehicleHealthCondition = 0.42f,
                VehicleTireWear = 0.63f
            };

            string json = SaveManager.SerializeForStorage(source);
            SaveLoadResult result = SaveManager.DeserializeForStorage(json, out GameSaveData restored);
            bool roundTripPass = result == SaveLoadResult.Success &&
                                 restored != null &&
                                 restored.HasPhase7VehicleConditionState &&
                                 Mathf.Abs(restored.VehicleHealthCondition - 0.42f) < 0.0001f &&
                                 Mathf.Abs(restored.VehicleTireWear - 0.63f) < 0.0001f;

            const string legacyJson = "{\"Version\":1,\"SceneId\":\"Phase5_Ocean\"}";
            SaveLoadResult legacyResult = SaveManager.DeserializeForStorage(legacyJson, out GameSaveData legacy);
            bool legacyPass = legacyResult == SaveLoadResult.Success &&
                              legacy != null &&
                              !legacy.HasPhase7VehicleConditionState &&
                              Mathf.Approximately(legacy.VehicleHealthCondition, 1f) &&
                              Mathf.Approximately(legacy.VehicleTireWear, 0f);

            if (!roundTripPass || !legacyPass)
            {
                throw new InvalidOperationException(
                    $"Phase 7 persistence validation failed: roundTrip={roundTripPass}, legacyFallback={legacyPass}, result={result}/{legacyResult}.");
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

        private static void DestroyImmediateSafe(UnityEngine.Object target)
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
