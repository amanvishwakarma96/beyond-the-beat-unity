using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6StoreAssetsBuilder
    {
        public const int StoreIconWidth = 512;
        public const int StoreIconHeight = 512;
        public const int StoreIconMaxBytes = 1024 * 1024;
        public const int FeatureGraphicWidth = 1024;
        public const int FeatureGraphicHeight = 500;
        public const string AppTitle = "Beyond The Beat";
        public const string ShortDescription = "Drive, explore, complete missions, and discover an offline open world.";
        public const string StoreOutputRelativePath = "build/store-assets";

        private const string StoreIconFileName = "play-store-icon-512.png";
        private const string FeatureGraphicFileName = "feature-graphic-1024x500.png";
        private const string ListingSourcePath = "Docs/Store/PLAY_STORE_LISTING.md";
        private const string ValidationDocPath = "Docs/Validation/PHASE_6_STORE_ASSETS.md";
        private const string LauncherFolder = "Assets/Generated/Store/LauncherIcons";

        private static readonly Color32 Ink = new Color32(7, 15, 28, 255);
        private static readonly Color32 InkBlue = new Color32(10, 35, 54, 255);
        private static readonly Color32 Cyan = new Color32(38, 209, 235, 255);
        private static readonly Color32 CyanSoft = new Color32(24, 122, 148, 255);
        private static readonly Color32 Amber = new Color32(255, 163, 46, 255);
        private static readonly Color32 Forest = new Color32(17, 62, 48, 255);
        private static readonly Color32 Ocean = new Color32(13, 79, 105, 255);
        private static readonly Color32 Road = new Color32(22, 29, 38, 255);

        [MenuItem("Beyond The Beat/Phase 6/Generate Store Assets")]
        public static void GenerateStoreAssets()
        {
            string output = ResolveProjectPath(StoreOutputRelativePath);
            Directory.CreateDirectory(output);

            PlayerSettings.productName = AppTitle;

            Texture2D storeIcon = RenderStoreIcon(StoreIconWidth);
            Texture2D feature = RenderFeatureGraphic(FeatureGraphicWidth, FeatureGraphicHeight);
            try
            {
                File.WriteAllBytes(Path.Combine(output, StoreIconFileName), storeIcon.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(output, FeatureGraphicFileName), feature.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(storeIcon);
                UnityEngine.Object.DestroyImmediate(feature);
            }

            GenerateAndAssignLauncherIcons();
            CopyListingFiles(output);
            WriteManifest(output);
            WriteScreenshotBoundary(output);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Beyond The Beat] Phase 6 Google Play icon, feature graphic, launcher icons and listing package generated.");
        }

        [MenuItem("Beyond The Beat/Phase 6/Validate Store Assets")]
        public static void ValidateStoreAssets()
        {
            if (!ValidateStoreAssetsInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool PrepareAndValidateOrThrow()
        {
            GenerateStoreAssets();
            if (ValidateStoreAssetsInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        internal static Texture2D RenderStoreIconForValidation(int size)
        {
            return RenderStoreIcon(size);
        }

        internal static Texture2D RenderFeatureGraphicForValidation(int width, int height)
        {
            return RenderFeatureGraphic(width, height);
        }

        internal static ulong ComputePixelChecksum(Texture2D texture)
        {
            if (texture == null)
            {
                return 0UL;
            }

            Color32[] pixels = texture.GetPixels32();
            ulong hash = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                hash = (hash ^ pixel.r) * prime;
                hash = (hash ^ pixel.g) * prime;
                hash = (hash ^ pixel.b) * prime;
                hash = (hash ^ pixel.a) * prime;
            }
            return hash;
        }

        internal static bool IsListingCopyConfigured()
        {
            if (string.IsNullOrWhiteSpace(AppTitle) ||
                string.IsNullOrWhiteSpace(ShortDescription) ||
                ShortDescription.Length > 80)
            {
                return false;
            }

            string path = ResolveProjectPath(ListingSourcePath);
            if (!File.Exists(path))
            {
                return false;
            }

            string listing = File.ReadAllText(path);
            return listing.Contains("Beyond The Beat", StringComparison.Ordinal) &&
                   listing.Contains(ShortDescription, StringComparison.Ordinal) &&
                   listing.Contains("## Full description", StringComparison.Ordinal) &&
                   listing.Contains("## App icon alt text", StringComparison.Ordinal) &&
                   listing.Contains("## Feature graphic alt text", StringComparison.Ordinal) &&
                   listing.Contains("actual Android build", StringComparison.OrdinalIgnoreCase) &&
                   listing.Contains("1920x1080", StringComparison.Ordinal);
        }

        private static bool ValidateStoreAssetsInternal(out string message)
        {
            string output = ResolveProjectPath(StoreOutputRelativePath);
            string iconPath = Path.Combine(output, StoreIconFileName);
            string featurePath = Path.Combine(output, FeatureGraphicFileName);
            string manifestPath = Path.Combine(output, "STORE-ASSET-MANIFEST.txt");
            string listingCopyPath = Path.Combine(output, "PLAY-STORE-LISTING.md");
            string screenshotBoundaryPath = Path.Combine(output, "SCREENSHOT-CAPTURE-REQUIRED.txt");

            bool iconPass = ValidatePng(iconPath, StoreIconWidth, StoreIconHeight, out bool iconOpaque, out long iconBytes) &&
                            iconBytes <= StoreIconMaxBytes;
            bool featurePass = ValidatePng(featurePath, FeatureGraphicWidth, FeatureGraphicHeight, out bool featureOpaque, out _) &&
                               featureOpaque;
            bool copyPass = IsListingCopyConfigured() &&
                            File.Exists(manifestPath) &&
                            File.Exists(listingCopyPath) &&
                            File.Exists(screenshotBoundaryPath) &&
                            File.Exists(ResolveProjectPath(ValidationDocPath));

            Texture2D[] assignedIcons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Android);
            bool launcherPass = assignedIcons != null && assignedIcons.Length > 0 && assignedIcons.All(icon => icon != null);
            bool productPass = string.Equals(PlayerSettings.productName, AppTitle, StringComparison.Ordinal);

            bool pass = iconPass && featurePass && copyPass && launcherPass && productPass;
            message = pass
                ? $"[Beyond The Beat] Phase 6 store-assets validation PASS: 512x512 icon ({iconBytes} bytes), opaque 1024x500 feature graphic, Android launcher icons, listing copy and real-screenshot capture boundary are ready."
                : "[Beyond The Beat] Phase 6 store-assets validation FAIL: " +
                  $"icon={iconPass} (opaque={iconOpaque}, bytes={iconBytes}), feature={featurePass} (opaque={featureOpaque}), " +
                  $"copy={copyPass}, launcher={launcherPass}, productName={productPass}.";
            return pass;
        }

        private static bool ValidatePng(string path, int expectedWidth, int expectedHeight, out bool fullyOpaque, out long bytes)
        {
            fullyOpaque = false;
            bytes = 0L;
            if (!File.Exists(path))
            {
                return false;
            }

            byte[] data = File.ReadAllBytes(path);
            bytes = data.LongLength;
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, data, false))
                {
                    return false;
                }

                fullyOpaque = texture.GetPixels32().All(pixel => pixel.a == 255);
                return texture.width == expectedWidth && texture.height == expectedHeight;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void GenerateAndAssignLauncherIcons()
        {
            int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
            if (sizes == null || sizes.Length == 0)
            {
                throw new InvalidOperationException("Unity reported no Android launcher icon slots.");
            }

            string absoluteLauncherFolder = ResolveProjectPath(LauncherFolder);
            Directory.CreateDirectory(absoluteLauncherFolder);
            Texture2D[] icons = new Texture2D[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                int size = Mathf.Max(16, sizes[i]);
                string assetPath = $"{LauncherFolder}/BTB_Launcher_{size}_{i}.png";
                string absolutePath = ResolveProjectPath(assetPath);

                Texture2D generated = RenderStoreIcon(size);
                try
                {
                    File.WriteAllBytes(absolutePath, generated.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(generated);
                }

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException($"Unable to configure launcher icon '{assetPath}'.");
                }

                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = Mathf.NextPowerOfTwo(size);
                importer.SaveAndReimport();

                icons[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (icons[i] == null)
                {
                    throw new InvalidOperationException($"Generated launcher icon '{assetPath}' could not be loaded.");
                }
            }

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
        }

        private static void CopyListingFiles(string output)
        {
            string listingSource = ResolveProjectPath(ListingSourcePath);
            if (!File.Exists(listingSource))
            {
                throw new FileNotFoundException("Play Store listing source is missing.", listingSource);
            }

            File.Copy(listingSource, Path.Combine(output, "PLAY-STORE-LISTING.md"), true);
        }

        private static void WriteManifest(string output)
        {
            string content =
                "BEYOND THE BEAT — STORE ASSET MANIFEST\n\n" +
                $"App title: {AppTitle}\n" +
                $"Short description: {ShortDescription}\n\n" +
                $"{StoreIconFileName}: {StoreIconWidth}x{StoreIconHeight} RGBA PNG, <= {StoreIconMaxBytes} bytes\n" +
                $"{FeatureGraphicFileName}: {FeatureGraphicWidth}x{FeatureGraphicHeight} opaque PNG\n" +
                "PLAY-STORE-LISTING.md: listing copy, alt text and screenshot storyboard\n" +
                "SCREENSHOT-CAPTURE-REQUIRED.txt: real-device screenshot acceptance boundary\n\n" +
                "No generated file in this package is a gameplay screenshot.\n" +
                "Final screenshots must be captured from the actual Android build.\n";
            File.WriteAllText(Path.Combine(output, "STORE-ASSET-MANIFEST.txt"), content);
        }

        private static void WriteScreenshotBoundary(string output)
        {
            string content =
                "REAL GAMEPLAY SCREENSHOTS REQUIRED\n\n" +
                "Do not publish generated/mock gameplay screenshots.\n" +
                "Minimum Play Store publishing gate: at least 2 actual screenshots.\n" +
                "Game recommendation target: at least 3 landscape 16:9 screenshots at 1920x1080 or higher.\n" +
                "Preferred capture set: driving, forest survival, restricted puzzle, ocean exploration, mechanic/free-roam.\n" +
                "Capture from the post-merge TEST-THIS-BUILD Android artifact and verify UI/safe-area readability first.\n";
            File.WriteAllText(Path.Combine(output, "SCREENSHOT-CAPTURE-REQUIRED.txt"), content);
        }

        private static Texture2D RenderStoreIcon(int size)
        {
            int dimension = Mathf.Max(16, size);
            Texture2D texture = new Texture2D(dimension, dimension, TextureFormat.RGBA32, false, false)
            {
                name = "BTB_StoreIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[dimension * dimension];
            for (int y = 0; y < dimension; y++)
            {
                float vertical = y / (float)Mathf.Max(1, dimension - 1);
                Color32 baseColor = LerpColor(Ink, InkBlue, vertical);
                for (int x = 0; x < dimension; x++)
                {
                    float nx = (x - dimension * 0.5f) / (dimension * 0.5f);
                    float ny = (y - dimension * 0.5f) / (dimension * 0.5f);
                    float glow = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny));
                    pixels[y * dimension + x] = LerpColor(baseColor, new Color32(12, 72, 89, 255), glow * 0.32f);
                }
            }

            float s = dimension;
            DrawThickLine(pixels, dimension, dimension, new Vector2(0.22f * s, 0.12f * s), new Vector2(0.43f * s, 0.86f * s), 0.035f * s, CyanSoft);
            DrawThickLine(pixels, dimension, dimension, new Vector2(0.78f * s, 0.12f * s), new Vector2(0.57f * s, 0.86f * s), 0.035f * s, Cyan);
            DrawThickLine(pixels, dimension, dimension, new Vector2(0.50f * s, 0.14f * s), new Vector2(0.50f * s, 0.31f * s), 0.018f * s, new Color32(210, 247, 252, 255));
            DrawThickLine(pixels, dimension, dimension, new Vector2(0.50f * s, 0.40f * s), new Vector2(0.50f * s, 0.55f * s), 0.014f * s, new Color32(210, 247, 252, 255));

            Vector2[] pulse =
            {
                new Vector2(0.12f * s, 0.55f * s),
                new Vector2(0.30f * s, 0.55f * s),
                new Vector2(0.38f * s, 0.68f * s),
                new Vector2(0.47f * s, 0.35f * s),
                new Vector2(0.57f * s, 0.62f * s),
                new Vector2(0.66f * s, 0.55f * s),
                new Vector2(0.88f * s, 0.55f * s)
            };
            DrawPolyline(pixels, dimension, dimension, pulse, Mathf.Max(2f, 0.022f * s), Amber);
            DrawDisc(pixels, dimension, dimension, new Vector2(0.50f * s, 0.82f * s), 0.045f * s, Cyan);

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D RenderFeatureGraphic(int width, int height)
        {
            int w = Mathf.Max(64, width);
            int h = Mathf.Max(32, height);
            Texture2D texture = new Texture2D(w, h, TextureFormat.RGB24, false, false)
            {
                name = "BTB_FeatureGraphic",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)Mathf.Max(1, h - 1);
                Color32 sky = t < 0.48f
                    ? LerpColor(new Color32(5, 20, 33, 255), new Color32(12, 58, 78, 255), t / 0.48f)
                    : LerpColor(new Color32(12, 58, 78, 255), new Color32(20, 104, 120, 255), (t - 0.48f) / 0.52f);
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = sky;
                }
            }

            int horizon = Mathf.RoundToInt(h * 0.42f);
            for (int y = 0; y < horizon; y++)
            {
                float t = y / (float)Mathf.Max(1, horizon - 1);
                for (int x = 0; x < w; x++)
                {
                    bool oceanSide = x > w * 0.58f;
                    Color32 ground = oceanSide
                        ? LerpColor(new Color32(8, 48, 66, 255), Ocean, t)
                        : LerpColor(new Color32(9, 33, 31, 255), Forest, t * 0.75f);
                    pixels[y * w + x] = ground;
                }
            }

            DrawRoad(pixels, w, h, horizon);
            DrawForestSilhouettes(pixels, w, h, horizon);
            DrawOceanLines(pixels, w, h, horizon);

            Vector2[] pulse =
            {
                new Vector2(0.08f * w, 0.53f * h),
                new Vector2(0.28f * w, 0.53f * h),
                new Vector2(0.34f * w, 0.63f * h),
                new Vector2(0.41f * w, 0.39f * h),
                new Vector2(0.49f * w, 0.59f * h),
                new Vector2(0.57f * w, 0.53f * h),
                new Vector2(0.92f * w, 0.53f * h)
            };
            DrawPolyline(pixels, w, h, pulse, Mathf.Max(3f, h * 0.012f), Amber);

            DrawDisc(pixels, w, h, new Vector2(0.50f * w, 0.64f * h), h * 0.035f, Cyan);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static void DrawRoad(Color32[] pixels, int width, int height, int horizon)
        {
            for (int y = 0; y < horizon; y++)
            {
                float t = y / (float)Mathf.Max(1, horizon);
                float halfWidth = Mathf.Lerp(width * 0.24f, width * 0.035f, t);
                float center = Mathf.Lerp(width * 0.50f, width * 0.49f, t);
                int minX = Mathf.Clamp(Mathf.RoundToInt(center - halfWidth), 0, width - 1);
                int maxX = Mathf.Clamp(Mathf.RoundToInt(center + halfWidth), 0, width - 1);
                for (int x = minX; x <= maxX; x++)
                {
                    pixels[y * width + x] = Road;
                }

                if ((y / Mathf.Max(1, height / 18)) % 2 == 0)
                {
                    int stripeHalf = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(width * 0.012f, width * 0.003f, t)));
                    for (int x = Mathf.Max(0, Mathf.RoundToInt(center) - stripeHalf); x <= Mathf.Min(width - 1, Mathf.RoundToInt(center) + stripeHalf); x++)
                    {
                        pixels[y * width + x] = new Color32(205, 235, 236, 255);
                    }
                }
            }
        }

        private static void DrawForestSilhouettes(Color32[] pixels, int width, int height, int horizon)
        {
            int count = 11;
            for (int i = 0; i < count; i++)
            {
                float x = width * (0.04f + i * 0.043f);
                float treeHeight = height * (0.12f + (i % 3) * 0.028f);
                float baseY = horizon + height * 0.015f;
                DrawThickLine(pixels, width, height, new Vector2(x, baseY - treeHeight * 0.45f), new Vector2(x, baseY), Mathf.Max(2f, width * 0.004f), new Color32(8, 38, 32, 255));
                DrawDisc(pixels, width, height, new Vector2(x, baseY + treeHeight * 0.12f), treeHeight * 0.30f, new Color32(10, 52, 40, 255));
                DrawDisc(pixels, width, height, new Vector2(x, baseY + treeHeight * 0.32f), treeHeight * 0.24f, new Color32(12, 68, 48, 255));
            }
        }

        private static void DrawOceanLines(Color32[] pixels, int width, int height, int horizon)
        {
            for (int i = 0; i < 4; i++)
            {
                float y = horizon * (0.18f + i * 0.18f);
                DrawThickLine(
                    pixels,
                    width,
                    height,
                    new Vector2(width * 0.64f, y),
                    new Vector2(width * 0.95f, y + (i % 2 == 0 ? height * 0.015f : -height * 0.012f)),
                    Mathf.Max(2f, height * 0.006f),
                    new Color32(29, 140, 168, 255));
            }
        }

        private static void DrawPolyline(Color32[] pixels, int width, int height, Vector2[] points, float thickness, Color32 color)
        {
            if (points == null || points.Length < 2)
            {
                return;
            }

            for (int i = 1; i < points.Length; i++)
            {
                DrawThickLine(pixels, width, height, points[i - 1], points[i], thickness, color);
            }
        }

        private static void DrawThickLine(Color32[] pixels, int width, int height, Vector2 start, Vector2 end, float thickness, Color32 color)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance * 1.25f));
            float radius = Mathf.Max(1f, thickness * 0.5f);
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(start, end, i / (float)steps);
                DrawDisc(pixels, width, height, point, radius, color);
            }
        }

        private static void DrawDisc(Color32[] pixels, int width, int height, Vector2 center, float radius, Color32 color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + radius));
            float radiusSquared = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    if (dx * dx + dy * dy <= radiusSquared)
                    {
                        pixels[y * width + x] = color;
                    }
                }
            }
        }

        private static Color32 LerpColor(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
        }

        private static string ResolveProjectPath(string relativePath)
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            return string.IsNullOrWhiteSpace(root)
                ? relativePath
                : Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
