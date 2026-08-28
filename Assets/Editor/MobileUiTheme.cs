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

        public static readonly Color Ink = new Color(0.025f, 0.035f, 0.055f, 0.92f);
        public static readonly Color InkSoft = new Color(0.035f, 0.055f, 0.085f, 0.82f);
        public static readonly Color Cyan = new Color(0.15f, 0.82f, 0.92f, 1f);
        public static readonly Color Amber = new Color(1.0f, 0.64f, 0.18f, 1f);
        public static readonly Color Red = new Color(0.95f, 0.27f, 0.24f, 1f);
        public static readonly Color White = new Color(0.96f, 0.98f, 1f, 1f);
        public static readonly Color Muted = new Color(0.66f, 0.74f, 0.82f, 1f);

        public static Sprite CircleSprite => GetOrCreateSprite(CircleSpritePath, true);
        public static Sprite RoundedRectSprite => GetOrCreateSprite(RoundedSpritePath, false);

        private static Sprite GetOrCreateSprite(string path, bool circle)
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
            importer.SaveAndReimport();

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
