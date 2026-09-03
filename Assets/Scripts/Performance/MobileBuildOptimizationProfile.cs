using UnityEngine;

namespace BeyondTheBeat.Performance
{
    public enum MobileManagedStrippingPolicy
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    [CreateAssetMenu(
        fileName = "MobileBuildOptimizationProfile",
        menuName = "Beyond The Beat/Performance/Mobile Build Optimization Profile")]
    public sealed class MobileBuildOptimizationProfile : ScriptableObject
    {
        [SerializeField] private bool stripEngineCode = true;
        [SerializeField] private MobileManagedStrippingPolicy managedStripping = MobileManagedStrippingPolicy.Low;
        [SerializeField] private bool useLz4HcCompression = true;
        [SerializeField, Range(5, 50)] private int buildReportTopFileCount = 15;
        [SerializeField] private bool preserveCurrentAndroidArchitectures = true;

        public bool StripEngineCode => stripEngineCode;
        public MobileManagedStrippingPolicy ManagedStripping => managedStripping;
        public bool UseLz4HcCompression => useLz4HcCompression;
        public int BuildReportTopFileCount => buildReportTopFileCount;
        public bool PreserveCurrentAndroidArchitectures => preserveCurrentAndroidArchitectures;

        public bool IsConfigured =>
            stripEngineCode &&
            buildReportTopFileCount >= 5 &&
            buildReportTopFileCount <= 50 &&
            preserveCurrentAndroidArchitectures;
    }
}
