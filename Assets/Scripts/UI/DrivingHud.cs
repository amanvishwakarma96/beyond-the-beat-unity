using BeyondTheBeat.Vehicle;
using TMPro;
using UnityEngine;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    public sealed class DrivingHud : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private TMP_Text speedValueText;
        [SerializeField] private TMP_Text speedUnitText;
        [SerializeField] private HudPanel speedPanel;

        private int displayedSpeed = int.MinValue;

        public VehicleController VehicleController => vehicleController;
        public TMP_Text SpeedValueText => speedValueText;
        public TMP_Text SpeedUnitText => speedUnitText;

        private void OnEnable()
        {
            ResolveSpeedPanel();
            speedPanel?.Show();
            Refresh(force: true);
        }

        private void Update()
        {
            Refresh(force: false);
        }

        public void SetSource(VehicleController controller)
        {
            vehicleController = controller;
            Refresh(force: true);
        }

        private void Refresh(bool force)
        {
            int speed = vehicleController != null
                ? Mathf.Max(0, Mathf.RoundToInt(vehicleController.CurrentSpeedKph))
                : 0;

            if (!force && speed == displayedSpeed)
            {
                return;
            }

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

        private void ResolveSpeedPanel()
        {
            if (speedPanel != null || speedValueText == null)
            {
                return;
            }

            speedPanel = speedValueText.GetComponentInParent<HudPanel>();
        }
    }
}
