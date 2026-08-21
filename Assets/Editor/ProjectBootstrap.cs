using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BeyondTheBeat.Editor
{
    [InitializeOnLoad]
    internal static class ProjectBootstrap
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string PipelineAssetPath = SettingsFolder + "/BeyondTheBeat_MobileURP.asset";
        private const string RendererAssetPath = SettingsFolder + "/BeyondTheBeat_MobileRenderer.asset";

        static ProjectBootstrap()
        {
            EditorApplication.delayCall += ConfigureProject;
        }

        [MenuItem("Beyond The Beat/Project/Run Bootstrap")]
        private static void ConfigureProject()
        {
            EnsureSettingsFolder();
            UniversalRenderPipelineAsset pipelineAsset = EnsureUrpAssets();
            ConfigureRenderPipeline(pipelineAsset);
            ConfigureMobilePlayerSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Beyond The Beat] Project bootstrap completed.");
        }

        [MenuItem("Beyond The Beat/Project/Validate Bootstrap")]
        private static void ValidateBootstrap()
        {
            bool editorVersionMatches = Application.unityVersion.StartsWith("2022.3.62f1", StringComparison.Ordinal);
            bool urpAssigned = GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset;
            bool landscapeOnly =
                PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation &&
                PlayerSettings.allowedAutorotateToLandscapeLeft &&
                PlayerSettings.allowedAutorotateToLandscapeRight &&
                !PlayerSettings.allowedAutorotateToPortrait &&
                !PlayerSettings.allowedAutorotateToPortraitUpsideDown;

            string message =
                $"[Beyond The Beat] Bootstrap validation\n" +
                $"Unity version: {(editorVersionMatches ? "PASS" : "FAIL")} ({Application.unityVersion})\n" +
                $"URP assigned: {(urpAssigned ? "PASS" : "FAIL")}\n" +
                $"Landscape configuration: {(landscapeOnly ? "PASS" : "FAIL")}\n" +
                $"Product name: {PlayerSettings.productName}\n" +
                $"Android application id: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)}";

            if (editorVersionMatches && urpAssigned && landscapeOnly)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
        }

        private static UniversalRenderPipelineAsset EnsureUrpAssets()
        {
            UniversalRenderPipelineAsset existingPipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);

            if (existingPipeline != null)
            {
                return existingPipeline;
            }

            UniversalRenderPipelineAsset pipelineAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            pipelineAsset.name = "BeyondTheBeat_MobileURP";
            pipelineAsset.renderScale = 1.0f;
            pipelineAsset.msaaSampleCount = 2;
            pipelineAsset.supportsHDR = false;
            pipelineAsset.supportsCameraDepthTexture = false;
            pipelineAsset.supportsCameraOpaqueTexture = false;
            pipelineAsset.useSRPBatcher = true;

            ScriptableRendererData builtinRenderer =
                pipelineAsset.LoadBuiltinRendererData(RendererType.UniversalRenderer);

            if (builtinRenderer == null)
            {
                UnityEngine.Object.DestroyImmediate(pipelineAsset);
                throw new InvalidOperationException("Unable to create the built-in URP renderer data.");
            }

            ScriptableRendererData rendererData = UnityEngine.Object.Instantiate(builtinRenderer);
            rendererData.name = "BeyondTheBeat_MobileRenderer";
            AssetDatabase.CreateAsset(rendererData, RendererAssetPath);

            SerializedObject serializedPipeline = new SerializedObject(pipelineAsset);
            SerializedProperty rendererDataList = serializedPipeline.FindProperty("m_RendererDataList");
            SerializedProperty defaultRendererIndex = serializedPipeline.FindProperty("m_DefaultRendererIndex");

            if (rendererDataList == null || defaultRendererIndex == null)
            {
                UnityEngine.Object.DestroyImmediate(pipelineAsset);
                AssetDatabase.DeleteAsset(RendererAssetPath);
                throw new InvalidOperationException("URP 14 renderer serialization layout was not found.");
            }

            rendererDataList.arraySize = 1;
            rendererDataList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            defaultRendererIndex.intValue = 0;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);
            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(pipelineAsset);

            return pipelineAsset;
        }

        private static void ConfigureRenderPipeline(UniversalRenderPipelineAsset pipelineAsset)
        {
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;
        }

        private static void ConfigureMobilePlayerSettings()
        {
            PlayerSettings.companyName = "Beyond The Beat";
            PlayerSettings.productName = "Beyond The Beat";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.beyondthebeat.prototype");

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = false;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        }
    }
}
