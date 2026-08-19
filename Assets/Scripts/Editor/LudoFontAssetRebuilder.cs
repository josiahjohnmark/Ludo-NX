using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using TMPro;

/// <summary>
/// Rebuilds the project's TMP font assets from their source TTFs.
///
/// The existing assets in Assets/Fonts were ~3.6 KB each with an empty m_AtlasTextures list —
/// created without ever generating an atlas. Any TextMeshPro object assigned one of them throws
/// "UnassignedReferenceException: The variable m_AtlasTextures of TMP_FontAsset has not been
/// assigned" and renders nothing.
///
/// This regenerates each asset in place (same path, so the .meta and GUID survive and every
/// existing reference keeps pointing at it) using a dynamic SDF atlas, which grows on demand
/// rather than baking a fixed glyph set.
/// </summary>
public static class LudoFontAssetRebuilder
{
    private const string FontsFolder = "Assets/Fonts";
    private const int SamplingPointSize = 90;
    private const int AtlasPadding = 9;
    private const int AtlasDimension = 1024;

    [MenuItem("Ludo NX/0. Rebuild TMP Font Assets")]
    public static void RebuildAll()
    {
        if (!Directory.Exists(FontsFolder))
        {
            Debug.LogError($"[LudoFontAssetRebuilder] No folder at {FontsFolder}.");
            return;
        }

        List<string> rebuilt = new List<string>();
        List<string> failed = new List<string>();

        foreach (string ttfPath in Directory.GetFiles(FontsFolder, "*.ttf", SearchOption.TopDirectoryOnly))
        {
            string unityPath = ttfPath.Replace('\\', '/');
            Font source = AssetDatabase.LoadAssetAtPath<Font>(unityPath);
            if (source == null)
            {
                failed.Add($"{Path.GetFileName(unityPath)} (source TTF would not load)");
                continue;
            }

            string assetPath = Path.ChangeExtension(unityPath, ".asset");
            string assetName = Path.GetFileNameWithoutExtension(unityPath) + " SDF";

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                source,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasDimension,
                AtlasDimension,
                AtlasPopulationMode.Dynamic);

            if (fontAsset == null)
            {
                failed.Add($"{Path.GetFileName(unityPath)} (CreateFontAsset returned null)");
                continue;
            }

            fontAsset.name = assetName;

            // Replace whatever is at the path. Unity keeps the GUID bound to the path via the
            // existing .meta, so scene and prefab references survive the swap.
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // The atlas texture and material must live inside the asset or they are lost on
            // reload — this is exactly what was missing before.
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
            {
                fontAsset.atlasTextures[0].name = assetName + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            rebuilt.Add(Path.GetFileName(assetPath));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary = $"Rebuilt {rebuilt.Count} font asset(s):\n  " + string.Join("\n  ", rebuilt);
        if (failed.Count > 0) summary += $"\n\nFailed {failed.Count}:\n  " + string.Join("\n  ", failed);

        // Deliberately no modal — see LudoPlayerCardBuilder for why.
        Debug.Log("[LudoFontAssetRebuilder] " + summary);
    }
}
