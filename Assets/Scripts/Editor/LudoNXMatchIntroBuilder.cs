using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

public static class LudoNXMatchIntroBuilder
{
    private const string ScreenPath = "Assets/Resources/UI/Screens/MatchIntro.prefab";
    private const string PanelMenu = "Assets/Art/UI/Menu/panel_menu.png";
    private const string DividerArt = "Assets/Art/UI/Menu/divider.png";
    private const string CancelBtnArt = "Assets/Art/UI/Shared/btn_cancel_black_circle.png";
    private const string CircleArt = "Assets/Resources/ui_circle.png";
    private const string SlotBtnArt = "Assets/Art/UI/Menu/slot_button.png";

    [MenuItem("Ludo NX/UI/Build Modern Match Intro", priority = 12)]
    public static void Build()
    {
        var rootRt = Make("MatchIntro", null, typeof(CanvasGroup), typeof(MatchIntroScreen));
        Stretch(rootRt);

        var screen = rootRt.GetComponent<MatchIntroScreen>();
        screen.SetId(ScreenId.MatchIntro);

        // 1. Dark Slate Wallpaper Background
        var bgRt = Make("Background", rootRt, typeof(Image));
        Stretch(bgRt);
        var bgImg = bgRt.GetComponent<Image>();
        bgImg.color = new Color32(23, 26, 33, 255); // Rich dark slate
        bgImg.raycastTarget = false;

        // 2. Full-Screen Tap Catcher (Skip Area)
        var skipRt = Make("SkipArea", rootRt, typeof(Image), typeof(Button));
        Stretch(skipRt);
        var skipImg = skipRt.GetComponent<Image>();
        Invisible(skipImg);
        skipImg.raycastTarget = true;
        screen.skipArea = skipRt.GetComponent<Button>();

        // 3. Top Header Bar
        var headerBand = Band(rootRt, "HeaderBand", DesignBand.Edge.Top, 110f);
        var title = Text(headerBand, "Title", "GET READY!", 32f, Cream, TextAlignmentOptions.Center);
        Top((RectTransform)title.transform, H.Centre, 220f, 65f, 300f, 40f);

        // Cancel / Back Button (Compact black outline circle with 'X')
        var cancelRt = Make("CancelButton", headerBand, typeof(Image), typeof(Button));
        Top(cancelRt, H.Right, 400f, 65f, 34f, 34f);
        var cancelImg = cancelRt.GetComponent<Image>();
        cancelImg.sprite = Sprite(CancelBtnArt);
        cancelImg.preserveAspect = true;
        cancelImg.raycastTarget = true;
        screen.backButton = cancelRt.GetComponent<Button>();

        // 4. Main Universal Parchment Card
        var contentBand = Band(rootRt, "ContentBand", DesignBand.Edge.Fill, 0f);
        var cardRt = Make("Card", contentBand, typeof(Image));
        Centre(cardRt, 0f, 0f, 380f, 540f);
        var cardImg = cardRt.GetComponent<Image>();
        cardImg.sprite = Sprite(PanelMenu);
        cardImg.type = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 1f;
        cardImg.raycastTarget = false;

        // 4a. Countdown Badge (Top of Card)
        var badgeRt = Make("CountdownBadge", cardRt, typeof(Image));
        Top(badgeRt, H.Centre, 190f, 75f, 84f, 84f);
        var badgeImg = badgeRt.GetComponent<Image>();
        badgeImg.sprite = Sprite(CircleArt);
        badgeImg.color = new Color32(38, 28, 18, 220); // Dark wooden bronze circle
        badgeImg.raycastTarget = false;

        var countText = Text(badgeRt, "Count", "3", 50f, Gold, TextAlignmentOptions.Center);
        Centre((RectTransform)countText.transform, 0f, 0f, 84f, 84f);
        screen.countdownLabel = countText;

        // 4b. Mode & Difficulty Chips
        var modeChipRt = Make("ModeChip", cardRt, typeof(Image));
        Top(modeChipRt, H.Centre, 125f, 138f, 140f, 30f);
        var modeChipImg = modeChipRt.GetComponent<Image>();
        modeChipImg.sprite = Sprite(SlotBtnArt);
        modeChipImg.type = Image.Type.Sliced;
        modeChipImg.raycastTarget = false;
        var modeLabel = Text(modeChipRt, "Label", "CLASSIC", 14f, new Color32(50, 36, 22, 255), TextAlignmentOptions.Center);
        Centre((RectTransform)modeLabel.transform, 0f, 0f, 136f, 28f);
        screen.modeChipLabel = modeLabel;

        var diffChipRt = Make("DiffChip", cardRt, typeof(Image));
        Top(diffChipRt, H.Centre, 255f, 138f, 100f, 30f);
        var diffChipImg = diffChipRt.GetComponent<Image>();
        diffChipImg.sprite = Sprite(SlotBtnArt);
        diffChipImg.type = Image.Type.Sliced;
        diffChipImg.raycastTarget = false;
        var diffLabel = Text(diffChipRt, "Label", "NORMAL", 14f, new Color32(50, 36, 22, 255), TextAlignmentOptions.Center);
        Centre((RectTransform)diffLabel.transform, 0f, 0f, 96f, 28f);
        screen.difficultyChipLabel = diffLabel;

        // Divider Line
        var divRt = Make("Divider", cardRt, typeof(Image));
        Top(divRt, H.Centre, 190f, 175f, 310f, 4f);
        var divImg = divRt.GetComponent<Image>();
        divImg.sprite = Sprite(DividerArt);
        divImg.color = new Color(1f, 1f, 1f, 0.7f);
        divImg.raycastTarget = false;

        // 4c. You Card / Section
        var youSection = Make("YouSection", cardRt);
        Top(youSection, H.Centre, 190f, 230f, 320f, 60f);

        var youSwatchRt = Make("YouSwatch", youSection, typeof(Image));
        Top(youSwatchRt, H.Left, 35f, 30f, 54f, 54f);
        var youSwatchImg = youSwatchRt.GetComponent<Image>();
        youSwatchImg.sprite = Sprite(CircleArt);
        youSwatchImg.color = new Color32(231, 55, 53, 255); // Red
        youSwatchImg.raycastTarget = false;
        screen.youColourSwatch = youSwatchImg;

        var youLabel = Text(youSection, "YouLabel", "YOU — RED", 20f, new Color32(40, 30, 20, 255), TextAlignmentOptions.Left);
        Top((RectTransform)youLabel.transform, H.Left, 190f, 30f, 230f, 40f);
        screen.youColourLabel = youLabel;

        // 4d. VS Divider
        var vsText = Text(cardRt, "VS", "VS", 22f, new Color32(180, 130, 60, 255), TextAlignmentOptions.Center);
        Top((RectTransform)vsText.transform, H.Centre, 190f, 295f, 100f, 30f);

        // 4e. Opponents Section (3 slots)
        var oppSection = Make("OpponentsSection", cardRt);
        Top(oppSection, H.Centre, 190f, 380f, 320f, 100f);

        var swatches = new Image[3];
        var labels = new TextMeshProUGUI[3];
        float[] xSlots = new[] { 50f, 160f, 270f };

        for (int i = 0; i < 3; i++)
        {
            var slotRt = Make("Slot_" + i, oppSection);
            Top(slotRt, H.Left, xSlots[i], 45f, 80f, 90f);

            var swRt = Make("Swatch", slotRt, typeof(Image));
            Top(swRt, H.Centre, 40f, 25f, 44f, 44f);
            var swImg = swRt.GetComponent<Image>();
            swImg.sprite = Sprite(CircleArt);
            swImg.color = new Color32(77, 174, 81, 255);
            swImg.raycastTarget = false;
            swatches[i] = swImg;

            var lb = Text(slotRt, "Label", "BOT " + (i + 1), 14f, new Color32(60, 48, 35, 255), TextAlignmentOptions.Center);
            Top((RectTransform)lb.transform, H.Centre, 40f, 65f, 80f, 24f);
            labels[i] = lb;
        }
        screen.opponentSwatches = swatches;
        screen.opponentLabels = labels;

        // 5. Bottom Hint: TAP ANYWHERE TO SKIP
        var hintBand = Band(rootRt, "HintBand", DesignBand.Edge.Bottom, 70f);
        var hint = Text(hintBand, "Hint", "TAP ANYWHERE TO SKIP", 15f, new Color(1f, 0.95f, 0.86f, 0.65f), TextAlignmentOptions.Center);
        Centre((RectTransform)hint.transform, 0f, -10f, 300f, 30f);

        // Save Prefab
        PrefabUtility.SaveAsPrefabAsset(rootRt.gameObject, ScreenPath);
        Object.DestroyImmediate(rootRt.gameObject);
        Debug.Log($"[Ludo NX] Successfully built modern MatchIntro prefab -> {ScreenPath}");
    }
}
