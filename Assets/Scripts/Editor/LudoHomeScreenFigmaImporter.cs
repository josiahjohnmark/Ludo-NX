using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AAA Modular Importer & Builder for the Ludo NX Main Menu UI.
/// Replaces the old procedural green screen with the pixel-perfect Figma design.
/// Features:
/// - Clean modular top bar with avatar gold frame, pinned prestige badge, purple coin pill, blue energy pill, and 3-dash menu.
/// - Left missions widget and right tournament live-ops widget with countdown timer.
/// - 3D LUDO NX logo.
/// - Interactive GameModeCarousel (Quick Play vs CPU, Online, Local Friends Pass & Play) with swipe & tap focus.
/// - Clean demarcated bottom navigation bar with zero double-rendering and tactile active tab states.
/// </summary>
[InitializeOnLoad]
public static class LudoHomeScreenFigmaImporter
{
    private const string SpriteDir = "Assets/Art/UI/HomeScreen";
    private const string ResRoot = "Assets/Resources";
    private const string ScreenDir = ResRoot + "/UI/Screens";

    static LudoHomeScreenFigmaImporter()
    {
        // Legacy auto-run disabled in favor of LudoNXMainMenuBuilder
    }

    [MenuItem("Ludo NX/UI/Legacy Import Figma Main Menu", priority = 20)]
    public static void ImportAndAssemble()
    {
        LudoNXMainMenuBuilder.Build();
    }

    [MenuItem("Ludo NX/UI/Configure Figma Sprites Only", priority = 3)]
    public static void ConfigureSprites()
    {
        EnsureTopBarProceduralAssets();

        if (!AssetDatabase.IsValidFolder(SpriteDir))
        {
            Debug.LogError($"[LudoNX Figma] Folder not found: {SpriteDir}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir });
        int count = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                string fn = Path.GetFileNameWithoutExtension(path).ToLower();

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.spritePixelsPerUnit = 100f;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);

                TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();

                if (fn.Contains("logo") || fn.Contains("banner"))
                {
                    platform.maxTextureSize = 2048;
                }
                else if (fn.Contains("card") || fn.Contains("trophy") || fn.Contains("widget") || fn.Contains("plate"))
                {
                    platform.maxTextureSize = 1024;
                }
                else
                {
                    platform.maxTextureSize = 512;
                }

                platform.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SetPlatformTextureSettings(platform);

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                count++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[LudoNX Figma] Configured {count} modular sprites in {SpriteDir}.");
    }

    public static void BuildFigmaHomeScreenPrefab()
    {
        if (!Directory.Exists(ScreenDir))
        {
            Directory.CreateDirectory(ScreenDir);
        }

        var fonts = UIFontSet.Current;

        // Root Screen GameObject
        GameObject root = new GameObject("Screen_Home", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        HomeScreen home = root.AddComponent<HomeScreen>();

        // Safe Area Root (1080x1920 reference resolution coordinate system)
        GameObject safeArea = new GameObject("SafeAreaContainer", typeof(RectTransform));
        safeArea.transform.SetParent(root.transform, false);
        RectTransform safeRt = safeArea.GetComponent<RectTransform>();
        safeRt.anchorMin = Vector2.zero;
        safeRt.anchorMax = Vector2.one;
        safeRt.offsetMin = Vector2.zero;
        safeRt.offsetMax = Vector2.zero;

        // Demo Dark Background Backdrop
        GameObject bgObj = new GameObject("DemoBackground", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(safeArea.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color32(0x13, 0x1A, 0x22, 0xFF); // Premium dark graphite table tone
        bgImg.raycastTarget = false;

        // ══════════════════════════════════════════════════════════════════════
        // 1. TOP NAVIGATION BAR (Exact Figma Reference Frame #33:91)
        // ══════════════════════════════════════════════════════════════════════
        // Floating Beige/Cream Pill Plate (#63:196) anchored directly at top with notch padding
        GameObject topBar = new GameObject("TopBarPlate", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(safeArea.transform, false);
        RectTransform topBarRt = topBar.GetComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0.5f, 1f);
        topBarRt.anchorMax = new Vector2(0.5f, 1f);
        topBarRt.pivot = new Vector2(0.5f, 0.5f);
        topBarRt.anchoredPosition = new Vector2(0f, -127f);
        topBarRt.sizeDelta = new Vector2(1026f, 134f);

        Image topBarImg = topBar.GetComponent<Image>();
        Sprite topBarSprite = LoadSprite("top_bar_plate.png");
        if (topBarSprite != null)
        {
            topBarImg.sprite = topBarSprite;
            topBarImg.type = Image.Type.Simple;
        }
        else
        {
            topBarImg.color = new Color32(0xF4, 0xE6, 0xCB, 0xFF);
        }

        // ── Avatar & Prestige Rank Badge (Left, centered at X = -432) ──
        GameObject avatarBtnObj = new GameObject("AvatarButton", typeof(RectTransform), typeof(Button));
        avatarBtnObj.transform.SetParent(topBar.transform, false);
        RectTransform avatarBtnRt = avatarBtnObj.GetComponent<RectTransform>();
        avatarBtnRt.anchoredPosition = new Vector2(-432f, 0f);
        avatarBtnRt.sizeDelta = new Vector2(144f, 144f);
        home.avatarButton = avatarBtnObj.GetComponent<Button>();

        // Inner Avatar Picture / Face Mask
        GameObject avatarPicObj = new GameObject("AvatarPic", typeof(RectTransform), typeof(Image), typeof(Mask));
        avatarPicObj.transform.SetParent(avatarBtnObj.transform, false);
        RectTransform picRt = avatarPicObj.GetComponent<RectTransform>();
        picRt.anchoredPosition = Vector2.zero;
        picRt.sizeDelta = new Vector2(118f, 118f);
        Image picImg = avatarPicObj.GetComponent<Image>();
        Sprite defaultPortrait = LoadSprite("avatar_default_portrait.png");
        if (defaultPortrait != null) picImg.sprite = defaultPortrait;
        else picImg.color = new Color32(0x1E, 0x29, 0x3B, 0xFF);
        avatarPicObj.GetComponent<Mask>().showMaskGraphic = true;

        // Outer Metallic Blue Ring
        GameObject avatarRingObj = new GameObject("AvatarRing", typeof(RectTransform), typeof(Image));
        avatarRingObj.transform.SetParent(avatarBtnObj.transform, false);
        RectTransform ringRt = avatarRingObj.GetComponent<RectTransform>();
        ringRt.anchoredPosition = Vector2.zero;
        ringRt.sizeDelta = new Vector2(144f, 144f);
        Image ringImg = avatarRingObj.GetComponent<Image>();
        Sprite ringSprite = LoadSprite("avatar_border_ring.png");
        if (ringSprite != null) ringImg.sprite = ringSprite;
        ringImg.raycastTarget = false;

        // Rank Badge: Pinned directly onto bottom rim of avatar ring (Figma node #63:192)
        GameObject badgeObj = new GameObject("RankBadge", typeof(RectTransform), typeof(Image));
        badgeObj.transform.SetParent(avatarBtnObj.transform, false);
        RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.5f, 0f);
        badgeRt.anchorMax = new Vector2(0.5f, 0f);
        badgeRt.pivot = new Vector2(0.5f, 0.5f);
        badgeRt.anchoredPosition = new Vector2(0f, 6f);
        badgeRt.sizeDelta = new Vector2(52f, 60f);
        Image badgeImg = badgeObj.GetComponent<Image>();
        Sprite badgeSprite = LoadSprite("badge_rank_01.png");
        if (badgeSprite != null) badgeImg.sprite = badgeSprite;
        badgeImg.raycastTarget = false;
        home.playerRankBadgeImage = badgeImg;
        home.rankBadgeSprites = new Sprite[10];
        home.rankBadgeSprites[0] = badgeSprite;

        // ── NX Coins Pill (Deep purple chassis, centered at X = -166) ──
        GameObject coinsPillObj = new GameObject("CoinsPill", typeof(RectTransform), typeof(Image), typeof(Button));
        coinsPillObj.transform.SetParent(topBar.transform, false);
        RectTransform coinsPillRt = coinsPillObj.GetComponent<RectTransform>();
        coinsPillRt.anchoredPosition = new Vector2(-166f, 0f);
        coinsPillRt.sizeDelta = new Vector2(296f, 92f);
        Image coinsBg = coinsPillObj.GetComponent<Image>();
        Sprite coinsChassisSprite = LoadSprite("coin_chassis_clean.png") ?? LoadSprite("nx_coins_bar.png");
        if (coinsChassisSprite != null)
        {
            coinsBg.sprite = coinsChassisSprite;
            coinsBg.type = Image.Type.Simple;
        }
        else
        {
            coinsBg.color = new Color32(0x6D, 0x0B, 0x82, 0xFF);
        }
        home.coinsChipButton = coinsPillObj.GetComponent<Button>();

        // 3D Gold Coin Icon (Single, placed cleanly on left)
        GameObject coinIconObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        coinIconObj.transform.SetParent(coinsPillObj.transform, false);
        RectTransform coinIconRt = coinIconObj.GetComponent<RectTransform>();
        coinIconRt.anchoredPosition = new Vector2(-105f, 0f);
        coinIconRt.sizeDelta = new Vector2(64f, 64f);
        Image coinIconImg = coinIconObj.GetComponent<Image>();
        Sprite coinSprite = LoadSprite("coin_icon_gold.png");
        if (coinSprite != null) coinIconImg.sprite = coinSprite;
        coinIconImg.raycastTarget = false;

        // Coins Text with crisp stroke/outline
        GameObject coinsTxtObj = new GameObject("CoinsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        coinsTxtObj.transform.SetParent(coinsPillObj.transform, false);
        RectTransform coinsTxtRt = coinsTxtObj.GetComponent<RectTransform>();
        coinsTxtRt.anchoredPosition = new Vector2(8f, 0f);
        coinsTxtRt.sizeDelta = new Vector2(130f, 40f);
        TextMeshProUGUI coinsTxt = coinsTxtObj.GetComponent<TextMeshProUGUI>();
        coinsTxt.text = "0";
        coinsTxt.fontSize = 28f;
        coinsTxt.font = fonts != null ? fonts.numeric : null;
        coinsTxt.alignment = TextAlignmentOptions.Center;
        coinsTxt.color = Color.black;
        home.coinsChipLabel = coinsTxt;

        // Plus button removed per user request: coin is now a point, starting fresh from 0

        // ── Energy Pill (Icon and text float cleanly on wood bar) ──
        GameObject energyPillObj = new GameObject("EnergyPill", typeof(RectTransform), typeof(Image), typeof(Button));
        energyPillObj.transform.SetParent(topBar.transform, false);
        RectTransform energyPillRt = energyPillObj.GetComponent<RectTransform>();
        energyPillRt.anchoredPosition = new Vector2(165f, 0f);
        energyPillRt.sizeDelta = new Vector2(245f, 92f);
        Image energyBg = energyPillObj.GetComponent<Image>();
        energyBg.color = new Color(0f, 0f, 0f, 0f);
        home.energyChipButton = energyPillObj.GetComponent<Button>();

        // Energy Bolt Icon (Single, placed cleanly on left)
        GameObject energyIconObj = new GameObject("EnergyIcon", typeof(RectTransform), typeof(Image));
        energyIconObj.transform.SetParent(energyPillObj.transform, false);
        RectTransform energyIconRt = energyIconObj.GetComponent<RectTransform>();
        energyIconRt.anchoredPosition = new Vector2(-80f, 0f);
        energyIconRt.sizeDelta = new Vector2(38f, 52f);
        Image energyIconImg = energyIconObj.GetComponent<Image>();
        Sprite energySprite = LoadSprite("energy_icon.png");
        if (energySprite != null) energyIconImg.sprite = energySprite;
        energyIconImg.raycastTarget = false;

        // Energy Count Text (Aligned inside dark cavity)
        GameObject energyTxtObj = new GameObject("EnergyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        energyTxtObj.transform.SetParent(energyPillObj.transform, false);
        RectTransform energyTxtRt = energyTxtObj.GetComponent<RectTransform>();
        energyTxtRt.anchoredPosition = new Vector2(-12f, 0f);
        energyTxtRt.sizeDelta = new Vector2(85f, 36f);
        TextMeshProUGUI energyTxt = energyTxtObj.GetComponent<TextMeshProUGUI>();
        energyTxt.text = "50";
        energyTxt.fontSize = 26f;
        energyTxt.font = fonts != null ? fonts.numeric : null;
        energyTxt.alignment = TextAlignmentOptions.Center;
        energyTxt.color = Color.black;
        home.energyChipLabel = energyTxt;

        // Energy Timer Text (Gold/Amber, aligned on right blue tab)
        GameObject energyTimerObj = new GameObject("EnergyTimer", typeof(RectTransform), typeof(TextMeshProUGUI));
        energyTimerObj.transform.SetParent(energyPillObj.transform, false);
        RectTransform energyTimerRt = energyTimerObj.GetComponent<RectTransform>();
        energyTimerRt.anchoredPosition = new Vector2(75f, 0f);
        energyTimerRt.sizeDelta = new Vector2(70f, 26f);
        TextMeshProUGUI energyTimer = energyTimerObj.GetComponent<TextMeshProUGUI>();
        energyTimer.text = "00:24";
        energyTimer.fontSize = 18f;
        energyTimer.font = fonts != null ? fonts.numeric : null;
        energyTimer.alignment = TextAlignmentOptions.Center;
        energyTimer.color = new Color32(0xFC, 0xD3, 0x4D, 0xFF);
        ApplyTextStroke(energyTimer, Color.black, 0.35f);
        home.energyTimerLabel = energyTimer;

        // ── 3-Dash Hamburger Menu Button (Right, centered at X = +422) ──
        GameObject menuBtnObj = new GameObject("MenuButton", typeof(RectTransform), typeof(Button));
        menuBtnObj.transform.SetParent(topBar.transform, false);
        RectTransform menuBtnRt = menuBtnObj.GetComponent<RectTransform>();
        menuBtnRt.anchoredPosition = new Vector2(422f, 0f);
        menuBtnRt.sizeDelta = new Vector2(70f, 70f);
        home.settingsButton = menuBtnObj.GetComponent<Button>();

        GameObject menuIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        menuIconObj.transform.SetParent(menuBtnObj.transform, false);
        RectTransform menuIconRt = menuIconObj.GetComponent<RectTransform>();
        menuIconRt.anchoredPosition = Vector2.zero;
        menuIconRt.sizeDelta = new Vector2(54f, 40f);
        Image menuIconImg = menuIconObj.GetComponent<Image>();
        Sprite menuSprite = LoadSprite("icon_hamburger.png");
        if (menuSprite != null) menuIconImg.sprite = menuSprite;
        menuIconImg.color = Color.white;
        menuIconImg.preserveAspect = true;
        // Red notification dot removed per user request

        // ══════════════════════════════════════════════════════════════════════
        // 2. LIVE-OPS EVENT RAIL (Missions Left, Tournament Right - Generous Breathing Room)
        // ══════════════════════════════════════════════════════════════════════
        float eventY = 560f;

        // Left: Missions Widget (Opens ScreenId.Missions)
        GameObject missionObj = new GameObject("MissionWidget", typeof(RectTransform), typeof(Image), typeof(Button));
        missionObj.transform.SetParent(safeArea.transform, false);
        RectTransform missRt = missionObj.GetComponent<RectTransform>();
        missRt.anchoredPosition = new Vector2(-380f, eventY);
        missRt.sizeDelta = new Vector2(180f, 280f);
        Image missImg = missionObj.GetComponent<Image>();
        Sprite missSprite = LoadSprite("mission_widget.png") ?? LoadSprite("mission_banner.png");
        if (missSprite != null)
        {
            missImg.sprite = missSprite;
            missImg.preserveAspect = true;
        }
        home.missionsButton = missionObj.GetComponent<Button>();

        // Right: Tournament Widget (NX Cup Card with Crown & Timer)
        GameObject tourObj = new GameObject("TournamentWidget", typeof(RectTransform), typeof(Image), typeof(Button));
        tourObj.transform.SetParent(safeArea.transform, false);
        RectTransform tourRt = tourObj.GetComponent<RectTransform>();
        tourRt.anchoredPosition = new Vector2(380f, eventY);
        tourRt.sizeDelta = new Vector2(190f, 290f);
        Image tourImg = tourObj.GetComponent<Image>();
        Sprite tourSprite = LoadSprite("tournament_widget.png") ?? LoadSprite("tournament_banner.png");
        if (tourSprite != null)
        {
            tourImg.sprite = tourSprite;
            tourImg.preserveAspect = true;
        }
        home.tournamentsButton = tourObj.GetComponent<Button>();

        // ══════════════════════════════════════════════════════════════════════
        // 3. 3D BRAND LOGO (Center)
        // ══════════════════════════════════════════════════════════════════════
        GameObject logoObj = new GameObject("LudoNXLogo", typeof(RectTransform), typeof(Image));
        logoObj.transform.SetParent(safeArea.transform, false);
        RectTransform logoRt = logoObj.GetComponent<RectTransform>();
        logoRt.anchoredPosition = new Vector2(0f, 440f);
        logoRt.sizeDelta = new Vector2(580f, 330f);
        Image logoImg = logoObj.GetComponent<Image>();
        Sprite logoSprite = LoadSprite("ludo_nx_logo.png");
        if (logoSprite != null)
        {
            logoImg.sprite = logoSprite;
            logoImg.preserveAspect = true;
        }
        logoImg.raycastTarget = false;

        // ══════════════════════════════════════════════════════════════════════
        // 4. INTERACTIVE GAME MODE CAROUSEL (Center Stage)
        // ══════════════════════════════════════════════════════════════════════
        GameObject carouselObj = new GameObject("GameModeCarousel", typeof(RectTransform), typeof(GameModeCarousel));
        carouselObj.transform.SetParent(safeArea.transform, false);
        RectTransform carouselRt = carouselObj.GetComponent<RectTransform>();
        carouselRt.anchoredPosition = new Vector2(0f, -100f);
        carouselRt.sizeDelta = new Vector2(1040f, 650f);

        GameModeCarousel carousel = carouselObj.GetComponent<GameModeCarousel>();
        carousel.spacing = 97f;
        carousel.frontScale = 1.0f;
        carousel.backScale = 0.79f;
        carousel.cards = new GameModeCarousel.ModeCard[3];

        // 1. ONLINE MATCH CARD (Left by default)
        GameObject onlineCardObj = CreateCarouselCard(carouselObj.transform, "Card_Online", "card_online.png", new Vector2(440f, 620f));
        RectTransform onlineRt = onlineCardObj.GetComponent<RectTransform>();
        onlineRt.anchoredPosition = new Vector2(-340f, -20f);
        onlineRt.localScale = Vector3.one * 0.78f;
        carousel.cards[0] = new GameModeCarousel.ModeCard
        {
            mode = GameModeCarousel.Mode.Online,
            rectTransform = onlineRt,
            canvasGroup = onlineCardObj.GetComponent<CanvasGroup>(),
            button = onlineCardObj.GetComponent<Button>()
        };

        // 2. QUICK PLAY CARD (Center Hero by default - Play vs CPU)
        GameObject qpCardObj = CreateCarouselCard(carouselObj.transform, "Card_QuickPlay", "card_quick_play.png", new Vector2(440f, 620f));
        RectTransform qpRt = qpCardObj.GetComponent<RectTransform>();
        qpRt.anchoredPosition = new Vector2(0f, 0f);
        qpRt.localScale = Vector3.one * 1.0f;
        carousel.cards[1] = new GameModeCarousel.ModeCard
        {
            mode = GameModeCarousel.Mode.QuickPlay,
            rectTransform = qpRt,
            canvasGroup = qpCardObj.GetComponent<CanvasGroup>(),
            button = qpCardObj.GetComponent<Button>()
        };

        // 3. LOCAL PLAY CARD (Right by default - Pass & Play Friends)
        GameObject localCardObj = CreateCarouselCard(carouselObj.transform, "Card_LocalPlay", "card_local_play.png", new Vector2(440f, 620f));
        RectTransform localRt = localCardObj.GetComponent<RectTransform>();
        localRt.anchoredPosition = new Vector2(340f, -20f);
        localRt.localScale = Vector3.one * 0.78f;
        carousel.cards[2] = new GameModeCarousel.ModeCard
        {
            mode = GameModeCarousel.Mode.LocalPlay,
            rectTransform = localRt,
            canvasGroup = localCardObj.GetComponent<CanvasGroup>(),
            button = localCardObj.GetComponent<Button>()
        };

        // Put Center Hero (Quick Play) on top in hierarchy
        qpCardObj.transform.SetAsLastSibling();

        home.modeCarousel = carousel;
        home.playButton = qpCardObj.GetComponent<Button>();
        home.quickModeButton = qpCardObj.GetComponent<Button>();
        home.classicModeButton = onlineCardObj.GetComponent<Button>();

        // ══════════════════════════════════════════════════════════════════════
        // ══════════════════════════════════════════════════════════════════════
        // 5. TACTILE BOTTOM NAVIGATION BAR (Exact Figma Reference Frame #67:207)
        // ══════════════════════════════════════════════════════════════════════
        GameObject navBarObj = new GameObject("BottomNavBar", typeof(RectTransform), typeof(Image));
        navBarObj.transform.SetParent(safeArea.transform, false);
        RectTransform navRt = navBarObj.GetComponent<RectTransform>();
        navRt.anchorMin = new Vector2(0f, 0f);
        navRt.anchorMax = new Vector2(1f, 0f);
        navRt.pivot = new Vector2(0.5f, 0f);
        navRt.anchoredPosition = Vector2.zero;
        navRt.sizeDelta = new Vector2(0f, 312f);

        Image navPlateImg = navBarObj.GetComponent<Image>();
        Sprite navPlateSprite = LoadSprite("bottom_nav_plate.png");
        if (navPlateSprite != null)
        {
            navPlateImg.sprite = navPlateSprite;
            navPlateImg.type = Image.Type.Simple;
        }
        else
        {
            navPlateImg.color = new Color32(0xC0, 0x96, 0x66, 0xFF);
        }

        // Demarcation Divider Lines Overlaid on the Plate (Figma node #77:227, 4 dividers + top highlight)
        // Scaled from 402x116 frame (S = 1080 / 402 ≈ 2.6866):
        // Lines start 4px below plate top and end exactly beside the text baseline!
        GameObject linesObj = new GameObject("DemarcationLines", typeof(RectTransform), typeof(Image));
        linesObj.transform.SetParent(navBarObj.transform, false);
        RectTransform linesRt = linesObj.GetComponent<RectTransform>();
        linesRt.anchorMin = new Vector2(0.5f, 1f);
        linesRt.anchorMax = new Vector2(0.5f, 1f);
        linesRt.pivot = new Vector2(0.5f, 1f);
        linesRt.anchoredPosition = new Vector2(0f, -4f);
        linesRt.sizeDelta = new Vector2(1042f, 178f);
        Image linesImg = linesObj.GetComponent<Image>();
        Sprite linesSprite = LoadSprite("bottom_nav_lines.png");
        if (linesSprite != null)
        {
            linesImg.sprite = linesSprite;
            linesImg.type = Image.Type.Simple;
            linesImg.preserveAspect = false;
            linesImg.color = Color.white;
        }
        else
        {
            linesImg.color = new Color(0, 0, 0, 0);
        }
        linesImg.raycastTarget = false;

        // Custom Lilita One Bold Material Preset for Bottom Nav Labels (Exact Figma #67:207 styling)
        TMP_FontAsset lilitaFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/LilitaOne-Regular.asset");
        if (lilitaFont == null && fonts != null) lilitaFont = fonts.display;

        Material navMat = null;
        if (lilitaFont != null)
        {
            string matFolder = "Assets/Art/UI/HomeScreen";
            string matPath = $"{matFolder}/Text_BottomNav.mat";
            navMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (navMat == null)
            {
                navMat = new Material(lilitaFont.material);
                AssetDatabase.CreateAsset(navMat, matPath);
            }
            navMat.shader = lilitaFont.material.shader;
            navMat.SetTexture(ShaderUtilities.ID_MainTex, lilitaFont.material.GetTexture(ShaderUtilities.ID_MainTex));

            // Bright rich cream matching Figma #FFF4E2 / #F4E6CB
            Color32 figmaCream = new Color32(0xFF, 0xF4, 0xE2, 0xFF);
            navMat.SetColor(ShaderUtilities.ID_FaceColor, figmaCream);
            navMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.18f); // Extra bold as requested by user

            // Crisp dark outline matching Figma rgba(0, 0, 0, 0.5)
            navMat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.15f, 0.08f, 0.02f, 0.90f));
            navMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);

            // Subtle depth underlay
            navMat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.45f));
            navMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.15f);
            navMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.25f);
            navMat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);
            navMat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.08f);

            navMat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            navMat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            EditorUtility.SetDirty(navMat);
        }

        // 5 Tabs (ALBUM, FRIENDS, HOME [Pops up], SHOP, LEVELS)
        HomeNavBar navBar = navBarObj.AddComponent<HomeNavBar>();
        navBar.items = new List<HomeNavBar.NavItem>();
        home.navBar = navBar;

        string[] tabNames = { "Album", "Friends", "Home", "Shop", "Levels" };
        string[] tabSprites = { "nav_album.png", "nav_friends.png", "nav_home.png", "nav_shop.png", "nav_levels.png" };
        HomeNavBar.Tab[] tabEnums = {
            HomeNavBar.Tab.Collection,
            HomeNavBar.Tab.Friends,
            HomeNavBar.Tab.Home,
            HomeNavBar.Tab.Shop,
            HomeNavBar.Tab.Missions
        };
        ScreenId[] tabScreens = {
            ScreenId.PawnCollection,
            ScreenId.Friends,
            ScreenId.Home,
            ScreenId.Shop,
            ScreenId.Missions
        };

        // Mathematically mapped from Figma node #67:207 (402w -> 1080w)
        float[] tabXPositions = { -432f, -227f, 0f, 218f, 428f };
        float[] tabYOffsets = { -14f, -14f, +22f, -14f, -14f };
        Vector2[] iconSizes = {
            new Vector2(122f, 110f), // Album
            new Vector2(120f, 110f), // Friends
            new Vector2(192f, 168f), // Home (elevated)
            new Vector2(118f, 108f), // Shop
            new Vector2(132f, 120f)  // Levels
        };
        float[] labelYPositions = { -114f, -114f, -172f, -114f, -122f };

        for (int i = 0; i < 5; i++)
        {
            bool isHomeTab = (i == 2);
            GameObject tabObj = new GameObject("Tab_" + tabNames[i], typeof(RectTransform), typeof(Button));
            tabObj.transform.SetParent(navBarObj.transform, false);
            RectTransform tabRt = tabObj.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0.5f, 1f);
            tabRt.anchorMax = new Vector2(0.5f, 1f);
            tabRt.pivot = new Vector2(0.5f, 1f);
            tabRt.anchoredPosition = new Vector2(tabXPositions[i], tabYOffsets[i]);
            tabRt.sizeDelta = isHomeTab ? new Vector2(210f, 240f) : new Vector2(160f, 170f);

            // Crisp Tab Icon (Home roof peaks above plate, others neatly framed)
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(tabObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = iconSizes[i];
            Image iconImg = iconObj.GetComponent<Image>();
            Sprite tabSp = LoadSprite(tabSprites[i]);
            if (tabSp != null) iconImg.sprite = tabSp;
            iconImg.preserveAspect = true;
            iconImg.color = isHomeTab ? Color.white : new Color(1f, 1f, 1f, 0.95f);

            // Tab Label (Lilita One Bold with outline and drop shadow - exact Figma baseline)
            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(tabObj.transform, false);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 1f);
            labelRt.anchorMax = new Vector2(0.5f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = new Vector2(0f, labelYPositions[i]);
            labelRt.sizeDelta = new Vector2(170f, 44f);
            TextMeshProUGUI labelTxt = labelObj.GetComponent<TextMeshProUGUI>();
            labelTxt.text = tabNames[i].ToUpper();
            labelTxt.fontSize = isHomeTab ? 35f : 33f;
            labelTxt.font = lilitaFont ?? (fonts != null ? fonts.display : null);
            if (navMat != null) labelTxt.fontSharedMaterial = navMat;
            labelTxt.fontStyle = FontStyles.Bold;
            labelTxt.alignment = TextAlignmentOptions.Center;
            labelTxt.color = isHomeTab ? Color.white : new Color32(0xFF, 0xF4, 0xE2, 0xFF);
            ApplyTextStroke(labelTxt, new Color(0.15f, 0.08f, 0.02f, 0.90f), 0.22f);

            HomeNavBar.NavItem item = new HomeNavBar.NavItem
            {
                tab = tabEnums[i],
                targetScreen = tabScreens[i],
                button = tabObj.GetComponent<Button>(),
                iconImage = iconImg,
                label = labelTxt,
                defaultPos = tabRt.anchoredPosition
            };
            navBar.items.Add(item);
        }

        // Save Prefab
        string prefabPath = ScreenDir + "/Home.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LudoNX Figma] Successfully built AAA Main Menu Screen at: {prefabPath}");
        LudoScreenshot.CaptureHomeScreen();
    }

    private static GameObject CreateCarouselCard(Transform parent, string name, string spriteName, Vector2 size)
    {
        GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Button));
        card.transform.SetParent(parent, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        Image img = card.GetComponent<Image>();
        Sprite sp = LoadSprite(spriteName);
        if (sp != null)
        {
            img.sprite = sp;
            img.preserveAspect = true;
        }

        return card;
    }

    private static void ApplyTextStroke(TextMeshProUGUI tmp, Color strokeColor, float thickness)
    {
        if (tmp == null) return;
        tmp.outlineColor = strokeColor;
        tmp.outlineWidth = thickness;
    }

    private static Sprite LoadSprite(string filename)
    {
        string path = SpriteDir + "/" + filename;
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return sp;
    }

    private static void EnsureTopBarProceduralAssets()
    {
        if (!Directory.Exists(SpriteDir)) Directory.CreateDirectory(SpriteDir);

        // 1. main_top_nav_frame.png (Warm wood/tan top navigation bar with rounded bottom corners)
        string mainTopPath = SpriteDir + "/main_top_nav_frame.png";
        if (!File.Exists(mainTopPath))
        {
            int w = 540, h = 137;
            float r = 27f; // 54px at 1080p
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32 fill = new Color32(0xC1, 0x97, 0x67, 0xFF);
            Color32 shadow = new Color32(0x8C, 0x62, 0x36, 0xFF);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float a = 1f;
                    if (y < r)
                    {
                        if (x < r)
                        {
                            float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
                            a = Mathf.Clamp01(r - d + 0.5f);
                        }
                        else if (x > w - 1 - r)
                        {
                            float d = Mathf.Sqrt((x - (w - 1 - r)) * (x - (w - 1 - r)) + (y - r) * (y - r));
                            a = Mathf.Clamp01(r - d + 0.5f);
                        }
                    }
                    Color c = (y < 4) ? Color.Lerp(shadow, fill, y / 4f) : (Color)fill;
                    c.a *= a;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(mainTopPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // 2. coin_chassis_clean.png (Clean purple chassis with dark inset cavity, NO baked text/icons)
        string coinPath = SpriteDir + "/coin_chassis_clean.png";
        if (!File.Exists(coinPath))
        {
            int w = 296, h = 92;
            float r = 46f;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32 purple = new Color32(0x6D, 0x0B, 0x82, 0xFF);
            Color32 darkCavity = new Color32(0x1D, 0x01, 0x25, 0xFF);
            Color32 border = new Color32(0x42, 0x26, 0x17, 0xFF);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x, r, w - 1 - r);
                    float cy = r;
                    float dOuter = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float aOuter = Mathf.Clamp01(r - dOuter + 0.5f);

                    if (aOuter <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color col = purple;
                    if (dOuter >= r - 2f) col = border;

                    float cavR = 38f;
                    float cavMinX = 8f + cavR;
                    float cavMaxX = 226f - cavR;
                    if (x >= 8 && x <= 226 && y >= 8 && y <= 84)
                    {
                        float ccx = Mathf.Clamp(x, cavMinX, cavMaxX);
                        float ccy = 46f;
                        float dCav = Mathf.Sqrt((x - ccx) * (x - ccx) + (y - ccy) * (y - ccy));
                        float aCav = Mathf.Clamp01(cavR - dCav + 0.5f);
                        if (aCav > 0f)
                        {
                            col = Color.Lerp(col, darkCavity, aCav);
                        }
                    }

                    col.a *= aOuter;
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            File.WriteAllBytes(coinPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // 3. energy_chassis_clean.png (Clean blue chassis with dark inset cavity, NO baked text/icons)
        string energyPath = SpriteDir + "/energy_chassis_clean.png";
        if (!File.Exists(energyPath))
        {
            int w = 245, h = 92;
            float r = 46f;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32 blue = new Color32(0x00, 0x52, 0xD7, 0xFF);
            Color32 darkCavity = new Color32(0x01, 0x0B, 0x32, 0xFF);
            Color32 border = new Color32(0x1B, 0x32, 0x4A, 0xFF);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x, r, w - 1 - r);
                    float cy = r;
                    float dOuter = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float aOuter = Mathf.Clamp01(r - dOuter + 0.5f);

                    if (aOuter <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color col = blue;
                    if (dOuter >= r - 1.5f) col = border;

                    float cavR = 38f;
                    float cavMinX = 8f + cavR;
                    float cavMaxX = 175f - cavR;
                    if (x >= 8 && x <= 175 && y >= 8 && y <= 84)
                    {
                        float ccx = Mathf.Clamp(x, cavMinX, cavMaxX);
                        float ccy = 46f;
                        float dCav = Mathf.Sqrt((x - ccx) * (x - ccx) + (y - ccy) * (y - ccy));
                        float aCav = Mathf.Clamp01(cavR - dCav + 0.5f);
                        if (aCav > 0f)
                        {
                            col = Color.Lerp(col, darkCavity, aCav);
                        }
                    }

                    col.a *= aOuter;
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            File.WriteAllBytes(energyPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // 4. avatar_default_portrait.png (Player face portrait for circular avatar)
        string avatarPicPath = SpriteDir + "/avatar_default_portrait.png";
        if (!File.Exists(avatarPicPath))
        {
            int s = 128;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color32 bgGrad = new Color32(0x1E, 0x29, 0x3B, 0xFF);
            Color32 skin = new Color32(0x7A, 0x48, 0x2D, 0xFF);
            Color32 hair = new Color32(0x11, 0x11, 0x11, 0xFF);
            Color32 shirt = new Color32(0x23, 0x2B, 0x38, 0xFF);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - 64;
                    float dy = y - 64;
                    float distFromCenter = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distFromCenter > 64f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color c = bgGrad;
                    // Head
                    float headDist = Mathf.Sqrt((x - 64) * (x - 64) + (y - 72) * (y - 72));
                    if (headDist < 26f)
                    {
                        c = skin;
                        if (y > 78) c = hair;
                    }
                    // Shoulders
                    if (y < 46)
                    {
                        float shDist = Mathf.Sqrt((x - 64) * (x - 64) * 0.45f + (y - 18) * (y - 18));
                        if (shDist < 46f) c = shirt;
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(avatarPicPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
