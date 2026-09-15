using System;
using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the Ludo NX Friends screen from the user's references in
/// 'real_freinds_screen/' (440 x 956 frame, exported at 2x).
///
/// Every coordinate is in points of that 440x956 frame, measured off the references. The
/// screen is made of DesignBands, so it scales uniformly and never stretches on one axis:
///   HeaderBand  (Top)     - wood panel, profile, friends icon, FRIENDS title
///   BodyBand    (TopFill) - tabs, search bar, panel; ends where the nav bar begins
///   PopupBand   (Fill)    - row menu, confirm dialog, messages
/// The nav bar itself is the shared UniversalNavBar prefab, laid out on the main-menu frame.
///
/// Decisions agreed with the user: search lives on the LIST tab (two tabs only); the selected
/// tab grows and glows while the other dims; every request has accept and decline; the row
/// menu offers Challenge (coming soon), Remove friend and Report.
///
/// KEY MEASUREMENTS (points)
///   header 102 tall · profile centre (40,69) · friends icon 37.5x34.6 centre (143.75,75)
///   FRIENDS Lilita One 39.3pt, ink x 173.5..321, cap centre y 74.75
///   tabs: LIST 94x41 centre (165.5,135) · REQUESTS 100x41 centre (271.5,135)
///   search bar 319x38 centre (219.5,190) · icon 20x21 centre x 82.5 · placeholder 14.9pt black 30%
///   panel 381 wide, top 225 (LIST) / 174 (REQUESTS), 11pt inner inset, title 19.9pt at top+32
///   rows 338x80 every 94, starting at panel top + 51 · number 15pt · name 24.7pt · subtitle 9.8pt
///   corner dice 22pt at 20/17 from the inner corners · watermark 318x375 at inner bottom-left (0.5,5)
/// </summary>
public static class LudoNXFriendsBuilder
{
    private const string FriendsArt = "Assets/Art/UI/Friends";
    private const string HomeArt = "Assets/Art/UI/HomeScreen";
    private const string PrefabPath = "Assets/Resources/UI/Screens/Friends.prefab";
    private const string LilitaPath = "Assets/Fonts/LilitaOne-Regular.asset";

    private static readonly Vector2 Reference = new Vector2(440f, 956f);
    private const float RefW = 440f;

    private const float HeaderH = 102f;
    private const float SafeTop = 45f;
    // Nav bar height minus the 7pt the panel tucks underneath it, in nav (402x874) points.
    private const float NavReserve = 116f - 7f;

    private const float ListPanelTop = 225f;
    private const float RequestsPanelTop = 174f;
    private const float PanelInset = 11f;
    private const float RowW = 338f;
    private const float RowH = 80f;
    private const float Slot = 94f;

    private static readonly Color Ink = new Color32(6, 5, 5, 255);
    private static readonly Color SubInk = new Color32(78, 74, 65, 255);
    private static readonly Color Brown = new Color32(75, 58, 42, 255);
    private static readonly Color Cream = new Color32(255, 242, 220, 255);
    private static readonly Color Slate = new Color32(40, 45, 56, 255);

    private static TMP_FontAsset _lilita;

    private enum H { Left, Right, Centre }

    [MenuItem("Ludo NX/UI/Build Friends Screen", priority = 2)]
    public static void Build()
    {
        _lilita = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LilitaPath);
        if (_lilita == null) Debug.LogError($"[Ludo NX] Lilita One font asset missing at {LilitaPath}.");

        ConfigureSprites();
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Make("Friends", null, typeof(CanvasGroup), typeof(FriendsScreen));
        Stretch(root);
        var screen = root.GetComponent<FriendsScreen>();
        screen.SetId(ScreenId.Friends);

        var bg = Make("Background", root, typeof(Image));
        Stretch(bg);
        bg.GetComponent<Image>().color = Slate;
        bg.GetComponent<Image>().raycastTarget = false;

        BuildBody(root, screen);
        BuildHeader(root, screen);
        BuildPopups(root, screen);

        screen.avatars = LoadAvatars();
        screen.onlineDot = Sprite($"{FriendsArt}/online_dot.png");
        screen.offlineDot = Sprite($"{FriendsArt}/offline_dot.png");
        screen.listPanelTop = ListPanelTop;
        screen.requestsPanelTop = RequestsPanelTop;

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Friends screen built from the {RefW}x{Reference.y} reference -> {PrefabPath}.");
    }

    // ======================================================================================
    // header
    // ======================================================================================

    private static void BuildHeader(RectTransform root, FriendsScreen screen)
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

        var profile = Make("ProfileButton", widgets, typeof(Image), typeof(Button));
        Top(profile, H.Left, 40f, 69f, 60f, 60f);
        Invisible(profile.GetComponent<Image>());
        Centred(profile, "Portrait", $"{HomeArt}/TopBar/profile_pics.png", 0f, -2f, 50f, 50f);
        Centred(profile, "Frame", $"{HomeArt}/TopBar/profile_frame.png", 0f, 0f, 60f, 60f);
        Centred(profile, "Crest", $"{HomeArt}/TopBar/ludo_nx_badge_for_profile_frame.png", 0f, 24f, 14f, 16f);
        screen.avatarButton = profile.GetComponent<Button>();

        var icon = Make("TitleIcon", widgets, typeof(Image));
        Top(icon, H.Centre, 143.75f, 75f, 37.5f, 34.6f);
        icon.GetComponent<Image>().sprite = Sprite($"{FriendsArt}/header_friends_icon.png");
        icon.GetComponent<Image>().raycastTarget = false;

        var title = Text(widgets, "Title", "FRIENDS", 39.3f, Ink, TextAlignmentOptions.MidlineLeft);
        Top((RectTransform)title.transform, H.Centre, 170.5f + 110f, 74.75f, 220f, 52f); // -3pt: text side-bearing
    }

    // ======================================================================================
    // body: tabs, search, panel
    // ======================================================================================

    private static void BuildBody(RectTransform root, FriendsScreen screen)
    {
        var band = Band(root, "BodyBand", DesignBand.Edge.TopFill, 0f);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.bottomReservePoints = NavReserve;
        bandComp.bottomReserveReference = new Vector2(402f, 874f);

        var body = Make("Body", band);
        Stretch(body);
        bandComp.safeAreaBaseline = SafeTop;
        bandComp.safeAreaContent = body;

        BuildTabs(body, screen);
        BuildSearch(body, screen);
        BuildPanel(body, screen);
    }

    private static void BuildTabs(RectTransform body, FriendsScreen screen)
    {
        var tabs = Make("Tabs", body);
        Stretch(tabs);

        // Glows first so both tabs always draw above them.
        screen.listTabGlow = Glow(tabs, "ListGlow", 165.5f, 118f);
        screen.requestsTabGlow = Glow(tabs, "RequestsGlow", 271.5f, 124f);

        screen.listTab = Tab(tabs, "ListTab", "tab_list.png", 165.5f, 94f, out screen.listTabImage);
        screen.requestsTab = Tab(tabs, "RequestsTab", "tab_requests.png", 271.5f, 100f, out screen.requestsTabImage);
    }

    private static Image Glow(RectTransform parent, string name, float cx, float w)
    {
        var rt = Make(name, parent, typeof(Image));
        Top(rt, H.Centre, cx, 135f, w, 65f);
        var img = rt.GetComponent<Image>();
        img.sprite = Sprite($"{FriendsArt}/tab_glow.png");
        img.raycastTarget = false;
        return img;
    }

    private static Button Tab(RectTransform parent, string name, string sprite, float cx, float w, out Image image)
    {
        var rt = Make(name, parent, typeof(Image), typeof(Button));
        Top(rt, H.Centre, cx, 135f, w, 41f);
        image = rt.GetComponent<Image>();
        image.sprite = Sprite($"{FriendsArt}/{sprite}");
        var button = rt.GetComponent<Button>();
        button.transition = Selectable.Transition.None; // the screen animates the look itself
        return button;
    }

    private static void BuildSearch(RectTransform body, FriendsScreen screen)
    {
        var bar = Make("Search", body, typeof(Image));
        bar.anchorMin = new Vector2(60f / RefW, 1f);
        bar.anchorMax = new Vector2(379f / RefW, 1f);
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.sizeDelta = new Vector2(0f, 38f);
        bar.anchoredPosition = new Vector2(0f, -190f);
        Sliced(bar.GetComponent<Image>(), $"{FriendsArt}/search_bar.png", 2f);
        bar.GetComponent<Image>().raycastTarget = false;
        screen.searchRoot = bar.gameObject;

        var icon = Make("Icon", bar, typeof(Image));
        icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = new Vector2(20f, 21f);
        icon.anchoredPosition = new Vector2(22.5f, 0.5f);
        icon.GetComponent<Image>().sprite = Sprite($"{FriendsArt}/search_icon.png");
        icon.GetComponent<Image>().raycastTarget = false;

        var inputRt = Make("Input", bar, typeof(Image), typeof(TMP_InputField));
        Stretch(inputRt);
        inputRt.offsetMin = new Vector2(40f, 0f);
        inputRt.offsetMax = new Vector2(-14f, 0f);
        Invisible(inputRt.GetComponent<Image>());

        var area = Make("TextArea", inputRt, typeof(RectMask2D));
        Stretch(area);

        var placeholder = Text(area, "Placeholder", "Search Player Name Or ID...", 14.9f,
            new Color32(60, 62, 69, 255), TextAlignmentOptions.MidlineLeft); // 30% black over the bar, pre-blended
        Stretch((RectTransform)placeholder.transform);

        var typed = Text(area, "Text", string.Empty, 14.9f, Cream, TextAlignmentOptions.MidlineLeft);
        Stretch((RectTransform)typed.transform);

        var input = inputRt.GetComponent<TMP_InputField>();
        input.textViewport = area;
        input.textComponent = typed;
        input.placeholder = placeholder;
        input.targetGraphic = inputRt.GetComponent<Image>();
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 24;
        input.caretColor = Cream;
        input.customCaretColor = true;
        input.selectionColor = new Color(1f, 0.9f, 0.6f, 0.35f);
        if (_lilita != null) input.fontAsset = _lilita;
        input.pointSize = 14.9f;
        screen.searchInput = input;
    }

    private static void BuildPanel(RectTransform body, FriendsScreen screen)
    {
        // Centred on the frame (the mock sits 4.5pt right of centre while its title is centred).
        var panel = Make("Panel", body);
        panel.anchorMin = new Vector2(29.5f / RefW, 0f);
        panel.anchorMax = new Vector2(410.5f / RefW, 1f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = new Vector2(0f, -ListPanelTop);
        screen.panel = panel;

        // The design's inner panel is 70% opaque tan over cream. This project renders in Linear
        // colour space, which blends see-through UI differently from Figma and came out lighter,
        // so the two layers are pre-blended (sRGB, as Figma does) into one solid image.
        var art = Make("Art", panel, typeof(Image));
        Stretch(art);
        Sliced(art.GetComponent<Image>(), $"{FriendsArt}/panel_combined.png", 2f);
        art.GetComponent<Image>().raycastTarget = false;

        var inner = Make("Inner", panel);
        Stretch(inner);
        inner.offsetMin = new Vector2(PanelInset, PanelInset);
        inner.offsetMax = new Vector2(-PanelInset, -PanelInset);

        var clip = Make("Clip", inner, typeof(RectMask2D));
        Stretch(clip);
        var watermark = Make("Watermark", clip, typeof(Image));
        watermark.anchorMin = watermark.anchorMax = watermark.pivot = Vector2.zero;
        watermark.sizeDelta = new Vector2(318f, 375f);
        watermark.anchoredPosition = new Vector2(0.5f, 5f);
        watermark.GetComponent<Image>().sprite = Sprite($"{FriendsArt}/panel_watermark_on_tan.png");
        watermark.GetComponent<Image>().raycastTarget = false;

        Dice(inner, "DiceTopLeft", new Vector2(0f, 1f), new Vector2(20f, -17f));
        Dice(inner, "DiceTopRight", new Vector2(1f, 1f), new Vector2(-20f, -17f));
        Dice(inner, "DiceBottomLeft", new Vector2(0f, 0f), new Vector2(20f, 17f));
        Dice(inner, "DiceBottomRight", new Vector2(1f, 0f), new Vector2(-20f, 17f));

        var title = Text(panel, "Title", "FRIEND LIST", 19.9f, Ink, TextAlignmentOptions.Midline);
        var titleRt = (RectTransform)title.transform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(-80f, 30f);
        titleRt.anchoredPosition = new Vector2(0f, -31.75f);
        screen.panelTitle = title;

        // Scrolling list, clipped where the reference clips its seventh row.
        var viewport = Make("Viewport", panel, typeof(Image), typeof(RectMask2D));
        Stretch(viewport);
        viewport.offsetMin = new Vector2(21.5f, 49.5f);
        viewport.offsetMax = new Vector2(-21.5f, -51f);
        Invisible(viewport.GetComponent<Image>());

        var content = Make("Content", viewport, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 0f;
        layout.padding = new RectOffset(0, 0, 0, 14);
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 30f;
        screen.scroll = scroll;
        screen.content = content;

        screen.rowTemplate = BuildRowTemplate(content);
        screen.sectionTemplate = BuildSection(content);
        screen.searchingTemplate = BuildSearching(content);
        screen.noteTemplate = BuildNote(content);

        var empty = Make("EmptyState", panel);
        Stretch(empty);
        empty.offsetMin = viewport.offsetMin;
        empty.offsetMax = viewport.offsetMax;
        var emptyTitle = Text(empty, "Title", "You have no friends yet", 18f, new Color32(114, 96, 73, 255), TextAlignmentOptions.Midline);
        CentreRect((RectTransform)emptyTitle.transform, 0f, 12f, 300f, 28f);
        var emptyHint = Text(empty, "Hint", "Search above to find players", 13f, new Color32(135, 113, 86, 255), TextAlignmentOptions.Midline);
        CentreRect((RectTransform)emptyHint.transform, 0f, -12f, 300f, 22f);
        empty.gameObject.SetActive(false);
        screen.emptyState = empty.gameObject;
        screen.emptyTitle = emptyTitle;
        screen.emptyHint = emptyHint;
    }

    private static void Dice(RectTransform inner, string name, Vector2 anchor, Vector2 position)
    {
        var rt = Make(name, inner, typeof(Image));
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(22f, 22f);
        rt.anchoredPosition = position;
        rt.GetComponent<Image>().sprite = Sprite($"{FriendsArt}/panel_dice_on_tan.png");
        rt.GetComponent<Image>().raycastTarget = false;
    }

    // ======================================================================================
    // row and list templates
    // ======================================================================================

    private static FriendRowView BuildRowTemplate(RectTransform content)
    {
        var slot = Make("Row", content, typeof(LayoutElement), typeof(CanvasGroup), typeof(FriendRowView));
        slot.GetComponent<LayoutElement>().preferredHeight = Slot;
        var view = slot.GetComponent<FriendRowView>();
        view.layout = slot.GetComponent<LayoutElement>();
        view.group = slot.GetComponent<CanvasGroup>();

        var card = Make("Card", slot, typeof(Image));
        card.anchorMin = new Vector2(0f, 1f);
        card.anchorMax = new Vector2(1f, 1f);
        card.pivot = new Vector2(0.5f, 1f);
        card.sizeDelta = new Vector2(0f, RowH);
        card.anchoredPosition = Vector2.zero;
        Sliced(card.GetComponent<Image>(), $"{FriendsArt}/row_card.png", 2f);
        view.card = card;

        view.number = Text(card, "Number", "1", 15f, Ink, TextAlignmentOptions.Midline);
        RowPlace((RectTransform)view.number.transform, H.Left, 15f, 39.5f, 26f, 22f);

        // Avatar: the main-menu profile stack scaled so its ring is 50pt across, as in the rows.
        var avatar = Make("Avatar", card, typeof(Image), typeof(Button));
        RowPlace(avatar, H.Left, 55f, 39f, 56f, 56f);
        Invisible(avatar.GetComponent<Image>());
        const float ring = 50f / 56f * 60f;         // frame art has a 56px ring in 60px
        float k = ring / 60f;
        var portrait = Centred(avatar, "Portrait", $"{FriendsArt}/Avatars/avatar_gen_00.png", 0f, -2f * k, 50f * k, 50f * k);
        view.portrait = portrait;
        Centred(avatar, "Frame", $"{HomeArt}/TopBar/profile_frame.png", 0f, 0f, ring, ring);
        Centred(avatar, "Crest", $"{HomeArt}/TopBar/ludo_nx_badge_for_profile_frame.png", 0f, 24f * k, 14f * k, 16f * k);
        view.avatarButton = avatar.GetComponent<Button>();

        var dot = Make("StatusDot", card, typeof(Image));
        RowPlace(dot, H.Left, 38.5f, 19.5f, 10f, 10f);
        view.statusDot = dot.GetComponent<Image>();
        view.statusDot.sprite = Sprite($"{FriendsArt}/online_dot.png");
        view.statusDot.raycastTarget = false;

        view.displayName = Text(card, "Name", "Josiah J.", 24.7f, Ink, TextAlignmentOptions.MidlineLeft);
        view.displayName.overflowMode = TextOverflowModes.Ellipsis;
        LeftText((RectTransform)view.displayName.transform, 94.5f, 34f, 212f, 30f);

        view.subtitle = Text(card, "Subtitle", "Friends: 7 Months Ago", 9.8f, SubInk, TextAlignmentOptions.MidlineLeft);
        view.subtitle.overflowMode = TextOverflowModes.Ellipsis;
        LeftText((RectTransform)view.subtitle.transform, 94.5f, 54f, 212f, 14f);

        var more = Make("More", card, typeof(Image), typeof(Button));
        RowPlace(more, H.Right, 318.5f, 39.5f, 36f, 56f);
        Invisible(more.GetComponent<Image>());
        Centred(more, "Dots", $"{FriendsArt}/more_dots.png", 0f, 0f, 5f, 19f);
        view.moreButton = more.GetComponent<Button>();

        view.acceptButton = IconButton(card, "Accept", "btn_accept.png", 235f, 40f, 41f);
        view.declineButton = IconButton(card, "Decline", "btn_decline.png", 289f, 40f, 42f);

        var spin = Make("Spinner", card, typeof(Image), typeof(UISpinner));
        RowPlace(spin, H.Right, 262f, 39.5f, 28f, 28f);
        spin.GetComponent<Image>().sprite = Sprite($"{FriendsArt}/spinner.png");
        spin.GetComponent<Image>().raycastTarget = false;
        spin.gameObject.SetActive(false);
        view.spinner = spin.gameObject;

        var add = Make("Add", card, typeof(Image), typeof(Button), typeof(CanvasGroup));
        RowPlace(add, H.Right, 278f, 39.5f, 72f, 34f);
        Sliced(add.GetComponent<Image>(), $"{FriendsArt}/btn_blank_tan.png", 2f);
        view.addButton = add.GetComponent<Button>();
        view.addGroup = add.GetComponent<CanvasGroup>();
        view.addLabel = Text(add, "Label", "ADD", 17f, Ink, TextAlignmentOptions.Midline);
        Stretch((RectTransform)view.addLabel.transform);
        add.gameObject.SetActive(false);

        view.acceptButton.gameObject.SetActive(false);
        view.declineButton.gameObject.SetActive(false);
        slot.gameObject.SetActive(false);
        return view;
    }

    private static Button IconButton(RectTransform card, string name, string sprite, float cx, float w, float h)
    {
        var rt = Make(name, card, typeof(Image), typeof(Button));
        RowPlace(rt, H.Right, cx, 39.5f, w, h);
        rt.GetComponent<Image>().sprite = Sprite($"{FriendsArt}/{sprite}");
        return rt.GetComponent<Button>();
    }

    private static RectTransform BuildSection(RectTransform content)
    {
        var slot = Make("Section", content, typeof(LayoutElement));
        slot.GetComponent<LayoutElement>().preferredHeight = 30f;
        var label = Text(slot, "Label", "YOUR FRIENDS", 13f, Brown, TextAlignmentOptions.MidlineLeft);
        var rt = (RectTransform)label.transform;
        Stretch(rt);
        rt.offsetMin = new Vector2(8f, 4f);
        slot.gameObject.SetActive(false);
        return slot;
    }

    private static RectTransform BuildSearching(RectTransform content)
    {
        var slot = Make("Searching", content, typeof(LayoutElement));
        slot.GetComponent<LayoutElement>().preferredHeight = 56f;
        var spin = Make("Spinner", slot, typeof(Image), typeof(UISpinner));
        CentreRect(spin, -52f, 0f, 22f, 22f);
        spin.GetComponent<Image>().sprite = Sprite($"{FriendsArt}/spinner.png");
        spin.GetComponent<Image>().raycastTarget = false;
        var label = Text(slot, "Label", "Searching...", 14f, SubInk, TextAlignmentOptions.MidlineLeft);
        CentreRect((RectTransform)label.transform, 30f, 0f, 120f, 24f);
        slot.gameObject.SetActive(false);
        return slot;
    }

    private static RectTransform BuildNote(RectTransform content)
    {
        var slot = Make("Note", content, typeof(LayoutElement));
        slot.GetComponent<LayoutElement>().preferredHeight = 44f;
        var label = Text(slot, "Label", "No other players found", 14f, new Color32(125, 104, 79, 255), TextAlignmentOptions.Midline);
        Stretch((RectTransform)label.transform);
        slot.gameObject.SetActive(false);
        return slot;
    }

    // ======================================================================================
    // popups: row menu, confirm dialog, message
    // ======================================================================================

    private static void BuildPopups(RectTransform root, FriendsScreen screen)
    {
        var band = Band(root, "PopupBand", DesignBand.Edge.Fill, Reference.y);

        // --- row menu ---
        var menu = Make("RowMenu", band);
        Stretch(menu);
        var blocker = Make("Blocker", menu, typeof(Image), typeof(Button));
        Stretch(blocker);
        blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
        screen.menuBlocker = blocker.GetComponent<Button>();

        var card = Make("Card", menu, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(1f, 1f);
        card.sizeDelta = new Vector2(196f, 150f);
        // The card is the same cream as the rows, so on its own it melted into the list. A soft
        // shadow and a thin brown outline lift it; all three layers sit outside the layout and
        // draw behind the items in this order.
        MenuLayer(card, "Shadow", "menu_shadow.png", 20f, raycast: false);
        MenuLayer(card, "Outline", "menu_outline.png", 2f, raycast: false);
        MenuLayer(card, "Fill", "row_card.png", 0f, raycast: true); // taps on padding stay in the menu
        var vlg = card.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 8, 8);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        card.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        screen.menuCard = card;
        screen.menuChallenge = MenuItem(card, "Challenge", "icon_challenge.png", "Challenge", Ink);
        screen.menuRemove = MenuItem(card, "Remove", "icon_remove_friend.png", "Remove friend", Ink);
        screen.menuReport = MenuItem(card, "Report", "icon_report.png", "Report", new Color32(170, 40, 32, 255));
        menu.gameObject.SetActive(false);
        screen.menuRoot = menu.gameObject;

        // --- confirm dialog ---
        var dialog = Make("Dialog", band);
        Stretch(dialog);
        var dim = Make("Dim", dialog, typeof(Image));
        Stretch(dim);
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var box = Make("Card", dialog);
        CentreRect(box, 0f, 0f, 300f, 200f);
        var boxArt = Make("Art", box, typeof(Image));
        Stretch(boxArt);
        Sliced(boxArt.GetComponent<Image>(), $"{FriendsArt}/panel_combined.png", 2f);

        screen.dialogTitle = Text(box, "Title", "Remove friend?", 24f, Ink, TextAlignmentOptions.Midline);
        TopRect((RectTransform)screen.dialogTitle.transform, 38f, 260f, 34f);
        screen.dialogMessage = Text(box, "Message", "Remove Josiah J. from your friends?", 15f, SubInk, TextAlignmentOptions.Midline);
        screen.dialogMessage.textWrappingMode = TextWrappingModes.Normal;
        TopRect((RectTransform)screen.dialogMessage.transform, 88f, 250f, 56f);

        var cancel = DialogButton(box, "Cancel", "btn_blank_tan.png", -66f, "CANCEL", Ink, out _);
        var confirm = DialogButton(box, "Confirm", "btn_blank_red.png", 66f, "REMOVE", Cream, out var confirmLabel);
        screen.dialogCancel = cancel;
        screen.dialogConfirm = confirm;
        screen.dialogConfirmLabel = confirmLabel;
        dialog.gameObject.SetActive(false);
        screen.dialogRoot = dialog.gameObject;

        // --- message ---
        var toast = Make("Message", band, typeof(Image), typeof(CanvasGroup));
        toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 0f);
        toast.pivot = new Vector2(0.5f, 0f);
        toast.sizeDelta = new Vector2(310f, 44f);
        toast.anchoredPosition = new Vector2(0f, 150f);
        Sliced(toast.GetComponent<Image>(), $"{FriendsArt}/search_bar.png", 2f);
        toast.GetComponent<Image>().raycastTarget = false;
        var toastText = Text(toast, "Text", "Friend request sent", 15f, Cream, TextAlignmentOptions.Midline);
        Stretch((RectTransform)toastText.transform);
        ((RectTransform)toastText.transform).offsetMin = new Vector2(16f, 0f);
        ((RectTransform)toastText.transform).offsetMax = new Vector2(-16f, 0f);
        screen.toast = toast.GetComponent<CanvasGroup>();
        screen.toastText = toastText;
        screen.toast.alpha = 0f;
    }

    private static void MenuLayer(RectTransform card, string name, string sprite, float grow, bool raycast)
    {
        var rt = Make(name, card, typeof(Image), typeof(LayoutElement));
        rt.GetComponent<LayoutElement>().ignoreLayout = true;
        Stretch(rt);
        rt.offsetMin = new Vector2(-grow, -grow);
        rt.offsetMax = new Vector2(grow, grow);
        Sliced(rt.GetComponent<Image>(), $"{FriendsArt}/{sprite}", 2f);
        rt.GetComponent<Image>().raycastTarget = raycast;
    }

    private static Button MenuItem(RectTransform card, string name, string icon, string label, Color color)
    {
        var item = Make(name, card, typeof(Image), typeof(Button), typeof(LayoutElement));
        item.GetComponent<LayoutElement>().preferredHeight = 44f;
        Invisible(item.GetComponent<Image>());

        var ic = Make("Icon", item, typeof(Image));
        ic.anchorMin = ic.anchorMax = new Vector2(0f, 0.5f);
        ic.pivot = new Vector2(0.5f, 0.5f);
        ic.sizeDelta = new Vector2(22f, 22f);
        ic.anchoredPosition = new Vector2(24f, 0f);
        ic.GetComponent<Image>().sprite = Sprite($"{FriendsArt}/{icon}");
        ic.GetComponent<Image>().raycastTarget = false;

        var text = Text(item, "Label", label, 17f, color, TextAlignmentOptions.MidlineLeft);
        var rt = (RectTransform)text.transform;
        Stretch(rt);
        rt.offsetMin = new Vector2(46f, 0f);
        rt.offsetMax = new Vector2(-8f, 0f);
        return item.GetComponent<Button>();
    }

    private static Button DialogButton(RectTransform box, string name, string sprite, float dx, string label,
        Color labelColor, out TextMeshProUGUI text)
    {
        var rt = Make(name, box, typeof(Image), typeof(Button));
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(112f, 44f);
        rt.anchoredPosition = new Vector2(dx, -158f);
        Sliced(rt.GetComponent<Image>(), $"{FriendsArt}/{sprite}", 2f);
        text = Text(rt, "Label", label, 18f, labelColor, TextAlignmentOptions.Midline);
        Stretch((RectTransform)text.transform);
        return rt.GetComponent<Button>();
    }

    // ======================================================================================
    // layout helpers - design points of the 440x956 frame
    // ======================================================================================

    private static RectTransform Band(RectTransform parent, string name, DesignBand.Edge edge, float height)
    {
        var rt = Make(name, parent, typeof(DesignBand));
        var band = rt.GetComponent<DesignBand>();
        band.referenceSize = Reference;
        band.edge = edge;
        band.designHeight = height;
        return rt;
    }

    /// <summary>Top-anchored, placed by centre; x is across the 440pt frame.</summary>
    private static void Top(RectTransform rt, H h, float cx, float cy, float w, float hgt)
    {
        float ax = h == H.Left ? 0f : h == H.Right ? 1f : 0.5f;
        float px = h == H.Left ? cx : h == H.Right ? cx - RefW : cx - RefW / 2f;
        rt.anchorMin = rt.anchorMax = new Vector2(ax, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, hgt);
        rt.anchoredPosition = new Vector2(px, -cy);
    }

    /// <summary>Inside an 80pt row card; x measured from the card's left edge (338pt wide).</summary>
    private static void RowPlace(RectTransform rt, H h, float cx, float cy, float w, float hgt)
    {
        float ax = h == H.Right ? 1f : 0f;
        float px = h == H.Right ? cx - RowW : cx;
        rt.anchorMin = rt.anchorMax = new Vector2(ax, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, hgt);
        rt.anchoredPosition = new Vector2(px, -cy);
    }

    /// <summary>Left-aligned text rect starting at <paramref name="left"/>, vertically centred on cy.</summary>
    private static void LeftText(RectTransform rt, float left, float cy, float w, float hgt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(w, hgt);
        rt.anchoredPosition = new Vector2(left, -cy);
    }

    private static void CentreRect(RectTransform rt, float dx, float dy, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(dx, dy);
    }

    private static void TopRect(RectTransform rt, float cy, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0f, -cy);
    }

    private static Image Centred(RectTransform parent, string name, string sprite, float dx, float dy, float w, float h)
    {
        var rt = Make(name, parent, typeof(Image));
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(dx, -dy);
        var img = rt.GetComponent<Image>();
        img.sprite = Sprite(sprite);
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI Text(RectTransform parent, string name, string text, float size,
        Color color, TextAlignmentOptions align)
    {
        var rt = Make(name, parent, typeof(TextMeshProUGUI));
        var tmp = rt.GetComponent<TextMeshProUGUI>();
        if (_lilita != null) tmp.font = _lilita;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static RectTransform Make(string name, Transform parent, params Type[] comps)
    {
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

    private static void Sliced(Image img, string sprite, float pixelsPerPoint)
    {
        img.sprite = Sprite(sprite);
        img.type = Image.Type.Sliced;
        img.fillCenter = true;
        img.pixelsPerUnitMultiplier = pixelsPerPoint;
    }

    private static void Invisible(Image img)
    {
        img.sprite = null;
        img.color = new Color(1f, 1f, 1f, 0f);
    }

    // ======================================================================================
    // assets
    // ======================================================================================

    private static Sprite Sprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogError($"[Ludo NX] Missing sprite: {path}");
        return sprite;
    }

    private static Sprite[] LoadAvatars()
    {
        var list = new List<Sprite>();
        for (int i = 0; i < 64; i++)
        {
            string path = $"{FriendsArt}/Avatars/avatar_gen_{i:00}.png";
            if (!File.Exists(path)) break;
            list.Add(Sprite(path));
        }
        return list.ToArray();
    }

    /// <summary>9-slice borders (left, bottom, right, top) in sprite pixels for the 2x Friends art.</summary>
    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "panel_combined.png", new Vector4(40, 40, 40, 40) },
        { "menu_shadow.png", new Vector4(80, 80, 80, 80) },
        { "menu_outline.png", new Vector4(28, 28, 28, 28) },
        { "panel_outer.png", new Vector4(20, 20, 20, 20) },
        { "panel_inner.png", new Vector4(24, 24, 24, 24) },
        { "row_card.png", new Vector4(28, 28, 28, 28) },
        { "search_bar.png", new Vector4(38, 0, 38, 0) },
        { "btn_blank_tan.png", new Vector4(20, 20, 20, 20) },
        { "btn_blank_red.png", new Vector4(20, 20, 20, 20) },
    };

    private static void ConfigureSprites()
    {
        int changed = 0;
        foreach (string file in Directory.GetFiles(FriendsArt, "*.png", SearchOption.AllDirectories))
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

            if (Borders.TryGetValue(Path.GetFileName(asset), out Vector4 border) && importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                dirty = true;
            }

            if (dirty) { importer.SaveAndReimport(); changed++; }
        }
        if (changed > 0) Debug.Log($"[Ludo NX] Configured {changed} Friends sprite(s).");
    }

    // ======================================================================================
    // play-mode test helpers
    // ======================================================================================

    /// <summary>Taps the real FRIENDS button on the bottom nav, exactly as a player would.</summary>
    [MenuItem("Ludo NX/UI/Friends Test/Tap nav FRIENDS button", priority = 49)]
    public static void TapNavFriends()
    {
        var nav = UnityEngine.Object.FindAnyObjectByType<HomeNavBar>();
        var item = nav != null ? nav.items.Find(i => i != null && i.tab == HomeNavBar.Tab.Friends) : null;
        if (!Application.isPlaying || item == null || item.button == null)
        {
            Debug.LogWarning("[Ludo NX] No bottom nav FRIENDS button found - enter play mode first.");
            return;
        }
        item.button.onClick.Invoke();
    }

    [MenuItem("Ludo NX/UI/Friends Test/Open Friends", priority = 50)]
    public static void OpenFriends()
    {
        if (!Application.isPlaying || ScreenManager.Instance == null)
        {
            Debug.LogWarning("[Ludo NX] Enter play mode first.");
            return;
        }
        ScreenManager.Instance.Push(ScreenId.Friends);
    }

    [MenuItem("Ludo NX/UI/Friends Test/Show LIST tab", priority = 51)]
    public static void ShowList() => WithScreen(s => s.SelectTab(FriendsScreen.Tab.List, true));

    [MenuItem("Ludo NX/UI/Friends Test/Show REQUESTS tab", priority = 52)]
    public static void ShowRequests() => WithScreen(s => s.SelectTab(FriendsScreen.Tab.Requests, true));

    [MenuItem("Ludo NX/UI/Friends Test/Search 'jo'", priority = 53)]
    public static void SearchJo() => WithScreen(s => { s.SelectTab(FriendsScreen.Tab.List, false); s.searchInput.text = "jo"; });

    [MenuItem("Ludo NX/UI/Friends Test/Search 'zzz'", priority = 54)]
    public static void SearchNone() => WithScreen(s => { s.SelectTab(FriendsScreen.Tab.List, false); s.searchInput.text = "zzz"; });

    [MenuItem("Ludo NX/UI/Friends Test/Clear search", priority = 55)]
    public static void SearchClear() => WithScreen(s => s.searchInput.text = string.Empty);

    [MenuItem("Ludo NX/UI/Friends Test/Scroll to bottom", priority = 56)]
    public static void ScrollBottom() => WithScreen(s => s.scroll.verticalNormalizedPosition = 0f);

    [MenuItem("Ludo NX/UI/Friends Test/Open menu on first friend", priority = 57)]
    public static void MenuFirst() => WithScreen(s =>
    {
        var row = s.content.GetComponentInChildren<FriendRowView>(false);
        if (row != null) row.moreButton.onClick.Invoke();
    });

    [MenuItem("Ludo NX/UI/Friends Test/Accept first request", priority = 58)]
    public static void AcceptFirst() => WithScreen(s =>
    {
        var row = s.content.GetComponentInChildren<FriendRowView>(false);
        if (row != null && row.acceptButton.gameObject.activeSelf) row.acceptButton.onClick.Invoke();
    });

    [MenuItem("Ludo NX/UI/Friends Test/Remove dialog on first friend", priority = 59)]
    public static void RemoveDialogFirst() => WithScreen(s =>
    {
        var row = s.content.GetComponentInChildren<FriendRowView>(false);
        if (row == null) return;
        row.moreButton.onClick.Invoke();
        s.menuRemove.onClick.Invoke();
    });

    [MenuItem("Ludo NX/UI/Friends Test/Tap ADD on first player", priority = 60)]
    public static void AddFirstPlayer() => WithScreen(s =>
    {
        foreach (var row in s.content.GetComponentsInChildren<FriendRowView>(false))
        {
            if (row.addButton.gameObject.activeSelf) { row.addButton.onClick.Invoke(); return; }
        }
    });

    [MenuItem("Ludo NX/UI/Friends Test/Close popups", priority = 61)]
    public static void ClosePopups() => WithScreen(s => { s.OnBack(); s.OnBack(); });

    private static void WithScreen(Action<FriendsScreen> action)
    {
        var screen = UnityEngine.Object.FindAnyObjectByType<FriendsScreen>();
        if (!Application.isPlaying || screen == null)
        {
            Debug.LogWarning("[Ludo NX] Open the Friends screen in play mode first.");
            return;
        }
        action(screen);
    }
}
