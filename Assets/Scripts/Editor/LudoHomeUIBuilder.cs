using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Builds the whole front end — home, play-mode hub, player count, AI difficulty and the result
/// screen — and wires it to <see cref="LudoMenuFlow"/>.
///
/// Generated rather than hand-placed for the same reason the rest of this project's UI is: the art
/// is still a stand-in. Every image here is a named node fed from a single palette and a single
/// sprite lookup, so replacing a placeholder with real art is a file swap or one line, not a
/// re-layout. Re-running the menu item rebuilds from scratch, so an edit that goes wrong costs one
/// click to undo.
///
/// Only the offline path is live. Locked tiles are drawn, greyed and wired to a stub so the shape
/// of the finished game is visible without pretending the modes exist.
///
/// Run order: 0 → 1 → this. It needs the TMP font assets and the generated sprite kit.
/// </summary>
public static class LudoHomeUIBuilder
{
    private const string GeneratedDir = "Assets/art_assets/generated";
    private const string ArtDir = "Assets/art_assets";
    private const string IconDir = "Assets/Art/UI/Icons";
    private const string MenuRootName = "Canvas_Menu";

    // ── Palette ────────────────────────────────────────────────────────────────────────────
    // Deep navy ground so the four Ludo colours stay the only saturated things on screen — the
    // board's own palette is loud, and a loud menu in front of it leaves nothing to look at.
    // Dark navy ground, teal action, gold accent — the direction from the supplied mockups. The
    // in-match HUD keeps its light gradient; menus are the dark half of the game.
    private static readonly Color Ground = new Color32(0x08, 0x11, 0x1C, 0xFF);
    private static readonly Color Panel = new Color32(0x0E, 0x1A, 0x2A, 0xFF);
    private static readonly Color PanelLift = new Color32(0x14, 0x24, 0x38, 0xFF);
    private static readonly Color Accent = new Color32(0x22, 0xD3, 0xEE, 0xFF);
    private static readonly Color AccentDeep = new Color32(0x0E, 0x7A, 0x90, 0xFF);
    private static readonly Color Gold = new Color32(0xE8, 0xB5, 0x4B, 0xFF);
    private static readonly Color Ink = Color.white;
    private static readonly Color InkMuted = new Color32(0x7C, 0x93, 0xAD, 0xFF);
    private static readonly Color Locked = new Color32(0x1B, 0x27, 0x36, 0xFF);
    private static readonly Color Hairline = new Color32(0x1E, 0x33, 0x4A, 0xFF);

    // Same drop shadow the in-match HUD uses, from the supplied spec: 0/4, black at 40%.
    private static readonly Vector2 ShadowOffset = new Vector2(0f, -4f);
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.40f);

    private static readonly Color LudoRed = new Color32(0xE8, 0x44, 0x3C, 0xFF);
    private static readonly Color LudoGreen = new Color32(0x35, 0xB4, 0x4A, 0xFF);
    private static readonly Color LudoYellow = new Color32(0xF2, 0xC2, 0x30, 0xFF);
    private static readonly Color LudoBlue = new Color32(0x3A, 0x7B, 0xD5, 0xFF);

    private static TMP_FontAsset _display;
    private static TMP_FontAsset _body;
    private static Sprite _panel;
    private static Sprite _circle;

    [MenuItem("Ludo NX/10. Build Home & Play Menu")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[LudoHomeUIBuilder] Stop play mode first — a generated UI would be discarded on exit.");
            return;
        }

        if (!LoadDependencies()) return;

        // Rebuild is destroy-and-recreate. Incremental patching of a generated hierarchy is where
        // these builders usually rot: half-updated nodes that nobody can tell apart from new ones.
        GameObject existing = GameObject.Find(MenuRootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        EnsureSceneServices(out LudoMenuFlow flow);

        GameObject canvasGo = BuildCanvas();
        Undo.RegisterCreatedObjectUndo(canvasGo, "Build Ludo NX menu");
        Transform root = canvasGo.transform;

        GameObject home = BuildHome(root, flow);
        GameObject modes = BuildModes(root, flow);
        GameObject players = BuildPlayerCount(root, flow);
        GameObject difficulty = BuildDifficulty(root, flow);
        GameObject result = BuildResult(root, flow);

        // ── wiring ──
        flow.menuCanvas = canvasGo;
        flow.gameHudCanvas = FindGameHudCanvas();
        flow.screenHome = home;
        flow.screenModes = modes;
        flow.screenPlayerCount = players;
        flow.screenDifficulty = difficulty;
        flow.screenResult = result;

        EditorUtility.SetDirty(flow);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = canvasGo;

        Debug.Log("[LudoHomeUIBuilder] Built Canvas_Menu with 5 screens and wired LudoMenuFlow. " +
                  "Offline Quick Match is live: PLAY → Quick Match → 2 or 4 players → difficulty. " +
                  "Every other tile is drawn but locked. Save the scene to keep it.");
    }

    // ── dependencies ───────────────────────────────────────────────────────────────────────

    private static bool LoadDependencies()
    {
        _display = FindFont("Baloo2-ExtraBold");
        _body = FindFont("Nunito-Bold");

        if (_display == null || _body == null)
        {
            Debug.LogError("[LudoHomeUIBuilder] Baloo2-ExtraBold or Nunito-Bold TMP asset is missing. " +
                           "Run 'Ludo NX/0. Rebuild TMP Font Assets' first.");
            return false;
        }

        _panel = LoadSprite($"{GeneratedDir}/ui_panel.png");
        _circle = LoadSprite($"{GeneratedDir}/ui_circle.png");

        // The kit is cheap to make and the builder is useless without it, so generate rather than
        // send the user away for one click.
        if (_panel == null || _circle == null)
        {
            LudoUIKitGenerator.GenerateKit(false);
            AssetDatabase.Refresh();
            _panel = LoadSprite($"{GeneratedDir}/ui_panel.png");
            _circle = LoadSprite($"{GeneratedDir}/ui_circle.png");
        }

        if (_panel == null || _circle == null)
        {
            Debug.LogError("[LudoHomeUIBuilder] The generated UI sprite kit is missing. " +
                           "Run 'Ludo NX/1. Generate UI Sprite Kit', then run this again.");
            return false;
        }

        return true;
    }

    /// <summary>Makes sure the two runtime singletons exist, without disturbing them if they do.</summary>
    private static void EnsureSceneServices(out LudoMenuFlow flow)
    {
        flow = Object.FindAnyObjectByType<LudoMenuFlow>();
        if (flow == null)
        {
            GameObject go = new GameObject("LudoMenuFlow");
            flow = go.AddComponent<LudoMenuFlow>();
            Undo.RegisterCreatedObjectUndo(go, "Create LudoMenuFlow");
        }

        if (Object.FindAnyObjectByType<LudoNXSession>() == null)
        {
            GameObject go = new GameObject("LudoNXSession");
            go.AddComponent<LudoNXSession>();
            Undo.RegisterCreatedObjectUndo(go, "Create LudoNXSession");
        }
    }

    /// <summary>The in-match HUD canvas, so the flow can hide it while a menu screen is up.</summary>
    private static GameObject FindGameHudCanvas()
    {
        UIManager ui = Object.FindAnyObjectByType<UIManager>();
        if (ui != null)
        {
            Canvas c = ui.GetComponentInParent<Canvas>();
            if (c != null) return c.rootCanvas.gameObject;
        }

        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.gameObject.name != MenuRootName) return c.rootCanvas.gameObject;
        }
        return null;
    }

    // ── canvas ─────────────────────────────────────────────────────────────────────────────

    private static GameObject BuildCanvas()
    {
        GameObject go = new GameObject(MenuRootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;   // above the in-match HUD, which sits at 5

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return go;
    }

    // ── screens ────────────────────────────────────────────────────────────────────────────

    private static GameObject BuildHome(Transform root, LudoMenuFlow flow)
    {
        GameObject screen = Screen_("Screen_Home", root);
        Transform t = screen.transform;

        Fill(t, "Background", Ground);
        CornerAccents(t);

        // ── top bar: who you are and what you have ──
        RectTransform bar = Node("TopBar", t, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(1000, 150));
        NineSlice(bar, "Plate", Panel);

        // Disc plus the player's own pawn, not the player-card art — that asset is a stats card and
        // brings its own WIN%/POINTS rows with it, which read as broken at avatar size.
        RectTransform avatar = Node("Avatar", bar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24, 0), new Vector2(104, 104));
        AddImage(avatar, _circle, PanelLift);

        RectTransform avatarPawn = Node("Pawn", avatar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(74, 74));
        Sprite pawnArt = LoadSprite($"{ArtDir}/Pawn_Master_Red.png");
        Image pawnImg = AddImage(avatarPawn, pawnArt != null ? pawnArt : _circle, pawnArt != null ? Color.white : LudoRed);
        pawnImg.preserveAspect = true;

        flow.homePlayerNameText = Label("Name", bar, "Player", _body, 38, Ink, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(148, 20), new Vector2(320, 46));

        // Level sits on its own chip: it is the number the player is trying to move, so it should
        // not read as part of their name.
        RectTransform lvlChip = Node("LevelChip", bar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(148, -26), new Vector2(150, 40));
        NineSlice(lvlChip, "Plate", AccentDeep);
        Label("Label", lvlChip, "LVL", _body, 20, Ink, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16, 0), new Vector2(60, 30));
        flow.homeLevelText = Label("Value", lvlChip, "1", _display, 26, Ink, TextAlignmentOptions.Right,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16, 0), new Vector2(70, 34));

        RectTransform track = Node("LevelTrack", bar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(310, -26), new Vector2(180, 12));
        NineSlice(track, "Groove", Locked);
        RectTransform fillRect = Node("Fill", track, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0, 0), new Vector2(180, 12));
        fillRect.anchorMin = new Vector2(0, 0); fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
        Image fill = AddImage(fillRect, _panel, Accent);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0f;
        flow.homeLevelProgressFill = fill;

        flow.homeCoinsText = Currency(bar, "Coins", "500", $"{IconDir}/icon_coin.png", Gold, new Vector2(-24, 28));
        flow.homeNXText = Currency(bar, "NX", "0", $"{IconDir}/icon_trophy.png", Accent, new Vector2(-24, -28));

        // ── hero ──
        // Sits high, with the primary action low. The empty band between them is deliberate: it is
        // where daily reward, missions and event chips land, and leaving it now means adding them
        // later without moving anything the player has already learned the position of.
        Label("Wordmark", t, "LUDO NX", _display, 132, Ink, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 420), new Vector2(900, 170));
        Label("Tagline", t, "EVERY CAPTURE COUNTS", _body, 30, InkMuted, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 320), new Vector2(900, 44));

        Pawns(t, new Vector2(0, 160), 150);

        // ── play ──
        // Low on the screen on purpose — the primary action belongs in the thumb arc on a phone,
        // not at the optical centre.
        RectTransform play = Node("PlayButton", t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -430), new Vector2(700, 172));
        NineSlice(play, "Plate", Accent);
        Label("Label", play, "PLAY", _display, 72, new Color32(0x06, 0x2A, 0x35, 0xFF), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 100));
        DropShadow(play.gameObject);
        Clickable(play, flow, "OnPlayPressed");

        BuildBottomNav(t, flow);
        return screen;
    }

    /// <summary>
    /// The six-tab bar from the plan. Only Home and Play do anything in this build; the rest are
    /// drawn so the shape of the finished game is legible, and dimmed so it is obvious they are not
    /// ready yet.
    /// </summary>
    private static void BuildBottomNav(Transform parent, LudoMenuFlow flow)
    {
        RectTransform nav = Node("BottomNav", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(1010, 156));
        NineSlice(nav, "Plate", Panel);

        string[] labels = { "HOME", "PLAY", "WORLD", "ATLAS", "CLAN", "SHOP" };
        bool[] live = { true, true, false, false, false, false };

        float slot = 1010f / labels.Length;
        for (int i = 0; i < labels.Length; i++)
        {
            float x = -1010f / 2f + slot * (i + 0.5f);
            RectTransform tab = Node($"Tab_{labels[i]}", nav, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(slot - 8, 130));

            bool isHome = i == 0;
            if (isHome) NineSlice(tab, "Plate", PanelLift);

            RectTransform dot = Node("Dot", tab, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(44, 44));
            AddImage(dot, _circle, live[i] ? (isHome ? Accent : InkMuted) : Locked);

            Label("Label", tab, labels[i], _body, 22, live[i] ? (isHome ? Ink : InkMuted) : Locked,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -26), new Vector2(slot, 30));

            Clickable(tab, flow, i == 1 ? "OnPlayPressed" : (isHome ? "OnHomePressed" : "OnLockedFeaturePressed"));
        }
    }

    private static GameObject BuildModes(Transform root, LudoMenuFlow flow)
    {
        GameObject screen = Screen_("Screen_Modes", root);
        Transform t = screen.transform;

        Fill(t, "Background", Ground);
        CornerAccents(t);
        Header(t, "PLAY", flow);

        // Live first, locked below, so the one thing that works is the first thing under the thumb.
        ModeCard(t, flow, "Card_QuickOffline", "QUICK MATCH", "Play offline against AI", 420, LudoGreen, true, "OnOfflineQuickMatchPressed");
        ModeCard(t, flow, "Card_QuickOnline", "ONLINE MATCH", "Coming soon", 200, Locked, false, "OnLockedFeaturePressed");
        ModeCard(t, flow, "Card_Ranked", "RANKED", "Coming soon", -20, Locked, false, "OnLockedFeaturePressed");
        ModeCard(t, flow, "Card_Private", "PRIVATE ROOM", "Coming soon", -240, Locked, false, "OnLockedFeaturePressed");
        ModeCard(t, flow, "Card_Clan", "CLAN BATTLE", "Coming soon", -460, Locked, false, "OnLockedFeaturePressed");

        return screen;
    }

    private static void ModeCard(Transform parent, LudoMenuFlow flow, string name, string title, string subtitle,
                                 float y, Color accent, bool live, string handler)
    {
        RectTransform card = Node(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(920, 190));
        NineSlice(card, "Plate", live ? PanelLift : Panel);

        // A colour rail rather than a tinted card: the card stays readable and the rail carries the
        // state, which also means a locked card is obviously the same object, just off.
        RectTransform rail = Node("Rail", card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(12, 140));
        AddImage(rail, _panel, accent);

        Label("Title", card, title, _display, 52, live ? Ink : InkMuted, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52, 26), new Vector2(700, 64));
        Label("Subtitle", card, subtitle, _body, 28, live ? InkMuted : Locked, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52, -30), new Vector2(700, 40));

        if (!live)
        {
            RectTransform lockIcon = Node("Lock", card, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40, 0), new Vector2(56, 56));
            Sprite shield = LoadSprite($"{IconDir}/icon_shield.png");
            AddImage(lockIcon, shield != null ? shield : _circle, Locked);
        }

        DropShadow(card.gameObject);
        Clickable(card, flow, handler);
    }

    private static GameObject BuildPlayerCount(Transform root, LudoMenuFlow flow)
    {
        GameObject screen = Screen_("Screen_PlayerCount", root);
        Transform t = screen.transform;

        Fill(t, "Background", Ground);
        CornerAccents(t);
        Header(t, "PLAYERS", flow);

        // The two-player choice is really a choice of colour, so it is split into the two
        // diagonals rather than hidden behind an extra "pick your side" screen. The subtitle names
        // the house the player will actually control, because that is the decision being made —
        // the other two houses are removed from the board entirely.
        RectTransform two = CountCard(t, "Card_2P_RedYellow", "2", "PLAYERS", "You play Red", 320,
            new[] { LudoRed, LudoYellow });
        flow.twoPlayerButton = Clickable(two, flow, "OnTwoPlayerSidePressed", 0);

        RectTransform twoAlt = CountCard(t, "Card_2P_GreenBlue", "2", "PLAYERS", "You play Green", 20,
            new[] { LudoGreen, LudoBlue });
        flow.twoPlayerAltButton = Clickable(twoAlt, flow, "OnTwoPlayerSidePressed", 1);

        RectTransform four = CountCard(t, "Card_4P", "4", "PLAYERS", "Classic Ludo. All four houses.", -280,
            new[] { LudoRed, LudoGreen, LudoYellow, LudoBlue });
        flow.fourPlayerButton = Clickable(four, flow, "OnPlayerCountPressed", 4);

        return screen;
    }

    private static RectTransform CountCard(Transform parent, string name, string bigNumber, string caption,
                                           string subtitle, float y, Color[] colors)
    {
        RectTransform card = Node(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(920, 260));
        NineSlice(card, "Plate", PanelLift);

        // Two columns: the number and its caption stack on the left, the description and colour
        // pips on the right. Both are centre-pivoted within their own column so the caption cannot
        // grow sideways into the pips.
        Label("Number", card, bigNumber, _display, 140, Accent, TextAlignmentOptions.Center,
            new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170, 28), new Vector2(220, 150));
        Label("Caption", card, caption, _body, 28, InkMuted, TextAlignmentOptions.Center,
            new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170, -66), new Vector2(220, 40));

        Label("Subtitle", card, subtitle, _body, 32, Ink, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(330, 44), new Vector2(540, 46));

        // Colour pips show exactly which houses are in play — the real question behind "2 or 4",
        // and faster to read than any sentence.
        for (int i = 0; i < colors.Length; i++)
        {
            RectTransform pip = Node($"Pip_{i}", card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(334 + i * 76, -46), new Vector2(60, 60));
            AddImage(pip, _circle, colors[i]);
        }

        DropShadow(card.gameObject);
        return card;
    }

    private static GameObject BuildDifficulty(Transform root, LudoMenuFlow flow)
    {
        GameObject screen = Screen_("Screen_Difficulty", root);
        Transform t = screen.transform;

        Fill(t, "Background", Ground);
        CornerAccents(t);
        Header(t, "OPPONENT", flow);

        flow.difficultySummaryText = Label("Summary", t, "Offline  ·  4 players", _body, 30, InkMuted, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -260), new Vector2(900, 44));

        flow.easyButton = DifficultyCard(t, flow, "Card_Easy", "EASY", "Learning the board", 0, LudoGreen, 300);
        flow.normalButton = DifficultyCard(t, flow, "Card_Normal", "NORMAL", "Plays a real game", 1, Gold, 60);
        flow.hardButton = DifficultyCard(t, flow, "Card_Hard", "HARD", "Takes every chance", 2, LudoRed, -180);

        return screen;
    }

    private static Button DifficultyCard(Transform parent, LudoMenuFlow flow, string name, string title,
                                         string subtitle, int value, Color accent, float y)
    {
        RectTransform card = Node(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(920, 200));
        NineSlice(card, "Plate", PanelLift);

        RectTransform rail = Node("Rail", card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(12, 150));
        AddImage(rail, _panel, accent);

        Label("Title", card, title, _display, 58, Ink, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52, 28), new Vector2(600, 70));
        Label("Subtitle", card, subtitle, _body, 28, InkMuted, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52, -32), new Vector2(600, 40));

        // Three filled pips at Hard, one at Easy — strength read at a glance, no legend needed.
        for (int i = 0; i < 3; i++)
        {
            RectTransform pip = Node($"Pip_{i}", card, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-40 - (2 - i) * 52, 0), new Vector2(38, 38));
            AddImage(pip, _circle, i <= value ? accent : Locked);
        }

        DropShadow(card.gameObject);
        return Clickable(card, flow, "OnDifficultyPressed", value);
    }

    private static GameObject BuildResult(Transform root, LudoMenuFlow flow)
    {
        GameObject screen = Screen_("Screen_Result", root);
        Transform t = screen.transform;

        Fill(t, "Background", Ground);
        CornerAccents(t);

        // Sized to its contents. The match-stats block (captures, tokens home, MVP) lands under the
        // reward chip later and the card grows then — an empty half now just reads as a bug.
        RectTransform card = Node("Card", t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(920, 540));
        NineSlice(card, "Plate", Panel);

        RectTransform crown = Node("Crown", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(140, 140));
        Sprite crownArt = LoadSprite($"{IconDir}/icon_crown.png");
        AddImage(crown, crownArt != null ? crownArt : _circle, Gold);

        flow.resultHeadlineText = Label("Headline", card, "VICTORY", _display, 96, Ink, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(820, 120));
        flow.resultPlacementText = Label("Placement", card, "1st place  ·  0 pts", _body, 36, InkMuted, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -330), new Vector2(820, 50));

        RectTransform reward = Node("RewardChip", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -410), new Vector2(720, 110));
        NineSlice(reward, "Plate", PanelLift);
        flow.resultRewardText = Label("Value", reward, "+0 coins    +0 NX", _display, 44, Gold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660, 70));

        RectTransform again = Node("PlayAgain", t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Vector2(700, 160));
        NineSlice(again, "Plate", Accent);
        Label("Label", again, "PLAY AGAIN", _display, 58, new Color32(0x06, 0x2A, 0x35, 0xFF), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 90));
        Clickable(again, flow, "OnPlayAgainPressed");

        RectTransform home = Node("HomeButton", t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -580), new Vector2(700, 120));
        NineSlice(home, "Plate", Panel);
        Label("Label", home, "HOME", _body, 40, InkMuted, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 70));
        Clickable(home, flow, "OnHomePressed");

        return screen;
    }

    // ── shared pieces ──────────────────────────────────────────────────────────────────────

    private static void Header(Transform parent, string title, LudoMenuFlow flow)
    {
        Label("Title", parent, title, _display, 78, Ink, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(800, 100));

        RectTransform back = Node("BackButton", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -150), new Vector2(96, 96));
        AddImage(back, _circle, Panel);
        Label("Glyph", back, "<", _display, 52, Ink, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(2, 4), new Vector2(80, 80));
        Clickable(back, flow, "OnBackPressed");
    }

    private static void Pawns(Transform parent, Vector2 center, float size)
    {
        string[] files = { "Pawn_Master_Red.png", "Pawn_Master_Green.png", "Pawn_Master_yellow.png", "Pawn_Master_Blue.png" };
        Color[] tints = { LudoRed, LudoGreen, LudoYellow, LudoBlue };

        float spacing = size + 30f;
        float startX = center.x - spacing * (files.Length - 1) / 2f;

        for (int i = 0; i < files.Length; i++)
        {
            RectTransform p = Node($"Pawn_{i}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + i * spacing, center.y), new Vector2(size, size));

            Sprite art = LoadSprite($"{ArtDir}/{files[i]}");
            if (art != null)
            {
                AddImage(p, art, Color.white).preserveAspect = true;
            }
            else
            {
                // Placeholder discs keep the composition intact if a pawn file is renamed.
                AddImage(p, _circle, tints[i]);
            }
        }
    }

    private static TextMeshProUGUI Currency(Transform bar, string name, string value, string iconPath, Color tint, Vector2 pos)
    {
        RectTransform chip = Node($"Chip_{name}", bar, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), pos, new Vector2(230, 58));
        NineSlice(chip, "Plate", Ground);

        RectTransform icon = Node("Icon", chip, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34, 0), new Vector2(44, 44));
        Sprite art = LoadSprite(iconPath);
        Image img = AddImage(icon, art != null ? art : _circle, art != null ? Color.white : tint);
        img.preserveAspect = true;

        return Label("Value", chip, value, _display, 32, Ink, TextAlignmentOptions.Right,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(140, 44));
    }

    // ── primitives ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The reference drop shadow: 0/4 offset, black at 40%.
    ///
    /// Named DropShadow, not Shadow: a method sharing a name with UnityEngine.UI.Shadow makes the
    /// type unresolvable inside its own body, which fails the whole assembly.
    /// </summary>
    private static void DropShadow(GameObject go)
    {
        if (go == null) return;
        UnityEngine.UI.Shadow s = go.GetComponent<UnityEngine.UI.Shadow>();
        if (s == null) s = go.AddComponent<UnityEngine.UI.Shadow>();
        s.effectColor = ShadowColor;
        s.effectDistance = ShadowOffset;
        s.useGraphicAlpha = true;
    }

    /// <summary>
    /// The gold diagonal in the top-left and bottom-right of every mockup.
    ///
    /// Built from a rotated rectangle rather than imported art: it is two nodes, it recolours with
    /// the palette, and it costs nothing to swap for a real PNG later.
    /// </summary>
    private static void CornerAccents(Transform screen)
    {
        Corner(screen, "Corner_TL", new Vector2(0f, 1f), new Vector2(-150, 150));
        Corner(screen, "Corner_BR", new Vector2(1f, 0f), new Vector2(150, -150));
    }

    private static void Corner(Transform parent, string name, Vector2 anchor, Vector2 offset)
    {
        RectTransform rt = Node(name, parent, anchor, new Vector2(0.5f, 0.5f), offset, new Vector2(520, 14));
        rt.localRotation = Quaternion.Euler(0, 0, -45f);
        AddImage(rt, _panel, Gold);

        RectTransform thin = Node("Thin", parent, anchor, new Vector2(0.5f, 0.5f),
                                  offset + new Vector2(anchor.x < 0.5f ? 26 : -26, anchor.y > 0.5f ? -26 : 26),
                                  new Vector2(520, 3));
        thin.localRotation = Quaternion.Euler(0, 0, -45f);
        AddImage(thin, _panel, new Color(Gold.r, Gold.g, Gold.b, 0.45f));
    }

    private static GameObject Screen_(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        return go;
    }

    private static void Fill(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        Stretch(rt);
        AddImage(rt, null, color);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static RectTransform Node(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static Image AddImage(RectTransform rt, Sprite sprite, Color color)
    {
        Image img = rt.gameObject.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Rounded plate stretched behind its parent — the standard card and button body.</summary>
    private static Image NineSlice(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        Stretch(rt);

        Image img = AddImage(rt, _panel, color);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 2.2f;   // keeps the corner radius tight at these sizes
        return img;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, string text, TMP_FontAsset font,
                                         float size, Color color, TextAlignmentOptions align,
                                         Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 rect)
    {
        RectTransform rt = Node(name, parent, anchor, pivot, pos, rect);

        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>
    /// Turns a node into a button. The click target is the node itself rather than a child, so the
    /// whole card is tappable — on a phone, a card whose hit area is only the label reads as broken.
    /// </summary>
    private static Button Clickable(RectTransform rt, LudoMenuFlow flow, string method, int arg = int.MinValue)
    {
        Image hit = rt.gameObject.GetComponent<Image>();
        if (hit == null)
        {
            hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);   // invisible but raycastable
        }
        hit.raycastTarget = true;

        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = hit;

        ColorBlock cb = btn.colors;
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        cb.fadeDuration = 0.06f;
        btn.colors = cb;

        // Persistent listeners so the wiring is visible in the inspector and survives a rebuild of
        // the flow object — a runtime AddListener would look like an unwired button to anyone
        // opening the scene.
        if (arg == int.MinValue)
        {
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                btn.onClick, (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction), flow, method));
        }
        else
        {
            var call = (UnityEngine.Events.UnityAction<int>)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction<int>), flow, method);
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(btn.onClick, call, arg);
        }

        return btn;
    }

    // ── asset lookup ───────────────────────────────────────────────────────────────────────

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static TMP_FontAsset FindFont(string contains)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains(contains)) return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
        return null;
    }
}
