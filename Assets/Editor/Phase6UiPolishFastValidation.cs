using System;
using System.IO;
using System.Reflection;
using BeyondTheBeat.UI;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6UiPolishFastValidation
    {
        private const string ValidationDocPath = "Docs/Validation/PHASE_6_UI_POLISH.md";

        public static void ValidateUiPolishOnly()
        {
            Rect screen = new Rect(0f, 0f, 1920f, 1080f);
            Rect safe = new Rect(60f, 24f, 1800f, 1032f);

            MobileSafeAreaFitter.CalculateMappedAnchors(
                screen,
                safe,
                Vector2.zero,
                Vector2.one,
                out Vector2 fullMin,
                out Vector2 fullMax);

            bool fullSafeAreaPass =
                Approximately(fullMin, new Vector2(60f / 1920f, 24f / 1080f)) &&
                Approximately(fullMax, new Vector2(1860f / 1920f, 1056f / 1080f));

            MobileSafeAreaFitter.CalculateMappedAnchors(
                screen,
                safe,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                out Vector2 topCenterMin,
                out Vector2 topCenterMax);

            bool topCenterPass =
                Approximately(topCenterMin, new Vector2(0.5f, 1056f / 1080f)) &&
                Approximately(topCenterMax, topCenterMin);

            MobileSafeAreaFitter.CalculateMappedAnchors(
                screen,
                new Rect(100f, 100f, 0f, 0f),
                new Vector2(-0.5f, 1.5f),
                new Vector2(1.5f, -0.5f),
                out Vector2 invalidMin,
                out Vector2 invalidMax);

            bool clampAndFallbackPass =
                Approximately(invalidMin, Vector2.zero) &&
                Approximately(invalidMax, Vector2.one);

            bool noUpdateLoop = typeof(MobileSafeAreaFitter).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null;

            bool layoutPass = Phase6UiPolishBuilder.ReferenceLayoutHasNoTopOverlayOverlap();
            bool repositoryPass = ValidateRepositoryContract();

            if (!fullSafeAreaPass || !topCenterPass || !clampAndFallbackPass || !noUpdateLoop ||
                !layoutPass || !repositoryPass)
            {
                throw new InvalidOperationException(
                    "Phase 6 fast UI-polish validation failed: " +
                    $"fullSafe={fullSafeAreaPass}, topCenter={topCenterPass}, clampFallback={clampAndFallbackPass}, " +
                    $"noUpdate={noUpdateLoop}, layout={layoutPass}, repository={repositoryPass}.");
            }

            Debug.Log(
                "[Beyond The Beat] FAST UI-POLISH VALIDATION PASS: safe-area mapping/fallback, no-Update fitter contract, " +
                "reference HUD separation and validation documentation passed without scene generation or APK packaging.");
        }

        private static bool ValidateRepositoryContract()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            string docPath = Path.Combine(root, ValidationDocPath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(docPath) &&
                   File.ReadAllText(docPath).Contains(
                       "CI GREEN IS NOT DEVICE UI/UX SIGN-OFF",
                       StringComparison.Ordinal);
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) <= 0.0001f &&
                   Mathf.Abs(a.y - b.y) <= 0.0001f;
        }
    }
}
