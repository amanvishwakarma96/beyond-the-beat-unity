using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6StoreAssetsFastValidation
    {
        private const string ListingPath = "Docs/Store/PLAY_STORE_LISTING.md";
        private const string ValidationDocPath = "Docs/Validation/PHASE_6_STORE_ASSETS.md";
        private const string FullBuildPath = "Assets/Editor/Phase6BuildAutomation.cs";
        private const string WorkflowPath = ".github/workflows/phase2-forest-foundation.yml";

        public static void ValidateStoreAssetsOnly()
        {
            bool constantsPass =
                Phase6StoreAssetsBuilder.StoreIconWidth == 512 &&
                Phase6StoreAssetsBuilder.StoreIconHeight == 512 &&
                Phase6StoreAssetsBuilder.StoreIconMaxBytes == 1024 * 1024 &&
                Phase6StoreAssetsBuilder.FeatureGraphicWidth == 1024 &&
                Phase6StoreAssetsBuilder.FeatureGraphicHeight == 500 &&
                string.Equals(Phase6StoreAssetsBuilder.AppTitle, "Beyond The Beat", StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(Phase6StoreAssetsBuilder.ShortDescription) &&
                Phase6StoreAssetsBuilder.ShortDescription.Length <= 80;

            bool renderPass = ValidateDeterministicRenderers();
            bool listingPass = Phase6StoreAssetsBuilder.IsListingCopyConfigured();
            bool repositoryPass = ValidateRepositoryContracts();

            if (!constantsPass || !renderPass || !listingPass || !repositoryPass)
            {
                throw new InvalidOperationException(
                    $"Phase 6 fast store-assets validation failed: constants={constantsPass}, render={renderPass}, listing={listingPass}, repository={repositoryPass}.");
            }

            Debug.Log(
                "[Beyond The Beat] Phase 6 FAST store-assets validation PASS: Play icon/feature dimensions, deterministic brand rendering, <=80-character listing copy, real-screenshot boundary and post-merge packaging contracts are intact.");
        }

        private static bool ValidateDeterministicRenderers()
        {
            Texture2D iconA = null;
            Texture2D iconB = null;
            Texture2D feature = null;
            try
            {
                iconA = Phase6StoreAssetsBuilder.RenderStoreIconForValidation(96);
                iconB = Phase6StoreAssetsBuilder.RenderStoreIconForValidation(96);
                feature = Phase6StoreAssetsBuilder.RenderFeatureGraphicForValidation(256, 125);

                ulong hashA = Phase6StoreAssetsBuilder.ComputePixelChecksum(iconA);
                ulong hashB = Phase6StoreAssetsBuilder.ComputePixelChecksum(iconB);
                bool deterministic = hashA != 0UL && hashA == hashB;
                bool iconConfigured = iconA.width == 96 && iconA.height == 96 &&
                                      iconA.GetPixels32().Any(pixel => pixel.r > 200 && pixel.g > 100 && pixel.b < 100) &&
                                      iconA.GetPixels32().Any(pixel => pixel.g > 180 && pixel.b > 180);
                bool featureConfigured = feature.width == 256 && feature.height == 125 &&
                                         feature.GetPixels32().All(pixel => pixel.a == 255) &&
                                         feature.GetPixels32().Any(pixel => pixel.r > 200 && pixel.g > 100 && pixel.b < 100);
                return deterministic && iconConfigured && featureConfigured;
            }
            finally
            {
                Destroy(iconA);
                Destroy(iconB);
                Destroy(feature);
            }
        }

        private static bool ValidateRepositoryContracts()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            string listingPath = Resolve(root, ListingPath);
            string validationPath = Resolve(root, ValidationDocPath);
            string buildPath = Resolve(root, FullBuildPath);
            string workflowPath = Resolve(root, WorkflowPath);
            if (!File.Exists(listingPath) || !File.Exists(validationPath) || !File.Exists(buildPath) || !File.Exists(workflowPath))
            {
                return false;
            }

            string listing = File.ReadAllText(listingPath);
            string validation = File.ReadAllText(validationPath);
            string build = File.ReadAllText(buildPath);
            string workflow = File.ReadAllText(workflowPath);

            return listing.Contains("actual Android build", StringComparison.OrdinalIgnoreCase) &&
                   listing.Contains("1920x1080", StringComparison.Ordinal) &&
                   validation.Contains("CI GREEN IS NOT STORE-PUBLISHING SIGN-OFF", StringComparison.Ordinal) &&
                   build.Contains("Phase6StoreAssetsBuilder.PrepareAndValidateOrThrow", StringComparison.Ordinal) &&
                   workflow.Contains("build/store-assets", StringComparison.Ordinal) &&
                   workflow.Contains("TEST-THIS-BUILD-${GITHUB_RUN_NUMBER}", StringComparison.Ordinal) &&
                   workflow.Contains("maximumApkSizeMb", StringComparison.Ordinal);
        }

        private static string Resolve(string root, string relativePath)
        {
            return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (value != null)
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}
