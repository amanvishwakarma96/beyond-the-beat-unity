using System;
using UnityEngine;

namespace BeyondTheBeat.Performance
{
    [DisallowMultipleComponent]
    public sealed class MobileQualityBootstrap : MonoBehaviour
    {
        [SerializeField] private MobileQualityProfile profile;
        [SerializeField] private Camera gameplayCamera;

        public MobileQualityProfile Profile => profile;
        public Camera GameplayCamera => gameplayCamera;

        private void Awake()
        {
            ApplyNow();
        }

        public void ApplyNow()
        {
            ApplyProfile(profile, gameplayCamera);
        }

        public static void ApplyProfile(MobileQualityProfile qualityProfile, Camera camera)
        {
            if (qualityProfile == null || !qualityProfile.IsConfigured)
            {
                throw new InvalidOperationException("MobileQualityBootstrap requires a configured MobileQualityProfile.");
            }

            QualitySettings.shadowDistance = qualityProfile.ShadowDistance;
            QualitySettings.shadowCascades = qualityProfile.ShadowCascades;
            QualitySettings.shadows = qualityProfile.ShadowQuality;
            QualitySettings.shadowResolution = qualityProfile.ShadowResolution;
            QualitySettings.antiAliasing = qualityProfile.AntiAliasing;
            QualitySettings.lodBias = qualityProfile.LodBias;
            QualitySettings.realtimeReflectionProbes = qualityProfile.RealtimeReflectionProbes;
            QualitySettings.softParticles = qualityProfile.SoftParticles;

            if (camera != null)
            {
                camera.allowHDR = qualityProfile.CameraHdr;
                camera.allowMSAA = qualityProfile.CameraMsaa;
            }
        }
    }
}
