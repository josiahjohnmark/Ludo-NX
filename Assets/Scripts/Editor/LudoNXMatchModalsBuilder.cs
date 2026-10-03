using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Builds the three pre-match popups into Resources/UI/Modals/: MatchSetup.prefab (VS Computer /
/// Local Play, sharing <see cref="MatchSetupModal"/>) and OnlineMatch.prefab
/// (<see cref="OnlineMatchModal"/>).
///
/// Built from the user's own reference, dropped in
/// shop_&amp;_play_card_assets_&amp;refrence/_extracted/: a light-cream card with dice icons baked into
/// its corners (card_for_play.png -&gt; card_shell.png), a mascot avatar straddling the top edge, a
/// PLAYERS count row, a HOUSES list, and one red PLAY button. Cards popup over Home rather than
/// replacing it — the scrim behind them is a soft, low-opacity dark fade, not a full black-out.
///
/// Two changes from a literal trace of the reference, per the user:
///   - stakes are online-only, so neither VS Computer nor Local Play show a multiplier; on the
///     online card the badge itself steps the stake up the x1-x1000 ladder, capped by capacity.
///   - VS Computer keeps its EASY/NORMAL/HARD row, which the reference card does not show.
///
/// A "your house" colour picker is kept (compact, four dots) even though the reference does not
/// show one, because picking a colour is real gameplay the app already had; it was folded into a
/// small strip rather than invented as a whole new section.
///
/// KEY MEASUREMENTS (points, 440x956 frame)
///   card 340 wide · avatar 80 at -18pt above the card's top edge · header zone 108
///   footer zone 108 · pill/row height 56 · house row pitch 56 (no gap)
/// </summary>
public static class LudoNXMatchModalsBuilder
{
    private const string Art = "Assets/Art/UI/MatchSetup";
    private const string Cards = "Assets/Art/UI/MatchSetup/Cards";
    private const string EmojiArt = "Assets/Art/UI/MatchSetup/Emoji";
    private const string CircleSprite = "Assets/Resources/ui_circle.png";
    private const string MatchSetupPrefab = "Assets/Resources/UI/Modals/MatchSetup.prefab";
    private const string OnlineMatchPrefab = "Assets/Resources/UI/Modals/OnlineMatch.prefab";
    private const string RulesPrefab = "Assets/Resources/UI/Modals/Rules.prefab";

    private const float CardW = 340f;
    private const float RulesCardW = 380f;
    private const float RulesCardH = 560f;
    private const float RowH = 56f;
    private const float HeaderH = 108f;
    private const float FooterH = 104f;
    private const float BodyH = 420f;
    private const float CardH = HeaderH + BodyH + FooterH;
    private const float Inset = 18f; // content margin inside the card

    private static readonly Color32 InkBlack = new Color32(30, 22, 12, 255);
    private static readonly Color32 MutedBrown = new Color32(120, 96, 70, 255);
    private static readonly Color32[] PawnColours =
    {
        new Color32(224, 62, 52, 255),   // red
        new Color32(58, 168, 92, 255),   // green
        new Color32(232, 176, 46, 255),  // yellow
        new Color32(58, 122, 224, 255),  // blue
    };

    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "card_shell.png", new Vector4(110, 110, 110, 110) },
        { "play_btn.png", new Vector4(40, 40, 40, 40) },
        { "pill_cream.png", new Vector4(20, 20, 20, 20) },
        { "play_plate.png", new Vector4(20, 20, 20, 20) },
    };

    private static TMP_FontAsset _nunito;
    private static TMP_FontAsset Nunito
    {
        get
        {
            if (_nunito == null)
            {
                _nunito = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Bold.asset");
                if (_nunito == null) _nunito = Lilita;
            }
            return _nunito;
        }
    }

    [MenuItem("Ludo NX/UI/Build Match Setup Modals", priority = 4)]
    public static void Build()
    {
        ConfigureSprites(Cards, Borders);
        ConfigureEmoji();
        Directory.CreateDirectory(Path.GetDirectoryName(MatchSetupPrefab));

        BuildMatchSetupModal();
        BuildOnlineMatchModal();
        BuildRulesModal();
        BuildPauseModal();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ludo NX] Match setup modals built -> Resources/UI/Modals/.");
    }

    [MenuItem("Ludo NX/UI/Build Rules Modal", priority = 5)]
    public static void BuildRules()
    {
        ConfigureSprites(Cards, Borders);
        Directory.CreateDirectory(Path.GetDirectoryName(RulesPrefab));
        BuildRulesModal();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ludo NX] Rules modal built -> " + RulesPrefab);
    }

    // ======================================================================================
    // shared shell: scrim + card + mascot avatar
    // ======================================================================================

    /// <summary>
    /// How dark the screen behind a card looks: 55%, the usual weight for a game popup — the
    /// menu stays recognisable but stops competing with the card. The project renders in Linear,
    /// where a see-through layer reads lighter than it would in Figma, so the alpha that actually
    /// produces a 55% dim is 1 - (1 - 0.55)^2.2 (see the Linear colour note in project memory).
    /// </summary>
    private const float PerceivedDim = 0.55f;
    private static readonly float ScrimAlpha = 1f - Mathf.Pow(1f - PerceivedDim, 2.2f);

    /// <summary>
    /// Round X centred under the card, closing it. A child of the card so it scales in and out
    /// with it; its hit area is a little larger than the drawn disc so it is easy to tap.
    /// </summary>
    private static void CloseButton(RectTransform card, UIModal modal)
    {
        var btn = Make("CloseButton", card, typeof(Image), typeof(Button));
        btn.anchorMin = btn.anchorMax = new Vector2(0.5f, 0f);
        btn.pivot = new Vector2(0.5f, 0.5f);
        btn.sizeDelta = new Vector2(50f, 50f);
        btn.anchoredPosition = new Vector2(0f, -(CloseGap + CloseSize / 2f));
        Invisible(btn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(btn.GetComponent<Button>().onClick, modal.Dismiss);

        var disc = Picture(btn, "Icon", "Assets/Art/UI/Shared/btn_cancel_black_circle.png");
        Centre((RectTransform)disc.transform, 0f, 0f, CloseSize, CloseSize);
        disc.preserveAspect = true;
        btn.GetComponent<Button>().targetGraphic = disc;
    }

    private const float CloseSize = 34f;
    private const float CloseGap = 16f;

    /// <summary>Scrim and the card panel, built under an already-created modal root.</summary>
    private static void Shell(RectTransform root, UIModal modal, out RectTransform card, out RectTransform header,
        out RectTransform footer)
    {
        var band = Band(root, "Frame", DesignBand.Edge.Fill, 0f);
        band.GetComponent<DesignBand>().referenceSize = Reference;

        var content = Make("Content", band);
        Stretch(content);

        var scrim = Make("Scrim", content, typeof(Image), typeof(Button));
        Stretch(scrim);
        // Reach well past the frame: the modal root is inset to the safe area, and the dim must
        // also cover the status bar and home-indicator strips on a notched phone.
        scrim.offsetMin = new Vector2(-3000f, -3000f);
        scrim.offsetMax = new Vector2(3000f, 3000f);
        scrim.GetComponent<Image>().color = new Color(0f, 0f, 0f, ScrimAlpha);
        scrim.GetComponent<Button>().transition = Selectable.Transition.None;
        // Persistent, so it survives into the prefab — a plain AddListener here is editor-only
        // and silently leaves the scrim dead at runtime.
        UnityEventTools.AddPersistentListener(scrim.GetComponent<Button>().onClick, modal.OnScrimTapped);

        card = Make("Card", content, typeof(Image));
        Centre(card, 0f, 0f, CardW, CardH);
        Sliced(card.GetComponent<Image>(), "Assets/Art/UI/Menu/panel_menu.png", 1f);
        // Must stay a raycast target: without it, a tap on blank card surface (no button under the
        // finger) falls through to the scrim behind it and closes the card by "mistake".
        card.GetComponent<Image>().raycastTarget = true;
        modal.card = card;

        header = Make("Header", card, typeof(RectTransform));
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, HeaderH);
        header.anchoredPosition = Vector2.zero;

        footer = Make("Footer", card, typeof(RectTransform));
        footer.anchorMin = new Vector2(0f, 0f);
        footer.anchorMax = new Vector2(1f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.sizeDelta = new Vector2(0f, FooterH);
        footer.anchoredPosition = Vector2.zero;
    }

    /// <summary>Mascot avatar straddling the card's top edge, plus the title beneath it.</summary>
    private static TextMeshProUGUI BuildIdentity(RectTransform header, string titleText)
    {
        var avatar = Picture(header, "Avatar", $"{Cards}/mascot_avatar.png");
        avatar.preserveAspect = true;
        var avatarRt = (RectTransform)avatar.transform;
        avatarRt.anchorMin = avatarRt.anchorMax = new Vector2(0.5f, 1f);
        avatarRt.pivot = new Vector2(0.5f, 0.5f);
        avatarRt.sizeDelta = new Vector2(80f, 80f);
        avatarRt.anchoredPosition = new Vector2(0f, 22f); // pokes 22pt above the card's top edge

        var title = Text(header, "Title", titleText, 22f, InkBlack, TextAlignmentOptions.Center);
        title.font = Lilita;
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(CardW - 20f, 30f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -46f);
        return title;
    }

    /// <summary>The scrollable body: a VerticalLayoutGroup of LayoutElement sections.</summary>
    private static RectTransform BuildScrollBody(RectTransform card)
    {
        var viewport = Make("BodyViewport", card, typeof(Image), typeof(RectMask2D));
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(Inset, FooterH);
        viewport.offsetMax = new Vector2(-Inset, -HeaderH);
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
        vlg.spacing = 14f;
        vlg.padding = new RectOffset(0, 0, 6, 12);
        list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = list;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.scrollSensitivity = 16f;

        return list;
    }

    /// <summary>A section heading with a rule flanking either side, matching the reference.</summary>
    private static RectTransform Section(RectTransform parent, string name, string heading, float height)
    {
        var section = Make(name, parent, typeof(LayoutElement));
        var width = CardW - Inset * 2f;
        section.sizeDelta = new Vector2(width, height);
        var le = section.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;

        var label = Text(section, "Heading", heading, 12.5f, InkBlack, TextAlignmentOptions.Center);
        label.font = Lilita;
        label.characterSpacing = 2f;
        InCard(label.rectTransform, width, H.Left, width / 2f, 9f, 150f, 18f);

        var ruleL = Picture(section, "RuleL", $"{Cards}/rule_line.png");
        Sliced(ruleL, $"{Cards}/rule_line.png", 4f);
        InCard((RectTransform)ruleL.transform, width, H.Left, 0f, 9f, width / 2f - 78f, 3f);

        var ruleR = Picture(section, "RuleR", $"{Cards}/rule_line.png");
        Sliced(ruleR, $"{Cards}/rule_line.png", 4f);
        InCard((RectTransform)ruleR.transform, width, H.Right, width, 9f, width / 2f - 78f, 3f);

        return section;
    }

    private static Button MakePill(RectTransform parent, string name, float cx, float w, float h, string caption,
        out Image background, out TextMeshProUGUI label)
    {
        var tile = Make(name, parent, typeof(Image), typeof(Button));
        InCard(tile, parent.sizeDelta.x, H.Left, cx, h / 2f, w, h);
        background = tile.GetComponent<Image>();
        Sliced(background, $"{Cards}/pill_cream.png", 4f);

        label = Text(tile, "Label", caption, 24f, InkBlack, TextAlignmentOptions.Center);
        label.font = Lilita;
        Stretch(label.rectTransform);
        return tile.GetComponent<Button>();
    }

    // ======================================================================================
    // MatchSetup.prefab — VS Computer / Local Play
    // ======================================================================================

    private static void BuildMatchSetupModal()
    {
        var root = Make("MatchSetupModal", null, typeof(CanvasGroup), typeof(MatchSetupModal));
        var modal = root.GetComponent<MatchSetupModal>();
        Stretch(root);

        Shell(root, modal, out var cardRt, out var header, out var footer);
        RectTransform card = cardRt;
        modal.title = BuildIdentity(header, "VS COMPUTER");

        var body = BuildScrollBody(cardRt);
        float w = CardW - Inset * 2f;

        // PLAYERS
        var playersSection = Section(body, "Players", "PLAYERS", 9f + RowH);
        modal.playerCountButtons = new Button[3];
        modal.playerCountBackgrounds = new Image[3];
        modal.playerCountLabels = new TextMeshProUGUI[3];
        string[] counts = { "2", "3", "4" };
        float pillW = (w - 16f) / 3f;
        for (int i = 0; i < 3; i++)
        {
            float cx = i * (pillW + 8f) + pillW / 2f;
            var btn = MakePill(playersSection, $"Count{i}", cx, pillW, RowH, counts[i], out var bg, out var lbl);
            InCard((RectTransform)btn.transform, w, H.Left, cx, 27f + RowH / 2f, pillW, RowH);
            modal.playerCountButtons[i] = btn;
            modal.playerCountBackgrounds[i] = bg;
            modal.playerCountLabels[i] = lbl;
        }

        // YOUR HOUSE — a compact colour picker, kept from the existing app (see class remarks)
        var houseSection = Section(body, "YourHouse", "YOUR HOUSE", 9f + 52f);
        modal.houseButtons = new Button[4];
        modal.houseSwatches = new Image[4];
        modal.houseRings = new Image[4];
        float dotW = (w - 3f * 14f) / 4f;
        for (int i = 0; i < 4; i++)
        {
            float cx = i * (dotW + 14f) + dotW / 2f;
            var dot = Make($"House{i}", houseSection, typeof(Image), typeof(Button));
            InCard(dot, w, H.Left, cx, 25f + 26f, dotW, 44f);

            var ring = Picture(dot, "Ring", CircleSprite);
            Centre((RectTransform)ring.transform, 0f, 0f, 44f, 44f);
            ring.color = new Color32(232, 185, 38, 255);
            ring.gameObject.SetActive(i == 0);

            var fill = Picture(dot, "Fill", CircleSprite);
            Centre((RectTransform)fill.transform, 0f, 0f, 34f, 34f);
            fill.color = PawnColours[i];

            Invisible(dot.GetComponent<Image>());
            modal.houseButtons[i] = dot.GetComponent<Button>();
            modal.houseSwatches[i] = fill;
            modal.houseRings[i] = ring;
        }

        // HOUSES
        var housesHeader = Section(body, "HousesHeader", "HOUSES", 18f);
        var seatWrap = Make("SeatWrap", body, typeof(LayoutElement));
        seatWrap.sizeDelta = new Vector2(w, RowH * 4f);
        var seatLe = seatWrap.GetComponent<LayoutElement>();
        seatLe.preferredWidth = w;
        seatLe.preferredHeight = RowH * 4f;

        var seatContent = Make("SeatContent", seatWrap, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        seatContent.anchorMin = new Vector2(0f, 1f);
        seatContent.anchorMax = new Vector2(1f, 1f);
        seatContent.pivot = new Vector2(0.5f, 1f);
        var seatVlg = seatContent.GetComponent<VerticalLayoutGroup>();
        seatVlg.childAlignment = TextAnchor.UpperCenter;
        seatVlg.childControlHeight = false;
        seatVlg.childControlWidth = false;
        seatVlg.spacing = 0f;
        seatContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        modal.seatContent = seatContent;

        modal.seatTemplate = BuildSeatRow(seatContent, w);

        // DIFFICULTY (VS Computer only)
        var difficulty = Section(body, "Difficulty", "DIFFICULTY", 9f + RowH);
        modal.difficultyButtons = new Button[3];
        modal.difficultyBackgrounds = new Image[3];
        modal.difficultyLabels = new TextMeshProUGUI[3];
        string[] diffNames = { "EASY", "NORMAL", "HARD" };
        float diffW = (w - 16f) / 3f;
        for (int i = 0; i < 3; i++)
        {
            float cx = i * (diffW + 8f) + diffW / 2f;
            var tile = Make($"Diff{i}", difficulty, typeof(Image), typeof(Button));
            InCard(tile, w, H.Left, cx, 27f + RowH / 2f, diffW, RowH);
            var img = tile.GetComponent<Image>();
            Sliced(img, $"{Cards}/pill_cream.png", 4f);

            var lbl = Text(tile, "Label", diffNames[i], 13f, InkBlack, TextAlignmentOptions.Center);
            lbl.font = Lilita;
            Stretch(lbl.rectTransform);

            modal.difficultyButtons[i] = tile.GetComponent<Button>();
            modal.difficultyBackgrounds[i] = img;
            modal.difficultyLabels[i] = lbl;
        }
        modal.difficultySection = difficulty;

        BuildFooter(footer, w, out modal.energyLabel, out modal.startButton, out modal.startLabel);
        modal.pillNormal = Sprite($"{Cards}/pill_cream.png");
        modal.pillSelected = Sprite($"{Cards}/pill_cream.png"); // tinted at runtime, see Paint()
        modal.pawnSprites = BuildPawnSprites();

        BuildToast(card, out modal.toast, out modal.toastText);

        CloseButton(card, modal);
        // Centre the whole group (avatar poking 62 above, X hanging 76 below), not just the card.
        card.anchoredPosition = new Vector2(0f, (CloseGap + CloseSize - 62f) / 2f);

        Directory.CreateDirectory(Path.GetDirectoryName(MatchSetupPrefab));
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, MatchSetupPrefab);
        Object.DestroyImmediate(root.gameObject);
    }

    private static SeatRow BuildSeatRow(RectTransform parent, float width)
    {
        var row = Make("SeatTemplate", parent, typeof(LayoutElement), typeof(SeatRow));
        row.sizeDelta = new Vector2(width, RowH);
        var le = row.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = RowH;

        var view = row.GetComponent<SeatRow>();

        var card = Make("Card", row, typeof(Image));
        Stretch(card);
        Sliced(card.GetComponent<Image>(), $"{Cards}/pill_cream.png", 4f);
        card.GetComponent<Image>().raycastTarget = false;
        view.card = card.GetComponent<Image>();

        // Sits in the row's own top-left corner rather than in the margin outside it — the row
        // list scrolls inside a masked viewport, and anything positioned outside a row's bounds
        // is clipped by that mask.
        var role = Text(row, "RoleTag", "CP", 8.5f, MutedBrown, TextAlignmentOptions.MidlineLeft);
        role.font = Lilita;
        LeftIn(role.rectTransform, 14f, 10f, 26f, 12f);
        view.roleTag = role;

        var number = Text(row, "Number", "1.", 18f, InkBlack, TextAlignmentOptions.MidlineLeft);
        number.font = Lilita;
        LeftIn(number.rectTransform, 14f, RowH / 2f + 2f, 30f, 24f);
        view.numberLabel = number;

        var nameLabel = Text(row, "NameLabel", "YOU", 16f, InkBlack, TextAlignmentOptions.Center);
        nameLabel.font = Lilita;
        Centre(nameLabel.rectTransform, -6f, 4f, width - 120f, 22f);
        view.nameLabel = nameLabel;

        var field = Make("NameField", row, typeof(Image), typeof(TMP_InputField));
        Centre(field, -6f, 6f, width - 120f, 26f);
        Invisible(field.GetComponent<Image>()); // needed as a raycast target so taps focus the field
        var textArea = Make("TextArea", field, typeof(RectMask2D));
        Stretch(textArea);
        var fieldText = Text(textArea, "Text", "", 16f, InkBlack, TextAlignmentOptions.Center);
        fieldText.font = Lilita;
        Stretch(fieldText.rectTransform);
        var input = field.GetComponent<TMP_InputField>();
        input.textViewport = textArea;
        input.textComponent = fieldText;
        input.targetGraphic = field.GetComponent<Image>();
        input.fontAsset = Lilita;
        input.pointSize = 16f;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 14;
        view.nameField = input;

        var hint = Text(row, "Hint", "TAP TO EDIT", 8.5f, MutedBrown, TextAlignmentOptions.Center);
        Centre(hint.rectTransform, -6f, -12f, width - 120f, 14f);
        view.hintLabel = hint;

        var ring = Picture(row, "Ring", CircleSprite);
        InCard((RectTransform)ring.transform, width, H.Right, width - 14f, RowH / 2f, 40f, 40f);
        ring.color = new Color32(255, 255, 255, 220);
        var pawn = Picture(row, "Pawn", CircleSprite);
        InCard((RectTransform)pawn.transform, width, H.Right, width - 14f, RowH / 2f, 30f, 30f);
        view.pawn = pawn;

        row.gameObject.SetActive(false);
        return view;
    }

    // ======================================================================================
    // OnlineMatch.prefab
    // ======================================================================================

    // Every measurement below is taken off the user's reference (ONLINE MATCH SCREEN.png) in its
    // own units — a 381-wide card whose top edge is y=0 — and scaled onto this 340-wide card, so
    // the three pre-match cards stay the same width while this one keeps the reference's exact
    // proportions. The reference grid and plate sit ~7pt right of the card's centre line while
    // its dividers are centred; that reads as a drafting slip, so both are centred here.
    private const float RefCardW = 381f;
    private const float K = CardW / RefCardW;
    private const float OnlineCardH = 589.5f * K;

    /// <summary>Reference units -&gt; card points.</summary>
    private static float R(float refUnits) => refUnits * K;

    /// <summary>Places a rect by its centre, in reference units measured from the card's top-left.</summary>
    private static void AtRef(RectTransform rt, float cx, float cy, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(R(w), R(h));
        rt.anchoredPosition = new Vector2(R(cx), -R(cy));
    }

    private static void BuildOnlineMatchModal()
    {
        var root = Make("OnlineMatchModal", null, typeof(CanvasGroup), typeof(OnlineMatchModal));
        var modal = root.GetComponent<OnlineMatchModal>();
        Stretch(root);

        Shell(root, modal, out var card, out var header, out var footer);
        Centre(card, 0f, 0f, CardW, OnlineCardH);
        // card_for_play.png is 2x of the 381-wide reference card; slice it at that ratio (scaled to
        // this 340-wide card) so the cream frame and corner dice come out the reference's size.
        card.GetComponent<Image>().pixelsPerUnitMultiplier = 2f / K;
        // This card places everything directly against the card edges; the shared header/footer
        // zones belong to the other card's layout.
        Object.DestroyImmediate(header.gameObject);
        Object.DestroyImmediate(footer.gameObject);

        const float mid = RefCardW / 2f;

        // Mascot, straddling the top edge: 96.5 across, centre 6.75 above the edge.
        var avatar = Picture(card, "Avatar", $"{Cards}/mascot_avatar.png");
        avatar.preserveAspect = true;
        AtRef((RectTransform)avatar.transform, mid, -6.75f, 96.5f, 96.5f);

        // ONLINE, where the reference had its token and dice.
        var title = Text(card, "Title", "QUICK MATCH", R(32f), Color.black, TextAlignmentOptions.Center);
        title.font = Lilita;
        AtRef(title.rectTransform, mid, 90f, 300f, 44f);
        modal.title = title;

        // EMOJI SELECTED heading on the reference's first divider line, rules either side.
        var heading = Text(card, "EmojiHeading", "EMOJI SELECTED", R(15f), Color.black, TextAlignmentOptions.Center);
        heading.font = Lilita;
        AtRef(heading.rectTransform, mid, 142.5f, 150f, 22f);
        const float ruleW = 77.5f; // (298 line - 119 text - 2 x 12 gap) / 2
        RefLine(card, "RuleL", 41.5f + ruleW / 2f, 142.5f, ruleW);
        RefLine(card, "RuleR", 339.5f - ruleW / 2f, 142.5f, ruleW);

        // Emoji room: 66 x 64 tiles, 7.33 across / 6 down, first row at 176.5.
        modal.emojiSlots = new FlipbookImage[8];
        const float tileW = 66f, tileH = 64f, gapX = 22f / 3f, gapY = 6f;
        const float gridW = 4f * tileW + 3f * gapX;
        const float gridLeft = mid - gridW / 2f;
        for (int i = 0; i < 8; i++)
        {
            int col = i % 4, row = i / 4;
            var slot = Make($"Slot{i}", card, typeof(Image));
            AtRef(slot, gridLeft + col * (tileW + gapX) + tileW / 2f, 176.5f + row * (tileH + gapY) + tileH / 2f,
                tileW, tileH);
            slot.GetComponent<Image>().sprite = Sprite($"{Cards}/emoji_slot.png");
            slot.GetComponent<Image>().raycastTarget = false;

            var emoji = Make("Emoji", slot, typeof(Image), typeof(FlipbookImage));
            Centre(emoji, 0f, 0f, R(56f), R(56f));
            emoji.GetComponent<Image>().raycastTarget = false;
            emoji.GetComponent<Image>().preserveAspect = true;
            modal.emojiSlots[i] = emoji.GetComponent<FlipbookImage>();
        }
        modal.emojiSets = BuildEmojiSets();

        // Second divider, full reference width.
        RefLine(card, "Divider", mid, 336.75f, 298f);

        // PLAY on its cream plate. The plate's round bump holds the stake badge; the badge is
        // the multiplier control (see OnlineMatchModal.StepMultiplier).
        const float plateW = 223f, plateH = 118f, plateTop = 348.5f;
        const float plateLeft = mid - 218.5f / 2f; // centre the plate's body, not its bump
        var plate = Picture(card, "PlayPlate", $"{Cards}/play_plate.png");
        AtRef((RectTransform)plate.transform, plateLeft + plateW / 2f, plateTop + plateH / 2f, plateW, plateH);

        var play = Make("PlayButton", card, typeof(Image), typeof(Button));
        AtRef(play, plateLeft + 6f + 207f / 2f, plateTop + 16f + 97f / 2f, 207f, 97f);
        play.GetComponent<Image>().sprite = Sprite($"{Cards}/play_btn_labelled.png");
        modal.startButton = play.GetComponent<Button>();

        var badge = Make("MultiplierBadge", card, typeof(Image), typeof(Button));
        AtRef(badge, plateLeft + 188.75f, plateTop + 34.5f, 65f, 65f);
        badge.GetComponent<Image>().sprite = Sprite($"{Cards}/multiplier_badge.png");
        var badgeLabel = Text(badge, "Label", "X1", R(20f), new Color32(255, 246, 228, 255), TextAlignmentOptions.Center);
        badgeLabel.font = Lilita;
        badgeLabel.enableAutoSizing = true;
        badgeLabel.fontSizeMax = R(20f);
        badgeLabel.fontSizeMin = R(11f);
        Centre(badgeLabel.rectTransform, 0f, R(2f), R(52f), R(30f));
        modal.multiplierBadgeButton = badge.GetComponent<Button>();
        modal.multiplierBadgeLabel = badgeLabel;

        // Energy under the plate: bolt, then current/max.
        var bolt = Picture(card, "EnergyIcon", $"{HomeArt}/TopBar/energy_icon.png");
        bolt.preserveAspect = true;
        AtRef((RectTransform)bolt.transform, 147.5f + 18.5f / 2f, 483.25f, 18.5f, 26.5f);
        var energy = Text(card, "EnergyLabel", "20/25", R(20.1f), Color.black, TextAlignmentOptions.MidlineLeft);
        energy.font = Lilita;
        AtRef(energy.rectTransform, 176f + 45f, 484.5f, 90f, 26f);
        modal.energyLabel = energy;

        BuildOnlineToast(card, modal);

        CloseButton(card, modal);
        // Centre the whole group (avatar poking above, X hanging below), not just the card.
        card.anchoredPosition = new Vector2(0f, (CloseGap + CloseSize - R(6.75f + 96.5f / 2f)) / 2f);

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, OnlineMatchPrefab);
        Object.DestroyImmediate(root.gameObject);
    }

    /// <summary>The reference's cream divider line (horixontal_line.png, 298 x 3 at 1x).</summary>
    private static void RefLine(RectTransform card, string name, float cx, float cy, float w)
    {
        var line = Picture(card, name, $"{Cards}/rule_line.png");
        AtRef((RectTransform)line.transform, cx, cy, w, 3f);
    }

    private static void BuildOnlineToast(RectTransform card, OnlineMatchModal modal)
    {
        var t = Make("Toast", card, typeof(CanvasGroup), typeof(Image));
        AtRef(t, RefCardW / 2f, 318f, 320f, 44f);
        Sliced(t.GetComponent<Image>(), $"{Cards}/pill_cream.png", 4f);
        t.GetComponent<Image>().raycastTarget = false;

        var label = Text(t, "Label", "Message", R(13f), InkBlack, TextAlignmentOptions.Center);
        label.font = Lilita;
        label.textWrappingMode = TextWrappingModes.Normal;
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(R(10f), 0f);
        label.rectTransform.offsetMax = new Vector2(-R(10f), 0f);

        modal.toast = t.GetComponent<CanvasGroup>();
        modal.toast.alpha = 0f;
        modal.toastText = label;
    }

    // ======================================================================================
    // shared: footer, toast, pawns, emoji import
    // ======================================================================================

    private static void BuildFooter(RectTransform footer, float w, out TextMeshProUGUI energyLabel,
        out Button startButton, out TextMeshProUGUI startLabel)
    {
        var energy = Text(footer, "EnergyLabel", "Costs 1 energy  ·  you have 20", 10.5f, MutedBrown,
            TextAlignmentOptions.Center);
        InCard(energy.rectTransform, CardW, H.Left, CardW / 2f, 14f, w, 16f);
        energyLabel = energy;

        // Centre sits 30pt (half its height) above a 14pt bottom margin, so the button stays
        // inside the card — it used to hang 24pt past the bottom edge, where the X now goes.
        var start = Make("PlayButton", footer, typeof(Image), typeof(Button));
        InCard(start, CardW, H.Left, CardW / 2f, FooterH - 14f - 30f, w, 60f);
        Sliced(start.GetComponent<Image>(), $"{Cards}/play_btn.png", 4f);
        startButton = start.GetComponent<Button>();

        var label = Text(start, "Label", "PLAY", 22f, Color.white, TextAlignmentOptions.Center);
        label.font = Lilita;
        Stretch(label.rectTransform);
        startLabel = label;
    }

    private static void BuildToast(RectTransform card, out CanvasGroup toast, out TextMeshProUGUI toastText)
    {
        var t = Make("Toast", card, typeof(CanvasGroup), typeof(Image));
        t.anchorMin = t.anchorMax = new Vector2(0.5f, 0f);
        t.pivot = new Vector2(0.5f, 0.5f);
        t.sizeDelta = new Vector2(CardW - 30f, 44f);
        t.anchoredPosition = new Vector2(0f, FooterH + 74f);
        Sliced(t.GetComponent<Image>(), $"{Cards}/pill_cream.png", 4f);
        t.GetComponent<Image>().raycastTarget = false;

        var label = Text(t, "Label", "Message", 13f, InkBlack, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);

        toast = t.GetComponent<CanvasGroup>();
        toast.alpha = 0f;
        toastText = label;
    }

    private static Sprite[] BuildPawnSprites()
    {
        return new[]
        {
            Sprite($"{Art}/pawn_red.png"),
            Sprite($"{Art}/pawn_green.png"),
            Sprite($"{Art}/pawn_yellow.png"),
            Sprite($"{Art}/pawn_blue.png"),
        };
    }

    // ======================================================================================
    // animated emoji import
    // ======================================================================================

    /// <summary>Frame count baked into each strip by the prep script — see scratchpad/prep script.</summary>
    private static readonly (string name, int frames)[] EmojiStrips =
    {
        ("emoji_grinning.png", 10),
        ("emoji_fire.png", 10),
        ("emoji_cool.png", 10),
        ("emoji_thinking.png", 10),
        ("emoji_clap.png", 10),
        ("emoji_party.png", 16),
        ("emoji_flex.png", 10),
        ("emoji_laughing.png", 10),
    };

    private static void ConfigureEmoji()
    {
        foreach (var (name, frameCount) in EmojiStrips)
        {
            string path = $"{EmojiArt}/{name}";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                Debug.LogError($"[Ludo NX] Missing emoji strip: {path}");
                continue;
            }

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Multiple) { importer.spriteImportMode = SpriteImportMode.Multiple; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }

#pragma warning disable CS0618 // SpriteMetaData/spritesheet: still the working way to grid-slice at import time.
            float cell = 128f;
            var metas = new SpriteMetaData[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                metas[i] = new SpriteMetaData
                {
                    name = $"{Path.GetFileNameWithoutExtension(name)}_{i}",
                    rect = new Rect(i * cell, 0f, cell, cell),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
            }
            importer.spritesheet = metas;
#pragma warning restore CS0618
            dirty = true;

            if (dirty) importer.SaveAndReimport();
        }
    }

    private static OnlineMatchModal.EmojiSet[] BuildEmojiSets()
    {
        var sets = new List<OnlineMatchModal.EmojiSet>();
        foreach (var (name, frameCount) in EmojiStrips)
        {
            string path = $"{EmojiArt}/{name}";
            var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            var frames = new List<Sprite>();
            foreach (var obj in sprites)
            {
                if (obj is Sprite s) frames.Add(s);
            }
            // Sprite names are "<file>_<index>" — sort by the numeric suffix, not the string, or
            // a 16-frame strip puts "_10".."_15" before "_2" (plain string order).
            frames.Sort((a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
            if (frames.Count != frameCount)
                Debug.LogWarning($"[Ludo NX] {name}: expected {frameCount} sliced sprites, found {frames.Count}.");
            sets.Add(new OnlineMatchModal.EmojiSet { frames = frames.ToArray() });
        }
        return sets.ToArray();
    }

    private static int FrameIndex(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(spriteName[(underscore + 1)..], out int i) ? i : 0;
    }

    // ======================================================================================
    // Rules.prefab — Rules / How to play popup
    // ======================================================================================

    private static void BuildRulesModal()
    {
        var root = Make("RulesModal", null, typeof(CanvasGroup), typeof(RulesModal));
        var modal = root.GetComponent<RulesModal>();
        Stretch(root);

        // Frame and Content
        var band = Band(root, "Frame", DesignBand.Edge.Fill, 0f);
        band.GetComponent<DesignBand>().referenceSize = Reference;

        var content = Make("Content", band);
        Stretch(content);

        // Scrim (ground.scrim #0B0D12 at 72%)
        var scrim = Make("Scrim", content, typeof(Image), typeof(Button));
        Stretch(scrim);
        scrim.offsetMin = new Vector2(-3000f, -3000f);
        scrim.offsetMax = new Vector2(3000f, 3000f);
        scrim.GetComponent<Image>().color = new Color(0.043f, 0.051f, 0.071f, ScrimAlpha);
        scrim.GetComponent<Button>().transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(scrim.GetComponent<Button>().onClick, modal.OnScrimTapped);

        // Card (380 wide, Lucky Six corner dice, cream rim)
        var card = Make("Card", content, typeof(Image));
        Centre(card, 0f, 16f, RulesCardW, RulesCardH);
        Sliced(card.GetComponent<Image>(), $"{Cards}/card_shell.png", 4f);
        card.GetComponent<Image>().raycastTarget = true;
        modal.card = card;

        // Nameplate straddling top edge
        var nameplate = Make("Nameplate", card, typeof(Image));
        nameplate.anchorMin = nameplate.anchorMax = new Vector2(0.5f, 1f);
        nameplate.pivot = new Vector2(0.5f, 0.5f);
        nameplate.sizeDelta = new Vector2(196f, 38f);
        nameplate.anchoredPosition = new Vector2(0f, 0f);
        var plateImg = nameplate.GetComponent<Image>();
        Sliced(plateImg, $"{Cards}/pill_cream.png", 4f);
        plateImg.color = new Color32(245, 235, 218, 255);

        var title = Text(nameplate, "Title", "HOW TO PLAY", 18f, InkBlack, TextAlignmentOptions.Center);
        title.font = Lilita;
        title.characterSpacing = 2f;
        Stretch(title.rectTransform);
        modal.titleLabel = title;

        // Folder Tabs (BASICS, MOVES, SCORING)
        string[] tabNames = { "BASICS", "MOVES", "SCORING" };
        modal.tabButtons = new Button[3];
        modal.tabBackgrounds = new Image[3];
        modal.tabLabels = new TextMeshProUGUI[3];
        modal.tabRects = new RectTransform[3];

        float totalTabsW = 340f;
        float tabW = (totalTabsW - 16f) / 3f;
        float startX = -totalTabsW / 2f + tabW / 2f;

        var tabsBar = Make("TabsBar", card, typeof(RectTransform));
        tabsBar.anchorMin = tabsBar.anchorMax = new Vector2(0.5f, 1f);
        tabsBar.pivot = new Vector2(0.5f, 1f);
        tabsBar.sizeDelta = new Vector2(totalTabsW, 44f);
        tabsBar.anchoredPosition = new Vector2(0f, -32f);

        for (int i = 0; i < 3; i++)
        {
            float cx = startX + i * (tabW + 8f);
            var tabObj = Make($"Tab_{tabNames[i]}", tabsBar, typeof(Image), typeof(Button));
            var tabRt = tabObj;
            tabRt.anchorMin = tabRt.anchorMax = new Vector2(0.5f, 1f);
            tabRt.pivot = new Vector2(0.5f, 1f);
            tabRt.sizeDelta = new Vector2(tabW, i == 0 ? 42f : 36f);
            tabRt.anchoredPosition = new Vector2(cx, 0f);

            var tabImg = tabObj.GetComponent<Image>();
            Sliced(tabImg, $"{Cards}/pill_cream.png", 4f);
            tabImg.color = i == 0 ? (Color)RulesModal.AmberSelected : (Color)RulesModal.CreamUnselected;

            var tabLbl = Text(tabObj, "Label", tabNames[i], 14f, i == 0 ? Color.white : (Color)MutedBrown, TextAlignmentOptions.Center);
            tabLbl.font = Lilita;
            tabLbl.characterSpacing = 1.5f;
            Stretch(tabLbl.rectTransform);

            modal.tabButtons[i] = tabObj.GetComponent<Button>();
            modal.tabBackgrounds[i] = tabImg;
            modal.tabLabels[i] = tabLbl;
            modal.tabRects[i] = tabRt;
        }

        // Paused turn caption (for mid-match state)
        var caption = Text(card, "PausedCaption", "Your turn is paused.", 12f, new Color32(212, 128, 16, 255), TextAlignmentOptions.Center);
        caption.font = Nunito;
        caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        caption.rectTransform.pivot = new Vector2(0.5f, 1f);
        caption.rectTransform.sizeDelta = new Vector2(RulesCardW - 40f, 18f);
        caption.rectTransform.anchoredPosition = new Vector2(0f, -76f);
        caption.gameObject.SetActive(false);
        modal.pausedCaption = caption;

        // Viewport and Scrolling content
        var viewport = Make("BodyViewport", card, typeof(Image), typeof(RectMask2D));
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(18f, 50f);
        viewport.offsetMax = new Vector2(-18f, -96f);
        Invisible(viewport.GetComponent<Image>());
        viewport.GetComponent<Image>().raycastTarget = true;

        var list = Make("ContentList", viewport, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        list.anchorMin = new Vector2(0f, 1f);
        list.anchorMax = new Vector2(1f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        var vlg = list.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(0, 0, 4, 12);
        list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = list;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.scrollSensitivity = 16f;

        modal.scrollRect = scroll;
        modal.contentContainer = list;

        // Rule Row Template (hidden, cloned at runtime)
        float rowW = RulesCardW - 36f;
        float rowH = 68f;
        var rowTpl = Make("RuleRowTemplate", viewport, typeof(LayoutElement), typeof(Image));
        rowTpl.sizeDelta = new Vector2(rowW, rowH);
        var le = rowTpl.GetComponent<LayoutElement>();
        le.preferredWidth = rowW;
        le.preferredHeight = rowH;
        var rowBg = rowTpl.GetComponent<Image>();
        Sliced(rowBg, $"{Cards}/pill_cream.png", 4f);
        rowBg.color = new Color32(250, 243, 232, 255);

        // Icon well (44x44)
        var iconWell = Make("IconWell", rowTpl, typeof(Image));
        iconWell.anchorMin = iconWell.anchorMax = new Vector2(0f, 0.5f);
        iconWell.pivot = new Vector2(0f, 0.5f);
        iconWell.sizeDelta = new Vector2(44f, 44f);
        iconWell.anchoredPosition = new Vector2(10f, 0f);
        var wellImg = iconWell.GetComponent<Image>();
        Sliced(wellImg, $"{Cards}/pill_cream.png", 4f);
        wellImg.color = new Color32(237, 226, 208, 255);

        var numText = Text(iconWell, "Number", "1", 20f, new Color32(194, 120, 20, 255), TextAlignmentOptions.Center);
        numText.font = Lilita;
        Stretch(numText.rectTransform);

        var iconImg = Picture(iconWell, "Icon", CircleSprite);
        Centre((RectTransform)iconImg.transform, 0f, 0f, 32f, 32f);
        iconImg.gameObject.SetActive(false);

        // Text Group
        var textGroup = Make("TextGroup", rowTpl, typeof(RectTransform));
        textGroup.anchorMin = new Vector2(0f, 0f);
        textGroup.anchorMax = new Vector2(1f, 1f);
        textGroup.offsetMin = new Vector2(62f, 6f);
        textGroup.offsetMax = new Vector2(-10f, -6f);

        var titleText = Text(textGroup, "Title", "Rule Title", 16f, InkBlack, TextAlignmentOptions.MidlineLeft);
        titleText.font = Lilita;
        titleText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.offsetMin = Vector2.zero;
        titleText.rectTransform.offsetMax = Vector2.zero;

        var bodyText = Text(textGroup, "Body", "Rule description goes here with wrapping.", 13f, MutedBrown, TextAlignmentOptions.TopLeft);
        bodyText.font = Nunito;
        bodyText.enableWordWrapping = true;
        bodyText.rectTransform.anchorMin = new Vector2(0f, 0f);
        bodyText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        bodyText.rectTransform.offsetMin = Vector2.zero;
        bodyText.rectTransform.offsetMax = Vector2.zero;

        rowTpl.gameObject.SetActive(false);
        modal.ruleRowPrefab = rowTpl.gameObject;

        // Pre-populate the initial BASICS rows so the prefab has content immediately
        for (int i = 0; i < RulesModal.BasicsRules.Length; i++)
        {
            var rowObj = Object.Instantiate(rowTpl.gameObject, list);
            rowObj.name = $"RuleRow_{i + 1}";
            rowObj.SetActive(true);
            var t = rowObj.transform.Find("TextGroup/Title")?.GetComponent<TextMeshProUGUI>();
            var b = rowObj.transform.Find("TextGroup/Body")?.GetComponent<TextMeshProUGUI>();
            var n = rowObj.transform.Find("IconWell/Number")?.GetComponent<TextMeshProUGUI>();
            if (t != null) t.text = RulesModal.BasicsRules[i].title;
            if (b != null) b.text = RulesModal.BasicsRules[i].body;
            if (n != null) n.text = (i + 1).ToString();
        }

        // Action Button in Cream Collar (straddles bottom edge)
        var collar = Make("Collar", card, typeof(Image));
        collar.anchorMin = collar.anchorMax = new Vector2(0.5f, 0f);
        collar.pivot = new Vector2(0.5f, 0.5f);
        collar.sizeDelta = new Vector2(212f, 58f);
        collar.anchoredPosition = new Vector2(0f, 0f); // straddles bottom edge
        var collarImg = collar.GetComponent<Image>();
        Sliced(collarImg, $"{Cards}/pill_cream.png", 4f);
        collarImg.color = new Color32(245, 235, 218, 255);

        var actionBtn = Make("GotItButton", collar, typeof(Image), typeof(Button));
        actionBtn.anchorMin = actionBtn.anchorMax = new Vector2(0.5f, 0.5f);
        actionBtn.pivot = new Vector2(0.5f, 0.5f);
        actionBtn.sizeDelta = new Vector2(200f, 48f);
        actionBtn.anchoredPosition = Vector2.zero;
        var btnImg = actionBtn.GetComponent<Image>();
        Sliced(btnImg, $"{Cards}/pill_cream.png", 4f);
        btnImg.color = new Color32(45, 168, 48, 255); // go green #2DA830

        var btnLbl = Text(actionBtn, "Label", "GOT IT", 20f, Color.white, TextAlignmentOptions.Center);
        btnLbl.font = Lilita;
        btnLbl.characterSpacing = 2f;
        Stretch(btnLbl.rectTransform);

        modal.gotItButton = actionBtn.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(modal.gotItButton.onClick, modal.Dismiss);

        // Close X button below card
        CloseButton(card, modal);
        modal.closeButton = card.Find("CloseButton")?.GetComponent<Button>();

        Directory.CreateDirectory(Path.GetDirectoryName(RulesPrefab));
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, RulesPrefab);
        Object.DestroyImmediate(root.gameObject);
    }

    [MenuItem("LudoNX/UI/Modals/Build Pause Modal")]
    public static void BuildPauseModal()
    {
        var root = Make("PauseModal", null, typeof(CanvasGroup), typeof(ConfirmModal));
        Stretch(root);
        var modal = root.GetComponent<ConfirmModal>();
        modal.dismissOnScrimTap = true;

        var band = Band(root, "Frame", DesignBand.Edge.Fill, 0f);
        band.GetComponent<DesignBand>().referenceSize = Reference;

        var content = Make("Content", band);
        Stretch(content);

        // Scrim (ground.scrim #0B0D12 at 72%)
        var scrim = Make("Scrim", content, typeof(Image), typeof(Button));
        Stretch(scrim);
        scrim.offsetMin = new Vector2(-3000f, -3000f);
        scrim.offsetMax = new Vector2(3000f, 3000f);
        scrim.GetComponent<Image>().color = new Color(0.043f, 0.051f, 0.071f, ScrimAlpha);
        var scrimBtn = scrim.GetComponent<Button>();
        scrimBtn.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(scrimBtn.onClick, modal.Dismiss);

        // Card (330 wide, 260 high, card_shell.png with Lucky Six corner dice)
        var card = Make("Card", content, typeof(Image));
        Centre(card, 0f, 10f, 330f, 260f);
        Sliced(card.GetComponent<Image>(), $"{Cards}/card_shell.png", 4f);
        card.GetComponent<Image>().raycastTarget = true;

        // Nameplate straddling top edge
        var nameplate = Make("Nameplate", card, typeof(Image));
        nameplate.anchorMin = nameplate.anchorMax = new Vector2(0.5f, 1f);
        nameplate.pivot = new Vector2(0.5f, 0.5f);
        nameplate.sizeDelta = new Vector2(160f, 36f);
        nameplate.anchoredPosition = new Vector2(0f, 0f);
        var plateImg = nameplate.GetComponent<Image>();
        Sliced(plateImg, $"{Cards}/pill_cream.png", 4f);
        plateImg.color = new Color32(245, 235, 218, 255);

        var title = Text(nameplate, "Title", "PAUSED", 18f, InkBlack, TextAlignmentOptions.Center);
        title.font = Lilita;
        title.characterSpacing = 2f;
        Stretch(title.rectTransform);
        modal.titleLabel = title;

        // Body message
        var body = Text(card, "Body", "The match is waiting for you.", 15f, new Color(0.25f, 0.22f, 0.20f, 0.9f), TextAlignmentOptions.Center);
        body.font = Nunito;
        body.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        body.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        body.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        body.rectTransform.sizeDelta = new Vector2(280f, 40f);
        body.rectTransform.anchoredPosition = new Vector2(0f, -55f);
        modal.bodyLabel = body;

        // 1. RESUME Button (Hero Green button, 240 x 52)
        var resumeObj = Make("ResumeButton", card, typeof(Image), typeof(Button));
        resumeObj.anchorMin = resumeObj.anchorMax = new Vector2(0.5f, 0.5f);
        resumeObj.pivot = new Vector2(0.5f, 0.5f);
        resumeObj.sizeDelta = new Vector2(240f, 52f);
        resumeObj.anchoredPosition = new Vector2(0f, -10f);
        var resumeImg = resumeObj.GetComponent<Image>();
        Sliced(resumeImg, $"{Cards}/play_btn.png", 2f);
        resumeImg.color = new Color32(40, 160, 50, 255); // vibrant emerald green

        var resumeLbl = Text(resumeObj, "Label", "RESUME", 20f, Color.white, TextAlignmentOptions.Center);
        resumeLbl.font = Lilita;
        resumeLbl.characterSpacing = 1.5f;
        Stretch(resumeLbl.rectTransform);

        modal.cancelButton = resumeObj.GetComponent<Button>();
        modal.cancelLabel = resumeLbl;

        // 2. QUIT MATCH Button (Quiet secondary action, 200 x 38)
        var quitObj = Make("QuitButton", card, typeof(Image), typeof(Button));
        quitObj.anchorMin = quitObj.anchorMax = new Vector2(0.5f, 0f);
        quitObj.pivot = new Vector2(0.5f, 0.5f);
        quitObj.sizeDelta = new Vector2(200f, 38f);
        quitObj.anchoredPosition = new Vector2(0f, 42f);
        var quitImg = quitObj.GetComponent<Image>();
        Sliced(quitImg, $"{Cards}/pill_cream.png", 4f);
        quitImg.color = new Color32(230, 220, 205, 200);

        var quitLbl = Text(quitObj, "Label", "QUIT MATCH", 14f, new Color32(180, 40, 40, 255), TextAlignmentOptions.Center);
        quitLbl.font = Lilita;
        quitLbl.characterSpacing = 1.2f;
        Stretch(quitLbl.rectTransform);

        modal.confirmButton = quitObj.GetComponent<Button>();
        modal.confirmLabel = quitLbl;

        string prefabPath = "Assets/Resources/UI/Modals/Pause.prefab";
        Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, prefabPath);
        Object.DestroyImmediate(root.gameObject);
        Debug.Log("[LudoNXMatchModalsBuilder] Rebuilt modern Pause.prefab on card_shell chassis.");
    }
}
