using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class MobileUiTheme
    {
        private const string GeneratedFolder = "Assets/Generated";
        private const string UiFolder = "Assets/Generated/UI";
        private const string CircleSpritePath = UiFolder + "/BTB_Circle.png";
        private const string RoundedSpritePath = UiFolder + "/BTB_RoundedRect.png";
        private const string ChamferedSpritePath = UiFolder + "/BTB_ChamferedRect.png";
        private const string RoundedBorderSpritePath = UiFolder + "/BTB_RoundedRectBorder.png";
        private const string ChamferedBorderSpritePath = UiFolder + "/BTB_ChamferedRectBorder.png";

        // Phase 7 tactical palette. Ink is the requested #1E222A charcoal anchor.
        public static readonly Color Ink = new Color(30f / 255f, 34f / 255f, 42f / 255f, 0.96f);
        public static readonly Color InkSoft = new Color(42f / 255f, 48f / 255f, 58f / 255f, 0.90f);
        public static readonly Color Cyan = new Color(47f / 255f, 230f / 255f, 242f / 255f, 1f);
        public static readonly Color Amber = new Color(1f, 181f / 255f, 71f / 255f, 1f);
        public static readonly Color Red = new Color(1f, 92f / 255f, 99f / 255f, 1f);
        public static readonly Color White = new Color(247f / 255f, 249f / 255f, 252f / 255f, 1f);
        public static readonly Color Muted = new Color(154f / 255f, 167f / 255f, 183f / 255f, 1f);

        public const float BorderPadding = 10f;

        public static Sprite CircleSprite => GetOrCreateLegacySprite(CircleSpritePath, true);
        public static Sprite RoundedRectSprite => GetOrCreateLegacySprite(RoundedSpritePath, false);
        public static Sprite ChamferedRectSprite => GetOrCreateShapeSprite(ChamferedSpritePath, Shape.Chamfered, false);
        public static Sprite RoundedRectBorderSprite => GetOrCreateShapeSprite(RoundedBorderSpritePath, Shape.Rounded, true);
        public static Sprite ChamferedRectBorderSprite => GetOrCreateShapeSprite(ChamferedBorderSpritePath, Shape.Chamfered, true);

        private enum Shape
        {
            Rounded,
            Chamfered
        }

        private static Sprite GetOrCreateLegacySprite(string path, bool circle)
        {
            EnsureFolders();

            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = Path.GetFileNameWithoutExtension(path),
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            float radius = half - 1f;
            float cornerRadius = 22f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha;
                    if (circle)
                    {
                        float dx = x - half;
                        float dy = y - half;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        alpha = Mathf.Clamp01(radius - distance + 1f);
                    }
                    else
                    {
                        float px = Mathf.Abs(x - half) - (half - cornerRadius);
                        float py = Mathf.Abs(y - half) - (half - cornerRadius);
                        float outsideX = Mathf.Max(px, 0f);
                        float outsideY = Mathf.Max(py, 0f);
                        float outsideDistance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);
                        float insideDistance = Mathf.Min(Mathf.Max(px, py), 0f);
                        float signedDistance = outsideDistance + insideDistance - cornerRadius;
                        alpha = Mathf.Clamp01(1f - signedDistance);
                    }

                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            ConfigureImporter(path, Vector4.zero);
            return LoadGeneratedSprite(path);
        }

        private static Sprite GetOrCreateShapeSprite(string path, Shape shape, bool borderOnly)
        {
            EnsureFolders();

            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            const int size = 96;
            const float roundedRadius = 22f;
            const float chamferSize = 20f;
            const float borderThickness = 2f;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = Path.GetFileNameWithoutExtension(path),
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float insideDistance = shape == Shape.Rounded
                        ? RoundedInsideDistance(x, y, size, roundedRadius)
                        : ChamferedInsideDistance(x, y, size, chamferSize);

                    float outerAlpha = Mathf.Clamp01(insideDistance + 0.75f);
                    float alpha = outerAlpha;
                    if (borderOnly)
                    {
                        float innerAlpha = Mathf.Clamp01(insideDistance - borderThickness + 0.75f);
                        alpha = outerAlpha * (1f - innerAlpha);
                    }

                    pixels[y * size + x] = new Color32(
                        255,
                        255,
                        255,
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            ConfigureImporter(path, new Vector4(24f, 24f, 24f, 24f));
            return LoadGeneratedSprite(path);
        }

        private static float RoundedInsideDistance(float x, float y, float size, float cornerRadius)
        {
            float half = (size - 1f) * 0.5f;
            float px = Mathf.Abs(x - half) - (half - cornerRadius);
            float py = Mathf.Abs(y - half) - (half - cornerRadius);
            float outsideX = Mathf.Max(px, 0f);
            float outsideY = Mathf.Max(py, 0f);
            float outsideDistance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);
            float insideDistance = Mathf.Min(Mathf.Max(px, py), 0f);
            return -(outsideDistance + insideDistance - cornerRadius);
        }

        private static float ChamferedInsideDistance(float x, float y, float size, float chamfer)
        {
            float max = size - 1f;
            float left = x;
            float right = max - x;
            float bottom = y;
            float top = max - y;
            const float inverseSqrtTwo = 0.70710678f;
            float bottomLeftCut = (x + y - chamfer) * inverseSqrtTwo;
            float topRightCut = ((max - x) + (max - y) - chamfer) * inverseSqrtTwo;

            return Mathf.Min(
                Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top)),
                Mathf.Min(bottomLeftCut, topRightCut));
        }

        private static void ConfigureImporter(string path, Vector4 spriteBorder)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Unable to configure generated UI sprite '{path}'.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (spriteBorder != Vector4.zero)
            {
                importer.spriteBorder = spriteBorder;
            }
            importer.SaveAndReimport();
        }

        private static Sprite LoadGeneratedSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Generated UI sprite '{path}' could not be loaded.");
            }
            return sprite;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Generated");
            }

            if (!AssetDatabase.IsValidFolder(UiFolder))
            {
                AssetDatabase.CreateFolder(GeneratedFolder, "UI");
            }
        }
    }
}
