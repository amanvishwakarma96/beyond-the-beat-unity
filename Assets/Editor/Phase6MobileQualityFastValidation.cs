using System;
using System.Reflection;
using BeyondTheBeat.Performance;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6MobileQualityFastValidation
    {
        public static void ValidateQualityOnly()
        {
            MobileQualityProfile profile = ScriptableObject.CreateInstance<MobileQualityProfile>();
            GameObject cameraObject = new GameObject("FastMobileQualityCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();

            float previousShadowDistance = QualitySettings.shadowDistance;
            int previousShadowCascades = QualitySettings.shadowCascades;
            ShadowQuality previousShadows = QualitySettings.shadows;
            ShadowResolution previousShadowResolution = QualitySettings.shadowResolution;
            int previousAntiAliasing = QualitySettings.antiAliasing;
            float previousLodBias = QualitySettings.lodBias;
            bool previousRealtimeReflectionProbes = QualitySettings.realtimeReflectionProbes;
            bool previousSoftParticles = QualitySettings.softParticles;

            try
            {
                MobileQualityBootstrap.ApplyProfile(profile, camera);

                bool profilePass = profile.IsConfigured &&
                                   Mathf.Approximately(profile.ShadowDistance, 35f) &&
                                   profile.ShadowCascades == 2 &&
                                   profile.ShadowQuality == ShadowQuality.HardOnly &&
                                   profile.ShadowResolution == ShadowResolution.Medium &&
                                   profile.AntiAliasing == 2 &&
                                   Mathf.Approximately(profile.LodBias, 0.8f) &&
                                   !profile.RealtimeReflectionProbes &&
                                   !profile.SoftParticles &&
                                   !profile.CameraHdr &&
                                   profile.CameraMsaa;

                bool appliedPass = Mathf.Approximately(QualitySettings.shadowDistance, profile.ShadowDistance) &&
                                   QualitySettings.shadowCascades == profile.ShadowCascades &&
                                   QualitySettings.shadows == profile.ShadowQuality &&
                                   QualitySettings.shadowResolution == profile.ShadowResolution &&
                                   QualitySettings.antiAliasing == profile.AntiAliasing &&
                                   Mathf.Approximately(QualitySettings.lodBias, profile.LodBias) &&
                                   QualitySettings.realtimeReflectionProbes == profile.RealtimeReflectionProbes &&
                                   QualitySettings.softParticles == profile.SoftParticles &&
                                   camera.allowHDR == profile.CameraHdr &&
                                   camera.allowMSAA == profile.CameraMsaa;

                bool oneShotPass = typeof(MobileQualityBootstrap).GetMethod(
                                           "Update",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null &&
                                       typeof(MobileQualityBootstrap).GetMethod(
                                           "LateUpdate",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null &&
                                       typeof(MobileQualityBootstrap).GetMethod(
                                           "FixedUpdate",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null;

                if (!profilePass || !appliedPass || !oneShotPass)
                {
                    throw new InvalidOperationException(
                        $"Phase 6 mobile quality fast validation failed: profile={profilePass}, applied={appliedPass}, oneShot={oneShotPass}.");
                }
            }
            finally
            {
                QualitySettings.shadowDistance = previousShadowDistance;
                QualitySettings.shadowCascades = previousShadowCascades;
                QualitySettings.shadows = previousShadows;
                QualitySettings.shadowResolution = previousShadowResolution;
                QualitySettings.antiAliasing = previousAntiAliasing;
                QualitySettings.lodBias = previousLodBias;
                QualitySettings.realtimeReflectionProbes = previousRealtimeReflectionProbes;
                QualitySettings.softParticles = previousSoftParticles;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
