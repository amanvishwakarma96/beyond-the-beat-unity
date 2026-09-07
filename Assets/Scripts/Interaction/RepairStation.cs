using System;
using BeyondTheBeat.Economy;
using BeyondTheBeat.Vehicle;
using UnityEngine;

namespace BeyondTheBeat.Interaction
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(InteractionTrigger))]
    public sealed class RepairStation : TimedActivityInteractable
    {
        [Header("Repair")]
        [SerializeField] private RepairableState target;
        [SerializeField] private string activityLabel = "Repair vehicle";

        [Header("Phase 7 Economy Integration")]
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private CreditWallet wallet;
        [SerializeField, Min(1)] private int fullHealthRepairCost = 300;
        [SerializeField, Min(1)] private int fullTireRepairCost = 200;

        private int repairsCompleted;

        public RepairableState Target => target;
        public string ActivityLabel => activityLabel;
        public int RepairsCompleted => repairsCompleted;
        public VehicleController Vehicle => vehicle;
        public CreditWallet Wallet => wallet;
        public int CurrentRepairCost => vehicle != null
            ? CalculateRepairCost(vehicle.HealthCondition, vehicle.TireWear, fullHealthRepairCost, fullTireRepairCost)
            : 0;

        public event Action<RepairStation, RepairableState, GameObject, int> RepairCompleted;
        public event Action<RepairStation, GameObject, int, int, bool> PaidRepairCompleted;

        private void Awake()
        {
            ResolveIntegratedSources();
            InitializeVehicleFromLegacyRepairState();
        }

        private void OnEnable()
        {
            ResolveIntegratedSources();
            if (vehicle != null)
            {
                vehicle.ConditionChanged -= HandleVehicleConditionChanged;
                vehicle.ConditionChanged += HandleVehicleConditionChanged;
            }
            SyncRepairableFromVehicle();
        }

        protected override void OnDisable()
        {
            if (vehicle != null)
            {
                vehicle.ConditionChanged -= HandleVehicleConditionChanged;
            }
            base.OnDisable();
        }

        protected override bool CanInteract(GameObject actor)
        {
            ResolveIntegratedSources();
            bool needsRepair = vehicle != null ? vehicle.NeedsService : target != null && target.NeedsRepair;
            return base.CanInteract(actor) && target != null && needsRepair;
        }

        protected override void OnActivityCompleted(GameObject actor)
        {
            ResolveIntegratedSources();

            // Historical Phase 4 validators and scenes without an economy source keep their
            // original full-repair behavior. Phase 7 integrated scenes always wire vehicle+wallet.
            if (vehicle == null || wallet == null)
            {
                if (target == null || !target.RepairFully())
                {
                    return;
                }

                repairsCompleted++;
                RepairCompleted?.Invoke(this, target, actor, repairsCompleted);
                return;
            }

            int repairCost = CurrentRepairCost;
            if (repairCost <= 0)
            {
                SyncRepairableFromVehicle();
                return;
            }

            int spend = Mathf.Min(wallet.Balance, repairCost);
            if (spend <= 0 || !wallet.TrySpend(spend))
            {
                SyncRepairableFromVehicle();
                PaidRepairCompleted?.Invoke(this, actor, 0, repairCost, false);
                return;
            }

            float repairFraction = CalculateRepairFraction(spend, repairCost);
            float missingHealth = 1f - vehicle.HealthCondition;
            float wornTires = vehicle.TireWear;
            vehicle.RepairCondition(missingHealth * repairFraction, wornTires * repairFraction);

            bool fullyRepaired = !vehicle.NeedsService;
            SyncRepairableFromVehicle();
            PaidRepairCompleted?.Invoke(this, actor, spend, repairCost, fullyRepaired);

            if (!fullyRepaired)
            {
                return;
            }

            repairsCompleted++;
            RepairCompleted?.Invoke(this, target, actor, repairsCompleted);
        }

        public static int CalculateRepairCost(
            float healthCondition,
            float tireWear,
            int fullHealthCost,
            int fullTireCost)
        {
            float healthCost = (1f - Mathf.Clamp01(healthCondition)) * Mathf.Max(0, fullHealthCost);
            float tireCost = Mathf.Clamp01(tireWear) * Mathf.Max(0, fullTireCost);
            return Mathf.CeilToInt(healthCost + tireCost);
        }

        public static float CalculateRepairFraction(int availableCredits, int totalRepairCost)
        {
            if (availableCredits <= 0 || totalRepairCost <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01(availableCredits / (float)totalRepairCost);
        }

        private void ResolveIntegratedSources()
        {
            if (vehicle == null && target != null)
            {
                vehicle = target.GetComponent<VehicleController>();
            }
        }

        private void InitializeVehicleFromLegacyRepairState()
        {
            if (vehicle == null || target == null || !target.NeedsRepair)
            {
                return;
            }

            bool vehicleStillPristine = vehicle.HealthCondition >= 0.9999f && vehicle.TireWear <= 0.0001f;
            if (vehicleStillPristine)
            {
                vehicle.RestoreCondition(target.Condition01, 0f);
            }
        }

        private void HandleVehicleConditionChanged(VehicleController source, float health, float wear)
        {
            SyncRepairableFromVehicle();
        }

        private void SyncRepairableFromVehicle()
        {
            if (target == null || vehicle == null)
            {
                return;
            }

            target.SetDamage01(vehicle.ServiceNeed01);
        }
    }
}
