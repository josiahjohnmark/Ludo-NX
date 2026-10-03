using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Builds the LEVELS screen (Resources/UI/Screens/Levels.prefab) and the vault doors that lead
/// to it (Resources/UI/Components/DoorTransition.prefab).
///
/// Same 440 x 956 frame and furniture as Daily Missions / Friends — top bar, cream panel,
/// row cards, tan chips — so it reads as one game. The level badges are Profile/badge_01..10,
/// one per mission set.
///
/// KEY MEASUREMENTS (points, from the top of the frame)
///   header 102 · hero card 110–290 · badge 104 at (92,196) · level road 298–378, node pitch 74
///   missions panel 386 → nav reserve · row card 356 x 72 on a 78 pitch
/// </summary>
public static class LudoNXLevelsBuilder
{
    private const string MissionsArt = "Assets/Art/UI/Missions";
    private const string ProfileArt = "Assets/Art/UI/Profile";
    private const string SetupArt = "Assets/Art/UI/MatchSetup";
    private const string Shared = "Assets/Art/UI/Shared";
    private const string PrefabPath = "Assets/Resources/UI/Screens/Levels.prefab";
    private const string DoorsPath = "Assets/Resources/UI/Components/DoorTransition.prefab";
    private const string DoorTexture = "Assets/Art/UI/Backgrounds/bg_match_emboss.jpg";
    private const string Emblem = HomeArt + "/nav_levels.png";
    // nav_levels.png with its faint alpha haze cut to zero; Linear colour space showed the haze as a grey box on the dark doors.
    private const string DoorEmblem = "Assets/Art/UI/Levels/door_emblem.png";

    private const float HeaderH = 102f;
    private const float HeroTop = 110f, HeroBottom = 290f;
    private const float RoadCy = 338f, RoadH = 80f, NodePitch = 74f;
    private const float PanelTop = 386f;
    private const float RowW = 356f, RowH = 72f, RowPitch = 78f;

    [MenuItem("Ludo NX/UI/Build Levels Screen + Doors", priority = 4)]
    public static void Build()
    {
        EnsureSprites();
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        Directory.CreateDirectory(Path.GetDirectoryName(DoorsPath));

        var root = Make("Levels", null, typeof(CanvasGroup), typeof(LevelsScreen));
        Stretch(root);
        var screen = root.GetComponent<LevelsScreen>();
        screen.SetId(ScreenId.Levels);

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

        screen.badges = new Sprite[10];
        for (int i = 0; i < 10; i++) screen.badges[i] = Sprite($"{ProfileArt}/badge_{i + 1:00}.png");
        screen.iconDice = Sprite($"{MissionsArt}/icon_dice.png");
        screen.iconBadge = Sprite($"{MissionsArt}/icon_badge.png");
        screen.iconStat = Sprite($"{MissionsArt}/icon_stat.png");

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);

        BuildDoors();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Levels screen built -> {PrefabPath}; doors -> {DoorsPath}.");
    }

    // ======================================================================================
    // doors
    // ======================================================================================

    private static void BuildDoors()
    {
        var go = new GameObject("DoorTransition", typeof(RectTransform), typeof(DoorTransition));
        var doors = go.GetComponent<DoorTransition>();
        doors.doorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DoorTexture);
        doors.emblem = Sprite(DoorEmblem);
        doors.plate = Sprite("Assets/Resources/ui_circle.png");
        if (doors.doorTexture == null) Debug.LogError($"[Ludo NX] Missing door texture: {DoorTexture}");
        PrefabUtility.SaveAsPrefabAsset(go, DoorsPath);
        Object.DestroyImmediate(go);
    }

    // ======================================================================================
    // header
    // ======================================================================================

    private static void BuildHeader(RectTransform root, LevelsScreen screen)
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

        var icon = Picture(widgets, "TitleIcon", Emblem);
        icon.preserveAspect = true;
        Top((RectTransform)icon.transform, H.Centre, 160f, 74f, 42f, 40f);

        var title = Text(widgets, "Title", "LEVELS", 33f, Ink, TextAlignmentOptions.MidlineLeft);
        Top((RectTransform)title.transform, H.Centre, 186f + 110f, 74.75f, 220f, 48f);
    }

    // ======================================================================================
    // body
    // ======================================================================================

    private static void BuildBody(RectTransform root, LevelsScreen screen)
    {
        var band = Band(root, "BodyBand", DesignBand.Edge.TopFill, 0f);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.bottomReservePoints = NavReserve;
        bandComp.bottomReserveReference = new Vector2(402f, 874f);
        bandComp.safeAreaBaseline = SafeTop;

        var content = Make("BodyContent", band);
        Stretch(content);
        bandComp.safeAreaContent = content;

        BuildHero(content, screen);
        BuildRoad(content, screen);
        BuildMissions(content, screen);
    }

    private static void BuildHero(RectTransform parent, LevelsScreen screen)
    {
        float h = HeroBottom - HeroTop, cy = HeroTop + h / 2f;

        var card = Make("HeroCard", parent, typeof(Image));
        Top(card, H.Centre, 220f, cy, 392f, h);
        Sliced(card.GetComponent<Image>(), $"{FriendsArt}/panel_combined.png", 2f);
        card.GetComponent<Image>().raycastTarget = false;

        // Badge, on a slow-turning glow — the hero of the screen.
        var glow = Picture(card, "Glow", $"{SetupArt}/tile_glow.png");
        glow.color = new Color(1f, 0.86f, 0.4f, 0.85f);
        InCard((RectTransform)glow.transform, 392f, H.Left, 92f, h / 2f, 170f, 170f);
        screen.heroGlow = (RectTransform)glow.transform;

        var badgeRoot = Make("BadgeRoot", card);
        InCard(badgeRoot, 392f, H.Left, 92f, h / 2f, 104f, 104f);
        screen.heroBadgeRoot = badgeRoot;
        var badge = Picture(badgeRoot, "Badge", $"{ProfileArt}/badge_01.png");
        badge.preserveAspect = true;
        Stretch((RectTransform)badge.transform);
        screen.heroBadge = badge;

        const float L = 170f;
        var level = Text(card, "Level", "LEVEL 1", 30f, Brown, TextAlignmentOptions.MidlineLeft);
        LeftIn(level.rectTransform, L, 38f, 210f, 36f);
        screen.heroLevel = level;

        var name = Text(card, "SetName", "FIRST STEPS", 19f, Ink, TextAlignmentOptions.MidlineLeft);
        LeftIn(name.rectTransform, L, 68f, 210f, 26f);
        screen.heroName = name;

        var chip = Make("DifficultyChip", card, typeof(Image));
        LeftIn(chip, L, 96f, 128f, 24f);
        Sliced(chip.GetComponent<Image>(), $"{FriendsArt}/btn_blank_tan.png", 3f);
        chip.GetComponent<Image>().raycastTarget = false;
        var diff = Text(chip, "Label", "BEGINNER", 12f, Cream, TextAlignmentOptions.Center);
        Stretch(diff.rectTransform);
        diff.enableAutoSizing = true;
        diff.fontSizeMin = 8f;
        diff.fontSizeMax = 12f;
        screen.heroDifficulty = diff;

        var track = Picture(card, "Track", $"{MissionsArt}/progress_track.png");
        track.type = Image.Type.Sliced;
        track.pixelsPerUnitMultiplier = 2f;
        LeftIn((RectTransform)track.transform, L, 126f, 196f, 14f);
        var fill = Picture((RectTransform)track.transform, "Fill", $"{MissionsArt}/progress_fill.png");
        fill.type = Image.Type.Sliced;
        fill.pixelsPerUnitMultiplier = 2f;
        screen.heroFill = (RectTransform)fill.transform;

        var progress = Text(card, "Progress", "0 / 5 MISSIONS", 13f, SubInk, TextAlignmentOptions.MidlineLeft);
        LeftIn(progress.rectTransform, L, 146f, 210f, 20f);
        screen.heroProgress = progress;

        var reward = Text(card, "Reward", "Finish all 5 to reach LEVEL 2", 11f, Brown, TextAlignmentOptions.Center);
        InCard(reward.rectTransform, 392f, H.Left, 196f, h - 16f, 360f, 18f);
        reward.enableAutoSizing = true;
        reward.fontSizeMin = 8f;
        reward.fontSizeMax = 11f;
        screen.heroReward = reward;

        // LEVEL UP banner, hidden until a celebration plays.
        var banner = Make("LevelUpBanner", card, typeof(CanvasGroup), typeof(Image));
        InCard(banner, 392f, H.Left, 196f, h / 2f, 280f, 70f);
        Sliced(banner.GetComponent<Image>(), $"{FriendsArt}/btn_blank_red.png", 2f);
        banner.GetComponent<Image>().raycastTarget = false;
        var bannerText = Text(banner, "Label", "LEVEL UP!", 32f, Cream, TextAlignmentOptions.Center);
        Stretch(bannerText.rectTransform);
        screen.levelUpBanner = banner.GetComponent<CanvasGroup>();
        screen.levelUpBanner.blocksRaycasts = false;
        screen.levelUpText = bannerText;
    }

    private static void BuildRoad(RectTransform parent, LevelsScreen screen)
    {
        var scroll = Make("Road", parent, typeof(ScrollRect), typeof(RectMask2D), typeof(Image));
        Top(scroll, H.Centre, 220f, RoadCy, 408f, RoadH);
        var hit = scroll.GetComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0.001f);   // drag target for the whole strip

        var contentRt = Make("Content", scroll);
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(0f, 1f);
        contentRt.pivot = new Vector2(0f, 0.5f);
        contentRt.sizeDelta = new Vector2(NodePitch * 10 + 24f, 0f);
        contentRt.anchoredPosition = Vector2.zero;

        var sr = scroll.GetComponent<ScrollRect>();
        sr.content = contentRt;
        sr.viewport = scroll;
        sr.horizontal = true;
        sr.vertical = false;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.inertia = true;
        sr.decelerationRate = 0.12f;
        screen.roadScroll = sr;
        screen.roadContent = contentRt;
        screen.nodeSpacing = NodePitch;

        // Node template: link to the next node, current ring, badge, tick/lock, number.
        var node = Make("NodeTemplate", contentRt, typeof(Image), typeof(Button));
        node.anchorMin = node.anchorMax = new Vector2(0f, 0.5f);
        node.pivot = new Vector2(0.5f, 0.5f);
        node.sizeDelta = new Vector2(66f, RoadH);
        Invisible(node.GetComponent<Image>());
        node.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

        var link = Make("Link", node, typeof(Image));
        link.anchorMin = link.anchorMax = new Vector2(0.5f, 0.5f);
        link.pivot = new Vector2(0f, 0.5f);
        link.sizeDelta = new Vector2(NodePitch, 5f);
        link.anchoredPosition = new Vector2(0f, 8f);
        link.GetComponent<Image>().raycastTarget = false;

        var ring = Picture(node, "CurrentRing", $"{SetupArt}/tile_glow.png");
        ring.color = new Color(1f, 0.86f, 0.4f, 1f);
        Centre((RectTransform)ring.transform, 0f, 8f, 92f, 92f);

        var badge = Picture(node, "Badge", $"{ProfileArt}/badge_01.png");
        badge.preserveAspect = true;
        Centre((RectTransform)badge.transform, 0f, 8f, 54f, 54f);

        var tick = Picture(node, "Tick", $"{FriendsArt}/btn_accept.png");
        tick.preserveAspect = true;
        Centre((RectTransform)tick.transform, 20f, -10f, 22f, 22f);

        var lockIcon = Picture(node, "Lock", $"{SetupArt}/icon_lock.png");
        lockIcon.preserveAspect = true;
        Centre((RectTransform)lockIcon.transform, 0f, 8f, 22f, 22f);

        var number = Text(node, "Number", "1", 13f, Color.white, TextAlignmentOptions.Center);
        Centre(number.rectTransform, 0f, -29f, 50f, 18f);

        var selected = Make("SelectedMark", node, typeof(Image));
        Centre(selected, 0f, -39f, 26f, 3f);
        selected.GetComponent<Image>().color = Gold;
        selected.GetComponent<Image>().raycastTarget = false;

        screen.nodeTemplate = node;
    }

    private static void BuildMissions(RectTransform parent, LevelsScreen screen)
    {
        var panel = Make("MissionsPanel", parent, typeof(Image));
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.offsetMin = new Vector2(24f, 0f);
        panel.offsetMax = new Vector2(-24f, -PanelTop);
        Sliced(panel.GetComponent<Image>(), $"{FriendsArt}/panel_combined.png", 2f);
        panel.GetComponent<Image>().raycastTarget = false;

        var title = Text(panel, "PanelTitle", "LEVEL 1  ·  FIRST STEPS", 17f, Brown, TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        title.rectTransform.sizeDelta = new Vector2(360f, 26f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -26f);
        title.enableAutoSizing = true;
        title.fontSizeMin = 11f;
        title.fontSizeMax = 17f;
        screen.listTitle = title;

        var list = Make("List", panel);
        list.anchorMin = new Vector2(0.5f, 1f);
        list.anchorMax = new Vector2(0.5f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        list.sizeDelta = new Vector2(RowW, RowPitch * 5);
        list.anchoredPosition = new Vector2(0f, -46f);
        screen.listContent = list;
        screen.rowSpacing = RowPitch;

        screen.rowTemplate = BuildRow(list);
    }

    private static RectTransform BuildRow(RectTransform list)
    {
        var row = Make("RowTemplate", list, typeof(CanvasGroup));
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.sizeDelta = new Vector2(RowW, RowH);
        row.anchoredPosition = new Vector2(0f, -RowPitch / 2f);

        var card = Make("Card", row, typeof(Image));
        Stretch(card);
        Sliced(card.GetComponent<Image>(), $"{FriendsArt}/row_card.png", 2f);
        card.GetComponent<Image>().raycastTarget = false;

        var plate = Picture(row, "IconPlate", $"{MissionsArt}/icon_plate.png");
        InCard((RectTransform)plate.transform, RowW, H.Left, 38f, RowH / 2f, 52f, 52f);
        var icon = Picture(row, "Icon", $"{MissionsArt}/icon_dice.png");
        icon.preserveAspect = true;
        InCard((RectTransform)icon.transform, RowW, H.Left, 38f, RowH / 2f, 36f, 36f);

        var title = Text(row, "Title", "READY TO ROLL", 16f, Ink, TextAlignmentOptions.MidlineLeft);
        LeftIn(title.rectTransform, 74f, 17f, 210f, 22f);

        var desc = Text(row, "Desc", "Roll the dice 10 times.", 11.5f, SubInk, TextAlignmentOptions.TopLeft);
        LeftIn(desc.rectTransform, 74f, 40f, 240f, 26f);
        desc.textWrappingMode = TextWrappingModes.Normal;
        desc.enableAutoSizing = true;
        desc.fontSizeMin = 8.5f;
        desc.fontSizeMax = 11.5f;
        desc.lineSpacing = -8f;

        var track = Picture(row, "Track", $"{MissionsArt}/progress_track.png");
        track.type = Image.Type.Sliced;
        track.pixelsPerUnitMultiplier = 2f;
        LeftIn((RectTransform)track.transform, 74f, 60f, 186f, 9f);
        var fill = Picture((RectTransform)track.transform, "Fill", $"{MissionsArt}/progress_fill.png");
        fill.type = Image.Type.Sliced;
        fill.pixelsPerUnitMultiplier = 2f;

        var count = Text(row, "Count", "0 / 10", 12f, Brown, TextAlignmentOptions.MidlineRight);
        InCard(count.rectTransform, RowW, H.Right, 302f, 60f, 76f, 18f);

        var tick = Picture(row, "Tick", $"{FriendsArt}/btn_accept.png");
        tick.preserveAspect = true;
        InCard((RectTransform)tick.transform, RowW, H.Right, 316f, 24f, 30f, 30f);

        var shade = Make("LockShade", row, typeof(Image));
        Stretch(shade);
        shade.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f, 0.38f);
        shade.GetComponent<Image>().raycastTarget = false;
        var lockIcon = Picture(shade, "LockIcon", $"{SetupArt}/icon_lock.png");
        lockIcon.preserveAspect = true;
        InCard((RectTransform)lockIcon.transform, RowW, H.Right, 326f, RowH / 2f, 24f, 24f);

        return row;
    }

    // ======================================================================================

    /// <summary>Makes sure every file this builder uses is imported as a single sprite.</summary>
    private static void EnsureSprites()
    {
        var paths = new System.Collections.Generic.List<string> { Emblem, DoorEmblem, $"{SetupArt}/tile_glow.png", $"{SetupArt}/icon_lock.png" };
        for (int i = 1; i <= 10; i++) paths.Add($"{ProfileArt}/badge_{i:00}.png");
        foreach (var p in paths)
        {
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null) { Debug.LogError($"[Ludo NX] Missing art: {p}"); continue; }
            if (imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
    }

    // ======================================================================================
    // screen tests
    // ======================================================================================

    private const string TestMenu = "Ludo NX/UI/Screen Test/";

    [MenuItem(TestMenu + "Levels (through the doors)", priority = 66)]
    private static void TestLevels()
    {
        if (!Application.isPlaying || ScreenManager.Instance == null)
        {
            Debug.LogWarning("[Ludo NX] Enter play mode first — screens live on a runtime canvas.");
            return;
        }
        DoorTransition.Play(() => ScreenManager.Instance.Push(ScreenId.Levels));
    }

    [MenuItem(TestMenu + "Levels - Complete Next Mission", priority = 67)]
    private static void TestCompleteNext()
    {
        MissionService.DebugCompleteNext();
        Debug.Log($"[Ludo NX] Missions: level {MissionService.Level}, {MissionService.DoneCount}/5 done.");
    }

    [MenuItem(TestMenu + "Levels - Reset Progress", priority = 68)]
    private static void TestReset()
    {
        MissionService.DebugReset();
        Debug.Log("[Ludo NX] Missions reset to level 1.");
    }
}
