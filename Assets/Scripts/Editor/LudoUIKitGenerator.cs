using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Generates the flat UI sprite kit the project is missing (circles, rings, rounded 9-slice
/// panels, soft glows, pips) and imports them with the correct sprite borders.
///
/// This exists because every sprite currently in the project has spriteBorder {0,0,0,0}, so no
/// panel or button can be resized without distorting. These generated sprites are white so they
/// can be tinted per player colour at runtime, and are intended as a stand-in that real Figma art
/// can later replace file-for-file.
/// </summary>
public static class LudoUIKitGenerator
{
    private const string GeneratedDir = "Assets/art_assets/generated";

    [MenuItem("Ludo NX/1. Generate UI Sprite Kit")]
    public static void GenerateKit()
    {
        EnsureFolder();

        CreateSpriteAsset("ui_circle", BuildCircle(256), Vector4.zero);
        CreateSpriteAsset("ui_pip", BuildCircle(64), Vector4.zero);
        CreateSpriteAsset("ui_ring", BuildRing(256, 0.80f, 0.98f), Vector4.zero);
        CreateSpriteAsset("ui_glow", BuildRadialGlow(256), Vector4.zero);

        // 48px corners on a 160px sprite — the border is what makes 9-slicing work.
        CreateSpriteAsset("ui_panel", BuildRoundedRect(160, 40f, 0f), new Vector4(48, 48, 48, 48));
        CreateSpriteAsset("ui_panel_outline", BuildRoundedRect(160, 40f, 5f), new Vector4(48, 48, 48, 48));

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Ludo NX",
            $"Generated the UI sprite kit into {GeneratedDir}.\n\n" +
            "All sprites are white so they can be tinted per player colour. " +
            "ui_panel and ui_panel_outline are 9-sliced with 48px borders.",
            "OK");
    }

    // ── Texture builders ────────────────────────────────────────────────────────────────────

    /// <summary>Solid antialiased disc — avatar plates, icon button backings, pips.</summary>
    private static Texture2D BuildCircle(int size)
    {
        return Build(size, (nx, ny) =>
        {
            float dist = Mathf.Sqrt(nx * nx + ny * ny);
            return 1f - Smooth(dist, 0.97f, 1.0f);
        });
    }

    /// <summary>Hollow ring — used as a radial-fill turn timer around the avatar.</summary>
    private static Texture2D BuildRing(int size, float innerRadius, float outerRadius)
    {
        return Build(size, (nx, ny) =>
        {
            float dist = Mathf.Sqrt(nx * nx + ny * ny);
            float inner = Smooth(dist, innerRadius - 0.02f, innerRadius + 0.01f);
            float outer = 1f - Smooth(dist, outerRadius - 0.01f, outerRadius + 0.02f);
            return Mathf.Min(inner, outer);
        });
    }

    /// <summary>Soft radial falloff — active-card glow, background ambience.</summary>
    private static Texture2D BuildRadialGlow(int size)
    {
        return Build(size, (nx, ny) =>
        {
            float dist = Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny));
            return Mathf.Pow(1f - dist, 2.2f);
        });
    }

    /// <summary>
    /// Rounded rectangle. thickness 0 = filled panel; &gt;0 = outline only (for glow borders).
    /// Built from a signed distance field so the corners stay clean at any 9-slice size.
    /// </summary>
    private static Texture2D BuildRoundedRect(int size, float cornerRadiusPx, float thicknessPx)
    {
        float half = size * 0.5f;
        float r = cornerRadiusPx;

        return Build(size, (nx, ny) =>
        {
            // Distance to the rounded-box edge, in pixels.
            float px = Mathf.Abs(nx) * half;
            float py = Mathf.Abs(ny) * half;

            float dx = px - (half - r);
            float dy = py - (half - r);

            float dist;
            if (dx > 0f && dy > 0f) dist = Mathf.Sqrt(dx * dx + dy * dy) - r;
            else dist = Mathf.Max(px - half, py - half);

            if (thicknessPx <= 0f)
            {
                return 1f - Smooth(dist, -1f, 1f);
            }

            // Outline: keep a band just inside the edge.
            float outer = 1f - Smooth(dist, -1f, 1f);
            float inner = 1f - Smooth(dist + thicknessPx, -1f, 1f);
            return Mathf.Clamp01(outer - inner);
        });
    }

    /// <summary>
    /// Shared texture loop. The callback receives normalised coordinates in -1..1 and returns
    /// alpha; colour is always white so the Image tint decides the final colour.
    /// </summary>
    private static Texture2D Build(int size, System.Func<float, float, float> alphaAt)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float half = size * 0.5f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - half) / half;
                float ny = (y + 0.5f - half) / half;
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alphaAt(nx, ny)));
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static float Smooth(float v, float edge0, float edge1)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge0, edge1, v));
    }

    // ── Asset writing ───────────────────────────────────────────────────────────────────────

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/art_assets"))
        {
            AssetDatabase.CreateFolder("Assets", "art_assets");
        }
        if (!AssetDatabase.IsValidFolder(GeneratedDir))
        {
            AssetDatabase.CreateFolder("Assets/art_assets", "generated");
        }
    }

    private static Sprite CreateSpriteAsset(string fileName, Texture2D tex, Vector4 border)
    {
        string path = $"{GeneratedDir}/{fileName}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static Sprite Load(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedDir}/{fileName}.png");
    }
}
