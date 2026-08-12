using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Rebuilds the player cards and repairs the scene's layout anchoring.
///
/// Run the menu items in order. Each is independent and re-runnable, so you can do one, look at
/// it, and continue. Save the scene (Ctrl+S) after each — these edit the open scene in memory.
/// </summary>
public static class LudoUILayoutBuilder
{
    // Card geometry, in canvas units at the 1080x1920 reference resolution.
    private const float CardWidth = 340f;
    private const float CardHeight = 200f;
    private const float SideMargin = 40f;

    private static readonly Color CardBackColor = new Color(0.08f, 0.09f, 0.13f, 0.95f);
    private static readonly Color MutedTextColor = new Color(0.75f, 0.78f, 0.85f, 1f);
    private static readonly Color PointsColor = new Color(1f, 0.85f, 0.2f, 1f);

    // ════════════════════════════════════════════════════════════════════════════════════════
    // 2. Player cards
    // ════════════════════════════════════════════════════════════════════════════════════════

    [MenuItem("Ludo NX/2. Rebuild Player Cards")]
    public static void RebuildPlayerCards()
    {
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Ludo NX", "No UIManager in the open scene.", "OK");
            return;
        }

        if (LudoUIKitGenerator.Load("ui_panel") == null)
        {
            EditorUtility.DisplayDialog("Ludo NX",
                "The UI sprite kit is missing.\n\nRun 'Ludo NX / 1. Generate UI Sprite Kit' first.", "OK");
            return;
        }

        TMP_FontAsset headerFont = FindFont("LilitaOne-Regular");
        TMP_FontAsset bodyFont = FindFont("PlusJakartaSans-Bold");

        int built = 0;
        built += BuildCard(ui.redPlayerCard, ui.redColor, headerFont, bodyFont) ? 1 : 0;
        built += BuildCard(ui.greenPlayerCard, ui.greenColor, headerFont, bodyFont) ? 1 : 0;
        built += BuildCard(ui.bluePlayerCard, ui.blueColor, headerFont, bodyFont) ? 1 : 0;
        built += BuildCard(ui.yellowPlayerCard, ui.yellowColor, headerFont, bodyFont) ? 1 : 0;

        MarkDirty(ui);
        EditorUtility.DisplayDialog("Ludo NX",
            $"Rebuilt {built} player card(s) and wired every PlayerCardUI reference.\n\nSAVE THE SCENE (Ctrl+S).",
            "OK");
    }

    private static bool BuildCard(PlayerCardUI card, Color playerColor, TMP_FontAsset headerFont, TMP_FontAsset bodyFont)
    {
        if (card == null) return false;

        Undo.RegisterFullObjectHierarchyUndo(card.gameObject, "Rebuild Player Card");

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);

        // Clear whatever ad-hoc children were there before (old cards had text mis-parented
        // inside other text objects, and three of the four had nothing at all).
        List<GameObject> stale = new List<GameObject>();
        foreach (Transform child in cardRect) stale.Add(child.gameObject);
        foreach (GameObject go in stale) Undo.DestroyObjectImmediate(go);

        // The card's own Image becomes the 9-sliced background plate.
        Image back = card.GetComponent<Image>();
        if (back == null) back = Undo.AddComponent<Image>(card.gameObject);
        back.sprite = LudoUIKitGenerator.Load("ui_panel");
        back.type = Image.Type.Sliced;
        back.color = CardBackColor;
        back.raycastTarget = false;

        CanvasGroup group = card.GetComponent<CanvasGroup>();
        if (group == null) group = Undo.AddComponent<CanvasGroup>(card.gameObject);

        // ── Glow border (sits behind the card, tinted the player's colour) ──
        Image glow = MakeImage("Glow", cardRect, "ui_panel_outline", playerColor);
        glow.type = Image.Type.Sliced;
        Stretch(glow.rectTransform, -10f);
        glow.transform.SetAsFirstSibling();
        glow.gameObject.SetActive(false);

        // ── Avatar cluster (left) ──
        float avatarX = -CardWidth * 0.5f + 74f;

        Image ringBack = MakeImage("AvatarRing_Back", cardRect, "ui_ring", new Color(1f, 1f, 1f, 0.15f));
        Box(ringBack.rectTransform, avatarX, 0f, 104f, 104f);

        Image avatar = MakeImage("Avatar", cardRect, "ui_circle", new Color(0.22f, 0.24f, 0.30f, 1f));
        Box(avatar.rectTransform, avatarX, 0f, 84f, 84f);

        // Radial-fill ring doubles as the turn timer — co-locating "whose turn" with "how long".
        Image ringFill = MakeImage("AvatarRing_Fill", cardRect, "ui_ring", playerColor);
        Box(ringFill.rectTransform, avatarX, 0f, 104f, 104f);
        ringFill.type = Image.Type.Filled;
        ringFill.fillMethod = Image.FillMethod.Radial360;
        ringFill.fillOrigin = 2;              // Top
        ringFill.fillClockwise = false;
        ringFill.fillAmount = 1f;

        // ── Text column (right) ──
        float textLeft = -CardWidth * 0.5f + 136f;
        float textRight = CardWidth * 0.5f - 18f;

        TextMeshProUGUI nameText = MakeText("Player_Name", cardRect, "PLAYER", bodyFont, 26,
            TextAlignmentOptions.Left, Color.white);
        AnchorLeft(nameText.rectTransform, textLeft, 46f, 170f, 40f);

        TextMeshProUGUI levelText = MakeText("Player_Level", cardRect, "Lv 1", bodyFont, 20,
            TextAlignmentOptions.Right, MutedTextColor);
        AnchorRight(levelText.rectTransform, textRight, 48f, 90f, 34f);

        TextMeshProUGUI pointsText = MakeText("Points", cardRect, "0 PTS", headerFont, 30,
            TextAlignmentOptions.Right, PointsColor);
        AnchorRight(pointsText.rectTransform, textRight, -34f, 150f, 46f);

        // ── Home progress pips ──
        Image[] pips = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            Image pip = MakeImage($"HomePip_{i}", cardRect, "ui_pip", new Color(0.3f, 0.3f, 0.3f, 0.4f));
            Box(pip.rectTransform, textLeft + 10f + i * 28f, -34f, 20f, 20f);
            pips[i] = pip;
        }

        // ── Fire streak holder (shown at 2+ captures) ──
        Image fire = MakeImage("FireStreak", cardRect, "ui_glow", new Color(1f, 0.45f, 0.1f, 0.75f));
        Stretch(fire.rectTransform, -6f);
        fire.gameObject.SetActive(false);

        // ── Wire every PlayerCardUI reference ──
        Undo.RecordObject(card, "Wire Player Card");
        card.avatarImage = avatar;
        card.avatarBorder = ringBack;
        card.playerNameText = nameText;
        card.pointsText = pointsText;
        // winRateText deliberately left unassigned — the level label is a static placeholder, and
        // wiring it here would let a SetWinRate() call overwrite "Lv 1" with a percentage.
        card.winRateText = null;
        card.homeTokenDots = pips;
        card.timerBarFill = ringFill;
        card.cardCanvasGroup = group;
        card.glowBorder = glow;
        card.fireEffectObject = fire.gameObject;
        card.activeGlowColor = playerColor;
        card.inactiveColor = new Color(1f, 1f, 1f, 0.6f);
        EditorUtility.SetDirty(card);

        return true;
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // 3. Layout anchoring
    // ════════════════════════════════════════════════════════════════════════════════════════

    [MenuItem("Ludo NX/3. Fix Layout Anchoring")]
    public static void FixLayoutAnchoring()
    {
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Ludo NX", "No UIManager in the open scene.", "OK");
            return;
        }

        List<string> notes = new List<string>();

        // The two container panels are 100x100 rects pinned at screen centre, with children
        // carrying hard-coded +/-660 and -830 offsets. Nothing is anchored to a screen edge, so
        // on a taller phone the whole bottom bar floats hundreds of pixels above the bottom.
        // Making them full-screen stretch lets their children anchor to real edges.
        if (StretchContainer("PlayerProfiles_Panel")) notes.Add("PlayerProfiles_Panel -> full-screen container");
        if (StretchContainer("BottomBar_Panel")) notes.Add("BottomBar_Panel -> full-screen container");

        // Player cards: top pair anchored to the top edge, bottom pair to the bottom edge.
        AnchorCard(ui.redPlayerCard, corner: 0, notes);      // top-left
        AnchorCard(ui.greenPlayerCard, corner: 1, notes);    // top-right
        AnchorCard(ui.bluePlayerCard, corner: 2, notes);     // bottom-left
        AnchorCard(ui.yellowPlayerCard, corner: 3, notes);   // bottom-right

        // Roll button owns the bottom centre, comfortably inside one-handed thumb reach.
        RectTransform roll = FindRect("Roll_Button", requireParentName: "Canvas");
        if (roll != null)
        {
            Undo.RecordObject(roll, "Anchor Roll Button");
            roll.anchorMin = new Vector2(0.5f, 0f);
            roll.anchorMax = new Vector2(0.5f, 0f);
            roll.pivot = new Vector2(0.5f, 0f);
            roll.sizeDelta = new Vector2(480f, 110f);
            roll.anchoredPosition = new Vector2(0f, 110f);
            notes.Add("Roll_Button -> bottom centre 480x110");
        }

        // Chat / Emoji shrink to circular icons flanking ROLL so they stop competing with it.
        AnchorBottomIcon("Chat_Button", leftSide: true, notes);
        AnchorBottomIcon("Emoji_Button", leftSide: false, notes);

        // Top bar: full-width strip, icons pinned to its corners.
        RectTransform topBar = FindRect("TopBar_Panel");
        if (topBar != null)
        {
            Undo.RecordObject(topBar, "Anchor Top Bar");
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.sizeDelta = new Vector2(-60f, 100f);
            topBar.anchoredPosition = new Vector2(0f, -40f);
            notes.Add("TopBar_Panel -> top strip");
        }

        AnchorTopIcon("Exit_Button", leftSide: true, notes);
        AnchorTopIcon("Audio_Button", leftSide: false, notes);

        MarkDirty(ui);
        EditorUtility.DisplayDialog("Ludo NX",
            "Layout anchoring fixed:\n\n• " + string.Join("\n• ", notes) +
            "\n\nSAVE THE SCENE (Ctrl+S).", "OK");
    }

    private static void AnchorCard(PlayerCardUI card, int corner, List<string> notes)
    {
        if (card == null) return;
        RectTransform rt = card.GetComponent<RectTransform>();
        Undo.RecordObject(rt, "Anchor Player Card");

        bool isTop = corner < 2;
        bool isLeft = corner % 2 == 0;

        Vector2 anchor = new Vector2(isLeft ? 0f : 1f, isTop ? 1f : 0f);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = new Vector2(CardWidth, CardHeight);
        rt.anchoredPosition = new Vector2(
            isLeft ? SideMargin : -SideMargin,
            isTop ? -270f : 250f);

        notes.Add($"{card.name} -> {(isTop ? "top" : "bottom")}-{(isLeft ? "left" : "right")}");
    }

    private static void AnchorBottomIcon(string name, bool leftSide, List<string> notes)
    {
        RectTransform rt = FindRect(name);
        if (rt == null) return;

        Undo.RecordObject(rt, "Anchor Icon");
        rt.anchorMin = new Vector2(leftSide ? 0f : 1f, 0f);
        rt.anchorMax = new Vector2(leftSide ? 0f : 1f, 0f);
        rt.pivot = new Vector2(leftSide ? 0f : 1f, 0f);
        rt.sizeDelta = new Vector2(88f, 88f);
        rt.anchoredPosition = new Vector2(leftSide ? 60f : -60f, 120f);
        notes.Add($"{name} -> bottom {(leftSide ? "left" : "right")} 88x88");
    }

    private static void AnchorTopIcon(string name, bool leftSide, List<string> notes)
    {
        RectTransform rt = FindRect(name);
        if (rt == null) return;

        Undo.RecordObject(rt, "Anchor Icon");
        rt.anchorMin = new Vector2(leftSide ? 0f : 1f, 0.5f);
        rt.anchorMax = new Vector2(leftSide ? 0f : 1f, 0.5f);
        rt.pivot = new Vector2(leftSide ? 0f : 1f, 0.5f);
        rt.sizeDelta = new Vector2(88f, 88f);
        rt.anchoredPosition = new Vector2(leftSide ? 6f : -6f, 0f);
        notes.Add($"{name} -> top {(leftSide ? "left" : "right")} 88x88");
    }

    private static bool StretchContainer(string name)
    {
        RectTransform rt = FindRect(name);
        if (rt == null) return false;

        Undo.RecordObject(rt, "Stretch Container");
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return true;
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // 4. Icon tints and the duplicate roll button
    // ════════════════════════════════════════════════════════════════════════════════════════

    [MenuItem("Ludo NX/4. Fix Icon Tints & Duplicate Roll Button")]
    public static void FixIconsAndDuplicates()
    {
        List<string> notes = new List<string>();

        // Exit and Audio are tinted {0.12, 0.15, 0.2, 0.9} — near-black at 90%, which crushes the
        // icon art into an unreadable silhouette.
        foreach (string iconName in new[] { "Exit_Button", "Audio_Button", "Chat_Button", "Emoji_Button" })
        {
            RectTransform rt = FindRect(iconName);
            if (rt == null) continue;

            Image img = rt.GetComponent<Image>();
            if (img != null && img.color != Color.white)
            {
                Undo.RecordObject(img, "Fix Icon Tint");
                img.color = Color.white;
                notes.Add($"{iconName}: tint cleared to white");
            }

            // The icons are self-explanatory; the labels only crowd them (and "QUIT" was a
            // 200px-wide label inside a 130px button).
            foreach (Transform child in rt)
            {
                if (child.GetComponent<TMP_Text>() != null && child.gameObject.activeSelf)
                {
                    Undo.RecordObject(child.gameObject, "Hide Icon Label");
                    child.gameObject.SetActive(false);
                    notes.Add($"{iconName}: hid redundant text label");
                }
            }
        }

        // Two live Roll_Buttons exist. The one nested in BottomBar_Panel sits at anchoredPosition
        // {180, 0} — dead centre of the screen, on top of the board — and bypasses UIManager via a
        // baked-in OnClick, so it never greys out. Disable it; keep the Canvas-level one.
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        Button wired = ui != null ? ui.rollButton : null;

        foreach (Button b in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (b == null || b.gameObject.name != "Roll_Button") continue;
            if (EditorUtility.IsPersistent(b.gameObject)) continue;
            if (b == wired) continue;

            Undo.RecordObject(b.gameObject, "Disable Duplicate Roll Button");
            b.gameObject.SetActive(false);
            notes.Add("Disabled the duplicate Roll_Button that was floating over the board");
        }

        // The diceslot plates sit ~200px below where the 3D dice actually render.
        foreach (string slot in new[] { "diceslot_1", "diceslot_2" })
        {
            RectTransform rt = FindRect(slot);
            if (rt != null && rt.gameObject.activeSelf)
            {
                Undo.RecordObject(rt.gameObject, "Disable Orphan Dice Slot");
                rt.gameObject.SetActive(false);
                notes.Add($"{slot}: disabled (detached from the real dice)");
            }
        }

        if (notes.Count == 0)
        {
            EditorUtility.DisplayDialog("Ludo NX", "Nothing to fix — already clean.", "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Ludo NX",
            "• " + string.Join("\n• ", notes) + "\n\nSAVE THE SCENE (Ctrl+S).", "OK");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static RectTransform FindRect(string name, string requireParentName = null)
    {
        foreach (RectTransform rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rt.gameObject.name != name) continue;
            if (EditorUtility.IsPersistent(rt.gameObject)) continue;
            if (rt.gameObject.scene.IsValid() == false) continue;
            if (requireParentName != null && (rt.parent == null || rt.parent.name != requireParentName)) continue;
            return rt;
        }
        return null;
    }

    private static TMP_FontAsset FindFont(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:TMP_FontAsset");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static Image MakeImage(string name, Transform parent, string spriteName, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.sprite = LudoUIKitGenerator.Load(spriteName);
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent, string text,
        TMP_FontAsset font, float size, TextAlignmentOptions align, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private static void Box(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    private static void AnchorLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    private static void AnchorRight(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    private static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static void MarkDirty(Object target)
    {
        EditorUtility.SetDirty(target);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
