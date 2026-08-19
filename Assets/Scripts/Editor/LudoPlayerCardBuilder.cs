using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Builds the four player cards from the Figma art in Assets/Art/UI/PlayerCards and wires every
/// PlayerCardUI field, then registers the cards with UIManager.
///
/// Layout follows the board UI sketch and the leaderboard reference: two cards above the board,
/// two below, avatar ring on the outer edge, name across the top band, and the live WIN% / POINTS
/// values sitting directly beneath the labels already baked into the card art.
///
/// Deterministic: every run deletes the children it owns and rebuilds them, so re-running always
/// converges on the same result no matter what state a previous run left behind.
/// </summary>
public static class LudoPlayerCardBuilder
{
    private struct CardSpec
    {
        public string objectName;
        public GameManager.PlayerColor color;
        public string colorKey;      // matches the sprite filename suffix
        public string demoName;
        public int level;
        public bool mirrored;        // true = card sits on the right, avatar on the right edge
        public Vector2 anchor;       // anchorMin/Max/pivot corner
        public Vector2 offset;
    }

    // Board quadrants: red top-left, green top-right, blue bottom-left, yellow bottom-right.
    private static readonly CardSpec[] Specs =
    {
        new CardSpec { objectName="Profile_TopLeft",     color=GameManager.PlayerColor.RED,    colorKey="red",    demoName="Josef",  level=3, mirrored=false, anchor=new Vector2(0f,1f), offset=new Vector2( 24f,-196f) },
        new CardSpec { objectName="Profile_TopRight",    color=GameManager.PlayerColor.GREEN,  colorKey="green",  demoName="Victor", level=6, mirrored=true,  anchor=new Vector2(1f,1f), offset=new Vector2(-24f,-196f) },
        new CardSpec { objectName="Profile_BottomLeft",  color=GameManager.PlayerColor.BLUE,   colorKey="blue",   demoName="Maryam", level=5, mirrored=false, anchor=new Vector2(0f,0f), offset=new Vector2( 24f, 356f) },
        new CardSpec { objectName="Profile_BottomRight", color=GameManager.PlayerColor.YELLOW, colorKey="yellow", demoName="Sofya",  level=3, mirrored=true,  anchor=new Vector2(1f,0f), offset=new Vector2(-24f, 356f) },
    };

    // All four cards render at the same HEIGHT with native aspect preserved, so nothing distorts.
    // The source PNGs are 1006-1060 px wide, so on-screen widths land within ~5% of each other.
    // Raised from 104 so the stat numbers can sit UNDER their labels and still be big enough to
    // read. At 104 the sub-panel was only ~40px tall, which forced the value alongside the label.
    // Two cards sit side by side across a 1080-wide reference canvas. The widest source art is
    // 4.08:1, so at 132 tall a pair spans 1076px plus insets and they collide in the middle.
    // 118 leaves a comfortable gutter between them.
    private const float CardHeight = 118f;
    private const float AvatarRingScale = 1.30f;   // ring overhangs the card, as in the art

    // Normalised positions measured off the exported PNGs.
    //
    // The stat sub-panels are a single line tall (y 0.45-0.84) and the baked labels are
    // left-aligned inside them: "WIN%" glyphs end at x 0.497 with the panel running on to 0.616,
    // and "POINTS" ends at 0.840 with the panel running to 0.954. That trailing gap is where the
    // value goes — stacking it underneath overlaps the label, because there is no room.
    // Measured off the exported PNGs. The stat sub-panels span y 0.45-0.84; the baked WIN% and
    // POINTS labels centre on y 0.60 with their glyph clusters centred at x 0.419 and x 0.751.
    // The values stack directly beneath, sized so they finish above the panel floor at 0.84.
    // Glyph-level measurements off card_red.png (1060x260), icons excluded:
    //   WIN%   text centres x 0.481, sits y 0.542-0.612
    //   POINTS text centres x 0.818, sits y 0.562-0.612
    //   stat sub-panels run y 0.454-0.838
    // That leaves y 0.612-0.838 for the value — 0.226 of card height. The value box has to be
    // taller than the glyph size or TMP auto-shrinks the text to fit its rect, which is what made
    // the numbers come out tiny.
    private const float NameCenterY = 0.215f;      // vertical centre of the top gloss band
    private const float StatValueY = 0.735f;       // centred in the gap beneath the labels
    private const float StatValueSize = 0.167f;    // fraction of card height
    private const float StatBoxHeight = 0.240f;    // must exceed StatValueSize * ~1.35
    private const float Stat1CenterX = 0.481f;     // directly under the WIN% label
    private const float Stat2CenterX = 0.818f;     // directly under the POINTS label

    // ---- Type system ----
    // Baloo 2 ExtraBold for anything numeric: rounded and heavy, so it sits on the glossy art
    // instead of floating over it. Nunito Bold for names. One ink colour across every card —
    // differentiate by typeface and weight, never by colour. Colour belongs to player identity.
    private const string DisplayFont = "Baloo2-ExtraBold";
    private const string BodyFont = "Nunito-Bold";

    // Warm near-black. Pure #000 looks like a hole punched in the card; this reads as printed ink.
    private static readonly Color Ink = new Color32(0x2A, 0x1E, 0x20, 0xFF);
    private static readonly Color InkSoft = new Color32(0x4A, 0x38, 0x3A, 0xFF);

    private static readonly string[] OwnedChildNames =
    {
        "GlowBorder", "Avatar_Ring", "Player_Name", "Points_Value", "WinRate_Value",
        "HomeDot_0", "HomeDot_1", "HomeDot_2", "HomeDot_3",
        "Player_Level", "WinRate", "Level_Badge", "Level_Star"
    };

    [MenuItem("Ludo NX/6. Build & Wire Player Cards")]
    public static void BuildPlayerCards()
    {
        UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("[LudoPlayerCardBuilder] No UIManager in the open scene. Open SampleScene and try again.");
            return;
        }

        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[LudoPlayerCardBuilder] Editor is in play mode — scene edits would be discarded on exit. Stop play mode first.");
            return;
        }

        TMP_FontAsset displayFont = FindFont(DisplayFont) ?? FindFont("LilitaOne-Regular");
        TMP_FontAsset bodyFont = FindFont(BodyFont) ?? FindFont("PlusJakartaSans-SemiBold");

        // The cards anchor to screen corners, so their container has to span the screen. It ships
        // as a 100x100 box pinned to the centre, which parked all four cards behind the board.
        GameObject panel = GameObject.Find("PlayerProfiles_Panel");
        if (panel != null)
        {
            RectTransform prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            prt.localScale = Vector3.one;
            EditorUtility.SetDirty(prt);
        }

        int built = 0;

        foreach (CardSpec spec in Specs)
        {
            GameObject cardGO = GameObject.Find(spec.objectName);
            if (cardGO == null)
            {
                Debug.LogWarning($"[LudoPlayerCardBuilder] '{spec.objectName}' not found — skipped.");
                continue;
            }

            PlayerCardUI card = cardGO.GetComponent<PlayerCardUI>();
            if (card == null) card = cardGO.AddComponent<PlayerCardUI>();

            PruneOwnedChildren(cardGO.transform);
            ClearCardReferences(card);

            ApplyCardBody(cardGO, spec);
            BuildCardContents(card, cardGO.transform, spec, displayFont, bodyFont);

            card.playerColor = spec.color;
            card.activeGlowColor = ColorFor(spec.color);
            card.inactiveColor = new Color(1f, 1f, 1f, 0.62f);
            card.SetPlayerName(spec.demoName);

            EditorUtility.SetDirty(card);
            RegisterWithUIManager(uiManager, spec.color, card);
            built++;
        }

        EditorUtility.SetDirty(uiManager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // No modal here on purpose: EditorUtility.DisplayDialog blocks Unity's main thread until
        // a human clicks it, which deadlocks menu-driven or automated invocation.
        Debug.Log($"[LudoPlayerCardBuilder] Built and wired {built} player cards. Save the scene to keep this.");
    }

    /// <summary>Card body sprite, sized to a uniform height with the art's own aspect preserved.</summary>
    private static void ApplyCardBody(GameObject cardGO, CardSpec spec)
    {
        Sprite body = LoadSprite($"Assets/Art/UI/PlayerCards/{Cap(spec.colorKey)}/card_{spec.colorKey}.png");

        Image img = cardGO.GetComponent<Image>();
        if (img == null) img = cardGO.AddComponent<Image>();
        img.sprite = body;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = false;

        RectTransform rt = cardGO.GetComponent<RectTransform>();
        rt.anchorMin = spec.anchor;
        rt.anchorMax = spec.anchor;
        rt.pivot = spec.anchor;

        float aspect = body != null ? body.rect.width / body.rect.height : 4f;
        rt.sizeDelta = new Vector2(CardHeight * aspect, CardHeight);
        rt.anchoredPosition = spec.offset;

        // No X-flip. The leaderboard reference keeps the avatar on the left for every card in both
        // columns, and mirroring around a right-edge pivot pushed the right-hand cards off-screen.
        rt.localScale = Vector3.one;
    }

    private static void BuildCardContents(PlayerCardUI card, Transform parent, CardSpec spec,
                                          TMP_FontAsset displayFont, TMP_FontAsset bodyFont)
    {
        string dir = $"Assets/Art/UI/PlayerCards/{Cap(spec.colorKey)}";
        RectTransform cardRT = parent.GetComponent<RectTransform>();
        float cardW = cardRT.sizeDelta.x;
        float cardH = cardRT.sizeDelta.y;

        // Plain AddComponent, not Undo.AddComponent: the undo-group collapse from the child
        // deletions above was silently rolling the component back, leaving this reference null.
        CanvasGroup group = parent.GetComponent<CanvasGroup>();
        if (group == null) group = parent.gameObject.AddComponent<CanvasGroup>();
        card.cardCanvasGroup = group;

        // --- glow behind the body, drives the active-turn signal ---
        Image glow = CreateImage("GlowBorder", parent, Color.white, LoadSprite($"{dir}/card_{spec.colorKey}.png"));
        RectTransform grt = glow.rectTransform;
        grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one;
        grt.offsetMin = new Vector2(-7f, -7f); grt.offsetMax = new Vector2(7f, 7f);
        glow.raycastTarget = false;
        glow.transform.SetAsFirstSibling();
        card.glowBorder = glow;

        // --- avatar ring on the leading edge, overhanging the card ---
        float ringSize = cardH * AvatarRingScale;
        Image ring = CreateImage("Avatar_Ring", parent, Color.white, LoadSprite($"{dir}/avatar_frame_{spec.colorKey}.png"));
        Place(ring.rectTransform, new Vector2(0f, 0.5f), new Vector2(ringSize * 0.44f, 0f), new Vector2(ringSize, ringSize));
        ring.raycastTarget = false;
        card.avatarBorder = ring;

        // Portrait sits under the ring art. No portrait assets exist yet, so this is a tinted
        // placeholder — the reference is wired so real art drops straight in.
        Image avatar = CreateImage("Avatar", ring.transform, PlaceholderTint(spec.color), UISprite());
        Place(avatar.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ringSize * 0.66f, ringSize * 0.66f));
        avatar.raycastTarget = false;
        avatar.transform.SetAsFirstSibling();   // behind the ring bevel
        card.avatarImage = avatar;

        // The ring doubles as the turn countdown. A donut sprite, not a filled disc — Knob is a
        // solid circle and radial-filling it just painted a white pie over the whole avatar.
        Image timer = CreateImage("Timer_Ring", ring.transform, new Color(1f, 1f, 1f, 0.92f), TimerRingSprite());
        Place(timer.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ringSize * 1.10f, ringSize * 1.10f));
        timer.type = Image.Type.Filled;
        timer.fillMethod = Image.FillMethod.Radial360;
        timer.fillOrigin = (int)Image.Origin360.Top;
        timer.fillClockwise = false;
        timer.fillAmount = 1f;
        timer.raycastTarget = false;
        card.timerBarFill = timer;

        // --- level star, bottom-outer corner of the ring ---
        float starSize = ringSize * 0.46f;
        Image star = CreateImage("Level_Star", ring.transform, Color.white, LoadSprite($"{dir}/level_star_{spec.colorKey}.png"));
        Place(star.rectTransform, new Vector2(0.82f, 0.14f), Vector2.zero, new Vector2(starSize, starSize));
        star.raycastTarget = false;
        star.rectTransform.localScale = Vector3.one;

        TextMeshProUGUI levelText = CreateText("Level_Text", star.transform, spec.level.ToString(),
                                               displayFont, starSize * 0.52f, TextAlignmentOptions.Center, Color.white);
        StretchFull(levelText.rectTransform);
        levelText.rectTransform.anchoredPosition = new Vector2(0f, -starSize * 0.04f);

        // --- text on the card body ---
        // The name occupies the top gloss band, which runs from just right of the avatar ring to
        // the card's right edge. Centre it in that band rather than left-aligning against the
        // ring, which left it visually crowded against the avatar.
        float nameLeft = ringSize * 0.94f;
        float nameRight = cardW * 0.965f;
        float nameWidth = nameRight - nameLeft;

        TextMeshProUGUI nameText = CreateText("Player_Name", parent, spec.demoName, bodyFont,
                                              cardH * 0.20f, TextAlignmentOptions.Center, Ink);
        nameText.fontStyle = FontStyles.Bold;
        RectTransform nrt = nameText.rectTransform;
        nrt.anchorMin = new Vector2(0f, 1f); nrt.anchorMax = new Vector2(0f, 1f);
        nrt.pivot = new Vector2(0.5f, 0.5f);
        nrt.sizeDelta = new Vector2(nameWidth, cardH * 0.26f);
        nrt.anchoredPosition = new Vector2(nameLeft + nameWidth * 0.5f, -cardH * NameCenterY);
        card.playerNameText = nameText;

        card.winRateText = CreateStatValue("WinRate_Value", parent, "0%", displayFont, cardW, cardH,
                                           Stat1CenterX, InkSoft);
        card.pointsText = CreateStatValue("Points_Value", parent, "0", displayFont, cardW, cardH,
                                          Stat2CenterX, Ink);

        // --- home-token pips, tucked under the name ---
        Image[] dots = new Image[4];
        float dotSize = cardH * 0.11f;
        for (int i = 0; i < 4; i++)
        {
            Image dot = CreateImage($"HomeDot_{i}", parent, new Color(0f, 0f, 0f, 0.20f), Knob());
            RectTransform drt = dot.rectTransform;
            drt.anchorMin = new Vector2(0f, 1f); drt.anchorMax = new Vector2(0f, 1f);
            drt.pivot = new Vector2(0f, 0.5f);
            drt.sizeDelta = new Vector2(dotSize, dotSize);
            drt.anchoredPosition = new Vector2(nameLeft + i * (dotSize * 1.45f), -cardH * 0.50f);
            dot.raycastTarget = false;
            dots[i] = dot;
        }
        card.homeTokenDots = dots;
    }

    /// <summary>A stat number centred in its sub-panel, sitting just below the baked label.</summary>
    private static TextMeshProUGUI CreateStatValue(string name, Transform parent, string content,
                                                   TMP_FontAsset font, float cardW, float cardH,
                                                   float centerX, Color color)
    {
        TextMeshProUGUI t = CreateText(name, parent, content, font, cardH * StatValueSize,
                                       TextAlignmentOptions.Center, color);
        t.fontStyle = FontStyles.Bold;
        // Fixed size, not auto-sizing: TMP shrinks to fit its rect, and a rect only as tall as the
        // glyphs collapsed these numbers to half the intended size. The box below is deliberately
        // taller than the type, and wide enough for a four-digit score.
        t.enableAutoSizing = false;
        t.overflowMode = TextOverflowModes.Overflow;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(cardW * 0.21f, cardH * StatBoxHeight);
        rt.anchoredPosition = new Vector2(cardW * centerX, -cardH * StatValueY);
        return t;
    }

    private static void PruneOwnedChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (System.Array.IndexOf(OwnedChildNames, child.name) >= 0)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void ClearCardReferences(PlayerCardUI card)
    {
        card.avatarImage = null;
        card.avatarBorder = null;
        card.playerNameText = null;
        card.pointsText = null;
        card.winRateText = null;
        card.homeTokenDots = null;
        card.timerBarFill = null;
        card.glowBorder = null;
    }

    private static void RegisterWithUIManager(UIManager ui, GameManager.PlayerColor color, PlayerCardUI card)
    {
        switch (color)
        {
            case GameManager.PlayerColor.RED: ui.redPlayerCard = card; break;
            case GameManager.PlayerColor.GREEN: ui.greenPlayerCard = card; break;
            case GameManager.PlayerColor.BLUE: ui.bluePlayerCard = card; break;
            case GameManager.PlayerColor.YELLOW: ui.yellowPlayerCard = card; break;
        }
    }

    // ---------- helpers ----------

    private static string Cap(string s) => char.ToUpper(s[0]) + s.Substring(1);

    /// <summary>
    /// The house colour, used for the active-turn halo and the filled home pips. Pulled a little
    /// off the raw board colours so it sits against the pale card body without vibrating.
    /// </summary>
    private static Color ColorFor(GameManager.PlayerColor c) => c switch
    {
        GameManager.PlayerColor.RED => new Color32(0xE1, 0x55, 0x4F, 0xFF),
        GameManager.PlayerColor.GREEN => new Color32(0x35, 0x9E, 0x63, 0xFF),
        GameManager.PlayerColor.YELLOW => new Color32(0xD9, 0xA3, 0x22, 0xFF),
        GameManager.PlayerColor.BLUE => new Color32(0x3D, 0x83, 0xC7, 0xFF),
        _ => Color.white
    };

    private static Color PlaceholderTint(GameManager.PlayerColor c) => c switch
    {
        GameManager.PlayerColor.RED => new Color(0.42f, 0.16f, 0.16f),
        GameManager.PlayerColor.GREEN => new Color(0.15f, 0.33f, 0.21f),
        GameManager.PlayerColor.YELLOW => new Color(0.40f, 0.31f, 0.10f),
        GameManager.PlayerColor.BLUE => new Color(0.14f, 0.24f, 0.42f),
        _ => Color.gray
    };

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning($"[LudoPlayerCardBuilder] Sprite not found: {path}");
        return s;
    }

    private static Sprite Knob() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    private static Sprite UISprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    private const string RingPath = "Assets/Art/UI/Generated/ring_timer.png";

    /// <summary>
    /// A crisp anti-aliased donut, generated once and cached as a project asset. Unity ships no
    /// built-in ring sprite, and a radial-filled solid circle reads as a pie chart rather than a
    /// countdown ring.
    /// </summary>
    private static Sprite TimerRingSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
        if (existing != null) return existing;

        const int size = 256;
        const float outer = 0.50f;
        const float inner = 0.395f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        float half = size * 0.5f;
        float aa = 1.2f / size;   // roughly one pixel of feathering

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / size;
                float dy = (y + 0.5f - half) / size;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float a = Mathf.Clamp01((outer - d) / aa) * Mathf.Clamp01((d - inner) / aa);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(RingPath));
        System.IO.File.WriteAllBytes(RingPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(RingPath, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter imp = AssetImporter.GetAtPath(RingPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            TextureImporterSettings s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
    }

    private static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        return img;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string content,
                                              TMP_FontAsset font, float size,
                                              TextAlignmentOptions align, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        if (font != null) text.font = font;
        return text;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
}
