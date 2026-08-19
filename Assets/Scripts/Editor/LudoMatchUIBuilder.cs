using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Lays out the in-match HUD to the supplied reference (Assets/refrence.png): gradient ground that
/// follows the turn, exit and volume on the top row, timer pill under them, two player cards above
/// the board and two below, then dice, ROLL, and the emoji/chat pair stacked at the left.
///
/// It arranges and skins the objects the scene already has rather than replacing them, because
/// those objects carry the wiring — UIManager's button and card references, PlayerCardUI, the dice
/// tray. Rebuilding them from scratch would mean re-wiring all of it for no visual gain.
///
/// The background is two stacked layers rather than one tinted image: a solid colour underneath and
/// a white-to-transparent gradient on top. That reproduces the reference's FFFFFF → colour ramp
/// while leaving a single flat colour for <see cref="BackgroundAmbience"/> to crossfade as the turn
/// passes, which a baked gradient sprite per colour could not do without four crossfading layers.
///
/// Run after Ludo NX/10. Numbers come from the reference, expressed against the 1080×1920 canvas.
/// </summary>
public static class LudoMatchUIBuilder
{
    private const string GeneratedDir = "Assets/art_assets/generated";
    private const string TabSprite = "Assets/general background tab.png";
    private const string TimerSprite = "Assets/timer_countdown_background.png";

    // Gradient ends supplied with the reference. Each is FFFFFF at the top of the screen down to
    // this colour at the bottom.
    private static readonly Color RedGround = Hex(0x8A, 0x2E, 0x2E);
    private static readonly Color GreenGround = Hex(0x3F, 0x63, 0x46);
    private static readonly Color YellowGround = Hex(0xC8, 0x8A, 0x32);
    private static readonly Color BlueGround = Hex(0x2A, 0x4D, 0x6C);

    // Drop shadow supplied with the reference: X 0, Y 4, blur 2, spread 0, black at 40%.
    private static readonly Vector2 ShadowOffset = new Vector2(0f, -4f);
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.40f);

    // Vertical anchors as a fraction of screen height, read off the reference.
    private const float RowIcons = 0.0815f;   // exit / volume
    private const float RowTimer = 0.1500f;
    private const float RowCardsTop = 0.2415f;
    private const float RowCardsBottom = 0.7565f;
    private const float RowDice = 0.7960f;
    private const float RowEmoji = 0.8275f;
    private const float RowRoll = 0.9150f;
    private const float RowChat = 0.9225f;

    private const float CanvasW = 1080f;
    private const float CanvasH = 1920f;

    [MenuItem("Ludo NX/11. Build Match HUD")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[LudoMatchUIBuilder] Stop play mode first — the layout would be discarded.");
            return;
        }

        GameObject canvasGo = FindHudCanvas();
        if (canvasGo == null)
        {
            Debug.LogError("[LudoMatchUIBuilder] Could not find the in-match Canvas.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvasGo, "Build match HUD");

        // Earlier tooling left this switched off in the saved scene. On is the honest authored
        // state: LudoMenuFlow hides it on its first frame while the menu is up.
        if (!canvasGo.activeSelf) canvasGo.SetActive(true);

        // Not 9-sliced. On both of these the rounded corner IS most of the image, so any border
        // value either overflows the rect (corners collapse into a diamond) or stretches the arc
        // into a cross. Drawn Simple at a fixed size they are simply correct.
        ImportSimple(TabSprite);
        ImportSimple(TimerSprite);

        Sprite tab = AssetDatabase.LoadAssetAtPath<Sprite>(TabSprite);
        Sprite timerPill = AssetDatabase.LoadAssetAtPath<Sprite>(TimerSprite);
        Sprite ramp = EnsureGradientSprite();

        Transform root = canvasGo.transform;

        BuildBackground(root, ramp);
        LayOutTopRow(root, tab, timerPill);
        LayOutCards(root);
        LayOutBottom(root);
        BuildExitConfirm(root);

        EditorUtility.SetDirty(canvasGo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[LudoMatchUIBuilder] Match HUD laid out to the reference. Save the scene to keep it.");
    }

    // ── background ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Solid colour underneath, white-to-transparent ramp on top. BackgroundAmbience keeps driving
    /// the solid layer, so the turn transition it already performs now reads as the whole ground
    /// shifting hue rather than a flat panel changing tint.
    /// </summary>
    private static void BuildBackground(Transform root, Sprite ramp)
    {
        Transform existing = root.Find("Background_Image");
        RectTransform baseRect = existing as RectTransform;

        if (baseRect == null)
        {
            GameObject go = new GameObject("Background_Image", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            baseRect = go.GetComponent<RectTransform>();
        }

        Stretch(baseRect);
        baseRect.SetSiblingIndex(0);

        Image baseImg = baseRect.GetComponent<Image>();
        if (baseImg == null) baseImg = baseRect.gameObject.AddComponent<Image>();
        baseImg.sprite = null;
        baseImg.color = BlueGround;
        baseImg.raycastTarget = false;

        RectTransform rampRect = Child(baseRect, "Background_Ramp");
        Stretch(rampRect);
        Image rampImg = rampRect.GetComponent<Image>();
        if (rampImg == null) rampImg = rampRect.gameObject.AddComponent<Image>();
        rampImg.sprite = ramp;
        rampImg.color = Color.white;
        rampImg.type = Image.Type.Simple;
        rampImg.raycastTarget = false;

        // Point the ambience at the solid layer and give it the reference colours.
        BackgroundAmbience amb = Object.FindAnyObjectByType<BackgroundAmbience>();
        if (amb != null)
        {
            Undo.RecordObject(amb, "Reference ground colours");
            amb.backgroundImage = baseImg;
            amb.redTurnColor = RedGround;
            amb.greenTurnColor = GreenGround;
            amb.yellowTurnColor = YellowGround;
            amb.blueTurnColor = BlueGround;
            amb.baseColor = BlueGround;

            // The vignette layer would sit on top of the ramp and muddy it; the gradient is the
            // atmosphere now.
            if (amb.vignetteOverlay != null) amb.vignetteOverlay.gameObject.SetActive(false);
            EditorUtility.SetDirty(amb);
        }
    }

    /// <summary>White at the top fading to clear at the bottom — the FFFFFF end of every ramp.</summary>
    private static Sprite EnsureGradientSprite()
    {
        string path = $"{GeneratedDir}/ui_ground_ramp.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        Directory.CreateDirectory(GeneratedDir);

        const int h = 256;
        Texture2D tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            // v = 1 at the top of the screen. Eased so the white holds through the upper third the
            // way the reference does, instead of washing out linearly across the whole height.
            float v = y / (float)(h - 1);
            float a = Mathf.Pow(v, 1.35f);
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(path) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ── rows ───────────────────────────────────────────────────────────────────────────────

    private static void LayOutTopRow(Transform root, Sprite tab, Sprite timerPill)
    {
        RectTransform bar = Find(root, "TopBar_Panel");
        if (bar != null)
        {
            // The panel was a strip the icons hung off. The reference has no strip — the icons and
            // the timer sit straight on the ground — so it becomes a full-bleed container and its
            // own graphic is switched off.
            Stretch(bar);
            Image barImg = bar.GetComponent<Image>();
            if (barImg != null) barImg.enabled = false;
        }

        Transform parent = bar != null ? bar : root;

        RectTransform exit = Find(parent, "Exit_Button") ?? Find(root, "Exit_Button");
        Backplate(exit, tab, 60);

        RectTransform audio = Find(parent, "Audio_Button") ?? Find(root, "Audio_Button");
        Backplate(audio, tab, 60);

        RectTransform timer = Find(parent, "Timer_Text") ?? Find(root, "Timer_Text");
        Backplate(timer, timerPill, 70);

        TextMeshProUGUI timerText = timer != null ? timer.GetComponent<TextMeshProUGUI>() : null;
        if (timerText != null)
        {
            Undo.RecordObject(timerText, "Timer type");
            timerText.color = new Color32(0x22, 0x22, 0x22, 0xFF);
            timerText.fontStyle = FontStyles.Bold;
            timerText.fontSize = 68f;
            timerText.characterSpacing = 4f;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.enableAutoSizing = false;
            Shadow(timerText.gameObject, false);
            EditorUtility.SetDirty(timerText);
        }
    }

    /// <summary>
    /// Wires every HUD element into MatchHudLayout and lets it do the positioning.
    ///
    /// Nothing here sets a row any more. Two places computing layout is what let the UI and the
    /// camera disagree, and only one of them can be right.
    /// </summary>
    private static void LayOutCards(Transform root)
    {
        RectTransform panel = Find(root, "PlayerProfiles_Panel");
        if (panel != null) Stretch(panel);
        Transform parent = panel != null ? panel : root;

        RectTransform tl = Find(parent, "Profile_TopLeft");
        RectTransform tr = Find(parent, "Profile_TopRight");
        RectTransform bl = Find(parent, "Profile_BottomLeft");
        RectTransform br = Find(parent, "Profile_BottomRight");

        MatchHudLayout layout = root.GetComponentInChildren<MatchHudLayout>(true);
        if (layout == null)
            layout = Undo.AddComponent<MatchHudLayout>(parent != null ? parent.gameObject : root.gameObject);

        Undo.RecordObject(layout, "Wire HUD layout");
        layout.topLeft = tl;
        layout.topRight = tr;
        layout.bottomLeft = bl;
        layout.bottomRight = br;
        layout.exitButton = Find(root, "Exit_Button");
        layout.audioButton = Find(root, "Audio_Button");
        layout.timer = Find(root, "Timer_Text");
        layout.rollButton = Find(root, "Roll_Button");
        layout.emojiButton = Find(root, "Emoji_Button");
        layout.chatButton = Find(root, "Chat_Button");
        layout.Apply();
        EditorUtility.SetDirty(layout);

        foreach (string n in new[] { "Profile_TopLeft", "Profile_TopRight", "Profile_BottomLeft", "Profile_BottomRight" })
        {
            RectTransform card = Find(parent, n);
            if (card == null) continue;
            Shadow(card.gameObject, false);
            BoldName(card);
        }
    }

    /// <summary>Names in the reference are heavy and widely tracked — that spacing is most of the look.</summary>
    private static void BoldName(RectTransform card)
    {
        foreach (TextMeshProUGUI t in card.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            Undo.RecordObject(t, "Card type");
            if (t.gameObject.name == "Player_Name")
            {
                t.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                t.characterSpacing = 8f;
            }
            else
            {
                t.fontStyle = FontStyles.Bold;
            }
            EditorUtility.SetDirty(t);
        }
    }

    /// <summary>Skins the bottom row. Placement belongs to MatchHudLayout.</summary>
    private static void LayOutBottom(Transform root)
    {
        RectTransform roll = Find(root, "Roll_Button");
        Shadow(roll != null ? roll.gameObject : null, false);

        if (roll != null)
        {
            TextMeshProUGUI label = roll.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                Undo.RecordObject(label, "Roll type");
                label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                label.characterSpacing = 10f;
                label.alignment = TextAlignmentOptions.Center;
                EditorUtility.SetDirty(label);
            }
        }

        Shadow(Find(root, "Emoji_Button")?.gameObject, false);
        Shadow(Find(root, "Chat_Button")?.gameObject, false);
    }

    /// <summary>
    /// A single "are you sure" sheet on the exit button — not a pause menu.
    ///
    /// Exit is the one irreversible tap in a match: the board is gone and the result is forfeited.
    /// One confirm is worth it there. Everything else stays one tap.
    /// </summary>
    private static void BuildExitConfirm(Transform root)
    {
        // Clear the older pause sheet if a previous build left one behind.
        Transform stale = root.Find("Pause_Overlay");
        if (stale != null) Undo.DestroyObjectImmediate(stale.gameObject);

        UIManager ui = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
        if (ui == null) return;

        RectTransform sheet = Child(root, "ExitConfirm_Overlay");
        Stretch(sheet);
        sheet.SetAsLastSibling();

        Image scrim = sheet.GetComponent<Image>();
        if (scrim == null) scrim = sheet.gameObject.AddComponent<Image>();
        scrim.color = new Color(0.02f, 0.05f, 0.09f, 0.78f);
        scrim.raycastTarget = true;   // blocks the board behind the dialog

        Sprite panel = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedDir}/ui_panel.png");

        RectTransform card = Child(sheet, "Card");
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;
        card.sizeDelta = new Vector2(800, 430);

        Image cardImg = card.GetComponent<Image>();
        if (cardImg == null) cardImg = card.gameObject.AddComponent<Image>();
        cardImg.sprite = panel;
        cardImg.type = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 2.2f;
        cardImg.color = new Color32(0x14, 0x1E, 0x30, 0xFF);
        Shadow(card.gameObject, false);

        TMP_FontAsset display = FindFont("Baloo2-ExtraBold");
        TMP_FontAsset body = FindFont("Nunito-Bold");

        Label(card, "Title", "LEAVE MATCH?", display, 58, Color.white, new Vector2(0, 118), new Vector2(720, 84));
        TextMeshProUGUI sub = Label(card, "Body", "This match will be forfeited.", body, 30,
                                    new Color32(0x9E, 0xAF, 0xC8, 0xFF), new Vector2(0, 46), new Vector2(720, 48));
        sub.characterSpacing = 0f;

        ConfirmButton(card, "Btn_Cancel", "CANCEL", body, new Vector2(-190, -102), new Color32(0x25, 0x37, 0x5B, 0xFF),
                      Color.white, ui, nameof(UIManager.CancelExit));
        ConfirmButton(card, "Btn_Exit", "EXIT", body, new Vector2(190, -102), new Color32(0xC4, 0x3B, 0x35, 0xFF),
                      Color.white, ui, nameof(UIManager.ConfirmExit));

        sheet.gameObject.SetActive(false);

        Undo.RecordObject(ui, "Wire exit confirm");
        ui.quitConfirmPanel = sheet.gameObject;
        EditorUtility.SetDirty(ui);
    }

    private static void ConfirmButton(RectTransform parent, string name, string text, TMP_FontAsset font,
                                      Vector2 pos, Color fill, Color ink, UIManager ui, string method)
    {
        RectTransform rt = Child(parent, name);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(340, 104);

        Sprite panel = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedDir}/ui_panel.png");
        Image img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.sprite = panel;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 2.2f;
        img.color = fill;

        Label(rt, "Label", text, font, 38, ink, Vector2.zero, new Vector2(300, 64));

        Button btn = rt.GetComponent<Button>();
        if (btn == null) btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        while (btn.onClick.GetPersistentEventCount() > 0)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, 0);

        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
            btn.onClick, (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), ui, method));

        Shadow(rt.gameObject, false);
    }

    private static TextMeshProUGUI Label(RectTransform parent, string name, string text, TMP_FontAsset font,
                                         float size, Color color, Vector2 pos, Vector2 rect)
    {
        RectTransform rt = Child(parent, name);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = rect;

        TextMeshProUGUI tmp = rt.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = 6f;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
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

    // ── helpers ────────────────────────────────────────────────────────────────────────────

    /// <summary>Anchors to a corner and positions by the reference's fraction-of-height rows.</summary>
    private static void Place(RectTransform rt, Vector2 anchor, Vector2 posFromAnchor, Vector2 size)
    {
        if (rt == null) return;
        Undo.RecordObject(rt, "Place HUD element");

        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(anchor.x, 0.5f);

        float y = posFromAnchor.y <= 1f && posFromAnchor.y >= 0f
            ? -(posFromAnchor.y * CanvasH)     // fraction of height, measured down from the top
            : posFromAnchor.y;

        rt.anchoredPosition = new Vector2(posFromAnchor.x, y);
        rt.sizeDelta = size;
        EditorUtility.SetDirty(rt);
    }

    /// <summary>
    /// Puts the supplied plate behind an element.
    ///
    /// If the element already draws something of its own — the exit and volume buttons each carry a
    /// circle sprite — that graphic becomes the plate rather than getting a second image stacked
    /// behind it. Layering a rounded tab under a filled circle would just hide the tab, and it
    /// would also leave the Button's targetGraphic pointing at the wrong thing, so the press tint
    /// would land on an invisible object.
    /// </summary>
    private static void Backplate(RectTransform host, Sprite sprite, int padding)
    {
        if (host == null || sprite == null) return;

        Image own = host.GetComponent<Image>();
        if (own != null)
        {
            Undo.RecordObject(own, "Reference backplate");
            own.sprite = sprite;
            own.type = Image.Type.Simple;
            own.preserveAspect = true;
            own.color = Color.white;
            own.enabled = true;
            EditorUtility.SetDirty(own);

            // Drop a plate left behind by an earlier run, now that the host draws the tab itself.
            Transform stale = host.Find("Backplate");
            if (stale != null) Undo.DestroyObjectImmediate(stale.gameObject);

            Shadow(host.gameObject, false);
            return;
        }

        RectTransform plate = Child(host, "Backplate");
        Stretch(plate);
        plate.SetSiblingIndex(0);

        Image img = plate.GetComponent<Image>();
        if (img == null) img = plate.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = false;

        Shadow(plate.gameObject, false);
    }

    /// <summary>The reference's drop shadow: 0/4 offset, black at 40%.</summary>
    private static void Shadow(GameObject go, bool outline)
    {
        if (go == null) return;

        Shadow s = go.GetComponent<Shadow>();
        if (s == null) s = Undo.AddComponent<Shadow>(go);

        Undo.RecordObject(s, "Reference drop shadow");
        s.effectColor = ShadowColor;
        s.effectDistance = ShadowOffset;
        s.useGraphicAlpha = true;
        EditorUtility.SetDirty(s);
    }

    /// <summary>Imports as a plain sprite with no 9-slice border.</summary>
    private static void ImportSimple(string path)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) return;
        bool dirty = false;
        if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
        if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
        if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; dirty = true; }
        if (imp.spriteBorder != Vector4.zero) { imp.spriteBorder = Vector4.zero; dirty = true; }
        if (dirty) imp.SaveAndReimport();
    }

    private static void ImportNineSlice(string path, int border)
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
        {
            Debug.LogWarning($"[LudoMatchUIBuilder] Missing sprite {path}.");
            return;
        }

        if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) return;

        bool dirty = false;
        if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
        if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
        if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; dirty = true; }

        Vector4 want = new Vector4(border, border, border, border);
        if (imp.spriteBorder != want) { imp.spriteBorder = want; dirty = true; }

        if (dirty) imp.SaveAndReimport();
    }

    private static GameObject FindHudCanvas()
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.gameObject.name == "Canvas") return c.gameObject;
        }
        return null;
    }

    private static RectTransform Find(Transform root, string name)
    {
        if (root == null) return null;
        foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rt.gameObject.name == name) return rt;
        }
        return null;
    }

    private static RectTransform Child(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t is RectTransform existing) return existing;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rt)
    {
        if (rt == null) return;
        Undo.RecordObject(rt, "Stretch");
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rt);
    }

    private static Color Hex(byte r, byte g, byte b) => new Color32(r, g, b, 0xFF);
}
