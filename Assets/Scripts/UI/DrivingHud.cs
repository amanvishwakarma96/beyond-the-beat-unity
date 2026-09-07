using BeyondTheBeat.Vehicle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    public sealed class DrivingHud : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private TMP_Text speedValueText;
        [SerializeField] private TMP_Text speedUnitText;
        [SerializeField] private HudPanel speedPanel;

        [Header("Phase 7 Digital Cluster")]
        [SerializeField] private Image damageFill;
        [SerializeField] private Image tractionFill;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text tractionText;

        private int displayedSpeed = int.MinValue;
        private int displayedDamage = int.MinValue;
        private int displayedTraction = int.MinValue;

        public VehicleController VehicleController => vehicleController;
        public TMP_Text SpeedValueText => speedValueText;
        public TMP_Text SpeedUnitText => speedUnitText;
        public Image DamageFill => damageFill;
        public Image TractionFill => tractionFill;
        public TMP_Text DamageText => damageText;
        public TMP_Text TractionText => tractionText;

        private void OnEnable()
        {
            ResolveSpeedPanel();
            speedPanel?.Show();
            SubscribeCondition();
            Refresh(force: true);
        }

        private void OnDisable()
        {
            UnsubscribeCondition();
        }

        private void Update()
        {
            Refresh(force: false);
        }

        public void SetSource(VehicleController controller)
        {
            UnsubscribeCondition();
            vehicleController = controller;
            if (isActiveAndEnabled)
            {
                SubscribeCondition();
            }
            Refresh(force: true);
        }

        private void Refresh(bool force)
        {
            int speed = vehicleController != null
                ? Mathf.Max(0, Mathf.RoundToInt(vehicleController.CurrentSpeedKph))
                : 0;
            int damage = vehicleController != null
                ? Mathf.RoundToInt((1f - vehicleController.HealthCondition) * 100f)
                : 0;
            int traction = vehicleController != null
                ? Mathf.RoundToInt(vehicleController.TireTraction01 * 100f)
                : 100;

            bool speedChanged = force || speed != displayedSpeed;
            bool conditionChanged = force || damage != displayedDamage || traction != displayedTraction;
            if (!speedChanged && !conditionChanged)
            {
                return;
            }

            if (speedChanged)
            {
                displayedSpeed = speed;
                if (speedValueText != null)
                {
                    speedValueText.text = speed.ToString("000");
                }
                if (speedUnitText != null && speedUnitText.text != "KM/H")
                {
                    speedUnitText.text = "KM/H";
                }
            }

            if (!conditionChanged)
            {
                return;
            }

            displayedDamage = damage;
            displayedTraction = traction;

            if (damageFill != null)
            {
                damageFill.fillAmount = damage / 100f;
            }
            if (tractionFill != null)
            {
                tractionFill.fillAmount = traction / 100f;
            }
            if (damageText != null)
            {
                damageText.text = $"DAMAGE  {damage}%";
            }
            if (tractionText != null)
            {
                tractionText.text = $"TRACTION  {GetTractionStatus(traction)}  {traction}%";
            }
        }

        private void HandleConditionChanged(VehicleController source, float health, float wear)
        {
            Refresh(force: true);
        }

        private void SubscribeCondition()
        {
            if (vehicleController == null)
            {
                return;
            }

            vehicleController.ConditionChanged -= HandleConditionChanged;
            vehicleController.ConditionChanged += HandleConditionChanged;
        }

        private void UnsubscribeCondition()
        {
            if (vehicleController != null)
            {
                vehicleController.ConditionChanged -= HandleConditionChanged;
            }
        }

        private void ResolveSpeedPanel()
        {
            if (speedPanel != null || speedValueText == null)
            {
                return;
            }

            speedPanel = speedValueText.GetComponentInParent<HudPanel>();
        }

        private static string GetTractionStatus(int tractionPercent)
        {
            if (tractionPercent >= 70)
            {
                return "HIGH";
            }
            if (tractionPercent >= 40)
            {
                return "MED";
            }
            return "LOW";
        }
    }
}
