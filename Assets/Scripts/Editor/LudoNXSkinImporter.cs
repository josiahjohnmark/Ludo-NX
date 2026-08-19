using System.IO;
using LudoNX.UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports a folder of theme sprites, sets their 9-slice borders, and builds the UISkin asset.
///
/// The borders are derived from each surface's corner radius rather than typed in by hand: a
/// 9-slice must never stretch its corners, so the inset is simply "radius plus a few pixels of
/// safety". Getting these wrong is the classic way a themed button ends up with smeared corners.
/// </summary>
public static class LudoNXSkinImporter
{
    private const string SourceFolder = "Assets/Art/UI/Themes/01_Default";
    private const string SkinAssetPath = "Assets/Resources/UISkin.asset";

    /// <summary>filename → uniform 9-slice inset in pixels. Zero means "do not slice".</summary>
    private static readonly (string file, int inset, UISkin.Role role)[] Spec =
    {
        ("btn_pill",         0, UISkin.Role.ButtonPill),        // inset computed from height
        ("btn_card",        44, UISkin.Role.ButtonCard),
        ("btn_icon_square", 32, UISkin.Role.ButtonIconSquare),
        ("btn_icon_circle",  0, UISkin.Role.ButtonIconCircle),  // circle: never sliced
        ("panel_card",      44, UISkin.Role.PanelCard),
        ("panel_modal",     60, UISkin.Role.PanelModal),
        ("panel_tile",      32, UISkin.Role.PanelTile),
        ("card_player",     32, UISkin.Role.CardPlayer),
        ("track_pill",       0, UISkin.Role.TrackPill),         // fully rounded: half the height
        ("track_well",      36, UISkin.Role.TrackWell),
        ("thumb_segment",    0, UISkin.Role.ThumbSegment),      // fully rounded
        ("toggle_track",     0, UISkin.Role.ToggleTrack),       // fully rounded
        ("toggle_knob",      0, UISkin.Role.ToggleKnob),        // circle
        ("chip_pill",        0, UISkin.Role.ChipPill),          // fully rounded
        ("avatar_ring",      0, UISkin.Role.AvatarRing),        // circle
        ("avatar_slot",     36, UISkin.Role.AvatarSlot),
        ("pip_empty",        0, UISkin.Role.PipEmpty),          // circle
        ("pip_filled",       0, UISkin.Role.PipFilled),         // circle
        ("die_frame",       36, UISkin.Role.DieFrame),
    };

    /// <summary>Surfaces whose ends are fully rounded — the inset is half the shorter side.</summary>
    private static readonly string[] FullyRounded =
    { "btn_pill", "track_pill", "thumb_segment", "toggle_track", "chip_pill" };

    [MenuItem("Ludo NX/UI/11. Import Theme Skin", priority = 11)]
    public static void ImportSkin()
    {
        if (!AssetDatabase.IsValidFolder(SourceFolder))
        {
            Debug.LogError("[LudoNX Skin] No folder at " + SourceFolder +
                           " — copy the theme PNGs there first.");
            return;
        }

        var skin = AssetDatabase.LoadAssetAtPath<UISkin>(SkinAssetPath);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<UISkin>();
            AssetDatabase.CreateAsset(skin, SkinAssetPath);
        }

        int imported = 0, missing = 0;
        var log = new System.Text.StringBuilder();

        foreach (var entry in Spec)
        {
            string path = SourceFolder + "/" + entry.file + ".png";
            if (!File.Exists(path)) { missing++; log.AppendLine("   missing  " + entry.file); continue; }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { missing++; continue; }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;

            // Work out the inset. Fully rounded shapes take half their shorter side; the rest
            // use the value in the table above.
            int inset = entry.inset;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool rounded = System.Array.IndexOf(FullyRounded, entry.file) >= 0;

            if (rounded && tex != null)
            {
                // A pill is sliced HORIZONTALLY ONLY. Its end caps are semicircles whose radius
                // must always equal half the current height, so the top and bottom must scale
                // rather than be pinned — slicing vertically as well forced a 240px-tall source
                // through a 118px-tall button and visibly crushed the curve.
                int capX = Mathf.Max(2, Mathf.Min(tex.height / 2, tex.width / 2 - 1));
                importer.spriteBorder = new Vector4(capX, 0f, capX, 0f);
            }
            else if (inset > 0 && tex != null)
            {
                // Never let opposite borders meet, or the middle slice inverts.
                int maxX = Mathf.Max(1, tex.width / 2 - 1);
                int maxY = Mathf.Max(1, tex.height / 2 - 1);
                int ix = Mathf.Min(inset, maxX);
                int iy = Mathf.Min(inset, maxY);
                importer.spriteBorder = new Vector4(ix, iy, ix, iy);
            }
            else
            {
                importer.spriteBorder = Vector4.zero;
            }

            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            AssignRole(skin, entry.role, sprite);
            imported++;
            log.AppendLine(string.Format("   {0,-18} {1,4}x{2,-4} border {3}",
                entry.file, tex != null ? tex.width : 0, tex != null ? tex.height : 0,
                importer.spriteBorder));
        }

        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[LudoNX Skin] Imported " + imported + " sprites, " + missing + " missing.\n" + log);
    }

    private static void AssignRole(UISkin s, UISkin.Role role, Sprite sprite)
    {
        switch (role)
        {
            case UISkin.Role.ButtonPill: s.buttonPill = sprite; break;
            case UISkin.Role.ButtonCard: s.buttonCard = sprite; break;
            case UISkin.Role.ButtonIconSquare: s.buttonIconSquare = sprite; break;
            case UISkin.Role.ButtonIconCircle: s.buttonIconCircle = sprite; break;
            case UISkin.Role.PanelCard: s.panelCard = sprite; break;
            case UISkin.Role.PanelModal: s.panelModal = sprite; break;
            case UISkin.Role.PanelTile: s.panelTile = sprite; break;
            case UISkin.Role.CardPlayer: s.cardPlayer = sprite; break;
            case UISkin.Role.TrackPill: s.trackPill = sprite; break;
            case UISkin.Role.TrackWell: s.trackWell = sprite; break;
            case UISkin.Role.ThumbSegment: s.thumbSegment = sprite; break;
            case UISkin.Role.ToggleTrack: s.toggleTrack = sprite; break;
            case UISkin.Role.ToggleKnob: s.toggleKnob = sprite; break;
            case UISkin.Role.ChipPill: s.chipPill = sprite; break;
            case UISkin.Role.AvatarRing: s.avatarRing = sprite; break;
            case UISkin.Role.AvatarSlot: s.avatarSlot = sprite; break;
            case UISkin.Role.PipEmpty: s.pipEmpty = sprite; break;
            case UISkin.Role.PipFilled: s.pipFilled = sprite; break;
            case UISkin.Role.DieFrame: s.dieFrame = sprite; break;
        }
    }
}
