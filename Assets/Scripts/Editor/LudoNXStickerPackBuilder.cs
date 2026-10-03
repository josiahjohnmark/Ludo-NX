using System.IO;
using LudoNX.UI;
using UnityEditor;
using UnityEngine;
using static LudoNXUIKit;

/// <summary>
/// Bakes the art references for the sticker-pack opener into
/// Resources/UI/Modals/StickerPackOpener.prefab. The opener builds its own layers at runtime
/// (see StickerPackOpener) so it fits every screen height; this only hands it the art.
///
/// Every sticker sheet listed in StickerPackService.Stickers is referenced here, in that
/// order, and also handed to the shop so its Emoji tab can animate the pack covers.
/// </summary>
public static class LudoNXStickerPackBuilder
{
    private const string PrefabPath = "Assets/Resources/UI/Modals/StickerPackOpener.prefab";
    private const string ShopArt = "Assets/Art/UI/Shop";
    private const string PricePill = ShopArt + "/price_pill.png";

    [MenuItem("Ludo NX/UI/Build Sticker Pack Opener", priority = 12)]
    public static void Build()
    {
        EnsurePillBorder();
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var go = new GameObject("StickerPackOpener", typeof(RectTransform), typeof(StickerPackOpener));
        var opener = go.GetComponent<StickerPackOpener>();
        opener.textures = LoadStickerTextures();
        opener.packFrame = Sprite($"{ShopArt}/card_frame.png");
        opener.ribbon = Sprite($"{ShopArt}/name_ribbon.png");
        opener.circle = Sprite("Assets/Resources/ui_circle.png");
        opener.glow = Sprite("Assets/Art/UI/MatchSetup/tile_glow.png");
        opener.button = Sprite(PricePill);            // green = buy/confirm (D3)
        opener.newChip = Sprite($"{FriendsArt}/btn_blank_red.png");
        opener.shine = Resources.Load<Material>("UIShine");
        opener.font = Lilita;

        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Ludo NX] Sticker pack opener built -> {PrefabPath} ({opener.textures.Length} sticker sheets).");
    }

    public static Texture2D[] LoadStickerTextures()
    {
        var list = new Texture2D[StickerPackService.Stickers.Length];
        for (int i = 0; i < list.Length; i++)
        {
            var path = StickerPackService.Stickers[i].path;
            list[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (list[i] == null) Debug.LogError($"[Ludo NX] Missing sticker sheet: {path}");
        }
        return list;
    }

    /// <summary>The pill is stretched to a wide button here; give it 9-slice borders if it has none.</summary>
    private static void EnsurePillBorder()
    {
        if (!(AssetImporter.GetAtPath(PricePill) is TextureImporter imp)) return;
        if (imp.spriteBorder != Vector4.zero) return;
        imp.spriteBorder = new Vector4(26, 24, 26, 24);
        imp.SaveAndReimport();
    }

    // ======================================================================================
    // screen tests
    // ======================================================================================

    private const string TestMenu = "Ludo NX/UI/Screen Test/";

    [MenuItem(TestMenu + "Sticker Pack - Open Hype (fresh)", priority = 69)]
    private static void TestOpenHype()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[Ludo NX] Enter play mode first."); return; }
        StickerPackOpener.Open(StickerPackService.Packs[2], true);
    }

    [MenuItem(TestMenu + "Sticker Pack - Reset Owned Packs", priority = 70)]
    private static void TestReset()
    {
        StickerPackService.DebugReset();
        Debug.Log("[Ludo NX] Sticker packs reset — only the Classic starter is owned.");
    }
}
