using BeyondTheBeat.Performance;
using UnityEngine;
using UnityEngine.UI;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    public sealed class PerformanceDiagnosticsOverlay : MonoBehaviour
    {
        [SerializeField] private MobilePerformanceMonitor monitor;
        [SerializeField] private Text metricsText;

        public MobilePerformanceMonitor Monitor => monitor;
        public Text MetricsText => metricsText;

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (monitor != null)
            {
                monitor.Sampled -= HandleSampled;
                monitor.Sampled += HandleSampled;
            }
        }

        private void OnDisable()
        {
            if (monitor != null)
            {
                monitor.Sampled -= HandleSampled;
            }
        }

        public void SetSource(MobilePerformanceMonitor source)
        {
            if (monitor == source)
            {
                return;
            }

            if (isActiveAndEnabled && monitor != null)
            {
                monitor.Sampled -= HandleSampled;
            }

            monitor = source;

            if (isActiveAndEnabled && monitor != null)
            {
                monitor.Sampled -= HandleSampled;
                monitor.Sampled += HandleSampled;
            }
        }

        private void HandleSampled(MobilePerformanceSnapshot snapshot)
        {
            if (metricsText == null)
            {
                return;
            }

            metricsText.text =
                $"FPS {snapshot.AverageFps:0}  FRAME {snapshot.AverageFrameTimeMilliseconds:0.0} ms\n" +
                $"MEM {snapshot.AllocatedMemoryMb:0} MB  {snapshot.WarningFlags}";
        }
    }
}
