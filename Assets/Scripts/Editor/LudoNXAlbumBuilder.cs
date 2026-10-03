using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Builds the ALBUM screen into Resources/UI/Screens/PawnCollection.prefab — the bottom nav's
/// ALBUM tab already targets ScreenId.PawnCollection, so the new screen takes that slot and the
/// older Clay Pawn Collection is retired.
///
/// Same 440 x 956 frame and furniture as Levels / Friends: header band, tan tab tiles like the
/// shop, a progress bar, and a cream panel holding a scrolling 4-column grid.
/// </summary>
public static class LudoNXAlbumBuilder
{
    private const string PrefabPath = "Assets/Resources/UI/Screens/PawnCollection.prefab";
    private const string SetupArt = "Assets/Art/UI/MatchSetup";
    private const string MissionsArt = "Assets/Art/UI/Missions";
    private const string DiceArt = "Assets/Art/UI/Shop/Dice";
    private const string Shared = "Assets/Art/UI/Shared";

    private const float HeaderH = 102f;
    private const float TabsCy = 132f, ProgressCy = 176f, PanelTop = 212f;
    private static readonly Vector2 CellSize = new Vector2(84f, 112f);

    private static readonly string[] DiceFiles =
    {
        "dice_01_classic", "dice_02_ruby", "dice_03_sapphire", "dice_04_emerald", "dice_05_gold",
        "dice_06_amethyst", "dice_07_onyx", "dice_08_blossom", "dice_09_teal", "dice_10_amber",
    };

    [MenuItem("Ludo NX/UI/Build Album Screen", priority = 5)]
    public static void Build()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Make("PawnCollection", null, typeof(CanvasGroup), typeof(AlbumScreen));
        Stretch(root);
        var screen = root.GetComponent<AlbumScreen>();
        screen.SetId(ScreenId.PawnCollection);

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

        screen.stickerTextures = LudoNXStickerPackBuilder.LoadStickerTextures();
        screen.diceSprites = new Sprite[DiceFiles.Length];
        screen.diceNames = new string[DiceFiles.Length];
        for (int i = 0; i < DiceFiles.Length; i++)
        {
            screen.diceSprites[i] = Sprite($"{DiceArt}/{DiceFiles[i]}.png");
            screen.diceNames[i] = DiceFiles[i].Substring(8).ToUpperInvariant();
        }
        screen.pawnSprites = new[]
        {
            Sprite($"{SetupArt}/pawn_red.png"), Sprite($"{SetupArt}/pawn_green.png"),
            Sprite($"{SetupArt}/pawn_yellow.png"), Sprite($"{SetupArt}/pawn_blue.png"),
        };
        screen.pawnNames = new[] { "RED", "GREEN", "YELLOW", "BLUE" };
        screen.tabSelected = Sprite($"{SetupArt}/tile_selected.png");
        screen.tabIdle = Sprite($"{SetupArt}/tile_tan.png");

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Ludo NX] Album screen built -> {PrefabPath}.");
    }

    private static void BuildHeader(RectTransform root, AlbumScreen screen)
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

        var icon = Picture(widgets, "TitleIcon", $"{HomeArt}/nav_album.png");
        icon.preserveAspect = true;
        Top((RectTransform)icon.transform, H.Centre, 164f, 74f, 40f, 38f);

        var title = Text(widgets, "Title", "ALBUM", 33f, Ink, TextAlignmentOptions.MidlineLeft);
        Top((RectTransform)title.transform, H.Centre, 190f + 110f, 74.75f, 220f, 48f);
    }

    private static void BuildBody(RectTransform root, AlbumScreen screen)
    {
        var band = Band(root, "BodyBand", DesignBand.Edge.TopFill, 0f);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.bottomReservePoints = NavReserve;
        bandComp.bottomReserveReference = new Vector2(402f, 874f);
        bandComp.safeAreaBaseline = SafeTop;
        var content = Make("BodyContent", band);
        Stretch(content);
        bandComp.safeAreaContent = content;

        // Tabs, the shop's tan tiles.
        string[] labels = { "STICKERS", "DICE", "TOKENS" };
        screen.tabButtons = new Button[3];
        screen.tabBackgrounds = new Image[3];
        screen.tabLabels = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            var tab = Make("Tab_" + labels[i], content, typeof(Image), typeof(Button));
            Top(tab, H.Centre, 88f + i * 132f, TabsCy, 124f, 42f);
            var img = tab.GetComponent<Image>();
            Sliced(img, $"{SetupArt}/tile_tan.png", 2f);
            tab.GetComponent<Button>().targetGraphic = img;
            var label = Text(tab, "Label", labels[i], 15f, Cream, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            screen.tabButtons[i] = tab.GetComponent<Button>();
            screen.tabBackgrounds[i] = img;
            screen.tabLabels[i] = label;
        }

        // Progress: count, then a bar.
        var progress = Text(content, "Progress", "0 / 18 STICKERS", 14f, Cream, TextAlignmentOptions.MidlineLeft);
        Top(progress.rectTransform, H.Left, 40f + 110f, ProgressCy - 10f, 220f, 20f);
        screen.progressLabel = progress;
        var track = Picture(content, "Track", $"{MissionsArt}/progress_track.png");
        track.type = Image.Type.Sliced;
        track.pixelsPerUnitMultiplier = 2f;
        Top((RectTransform)track.transform, H.Centre, 220f, ProgressCy + 10f, 360f, 12f);
        var fill = Picture((RectTransform)track.transform, "Fill", $"{MissionsArt}/progress_fill.png");
        fill.type = Image.Type.Sliced;
        fill.pixelsPerUnitMultiplier = 2f;
        screen.progressFill = (RectTransform)fill.transform;

        // Panel with a scrolling grid.
        var panel = Make("Panel", content, typeof(Image));
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.offsetMin = new Vector2(24f, 0f);
        panel.offsetMax = new Vector2(-24f, -PanelTop);
        Sliced(panel.GetComponent<Image>(), $"{FriendsArt}/panel_combined.png", 2f);
        panel.GetComponent<Image>().raycastTarget = false;

        var viewport = Make("Viewport", panel, typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12f, 14f);
        viewport.offsetMax = new Vector2(-12f, -14f);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

        var grid = Make("Grid", viewport);
        grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.sizeDelta = new Vector2(360f, 400f);
        grid.anchoredPosition = Vector2.zero;

        var sr = viewport.GetComponent<ScrollRect>();
        sr.content = grid;
        sr.viewport = viewport;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        screen.scroll = sr;
        screen.gridContent = grid;
        screen.columns = 4;
        screen.cellPitch = new Vector2(88f, 122f);

        screen.cellTemplate = BuildCell(grid);
    }

    private static RectTransform BuildCell(RectTransform grid)
    {
        var cell = Make("CellTemplate", grid, typeof(Image), typeof(Button));
        cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 1f);
        cell.pivot = new Vector2(0.5f, 0.5f);
        cell.sizeDelta = CellSize;
        cell.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
        cell.GetComponent<Button>().transition = Selectable.Transition.None;

        var disc = Picture(cell, "Disc", "Assets/Resources/ui_circle.png");
        disc.color = new Color32(245, 235, 218, 255);
        InCard((RectTransform)disc.transform, CellSize.x, H.Left, CellSize.x / 2f, 38f, 74f, 74f);
        var art = Picture(cell, "Art", null);
        art.preserveAspect = true;
        InCard((RectTransform)art.transform, CellSize.x, H.Left, CellSize.x / 2f, 38f, 58f, 58f);
        // Lock on a small tan chip at the disc's lower right, readable at a glance.
        var lockChip = Picture(cell, "Lock", "Assets/Resources/ui_circle.png");
        lockChip.color = new Color32(192, 150, 102, 255);   // action.neutral.face
        InCard((RectTransform)lockChip.transform, CellSize.x, H.Left, CellSize.x / 2f + 26f, 62f, 26f, 26f);
        var lockGlyph = Picture((RectTransform)lockChip.transform, "Glyph", $"{SetupArt}/icon_lock.png");
        lockGlyph.preserveAspect = true;
        lockGlyph.color = new Color32(255, 243, 225, 255);
        Centre((RectTransform)lockGlyph.transform, 0f, 1f, 15f, 15f);

        var name = Text(cell, "Name", "NAME", 11.5f, Ink, TextAlignmentOptions.Center);
        InCard(name.rectTransform, CellSize.x, H.Left, CellSize.x / 2f, 88f, CellSize.x + 4f, 16f);
        name.enableAutoSizing = true; name.fontSizeMin = 8f; name.fontSizeMax = 11.5f;
        var sub = Text(cell, "Sub", "OWNED", 9f, SubInk, TextAlignmentOptions.Center);
        InCard(sub.rectTransform, CellSize.x, H.Left, CellSize.x / 2f, 103f, CellSize.x + 4f, 13f);
        sub.enableAutoSizing = true; sub.fontSizeMin = 7f; sub.fontSizeMax = 9f;
        return cell;
    }

    [MenuItem("Ludo NX/UI/Screen Test/Album", priority = 71)]
    private static void TestAlbum()
    {
        if (!Application.isPlaying || ScreenManager.Instance == null) { Debug.LogWarning("[Ludo NX] Enter play mode first."); return; }
        ScreenManager.Instance.Push(ScreenId.PawnCollection);
    }
}
