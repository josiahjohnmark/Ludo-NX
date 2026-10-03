using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Builds the Shop into Resources/UI/Screens/Shop.prefab from the user's own reference: wood top
/// bar, SHOP title with a cart glyph, four category tabs, and a 3-column card grid below.
///
/// KEY MEASUREMENTS (points of the 440x956 frame, measured directly off the user's reference —
/// it was exported at 1x, unlike the Friends kit's 2x references, so these are frame points as-is)
///   title "SHOP" at y 145, cart icon at (152, 145), tagline at y 175
///   tabs row centred y 199, 4 tabs ~88 wide, 8 gap, starting x 27
///   grid: card 121x171, left margin 27, column gap 11, row pitch 181, first row top y 247
/// </summary>
public static class LudoNXStoreBuilder
{
    private const string ShopArt = "Assets/Art/UI/Shop";
    private const string DiceArt = "Assets/Art/UI/Shop/Dice";
    private const string SetupArt = "Assets/Art/UI/MatchSetup";
    private const string PrefabPath = "Assets/Resources/UI/Screens/Shop.prefab";

    private const float HeaderH = 102f;
    private const float CardW = 121f;
    private const float CardH = 171f;
    private const float ColGap = 11f;
    private const float RowGap = 10f;
    private const float GridLeft = 27f;

    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "card_frame.png", new Vector4(20, 20, 20, 20) },
        { "name_ribbon.png", new Vector4(30, 0, 30, 0) },
        { "price_pill.png", new Vector4(22, 22, 22, 22) },
    };

    [MenuItem("Ludo NX/UI/Build Shop Screen (new)", priority = 11)]
    public static void Build()
    {
        ConfigureSprites(ShopArt, Borders);
        ConfigureSprites(DiceArt, null);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Make("Shop", null, typeof(CanvasGroup), typeof(StoreScreen));
        Stretch(root);
        var screen = root.GetComponent<StoreScreen>();
        screen.SetId(ScreenId.Shop);

        var bg = Make("Background", root, typeof(Image));
        Stretch(bg);
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Backgrounds/bg_match_emboss.jpg");
        bgImg.color = Color.white;
        bgImg.raycastTarget = false;
        var cover = bg.gameObject.AddComponent<BackgroundCoverFitter>();
        cover.overscan = 1.05f;

        BuildHeader(root, screen);
        BuildBody(root, screen);
        BuildPurchasePopup(root, screen);
        BuildToast(root, screen);

        var diceSprites = new List<Sprite>();
        foreach (var f in new[]
        {
            "dice_01_classic","dice_02_ruby","dice_03_sapphire","dice_04_emerald","dice_05_gold",
            "dice_06_amethyst","dice_07_onyx","dice_08_blossom","dice_09_teal","dice_10_amber",
        })
        {
            diceSprites.Add(Sprite($"{DiceArt}/{f}.png"));
        }
        screen.diceSprites = diceSprites.ToArray();
        screen.tabSelected = Sprite($"{SetupArt}/tile_selected.png");
        screen.tabIdle = Sprite($"{SetupArt}/tile_tan.png");

        screen.energyIcon = Sprite($"{HomeArt}/TopBar/energy_icon.png");
        screen.stickerTextures = LudoNXStickerPackBuilder.LoadStickerTextures();
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Shop screen built -> {PrefabPath}.");
    }

    // ======================================================================================
    // header
    // ======================================================================================

    private static void BuildHeader(RectTransform root, StoreScreen screen)
    {
        var band = Band(root, "HeaderBand", DesignBand.Edge.Top, HeaderH);

        var panel = Make("Panel", band, typeof(Image));
        var panelImg = panel.GetComponent<Image>();
        panelImg.sprite = Sprite($"{HomeArt}/TopBar/top_navigation_bar.png");
        panelImg.raycastTarget = false;
        var cover = panel.gameObject.AddComponent<BackgroundCoverFitter>();
        cover.alignment = new Vector2(0.5f, 0f);
        cover.overscan = 1f;

        var widgets = Make("HeaderWidgets", band);
        Stretch(widgets);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.safeAreaBaseline = SafeTop;
        bandComp.safeAreaContent = widgets;

        // Prominent SHOP title + 3D canopy shop icon (using bottom nav shop icon nav_shop.png)
        var cartIcon = Picture(widgets, "CartIcon", $"{HomeArt}/nav_shop.png");
        Top((RectTransform)cartIcon.transform, H.Centre, 155f, 74f, 38f, 38f);
        cartIcon.preserveAspect = true;

        var title = Text(widgets, "Title", "SHOP", 39.3f, Ink, TextAlignmentOptions.MidlineLeft);
        title.font = Lilita;
        Top((RectTransform)title.transform, H.Centre, 240f, 74.75f, 150f, 52f);

        var menu = Make("MenuButton", widgets, typeof(Image), typeof(Button));
        Top(menu, H.Right, 410f, 68f, 44f, 44f);
        Invisible(menu.GetComponent<Image>());
        var glyph = Picture(menu, "Glyph", $"{HomeArt}/TopBar/navigation_btn.png");
        Centre((RectTransform)glyph.transform, 0f, 0f, 30f, 26f);
        screen.menuButton = menu.GetComponent<Button>();

        screen.coinsLabel = null;
        screen.energyLabel = null;
    }

    // ======================================================================================
    // tabs + grid
    // ======================================================================================

    private static void BuildBody(RectTransform root, StoreScreen screen)
    {
        var band = Band(root, "BodyBand", DesignBand.Edge.TopFill, 0f);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.bottomReservePoints = NavReserve;
        bandComp.bottomReserveReference = new Vector2(402f, 874f);
        bandComp.safeAreaBaseline = SafeTop;

        var content = Make("BodyContent", band);
        Stretch(content);
        bandComp.safeAreaContent = content;

        BuildTabs(content, screen);

        var viewport = Make("Viewport", content, typeof(Image), typeof(RectMask2D));
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(0f, 0f);
        viewport.offsetMax = new Vector2(0f, -170f); // More breathing room for cards since title moved to top bar
        Invisible(viewport.GetComponent<Image>());
        viewport.GetComponent<Image>().raycastTarget = true;

        var grid = Make("Grid", viewport, typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        grid.anchorMin = new Vector2(0f, 1f);
        grid.anchorMax = new Vector2(1f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.offsetMin = new Vector2(GridLeft, 0f);
        grid.offsetMax = new Vector2(-GridLeft, 0f);
        grid.anchoredPosition = new Vector2(0f, -8f);

        var gl = grid.GetComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(CardW, CardH);
        gl.spacing = new Vector2(ColGap, RowGap);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 3;
        gl.childAlignment = TextAnchor.UpperLeft;
        grid.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = grid;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.scrollSensitivity = 18f;
        screen.gridContent = grid;

        screen.cardTemplate = BuildCard(grid);

        var comingSoon = Make("ComingSoon", viewport);
        Stretch(comingSoon);
        var label = Text(comingSoon, "Label", "Token skins are coming soon", 15f,
            new Color32(214, 206, 192, 200), TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.Normal;
        Centre(label.rectTransform, 0f, 60f, 320f, 40f);
        comingSoon.gameObject.SetActive(false);
        screen.comingSoonState = comingSoon.gameObject;
        screen.comingSoonLabel = label;
    }

    /// <summary>
    /// "SHOP" + cart glyph + tagline. Lives in BodyBand, not HeaderBand: the reference shows it
    /// on the plain background BELOW the wood top bar, not on the wood texture itself, and
    /// putting it in Header made it a separate sibling that the tabs band (built after) drew
    /// straight over.
    /// </summary>
    private static void BuildTitle(RectTransform parent)
    {
        var icon = Picture(parent, "CartIcon", $"{ShopArt}/icon_cart.png");
        Top((RectTransform)icon.transform, H.Centre, 152f, 145f, 28f, 28f);

        var title = Text(parent, "Title", "SHOP", 34f, Color.white, TextAlignmentOptions.MidlineLeft);
        Top(title.rectTransform, H.Centre, 175f + 90f, 148f, 180f, 44f);   // left-aligned text starts at x=175, clear of the cart icon

        var tagline = Text(parent, "Tagline", "Get Cool Items, Enhance Your Game.", 11f,
            new Color32(214, 206, 192, 220), TextAlignmentOptions.Center);
        Top(tagline.rectTransform, H.Centre, 220f, 175f, 300f, 18f);
    }

    private static void BuildTabs(RectTransform parent, StoreScreen screen)
    {
        var row = Make("Tabs", parent);
        Top(row, H.Centre, 220f, 130f, 386f, 34f);

        string[] names = { "DICE SKINS", "TOKENS", "ENERGY", "EMOJI" };
        string[] icons =
        {
            "Assets/Art/UI/Missions/icon_dice.png",
            "Assets/Art/UI/Icons/coin_3d.png",
            $"{HomeArt}/TopBar/energy_icon.png",
            $"{ShopArt}/icon_emoji_tab.png",
        };

        screen.tabButtons = new Button[4];
        screen.tabBackgrounds = new Image[4];
        screen.tabLabels = new TextMeshProUGUI[4];

        float tabW = 92f, gap = 5f;
        for (int i = 0; i < 4; i++)
        {
            float cx = i * (tabW + gap) + tabW / 2f;
            var tab = Make(names[i], row, typeof(Image), typeof(Button));
            InCard(tab, 386f, H.Left, cx, 20f, tabW, 36f);
            var img = tab.GetComponent<Image>();
            Sliced(img, $"{SetupArt}/tile_tan.png", 2f);

            var icon = Picture(tab, "Icon", icons[i]);
            InCard((RectTransform)icon.transform, tabW, H.Left, 16f, 18f, 16f, 16f);

            var label = Text(tab, "Label", names[i], 9f, Cream, TextAlignmentOptions.MidlineLeft);
            LeftIn(label.rectTransform, 28f, 20f, tabW - 30f, 16f);

            screen.tabButtons[i] = tab.GetComponent<Button>();
            screen.tabBackgrounds[i] = img;
            screen.tabLabels[i] = label;
        }
    }

    // ======================================================================================
    // card template
    // ======================================================================================

    private static StoreItemCard BuildCard(RectTransform parent)
    {
        var rt = Make("DiceCardTemplate", parent, typeof(StoreItemCard));
        rt.sizeDelta = new Vector2(CardW, CardH);

        var card = rt.GetComponent<StoreItemCard>();

        var frame = Make("Frame", rt, typeof(Image));
        Stretch(frame);
        var frameImg = frame.GetComponent<Image>();
        Sliced(frameImg, $"{ShopArt}/card_frame.png", 2f);
        frameImg.raycastTarget = false;
        card.frame = frameImg;

        var icon = Picture(rt, "Icon", $"{DiceArt}/dice_01_classic.png");
        Centre((RectTransform)icon.transform, 0f, 22f, 70f, 76f);
        card.icon = icon;

        var ribbon = Picture(rt, "Ribbon", $"{ShopArt}/name_ribbon.png");
        ribbon.type = Image.Type.Sliced;
        ribbon.pixelsPerUnitMultiplier = 2f;
        Centre((RectTransform)ribbon.transform, 0f, -32f, 106f, 23f);
        card.ribbon = ribbon;

        var name = Text(ribbon.transform as RectTransform, "Label", "CLASSIC", 11f, Cream,
            TextAlignmentOptions.Center);
        Stretch(name.rectTransform);
        card.nameLabel = name;

        var buy = Make("BuyButton", rt, typeof(Image), typeof(Button));
        Centre(buy, 0f, -60f, 96f, 30f);
        var pill = buy.GetComponent<Image>();
        Sliced(pill, $"{ShopArt}/price_pill.png", 2f);
        card.pricePill = pill;
        card.buyButton = buy.GetComponent<Button>();

        var price = Text(buy, "Label", "$0.99", 13f, new Color32(20, 46, 30, 255),
            TextAlignmentOptions.Center);
        Stretch(price.rectTransform);
        card.priceLabel = price;

        rt.gameObject.SetActive(false);
        return card;
    }

    // ======================================================================================
    // purchase popup (placeholder)
    // ======================================================================================

    private static void BuildPurchasePopup(RectTransform root, StoreScreen screen)
    {
        var band = Band(root, "PopupBand", DesignBand.Edge.Fill, 0f);

        var popup = Make("BuyPopup", band);
        Stretch(popup);
        screen.buyPopup = popup.gameObject;

        var blocker = Make("Blocker", popup, typeof(Image), typeof(Button));
        Stretch(blocker);
        blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        screen.buyPopupBlocker = blocker.GetComponent<Button>();

        var card = Make("Card", popup, typeof(Image));
        Centre(card, 0f, 0f, 300f, 280f);
        Sliced(card.GetComponent<Image>(), $"{FriendsArt}/panel_combined.png", 2f);

        var icon = Picture(card, "Icon", $"{DiceArt}/dice_01_classic.png");
        Centre((RectTransform)icon.transform, 0f, 70f, 90f, 98f);
        screen.buyPopupIcon = icon;

        var title = Text(card, "Title", "CLASSIC", 20f, Brown, TextAlignmentOptions.Center);
        Centre(title.rectTransform, 0f, 0f, 260f, 28f);
        screen.buyPopupTitle = title;

        var price = Text(card, "Price", "$0.99", 26f, new Color32(46, 122, 72, 255),
            TextAlignmentOptions.Center);
        Centre(price.rectTransform, 0f, -38f, 200f, 36f);
        screen.buyPopupPrice = price;

        var confirm = Make("Confirm", card, typeof(Image), typeof(Button));
        Centre(confirm, 0f, -92f, 220f, 48f);
        Sliced(confirm.GetComponent<Image>(), $"{FriendsArt}/btn_blank_tan.png", 2f);
        screen.buyPopupConfirm = confirm.GetComponent<Button>();
        var confirmLabel = Text(confirm, "Label", "BUY", 18f, Cream, TextAlignmentOptions.Center);
        Stretch(confirmLabel.rectTransform);
        confirmLabel.rectTransform.offsetMin = new Vector2(0f, 3f);

        var cancel = Make("Cancel", card, typeof(Image), typeof(Button));
        Centre(cancel, 0f, -140f, 220f, 40f);
        Invisible(cancel.GetComponent<Image>());
        screen.buyPopupCancel = cancel.GetComponent<Button>();
        var cancelLabel = Text(cancel, "Label", "CANCEL", 14f, new Color32(150, 138, 120, 255),
            TextAlignmentOptions.Center);
        Stretch(cancelLabel.rectTransform);
    }

    private static void BuildToast(RectTransform root, StoreScreen screen)
    {
        var band = Band(root, "ToastBand", DesignBand.Edge.Fill, 0f);

        var toast = Make("Message", band, typeof(CanvasGroup), typeof(Image));
        toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 0f);
        toast.pivot = new Vector2(0.5f, 0.5f);
        toast.sizeDelta = new Vector2(340f, 52f);
        toast.anchoredPosition = new Vector2(0f, 190f);
        Sliced(toast.GetComponent<Image>(), $"{FriendsArt}/btn_blank_tan.png", 2f);
        toast.GetComponent<Image>().raycastTarget = false;

        var label = Text(toast, "Label", "Purchase flow coming soon", 14f, Cream, TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.Normal;
        Stretch(label.rectTransform);

        screen.toast = toast.GetComponent<CanvasGroup>();
        screen.toastText = label;
    }
}
