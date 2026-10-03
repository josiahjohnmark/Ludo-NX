using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Builds the Daily Missions screen into Resources/UI/Screens/DailyMissions.prefab.
///
/// Same 440 x 956 frame and the same furniture as the Friends screen — wood panel, cream row
/// cards, tan buttons — so the two read as one game. The nav bar stays visible: missions are a
/// hub destination reached from the home badge, not a dead end.
///
/// KEY MEASUREMENTS (points)
///   header 102 · back button 44 at (30,69) · icon 34 at (150,75) · MISSIONS 33pt from x 178
///   reset line at cy 132 · panel top 160, 381 wide, 11pt inset, title 19.9pt at top+32
///   mission card 338 x 104 in a 118 slot · icon plate 62 at (46,52) · title 18pt at left 86
///   progress track 150x12 at left 86 cy 58 · CLAIM 84x36 centred x 284
/// </summary>
public static class LudoNXMissionsBuilder
{
    private const string Art = "Assets/Art/UI/Missions";
    private const string Shared = "Assets/Art/UI/Shared";
    private const string PrefabPath = "Assets/Resources/UI/Screens/DailyMissions.prefab";

    private const float HeaderH = 102f;
    private const float PanelTop = 160f;
    private const float PanelInset = 11f;
    private const float CardW = 338f;
    private const float CardH = 104f;

    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "progress_track.png", new Vector4(14, 14, 14, 14) },
        { "progress_fill.png", new Vector4(14, 14, 14, 14) },
        { "btn_blank_grey.png", new Vector4(20, 20, 20, 20) },
    };

    [MenuItem("Ludo NX/UI/Build Daily Missions Screen", priority = 3)]
    public static void Build()
    {
        ConfigureSprites(Art, Borders);
        ConfigureSprites(Shared, null);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Make("DailyMissions", null, typeof(CanvasGroup), typeof(DailyMissionsScreen));
        Stretch(root);
        var screen = root.GetComponent<DailyMissionsScreen>();
        screen.SetId(ScreenId.DailyMissions);

        var bg = Make("Background", root, typeof(Image));
        Stretch(bg);
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Backgrounds/bg_match_emboss.jpg");
        bgImg.color = Color.white;
        bgImg.raycastTarget = false;
        var cover = bg.gameObject.AddComponent<BackgroundCoverFitter>();
        cover.overscan = 1.05f;

        BuildBody(root, screen);
        BuildHeader(root, screen);
        BuildToast(root, screen);

        screen.claimEnabled = Sprite($"{FriendsArt}/btn_blank_tan.png");
        screen.claimDisabled = Sprite($"{Art}/btn_blank_grey.png");
        screen.icons = new[]
        {
            Sprite($"{Art}/icon_dice.png"),
            Sprite($"{Art}/icon_badge.png"),
            Sprite($"{Art}/icon_stat.png"),
        };
        screen.iconNames = new[] { "icon_dice", "icon_badge", "icon_stat" };

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Daily Missions screen built -> {PrefabPath}.");
    }

    // ======================================================================================
    // header
    // ======================================================================================

    private static void BuildHeader(RectTransform root, DailyMissionsScreen screen)
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

        var icon = Picture(widgets, "TitleIcon", $"{Art}/icon_badge.png");
        Top((RectTransform)icon.transform, H.Centre, 152f, 74f, 38f, 38f);

        var title = Text(widgets, "Title", "MISSIONS", 33f, Ink, TextAlignmentOptions.MidlineLeft);
        Top((RectTransform)title.transform, H.Centre, 178f + 110f, 74.75f, 220f, 48f);
    }

    // ======================================================================================
    // body
    // ======================================================================================

    private static void BuildBody(RectTransform root, DailyMissionsScreen screen)
    {
        var band = Band(root, "BodyBand", DesignBand.Edge.TopFill, 0f);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.bottomReservePoints = NavReserve;
        bandComp.bottomReserveReference = new Vector2(402f, 874f);
        bandComp.safeAreaBaseline = SafeTop;

        var content = Make("BodyContent", band);
        Stretch(content);
        bandComp.safeAreaContent = content;

        // Reset countdown, centred above the panel.
        var clock = Picture(content, "ClockIcon", $"{Art}/icon_clock.png");
        Top((RectTransform)clock.transform, H.Centre, 150f, 132f, 22f, 22f);

        var reset = Text(content, "ResetLabel", "New missions in 6h 40m", 15f, Cream, TextAlignmentOptions.MidlineLeft);
        Top((RectTransform)reset.transform, H.Centre, 168f + 110f, 132f, 220f, 26f);
        screen.resetLabel = reset;

        BuildPanel(content, screen);
    }

    private static void BuildPanel(RectTransform parent, DailyMissionsScreen screen)
    {
        var panel = Make("Panel", parent, typeof(Image));
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

        Dice(inner, "DiceTL", new Vector2(0f, 1f), new Vector2(20f, -17f));
        Dice(inner, "DiceTR", new Vector2(1f, 1f), new Vector2(-20f, -17f));
        Dice(inner, "DiceBL", new Vector2(0f, 0f), new Vector2(20f, 17f));
        Dice(inner, "DiceBR", new Vector2(1f, 0f), new Vector2(-20f, 17f));

        var title = Text(inner, "PanelTitle", "DAILY MISSIONS", 19.9f, Brown, TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        title.rectTransform.sizeDelta = new Vector2(300f, 30f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -31.75f);
        screen.panelTitle = title;

        var viewport = Make("Viewport", inner, typeof(Image), typeof(RectMask2D));
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(10.5f, 14f);
        viewport.offsetMax = new Vector2(-10.5f, -51f);
        Invisible(viewport.GetComponent<Image>());
        viewport.GetComponent<Image>().raycastTarget = true;

        var list = Make("Content", viewport, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        list.anchorMin = new Vector2(0f, 1f);
        list.anchorMax = new Vector2(1f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        list.offsetMin = new Vector2(0f, 0f);
        list.offsetMax = new Vector2(0f, 0f);

        var vlg = list.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.spacing = 14f;
        vlg.padding = new RectOffset(0, 0, 6, 12);

        var fitter = list.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = list;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.scrollSensitivity = 18f;
        screen.scroll = scroll;
        screen.content = list;

        screen.rowTemplate = BuildRow(list, "MissionTemplate", $"{Art}/icon_dice.png", false);
        screen.bonusTemplate = BuildRow(list, "BonusTemplate", $"{Art}/chest.png", true);

        // Closes the list instead of leaving the panel trailing off into empty wood.
        var hint = Text(list, "Hint", "Three new missions every day at midnight", 12f,
            new Color32(120, 96, 66, 200), TextAlignmentOptions.Center);
        var hintElement = hint.gameObject.AddComponent<LayoutElement>();
        hintElement.preferredWidth = CardW;
        hintElement.preferredHeight = 28f;
        hint.rectTransform.sizeDelta = new Vector2(CardW, 28f);
    }

    private static void Dice(RectTransform inner, string name, Vector2 anchor, Vector2 position)
    {
        var img = Picture(inner, name, $"{FriendsArt}/panel_dice_on_tan.png");
        var rt = (RectTransform)img.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(anchor.x, anchor.y);
        rt.sizeDelta = new Vector2(22f, 22f);
        rt.anchoredPosition = position;
    }

    // ======================================================================================
    // row template
    // ======================================================================================

    private static MissionRowView BuildRow(RectTransform parent, string name, string iconPath, bool bonus)
    {
        var slot = Make(name, parent, typeof(LayoutElement), typeof(MissionRowView));
        slot.sizeDelta = new Vector2(CardW, CardH);
        var element = slot.GetComponent<LayoutElement>();
        element.preferredWidth = CardW;
        element.preferredHeight = CardH;

        var view = slot.GetComponent<MissionRowView>();
        view.layout = slot;
        view.baseTint = bonus ? new Color(1f, 0.955f, 0.84f) : Color.white;

        var card = Make("Card", slot, typeof(Image));
        Stretch(card);
        Sliced(card.GetComponent<Image>(), $"{FriendsArt}/row_card.png", 2f);
        card.GetComponent<Image>().raycastTarget = false;
        view.card = card.GetComponent<Image>();

        var plate = Picture(slot, "IconPlate", $"{Art}/icon_plate.png");
        InCard((RectTransform)plate.transform, CardW, H.Left, 46f, 52f, 62f, 62f);
        view.iconPlate = plate;

        var icon = Picture(slot, "Icon", iconPath);
        InCard((RectTransform)icon.transform, CardW, H.Left, 46f, bonus ? 50f : 51f, bonus ? 52f : 44f, bonus ? 52f : 44f);
        view.icon = icon;

        var title = Text(slot, "Title", bonus ? "Finish all 3 missions" : "Play 3 matches", 18f, Ink,
            TextAlignmentOptions.MidlineLeft);
        LeftIn(title.rectTransform, 88f, 28f, 200f, 26f);
        view.title = title;

        var track = Picture(slot, "ProgressTrack", $"{Art}/progress_track.png");
        track.type = Image.Type.Sliced;
        track.pixelsPerUnitMultiplier = 2f;
        LeftIn((RectTransform)track.transform, 88f, 58f, 120f, 12f);
        view.progressTrack = (RectTransform)track.transform;

        var fill = Picture((RectTransform)track.transform, "ProgressFill", $"{Art}/progress_fill.png");
        fill.type = Image.Type.Sliced;
        fill.pixelsPerUnitMultiplier = 2f;
        var fillRt = (RectTransform)fill.transform;
        fillRt.anchorMin = new Vector2(0f, 0.5f);
        fillRt.anchorMax = new Vector2(0f, 0.5f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.sizeDelta = new Vector2(60f, 12f);
        fillRt.anchoredPosition = Vector2.zero;
        view.progressFill = fill;

        // Sits between the bar and the button, with clearance on both sides — at 0/10 or 12/12
        // this label is the only place the player reads the actual numbers.
        var progressLabel = Text(slot, "ProgressLabel", "1/3", 13f, SubInk, TextAlignmentOptions.MidlineLeft);
        LeftIn(progressLabel.rectTransform, 214f, 58f, 44f, 22f);
        view.progressLabel = progressLabel;

        var reward = Text(slot, "RewardLabel", "+90   +10 NX", 13f, Brown, TextAlignmentOptions.MidlineLeft);
        LeftIn(reward.rectTransform, 88f, 82f, 200f, 22f);
        view.rewardLabel = reward;

        var claim = Make("ClaimButton", slot, typeof(Image), typeof(Button));
        InCard(claim, CardW, H.Right, 298f, 52f, 72f, 36f);
        var claimImg = claim.GetComponent<Image>();
        Sliced(claimImg, $"{FriendsArt}/btn_blank_tan.png", 2f);
        view.claimButton = claim.GetComponent<Button>();
        view.claimBackground = claimImg;

        var claimLabel = Text(claim, "Label", "CLAIM", 16f, Cream, TextAlignmentOptions.Center);
        Stretch(claimLabel.rectTransform);
        claimLabel.rectTransform.offsetMin = new Vector2(0f, 2f);
        view.claimLabel = claimLabel;

        var tick = Picture(slot, "Tick", $"{FriendsArt}/btn_accept.png");
        InCard((RectTransform)tick.transform, CardW, H.Right, 322f, 20f, 18f, 18f);
        tick.gameObject.SetActive(false);
        view.tick = tick;

        slot.gameObject.SetActive(false);
        return view;
    }

    // ======================================================================================
    // toast
    // ======================================================================================

    private static void BuildToast(RectTransform root, DailyMissionsScreen screen)
    {
        var band = Band(root, "PopupBand", DesignBand.Edge.Fill, 0f);

        var toast = Make("Message", band, typeof(CanvasGroup), typeof(Image));
        toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 0f);
        toast.pivot = new Vector2(0.5f, 0.5f);
        toast.sizeDelta = new Vector2(320f, 52f);
        toast.anchoredPosition = new Vector2(0f, 190f);
        Sliced(toast.GetComponent<Image>(), $"{FriendsArt}/btn_blank_tan.png", 2f);
        toast.GetComponent<Image>().raycastTarget = false;

        var label = Text(toast, "Label", "+90 coins   +10 NX", 15f, Cream, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);

        screen.toast = toast.GetComponent<CanvasGroup>();
        screen.toastText = label;
    }
}
