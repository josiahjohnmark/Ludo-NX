using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Builds the Match Setup screen into Resources/UI/Screens/MatchSetup.prefab.
///
/// One screen serves quick match, vs computer and pass-and-play; <see cref="MatchSetupScreen"/>
/// shows and hides the sections. The body scrolls, because vs-computer with four houses is
/// genuinely taller than a phone, and the START button lives in a fixed footer so it is always
/// under the thumb no matter how far the player has scrolled.
///
/// KEY MEASUREMENTS (points)
///   header 102 · back 48 at (30,69) · title 33pt centred
///   panel top 116, 381 wide, 11pt inset · section label 13pt · footer band 118
///   player tiles 96x60 · house tiles 76x76 · difficulty tiles 108x54
///   seat row 338x62 · multiplier tiles 62x50 in a 5-wide grid
/// </summary>
public static class LudoNXMatchSetupBuilder
{
    private const string Art = "Assets/Art/UI/MatchSetup";
    private const string MissionArt = "Assets/Art/UI/Missions";
    private const string Shared = "Assets/Art/UI/Shared";
    private const string PrefabPath = "Assets/Resources/UI/Screens/MatchSetup.prefab";

    private const float HeaderH = 102f;
    private const float FooterH = 118f;
    private const float PanelTop = 116f;
    private const float PanelInset = 11f;
    private const float RowW = 338f;

    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "tile_tan.png", new Vector4(20, 20, 20, 20) },
        { "tile_dim.png", new Vector4(20, 20, 20, 20) },
        { "tile_selected.png", new Vector4(20, 20, 20, 20) },
        { "tile_locked.png", new Vector4(20, 20, 20, 20) },
        { "field_cream.png", new Vector4(18, 18, 18, 18) },
        { "tile_glow.png", new Vector4(40, 40, 40, 40) },
    };

    [MenuItem("Ludo NX/UI/Build Match Setup Screen", priority = 4)]
    public static void Build()
    {
        ConfigureSprites(Art, Borders);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Make("MatchSetup", null, typeof(CanvasGroup), typeof(MatchSetupScreen));
        Stretch(root);
        var screen = root.GetComponent<MatchSetupScreen>();
        screen.SetId(ScreenId.MatchSetup);

        var bg = Make("Background", root, typeof(Image));
        Stretch(bg);
        bg.GetComponent<Image>().color = Slate;
        bg.GetComponent<Image>().raycastTarget = false;

        BuildBody(root, screen);
        BuildHeader(root, screen);
        BuildFooter(root, screen);
        BuildToast(root, screen);

        screen.tileTan = Sprite($"{Art}/tile_tan.png");
        screen.tileSelected = Sprite($"{Art}/tile_selected.png");
        screen.tileLocked = Sprite($"{Art}/tile_locked.png");
        screen.pawnSprites = new[]
        {
            Sprite($"{Art}/pawn_red.png"),
            Sprite($"{Art}/pawn_green.png"),
            Sprite($"{Art}/pawn_yellow.png"),
            Sprite($"{Art}/pawn_blue.png"),
        };

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Match Setup screen built -> {PrefabPath}.");
    }

    // ======================================================================================
    // header and footer
    // ======================================================================================

    private static void BuildHeader(RectTransform root, MatchSetupScreen screen)
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

        var back = Make("BackButton", widgets, typeof(Image), typeof(Button));
        Top(back, H.Left, 30f, 69f, 48f, 48f);
        Invisible(back.GetComponent<Image>());
        var arrow = Picture(back, "Arrow", $"{Shared}/back_btn.png");
        Centre((RectTransform)arrow.transform, 0f, 0f, 18f, 23f);
        screen.backButton = back.GetComponent<Button>();

        var title = Text(widgets, "Title", "VS COMPUTER", 30f, Ink, TextAlignmentOptions.Center);
        Top((RectTransform)title.transform, H.Centre, 220f, 70f, 300f, 46f);
        screen.title = title;
    }

    private static void BuildFooter(RectTransform root, MatchSetupScreen screen)
    {
        var band = Band(root, "FooterBand", DesignBand.Edge.Bottom, FooterH);

        var energy = Text(band, "EnergyLabel", "Costs 1 energy  ·  you have 5", 13f, Cream, TextAlignmentOptions.Center);
        energy.rectTransform.anchorMin = energy.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        energy.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        energy.rectTransform.sizeDelta = new Vector2(380f, 22f);
        energy.rectTransform.anchoredPosition = new Vector2(0f, -18f);
        energy.color = new Color32(214, 206, 192, 255);
        screen.energyLabel = energy;

        var start = Make("StartButton", band, typeof(Image), typeof(Button));
        start.anchorMin = start.anchorMax = new Vector2(0.5f, 1f);
        start.pivot = new Vector2(0.5f, 0.5f);
        start.sizeDelta = new Vector2(300f, 56f);
        start.anchoredPosition = new Vector2(0f, -62f);
        Sliced(start.GetComponent<Image>(), $"{FriendsArt}/btn_blank_tan.png", 2f);
        screen.startButton = start.GetComponent<Button>();

        var label = Text(start, "Label", "START", 22f, Cream, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(0f, 3f);
        screen.startLabel = label;
    }

    // ======================================================================================
    // body
    // ======================================================================================

    private static void BuildBody(RectTransform root, MatchSetupScreen screen)
    {
        var band = Band(root, "BodyBand", DesignBand.Edge.TopFill, 0f);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.bottomReservePoints = FooterH - 6f;
        bandComp.bottomReserveReference = Reference;
        bandComp.safeAreaBaseline = SafeTop;

        var body = Make("BodyContent", band);
        Stretch(body);
        bandComp.safeAreaContent = body;

        var panel = Make("Panel", body, typeof(Image));
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.offsetMin = new Vector2(29.5f, 0f);
        panel.offsetMax = new Vector2(-29.5f, -PanelTop);
        Sliced(panel.GetComponent<Image>(), $"{FriendsArt}/panel_combined.png", 2f);
        panel.GetComponent<Image>().raycastTarget = false;

        var inner = Make("Inner", panel, typeof(RectMask2D));
        inner.anchorMin = Vector2.zero;
        inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(PanelInset, PanelInset);
        inner.offsetMax = new Vector2(-PanelInset, -PanelInset);

        var viewport = Make("Viewport", inner, typeof(Image), typeof(RectMask2D));
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(10.5f, 12f);
        viewport.offsetMax = new Vector2(-10.5f, -12f);
        Invisible(viewport.GetComponent<Image>());
        viewport.GetComponent<Image>().raycastTarget = true;

        var list = Make("Sections", viewport, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        list.anchorMin = new Vector2(0f, 1f);
        list.anchorMax = new Vector2(1f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        var vlg = list.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.spacing = 16f;
        vlg.padding = new RectOffset(0, 0, 8, 16);
        list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = list;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.scrollSensitivity = 18f;

        screen.teamSection = BuildTeamSection(list, screen);
        screen.playersSection = BuildPlayersSection(list, screen);
        screen.houseSection = BuildHouseSection(list, screen);
        screen.difficultySection = BuildDifficultySection(list, screen);
        screen.seatsSection = BuildSeatsSection(list, screen);
        screen.multiplierSection = BuildMultiplierSection(list, screen);
    }

    // ======================================================================================
    // sections
    // ======================================================================================

    private static RectTransform Section(RectTransform parent, string name, string heading, float height)
    {
        var section = Make(name, parent, typeof(LayoutElement));
        section.sizeDelta = new Vector2(RowW, height);
        var le = section.GetComponent<LayoutElement>();
        le.preferredWidth = RowW;
        le.preferredHeight = height;

        var label = Text(section, "Heading", heading, 13f, Brown, TextAlignmentOptions.MidlineLeft);
        label.characterSpacing = 6f;
        LeftIn(label.rectTransform, 4f, 11f, 300f, 20f);

        return section;
    }

    private static RectTransform BuildPlayersSection(RectTransform parent, MatchSetupScreen screen)
    {
        var section = Section(parent, "PlayersSection", "PLAYERS", 94f);

        screen.playerCountButtons = new Button[3];
        screen.playerCountBackgrounds = new Image[3];
        screen.playerCountLabels = new TextMeshProUGUI[3];

        string[] captions = { "2", "3", "4" };
        for (int i = 0; i < 3; i++)
        {
            var tile = Make($"Players{i + 2}", section, typeof(Image), typeof(Button));
            InCard(tile, RowW, H.Left, 54f + i * 116f, 58f, 96f, 60f);
            var img = tile.GetComponent<Image>();
            Sliced(img, $"{Art}/tile_tan.png", 2f);

            var label = Text(tile, "Label", captions[i], 24f, Cream, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(0f, 3f);

            var caption = Text(tile, "Caption", i == 0 ? "1v1" : i == 1 ? "3 houses" : "4 houses", 10f,
                new Color32(255, 242, 220, 200), TextAlignmentOptions.Center);
            caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            caption.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            caption.rectTransform.sizeDelta = new Vector2(90f, 14f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, 11f);

            screen.playerCountButtons[i] = tile.GetComponent<Button>();
            screen.playerCountBackgrounds[i] = img;
            screen.playerCountLabels[i] = label;
        }
        return section;
    }

    private static RectTransform BuildHouseSection(RectTransform parent, MatchSetupScreen screen)
    {
        var section = Section(parent, "HouseSection", "YOUR HOUSE", 110f);

        screen.houseButtons = new Button[4];
        screen.houseBackgrounds = new Image[4];
        screen.houseGlows = new Image[4];

        string[] names = { "RED", "GREEN", "YELLOW", "BLUE" };
        string[] files = { "pawn_red", "pawn_green", "pawn_yellow", "pawn_blue" };

        for (int i = 0; i < 4; i++)
        {
            var tile = Make(names[i], section, typeof(Image), typeof(Button));
            InCard(tile, RowW, H.Left, 42f + i * 86f, 62f, 76f, 76f);
            var img = tile.GetComponent<Image>();
            Sliced(img, $"{Art}/tile_tan.png", 2f);

            var glow = Picture(tile, "Glow", $"{Art}/tile_glow.png");
            Centre((RectTransform)glow.transform, 0f, 0f, 104f, 104f);
            glow.transform.SetAsFirstSibling();
            glow.gameObject.SetActive(false);

            var pawn = Picture(tile, "Pawn", $"{Art}/{files[i]}.png");
            Centre((RectTransform)pawn.transform, 0f, 4f, 44f, 44f);

            var caption = Text(tile, "Caption", names[i], 9.5f, new Color32(255, 242, 220, 220),
                TextAlignmentOptions.Center);
            caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            caption.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            caption.rectTransform.sizeDelta = new Vector2(72f, 14f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, 12f);

            screen.houseButtons[i] = tile.GetComponent<Button>();
            screen.houseBackgrounds[i] = img;
            screen.houseGlows[i] = glow;
        }
        return section;
    }

    private static RectTransform BuildDifficultySection(RectTransform parent, MatchSetupScreen screen)
    {
        var section = Section(parent, "DifficultySection", "DIFFICULTY", 88f);

        screen.difficultyButtons = new Button[3];
        screen.difficultyBackgrounds = new Image[3];
        screen.difficultyLabels = new TextMeshProUGUI[3];

        string[] names = { "EASY", "NORMAL", "HARD" };
        for (int i = 0; i < 3; i++)
        {
            var tile = Make(names[i], section, typeof(Image), typeof(Button));
            InCard(tile, RowW, H.Left, 58f + i * 112f, 56f, 106f, 48f);
            var img = tile.GetComponent<Image>();
            Sliced(img, $"{Art}/tile_tan.png", 2f);

            var label = Text(tile, "Label", names[i], 15f, Cream, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(0f, 2f);

            screen.difficultyButtons[i] = tile.GetComponent<Button>();
            screen.difficultyBackgrounds[i] = img;
            screen.difficultyLabels[i] = label;
        }
        return section;
    }

    private static RectTransform BuildSeatsSection(RectTransform parent, MatchSetupScreen screen)
    {
        var section = Section(parent, "SeatsSection", "AT THE TABLE", 230f);

        var content = Make("SeatContent", section, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = new Vector2(0f, -28f);
        content.sizeDelta = new Vector2(0f, 0f);
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.spacing = 8f;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        screen.seatContent = content;

        screen.seatTemplate = BuildSeatRow(content);
        return section;
    }

    private static SeatRow BuildSeatRow(RectTransform parent)
    {
        var row = Make("SeatTemplate", parent, typeof(LayoutElement), typeof(SeatRow));
        row.sizeDelta = new Vector2(RowW, 62f);
        var le = row.GetComponent<LayoutElement>();
        le.preferredWidth = RowW;
        le.preferredHeight = 62f;

        var view = row.GetComponent<SeatRow>();

        var card = Make("Card", row, typeof(Image));
        Stretch(card);
        Sliced(card.GetComponent<Image>(), $"{FriendsArt}/row_card.png", 2f);
        card.GetComponent<Image>().raycastTarget = false;
        view.card = card.GetComponent<Image>();

        var pawn = Picture(row, "Pawn", $"{Art}/pawn_red.png");
        InCard((RectTransform)pawn.transform, RowW, H.Left, 34f, 31f, 34f, 34f);
        view.pawn = pawn;

        var house = Text(row, "House", "GREEN", 12f, Brown, TextAlignmentOptions.MidlineLeft);
        LeftIn(house.rectTransform, 60f, 22f, 90f, 18f);
        view.houseLabel = house;

        var style = Text(row, "Style", "Hunter", 10f, SubInk, TextAlignmentOptions.MidlineLeft);
        LeftIn(style.rectTransform, 60f, 40f, 90f, 16f);
        view.styleLabel = style;

        var field = Make("NameField", row, typeof(Image), typeof(TMP_InputField));
        InCard(field, RowW, H.Right, 322f, 31f, 168f, 40f);
        Sliced(field.GetComponent<Image>(), $"{Art}/field_cream.png", 2f);

        var textArea = Make("TextArea", field, typeof(RectMask2D));
        Stretch(textArea);
        textArea.offsetMin = new Vector2(12f, 2f);
        textArea.offsetMax = new Vector2(-12f, -2f);

        var placeholder = Text(textArea, "Placeholder", "Name", 14f, new Color32(140, 120, 92, 255),
            TextAlignmentOptions.MidlineLeft);
        Stretch(placeholder.rectTransform);

        var text = Text(textArea, "Text", "", 14f, Ink, TextAlignmentOptions.MidlineLeft);
        Stretch(text.rectTransform);

        var input = field.GetComponent<TMP_InputField>();
        input.textViewport = textArea;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.fontAsset = Lilita;
        input.pointSize = 14f;
        input.targetGraphic = field.GetComponent<Image>();
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 14;
        view.nameField = input;

        row.gameObject.SetActive(false);
        return view;
    }

    private static RectTransform BuildTeamSection(RectTransform parent, MatchSetupScreen screen)
    {
        var section = Section(parent, "TeamSection", "HOUSES", 132f);

        screen.teamYourPawns = new Image[2];
        screen.teamOpponentPawns = new Image[2];

        var card = Make("Card", section, typeof(Image));
        InCard(card, RowW, H.Left, RowW / 2f, 74f, RowW, 96f);
        Sliced(card.GetComponent<Image>(), $"{FriendsArt}/row_card.png", 2f);
        card.GetComponent<Image>().raycastTarget = false;

        var you = Text(card, "YouLabel", "YOU", 12f, Brown, TextAlignmentOptions.Center);
        InCard(you.rectTransform, RowW, H.Left, 70f, 18f, 120f, 18f);

        var them = Text(card, "ThemLabel", "OPPONENT", 12f, Brown, TextAlignmentOptions.Center);
        InCard(them.rectTransform, RowW, H.Left, 268f, 18f, 120f, 18f);

        var vs = Text(card, "VS", "VS", 20f, new Color32(188, 56, 46, 255), TextAlignmentOptions.Center);
        InCard(vs.rectTransform, RowW, H.Left, 169f, 44f, 60f, 30f);

        string[] files = { "pawn_red", "pawn_yellow", "pawn_green", "pawn_blue" };
        float[] xs = { 44f, 96f, 242f, 294f };
        for (int i = 0; i < 4; i++)
        {
            var pawn = Picture(card, $"Pawn{i}", $"{Art}/{files[i]}.png");
            InCard((RectTransform)pawn.transform, RowW, H.Left, xs[i], 46f, 40f, 40f);
            if (i < 2) screen.teamYourPawns[i] = pawn;
            else screen.teamOpponentPawns[i - 2] = pawn;
        }

        var blurb = Text(section, "Blurb", "You play RED and YELLOW.", 11f, SubInk, TextAlignmentOptions.Center);
        blurb.textWrappingMode = TextWrappingModes.Normal;
        InCard(blurb.rectTransform, RowW, H.Left, RowW / 2f, 76f + 34f, RowW - 24f, 30f);
        screen.teamBlurb = blurb;

        return section;
    }

    private static RectTransform BuildMultiplierSection(RectTransform parent, MatchSetupScreen screen)
    {
        var section = Section(parent, "MultiplierSection", "STAKE", 202f);

        var grid = Make("Grid", section, typeof(GridLayoutGroup));
        grid.anchorMin = new Vector2(0f, 1f);
        grid.anchorMax = new Vector2(1f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.anchoredPosition = new Vector2(0f, -26f);
        grid.sizeDelta = new Vector2(0f, 108f);
        var g = grid.GetComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(62f, 50f);
        g.spacing = new Vector2(7f, 8f);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 5;
        g.childAlignment = TextAnchor.UpperCenter;
        screen.multiplierGrid = grid;

        screen.multiplierTemplate = BuildMultiplierTile(grid);

        var stake = Text(section, "StakeSummary", "Win  +20 NX          Lose  -10 NX", 13f, Brown,
            TextAlignmentOptions.Center);
        InCard(stake.rectTransform, RowW, H.Left, RowW / 2f, 148f, RowW, 20f);
        screen.stakeSummary = stake;

        var capacity = Text(section, "CapacityLabel", "Max energy 20  ·  your ceiling x5", 11f, SubInk,
            TextAlignmentOptions.Center);
        InCard(capacity.rectTransform, RowW, H.Left, RowW / 2f, 168f, RowW, 18f);
        screen.capacityLabel = capacity;

        // Says what the padlocks are for, so a locked tile reads as a goal rather than a refusal.
        var hint = Text(section, "UnlockHint", "Bigger stakes unlock as your energy capacity grows",
            11f, new Color32(120, 96, 66, 190), TextAlignmentOptions.Center);
        InCard(hint.rectTransform, RowW, H.Left, RowW / 2f, 186f, RowW, 18f);

        return section;
    }

    private static MultiplierTile BuildMultiplierTile(RectTransform parent)
    {
        var tile = Make("MultiplierTemplate", parent, typeof(Image), typeof(Button), typeof(MultiplierTile));
        var view = tile.GetComponent<MultiplierTile>();
        var img = tile.GetComponent<Image>();
        Sliced(img, $"{Art}/tile_tan.png", 2f);

        var glow = Picture(tile, "Glow", $"{Art}/tile_glow.png");
        Centre((RectTransform)glow.transform, 0f, 0f, 86f, 74f);
        glow.transform.SetAsFirstSibling();
        glow.gameObject.SetActive(false);

        var label = Text(tile, "Label", "x1", 17f, Cream, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(0f, 2f);

        // Corner, not centre: a locked tile must still say which multiple it is, or the player
        // cannot tell what they are climbing toward.
        var padlock = Picture(tile, "Lock", $"{Art}/icon_lock.png");
        var lockRt = (RectTransform)padlock.transform;
        lockRt.anchorMin = lockRt.anchorMax = new Vector2(1f, 0f);
        lockRt.pivot = new Vector2(1f, 0f);
        lockRt.sizeDelta = new Vector2(15f, 15f);
        lockRt.anchoredPosition = new Vector2(-4f, 4f);
        padlock.gameObject.SetActive(false);

        view.button = tile.GetComponent<Button>();
        view.background = img;
        view.glow = glow;
        view.lockIcon = padlock;
        view.label = label;

        tile.gameObject.SetActive(false);
        return view;
    }

    // ======================================================================================
    // toast
    // ======================================================================================

    private static void BuildToast(RectTransform root, MatchSetupScreen screen)
    {
        var band = Band(root, "PopupBand", DesignBand.Edge.Fill, 0f);

        var toast = Make("Message", band, typeof(CanvasGroup), typeof(Image));
        toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 0f);
        toast.pivot = new Vector2(0.5f, 0.5f);
        toast.sizeDelta = new Vector2(340f, 52f);
        toast.anchoredPosition = new Vector2(0f, 200f);
        Sliced(toast.GetComponent<Image>(), $"{FriendsArt}/btn_blank_tan.png", 2f);
        toast.GetComponent<Image>().raycastTarget = false;

        var label = Text(toast, "Label", "Reach 800 max energy", 15f, Cream, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);

        screen.toast = toast.GetComponent<CanvasGroup>();
        screen.toastText = label;
    }
}
