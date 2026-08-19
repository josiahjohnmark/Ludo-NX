using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Applies consistent import settings to every UI sprite under Assets/Art/UI.
///
/// The Figma exports arrive as plain textures; without this they import as default Textures with
/// mip maps and compression, which makes UI art soft and adds artefacts around the glossy bevels.
/// Mesh Type is forced to Full Rect because Tight silently breaks 9-slicing if any of these are
/// later given borders.
///
/// Card bodies get a Left pivot so they anchor cleanly against a screen edge; everything else is
/// centred.
/// </summary>
public static class LudoSpriteImportSettings
{
    private const string Root = "Assets/Art/UI";

    [MenuItem("Ludo NX/1. Apply UI Sprite Import Settings")]
    public static void ApplyAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root });
        List<string> changed = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool isCardBody = System.IO.Path.GetFileNameWithoutExtension(path).StartsWith("card_");

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.npotScale = TextureImporterNPOTScale.None;

                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = Vector4.zero;

                // spriteMeshType and spriteAlignment are only exposed on TextureImporterSettings,
                // not on TextureImporter itself. Full Rect is mandatory for any sprite that might
                // be 9-sliced later; Tight also costs more verts on these rectangular shapes.
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)(isCardBody ? SpriteAlignment.LeftCenter : SpriteAlignment.Center);
                importer.SetTextureSettings(settings);

                TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
                platform.maxTextureSize = 512;
                platform.textureCompression = TextureImporterCompression.CompressedHQ;
                platform.compressionQuality = 100;
                importer.SetPlatformTextureSettings(platform);

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                changed.Add(path.Substring(Root.Length + 1));
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[LudoSpriteImportSettings] Applied sprite settings to {changed.Count} texture(s) under {Root}:\n  "
                  + string.Join("\n  ", changed));
    }
}
