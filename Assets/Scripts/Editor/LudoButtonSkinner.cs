using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Applies the Figma button art to the HUD and removes the duplicate Roll_Button.
///
/// The exported set is half-composed: chat and emoji already have their icons baked onto a blue
/// circle, while sound and exit are bare icons with no base. Those two are therefore layered over
/// btn_circle here as children, so all four read as the same family without needing a re-export.
///
/// Re-runnable: the composed icon child is recreated each run.
/// </summary>
public static class LudoButtonSkinner
{
    private const string Buttons = "Assets/Art/UI/Buttons";
    private const string Icons = "Assets/Art/UI/Icons";
    private const string IconChild = "Icon";

    [MenuItem("Ludo NX/7. Skin Buttons & Icons")]
    public static void SkinButtons()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[LudoButtonSkinner] Editor is in play mode — changes would be discarded. Stop play mode first.");
            return;
        }

        int done = 0;

        // Pre-composed: the sprite already contains its icon.
        done += SkinPlain("Chat_Button", $"{Buttons}/btn_chat.png", 96f);
        done += SkinPlain("Emoji_Button", $"{Buttons}/btn_emoji.png", 96f);

        // Bare icons layered over the shared circular base.
        done += SkinComposed("Audio_Button", $"{Buttons}/btn_circle.png", $"{Icons}/icon_sound_on.png", 96f, 0.52f);
        AddMuteStrike("Audio_Button", 96f);
        done += SkinComposed("Exit_Button", $"{Buttons}/btn_circle.png", $"{Icons}/icon_exit.png", 96f, 0.48f);

        // The roll button keeps its existing behaviour; only its look changes, so it stops
        // reading as a bare orange rectangle.
        done += SkinRollButton();

        RemoveDuplicateRollButton();
        LayoutBottomBar();
        ApplyHudTypography();
        AttachCameraFramer();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[LudoButtonSkinner] Skinned {done} button(s). Save the scene to keep this.");
    }

    private static int SkinPlain(string objectName, string spritePath, float size)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null) { Debug.LogWarning($"[LudoButtonSkinner] '{objectName}' not found."); return 0; }

        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();

        Sprite sprite = Load(spritePath);
        if (sprite == null) return 0;

        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.preserveAspect = true;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);

        ClearIconChild(go.transform);
        EditorUtility.SetDirty(go);
        return 1;
    }

    private static int SkinComposed(string objectName, string basePath, string iconPath, float size, float iconScale)
    {
        if (SkinPlain(objectName, basePath, size) == 0) return 0;

        GameObject go = GameObject.Find(objectName);
        Sprite icon = Load(iconPath);
        if (icon == null) return 0;

        GameObject child = new GameObject(IconChild, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(go.transform, false);

        Image img = child.GetComponent<Image>();
        img.sprite = icon;
        img.preserveAspect = true;   // the icons have inconsistent aspect ratios
        img.raycastTarget = false;

        RectTransform rt = child.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size * iconScale, size * iconScale);

        EditorUtility.SetDirty(go);
        return 1;
    }

    // The pill's rounded caps are 208 px wide in the 2160x720 export. The 9-slice border has to
    // clear them or the caps get caught in the stretching region and smear.
    private const string RollSpritePath = Buttons + "/btn_roll_cyan.png";
    private const int RollSliceX = 224;
    private const int RollMaxTextureSize = 2048;
    private static readonly Vector2 RollButtonSize = new Vector2(360f, 120f);

    // Deep teal-navy — same hue family as the button so it reads as part of it, dark enough to
    // clear ~7.5:1 against the cyan face.
    private static readonly Color RollInk = new Color32(0x06, 0x33, 0x40, 0xFF);

    /// <summary>
    /// A diagonal bar across the speaker, hidden until the player mutes. Built here rather than
    /// exported because the icon set has no muted variant, and a drawn strike stays perfectly
    /// aligned to whatever icon sits underneath it.
    /// </summary>
    private static void AddMuteStrike(string objectName, float buttonSize)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null) return;

        Transform existing = go.transform.Find(UIManager.SoundStrikeName);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Dark under-stroke first, then the light bar on top, so the strike stays legible
        // whichever way the icon beneath it is coloured.
        CreateStrikeBar(go.transform, buttonSize, 9f, new Color(0.05f, 0.09f, 0.13f, 0.85f), "StrikeShadow");
        GameObject bar = CreateStrikeBar(go.transform, buttonSize, 5f, new Color(1f, 1f, 1f, 0.92f), UIManager.SoundStrikeName);

        // The shadow lives inside the strike so both toggle together.
        Transform shadow = go.transform.Find("StrikeShadow");
        if (shadow != null)
        {
            shadow.SetParent(bar.transform, false);
            RectTransform srt = shadow.GetComponent<RectTransform>();
            srt.anchoredPosition = Vector2.zero;
            srt.localRotation = Quaternion.identity;
            srt.sizeDelta = new Vector2(buttonSize * 0.72f, 9f);
        }

        bar.SetActive(false);
    }

    private static GameObject CreateStrikeBar(Transform parent, float buttonSize, float thickness, Color color, string name)
    {
        GameObject bar = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(parent, false);

        Image img = bar.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(buttonSize * 0.72f, thickness);
        rt.localRotation = Quaternion.Euler(0f, 0f, -45f);

        return bar;
    }

    private static int SkinRollButton()
    {
        if (!ConfigureRollSpriteImport()) return 0;

        UIManager ui = Object.FindAnyObjectByType<UIManager>();
        Button target = ui != null ? ui.rollButton : null;

        GameObject go = target != null ? target.gameObject : GameObject.Find("Roll_Button");
        if (go == null) { Debug.LogWarning("[LudoButtonSkinner] No Roll_Button found."); return 0; }

        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();

        Sprite sprite = Load(RollSpritePath);
        if (sprite == null) return 0;

        img.sprite = sprite;
        img.type = Image.Type.Sliced;     // caps stay fixed, the middle stretches
        img.fillCenter = true;
        img.color = Color.white;
        img.preserveAspect = false;

        // 100 was the old height; 120 clears the 48dp comfortable touch minimum on a real phone.
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = RollButtonSize;

        ApplyRollLabel(go);

        EditorUtility.SetDirty(go);
        return 1;
    }

    /// <summary>
    /// The ROLL label.
    ///
    /// Contrast is the whole decision here. The cyan face sits at roughly 55% relative luminance,
    /// so white text scores about 1.8:1 against it — unreadable, and the usual fix (a heavy dark
    /// outline) is exactly the noise we're trying to avoid. Deep teal-navy ink scores about 7.5:1
    /// on the same face, clears WCAG AA comfortably at this size, and reads as moulded into the
    /// button rather than stuck on top of it. No outline needed.
    /// </summary>
    private static void ApplyRollLabel(GameObject buttonGO)
    {
        foreach (TextMeshProUGUI stale in buttonGO.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            Object.DestroyImmediate(stale.gameObject);
        }

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(buttonGO.transform, false);

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "ROLL";
        label.font = FindFont("Baloo2-ExtraBold") ?? FindFont("LilitaOne-Regular");
        label.fontSize = 46f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = RollInk;
        label.characterSpacing = 8f;      // a touch of tracking so a 4-letter word fills the pill
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        // Nudge up a hair: the pill's lower bevel is darker, so optical centre sits above true centre.
        lrt.anchoredPosition = new Vector2(0f, 3f);
    }

    /// <summary>
    /// Puts the chat and emoji buttons on the same baseline as the roll button.
    ///
    /// They were parented to BottomBar_Panel — a 1.5-unit box pinned to screen centre — while the
    /// roll button hangs off the Canvas anchored bottom-centre. Two different anchor systems meant
    /// they could never line up, and on a short screen the pair fell off the bottom edge entirely.
    /// Re-anchoring them bottom-centre alongside the roll button fixes both at once.
    /// </summary>
    private static void LayoutBottomBar()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        UIManager ui = Object.FindAnyObjectByType<UIManager>();
        GameObject roll = ui != null && ui.rollButton != null ? ui.rollButton.gameObject : GameObject.Find("Roll_Button");
        if (roll == null) return;

        RectTransform rollRT = roll.GetComponent<RectTransform>();

        // anchoredPosition is measured from the pivot, so two rects sharing a Y value do not share
        // a centre unless their pivots match. Convert to the roll button's true centre and left
        // edge first — its pivot is not (0.5, 0.5).
        float baselineY = rollRT.anchoredPosition.y + (0.5f - rollRT.pivot.y) * rollRT.sizeDelta.y;
        float rollCentreX = rollRT.anchoredPosition.x + (0.5f - rollRT.pivot.x) * rollRT.sizeDelta.x;
        float rollLeftEdge = rollCentreX - rollRT.sizeDelta.x * 0.5f;

        // Sit outside the roll button's left edge with an even gutter between them.
        const float side = 96f;
        const float gutter = 22f;

        Place("Emoji_Button", canvas.transform, rollLeftEdge - gutter - side * 0.5f, baselineY, side);
        Place("Chat_Button", canvas.transform, rollLeftEdge - gutter * 2f - side * 1.5f, baselineY, side);
    }

    private static void Place(string objectName, Transform canvas, float x, float y, float size)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null) return;

        // Reparent to the Canvas so it shares the roll button's anchor space.
        go.transform.SetParent(canvas, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(x, y);
        rt.localScale = Vector3.one;

        EditorUtility.SetDirty(go);
    }

    /// <summary>
    /// Brings the remaining HUD text onto the project type system. The countdown was still on
    /// TMP's default LiberationSans while the cards had already moved to Baloo 2 / Nunito.
    /// </summary>
    private static void ApplyHudTypography()
    {
        TMP_FontAsset display = FindFont("Baloo2-ExtraBold");
        if (display == null) return;

        UIManager ui = Object.FindAnyObjectByType<UIManager>();

        GameObject timerGO = GameObject.Find("Timer_Text");
        if (timerGO != null)
        {
            TextMeshProUGUI timer = timerGO.GetComponent<TextMeshProUGUI>();
            if (timer != null)
            {
                timer.font = display;
                timer.fontSize = 56f;                 // the most time-critical element on screen
                timer.characterSpacing = 2f;
                timer.alignment = TextAlignmentOptions.Center;
                timer.enableWordWrapping = false;
                EditorUtility.SetDirty(timer);
            }
        }

        if (ui != null && ui.turnStatusText != null)
        {
            ui.turnStatusText.font = display;
            EditorUtility.SetDirty(ui.turnStatusText);
        }
    }

    /// <summary>
    /// Adds the framer to the main camera so the board fills the frame without the waypoints
    /// drifting out of alignment.
    /// </summary>
    private static void AttachCameraFramer()
    {
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[LudoButtonSkinner] No MainCamera to frame with."); return; }

        BoardCameraFramer framer = cam.GetComponent<BoardCameraFramer>();
        if (framer == null) framer = cam.gameObject.AddComponent<BoardCameraFramer>();
        framer.Frame();

        EditorUtility.SetDirty(cam);
        Debug.Log($"[LudoButtonSkinner] Board framed — orthographicSize now {cam.orthographicSize:F2}.");
    }

    private static TMP_FontAsset FindFont(string nameContains)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains(nameContains)) return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
        return null;
    }

    /// <summary>
    /// The roll button is the one sprite that must survive being stretched, so it needs its
    /// border set and a higher max size than the rest of the UI atlas.
    /// </summary>
    private static bool ConfigureRollSpriteImport()
    {
        TextureImporter imp = AssetImporter.GetAtPath(RollSpritePath) as TextureImporter;
        if (imp == null)
        {
            Debug.LogWarning($"[LudoButtonSkinner] No importer at {RollSpritePath} — is the file in the project?");
            return false;
        }

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.filterMode = FilterMode.Bilinear;
        imp.wrapMode = TextureWrapMode.Clamp;

        // Vector4 order is (left, bottom, right, top).
        imp.spriteBorder = new Vector4(RollSliceX, 0f, RollSliceX, 0f);

        TextureImporterSettings s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;   // required for slicing
        s.spriteAlignment = (int)SpriteAlignment.Center;
        imp.SetTextureSettings(s);

        TextureImporterPlatformSettings plat = imp.GetDefaultPlatformTextureSettings();
        plat.maxTextureSize = RollMaxTextureSize;
        plat.textureCompression = TextureImporterCompression.CompressedHQ;
        plat.compressionQuality = 100;
        imp.SetPlatformTextureSettings(plat);

        imp.SaveAndReimport();
        return true;
    }

    /// <summary>
    /// Two Roll_Buttons ship in the scene — one under Canvas, one under BottomBar_Panel. The
    /// stray one is invisible dead weight that can still intercept taps. Keep whichever UIManager
    /// actually references.
    /// </summary>
    private static void RemoveDuplicateRollButton()
    {
        UIManager ui = Object.FindAnyObjectByType<UIManager>();
        GameObject keep = ui != null && ui.rollButton != null ? ui.rollButton.gameObject : null;

        Button[] all = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int removed = 0;

        foreach (Button b in all)
        {
            if (b == null || b.gameObject.name != "Roll_Button") continue;
            if (keep != null && b.gameObject == keep) continue;

            Debug.Log($"[LudoButtonSkinner] Removing duplicate Roll_Button under '{b.transform.parent?.name}'.");
            Object.DestroyImmediate(b.gameObject);
            removed++;
        }

        if (removed == 0) Debug.Log("[LudoButtonSkinner] No duplicate Roll_Button found.");
    }

    private static void ClearIconChild(Transform parent)
    {
        Transform existing = parent.Find(IconChild);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
    }

    private static Sprite Load(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning($"[LudoButtonSkinner] Sprite not found: {path}");
        return s;
    }
}
