using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rebuilds the in-match HUD, following the supplied gameplay reference.
///
/// Layout is three fixed bands. The board fills the full screen width and sits dead centre, so
/// with a 1080×1920 canvas it is 1080 tall and leaves exactly 420px above and below:
///
///   top    +540..+960   EXIT · timer · SOUND, then the RED and GREEN cards
///   board  −540..+540   untouched
///   bottom −960..−540   the BLUE and YELLOW cards, then the dice and ROLL
///
/// Each player card sits at the corner of its own house — red top-left, green top-right, blue
/// bottom-left, yellow bottom-right — so a card is always adjacent to the tokens it describes.
///
/// Game logic stays in UIManager and PlayerCardUI; this only replaces what they point at.
/// </summary>
public static class LudoNXMatchHudAuthor
{
    private const float BoardHalf = 540f;
    private const float CardW = 512f;
    private const float CardH = 168f;
    private const float CardX = 264f;    // card centres, left/right
    private const float CardTopY = 630f;
    private const float CardBottomY = -630f;

    [MenuItem("Ludo NX/UI/10. Rebuild Match HUD", priority = 10)]
    public static void BuildMatchHud()
    {
        LudoNXScreenAuthor.CreateSupportAssets();
        var T = UITheme.Current;
        var icons = UIIconSet.Current;

        var uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (uiManager == null) { Debug.LogError("[LudoNX HUD] No UIManager in the scene."); return; }

        GameObject canvasGo = FindHudCanvas();
        if (canvasGo == null) { Debug.LogError("[LudoNX HUD] Could not find the match HUD canvas."); return; }

        // ── Canvas ───────────────────────────────────────────────────────────────────────
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;   // above the board, below ScreenManager's modal layer (200)
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                                        | AdditionalCanvasShaderChannels.TexCoord2
                                        | AdditionalCanvasShaderChannels.TexCoord3;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        if (canvasGo.GetComponent<GraphicRaycaster>() == null) canvasGo.AddComponent<GraphicRaycaster>();

        var oldLayout = Object.FindFirstObjectByType<MatchHudLayout>(FindObjectsInactive.Include);
        if (oldLayout != null) oldLayout.enabled = false;

        // These have to be REMOVED, not disabled: Unity runs Awake even on a disabled component,
        // so BackgroundAmbience would still build its full-screen wash and vignette.
        var ambience = canvasGo.GetComponent<BackgroundAmbience>();
        if (ambience != null) Object.DestroyImmediate(ambience);
        var winUi = canvasGo.GetComponent<WinUIController>();
        if (winUi != null) Object.DestroyImmediate(winUi);

        for (int i = canvasGo.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(canvasGo.transform.GetChild(i).gameObject);

        Transform p = canvasGo.transform;

        // ── Top row: EXIT · timer · SOUND ────────────────────────────────────────────────
        var quit = SquareButton(p, "Quit_Button", 108f, new Vector2(-432f, 862f), T.panel, T);
        SpriteIcon(quit.content, "Icon", 50f, Vector2.zero,
                   UIIconSet.IconAny("close", "cancel"), T.InkOn(T.panel));
        Caption(p, "Quit_Label", new Vector2(-432f, 782f), "EXIT", T);

        var sound = SquareButton(p, "Sound_Button", 108f, new Vector2(432f, 862f), T.panel, T);
        Color soundInk = T.InkOn(T.panel);
        var soundOn = SpriteIcon(sound.content, "Icon_On", 50f, Vector2.zero,
                                 UIIconSet.Icon("volume_up"), soundInk);
        var soundOff = SpriteIcon(sound.content, "Icon_Off", 50f, Vector2.zero,
                                  UIIconSet.Icon("volume_off"), soundInk);
        soundOff.gameObject.SetActive(false);   // UIManager swaps these
        Caption(p, "Sound_Label", new Vector2(432f, 782f), "SOUND", T);

        var timerPill = UIFactory.Clay("Timer_Pill", p, new Vector2(400f, 100f), new Vector2(0f, 862f),
                                       T.panel, ClayGraphic.Shape.Pill);
        timerPill.raycastTarget = false;
        SpriteIcon(timerPill.transform, "Timer_Icon", 46f, new Vector2(-128f, 0f),
                   UIIconSet.Icon("timer"), T.InkOn(T.panel));
        var timerText = UIFactory.Label("Timer_Text", timerPill.transform, new Vector2(250f, 74f),
                                        new Vector2(28f, 0f), "1:00", UIFontSet.Current.numeric,
                                        48f, T.InkOn(T.panel));

        // The turn banner is small and sits just under the timer.
        var turnText = UIFactory.Label("Turn_Text", p, new Vector2(700f, 48f), new Vector2(0f, 782f),
                                       "YOUR TURN", UIFontSet.Current.body, 30f, T.inkMuted);
        turnText.fontStyle = FontStyles.Bold;
        turnText.characterSpacing = 6f;

        // ── Player cards, each at its own house corner ───────────────────────────────────
        var red = BuildCard(p, "Card_Red", new Vector2(-CardX, CardTopY), GameManager.PlayerColor.RED, T, icons);
        var green = BuildCard(p, "Card_Green", new Vector2(CardX, CardTopY), GameManager.PlayerColor.GREEN, T, icons);
        var blue = BuildCard(p, "Card_Blue", new Vector2(-CardX, CardBottomY), GameManager.PlayerColor.BLUE, T, icons);
        var yellow = BuildCard(p, "Card_Yellow", new Vector2(CardX, CardBottomY), GameManager.PlayerColor.YELLOW, T, icons);

        // ── Bottom: emoji / chat, then the roll button ───────────────────────────────────
        var emoji = SquareButton(p, "Emoji_Button", 88f, new Vector2(-440f, -888f), T.panel, T);
        SpriteIcon(emoji.content, "Icon", 46f, Vector2.zero,
                   UIIconSet.Icon("sentiment_satisfied"), T.InkOn(T.panel));
        var chat = SquareButton(p, "Chat_Button", 88f, new Vector2(440f, -888f), T.panel, T);
        SpriteIcon(chat.content, "Icon", 46f, Vector2.zero,
                   UIIconSet.IconAny("send", "support_agent"), T.InkOn(T.panel));

        var roll = UIFactory.Button("Roll_Button", p, new Vector2(560f, 118f), new Vector2(0f, -888f),
                                    T.gold, "ROLL", 46f, ClayGraphic.Shape.Pill, glow: 0.12f, hero: true);
        roll.Clay.glowSpread = 36f;
        var rollLabel = roll.content.GetComponentInChildren<TextMeshProUGUI>(true);
        if (rollLabel != null) rollLabel.rectTransform.anchoredPosition = new Vector2(28f, 0f);
        SpriteIcon(roll.content, "Roll_Icon", 52f, new Vector2(-118f, 0f),
                   UIIconSet.IconAny("casino", "play_circle_filled", "play_arrow"), T.InkOn(T.gold));

        // ── Panels UIManager expects to exist (its own overlays are superseded) ──────────
        var victory = Hidden(p, "VictoryPanel");
        var stats = Hidden(p, "StatsPanel");
        var quitConfirm = Hidden(p, "QuitConfirmPanel");

        // ── Wire UIManager ───────────────────────────────────────────────────────────────
        uiManager.turnStatusText = turnText;
        uiManager.turnBannerHeader = null;
        uiManager.redPlayerCard = red;
        uiManager.greenPlayerCard = green;
        uiManager.yellowPlayerCard = yellow;
        uiManager.bluePlayerCard = blue;
        uiManager.quitButton = quit;
        uiManager.soundToggleButton = sound;
        uiManager.soundOnIcon = soundOn;
        uiManager.soundOffIcon = soundOff;
        uiManager.timerText = timerText;
        uiManager.rollButton = roll;
        uiManager.rollButtonImage = null;
        uiManager.rollButtonText = rollLabel;
        uiManager.extraRollButtons = new Button[0];
        uiManager.victoryPanel = victory;
        uiManager.statsPanel = stats;
        uiManager.quitConfirmPanel = quitConfirm;
        uiManager.chatButton = chat;
        uiManager.emojiButton = emoji;
        uiManager.chatPanel = null;
        uiManager.emojiPanel = null;

        uiManager.redColor = T.playerRed;
        uiManager.greenColor = T.playerGreen;
        uiManager.yellowColor = T.playerYellow;
        uiManager.blueColor = T.playerBlue;
        uiManager.timerSafeColor = T.InkOn(T.panel);
        uiManager.timerWarnColor = T.goldDeep;
        uiManager.timerDangerColor = T.primary;
        uiManager.rollButtonActiveColor = T.gold;
        uiManager.rollButtonDisabledColor = T.neutral;

        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(canvasGo);

        FrameBoard();
        BuildBackground();
        BuildDiceFrames(T);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(canvasGo.scene);
        Debug.Log("[LudoNX HUD] Match HUD rebuilt: cards at house corners, per-die frames, background fitted.");
    }

    // ── Player card ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A card in the player's own colour: avatar, name, win rate, points and level.
    ///
    /// Win rate carries a "%" and points carry a star, so neither needs a written label — four
    /// of these are read mid-turn, and a glyph parses faster than a word, survives translation,
    /// and leaves the numbers large enough to actually read.
    /// </summary>
    private static PlayerCardUI BuildCard(Transform parent, string name, Vector2 pos,
                                          GameManager.PlayerColor colour, UITheme T, UIIconSet icons)
    {
        Color tint = T.ForPlayer(colour);
        Color ink = T.InkOn(tint);

        var card = UIFactory.Clay(name, parent, new Vector2(CardW, CardH), pos, tint,
                                  ClayGraphic.Shape.Panel, T.radiusMd);
        card.raycastTarget = false;
        card.shadowAlpha = 0.28f;

        var group = card.gameObject.AddComponent<CanvasGroup>();
        var ui = card.gameObject.AddComponent<PlayerCardUI>();
        ui.playerColor = colour;
        ui.cardCanvasGroup = group;
        ui.activeScale = 1.02f;
        ui.activeGlowColor = Color.white;
        ui.inactiveColor = new Color(1f, 1f, 1f, 0.55f);

        // Waiting cards fade rather than recolour, so the page shows through them — at 0.55 over
        // a coloured wash the blue card read as purple and the green as olive, which is exactly
        // the identity the card exists to carry. Kept high enough for the house colour to survive;
        // the scale bump, bright rim and running timer bar are what mark whose turn it is.
        ui.inactiveAlpha = 0.72f;

        // Active-turn marker: a light rim PlayerCardUI brightens on this player's turn.
        ui.glowBorder = SolidImage(card.transform, "GlowBorder", new Vector2(CardW - 24f, 8f),
                                   new Vector2(0f, CardH * 0.5f - 10f), null, Color.white);

        // ── Avatar ───────────────────────────────────────────────────────────────────────
        const float ax = -CardW * 0.5f + 84f;
        var slot = UIFactory.Clay("Avatar_Slot", card.transform, new Vector2(120f, 120f),
                                  new Vector2(ax, 0f), Color.white, ClayGraphic.Shape.Panel, 28f);
        slot.raycastTarget = false;
        slot.shadowAlpha = 0.20f;

        var circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/art_assets/generated/ui_circle.png");
        ui.avatarImage = SolidImage(slot.transform, "Avatar", new Vector2(102f, 102f), Vector2.zero,
                                    circle, Color.Lerp(Color.white, tint, 0.18f));
        ui.avatarBorder = null;

        int pawnIndex = colour == GameManager.PlayerColor.RED ? 0
                      : colour == GameManager.PlayerColor.GREEN ? 1
                      : colour == GameManager.PlayerColor.YELLOW ? 2 : 3;
        if (icons != null && icons.pawns != null && icons.pawns.Length > pawnIndex && icons.pawns[pawnIndex] != null)
            SpriteIcon(slot.transform, "Pawn", 74f, new Vector2(0f, -2f), icons.pawns[pawnIndex], Color.white);

        // AvatarSlot is where MatchHudAvatar drops the chosen photo at runtime, masked to
        // the frame so a square picture fits a rounded slot without being re-cut.
        slot.gameObject.AddComponent<MatchHudAvatar>().playerColour = colour;

        // ── Text block ───────────────────────────────────────────────────────────────────
        const float textLeft = ax + 82f;

        var nameText = UIFactory.Label("Name_Text", card.transform, new Vector2(230f, 46f),
                                       new Vector2(textLeft + 115f, 50f), colour.ToString(),
                                       UIFontSet.Current.body, 32f, ink, TextAlignmentOptions.Left);
        nameText.fontStyle = FontStyles.Bold;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        ui.playerNameText = nameText;

        // Win rate — the "%" identifies it.
        var winText = UIFactory.Label("WinRate_Text", card.transform, new Vector2(100f, 42f),
                                      new Vector2(textLeft + 50f, 2f), "0%",
                                      UIFontSet.Current.numeric, 28f, ink, TextAlignmentOptions.Left);
        ui.winRateText = winText;

        // Points — the star identifies it.
        if (icons != null && icons.star != null)
            SpriteIcon(card.transform, "Points_Icon", 28f, new Vector2(textLeft + 128f, 2f),
                       icons.star, T.gold);
        var pointsText = UIFactory.Label("Points_Text", card.transform, new Vector2(110f, 42f),
                                         new Vector2(textLeft + 198f, 2f), "0",
                                         UIFontSet.Current.numeric, 28f, ink, TextAlignmentOptions.Left);
        ui.pointsText = pointsText;

        // Level chip.
        var lvl = UIFactory.Clay("Level_Chip", card.transform, new Vector2(112f, 38f),
                                 new Vector2(textLeft + 56f, -46f),
                                 Color.Lerp(tint, Color.black, 0.22f), ClayGraphic.Shape.Pill);
        lvl.raycastTarget = false;
        lvl.shadowAlpha = 0f;
        UIFactory.Label("Level_Text", lvl.transform, new Vector2(112f, 36f), Vector2.zero, "Lv. 1",
                        UIFontSet.Current.numeric, 22f, ink);

        // ── Home tokens: four pips, top-right ───────────────────────────────────────────
        var dots = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            float dx = CardW * 0.5f - 112f + i * 26f;
            var well = UIFactory.Clay("HomeDot_" + i, card.transform, new Vector2(18f, 18f),
                                      new Vector2(dx, 54f), Color.Lerp(tint, Color.black, 0.28f),
                                      ClayGraphic.Shape.Circle);
            well.raycastTarget = false;
            well.shadowAlpha = 0f;
            dots[i] = SpriteIcon(well.transform, "Fill", 18f, Vector2.zero, circle, Color.white);
            dots[i].color = new Color(1f, 1f, 1f, 0f);
        }
        ui.homeTokenDots = dots;

        // ── Turn timer: a hairline along the bottom, only while this player is up ───────
        var track = UIFactory.Clay("Timer_Track", card.transform, new Vector2(CardW - 48f, 8f),
                                   new Vector2(0f, -CardH * 0.5f + 13f),
                                   Color.Lerp(tint, Color.black, 0.30f), ClayGraphic.Shape.Pill);
        track.raycastTarget = false;
        track.shadowAlpha = 0f;

        var fill = SpriteIcon(track.transform, "Timer_Fill", CardW - 48f, Vector2.zero, null, Color.white);
        fill.rectTransform.sizeDelta = new Vector2(CardW - 48f, 8f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.enabled = false;
        ui.timerBarFill = fill;

        return ui;
    }

    // ── Small pieces ──────────────────────────────────────────────────────────────────────

    private static ClayButton SquareButton(Transform parent, string name, float size, Vector2 pos,
                                           Color colour, UITheme T)
    {
        var btn = UIFactory.Button(name, parent, new Vector2(size, size), pos, colour, null,
                                   shape: ClayGraphic.Shape.Panel);
        btn.Clay.cornerRadius = T.radiusSm + 8f;
        btn.Clay.shadowAlpha = T.shadowAlpha;
        return btn;
    }

    private static void Caption(Transform parent, string name, Vector2 pos, string text, UITheme T)
    {
        var t = UIFactory.Label(name, parent, new Vector2(240f, 36f), pos, text,
                                UIFontSet.Current.body, 24f, T.inkMuted);
        t.characterSpacing = 6f;
        t.fontStyle = FontStyles.Bold;
    }

    private static Image SolidImage(Transform parent, string name, Vector2 size, Vector2 pos,
                                    Sprite sprite, Color tint)
    {
        var rt = UIFactory.Rect(name, parent, size, pos);
        var img = rt.gameObject.AddComponent<Image>();
        if (sprite != null) img.sprite = sprite;
        img.color = tint;
        img.material = UIFactory.SpriteMaterial;
        img.raycastTarget = false;
        return img;
    }

    private static Image SpriteIcon(Transform parent, string name, float size, Vector2 pos,
                                    Sprite sprite, Color tint)
    {
        var rt = UIFactory.Rect(name, parent, new Vector2(size, size), pos);
        var img = rt.gameObject.AddComponent<Image>();
        if (sprite != null) img.sprite = sprite;
        img.color = tint;
        img.material = UIFactory.SpriteMaterial;
        img.raycastTarget = false;
        img.preserveAspect = sprite != null;
        return img;
    }

    private static void BuildCross(Transform parent, Color c)
    {
        for (int i = 0; i < 2; i++)
        {
            var s = UIFactory.Clay("Bar" + i, parent, new Vector2(46f, 10f), Vector2.zero, c,
                                   ClayGraphic.Shape.Pill);
            s.transform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
            s.raycastTarget = false;
            s.shadowAlpha = 0f; s.innerHighlight = 0f; s.innerShadow = 0f;
        }
    }

    /// <summary>
    /// A volume meter — a rounded body plus rising bars. Drawn rather than an icon glyph, and a
    /// meter rather than a speaker outline because overlapping rounded rectangles at this size
    /// merge into a blob while separated bars stay legible.
    /// </summary>
    private static void BuildSpeaker(Transform parent, Color c, out Image onBar, out Image offStrike)
    {
        var body = UIFactory.Clay("Spk_Body", parent, new Vector2(13f, 20f), new Vector2(-18f, 0f), c,
                                  ClayGraphic.Shape.Pill);
        body.raycastTarget = false; body.shadowAlpha = 0f;
        body.innerHighlight = 0f; body.innerShadow = 0f;

        for (int i = 0; i < 2; i++)
        {
            var bar = UIFactory.Clay("Spk_Bar" + i, parent, new Vector2(9f, 18f + i * 12f),
                                     new Vector2(-4f + i * 14f, 0f), c, ClayGraphic.Shape.Pill);
            bar.raycastTarget = false; bar.shadowAlpha = 0f;
            bar.innerHighlight = 0f; bar.innerShadow = 0f;
        }

        onBar = SolidImage(parent, "Icon_On", new Vector2(9f, 44f), new Vector2(24f, 0f), null, c);
        offStrike = SolidImage(parent, "Icon_Off", new Vector2(56f, 8f), new Vector2(0f, 0f), null, c);
        offStrike.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
        offStrike.gameObject.SetActive(false);
    }

    private static GameObject Hidden(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.SetActive(false);
        return go;
    }

    // ── World pieces ──────────────────────────────────────────────────────────────────────

    /// <summary>Board across the full screen width, dead centre.</summary>
    private static void FrameBoard()
    {
        var framer = Object.FindFirstObjectByType<BoardCameraFramer>(FindObjectsInactive.Include);
        if (framer == null) return;

        framer.referenceHeight = 1920f;
        framer.topReservedPx = 420f;
        framer.bottomReservedPx = 420f;
        framer.horizontalMargin = 0f;   // width is the binding constraint, so the board is edge-to-edge
        framer.verticalMargin = 0f;     // equal bands put the free band exactly in the middle
        framer.diceRowScreenY = 0.905f; // ≈ y −778, clear of the bottom cards and of ROLL
        framer.diceSizeFraction = 0.029f;
        framer.diceSpacingFraction = 0.026f;
        framer.Frame();
        EditorUtility.SetDirty(framer);

        var cam = framer.GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UITheme.Current.groundTop;
            EditorUtility.SetDirty(cam);
        }
    }

    /// <summary>
    /// Removes the photographic backdrop and installs the turn-colour wash instead.
    ///
    /// A fixed image says nothing about the state of the game; the page taking on the current
    /// player's colour tells you whose turn it is peripherally, with no extra label.
    /// </summary>
    private static void BuildBackground()
    {
        var old = GameObject.Find("Match_Background");
        if (old != null) Object.DestroyImmediate(old);

        var cam = Camera.main;
        if (cam == null) return;

        var amb = cam.GetComponent<MatchAmbience>();
        if (amb == null) amb = cam.gameObject.AddComponent<MatchAmbience>();
        amb.targetCamera = cam;
        amb.lightenToward = 0.70f;   // soft pastel of the house colour, not a white wash
        amb.blendSeconds = 0.7f;
        EditorUtility.SetDirty(cam);
    }

    /// <summary>
    /// Gives each die its own frame instead of one shared tray.
    ///
    /// A frame follows its die's position but keeps its own rotation, so it stays a flat plate
    /// while the die tumbles on top of it.
    /// </summary>
    private static void BuildDiceFrames(UITheme T)
    {
        var tray = GameObject.Find("Dice_Tray");
        if (tray != null) tray.SetActive(false);   // the shared shelf is replaced by per-die frames

        var panel = LoadSprite("Assets/art_assets/generated/ui_panel.png");
        string[] dieNames = { "Dice_3D_1", "Dice_3D_2" };

        for (int i = 0; i < dieNames.Length; i++)
        {
            var die = GameObject.Find(dieNames[i]);
            if (die == null) continue;

            string frameName = "Die_Frame_" + (i + 1);
            var frame = GameObject.Find(frameName);
            if (frame == null) frame = new GameObject(frameName);

            var sr = frame.GetComponent<SpriteRenderer>();
            if (sr == null) sr = frame.AddComponent<SpriteRenderer>();
            if (panel != null) sr.sprite = panel;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.color = UITheme.Lin(T.panel);
            sr.sortingOrder = -20;      // under the die, over the background

            var follow = frame.GetComponent<DieFramePlate>();
            if (follow == null) follow = frame.AddComponent<DieFramePlate>();
            follow.die = die.transform;
            // 1.45 made the two plates exactly meet at the dice's resting spacing, so they read
            // as the single shared tray this was meant to replace. Sized to leave a clear gap.
            follow.scaleMultiplier = 1.22f;
            follow.Apply();

            EditorUtility.SetDirty(frame);
        }
    }

    private static Sprite LoadSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (!importer.sRGBTexture) { importer.sRGBTexture = true; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject FindHudCanvas()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.GetComponent<ScreenManager>() != null) continue;
            if (c.name == "Canvas_Menu") continue;
            if (c.GetComponent<BackgroundAmbience>() != null || c.name == "Canvas") return c.gameObject;
        }
        return null;
    }
}
