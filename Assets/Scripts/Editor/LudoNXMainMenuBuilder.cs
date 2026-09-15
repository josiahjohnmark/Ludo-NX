using System;
using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the Ludo NX main menu to the measured design reference.
///
/// DESIGN POINTS
/// Every number here is in points of 'main_menu_ui_ui_assets/ui refence main menu.png'
/// (402 x 874), measured off that image. Nothing is pre-multiplied into canvas units: each
/// strip of the screen is a <see cref="DesignBand"/> that scales its points to the screen
/// uniformly at runtime. On the reference aspect that reproduces the design exactly, and on
/// wider screens (tablets, foldables) nothing is stretched along one axis:
///   - items pinned Left / Right stay at that screen edge (profile, hamburger);
///   - items anchored Proportionally spread with the width (coins, energy, nav icons),
///     which is how the user's own 402 -> 440 frames behave;
///   - flat plates and rails are 9-sliced, so only their plain middle extends;
///   - the textured top panel is cover-fitted and trims its top, never stretched.
///
/// OUTPUT
/// ScreenManager owns the canvas and loads screens as prefabs, so this writes:
///   Resources/UI/Screens/Home.prefab               - background, top bar, badges, logo, carousel
///   Resources/UI/Components/UniversalNavBar.prefab - bottom navigation with its drop shadow
///
/// KEY MEASUREMENTS (points, validated to within 2px against the reference)
///   top panel 402x102 at y0 · cream pill 386x54 at (8,45) · profile 60x60 centre (40,69)
///   coin 110x34 centre (139,70) · energy 91x34 centre (262.5,70) · menu centre (360,68)
///   missions 68x122 centre (62,196) · cup 72x138 centre (339,191) · logo 246x148 centre (201,346)
///   cards 180x280 slot, back scale 0.79, spacing 97, band centre y 560
///   nav plate 402x116 · icons bottom-aligned 36 above the screen bottom
///   nav shadow fitted to the reference: 6.5pt lift, 4pt softness, 38% black
///
/// The bottom-nav icon PNGs have their labels baked in, so no TextMeshPro labels are added.
/// </summary>
public static class LudoNXMainMenuBuilder
{
    private const string Art = "Assets/Art/UI/HomeScreen";
    private const string ScreensDir = "Assets/Resources/UI/Screens";
    private const string ComponentsDir = "Assets/Resources/UI/Components";
    private const string HomePrefab = ScreensDir + "/Home.prefab";
    private const string NavPrefab = ComponentsDir + "/UniversalNavBar.prefab";

    // --- reference frame, in points -------------------------------------------------------
    private static readonly Vector2 Reference = new Vector2(402f, 874f);
    private const float RefW = 402f;
    private const float RefH = 874f;

    private const float TopBandH = 102f;
    private const float SafeTop = 45f;        // the gap the user left above the cream pill
    private const float NavBandH = 116f;
    private const float NavIconBottom = 36f;  // icons sit this far above the screen bottom

    private const float CardW = 180f;
    private const float CardH = 280f;
    private const float CardBandY = 560f;
    private const float CardSpacing = 97f;
    private const float CardBackScale = 0.79f;

    private enum H { Left, Right, Proportional, Centre }
    private enum V { Top, Bottom, Centre }

    [MenuItem("Ludo NX/UI/Build Main Menu", priority = 1)]
    public static void Build()
    {
        WarnIfNavShadowMissing();
        ConfigureSprites();
        Directory.CreateDirectory(ScreensDir);
        Directory.CreateDirectory(ComponentsDir);

        SavePrefab(BuildNavBar(), NavPrefab);
        SavePrefab(BuildHome(), HomePrefab);

        RetireSceneMenus();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Main menu rebuilt in design points ({RefW}x{RefH}) -> {HomePrefab}, {NavPrefab}.");
    }

    // ======================================================================================
    // Home screen
    // ======================================================================================

    private static GameObject BuildHome()
    {
        var root = Make("Home", null, typeof(CanvasGroup), typeof(HomeScreen));
        Stretch(root);
        var screen = root.GetComponent<HomeScreen>();

        // Background covers the whole screen and is cropped evenly, never stretched.
        var bg = Make("Background", root, typeof(Image));
        Stretch(bg);
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = Sprite("bg_main_menu");
        bgImg.raycastTarget = false;
        bg.gameObject.AddComponent<BackgroundCoverFitter>();

        var content = Band(root, "ContentBand", DesignBand.Edge.Fill, RefH);
        BuildBadges(content, screen);
        BuildLogo(content);
        screen.modeCarousel = BuildCarousel(content);

        BuildTopBar(root, screen);
        return root.gameObject;
    }

    private static void BuildTopBar(RectTransform root, HomeScreen screen)
    {
        var band = Band(root, "TopBand", DesignBand.Edge.Top, TopBandH);

        // Textured wood panel: cover-fitted to the band and pinned to its bottom edge, so on a
        // wider screen it scales evenly and trims the top (which sits under the status bar).
        var panel = Make("Panel", band, typeof(Image));
        var panelImg = panel.GetComponent<Image>();
        panelImg.sprite = Sprite("TopBar/top_navigation_bar");
        panelImg.raycastTarget = false;
        var cover = panel.gameObject.AddComponent<BackgroundCoverFitter>();
        cover.alignment = new Vector2(0.5f, 0f);
        cover.overscan = 1f;

        // Widgets live in their own container so a deep cutout can push them down without
        // moving the panel, which keeps bleeding to the top of the screen.
        var widgets = Make("Widgets", band);
        Stretch(widgets);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.safeAreaBaseline = SafeTop;
        bandComp.safeAreaContent = widgets;

        // Cream pill: 8pt from both edges, so it extends with the screen width.
        var pill = Make("Pill", widgets, typeof(Image));
        StretchH(pill, 8f, 8f, V.Top, SafeTop + 27f, 54f, TopBandH);
        Sliced(pill.GetComponent<Image>(), "TopBar/top_navigation_bar_inner_card", 1f);
        pill.GetComponent<Image>().raycastTarget = false;

        screen.avatarButton = BuildProfile(widgets);
        BuildCoinPill(widgets, screen);
        BuildEnergyPill(widgets, screen);
        screen.settingsButton = BuildMenuButton(widgets);
    }

    private static Button BuildProfile(RectTransform bar)
    {
        var btn = Make("ProfileButton", bar, typeof(Image), typeof(Button));
        Place(btn, H.Left, 40f, V.Top, 69f, 60f, 60f, TopBandH);
        Invisible(btn.GetComponent<Image>());

        Art_(btn, "Portrait", "TopBar/profile_pics", 0f, -2f, 50f, 50f);
        Art_(btn, "Frame", "TopBar/profile_frame", 0f, 0f, 60f, 60f);
        Art_(btn, "Crest", "TopBar/ludo_nx_badge_for_profile_frame", 0f, 24f, 14f, 16f);
        return btn.GetComponent<Button>();
    }

    private static void BuildCoinPill(RectTransform bar, HomeScreen screen)
    {
        var pill = Make("CoinPill", bar, typeof(Image), typeof(Button));
        Place(pill, H.Proportional, 139f, V.Top, 70f, 110f, 34f, TopBandH);
        pill.GetComponent<Image>().sprite = Sprite("TopBar/NX_coins_bar");

        Art_(pill, "Inner", "TopBar/nx_coin_bar_inner_frame", -2f, 0f, 77f, 28f);
        Art_(pill, "CoinIcon", "TopBar/NX_coin", -37f, -1f, 25f, 27f);

        var label = Label(pill, "CoinAmount", "12,000", 17f);
        AtCentre((RectTransform)label.transform, 1f, -1f, 56f, 22f);

        var add = Make("AddButton", pill, typeof(Image), typeof(Button));
        AtCentre(add, 37f, -2f, 21f, 23f);
        add.GetComponent<Image>().sprite = Sprite("TopBar/NX_coin_add_icon");

        screen.coinsChipButton = pill.GetComponent<Button>();
        screen.coinsChipLabel = label;
    }

    private static void BuildEnergyPill(RectTransform bar, HomeScreen screen)
    {
        var pill = Make("EnergyPill", bar, typeof(Image), typeof(Button));
        Place(pill, H.Proportional, 262.5f, V.Top, 70f, 91f, 34f, TopBandH);
        pill.GetComponent<Image>().sprite = Sprite("TopBar/energy_bar_outer_frame");

        Art_(pill, "Inner", "TopBar/energy_bar_inner_frame", -10.5f, 0f, 64f, 28f);
        Art_(pill, "BoltIcon", "TopBar/energy_icon", -27.5f, 0f, 17f, 24f);

        var label = Label(pill, "EnergyAmount", "7/20", 17f);
        AtCentre((RectTransform)label.transform, 3.5f, -1f, 42f, 22f);

        var timer = Label(pill, "EnergyTimer", "00:24", 9f);
        timer.color = new Color32(214, 226, 245, 255);
        AtCentre((RectTransform)timer.transform, 32.5f, 0f, 30f, 14f);

        screen.energyChipButton = pill.GetComponent<Button>();
        screen.energyChipLabel = label;
        screen.energyTimerLabel = timer;
    }

    private static Button BuildMenuButton(RectTransform bar)
    {
        var btn = Make("MenuButton", bar, typeof(Image), typeof(Button));
        Place(btn, H.Right, 360f, V.Top, 68f, 44f, 44f, TopBandH);
        Invisible(btn.GetComponent<Image>());

        Art_(btn, "Icon", "TopBar/navigation_btn", 0f, 0f, 25f, 21f);
        Art_(btn, "NotificationDot", "Branding/notification_icon", 10f, -13f, 10f, 10f);
        return btn.GetComponent<Button>();
    }

    private static void BuildBadges(RectTransform content, HomeScreen screen)
    {
        var missions = Make("MissionsBadge", content, typeof(Image), typeof(Button));
        Place(missions, H.Proportional, 62f, V.Centre, 196f, 68f, 122f, RefH);
        missions.GetComponent<Image>().sprite = Sprite("Badges/mission_btn");
        screen.missionsButton = missions.GetComponent<Button>();

        var cup = Make("CupBadge", content, typeof(Image), typeof(Button));
        Place(cup, H.Proportional, 339f, V.Centre, 191f, 72f, 138f, RefH);
        cup.GetComponent<Image>().sprite = Sprite("Badges/refrence_tournament_btn");
        screen.tournamentsButton = cup.GetComponent<Button>();
    }

    private static void BuildLogo(RectTransform content)
    {
        var logo = Make("Logo", content, typeof(Image));
        Place(logo, H.Centre, 201f, V.Centre, 346f, 246f, 148f, RefH);
        var img = logo.GetComponent<Image>();
        img.sprite = Sprite("Branding/LUDO_NX_logo");
        img.raycastTarget = false;
    }

    private static GameModeCarousel BuildCarousel(RectTransform content)
    {
        var root = Make("GameModeCarousel", content, typeof(GameModeCarousel));
        Place(root, H.Centre, RefW / 2f, V.Centre, CardBandY, RefW, CardH, RefH);

        var carousel = root.GetComponent<GameModeCarousel>();
        carousel.spacing = CardSpacing;
        carousel.frontScale = 1f;
        carousel.backScale = CardBackScale;
        carousel.frontAlpha = 1f;
        carousel.backAlpha = 1f;
        carousel.dragThreshold = 45f;

        carousel.cards = new[]
        {
            Card(root, "Card_Online",    "Cards/vs_cpu_btn",      GameModeCarousel.Mode.Online),
            Card(root, "Card_QuickPlay", "Cards/quick_play_btn",  GameModeCarousel.Mode.QuickPlay),
            Card(root, "Card_LocalPlay", "Cards/loccal_play_btn", GameModeCarousel.Mode.LocalPlay),
        };
        return carousel;
    }

    private static GameModeCarousel.ModeCard Card(
        RectTransform parent, string name, string sprite, GameModeCarousel.Mode mode)
    {
        var rt = Make(name, parent, typeof(Image), typeof(Button), typeof(CanvasGroup));
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(CardW, CardH);
        rt.anchoredPosition = Vector2.zero;

        var img = rt.GetComponent<Image>();
        img.sprite = Sprite(sprite);
        img.preserveAspect = true;

        return new GameModeCarousel.ModeCard
        {
            mode = mode,
            rectTransform = rt,
            canvasGroup = rt.GetComponent<CanvasGroup>(),
            button = rt.GetComponent<Button>(),
        };
    }

    // ======================================================================================
    // Bottom navigation
    // ======================================================================================

    private static GameObject BuildNavBar()
    {
        var nav = Make("UniversalNavBar", null, typeof(DesignBand), typeof(HomeNavBar));
        ConfigureBand(nav.GetComponent<DesignBand>(), DesignBand.Edge.Bottom, NavBandH);

        // Shadow cast upward from the plate, so whatever sits above looks tucked behind the bar.
        // Its sprite spans 24pt above to 8pt below the plate's top edge, 15pt past each side.
        var shadow = Make("DropShadow", nav, typeof(Image));
        shadow.anchorMin = new Vector2(0f, 0f);
        shadow.anchorMax = new Vector2(1f, 0f);
        shadow.pivot = new Vector2(0.5f, 0f);
        shadow.offsetMin = new Vector2(-15f, NavBandH - 8f);
        shadow.offsetMax = new Vector2(15f, NavBandH + 24f);
        Sliced(shadow.GetComponent<Image>(), "BottomNav/nav_shadow", 4f);
        shadow.GetComponent<Image>().raycastTarget = false;

        var plate = Make("Plate", nav, typeof(Image));
        Stretch(plate);
        Sliced(plate.GetComponent<Image>(), "BottomNav/botton_nav_background_layer", 1f);
        plate.GetComponent<Image>().raycastTarget = false;

        // Rails run from each edge to a fixed gap either side of the HOME roof, so the gap
        // follows HOME as the icons spread and the rails only ever change length.
        const float homeX = 200f;
        float homeAnchor = homeX / RefW;
        const float railBottom = NavBandH - 8f; // 4pt below the plate top, 4pt thick
        const float railTop = NavBandH - 4f;
        Rail(nav, "RailLeft", "BottomNav/horizontal_line_for_bottom_nav_at_the_left",
            0f, homeAnchor, 5f, -(homeX - 175f), railBottom, railTop);
        Rail(nav, "RailRight", "BottomNav/horizontal_line_for_bottom_nav_at_the_right",
            homeAnchor, 1f, 233f - homeX, -6f, railBottom, railTop);

        foreach (float x in new[] { 75f, 155f, 248f, 320f })
        {
            var div = Make($"Divider_{x:0}", nav, typeof(Image));
            Place(div, H.Proportional, x, V.Bottom, 44f, 4f, 68f, NavBandH);
            var img = div.GetComponent<Image>();
            img.sprite = Sprite("BottomNav/line_dimarctions_for_bottom_nav_icons");
            img.raycastTarget = false;
        }

        var bar = nav.GetComponent<HomeNavBar>();
        // The reference shows no lift, zoom or dimming on the selected tab.
        bar.selectedRaise = 0f;
        bar.selectedScale = 1f;
        bar.unselectedAlpha = 1f;
        bar.items = new List<HomeNavBar.NavItem>
        {
            Tab(nav, "Tab_Album",   "BottomNav/album_btn",   38.5f, 46f, 58f, HomeNavBar.Tab.Album,   ScreenId.PawnCollection),
            Tab(nav, "Tab_Friends", "BottomNav/friends_btn", 116f,  49f, 57f, HomeNavBar.Tab.Friends, ScreenId.Friends),
            Tab(nav, "Tab_Home",    "BottomNav/home_btn",    200f,  74f, 81f, HomeNavBar.Tab.Home,    ScreenId.Home),
            Tab(nav, "Tab_Shop",    "BottomNav/shop_btn",    281f,  44f, 58f, HomeNavBar.Tab.Shop,    ScreenId.Shop),
            Tab(nav, "Tab_Levels",  "BottomNav/levels_btn",  359f,  50f, 61f, HomeNavBar.Tab.Levels,  ScreenId.PlayerStats),
        };
        return nav.gameObject;
    }

    private static void Rail(RectTransform nav, string name, string sprite,
        float anchorFrom, float anchorTo, float insetFrom, float insetTo, float bottom, float top)
    {
        var rt = Make(name, nav, typeof(Image));
        rt.anchorMin = new Vector2(anchorFrom, 0f);
        rt.anchorMax = new Vector2(anchorTo, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(insetFrom, bottom);
        rt.offsetMax = new Vector2(insetTo, top);
        Sliced(rt.GetComponent<Image>(), sprite, 1f);
        rt.GetComponent<Image>().raycastTarget = false;
    }

    private static HomeNavBar.NavItem Tab(RectTransform nav, string name, string sprite,
        float cx, float w, float h, HomeNavBar.Tab tab, ScreenId target)
    {
        var rt = Make(name, nav, typeof(Image), typeof(Button));
        // Bottom-aligned: the centre sits (bottom offset + half height) above the band bottom.
        Place(rt, H.Proportional, cx, V.Bottom, NavBandH - (NavIconBottom + h / 2f), w, h, NavBandH);
        var img = rt.GetComponent<Image>();
        img.sprite = Sprite(sprite);

        return new HomeNavBar.NavItem
        {
            tab = tab,
            targetScreen = target,
            button = rt.GetComponent<Button>(),
            iconImage = img,
        };
    }

    // ======================================================================================
    // layout helpers - every value is in design points
    // ======================================================================================

    private static RectTransform Band(RectTransform parent, string name, DesignBand.Edge edge, float height)
    {
        var rt = Make(name, parent, typeof(DesignBand));
        ConfigureBand(rt.GetComponent<DesignBand>(), edge, height);
        return rt;
    }

    private static void ConfigureBand(DesignBand band, DesignBand.Edge edge, float height)
    {
        band.referenceSize = Reference;
        band.edge = edge;
        band.designHeight = height;
    }

    /// <summary>
    /// Places by centre. <paramref name="cx"/> is across the reference width;
    /// <paramref name="cy"/> is down from the top of a strip <paramref name="stripHeight"/> tall.
    /// </summary>
    private static void Place(RectTransform rt, H h, float cx, V v, float cy, float w, float hgt, float stripHeight)
    {
        float ax = h switch { H.Left => 0f, H.Right => 1f, H.Proportional => cx / RefW, _ => 0.5f };
        float px = h switch { H.Left => cx, H.Right => cx - RefW, H.Proportional => 0f, _ => cx - RefW / 2f };
        float ay = v switch { V.Top => 1f, V.Bottom => 0f, _ => 0.5f };
        float py = v switch { V.Top => -cy, V.Bottom => stripHeight - cy, _ => stripHeight / 2f - cy };

        rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, hgt);
        rt.anchoredPosition = new Vector2(px, py);
    }

    /// <summary>Spans the strip's width minus the insets; vertical placement as in Place.</summary>
    private static void StretchH(RectTransform rt, float insetLeft, float insetRight,
        V v, float cy, float hgt, float stripHeight)
    {
        float ay = v switch { V.Top => 1f, V.Bottom => 0f, _ => 0.5f };
        float py = v switch { V.Top => -cy, V.Bottom => stripHeight - cy, _ => stripHeight / 2f - cy };

        rt.anchorMin = new Vector2(0f, ay);
        rt.anchorMax = new Vector2(1f, ay);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(-(insetLeft + insetRight), hgt);
        rt.anchoredPosition = new Vector2((insetLeft - insetRight) / 2f, py);
    }

    private static RectTransform Make(string name, Transform parent, params Type[] comps)
    {
        // RectTransform must exist before any UI script is attached.
        var types = new List<Type>(comps.Length + 1) { typeof(RectTransform) };
        foreach (var t in comps)
        {
            if (t != typeof(RectTransform)) types.Add(t);
        }

        var go = new GameObject(name, types.ToArray());
        var rt = go.GetComponent<RectTransform>();
        if (parent != null) rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AtCentre(RectTransform rt, float dx, float dy, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(dx, -dy);
    }

    private static void Art_(RectTransform parent, string name, string sprite, float dx, float dy, float w, float h)
    {
        var rt = Make(name, parent, typeof(Image));
        AtCentre(rt, dx, dy, w, h);
        var img = rt.GetComponent<Image>();
        img.sprite = Sprite(sprite);
        img.raycastTarget = false;
    }

    private static void Sliced(Image img, string sprite, float pixelsPerPoint)
    {
        img.sprite = Sprite(sprite);
        img.type = Image.Type.Sliced;
        img.fillCenter = true;
        // Borders are authored in sprite pixels; this renders them at their design size in
        // points whatever the art's export scale.
        img.pixelsPerUnitMultiplier = pixelsPerPoint;
    }

    private static void Invisible(Image img)
    {
        img.sprite = null;
        img.color = new Color(1f, 1f, 1f, 0f); // still raycastable, just not drawn
    }

    private static TextMeshProUGUI Label(RectTransform parent, string name, string text, float size)
    {
        var rt = Make(name, parent, typeof(TextMeshProUGUI));
        var tmp = rt.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        tmp.color = Color.white;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Baloo2-ExtraBold.asset");
        if (font != null) tmp.font = font;
        return tmp;
    }

    // ======================================================================================
    // assets
    // ======================================================================================

    private static Sprite Sprite(string relative)
    {
        string path = $"{Art}/{relative}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogError($"[Ludo NX] Missing sprite: {path}");
        return sprite;
    }

    private static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
    }

    /// <summary>
    /// 9-slice borders (left, bottom, right, top) in sprite pixels. Only flat pieces are sliced,
    /// and only across their plain part, so extending them never distorts their corners.
    /// </summary>
    private static readonly Dictionary<string, Vector4> SliceBorders = new Dictionary<string, Vector4>
    {
        { "TopBar/top_navigation_bar_inner_card.png", new Vector4(27, 0, 27, 0) },
        { "BottomNav/botton_nav_background_layer.png", new Vector4(10, 0, 10, 10) },
        { "BottomNav/horizontal_line_for_bottom_nav_at_the_left.png", new Vector4(2, 0, 2, 0) },
        { "BottomNav/horizontal_line_for_bottom_nav_at_the_right.png", new Vector4(2, 0, 2, 0) },
        { "BottomNav/nav_shadow.png", new Vector4(160, 0, 160, 0) },
    };

    private static void ConfigureSprites()
    {
        if (!Directory.Exists(Art)) return;

        int changed = 0;
        foreach (string file in Directory.GetFiles(Art, "*.png", SearchOption.AllDirectories))
        {
            string asset = file.Replace('\\', '/');
            if (AssetImporter.GetAtPath(asset) is not TextureImporter importer) continue;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }
            if (importer.maxTextureSize < 2048) { importer.maxTextureSize = 2048; dirty = true; }

            string relative = asset.Substring(Art.Length + 1);
            if (SliceBorders.TryGetValue(relative, out Vector4 border) && importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                dirty = true;
            }

            if (dirty) { importer.SaveAndReimport(); changed++; }
        }

        if (changed > 0) Debug.Log($"[Ludo NX] Reimported {changed} sprite(s).");
    }

    private static void WarnIfNavShadowMissing()
    {
        if (!File.Exists($"{Art}/BottomNav/nav_shadow.png"))
        {
            Debug.LogError("[Ludo NX] BottomNav/nav_shadow.png is missing. It is generated offline, " +
                           "fitted to the reference (6.5pt lift, 4pt softness, 38% black).");
        }
    }

    private static void RetireSceneMenus()
    {
        foreach (string name in new[] { "[EDIT] Home Screen Canvas", "[PREVIEW] Home Canvas", "LudoNX_MainMenu" })
        {
            var go = FindSceneRoot(name);
            if (go == null || !go.activeSelf) continue;
            go.SetActive(false);
            Debug.Log($"[Ludo NX] Deactivated superseded canvas '{name}'.");
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    }

    private static GameObject FindSceneRoot(string name)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == name && go.scene.IsValid() && go.transform.parent == null) return go;
        }
        return null;
    }

    // ======================================================================================
    // Game view sizes and play-mode helpers, for reproducible device checks
    // ======================================================================================

    [MenuItem("Ludo NX/UI/Game View/iPhone 16 Pro (1206x2622)", priority = 30)]
    public static void ViewPro() => SetGameViewSize(1206, 2622, "iPhone 16 Pro");

    [MenuItem("Ludo NX/UI/Game View/iPhone 16 Pro Max (1320x2868)", priority = 31)]
    public static void ViewProMax() => SetGameViewSize(1320, 2868, "iPhone 16 Pro Max");

    [MenuItem("Ludo NX/UI/Game View/Android 20x9 (1080x2400)", priority = 32)]
    public static void ViewAndroid() => SetGameViewSize(1080, 2400, "Android 20:9");

    [MenuItem("Ludo NX/UI/Game View/iPad Air (1640x2360)", priority = 33)]
    public static void ViewIPad() => SetGameViewSize(1640, 2360, "iPad Air");

    /// <summary>Play-mode helper: scrolls the carousel so the next card comes forward.</summary>
    [MenuItem("Ludo NX/UI/Carousel: Focus Next Card", priority = 40)]
    public static void CarouselFocusNext()
    {
        var carousel = UnityEngine.Object.FindAnyObjectByType<GameModeCarousel>();
        if (carousel == null)
        {
            Debug.LogWarning("[Ludo NX] No GameModeCarousel in the scene - enter play mode first.");
            return;
        }

        carousel.Next();
        Debug.Log($"[Ludo NX] Carousel front card is now {carousel.CurrentMode}.");
    }

    public static void SetGameViewSize(int w, int h, string label)
    {
        try
        {
            var asm = typeof(Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var singleton = asm.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance").GetValue(null);
            object group = sizesType.GetMethod("GetGroup")
                .Invoke(sizes, new object[] { sizesType.GetProperty("currentGroupType").GetValue(sizes) });

            var groupType = group.GetType();
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = groupType.GetMethod("GetGameViewSize");

            int found = -1;
            for (int i = 0; i < count; i++)
            {
                object size = getSize.Invoke(group, new object[] { i });
                var st = size.GetType();
                if ((int)st.GetProperty("width").GetValue(size) == w &&
                    (int)st.GetProperty("height").GetValue(size) == h)
                {
                    found = i;
                    break;
                }
            }

            if (found < 0)
            {
                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var kindType = asm.GetType("UnityEditor.GameViewSizeType");
                object created = Activator.CreateInstance(sizeType,
                    Enum.Parse(kindType, "FixedResolution"), w, h, label);
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { created });
                found = count;
            }

            var gameViewType = asm.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType, false, null, false);
            gameViewType.GetMethod("SizeSelectionCallback").Invoke(window, new object[] { found, null });
            Debug.Log($"[Ludo NX] Game view set to {label} ({w}x{h}).");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Ludo NX] Could not set the game view size: {e.Message}");
        }
    }
}
