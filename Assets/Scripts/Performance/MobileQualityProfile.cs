using UnityEngine;

namespace BeyondTheBeat.Performance
{
    [CreateAssetMenu(fileName = "MobileQualityProfile", menuName = "Beyond The Beat/Performance/Mobile Quality Profile")]
    public sealed class MobileQualityProfile : ScriptableObject
    {
        [Header("Shadows")]
        [SerializeField, Min(0f)] private float shadowDistance = 35f;
        [SerializeField] private int shadowCascades = 2;
        [SerializeField] private ShadowQuality shadowQuality = ShadowQuality.HardOnly;
        [SerializeField] private ShadowResolution shadowResolution = ShadowResolution.Medium;

        [Header("Raster Quality")]
        [SerializeField] private int antiAliasing = 2;
        [SerializeField, Min(0.1f)] private float lodBias = 0.8f;
        [SerializeField] private bool realtimeReflectionProbes;
        [SerializeField] private bool softParticles;

        [Header("Gameplay Camera")]
        [SerializeField] private bool cameraHdr;
        [SerializeField] private bool cameraMsaa = true;

        public float ShadowDistance => shadowDistance;
        public int ShadowCascades => shadowCascades;
        public ShadowQuality ShadowQuality => shadowQuality;
        public ShadowResolution ShadowResolution => shadowResolution;
        public int AntiAliasing => antiAliasing;
        public float LodBias => lodBias;
        public bool RealtimeReflectionProbes => realtimeReflectionProbes;
        public bool SoftParticles => softParticles;
        public bool CameraHdr => cameraHdr;
        public bool CameraMsaa => cameraMsaa;

        public bool IsConfigured =>
            shadowDistance >= 0f &&
            (shadowCascades == 0 || shadowCascades == 2 || shadowCascades == 4) &&
            (antiAliasing == 0 || antiAliasing == 2 || antiAliasing == 4 || antiAliasing == 8) &&
            lodBias > 0f;
    }
}
