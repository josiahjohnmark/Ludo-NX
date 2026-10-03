using System.IO;
using UnityEditor;
using UnityEngine;

namespace LudoNX.Editor
{
    [InitializeOnLoad]
    public static class LudoNXCancelButtonGenerator
    {
        public const string CancelButtonPath = "Assets/Art/UI/Shared/btn_cancel_black_circle.png";

        static LudoNXCancelButtonGenerator()
        {
            EnsureAsset();
        }

        [MenuItem("Ludo NX/UI/Generate Cancel Button Asset")]
        public static void EnsureAsset()
        {
            if (File.Exists(CancelButtonPath)) return;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var transparent = new Color(0f, 0f, 0f, 0f);
            var black = new Color(0.08f, 0.08f, 0.08f, 1f); // rich crisp black

            float center = size * 0.5f;
            float circleRadius = 50f;
            float circleHalfThick = 4f; // 8px total thickness
            float xSpan = 18f;
            float xHalfThick = 4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    // Distance to circle ring
                    float distToCenter = Mathf.Sqrt((px - center) * (px - center) + (py - center) * (py - center));
                    float distToRing = Mathf.Abs(distToCenter - circleRadius);
                    float ringAlpha = Mathf.Clamp01(1f - (distToRing - (circleHalfThick - 0.5f)));

                    // Distance to 'X' diagonals
                    float dx = Mathf.Abs(px - center);
                    float dy = Mathf.Abs(py - center);

                    // Diagonals: line 1 is px - py = 0, line 2 is px + py = 2*center
                    float distDiag1 = Mathf.Abs((px - center) - (py - center)) * 0.70710678f;
                    float distDiag2 = Mathf.Abs((px - center) + (py - center)) * 0.70710678f;
                    float distDiag = Mathf.Min(distDiag1, distDiag2);

                    float xAlpha = 0f;
                    if (dx <= xSpan + xHalfThick && dy <= xSpan + xHalfThick)
                    {
                        // Check if within arm endpoints
                        float proj1 = Mathf.Abs((px - center) + (py - center)) * 0.70710678f;
                        float proj2 = Mathf.Abs((px - center) - (py - center)) * 0.70710678f;
                        bool inArm1 = distDiag1 <= circleHalfThick && proj1 <= (xSpan * 1.4142f + 1f);
                        bool inArm2 = distDiag2 <= circleHalfThick && proj2 <= (xSpan * 1.4142f + 1f);

                        if (inArm1 || inArm2)
                        {
                            xAlpha = Mathf.Clamp01(1f - (distDiag - (xHalfThick - 0.5f)));
                        }
                    }

                    float finalAlpha = Mathf.Max(ringAlpha, xAlpha);
                    tex.SetPixel(x, y, new Color(black.r, black.g, black.b, finalAlpha));
                }
            }

            tex.Apply();
            byte[] pngData = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string dir = Path.GetDirectoryName(CancelButtonPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(CancelButtonPath, pngData);

            AssetDatabase.ImportAsset(CancelButtonPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(CancelButtonPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Debug.Log($"[Ludo NX] Generated cancel button sprite -> {CancelButtonPath}");
        }
    }
}
