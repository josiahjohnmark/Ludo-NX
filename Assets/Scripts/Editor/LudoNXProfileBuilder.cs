using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Builds the player profile into Resources/UI/Screens/Profile.prefab from the user's reference
/// (C:\Dev\LudoNX\profile_page\profile_page.png, 440 x 956).
///
/// Follows the reference exactly, with two changes the user asked for: both grids are four
/// tiles across instead of five, and the RECORDS section is gone (records get their own
/// screen). The ten badges sit 4 + 4 + 2, the last two centred. The stats grid holds the
/// sixteen approved profile stats, most important first. No bottom nav: the back arrow leaves.
///
/// Sprites are made by the prep script from the user's assets and pre-composited onto the flat
/// colours they sit on, because the project renders in Linear and see-through layers drift.
///
/// KEY MEASUREMENTS (points, from the reference)
///   tan band 0-107 + 4pt shadow · back arrow 23x29.5 at (31,77) · ring 90 at (220,102)
///   crest 22x25.5 at (220,138) · name plate 259x36 at (219.5,175) · ID y 211
///   joined y 228.5 · tenure y 242.5 · share 121x40 at (219.5,279)
///   panel x 25-415 · tile 62x60, row pitch 65, first tile 10 below panel top
///   badges header y 306 · badges panel top 316 · stats header 12.5 under it, panel 28 under
/// </summary>
public static class LudoNXProfileBuilder
{
    private const string Art = "Assets/Art/UI/Profile";
    private const string Shared = "Assets/Art/UI/Shared";
    private const string PrefabPath = "Assets/Resources/UI/Screens/Profile.prefab";
    private const string TitanOneTtf = "Assets/Fonts/TitanOne-Regular.ttf";
    private const string TitanOnePath = "Assets/Fonts/TitanOne-Regular.asset";
    private const string JakartaRegularPath = "Assets/Fonts/PlusJakartaSans-Regular.asset";
    private const string JakartaBoldPath = "Assets/Fonts/PlusJakartaSans-Bold.asset";

    // colours sampled from the reference
    private static readonly Color PageSlate = new Color32(108, 122, 156, 255);
    private static readonly Color Tan = new Color32(192, 150, 102, 255);
    private static readonly Color Navy = new Color32(5, 29, 68, 255);
    private static readonly Color LabelNavy = new Color32(30, 50, 88, 255);
    private static readonly Color BadgeNumber = new Color32(46, 65, 99, 255);
    private static readonly Color WinRed = new Color32(128, 16, 42, 255);
    private static readonly Color RuleBlack = new Color32(54, 61, 78, 255);    // black 50% on slate
    private static readonly Color RuleNavy = new Color32(56, 75, 112, 255);    // navy 50% on slate
    private static readonly Color DividerNavy = new Color32(52, 70, 104, 255); // navy 50% on panel

    // grid geometry
    private const float PanelX = 25f;
    private const float PanelW = 390f;
    private const int Columns = 4;
    private const float ColW = PanelW / Columns;
    private const float TileW = 62f;
    private const float TileH = 60f;
    private const float RowPitch = 65f;
    private const float FirstTile = 10f;

    private const float BadgesHeaderY = 306f;
    private const float BadgesTop = 316f;
    private const int BadgeRows = 3;
    private const float BadgesH = FirstTile + RowPitch * (BadgeRows - 1) + TileH;        // 200
    private const float StatsHeaderY = BadgesTop + BadgesH + 12.5f;
    private const float StatsTop = BadgesTop + BadgesH + 28f;
    private const int StatRows = 4;
    private const float StatsH = FirstTile + RowPitch * (StatRows - 1) + 68f;             // 273
    private const float ContentH = StatsTop + StatsH + 40f;

    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "header_band.png", new Vector4(0, 16, 0, 0) },
        { "panel_inset.png", new Vector4(48, 48, 48, 48) },
    };

    [MenuItem("Ludo NX/UI/Build Profile Screen (new)", priority = 8)]
    public static void Build()
    {
        ConfigureSprites(Art, Borders);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Make("Profile", null, typeof(CanvasGroup), typeof(PlayerProfileScreen));
        Stretch(root);
        var screen = root.GetComponent<PlayerProfileScreen>();
        screen.SetId(ScreenId.Profile);

        var bg = Make("Background", root, typeof(Image));
        Stretch(bg);
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = PageSlate;
        bgImg.raycastTarget = false;

        BuildPage(root, screen);
        BuildBadgePopup(root, screen);
        BuildToast(root, screen);

        var badges = new List<Sprite>();
        for (int i = 1; i <= 10; i++) badges.Add(Sprite($"{Art}/badge_{i:00}.png"));
        screen.badgeSprites = badges.ToArray();

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Profile screen built -> {PrefabPath}.");
    }

    // ======================================================================================
    // page
    // ======================================================================================

    /// <summary>
    /// The whole page, band included, is one scrolling column. It fits a 440x956 frame with room
    /// to spare, so it only scrolls when a deep cutout pushes it down.
    /// </summary>
    private static void BuildPage(RectTransform root, PlayerProfileScreen screen)
    {
        var band = Band(root, "PageBand", DesignBand.Edge.TopFill, 0f);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.bottomReservePoints = 0f;
        bandComp.safeAreaBaseline = SafeTop;

        var viewport = Make("Viewport", band, typeof(Image), typeof(RectMask2D));
        Stretch(viewport);
        Invisible(viewport.GetComponent<Image>());
        viewport.GetComponent<Image>().raycastTarget = true;

        var content = Make("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(0f, ContentH);
        bandComp.safeAreaContent = content;

        var scroll = band.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.scrollSensitivity = 18f;

        BuildHeader(content, screen);
        BuildIdentity(content, screen);
        BuildBadges(content, screen);
        BuildStats(content, screen);
    }

    private static void BuildHeader(RectTransform parent, PlayerProfileScreen screen)
    {
        // Tan runs far above the frame so a deep cutout or an overscroll never shows slate.
        var tan = Make("HeaderBand", parent, typeof(Image));
        tan.anchorMin = new Vector2(0f, 1f);
        tan.anchorMax = new Vector2(1f, 1f);
        tan.pivot = new Vector2(0.5f, 0f);
        tan.sizeDelta = new Vector2(0f, 711f);
        tan.anchoredPosition = new Vector2(0f, -111f);
        var tanImg = tan.GetComponent<Image>();
        Sliced(tanImg, $"{Art}/header_band.png", 4f);
        tanImg.raycastTarget = false;

        var back = Make("BackButton", parent, typeof(Image), typeof(Button));
        Top(back, H.Left, 31f, 77f, 52f, 52f);
        Invisible(back.GetComponent<Image>());
        var arrow = Picture(back, "Arrow", $"{Shared}/back_btn.png");
        Centre((RectTransform)arrow.transform, 0f, 0f, 23f, 29.5f);
        screen.backButton = back.GetComponent<Button>();
    }

    private static void BuildIdentity(RectTransform parent, PlayerProfileScreen screen)
    {
        var holder = Make("Avatar", parent);
        Top(holder, H.Centre, 220f, 102f, 90f, 90f);

        // Default picture underneath; the player's chosen one is masked in on top of it.
        var fallback = Picture(holder, "DefaultPortrait", $"{HomeArt}/TopBar/profile_pics.png");
        Centre((RectTransform)fallback.transform, 0f, 0f, 66f, 66f);
        var chosen = Make("Portrait", holder);
        Centre(chosen, 0f, 0f, 66f, 66f);
        chosen.gameObject.AddComponent<ProfileAvatarSlot>().diameter = 66f;

        var ring = Picture(holder, "Ring", $"{Art}/avatar_ring.png");
        Centre((RectTransform)ring.transform, 0f, 0f, 90f, 90f);

        var crest = Picture(holder, "LevelBadge", $"{Art}/badge_01.png");
        Centre((RectTransform)crest.transform, 0f, -36f, 22f, 25.5f);
        crest.preserveAspect = true;
        screen.levelBadge = crest;

        var plate = Picture(parent, "NamePlate", $"{Art}/name_plate.png");
        Top((RectTransform)plate.transform, H.Centre, 219.5f, 175f, 259f, 36f);

        var name = Text((RectTransform)plate.transform, "Name", "Josiah J.", 22.3f, Color.black,
            TextAlignmentOptions.Center);
        name.font = FontAt(TitanOnePath);
        name.enableAutoSizing = true;
        name.fontSizeMax = 22.3f;
        name.fontSizeMin = 12f;
        Stretch(name.rectTransform);
        screen.nameLabel = name;

        // "ID:" regular and the number bold, as two runs laid out side by side and centred.
        var idRow = Make("IdRow", parent, typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        Top(idRow, H.Centre, 220f, 211f, 200f, 22f);
        var layout = idRow.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        idRow.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var idKey = Text(idRow, "Key", "ID:", 16f, Color.black, TextAlignmentOptions.Midline);
        idKey.font = FontAt(JakartaRegularPath);
        var idValue = Text(idRow, "Value", "--------", 17.7f, Color.black, TextAlignmentOptions.Midline);
        idValue.font = FontAt(JakartaBoldPath);
        screen.idLabel = idValue;

        var joined = Text(parent, "Joined", "Joined today", 9.5f, Color.black,
            TextAlignmentOptions.Center);
        joined.font = FontAt(JakartaRegularPath);
        Top(joined.rectTransform, H.Centre, 219.5f, 228.5f, 300f, 14f);
        screen.joinedLabel = joined;

        var tenure = Text(parent, "Tenure", "(today)", 10.5f, Color.black, TextAlignmentOptions.Center);
        tenure.font = FontAt(JakartaRegularPath);
        Top(tenure.rectTransform, H.Centre, 219.5f, 242.5f, 200f, 14f);
        screen.tenureLabel = tenure;

        var share = Make("ShareButton", parent, typeof(Image), typeof(Button));
        Top(share, H.Centre, 219.5f, 279f, 110f, 38f);
        var shareImg = share.GetComponent<Image>();
        shareImg.sprite = Sprite($"{Art}/share_link_btn.png");
        shareImg.preserveAspect = true;
        screen.shareButton = share.GetComponent<Button>();
    }

    // ======================================================================================
    // badges
    // ======================================================================================

    private static void BuildBadges(RectTransform parent, PlayerProfileScreen screen)
    {
        // BADGES ▼ in black, rules either side (reference x 89-195 and 262-365).
        Rule(parent, "BadgesRuleL", 142f, BadgesHeaderY, 106f, RuleBlack);
        Rule(parent, "BadgesRuleR", 313.5f, BadgesHeaderY, 103f, RuleBlack);
        var title = Text(parent, "BadgesTitle", "BADGES", 9.5f, Color.black, TextAlignmentOptions.Center);
        title.font = FontAt(JakartaBoldPath);
        Top(title.rectTransform, H.Centre, 220f, BadgesHeaderY, 60f, 14f);
        var arrow = Picture(parent, "BadgesArrow", $"{Art}/dropdown_arrow.png");
        Top((RectTransform)arrow.transform, H.Centre, 249.5f, BadgesHeaderY, 7f, 6f);

        var panel = Panel(parent, "BadgesPanel", BadgesTop, BadgesH);

        screen.badgeSlots = new Image[10];
        screen.badgeLabels = new TextMeshProUGUI[10];
        screen.badgeButtons = new Button[10];

        for (int i = 0; i < 10; i++)
        {
            int row = i / Columns;
            // The last row holds two: they take the middle two columns, so they sit centred.
            int col = row == 2 ? (i % Columns) + 1 : i % Columns;
            float tileTop = FirstTile + row * RowPitch;

            var button = Make($"Badge{i + 1}", panel, typeof(Image), typeof(Button));
            InCard(button, PanelW, H.Left, ColCentre(col), tileTop + TileH / 2f, TileW, TileH);
            button.GetComponent<Image>().sprite = Sprite($"{Art}/tile_cup.png");
            screen.badgeButtons[i] = button.GetComponent<Button>();

            // Strict 1:1 aspect ratio badge icon matching player card proportions (never stretched/fat)
            var icon = Picture(button, "Icon", $"{Art}/badge_{i + 1:00}.png");
            TileChild((RectTransform)icon.transform, 22f, 32f, 32f);
            icon.preserveAspect = true;
            screen.badgeSlots[i] = icon;

            var label = Text(button, "Number", (i + 1).ToString(), 6.5f, BadgeNumber, TextAlignmentOptions.Center);
            TileChild(label.rectTransform, 48f, TileW, 10f);
            screen.badgeLabels[i] = label;
        }

        // Dividers between neighbouring tiles only.
        for (int row = 0; row < BadgeRows; row++)
        {
            int first = row == 2 ? 1 : 0;
            int last = row == 2 ? 2 : Columns - 1;
            for (int col = first; col < last; col++)
                Divider(panel, $"Divider{row}{col}", (col + 1) * ColW, FirstTile + row * RowPitch);
        }
    }

    // ======================================================================================
    // stats
    // ======================================================================================

    private static void BuildStats(RectTransform parent, PlayerProfileScreen screen)
    {
        // STATS in navy, rules either side (reference x 89-195 and 246-350).
        Rule(parent, "StatsRuleL", 142f, StatsHeaderY, 106f, RuleNavy);
        Rule(parent, "StatsRuleR", 298f, StatsHeaderY, 104f, RuleNavy);
        var title = Text(parent, "StatsTitle", "STATS", 9.5f, Navy, TextAlignmentOptions.Center);
        title.font = FontAt(JakartaBoldPath);
        Top(title.rectTransform, H.Centre, 219.5f, StatsHeaderY, 60f, 14f);

        var panel = Panel(parent, "StatsPanel", StatsTop, StatsH);

        int count = Columns * StatRows;
        screen.statValues = new TextMeshProUGUI[count];

        for (int i = 0; i < count; i++)
        {
            int row = i / Columns;
            int col = i % Columns;
            float tileTop = FirstTile + row * RowPitch;

            var tile = Make($"Stat{i + 1}", panel, typeof(Image));
            InCard(tile, PanelW, H.Left, ColCentre(col), tileTop + TileH / 2f, TileW, TileH);
            tile.GetComponent<Image>().sprite = Sprite($"{Art}/tile_cup.png");
            tile.GetComponent<Image>().raycastTarget = false;

            // The first row (games, wins, losses, win rate) is the headline, a touch larger.
            float size = row == 0 ? 19f : 17.6f;
            var value = Text(tile, "Value", "0", size, i == 3 ? WinRed : Navy, TextAlignmentOptions.Center);
            TileChild(value.rectTransform, 22.5f, TileW - 4f, 24f);
            value.enableAutoSizing = true;
            value.fontSizeMax = size;
            value.fontSizeMin = 11f;
            screen.statValues[i] = value;

            // The label hangs below the cup and may be wider than it, up to the column.
            var label = Text(tile, "Label", PlayerProfileScreen.StatLabels[i], 6.3f, LabelNavy,
                TextAlignmentOptions.Center);
            TileChild(label.rectTransform, 55.5f, ColW - 6f, 16f);
            label.lineSpacing = -8f;
        }

        for (int row = 0; row < StatRows; row++)
        {
            for (int col = 0; col < Columns - 1; col++)
                Divider(panel, $"Divider{row}{col}", (col + 1) * ColW, FirstTile + row * RowPitch);
        }
    }

    // ======================================================================================
    // shared pieces
    // ======================================================================================

    private static float ColCentre(int col) => ColW * (col + 0.5f);

    /// <summary>Recessed plate spanning the reference's panel column, 9-sliced.</summary>
    private static RectTransform Panel(RectTransform parent, string name, float top, float height)
    {
        var panel = Make(name, parent, typeof(Image));
        Top(panel, H.Centre, PanelX + PanelW / 2f, top + height / 2f, PanelW, height);
        var img = panel.GetComponent<Image>();
        Sliced(img, $"{Art}/panel_inset.png", 4f);
        img.raycastTarget = false;
        return panel;
    }

    /// <summary>Places a child of a tile by its centre, measured down from the tile's top edge.</summary>
    private static void TileChild(RectTransform rt, float yFromTop, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0f, -yFromTop);
    }

    /// <summary>2pt horizontal rule beside a section title.</summary>
    private static void Rule(RectTransform parent, string name, float cx, float cy, float w, Color colour)
    {
        var img = Picture(parent, name, $"{Art}/solid.png");
        img.color = colour;
        Top(img.rectTransform, H.Centre, cx, cy, w, 2f);
    }

    /// <summary>2pt vertical divider between two tiles of a row (reference: tile top -1 to +45).</summary>
    private static void Divider(RectTransform panel, string name, float x, float tileTop)
    {
        var img = Picture(panel, name, $"{Art}/solid.png");
        img.color = DividerNavy;
        InCard(img.rectTransform, PanelW, H.Left, x, tileTop + 22f, 2f, 46f);
    }

    // ======================================================================================
    // badge popup
    // ======================================================================================

    private static void BuildBadgePopup(RectTransform root, PlayerProfileScreen screen)
    {
        var band = Band(root, "PopupBand", DesignBand.Edge.Fill, 0f);

        var popup = Make("BadgePopup", band);
        Stretch(popup);
        screen.badgePopup = popup.gameObject;

        var blocker = Make("Blocker", popup, typeof(Image), typeof(Button));
        Stretch(blocker);
        blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        screen.badgePopupBlocker = blocker.GetComponent<Button>();

        var card = Make("Card", popup, typeof(Image));
        Centre(card, 0f, 0f, 300f, 240f);
        Sliced(card.GetComponent<Image>(), $"{FriendsArt}/panel_combined.png", 2f);

        var icon = Picture(card, "Icon", $"{Art}/badge_01.png");
        Centre((RectTransform)icon.transform, 0f, 56f, 86f, 86f);
        icon.preserveAspect = true;
        screen.badgePopupIcon = icon;

        var title = Text(card, "Title", "FIRST STEPS", 22f, Brown, TextAlignmentOptions.Center);
        Centre(title.rectTransform, 0f, -8f, 260f, 30f);
        screen.badgePopupTitle = title;

        var body = Text(card, "Body", "Earned at level 5.", 13f, SubInk, TextAlignmentOptions.Center);
        body.textWrappingMode = TextWrappingModes.Normal;
        Centre(body.rectTransform, 0f, -62f, 250f, 60f);
        screen.badgePopupBody = body;

        popup.gameObject.SetActive(false);
    }

    private static void BuildToast(RectTransform root, PlayerProfileScreen screen)
    {
        var band = Band(root, "ToastBand", DesignBand.Edge.Fill, 0f);

        var toast = Make("Message", band, typeof(CanvasGroup), typeof(Image));
        toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 0f);
        toast.pivot = new Vector2(0.5f, 0.5f);
        toast.sizeDelta = new Vector2(320f, 52f);
        toast.anchoredPosition = new Vector2(0f, 90f);
        Sliced(toast.GetComponent<Image>(), $"{FriendsArt}/btn_blank_tan.png", 2f);
        toast.GetComponent<Image>().raycastTarget = false;

        var label = Text(toast, "Label", "Copied", 15f, Cream, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);

        screen.toast = toast.GetComponent<CanvasGroup>();
        screen.toastText = label;
    }

    // ======================================================================================
    // fonts
    // ======================================================================================

    private static TMP_FontAsset FontAt(string path)
    {
        if (path == TitanOnePath) EnsureTitanOne();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font == null) Debug.LogError($"[Ludo NX] Font asset missing at {path}.");
        return font != null ? font : Lilita;
    }

    /// <summary>
    /// The name on the reference is set in Super Bouncer. Makes its TMP asset once, the same way
    /// LudoFontAssetRebuilder makes the others: a dynamic SDF atlas stored inside the asset.
    /// </summary>
    private static void EnsureTitanOne()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitanOnePath) != null) return;

        var source = AssetDatabase.LoadAssetAtPath<Font>(TitanOneTtf);
        if (source == null) { Debug.LogError($"[Ludo NX] Missing {TitanOneTtf}."); return; }

        var asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic);
        asset.name = "TitanOne-Regular SDF";
        AssetDatabase.CreateAsset(asset, TitanOnePath);
        asset.atlasTextures[0].name = asset.name + " Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        asset.material.name = asset.name + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
}
