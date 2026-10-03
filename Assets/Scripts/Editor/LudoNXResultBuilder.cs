using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Builds the end-of-match result screen into Resources/UI/Screens/Result.prefab, replacing the
/// old pastel one.
///
/// Reading order top to bottom: how you did, who finished where, what it paid, what it cost you
/// in the match, and then the two things you can do next. The rewards sit above the buttons
/// rather than below, so the last thing read before tapping is the payout.
///
/// KEY MEASUREMENTS (points)
///   crest 92 at y 150 · headline 42 at y 232 · subhead 14 at y 268
///   placement panel from y 300, rows 338x62 in a 70 slot
///   rewards strip at y 622 · stats row at y 712 · buttons at y 820 and 882
/// </summary>
public static class LudoNXResultBuilder
{
    private const string Art = "Assets/Art/UI/MatchSetup";
    private const string ProfileArt = "Assets/Art/UI/Profile";
    private const string PrefabPath = "Assets/Resources/UI/Screens/Result.prefab";

    private const float RowW = 338f;

    [MenuItem("Ludo NX/UI/Build Result Screen (new)", priority = 7)]
    public static void Build()
    {
        // The crest comes from the profile badge set; import it as a sprite first or the
        // reference resolves to null and the screen shows a white box where the crest should be.
        ConfigureSprites(ProfileArt, null);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Make("Result", null, typeof(CanvasGroup), typeof(ResultScreen));
        Stretch(root);
        var screen = root.GetComponent<ResultScreen>();
        screen.SetId(ScreenId.Result);

        var bg = Make("Background", root, typeof(Image));
        Stretch(bg);
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Backgrounds/bg_match_emboss.jpg");
        bgImg.color = Color.white;
        bgImg.raycastTarget = false;
        var cover = bg.gameObject.AddComponent<BackgroundCoverFitter>();
        cover.overscan = 1.05f;

        var band = Band(root, "Body", DesignBand.Edge.Fill, 0f);
        var body = Make("Content", band);
        Stretch(body);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.safeAreaBaseline = SafeTop;
        bandComp.safeAreaContent = body;

        BuildHeadline(body, screen);
        BuildPlacements(body, screen);
        BuildRewards(body, screen);
        BuildStats(body, screen);
        BuildButtons(body, screen);

        screen.pawnSprites = new[]
        {
            Sprite($"{Art}/pawn_red.png"),
            Sprite($"{Art}/pawn_green.png"),
            Sprite($"{Art}/pawn_yellow.png"),
            Sprite($"{Art}/pawn_blue.png"),
        };
        screen.winCrest = Sprite($"{ProfileArt}/badge_07.png");
        screen.loseCrest = Sprite($"{ProfileArt}/badge_01.png");

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Result screen built -> {PrefabPath}.");
    }

    private static void BuildHeadline(RectTransform parent, ResultScreen screen)
    {
        var crest = Picture(parent, "Crest", $"{ProfileArt}/badge_07.png");
        Top((RectTransform)crest.transform, H.Centre, 220f, 150f, 92f, 92f);
        screen.crest = crest;

        var headline = Text(parent, "Headline", "VICTORY!", 42f, Cream, TextAlignmentOptions.Center);
        Top(headline.rectTransform, H.Centre, 220f, 232f, 400f, 56f);
        screen.headline = headline;

        var subhead = Text(parent, "Subhead", "Every token home first.", 14f,
            new Color32(206, 198, 184, 255), TextAlignmentOptions.Center);
        Top(subhead.rectTransform, H.Centre, 220f, 268f, 400f, 24f);
        screen.subhead = subhead;
    }

    private static void BuildPlacements(RectTransform parent, ResultScreen screen)
    {
        var panel = Make("Placements", parent, typeof(Image));
        Top(panel, H.Centre, 220f, 300f + 160f, 381f, 320f);
        Sliced(panel.GetComponent<Image>(), $"{FriendsArt}/panel_combined.png", 2f);
        panel.GetComponent<Image>().raycastTarget = false;

        var list = Make("Rows", panel, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        list.anchorMin = new Vector2(0f, 1f);
        list.anchorMax = new Vector2(1f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        list.offsetMin = new Vector2(21f, 0f);
        list.offsetMax = new Vector2(-21f, 0f);
        list.anchoredPosition = new Vector2(0f, -22f);
        var vlg = list.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.spacing = 8f;
        list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        screen.placementContent = list;

        screen.rowTemplate = BuildRow(list);
    }

    private static ResultRow BuildRow(RectTransform parent)
    {
        var rt = Make("PlacementRow", parent, typeof(LayoutElement), typeof(ResultRow));
        rt.sizeDelta = new Vector2(RowW, 62f);
        var le = rt.GetComponent<LayoutElement>();
        le.preferredWidth = RowW;
        le.preferredHeight = 62f;

        var row = rt.GetComponent<ResultRow>();

        var card = Make("Card", rt, typeof(Image));
        Stretch(card);
        Sliced(card.GetComponent<Image>(), $"{FriendsArt}/row_card.png", 2f);
        card.GetComponent<Image>().raycastTarget = false;
        row.card = card.GetComponent<Image>();

        var highlight = Picture(rt, "Highlight", $"{Art}/tile_glow.png");
        Stretch((RectTransform)highlight.transform);
        ((RectTransform)highlight.transform).offsetMin = new Vector2(-10f, -10f);
        ((RectTransform)highlight.transform).offsetMax = new Vector2(10f, 10f);
        highlight.transform.SetAsFirstSibling();
        highlight.gameObject.SetActive(false);
        row.highlight = highlight;

        var place = Text(rt, "Place", "1ST", 17f, new Color32(150, 112, 74, 255), TextAlignmentOptions.Center);
        InCard(place.rectTransform, RowW, H.Left, 34f, 31f, 48f, 24f);
        row.place = place;

        var pawn = Picture(rt, "Pawn", $"{Art}/pawn_red.png");
        InCard((RectTransform)pawn.transform, RowW, H.Left, 80f, 31f, 32f, 32f);
        row.pawn = pawn;

        var name = Text(rt, "Name", "Josiah", 17f, Ink, TextAlignmentOptions.MidlineLeft);
        LeftIn(name.rectTransform, 104f, 31f, 150f, 24f);
        row.nameLabel = name;

        var score = Text(rt, "Score", "120 pts", 13f, SubInk, TextAlignmentOptions.MidlineRight);
        InCard(score.rectTransform, RowW, H.Right, 322f - 45f, 31f, 90f, 22f);
        row.scoreLabel = score;

        rt.gameObject.SetActive(false);
        return row;
    }

    private static void BuildRewards(RectTransform parent, ResultScreen screen)
    {
        var strip = Make("Rewards", parent, typeof(Image));
        Top(strip, H.Centre, 220f, 666f, 381f, 74f);
        Sliced(strip.GetComponent<Image>(), $"{Art}/tile_tan.png", 2f);
        strip.GetComponent<Image>().raycastTarget = false;

        var coin = Picture(strip, "Coin", "Assets/Art/UI/Icons/coin_3d.png");
        InCard((RectTransform)coin.transform, 381f, H.Left, 96f, 30f, 26f, 26f);

        var coins = Text(strip, "Coins", "+120", 22f, Cream, TextAlignmentOptions.MidlineLeft);
        LeftIn(coins.rectTransform, 118f, 30f, 120f, 30f);
        screen.coinsLabel = coins;

        var nx = Text(strip, "NX", "+2,000 NX", 22f, Gold, TextAlignmentOptions.MidlineRight);
        InCard(nx.rectTransform, 381f, H.Right, 288f, 30f, 180f, 30f);
        screen.nxLabel = nx;

        var note = Text(strip, "StakeNote", "x100 stake paid out", 11f,
            new Color32(255, 242, 220, 190), TextAlignmentOptions.Center);
        InCard(note.rectTransform, 381f, H.Left, 190f, 58f, 360f, 18f);
        screen.stakeNote = note;
    }

    private static void BuildStats(RectTransform parent, ResultScreen screen)
    {
        var row = Make("MatchStats", parent);
        Top(row, H.Centre, 220f, 752f, 381f, 60f);

        screen.tokensHomeValue = StatTile(row, "Home", "TOKENS HOME", 63f);
        screen.capturesValue = StatTile(row, "Captures", "CAPTURES", 190f);
        screen.turnsValue = StatTile(row, "Turns", "TURNS", 317f);
    }

    private static TextMeshProUGUI StatTile(RectTransform parent, string name, string caption, float cx)
    {
        var tile = Make(name, parent);
        InCard(tile, 381f, H.Left, cx, 30f, 118f, 56f);

        var value = Text(tile, "Value", "0", 24f, Cream, TextAlignmentOptions.Center);
        Centre(value.rectTransform, 0f, 8f, 110f, 30f);

        var key = Text(tile, "Key", caption, 10f, new Color32(190, 184, 172, 255), TextAlignmentOptions.Center);
        Centre(key.rectTransform, 0f, -16f, 116f, 16f);

        return value;
    }

    private static void BuildButtons(RectTransform parent, ResultScreen screen)
    {
        screen.playAgainButton = Button(parent, "PlayAgain", "PLAY AGAIN", 830f,
            $"{FriendsArt}/btn_blank_tan.png", 22f);
        screen.homeButton = Button(parent, "Home", "HOME", 896f, $"{Art}/tile_dim.png", 18f);
    }

    private static Button Button(RectTransform parent, string name, string label, float cy,
        string sprite, float size)
    {
        var rt = Make(name, parent, typeof(Image), typeof(Button));
        Top(rt, H.Centre, 220f, cy, 300f, 56f);
        Sliced(rt.GetComponent<Image>(), sprite, 2f);

        var text = Text(rt, "Label", label, size, Cream, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(0f, 3f);

        return rt.GetComponent<Button>();
    }
}
