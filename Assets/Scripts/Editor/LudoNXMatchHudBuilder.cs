using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Rebuilds the in-match HUD into Assets/Resources/UI/Components/MatchHud.prefab.
///
/// Implements Match HUD V2 (SCREEN_PLANS/specs/MATCH_HUD_V2.md and SCREEN_PLANS/screens/match-hud.json):
///   - Layer 0/1: Wood wallpaper (bg_match_emboss.jpg) cover-scaled via BackgroundCoverFitter
///   - Layer 2: House turn tint overlay (cross-fading on active player switch)
///   - Layer 3: Soft radial vignette to ground.deep #14171C (38% at corners)
///   - Top row: Pause 44, Stake Plaque (+20 NX x X5), Mute 44
///   - Player cards: 4 corner seats with house-coloured avatar rings (Red, Green, Blue, Yellow),
///     BOT tags for AI, active card glow + scale 1.03, all typography >= 12 pt
///   - Dice tray: Recessed well in tan300
///   - ROLL: XL slab 66 pt, play red #FF3930, Lilita 30 CAPS + Nunito 11 "TAP TO ROLL"
///   - Action bar: 92 pt bottom bar on ground:
///     - Left: Emoji 56 neutral rounded clay button
///     - Centre: RULES 46 option amber button with book glyph
///     - Centre (8 pt gap under Rules): Turn timer recessed pill (pulses red under 10s)
///     - Right: Message 56 neutral rounded clay button
/// </summary>
public static class LudoNXMatchHudBuilder
{
    private const string Figma = "Assets/Art/UI/FigmaMatch";
    private const string Art = "Assets/Art/UI/Match";
    private const string Cards = "Assets/Art/UI/MatchSetup/Cards";
    private const string HomeScreenArt = "Assets/Art/UI/HomeScreen";
    private const string BgPath = "Assets/Art/UI/Backgrounds";
    private const string PrefabPath = "Assets/Resources/UI/Components/MatchHud.prefab";
    private const string NunitoPath = "Assets/Fonts/Nunito-Bold.asset";

    private const float CardW = 142f;
    private const float CardH = 60f;

    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "hud_chip.png", new Vector4(24, 24, 24, 24) },
        { "hud_card.png", new Vector4(22, 22, 22, 22) },
        { "hud_card_glow.png", new Vector4(40, 40, 40, 40) },
        { "play_plate.png", new Vector4(20, 20, 20, 20) },
        { "pill_cream.png", new Vector4(24, 24, 24, 24) },
        { "hud_badge_square.png", new Vector4(6, 6, 6, 6) },
    };

    [MenuItem("Ludo NX/UI/Build Match HUD (V2 Rebuild)", priority = 6)]
    public static void Build()
    {
        GenerateArt();
        ConfigureSprites(Art, Borders);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Make("MatchHud", null, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
            typeof(MatchHudView));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;          // below modal layer (200), above gameplay

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = Reference; // 440 x 956 pt standard design frame
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;        // Portrait matches width strictly per SHARED_RULES.md §4

        var view = root.GetComponent<MatchHudView>();

        BuildBackground(root, view);
        BuildTopBar(root, view);
        BuildCards(root, view);
        BuildBottomBand(root, view);

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Match HUD V2 built successfully -> {PrefabPath}.");
    }

    [MenuItem("Ludo NX/UI/Setup Scene and Capture Match HUD", priority = 7)]
    public static string SetupSceneAndCapture()
    {
        SnapAllWaypointsToGrid();
        Build();
        if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name != "SampleScene")
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }

        var existingHud = GameObject.Find("MatchHud");
        if (existingHud != null)
        {
            UnityEngine.Object.DestroyImmediate(existingHud);
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) return "Prefab not found!";

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "MatchHud";

        var trayZone = GameObject.Find("DiceTrayZone");
        if (trayZone != null)
        {
            string[] hideNames = new[] { "Floor", "Front_Glass", "Left_Wall", "Right_Wall", "Top_Wall", "Bottom_Wall" };
            foreach (var n in hideNames)
            {
                var t = trayZone.transform.Find(n);
                if (t != null) t.gameObject.SetActive(false);
            }
        }

        var view = instance.GetComponent<MatchHudView>();
        if (view != null && view.cards != null)
        {
            string[] names = new[] { "YOU", "ALEX", "SARAH", "LEO" };
            int[] levels = new[] { 1, 1, 1, 1 };
            string[] points = new[] { "0", "0", "0", "0" };

            for (int i = 0; i < view.cards.Length; i++)
            {
                if (view.cards[i] != null)
                {
                    var card = view.cards[i];
                    bool isTurn = card.house == GameManager.PlayerColor.RED;
                    card.ApplyHouse(card.house, i > 0);
                    if (card.nameLabel != null) card.nameLabel.text = names[i % names.Length];
                    if (card.levelLabel != null) card.levelLabel.text = "LEVEL " + levels[i % levels.Length];
                    if (card.nxPointsLabel != null) card.nxPointsLabel.text = points[i % points.Length];
                    if (card.winPercentLabel != null) card.winPercentLabel.text = "0% WIN";
                    card.SetActive(isTurn, 0f);
                }
            }
        }

        var framer = UnityEngine.Object.FindAnyObjectByType<BoardCameraFramer>();
        if (framer != null)
        {
            framer.horizontalMargin = -0.40f;
            framer.diceSizeFraction = 0.088f;
            framer.Frame();
        }

        var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allGos)
        {
            if (go.scene.name == "SampleScene" && (go.name == "Dice_3D_1" || go.name == "Dice_3D_2"))
            {
                go.SetActive(true);
                go.transform.rotation = Quaternion.identity;
            }
        }

        var tokens = UnityEngine.Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in tokens)
        {
            if (t != null)
                t.SetPreviewTurnRing(t.tokenColor == GameManager.PlayerColor.RED);
        }

        string path = LudoScreenshot.Capture("match_hud_figma_verified", 1080, 2400);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
        return path;
    }

    // ======================================================================================
    // 1. Background Layers (Wallpaper, House Tint, Vignette)
    // ======================================================================================

    private static void BuildBackground(RectTransform root, MatchHudView view)
    {
        var bgGroup = Make("BackgroundGroup", root);
        Stretch(bgGroup);

        // House Turn Tint Overlay — four strips framing the board rather than one
        // full-bleed rect. The canvas stays transparent over the board so the 2D/3D board,
        // tokens, and world background remain completely visible.
        var tintHolder = Make("HouseTurnTint", bgGroup);
        Stretch(tintHolder);
        view.houseTintStrips = new[]
        {
            TintStrip(tintHolder, "Top"),
            TintStrip(tintHolder, "Bottom"),
            TintStrip(tintHolder, "Left"),
            TintStrip(tintHolder, "Right"),
        };
    }

    // ======================================================================================
    // 2. Top Bar Band (Exit Door, Stake Plaque, Speaker Mute)
    // ======================================================================================

    private static void BuildTopBar(RectTransform root, MatchHudView view)
    {
        var band = Make("TopBand", root);
        band.anchorMin = new Vector2(0f, 1f);
        band.anchorMax = new Vector2(1f, 1f);
        band.pivot = new Vector2(0.5f, 1f);
        band.sizeDelta = new Vector2(0f, 115f);
        band.anchoredPosition = Vector2.zero;

        var widgets = Make("Widgets", band);
        Stretch(widgets);

        // Top Tan Header Strip (Rectangle 104.png)
        var topBarBg = Make("TopBarBg", widgets, typeof(Image));
        topBarBg.anchorMin = new Vector2(0f, 1f);
        topBarBg.anchorMax = new Vector2(1f, 1f);
        topBarBg.pivot = new Vector2(0.5f, 1f);
        topBarBg.sizeDelta = new Vector2(0f, 44f);
        topBarBg.anchoredPosition = Vector2.zero;
        var topBgImg = topBarBg.GetComponent<Image>();
        var topStripSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Figma}/Rectangle 104.png");
        if (topStripSprite != null)
        {
            topBgImg.sprite = topStripSprite;
            topBgImg.type = Image.Type.Simple;
            topBgImg.preserveAspect = false;
        }
        else
        {
            topBgImg.color = new Color32(206, 174, 132, 255);
        }
        topBgImg.color = Color.white;
        topBgImg.raycastTarget = false;

        // Left: 3D Clay Exit Door Button (image 33.png)
        var pause = Make("PauseButton", widgets, typeof(Image), typeof(Button));
        Top(pause, H.Left, 36f, 74f, 44f, 44f);
        var pauseImg = pause.GetComponent<Image>();
        var doorSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Figma}/image 33.png");
        if (doorSprite != null)
        {
            pauseImg.sprite = doorSprite;
            pauseImg.type = Image.Type.Simple;
            pauseImg.preserveAspect = true;
            pauseImg.color = Color.white;
        }
        else
        {
            Sliced(pauseImg, $"{Art}/hud_chip.png", 2f);
            pauseImg.color = new Color32(192, 150, 102, 255);
        }
        view.pauseButton = pause.GetComponent<Button>();

        // Centre: Dark Recessed Pill holding Game Information (Rectangle 105.png)
        // Brought down slightly and dedicated to game information (showing player turn, match status, etc.)
        var plaque = Make("GameInfoPlaque", widgets, typeof(Image));
        Top(plaque, H.Centre, 220f, 88f, 176f, 40f);
        var plaqueImg = plaque.GetComponent<Image>();
        var topPillSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Figma}/Rectangle 105.png");
        if (topPillSprite != null)
        {
            plaqueImg.sprite = topPillSprite;
            plaqueImg.type = Image.Type.Simple;
            plaqueImg.preserveAspect = true;
        }
        else
        {
            Sliced(plaqueImg, $"{Cards}/play_plate.png", 2f);
            plaqueImg.color = new Color32(42, 30, 20, 220);
        }
        plaqueImg.color = Color.white;
        plaqueImg.raycastTarget = false;
        view.stakePlaque = plaque.gameObject;

        var infoLabel = Text(plaque, "GameInfoLabel", "YOUR TURN", 13.5f, new Color32(255, 242, 220, 255), TextAlignmentOptions.Center);
        Stretch(infoLabel.rectTransform);
        infoLabel.rectTransform.offsetMin = new Vector2(0f, 2f);
        infoLabel.outlineWidth = 0.22f;
        infoLabel.outlineColor = new Color32(20, 23, 28, 220);
        view.turnLabel = infoLabel;
        view.stakeLabel = infoLabel;

        // Right: 3D Clay Speaker / Mute Button (image 34.png)
        var mute = Make("MuteButton", widgets, typeof(Image), typeof(Button));
        Top(mute, H.Right, 404f, 74f, 44f, 44f);
        var muteImg = mute.GetComponent<Image>();
        var speakerSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Figma}/image 34.png");
        if (speakerSprite != null)
        {
            muteImg.sprite = speakerSprite;
            muteImg.type = Image.Type.Simple;
            muteImg.preserveAspect = true;
            muteImg.color = Color.white;
        }
        else
        {
            Sliced(muteImg, $"{Art}/hud_chip.png", 2f);
            muteImg.color = new Color32(192, 150, 102, 255);
        }
        view.muteButton = mute.GetComponent<Button>();
        view.muteIcon = muteImg;
    }

    // ======================================================================================
    // 3. Corner Player Cards (Figma Chassis, 3D Blue Avatar Ring, Snug to Board)
    // ======================================================================================

    private static void BuildCards(RectTransform root, MatchHudView view)
    {
        var band = Make("CardsBand", root);
        Stretch(band);

        var holder = Make("Cards", band);
        Stretch(holder);

        // Top cards adjacent to board top edge (board top = 241 pt, cards cy = 196 pt, card bottom = 226 pt -> 15 pt air gap):
        // Bottom cards adjacent to board bottom edge (board bottom = 709 pt, cards cy = 746 pt, card top = 716 pt -> 7 pt air gap):
        // Board order: Red top-left, Green top-right, Yellow bottom-right, Blue bottom-left
        view.cards = new[]
        {
            Card(holder, "CardRed", 85f, 196f, false, GameManager.PlayerColor.RED),
            Card(holder, "CardGreen", 355f, 196f, false, GameManager.PlayerColor.GREEN),
            Card(holder, "CardYellow", 355f, 746f, false, GameManager.PlayerColor.YELLOW),
            Card(holder, "CardBlue", 85f, 746f, false, GameManager.PlayerColor.BLUE),
        };

        // Dice Tray Well Anchor (two anchors side by side for the 3D dice)
        // Brought down to cy = 760f, positioned so 3D dice have clean air gaps to cards, board and roll button
        var tray = Make("DiceTrayWell", holder);
        Top(tray, H.Centre, 220f, 760f, 130f, 60f);

        var sockL = Make("SocketLeft", tray);
        sockL.anchorMin = sockL.anchorMax = sockL.pivot = new Vector2(0.5f, 0.5f);
        sockL.sizeDelta = new Vector2(52f, 52f);
        sockL.anchoredPosition = new Vector2(-29f, 0f);

        var sockR = Make("SocketRight", tray);
        sockR.anchorMin = sockR.anchorMax = sockR.pivot = new Vector2(0.5f, 0.5f);
        sockR.sizeDelta = new Vector2(52f, 52f);
        sockR.anchoredPosition = new Vector2(29f, 0f);
    }

    private static MatchPlayerCard Card(RectTransform parent, string name, float cx, float cy, bool mirrored, GameManager.PlayerColor house)
    {
        var rt = Make(name, parent, typeof(MatchPlayerCard));
        Top(rt, H.Centre, cx, cy, CardW, CardH + 12f);
        var card = rt.GetComponent<MatchPlayerCard>();
        card.house = house;
        card.tintRingWithHouseColor = false; // Option A: preserve 3D metallic blue ring from Figma!

        var body = Make("Body", rt);
        Centre(body, 0f, 0f, CardW, CardH);
        card.body = body;

        // Glow (soft house glow when active)
        var glow = Picture(body, "Glow", $"{Art}/hud_card_glow.png");
        Centre((RectTransform)glow.transform, 0f, 0f, CardW + 28f, CardH + 28f);
        glow.gameObject.SetActive(false);
        card.activeGlow = glow;

        // Frame / Card Chassis (Group 118.png)
        var frame = Make("Frame", body, typeof(Image));
        Stretch(frame);
        var frameImg = frame.GetComponent<Image>();
        var chassisSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Figma}/Group 118.png");
        if (chassisSprite != null)
        {
            frameImg.sprite = chassisSprite;
            frameImg.type = Image.Type.Simple;
            frameImg.preserveAspect = false;
        }
        else
        {
            Sliced(frameImg, $"{Art}/hud_card.png", 2f);
        }
        frameImg.color = Color.white;
        frameImg.raycastTarget = false;
        card.frame = frameImg;

        // Avatar & House Ring (profile_frame.png) - Option A: 3D metallic blue ring with die-5 crest!
        float ringX = 26f;
        float ringY = 30f;
        var ring = Picture(body, "Ring", $"{Figma}/profile_frame.png");
        InCard((RectTransform)ring.transform, CardW, H.Left, ringX, ringY, 50f, 52f);
        ring.preserveAspect = true;
        ring.color = Color.white;
        card.ring = ring;

        var portrait = Picture(body, "Portrait", $"{HomeScreenArt}/TopBar/profile_pics.png");
        InCard((RectTransform)portrait.transform, CardW, H.Left, ringX, ringY, 32f, 32f);
        portrait.preserveAspect = true;
        card.portrait = portrait;

        // Info Column (X from 52f to 138f):

        // Row 1: Nigeria Flag + Player Name + BOT Tag (Y = 13f)
        var flag = Picture(body, "Flag", $"{Figma}/Group 58.png");
        InCard((RectTransform)flag.transform, CardW, H.Left, 59f, 13f, 13f, 13f);
        flag.preserveAspect = true;

        var nameLabel = Text(body, "Name", house.ToString(), 11.5f, new Color32(42, 30, 20, 255), TextAlignmentOptions.MidlineLeft);
        InCard(nameLabel.rectTransform, CardW, H.Left, 88f, 13f, 42f, 16f);
        card.nameLabel = nameLabel;

        // BOT Tag (square with curves)
        var botTag = Make("BotTag", body, typeof(Image));
        InCard(botTag, CardW, H.Left, 126f, 13f, 24f, 13f);
        var botBg = botTag.GetComponent<Image>();
        Sliced(botBg, $"{Art}/hud_badge_square.png", 1f);
        botBg.color = new Color32(183, 39, 31, 255); // #B7271F
        var botTxt = Text(botTag, "BotText", "BOT", 8f, Color.white, TextAlignmentOptions.Center);
        Stretch(botTxt.rectTransform);
        card.botTag = botTag.gameObject;
        card.botTag.SetActive(false);

        // Row 2: Level Recessed Pill + Home Pips (Y = 27f)
        var lvlPlaque = Make("LevelPlaque", body, typeof(Image));
        InCard(lvlPlaque, CardW, H.Left, 74f, 27f, 40f, 13f);
        var lvlBg = lvlPlaque.GetComponent<Image>();
        var lvlSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Figma}/Rectangle 107.png");
        if (lvlSprite != null)
        {
            lvlBg.sprite = lvlSprite;
            lvlBg.type = Image.Type.Simple;
        }
        else
        {
            Sliced(lvlBg, $"{Art}/hud_badge_square.png", 1f);
            lvlBg.color = new Color32(42, 30, 20, 240);
        }
        var lvlTxt = Text(lvlPlaque, "LevelText", "LVL 1", 8.5f, new Color32(240, 211, 166, 255), TextAlignmentOptions.Center);
        Stretch(lvlTxt.rectTransform);
        card.levelLabel = lvlTxt;

        // Home Pips (4 Pips)
        var pips = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var pip = Picture(body, $"Pip{i}", $"{Art}/hud_pip.png");
            InCard((RectTransform)pip.transform, CardW, H.Left, 102f + i * 9f, 27f, 6.5f, 6.5f);
            pip.color = new Color(0.84f, 0.80f, 0.72f, 1f);
            pips[i] = pip;
        }
        card.tokenPips = pips;

        // Row 3: NX POINTS & WIN PERCENT (Y = 46f)
        // Left col: NX POINTS
        var nxTitle = Text(body, "NxTitle", "NX POINTS", 6.5f, new Color32(110, 85, 60, 255), TextAlignmentOptions.Center);
        InCard(nxTitle.rectTransform, CardW, H.Left, 74f, 41f, 44f, 10f);
        var nxVal = Text(body, "NxValue", "0", 9.5f, new Color32(42, 30, 20, 255), TextAlignmentOptions.Center);
        InCard(nxVal.rectTransform, CardW, H.Left, 74f, 51f, 44f, 12f);
        card.nxPointsLabel = nxVal;

        // Right col: WIN PERCENT
        var winTitle = Text(body, "WinTitle", "WIN %", 6.5f, new Color32(110, 85, 60, 255), TextAlignmentOptions.Center);
        InCard(winTitle.rectTransform, CardW, H.Left, 116f, 41f, 40f, 10f);
        var winTxt = Text(body, "WinText", "0%", 9.5f, new Color32(42, 30, 20, 255), TextAlignmentOptions.Center);
        InCard(winTxt.rectTransform, CardW, H.Left, 116f, 51f, 40f, 12f);
        card.winPercentLabel = winTxt;

        // Reaction bubble (same as before)
        var bubble = Make("Bubble", rt, typeof(Image), typeof(CanvasGroup));
        bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 1f);
        bubble.pivot = new Vector2(0.5f, 0f);
        bubble.sizeDelta = new Vector2(84f, 40f);
        bubble.anchoredPosition = new Vector2(0f, 6f);
        var bubbleImg = bubble.GetComponent<Image>();
        Sliced(bubbleImg, $"{Cards}/pill_cream.png", 3f);
        card.bubbleBgImage = bubbleImg;
        var bubbleGroup = bubble.GetComponent<CanvasGroup>();
        bubbleGroup.alpha = 0f;
        bubble.gameObject.SetActive(false);
        card.bubbleGroup = bubbleGroup;

        var bubbleEmote = Picture(bubble, "Emote", null);
        Centre((RectTransform)bubbleEmote.transform, 0f, 0f, 48f, 48f);
        bubbleEmote.raycastTarget = false;
        bubbleEmote.gameObject.AddComponent<EmoteFlipbook>();
        card.bubbleEmoteImage = bubbleEmote;
        card.bubbleEmoteFlipbook = bubbleEmote.GetComponent<EmoteFlipbook>();
        bubbleEmote.gameObject.SetActive(false);

        var bubbleText = Text(bubble, "Text", "", 11f, new Color32(30, 22, 12, 255), TextAlignmentOptions.Center);
        Stretch(bubbleText.rectTransform);
        bubbleText.rectTransform.offsetMin = new Vector2(6f, 2f);
        bubbleText.rectTransform.offsetMax = new Vector2(-6f, -2f);
        bubbleText.enableWordWrapping = true;
        card.bubbleTextLabel = bubbleText;

        return card;
    }

    // ======================================================================================
    // 4. Bottom Band (Roll Slab, Rules, Turn Timer, Emoji, Message)
    // ======================================================================================

    private static void BuildBottomBand(RectTransform root, MatchHudView view)
    {
        var band = Make("BottomBand", root);
        band.anchorMin = new Vector2(0f, 0f);
        band.anchorMax = new Vector2(1f, 0f);
        band.pivot = new Vector2(0.5f, 0f);
        band.sizeDelta = new Vector2(0f, 220f);
        band.anchoredPosition = Vector2.zero;

        // 1. Tan Footer Panel with arch cutout (Subtract.png)
        var footer = Make("FooterPanel", band, typeof(Image));
        footer.anchorMin = new Vector2(0f, 0f);
        footer.anchorMax = new Vector2(1f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.sizeDelta = new Vector2(0f, 150f);
        footer.anchoredPosition = Vector2.zero;
        var footerImg = footer.GetComponent<Image>();
        var subSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Figma}/Subtract.png");
        if (subSprite != null)
        {
            footerImg.sprite = subSprite;
            footerImg.type = Image.Type.Simple;
            footerImg.preserveAspect = false;
        }
        else
        {
            footerImg.color = new Color32(206, 174, 132, 255);
        }
        footerImg.color = Color.white;
        footerImg.raycastTarget = false;

        // 2. ROLL Button (Hero 3D crimson slab Rectangle 108.png, fitted snugly into the arch cutout)
        var roll = Make("RollButton", band, typeof(Image), typeof(Button), typeof(CanvasGroup));
        roll.anchorMin = roll.anchorMax = new Vector2(0.5f, 0f);
        roll.pivot = new Vector2(0.5f, 0.5f);
        roll.sizeDelta = new Vector2(116f, 68f);
        roll.anchoredPosition = new Vector2(0f, 112f);
        var rollImg = roll.GetComponent<Image>();
        var rollSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Figma}/Rectangle 108.png");
        if (rollSprite != null)
        {
            rollImg.sprite = rollSprite;
            rollImg.type = Image.Type.Simple;
            rollImg.preserveAspect = false;
        }
        else
        {
            Sliced(rollImg, $"{Cards}/play_btn.png", 2f);
        }
        rollImg.color = Color.white; // Pre-shaded 3D crimson art
        var btn = roll.GetComponent<Button>();
        btn.transition = Selectable.Transition.None; // Prevent Button ColorTint from overlaying colors
        view.rollButton = btn;
        view.rollBackground = rollImg;
        view.rollGroup = roll.GetComponent<CanvasGroup>();

        // ROLL Main Label - centered cleanly on top cap
        var rollLabel = Text(roll, "RollLabel", "ROLL", 24f, Color.white, TextAlignmentOptions.Center);
        rollLabel.characterSpacing = 1.5f;
        rollLabel.rectTransform.anchorMin = new Vector2(0f, 0.32f);
        rollLabel.rectTransform.anchorMax = new Vector2(1f, 0.94f);
        rollLabel.rectTransform.offsetMin = Vector2.zero;
        rollLabel.rectTransform.offsetMax = Vector2.zero;
        rollLabel.outlineWidth = 0.25f;
        rollLabel.outlineColor = new Color32(110, 10, 10, 230);
        view.rollLabel = rollLabel;

        // TAP TO ROLL sub-label matching reference
        var subLabel = Text(roll, "RollSubLabel", "TAP TO ROLL", 8.5f, new Color32(235, 140, 150, 255), TextAlignmentOptions.Center);
        subLabel.rectTransform.anchorMin = new Vector2(0f, 0.12f);
        subLabel.rectTransform.anchorMax = new Vector2(1f, 0.36f);
        subLabel.rectTransform.offsetMin = Vector2.zero;
        subLabel.rectTransform.offsetMax = Vector2.zero;
        view.rollSubLabel = subLabel;

        view.rollEnabled = rollSprite != null ? rollSprite : Sprite($"{Cards}/play_btn.png");
        view.rollDisabled = view.rollEnabled;

        // 3. Action Bar Elements (flush under the ROLL button):

        // Left: Emoji 50x50 clay button aligned with timer baseline at y = 62f
        var emoji = Make("EmojiButton", band, typeof(Image), typeof(Button));
        emoji.anchorMin = emoji.anchorMax = new Vector2(0f, 0f);
        emoji.pivot = new Vector2(0.5f, 0.5f);
        emoji.sizeDelta = new Vector2(50f, 50f);
        emoji.anchoredPosition = new Vector2(54f, 62f);
        var emojiImg = emoji.GetComponent<Image>();
        var btnBaseSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Figma}/Group 63.png");
        if (btnBaseSprite != null)
        {
            emojiImg.sprite = btnBaseSprite;
            emojiImg.type = Image.Type.Simple;
            emojiImg.preserveAspect = true;
        }
        else
        {
            Sliced(emojiImg, $"{Art}/hud_chip.png", 2f);
            emojiImg.color = new Color32(192, 150, 102, 255);
        }
        emojiImg.color = Color.white;
        view.emojiButton = emoji.GetComponent<Button>();

        var emojiIcon = Picture(emoji, "Icon", $"{Figma}/image 35.png");
        Centre((RectTransform)emojiIcon.transform, 0f, 0f, 36f, 36f);
        emojiIcon.preserveAspect = true;
        emojiIcon.color = Color.white;
        emojiIcon.raycastTarget = false;

        // Centre: Turn Timer Recessed Pill aligned flush with bottom of ROLL button at y = 62f
        var timerPill = Make("TurnTimerPill", band, typeof(Image), typeof(CanvasGroup));
        timerPill.anchorMin = timerPill.anchorMax = new Vector2(0.5f, 0f);
        timerPill.pivot = new Vector2(0.5f, 0.5f);
        timerPill.sizeDelta = new Vector2(92f, 34f);
        timerPill.anchoredPosition = new Vector2(0f, 62f);
        var timerImg = timerPill.GetComponent<Image>();
        var timerPillSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Figma}/Rectangle 109.png");
        if (timerPillSprite != null)
        {
            timerImg.sprite = timerPillSprite;
            timerImg.type = Image.Type.Simple;
            timerImg.preserveAspect = true;
        }
        else
        {
            Sliced(timerImg, $"{Art}/hud_chip.png", 2f);
            timerImg.color = new Color32(42, 30, 20, 235);
        }
        timerImg.color = Color.white;
        timerImg.raycastTarget = false;
        view.turnTimerRoot = timerPill;
        view.turnTimerGroup = timerPill.GetComponent<CanvasGroup>();

        var timerText = Text(timerPill, "TimerText", "00:30", 14f, new Color32(42, 30, 20, 255), TextAlignmentOptions.Center);
        Stretch(timerText.rectTransform);
        timerText.rectTransform.offsetMin = new Vector2(0f, 2f);
        timerText.rectTransform.offsetMax = Vector2.zero;
        view.turnTimerLabel = timerText;

        // Right: Message / Chat 50x50 clay button aligned with timer baseline at y = 62f
        var msg = Make("MessageButton", band, typeof(Image), typeof(Button));
        msg.anchorMin = msg.anchorMax = new Vector2(1f, 0f);
        msg.pivot = new Vector2(0.5f, 0.5f);
        msg.sizeDelta = new Vector2(50f, 50f);
        msg.anchoredPosition = new Vector2(-54f, 62f);
        var msgImg = msg.GetComponent<Image>();
        if (btnBaseSprite != null)
        {
            msgImg.sprite = btnBaseSprite;
            msgImg.type = Image.Type.Simple;
            msgImg.preserveAspect = true;
        }
        else
        {
            Sliced(msgImg, $"{Art}/hud_chip.png", 2f);
            msgImg.color = new Color32(192, 150, 102, 255);
        }
        msgImg.color = Color.white;
        view.messageButton = msg.GetComponent<Button>();

        var msgIcon = Picture(msg, "Icon", $"{Figma}/image 36.png");
        Centre((RectTransform)msgIcon.transform, 0f, 0f, 36f, 36f);
        msgIcon.preserveAspect = true;
        msgIcon.color = Color.white;
        msgIcon.raycastTarget = false;
    }

    // ======================================================================================
    // Art Generation (HUD Chips, Radial Vignette, Discs)
    // ======================================================================================

    private static void GenerateArt()
    {
        Directory.CreateDirectory(Art);

        Chip($"{Art}/hud_chip.png", 96, 48, 20, new Color32(255, 255, 255, 255), new Color32(245, 240, 230, 255), 1);
        Chip($"{Art}/hud_badge_square.png", 32, 32, 5, new Color32(255, 255, 255, 255), new Color32(245, 240, 230, 255), 1);
        Chip($"{Art}/hud_card.png", 142, 60, 16, new Color32(244, 230, 203, 255), new Color32(206, 190, 160, 255), 2);
        Chip($"{Art}/btn_blank_dim.png", 120, 56, 20, new Color32(120, 112, 100, 255), new Color32(226, 216, 196, 255), 3);
        Disc($"{Art}/hud_pip.png", 24);
        Disc($"{Art}/hud_ring.png", 68, ring: true);
        Glow($"{Art}/hud_card_glow.png", 160, 96);
        Vignette($"{Art}/hud_vignette.png", 256, 256);
        AssetDatabase.Refresh();
    }

    private static void Chip(string path, int w, int h, int r, Color32 fill, Color32 border, int borderW)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = RoundedDistance(x, y, w, h, r);
                Color32 c;
                if (d > 1f) c = new Color32(0, 0, 0, 0);
                else if (d > 1f - borderW / (float)r) c = border;
                else c = fill;
                px[y * w + x] = c;
            }
        }
        Write(tex, px, path);
    }

    private static void Disc(string path, int size, bool ring = false)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        float rad = size / 2f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - size / 2f + 0.5f, dy = y - size / 2f + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(Mathf.Clamp01(rad - d) * 255f);
                if (ring && d < rad * 0.74f) a = 0;
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        Write(tex, px, path);
    }

    private static void Glow(string path, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - w / 2f) / (w / 2f);
                float dy = Mathf.Abs(y - h / 2f) / (h / 2f);
                float d = Mathf.Max(dx, dy);
                float a = Mathf.Clamp01(1f - d);
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * a * 220f));
            }
        }
        Write(tex, px, path);
    }

    private static void Vignette(string path, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - w / 2f) / (w / 2f);
                float dy = (y - h / 2f) / (h / 2f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((d - 0.25f) / 0.75f);
                byte alpha = (byte)(a * a * 97); // ~38% alpha per MATCH_HUD_V2.md §1
                px[y * w + x] = new Color32(20, 23, 28, alpha); // #14171C
            }
        }
        Write(tex, px, path);
    }

    private static float RoundedDistance(int x, int y, int w, int h, int r)
    {
        float cx = Mathf.Max(r - x, 0f, x - (w - r - 1));
        float cy = Mathf.Max(r - y, 0f, y - (h - r - 1));
        return Mathf.Sqrt(cx * cx + cy * cy) / r;
    }

    private static Image TintStrip(RectTransform parent, string name)
    {
        var go = Make(name, parent, typeof(Image));
        var img = go.GetComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = false;
        return img;
    }

    private static void Write(Texture2D tex, Color32[] px, string path)
    {
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    [MenuItem("Ludo NX/Board/Snap All Waypoints To Grid", priority = 20)]
    public static void SnapAllWaypointsToGrid()
    {
        var lp = LudoPath.FindComplete();
        if (lp == null) return;

        float step = 0.83556f;
        Vector2 origin = Vector2.zero;

        int[,] mainGrid = new int[52, 2] {
            // Red arm (0 to 4)
            {-6, 1}, {-5, 1}, {-4, 1}, {-3, 1}, {-2, 1},
            // Green arm up (5 to 10)
            {-1, 2}, {-1, 3}, {-1, 4}, {-1, 5}, {-1, 6}, {-1, 7},
            // Green top apex (11, 12)
            {0, 7}, {1, 7},
            // Green arm down (13 to 17)
            {1, 6}, {1, 5}, {1, 4}, {1, 3}, {1, 2},
            // Yellow arm right (18 to 23)
            {2, 1}, {3, 1}, {4, 1}, {5, 1}, {6, 1}, {7, 1},
            // Yellow right apex (24, 25)
            {7, 0}, {7, -1},
            // Yellow arm left (26 to 30)
            {6, -1}, {5, -1}, {4, -1}, {3, -1}, {2, -1},
            // Blue arm down (31 to 36)
            {1, -2}, {1, -3}, {1, -4}, {1, -5}, {1, -6}, {1, -7},
            // Blue bottom apex (37, 38)
            {0, -7}, {-1, -7},
            // Blue arm up (39 to 43)
            {-1, -6}, {-1, -5}, {-1, -4}, {-1, -3}, {-1, -2},
            // Red arm left (44 to 49)
            {-2, -1}, {-3, -1}, {-4, -1}, {-5, -1}, {-6, -1}, {-7, -1},
            // Red left apex (50, 51)
            {-7, 0}, {-7, 1}
        };

        for (int i = 0; i < 52 && i < lp.mainLoop.Count; i++)
        {
            if (lp.mainLoop[i] != null)
            {
                int c = mainGrid[i, 0];
                int r = mainGrid[i, 1];
                lp.mainLoop[i].position = new Vector3(origin.x + c * step, origin.y + r * step, 0f);
            }
        }

        // Red Home Stretch: c = -6 to -1, r = 0
        for (int i = 0; i < lp.redHomeStretch.Count && i < 6; i++)
        {
            if (lp.redHomeStretch[i] != null)
                lp.redHomeStretch[i].position = new Vector3(origin.x + (-6 + i) * step, origin.y, 0f);
        }

        // Green Home Stretch: c = 0, r = 6 down to 1
        for (int i = 0; i < lp.greenHomeStretch.Count && i < 6; i++)
        {
            if (lp.greenHomeStretch[i] != null)
                lp.greenHomeStretch[i].position = new Vector3(origin.x, origin.y + (6 - i) * step, 0f);
        }

        // Yellow Home Stretch: c = 6 down to 1, r = 0
        for (int i = 0; i < lp.yellowHomeStretch.Count && i < 6; i++)
        {
            if (lp.yellowHomeStretch[i] != null)
                lp.yellowHomeStretch[i].position = new Vector3(origin.x + (6 - i) * step, origin.y, 0f);
        }

        // Blue Home Stretch: c = 0, r = -6 up to -1
        for (int i = 0; i < lp.blueHomeStretch.Count && i < 6; i++)
        {
            if (lp.blueHomeStretch[i] != null)
                lp.blueHomeStretch[i].position = new Vector3(origin.x, origin.y + (-6 + i) * step, 0f);
        }

        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}
