using UnityEngine;
using UnityEngine.UI;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    public sealed class InputDiagnosticsHud : MonoBehaviour
    {
        [SerializeField] private MobileDrivingInput mobileInput;
        [SerializeField] private Text diagnosticsText;
        [SerializeField] private GameObject diagnosticsRoot;

        public MobileDrivingInput MobileInput => mobileInput;
        public Text DiagnosticsText => diagnosticsText;

        private void Awake()
        {
            if (!Debug.isDebugBuild && diagnosticsRoot != null)
            {
                diagnosticsRoot.SetActive(false);
            }
        }

        private void Update()
        {
            if (!Debug.isDebugBuild || diagnosticsText == null)
            {
                return;
            }

            diagnosticsText.text = mobileInput != null
                ? mobileInput.DiagnosticSummary
                : "INPUT VEH:MISS / MobileDrivingInput missing";
        }
    }
}
