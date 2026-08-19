using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot authoring for the Ludo NX interface.
///
/// This is scaffolding, not a pipeline. It writes each screen out as a real prefab and then
/// gets out of the way â€” the prefabs are the source of truth from that point on, and you can
/// open and edit them like any other prefab. That is the whole difference from the old
/// <c>Ludo*Builder</c> scripts, where the scene was regenerated from code every time and any
/// hand edit was lost.
///
/// Re-running a builder overwrites that screen's prefab, so only do it if you want to discard
/// hand edits to that screen.
/// </summary>
public static class LudoNXScreenAuthor
{
    private const string ResRoot = "Assets/Resources";
    private const string ScreenDir = ResRoot + "/UI/Screens";
    private const string ModalDir = ResRoot + "/UI/Modals";
    private const string FontDir = "Assets/Fonts";

    // â”€â”€ Setup â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/0. Create Theme + Font Set", priority = 0)]
    public static void CreateSupportAssets()
    {
        EnsureFolder(ResRoot);
        EnsureFolder(ResRoot + "/UI");
        EnsureFolder(ScreenDir);
        EnsureFolder(ModalDir);

        // Theme
        var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ResRoot + "/UITheme.asset");
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<UITheme>();
            AssetDatabase.CreateAsset(theme, ResRoot + "/UITheme.asset");
        }

        // Font set â€” references the TMP assets in Assets/Fonts without copying them.
        var fonts = AssetDatabase.LoadAssetAtPath<UIFontSet>(ResRoot + "/UIFontSet.asset");
        if (fonts == null)
        {
            fonts = ScriptableObject.CreateInstance<UIFontSet>();
            AssetDatabase.CreateAsset(fonts, ResRoot + "/UIFontSet.asset");
        }
        fonts.display = LoadFont("Baloo2-ExtraBold");
        fonts.body = LoadFont("Nunito-Bold");
        fonts.bodyLight = LoadFont("Nunito-SemiBold");
        fonts.numeric = LoadFont("PlusJakartaSans-Bold");
        EditorUtility.SetDirty(fonts);

        // Icon set â€” the star from the board's safe zones, reused for Safe Zone mode so the
        // menu and the board mean the same thing by the same symbol.
        var icons = AssetDatabase.LoadAssetAtPath<UIIconSet>(ResRoot + "/UIIconSet.asset");
        if (icons == null)
        {
            icons = ScriptableObject.CreateInstance<UIIconSet>();
            AssetDatabase.CreateAsset(icons, ResRoot + "/UIIconSet.asset");
        }
        icons.star = LoadAsSprite("Assets/art_assets/level start badge alone.png");
        icons.pawns = new[]
        {
            LoadAsSprite("Assets/art_assets/Pawn_Master_Red.png"),
            LoadAsSprite("Assets/art_assets/Pawn_Master_Green.png"),
            LoadAsSprite("Assets/art_assets/Pawn_Master_yellow.png"),
            LoadAsSprite("Assets/art_assets/Pawn_Master_Blue.png")
        };
        icons.diceLogo = LoadAsSprite("Assets/Art/UI/Loader/DiceLogo.png");
        icons.loaderLine = LoadAsSprite("Assets/Art/UI/Loader/LoaderLine.png");
        icons.softGlow = LoadAsSprite("Assets/art_assets/generated/ui_glow.png");

        LoadIconLibrary(icons);
        LoadAvatars(icons);
        EditorUtility.SetDirty(icons);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LudoNX UI] Support assets ready.  display=" + (fonts.display != null) +
                  " body=" + (fonts.body != null) + " numeric=" + (fonts.numeric != null) +
                  " star=" + (icons.star != null) +
                  " pawns=" + System.Array.FindAll(icons.pawns, s => s != null).Length + "/4");
    }


    /// <summary>
    /// Reads every SVG in the icon folder into the set, keyed by filename.
    ///
    /// Vector rather than bitmap on purpose: one file serves every size, the colour is set in
    /// code, and the whole library is a few tens of kilobytes.
    /// </summary>
    private static void LoadIconLibrary(UIIconSet icons)
    {
        const string dir = "Assets/Art/UI/Icons";
        if (!AssetDatabase.IsValidFolder(dir)) { Debug.LogWarning("[LudoNX UI] No " + dir); return; }

        var names = new System.Collections.Generic.List<string>();
        var sprites = new System.Collections.Generic.List<Sprite>();

        // Enumerate the files rather than using AssetDatabase.FindAssets("t:Sprite"): an SVG is
        // brought in by a ScriptedImporter, and the type filter does not match what it produces.
        string absolute = Path.Combine(Directory.GetParent(Application.dataPath).FullName, dir);
        if (!Directory.Exists(absolute)) { Debug.LogWarning("[LudoNX UI] No " + dir); return; }

        foreach (string file in Directory.GetFiles(absolute, "*.svg", SearchOption.TopDirectoryOnly))
        {
            string path = dir + "/" + Path.GetFileName(file);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                // Some SVG import modes expose the sprite as a sub-asset rather than the main one.
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    sprite = sub as Sprite;
                    if (sprite != null) break;
                }
            }
            if (sprite == null) continue;
            names.Add(Path.GetFileNameWithoutExtension(path));
            sprites.Add(sprite);
        }

        icons.iconNames = names.ToArray();
        icons.iconSprites = sprites.ToArray();
        Debug.Log("[LudoNX UI] icon library: " + names.Count + " icons");
    }

    /// <summary>Imports the profile pictures. Square is fine â€” the slot masks them to a circle.</summary>
    private static void LoadAvatars(UIIconSet icons)
    {
        const string dir = "Assets/Art/UI/Avatars";
        if (!AssetDatabase.IsValidFolder(dir)) return;

        var list = new System.Collections.Generic.List<Sprite>();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = LoadAsSprite(path);
            if (sprite != null) list.Add(sprite);
        }
        icons.avatars = list.ToArray();
        Debug.Log("[LudoNX UI] avatars: " + list.Count);
    }

    /// <summary>
    /// Loads a texture as a Sprite, correcting its import settings if needed.
    ///
    /// <c>sRGBTexture</c> matters more than it looks. The project renders in Linear colour
    /// space; with the flag off, Unity treats the file's bytes as already-linear and the GPU
    /// skips the sRGBâ†’linear conversion on sample, so every artwork renders visibly lighter.
    /// Measured on the die logo: source #FA4244 arriving on screen as #F4B5B7.
    /// </summary>
    private static Sprite LoadAsSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            changed = true;
        }
        if (!importer.sRGBTexture) { importer.sRGBTexture = true; changed = true; }
        if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }

        // Interface art is small and sits still on screen, where DXT block artefacts are very
        // visible â€” the die logo is 232px of smooth gradient, exactly the worst case for it.
        // Uncompressed costs a couple of hundred KB and looks like the source file.
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }
        if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }

        if (changed) importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static TMP_FontAsset LoadFont(string n) =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "/" + n + ".asset");

    // â”€â”€ Home â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/1. Build Home Screen", priority = 1)]
    public static void BuildHome()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("Home", ScreenId.Home);
        var home = root.AddComponent<HomeScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        // â”€â”€ Top bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var avatar = UIFactory.CircleButton("Avatar", p, 112f, new Vector2(-424f, 806f), T.panelHigh);
        var avatarInner = UIFactory.Clay("AvatarInner", avatar.content, new Vector2(84f, 84f),
                                         Vector2.zero, T.secondary, ClayGraphic.Shape.Circle);
        avatarInner.raycastTarget = false;
        avatarInner.shadowAlpha = 0f;
        avatar.gameObject.AddComponent<ProfileAvatarSlot>().diameter = 84f;

        var nameLabel = UIFactory.Body("PlayerName", p, new Vector2(420f, 60f), new Vector2(-130f, 806f),
                                       "Player", T.ink, UIFontSet.SizeBody);
        nameLabel.alignment = TextAlignmentOptions.Left;

        var settings = UIFactory.CircleButton("SettingsButton", p, 100f, new Vector2(432f, 806f), T.neutral);
        UIFactory.Icon("GearIcon", settings.content, new Vector2(50f, 50f), Vector2.zero,
                       UIIconSet.Icon("settings"), T.InkOn(T.neutral));

        // â”€â”€ Logo mark: the same die and disc as the loader, so the brand is one thing â”€â”€â”€â”€â”€
        var icons = UIIconSet.Current;
        var disc = UIFactory.Clay("LogoDisc", p, new Vector2(300f, 300f), new Vector2(0f, 452f),
                                  Color.white, ClayGraphic.Shape.Circle);
        disc.raycastTarget = false;
        disc.shadowSpread = 46f;
        disc.shadowAlpha = 0.10f;
        disc.innerHighlight = 0.10f;
        disc.innerShadow = 0.04f;
        disc.gradientStrength = 0.02f;
        if (icons != null && icons.diceLogo != null)
            UIFactory.Icon("Dice", disc.transform, new Vector2(232f, 232f), Vector2.zero,
                           icons.diceLogo, Color.white);

        // â”€â”€ Wordmark â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var word = UIFactory.Label("Wordmark", p, new Vector2(1000f, 130f), new Vector2(0f, 244f),
                                   "Ludo NX", UIFontSet.Current.numeric, 86f, T.primary);
        word.characterSpacing = -2f;
        UIFactory.Label("Tagline", p, new Vector2(1000f, 60f), new Vector2(0f, 160f),
                        "Every capture counts", UIFontSet.Current.bodyLight, 36f, T.inkMuted);

        // â”€â”€ Hero action â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var play = UIFactory.Button("PlayButton", p, new Vector2(840f, 208f), new Vector2(0f, -46f),
                                    T.primary, "PLAY", 54f, ClayGraphic.Shape.Pill, glow: 0.16f, hero: true);
        play.Clay.glowSpread = 40f;

        // No PROFILE or STATS buttons here. The avatar in the top bar is already the route to
        // the profile, and statistics now live inside it â€” a second button to the same screen
        // is just a wider tap target for a place the player can already reach.

        // â”€â”€ Locked teaser â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var tour = UIFactory.Button("TournamentsButton", p, new Vector2(840f, 148f), new Vector2(0f, -320f),
                                    T.locked, null, shape: ClayGraphic.Shape.Panel);
        tour.Clay.cornerRadius = T.radiusLg;
        tour.Clay.shadowAlpha = 0.18f;
        var tourLabel = UIFactory.Body("Label", tour.content, new Vector2(560f, 60f), new Vector2(-96f, 0f),
                                       "TOURNAMENTS", T.inkDim, 32f);
        tourLabel.alignment = TextAlignmentOptions.Left;
        tourLabel.characterSpacing = 4f;

        var soonPill = UIFactory.Clay("SoonPill", tour.content, new Vector2(168f, 66f), new Vector2(292f, 0f),
                                      T.gold, ClayGraphic.Shape.Pill);
        soonPill.raycastTarget = false;
        soonPill.shadowAlpha = 0.2f;
        var soon = UIFactory.Body("SoonLabel", soonPill.transform, new Vector2(168f, 66f), Vector2.zero,
                                  "SOON", T.groundTop, 26f);
        soon.characterSpacing = 6f;
        soon.fontStyle = FontStyles.Bold;

        // â”€â”€ Wire â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        home.playButton = play;
        home.settingsButton = settings;
        home.avatarButton = avatar;
        home.tournamentsButton = tour;
        home.playerNameLabel = nameLabel;

        SavePrefab(root, ScreenDir + "/Home.prefab");
    }

    private static void MarkTile(Transform parent, string name, Vector2 pos, float size, Color c)
    {
        var g = UIFactory.Clay(name, parent, new Vector2(size, size), pos, c, ClayGraphic.Shape.Panel, 30f);
        g.raycastTarget = false;
        g.shadowSpread = 18f;
        g.shadowAlpha = 0.22f;
        g.innerHighlight = 0.30f;
        g.innerShadow = 0.34f;
    }

    /// <summary>A gear drawn from clay pieces â€” no icon font needed yet.</summary>
    private static void BuildGear(Transform parent, Color c)
    {
        for (int i = 0; i < 3; i++)
        {
            var tooth = UIFactory.Clay("Tooth" + i, parent, new Vector2(56f, 18f), Vector2.zero, c,
                                       ClayGraphic.Shape.Pill);
            tooth.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
            tooth.raycastTarget = false;
            tooth.shadowAlpha = 0f;
            tooth.innerHighlight = 0f;
            tooth.innerShadow = 0f;
        }
        var hub = UIFactory.Clay("GearHub", parent, new Vector2(22f, 22f), Vector2.zero,
                                 UITheme.Current.neutral, ClayGraphic.Shape.Circle);
        hub.raycastTarget = false;
        hub.shadowAlpha = 0f;
        hub.innerHighlight = 0f;
        hub.innerShadow = 0f;
    }

    // â”€â”€ Play menu â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/2. Build Play Menu", priority = 2)]
    public static void BuildPlayMenu()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("PlayMenu", ScreenId.PlayMenu);
        var screen = root.AddComponent<PlayMenuScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);
        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "PLAY", out title);

        // â”€â”€ Offline, playable â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        screen.quickPlayCard = ModeCard(p, "QuickPlayCard", "QUICK PLAY",
                                        "Play against the computer.",
                                        new Vector2(0f, 480f), T.primary, T, false);
        screen.passAndPlayCard = ModeCard(p, "PassAndPlayCard", "PLAY WITH FRIENDS",
                                          "Four players, one device. No computer.",
                                          new Vector2(0f, 210f), T.secondary, T, false);

        // â”€â”€ Online, locked for this release â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        UIFactory.Label("OnlineHeader", p, new Vector2(880f, 44f), new Vector2(0f, 40f), "ONLINE",
                        UIFontSet.Current.body, 26f, T.inkDim, TextAlignmentOptions.Left).characterSpacing = 8f;

        screen.onlineRandomCard = ModeCard(p, "OnlineRandomCard", "PLAY ONLINE",
                                           "Match with random players worldwide.",
                                           new Vector2(0f, -110f), T.locked, T, true);
        screen.onlineFriendsCard = ModeCard(p, "OnlineFriendsCard", "PLAY WITH FRIENDS ONLINE",
                                            "Invite people from your friends list.",
                                            new Vector2(0f, -380f), T.locked, T, true);

        SavePrefab(root, ScreenDir + "/PlayMenu.prefab");
    }

    /// <summary>A mode tile. Locked ones keep their place in the list but read as unavailable.</summary>
    private static ClayButton ModeCard(Transform p, string name, string title, string desc,
                                       Vector2 pos, Color fill, UITheme T, bool locked)
    {
        var card = UIFactory.Button(name, p, new Vector2(880f, 240f), pos, fill, null,
                                    shape: ClayGraphic.Shape.Panel, glow: locked ? 0f : 0.10f);
        card.Clay.cornerRadius = T.radiusLg;
        card.Clay.glowSpread = 34f;
        if (locked) card.Clay.shadowAlpha = 0.10f;

        Color ink = locked ? T.inkDim : T.InkOn(fill);

        var t = UIFactory.Label("Name", card.content, new Vector2(680f, 56f), new Vector2(-60f, 40f),
                                title, UIFontSet.Current.body, 38f, ink, TextAlignmentOptions.Left);
        t.characterSpacing = 2f;
        t.fontStyle = FontStyles.Bold;

        UIFactory.Label("Desc", card.content, new Vector2(680f, 50f), new Vector2(-60f, -18f),
                        desc, UIFontSet.Current.bodyLight, 28f,
                        locked ? T.inkDim : ink, TextAlignmentOptions.Left);

        if (locked)
        {
            var pill = UIFactory.Clay("SoonPill", card.content, new Vector2(160f, 58f),
                                      new Vector2(320f, 40f), T.neutralDeep, ClayGraphic.Shape.Pill);
            pill.raycastTarget = false;
            pill.shadowAlpha = 0f;
            var pl = UIFactory.Body("Label", pill.transform, new Vector2(160f, 58f), Vector2.zero,
                                    "SOON", T.inkDim, 24f);
            pl.characterSpacing = 4f;
        }
        return card;
    }

    // â”€â”€ Quick play setup â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/3. Build Quick Play Setup", priority = 3)]
    public static void BuildQuickPlaySetup()
    {
        CreateSupportAssets();
        var T = UITheme.Current;
        var icons = UIIconSet.Current;

        GameObject root = NewScreenRoot("QuickPlaySetup", ScreenId.QuickPlaySetup);
        var screen = root.AddComponent<QuickPlaySetupScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);
        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "QUICK PLAY", out title);
        screen.titleLabel = title;

        // Players
        var playersCard = UIFactory.Card("PlayersCard", p, new Vector2(880f, 250f), new Vector2(0f, 540f),
                                         "PLAYERS", T.secondary);
        screen.playersToggle = UIFactory.Segmented("PlayersToggle", playersCard.transform,
                                                   new Vector2(780f, 104f), new Vector2(0f, -40f),
                                                   new[] { "2 PLAYERS", "4 PLAYERS" }, T.secondary);

        // Colour choice
        var colourCard = UIFactory.Card("ColourCard", p, new Vector2(880f, 290f), new Vector2(0f, 235f),
                                        "YOUR COLOUR", T.success);
        screen.colourHint = UIFactory.Label("Hint", colourCard.transform, new Vector2(780f, 40f),
                                            new Vector2(0f, -118f), "Pick the house you want to play",
                                            UIFontSet.Current.bodyLight, 24f, T.inkMuted);

        Color[] houseColours = { T.playerRed, T.playerGreen, T.playerYellow, T.playerBlue };
        var cButtons = new ClayButton[4];
        var cSwatches = new ClayGraphic[4];
        var cChecks = new ClayGraphic[4];
        for (int k = 0; k < 4; k++)
        {
            float x = -285f + k * 190f;
            var b = UIFactory.Button("Colour" + k, colourCard.transform, new Vector2(150f, 118f),
                                     new Vector2(x, -30f), T.recessed, null,
                                     shape: ClayGraphic.Shape.Panel);
            b.Clay.cornerRadius = T.radiusMd;
            b.Clay.shadowAlpha = 0f;
            b.Clay.innerShadow = 0.30f;

            var sw = UIFactory.Clay("Swatch", b.content, new Vector2(74f, 74f), new Vector2(0f, 6f),
                                    houseColours[k], ClayGraphic.Shape.Circle);
            sw.raycastTarget = false;

            // A tick that only appears on the chosen house.
            var chk = UIFactory.Clay("Check", b.content, new Vector2(34f, 34f), new Vector2(40f, -34f),
                                     T.ink, ClayGraphic.Shape.Circle);
            chk.raycastTarget = false;
            chk.shadowAlpha = 0f;
            chk.gameObject.SetActive(false);

            cButtons[k] = b; cSwatches[k] = sw; cChecks[k] = chk;
        }
        screen.colourButtons = cButtons;
        screen.colourSwatches = cSwatches;
        screen.colourChecks = cChecks;

        // Mode. Safe Zone is locked for this release.
        var modeCard = UIFactory.Card("ModeCard", p, new Vector2(880f, 280f), new Vector2(0f, -105f),
                                      "GAME MODE", T.gold);
        var starIcon = icons != null ? icons.star : null;
        if (starIcon != null)
        {
            var bubble = modeCard.transform.Find("Bubble");
            if (bubble != null)
                UIFactory.Icon("ModeStar", bubble, new Vector2(46f, 46f), Vector2.zero,
                               starIcon, Color.white);
        }

        screen.modeToggle = UIFactory.Segmented("ModeToggle", modeCard.transform,
                                                new Vector2(780f, 104f), new Vector2(0f, -30f),
                                                new[] { "CLASSIC", "SAFE ZONE" }, T.gold);
        screen.modeBlurb = UIFactory.Label("Blurb", modeCard.transform, new Vector2(780f, 44f),
                                           new Vector2(0f, -108f), "Standard rules. May the best roller win.",
                                           UIFontSet.Current.bodyLight, 24f, T.inkMuted);

        // Difficulty, hidden entirely in pass-and-play.
        var diffSection = UIFactory.Rect("DifficultySection", p, new Vector2(880f, 260f), new Vector2(0f, -420f));
        screen.difficultySection = diffSection.gameObject;
        var aiCard = UIFactory.Card("DifficultyCard", diffSection, new Vector2(880f, 260f), Vector2.zero,
                                    "AI DIFFICULTY", T.primary);
        screen.difficultyToggle = UIFactory.Segmented("DifficultyToggle", aiCard.transform,
                                                      new Vector2(780f, 104f), new Vector2(0f, -40f),
                                                      new[] { "EASY", "NORMAL", "HARD" }, T.primary);

        // Start
        screen.startButton = UIFactory.Button("StartButton", p, new Vector2(840f, 180f),
                                              new Vector2(0f, -720f), T.success, "START GAME", 46f,
                                              ClayGraphic.Shape.Pill, glow: 0.16f, hero: true);
        screen.startButton.Clay.glowSpread = 40f;

        SavePrefab(root, ScreenDir + "/QuickPlaySetup.prefab");
    }

    // â”€â”€ Settings â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/4. Build Settings", priority = 4)]
    public static void BuildSettings()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("Settings", ScreenId.Settings);
        var screen = root.AddComponent<SettingsScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);
        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "SETTINGS", out title);

        // Width 880 centred spans -440..440, matching the rows below. A left-aligned label of the
        // same width therefore starts exactly where the rows start; offsetting it pushes it
        // off-screen, which is what clipped this to "CES".
        UIFactory.Label("PrefHeader", p, new Vector2(880f, 44f), new Vector2(0f, 640f), "PREFERENCES",
                        UIFontSet.Current.body, 26f, T.inkDim, TextAlignmentOptions.Left).characterSpacing = 8f;

        screen.soundToggle = SettingRowWithSwitch(p, "SoundRow", "Sound Effects", 540f, T);
        screen.musicToggle = SettingRowWithSwitch(p, "MusicRow", "Music", 396f, T);
        screen.vibrationToggle = SettingRowWithSwitch(p, "VibrationRow", "Vibration", 252f, T);

        TextMeshProUGUI langLabel;
        screen.languageRow = UIFactory.Row("LanguageRow", p, 880f, 108f, "Language", out langLabel);
        // Ends at x=320, clear of the chevron at 396.
        screen.languageValue = UIFactory.Label("Value", screen.languageRow.content, new Vector2(260f, 50f),
                                               new Vector2(190f, 0f), "English", UIFontSet.Current.body,
                                               30f, T.inkMuted, TextAlignmentOptions.Right);
        UIFactory.Chevron(screen.languageRow.content, new Vector2(396f, 0f), T.inkDim);

        UIFactory.Label("AboutHeader", p, new Vector2(880f, 44f), new Vector2(0f, -50f), "ABOUT",
                        UIFontSet.Current.body, 26f, T.inkDim, TextAlignmentOptions.Left).characterSpacing = 8f;

        screen.aboutRow = LinkRow(p, "AboutRow", "About Ludo NX", -150f, T);
        screen.privacyRow = LinkRow(p, "PrivacyRow", "Privacy Policy", -294f, T);
        screen.termsRow = LinkRow(p, "TermsRow", "Terms of Service", -438f, T);

        screen.versionLabel = UIFactory.Label("Version", p, new Vector2(880f, 44f), new Vector2(0f, -600f),
                                              "Version 1.0", UIFontSet.Current.bodyLight, 26f, T.inkDim);

        SavePrefab(root, ScreenDir + "/Settings.prefab");
    }

    private static ToggleSwitch SettingRowWithSwitch(Transform p, string name, string label, float y, UITheme T)
    {
        TextMeshProUGUI unused;
        var row = UIFactory.Row(name, p, 880f, y, label, out unused);

        var track = UIFactory.Clay("SwitchTrack", row.content, new Vector2(128f, 68f),
                                   new Vector2(356f, 0f), T.success, ClayGraphic.Shape.Pill);
        track.shadowAlpha = 0f;
        track.innerHighlight = 0.05f;
        track.innerShadow = 0.70f;
        track.raycastTarget = false;

        var knob = UIFactory.Clay("Knob", track.transform, new Vector2(56f, 56f), new Vector2(34f, 0f),
                                  T.ink, ClayGraphic.Shape.Circle);
        knob.raycastTarget = false;
        knob.shadowSpread = 16f;
        knob.shadowAlpha = 0.35f;

        var sw = row.gameObject.AddComponent<ToggleSwitch>();
        sw.track = track;
        sw.knob = knob;
        sw.hitbox = row;      // the whole row toggles, not just the switch
        return sw;
    }

    private static ClayButton LinkRow(Transform p, string name, string label, float y, UITheme T)
    {
        TextMeshProUGUI unused;
        var row = UIFactory.Row(name, p, 880f, y, label, out unused);
        UIFactory.Chevron(row.content, new Vector2(396f, 0f), T.inkDim);
        return row;
    }

    // â”€â”€ Profile â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/5. Build Profile", priority = 5)]
    public static void BuildProfile()
    {
        CreateSupportAssets();
        var T = UITheme.Current;
        var icons = UIIconSet.Current;

        GameObject root = NewScreenRoot("Profile", ScreenId.Profile);
        var screen = root.AddComponent<ProfileScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);
        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "PROFILE", out title);

        screen.identityTab = UIFactory.Button("IdentityTab", p, new Vector2(424f, 96f),
                                              new Vector2(-226f, 640f), T.secondary, "IDENTITY", 28f);
        screen.achievementsTab = UIFactory.Button("AchievementsTab", p, new Vector2(424f, 96f),
                                                  new Vector2(226f, 640f), T.neutral, "AWARDS", 28f);

        // Identity panel
        var identity = UIFactory.Rect("IdentityPanel", p, new Vector2(1080f, 1200f), new Vector2(0f, -120f));
        screen.identityPanel = identity.gameObject;

        var ring = UIFactory.Clay("AvatarRing", identity, new Vector2(240f, 240f), new Vector2(0f, 430f),
                                  T.panelHigh, ClayGraphic.Shape.Circle);
        ring.raycastTarget = false;
        var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/art_assets/generated/ui_circle.png");
        var inner = UIFactory.Icon("Avatar", ring.transform, new Vector2(196f, 196f), Vector2.zero,
                                   circleSprite, T.secondary);
        inner.raycastTarget = false;
        ring.gameObject.AddComponent<ProfileAvatarSlot>().diameter = 196f;

        screen.nameLabel = UIFactory.Display("Name", identity, new Vector2(900f, 90f), new Vector2(0f, 268f),
                                             "Player", T.ink);
        screen.nameLabel.fontSize = 62f;
        screen.levelLabel = UIFactory.Body("Level", identity, new Vector2(900f, 50f), new Vector2(0f, 196f),
                                           "LEVEL 1", T.inkMuted, 28f);
        screen.levelLabel.characterSpacing = 6f;

        screen.editButton = UIFactory.Button("EditButton", identity, new Vector2(300f, 88f),
                                             new Vector2(0f, 108f), T.neutral, "EDIT NAME", 26f);

        screen.winsValue = UIFactory.StatTile("WinsTile", identity, new Vector2(410f, 210f),
                                              new Vector2(-222f, -60f), "0", "WINS", T.gold);
        screen.matchesValue = UIFactory.StatTile("MatchesTile", identity, new Vector2(410f, 210f),
                                                 new Vector2(222f, -60f), "0", "MATCHES", T.secondary);
        screen.winRateValue = UIFactory.StatTile("RateTile", identity, new Vector2(410f, 210f),
                                                 new Vector2(-222f, -300f), "0%", "WIN RATE", T.success);
        screen.streakValue = UIFactory.StatTile("StreakTile", identity, new Vector2(410f, 210f),
                                                new Vector2(222f, -300f), "0", "BEST STREAK", T.primary);

        // Achievements panel
        var awards = UIFactory.Rect("AchievementsPanel", p, new Vector2(1080f, 1200f), new Vector2(0f, -120f));
        screen.achievementsPanel = awards.gameObject;

        screen.achievementSummary = UIFactory.Body("Summary", awards, new Vector2(880f, 50f),
                                                   new Vector2(0f, 500f), "0 of 20 unlocked", T.inkMuted, 28f);

        var viewport = UIFactory.Rect("Viewport", awards, new Vector2(940f, 980f), new Vector2(0f, -40f));
        var vpImage = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
        vpImage.color = new Color(1f, 1f, 1f, 0.004f);   // a Mask needs something to draw
        var mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();
        mask.showMaskGraphic = false;

        var content = UIFactory.Rect("Content", viewport, new Vector2(900f, 2000f), Vector2.zero);
        content.anchorMin = new Vector2(0.5f, 1f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        screen.achievementContent = content;

        var scroll = awards.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.scrollSensitivity = 30f;

        screen.achievementRowTemplate = BuildAchievementRow(content, T, icons);

        SavePrefab(root, ScreenDir + "/Profile.prefab");
    }

    /// <summary>
    /// One achievement row, used as a template. ProfileScreen clones it once per achievement and
    /// finds its parts by name, so these child names are load-bearing.
    /// </summary>
    private static RectTransform BuildAchievementRow(Transform parent, UITheme T, UIIconSet icons)
    {
        var card = UIFactory.Clay("RowTemplate", parent, new Vector2(880f, 132f), Vector2.zero,
                                  T.panel, ClayGraphic.Shape.Panel, T.radiusMd);
        card.raycastTarget = false;
        card.gameObject.AddComponent<CanvasGroup>();   // ProfileScreen dims locked rows through this

        var badge = UIFactory.Clay("Badge", card.transform, new Vector2(84f, 84f),
                                   new Vector2(-364f, 14f), T.gold, ClayGraphic.Shape.Circle);
        badge.raycastTarget = false;
        if (icons != null && icons.star != null)
            UIFactory.Icon("Star", badge.transform, new Vector2(46f, 46f), Vector2.zero,
                           icons.star, Color.white);
        // Shown while the achievement is still locked; ProfileScreen swaps the two.
        BuildPadlock(badge.transform, T);

        var t = UIFactory.Label("Title_Text", card.transform, new Vector2(500f, 44f),
                                new Vector2(-40f, 34f), "Title", UIFontSet.Current.body,
                                30f, T.ink, TextAlignmentOptions.Left);
        t.fontStyle = FontStyles.Bold;

        UIFactory.Label("Desc_Text", card.transform, new Vector2(500f, 40f), new Vector2(-40f, -8f),
                        "Description", UIFontSet.Current.bodyLight, 24f, T.inkMuted,
                        TextAlignmentOptions.Left);

        UIFactory.Label("Count_Text", card.transform, new Vector2(180f, 40f), new Vector2(340f, 34f),
                        "0 / 1", UIFontSet.Current.numeric, 26f, T.inkDim, TextAlignmentOptions.Right);

        var bar = UIFactory.Clay("Bar", card.transform, new Vector2(560f, 14f), new Vector2(-10f, -46f),
                                 T.recessed, ClayGraphic.Shape.Pill);
        bar.raycastTarget = false;
        bar.shadowAlpha = 0f;

        // The fill is a clay pill whose width ProfileScreen drives, rather than an Image on
        // fillAmount. A uGUI Image with no sprite ignores its fill settings and draws the whole
        // quad, so every locked achievement showed a full bar at 0%. Driving width also keeps
        // the fill's rounded ends matching the track it sits in.
        var fill = UIFactory.Clay("Fill", bar.transform, new Vector2(0f, 14f), Vector2.zero,
                                  T.secondary, ClayGraphic.Shape.Pill);
        fill.raycastTarget = false;
        fill.shadowAlpha = 0f;
        var fillRt = fill.rectTransform;
        fillRt.anchorMin = new Vector2(0f, 0.5f);
        fillRt.anchorMax = new Vector2(0f, 0.5f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;

        card.gameObject.SetActive(false);
        return (RectTransform)card.transform;
    }

    /// <summary>
    /// A padlock drawn from clay rather than an icon file.
    ///
    /// The supplied icon set has no lock glyph, and the fallback was landing on a plain cross,
    /// which reads as "dismissed" rather than "not yet earned". Three primitives — a ring, a hole
    /// punched in it matching the badge behind, and a body drawn over the ring's lower half —
    /// give a real padlock at any size with no new art to source.
    /// </summary>
    private static void BuildPadlock(Transform parent, UITheme T)
    {
        var root = UIFactory.Rect("Lock", parent, new Vector2(48f, 48f), Vector2.zero);

        // A shackle close to the body's width reads as a head over shoulders. Keeping it near
        // 0.55x the body, with a wide hole, is what makes the silhouette a padlock.
        var ring = UIFactory.Clay("Shackle", root, new Vector2(22f, 22f), new Vector2(0f, 11f),
                                  T.inkMuted, ClayGraphic.Shape.Circle);
        ring.raycastTarget = false;
        ring.shadowAlpha = 0f;

        // Matches the badge behind so the ring reads as an open shackle, not a filled disc.
        var hole = UIFactory.Clay("ShackleHole", root, new Vector2(12f, 12f), new Vector2(0f, 11f),
                                  T.recessed, ClayGraphic.Shape.Circle);
        hole.raycastTarget = false;
        hole.shadowAlpha = 0f;

        // Its top edge must reach the ring's centre, or the whole ring stays visible and the glyph
        // reads as a head above shoulders rather than a shackle entering a lock body.
        var body = UIFactory.Clay("Body", root, new Vector2(38f, 27f), new Vector2(0f, -3f),
                                  T.inkMuted, ClayGraphic.Shape.Panel, 8f);
        body.raycastTarget = false;
        body.shadowAlpha = 0f;

        // The keyhole is the detail that makes a padlock legible at badge size.
        var keyhole = UIFactory.Clay("Keyhole", root, new Vector2(7f, 7f), new Vector2(0f, -4f),
                                     T.recessed, ClayGraphic.Shape.Circle);
        keyhole.raycastTarget = false;
        keyhole.shadowAlpha = 0f;
    }

    // â”€â”€ Match intro â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/6. Build Match Intro", priority = 6)]
    public static void BuildMatchIntro()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("MatchIntro", ScreenId.MatchIntro);
        var screen = root.AddComponent<MatchIntroScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        // Full-screen tap catcher, behind everything, so tapping anywhere skips the countdown.
        var skip = UIFactory.Button("SkipArea", p, new Vector2(1080f, 1920f), Vector2.zero,
                                    new Color(1f, 1f, 1f, 0f), null, shape: ClayGraphic.Shape.Panel);
        skip.Clay.shadowAlpha = 0f; skip.Clay.innerHighlight = 0f;
        skip.Clay.innerShadow = 0f; skip.Clay.gradientStrength = 0f;
        skip.Clay.cornerRadius = 0f;
        screen.skipArea = skip;

        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "", out title);

        UIFactory.Display("Ready", p, new Vector2(1000f, 140f), new Vector2(0f, 560f), "GET READY!", T.ink);

        var ring = UIFactory.Clay("CountdownRing", p, new Vector2(200f, 200f), new Vector2(0f, 372f),
                                  T.primary, ClayGraphic.Shape.Circle);
        ring.glowAlpha = 0.18f; ring.glowSpread = 40f;
        screen.countdownLabel = UIFactory.Display("Count", ring.transform, new Vector2(200f, 140f),
                                                  Vector2.zero, "3", T.ink);

        screen.modeChipLabel = UIFactory.Chip("ModeChip", p, new Vector2(300f, 66f), new Vector2(-160f, 216f),
                                              "CLASSIC", T.panelHigh, T.ink);
        screen.difficultyChipLabel = UIFactory.Chip("AIChip", p, new Vector2(300f, 66f), new Vector2(160f, 216f),
                                                    "NORMAL", T.panelHigh, T.ink);

        // You
        var youCard = UIFactory.Clay("YouCard", p, new Vector2(880f, 230f), new Vector2(0f, 40f),
                                     T.panel, ClayGraphic.Shape.Panel, T.radiusLg);
        youCard.raycastTarget = false;
        screen.youColourSwatch = UIFactory.Clay("YouSwatch", youCard.transform, new Vector2(120f, 120f),
                                                new Vector2(-320f, 0f), T.playerRed, ClayGraphic.Shape.Circle);
        screen.youColourSwatch.raycastTarget = false;
        screen.youColourLabel = UIFactory.Label("YouLabel", youCard.transform, new Vector2(560f, 70f),
                                                new Vector2(60f, 0f), "YOU â€” RED", UIFontSet.Current.display,
                                                48f, T.ink, TextAlignmentOptions.Left);

        UIFactory.Label("VS", p, new Vector2(200f, 70f), new Vector2(0f, -110f), "VS",
                        UIFontSet.Current.display, 44f, T.inkDim);

        // Opponents
        var swatches = new ClayGraphic[3];
        var labels = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            float x = -290f + i * 290f;
            var card = UIFactory.Clay("Bot" + i, p, new Vector2(270f, 270f), new Vector2(x, -320f),
                                      T.panel, ClayGraphic.Shape.Panel, T.radiusLg);
            card.raycastTarget = false;
            swatches[i] = UIFactory.Clay("Swatch", card.transform, new Vector2(120f, 120f),
                                         new Vector2(0f, 32f), T.playerGreen, ClayGraphic.Shape.Circle);
            swatches[i].raycastTarget = false;
            labels[i] = UIFactory.Body("Label", card.transform, new Vector2(240f, 50f),
                                       new Vector2(0f, -78f), "BOT " + (i + 1), T.inkMuted, 28f);
        }
        screen.opponentSwatches = swatches;
        screen.opponentLabels = labels;

        UIFactory.Body("Hint", p, new Vector2(900f, 50f), new Vector2(0f, -640f),
                       "TAP ANYWHERE TO SKIP", T.inkDim, 26f).characterSpacing = 6f;

        SavePrefab(root, ScreenDir + "/MatchIntro.prefab");
    }

    // â”€â”€ Result â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/7. Build Match Result", priority = 7)]
    public static void BuildResult()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("Result", ScreenId.Result);
        var screen = root.AddComponent<MatchResultScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        screen.headlineBanner = UIFactory.Clay("Banner", p, new Vector2(760f, 190f), new Vector2(0f, 720f),
                                               T.gold, ClayGraphic.Shape.Panel, T.radiusXl);
        screen.headlineBanner.raycastTarget = false;
        screen.headlineBanner.glowAlpha = 0.16f;
        screen.headlineBanner.glowSpread = 44f;
        var star = UIIconSet.Current != null ? UIIconSet.Current.star : null;
        if (star != null)
            UIFactory.Icon("BannerStar", screen.headlineBanner.transform, new Vector2(70f, 70f),
                           new Vector2(-292f, 0f), star, Color.white);

        screen.headlineLabel = UIFactory.Display("Headline", screen.headlineBanner.transform,
                                                 new Vector2(700f, 110f), new Vector2(24f, 0f),
                                                 "VICTORY!", T.groundTop);
        screen.headlineLabel.fontSize = 64f;
        screen.subheadLabel = UIFactory.Body("Subhead", p, new Vector2(900f, 50f), new Vector2(0f, 596f),
                                             "You dominated the board.", T.inkMuted, 30f);

        // Placement table
        var panels = new ClayGraphic[4];
        var swatch = new ClayGraphic[4];
        var places = new TextMeshProUGUI[4];
        var names = new TextMeshProUGUI[4];
        var scores = new TextMeshProUGUI[4];
        for (int i = 0; i < 4; i++)
        {
            float y = 430f - i * 130f;
            panels[i] = UIFactory.Clay("Row" + i, p, new Vector2(880f, 112f), new Vector2(0f, y),
                                       T.panel, ClayGraphic.Shape.Panel, T.radiusMd);
            panels[i].raycastTarget = false;
            places[i] = UIFactory.Label("Place", panels[i].transform, new Vector2(90f, 70f),
                                        new Vector2(-368f, 0f), (i + 1).ToString(),
                                        UIFontSet.Current.display, 44f, T.inkMuted);
            swatch[i] = UIFactory.Clay("Swatch", panels[i].transform, new Vector2(64f, 64f),
                                       new Vector2(-268f, 0f), T.playerRed, ClayGraphic.Shape.Circle);
            swatch[i].raycastTarget = false;
            names[i] = UIFactory.Label("Name", panels[i].transform, new Vector2(420f, 60f),
                                       new Vector2(-10f, 0f), "â€”", UIFontSet.Current.body,
                                       34f, T.ink, TextAlignmentOptions.Left);
            scores[i] = UIFactory.Label("Score", panels[i].transform, new Vector2(220f, 60f),
                                        new Vector2(320f, 0f), "0", UIFontSet.Current.numeric,
                                        34f, T.gold, TextAlignmentOptions.Right);
        }
        screen.rowPanels = panels;
        screen.rowSwatches = swatch;
        screen.rowPlaces = places;
        screen.rowNames = names;
        screen.rowScores = scores;

        // Headline tiles
        screen.tokensHomeValue = UIFactory.StatTile("HomeTile", p, new Vector2(276f, 200f),
                                                    new Vector2(-300f, -220f), "0/4", "TOKENS HOME", T.success);
        screen.capturesValue = UIFactory.StatTile("CapTile", p, new Vector2(276f, 200f),
                                                  new Vector2(0f, -220f), "0", "CAPTURES", T.primary);
        screen.nxValue = UIFactory.StatTile("NXTile", p, new Vector2(276f, 200f),
                                            new Vector2(300f, -220f), "+0", "NX EARNED", T.gold);

        screen.playAgainButton = UIFactory.Button("PlayAgain", p, new Vector2(840f, 190f),
                                                  new Vector2(0f, -520f), T.primary, "PLAY AGAIN", 46f,
                                                  ClayGraphic.Shape.Pill, glow: 0.16f, hero: true);
        screen.playAgainButton.Clay.glowSpread = 40f;
        screen.homeButton = UIFactory.Button("HomeButton", p, new Vector2(840f, 160f),
                                             new Vector2(0f, -710f), T.neutral, "HOME", 38f);

        SavePrefab(root, ScreenDir + "/Result.prefab");
    }

    // â”€â”€ Splash â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/8. Build Splash", priority = 8)]
    public static void BuildSplash()
    {
        CreateSupportAssets();
        var T = UITheme.Current;
        var icons = UIIconSet.Current;

        GameObject root = NewScreenRoot("Splash", ScreenId.Splash);
        var screen = root.AddComponent<SplashScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        // Geometry is transposed from the 390Ã—972 reference. Vertical positions are kept as
        // fractions of its height so the composition survives the move to a 1080Ã—1920 canvas;
        // widths scale by 1080/390 = 2.769.
        //
        //   die centre   y 325/972 = 0.334      wordmark y 524/972 = 0.539
        //   tagline      y 582/972 = 0.599      status   y 684/972 = 0.704
        //   bar          y 710/972 = 0.730      bar 345Ã—21 â†’ 955Ã—58, side margin 55

        // â”€â”€ Hero: the die sitting in a white disc â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var mark = UIFactory.Rect("LogoMark", p, new Vector2(660f, 660f), new Vector2(0f, 318f));
        screen.logoMark = mark;

        var disc = UIFactory.Clay("Disc", mark, new Vector2(660f, 660f), Vector2.zero,
                                  Color.white, ClayGraphic.Shape.Circle);
        disc.raycastTarget = false;
        disc.shadowSpread = 70f;
        disc.shadowAlpha = 0.10f;      // the reference's shadow is barely there
        disc.innerHighlight = 0.10f;
        disc.innerShadow = 0.04f;
        disc.gradientStrength = 0.02f;

        if (icons != null && icons.diceLogo != null)
            UIFactory.Icon("Dice", disc.transform, new Vector2(520f, 520f), Vector2.zero,
                           icons.diceLogo, Color.white);

        // â”€â”€ Wordmark â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Plus Jakarta Sans Bold, not Baloo â€” the reference wordmark is a geometric sans.
        var word = UIFactory.Label("Wordmark", p, new Vector2(1000f, 150f), new Vector2(0f, -75f),
                                   "Ludo NX", UIFontSet.Current.numeric, 100f, T.primary);
        word.characterSpacing = -2f;

        UIFactory.Label("Tagline", p, new Vector2(1000f, 70f), new Vector2(0f, -190f),
                        "Roll into a new world", UIFontSet.Current.bodyLight, 44f, T.inkMuted);

        // â”€â”€ Status row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Flush with the ends of the bar below (Â±485), as in the reference.
        screen.statusLabel = UIFactory.Label("Status", p, new Vector2(500f, 50f), new Vector2(-235f, -391f),
                                             "Loading...", UIFontSet.Current.body, 34f, T.primary,
                                             TextAlignmentOptions.Left);
        screen.versionLabel = UIFactory.Label("Version", p, new Vector2(400f, 50f), new Vector2(285f, -391f),
                                              "v1.0", UIFontSet.Current.bodyLight, 30f, T.inkDim,
                                              TextAlignmentOptions.Right);

        // â”€â”€ Progress bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var track = UIFactory.Clay("ProgressTrack", p, new Vector2(970f, 58f), new Vector2(0f, -442f),
                                   T.recessed, ClayGraphic.Shape.Pill);
        track.raycastTarget = false;
        track.shadowAlpha = 0f;
        track.innerHighlight = 0.05f;
        track.innerShadow = 0.45f;

        screen.progressFill = UIFactory.Clay("ProgressFill", track.transform, new Vector2(950f, 46f),
                                             Vector2.zero, T.primary, ClayGraphic.Shape.Pill);
        screen.progressFill.raycastTarget = false;
        screen.progressFill.shadowSpread = 16f;
        screen.progressFill.shadowAlpha = 0.22f;

        // Anchor left so growing the width fills rightwards instead of from the centre.
        var fillRect = (RectTransform)screen.progressFill.transform;
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(10f, 0f);

        SavePrefab(root, ScreenDir + "/Splash.prefab");
    }

    // â”€â”€ Modals â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/9. Build Modals", priority = 9)]
    public static void BuildModals()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        // Confirm (quit a match, reset profileâ€¦)
        {
            GameObject root = NewModalRoot("Confirm");
            var modal = root.AddComponent<ConfirmModal>();
            var card = ModalCard(root.transform, new Vector2(880f, 720f), T, modal);

            modal.titleLabel = UIFactory.Display("Title", card, new Vector2(760f, 100f),
                                                 new Vector2(0f, 226f), "LEAVING SO SOON?", T.ink);
            modal.titleLabel.fontSize = 52f;
            modal.bodyLabel = UIFactory.Label("Body", card, new Vector2(720f, 180f), new Vector2(0f, 76f),
                                              "Are you sure you want to quit? Your progress will be lost.",
                                              UIFontSet.Current.bodyLight, 30f, T.inkMuted);

            // The loud button keeps you playing; the quiet one is destructive.
            var keep = UIFactory.Button("CancelButton", card, new Vector2(760f, 170f), new Vector2(0f, -78f),
                                        T.primary, "NO, CONTINUE", 40f, ClayGraphic.Shape.Pill,
                                        glow: 0.14f, hero: true);
            keep.Clay.glowSpread = 36f;
            modal.cancelButton = keep;
            modal.cancelLabel = keep.content.GetComponentInChildren<TextMeshProUGUI>(true);

            var quit = UIFactory.Button("ConfirmButton", card, new Vector2(760f, 150f), new Vector2(0f, -256f),
                                        T.neutral, "YES, QUIT", 36f);
            modal.confirmButton = quit;
            modal.confirmLabel = quit.content.GetComponentInChildren<TextMeshProUGUI>(true);

            SavePrefabRaw(root, ModalDir + "/Confirm.prefab");
        }

        // Pause
        {
            GameObject root = NewModalRoot("Pause");
            var modal = root.AddComponent<ConfirmModal>();
            var card = ModalCard(root.transform, new Vector2(880f, 780f), T, modal);
            modal.dismissOnScrimTap = true;

            modal.titleLabel = UIFactory.Display("Title", card, new Vector2(760f, 100f),
                                                 new Vector2(0f, 262f), "PAUSED", T.ink);
            modal.titleLabel.fontSize = 56f;
            modal.bodyLabel = UIFactory.Label("Body", card, new Vector2(720f, 90f), new Vector2(0f, 168f),
                                              "The match is waiting for you.",
                                              UIFontSet.Current.bodyLight, 30f, T.inkMuted);

            var resume = UIFactory.Button("CancelButton", card, new Vector2(760f, 170f), new Vector2(0f, 22f),
                                          T.success, "RESUME", 42f, ClayGraphic.Shape.Pill,
                                          glow: 0.14f, hero: true);
            resume.Clay.glowSpread = 36f;
            modal.cancelButton = resume;
            modal.cancelLabel = resume.content.GetComponentInChildren<TextMeshProUGUI>(true);

            var quit = UIFactory.Button("ConfirmButton", card, new Vector2(760f, 150f), new Vector2(0f, -178f),
                                        T.neutral, "QUIT MATCH", 36f);
            modal.confirmButton = quit;
            modal.confirmLabel = quit.content.GetComponentInChildren<TextMeshProUGUI>(true);

            SavePrefabRaw(root, ModalDir + "/Pause.prefab");
        }

        // Language picker
        {
            GameObject root = NewModalRoot("LanguagePicker");
            var modal = root.AddComponent<ListPickerModal>();
            var card = ModalCard(root.transform, new Vector2(880f, 1020f), T, modal);

            modal.titleLabel = UIFactory.Display("Title", card, new Vector2(760f, 90f),
                                                 new Vector2(0f, 420f), "LANGUAGE", T.ink);
            modal.titleLabel.fontSize = 50f;

            var container = UIFactory.Rect("Rows", card, new Vector2(780f, 800f), new Vector2(0f, -40f));
            modal.rowContainer = container;

            var template = UIFactory.Button("RowTemplate", container, new Vector2(760f, 108f),
                                            Vector2.zero, T.panelHigh, "Language", 32f,
                                            ClayGraphic.Shape.Panel);
            template.Clay.cornerRadius = T.radiusMd;
            template.gameObject.SetActive(false);
            modal.rowTemplate = template;

            SavePrefabRaw(root, ModalDir + "/LanguagePicker.prefab");
        }

        // Text page (About / Privacy / Terms)
        {
            GameObject root = NewModalRoot("TextPage");
            var modal = root.AddComponent<TextPageModal>();
            var card = ModalCard(root.transform, new Vector2(920f, 1180f), T, modal);

            modal.titleLabel = UIFactory.Display("Title", card, new Vector2(800f, 90f),
                                                 new Vector2(0f, 500f), "ABOUT", T.ink);
            modal.titleLabel.fontSize = 48f;
            modal.bodyLabel = UIFactory.Label("Body", card, new Vector2(800f, 840f), new Vector2(0f, 20f),
                                              "â€¦", UIFontSet.Current.bodyLight, 28f, T.inkMuted,
                                              TextAlignmentOptions.TopLeft);

            var close = UIFactory.Button("CloseButton", card, new Vector2(760f, 150f), new Vector2(0f, -480f),
                                         T.secondary, "CLOSE", 38f);
            modal.closeButton = close;

            SavePrefabRaw(root, ModalDir + "/TextPage.prefab");
        }

        // Edit profile
        {
            GameObject root = NewModalRoot("EditProfile");
            var modal = root.AddComponent<EditProfileModal>();
            var card = ModalCard(root.transform, new Vector2(880f, 1100f), T, modal);

            UIFactory.Display("Title", card, new Vector2(760f, 90f), new Vector2(0f, 450f),
                              "YOUR NAME", T.ink).fontSize = 50f;

            var well = UIFactory.Recess("Field", card, new Vector2(760f, 120f), new Vector2(0f, 342f),
                                        ClayGraphic.Shape.Panel);
            well.cornerRadius = T.radiusMd;

            var textArea = UIFactory.Rect("TextArea", well.transform, new Vector2(700f, 90f), Vector2.zero);
            var input = well.gameObject.AddComponent<TMP_InputField>();
            var visible = UIFactory.Label("Text", textArea, new Vector2(700f, 90f), Vector2.zero, "",
                                          UIFontSet.Current.body, 36f, T.ink);
            visible.raycastTarget = false;
            input.textViewport = textArea;
            input.textComponent = visible;
            input.characterLimit = 16;
            input.text = "Player";

            UIFactory.Label("PickerCaption", card, new Vector2(760f, 50f), new Vector2(0f, 252f),
                            "YOUR PICTURE", UIFontSet.Current.body, 28f, T.inkDim);

            // Left empty on purpose â€” the modal fills it from the avatar set at runtime.
            modal.pickerRoot = UIFactory.Rect("Picker", card, new Vector2(780f, 320f),
                                              new Vector2(0f, 66f));

            var save = UIFactory.Button("SaveButton", card, new Vector2(760f, 160f), new Vector2(0f, -220f),
                                        T.success, "SAVE", 40f, ClayGraphic.Shape.Pill, glow: 0.14f);
            modal.saveButton = save;
            var cancel = UIFactory.Button("CancelButton", card, new Vector2(760f, 140f), new Vector2(0f, -400f),
                                          T.neutral, "CANCEL", 34f);
            modal.cancelButton = cancel;
            modal.nameField = input;

            SavePrefabRaw(root, ModalDir + "/EditProfile.prefab");
        }

        Debug.Log("[LudoNX UI] modals written.");
    }

    private static GameObject NewModalRoot(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    /// <summary>Scrim plus the raised card that scales in. Returns the card to fill.</summary>
    private static Transform ModalCard(Transform root, Vector2 size, UITheme T, UIModal modal)
    {
        var scrimBtn = UIFactory.Button("Scrim", root, new Vector2(1080f, 1920f), Vector2.zero,
                                        new Color(0f, 0f, 0f, 0.72f), null, shape: ClayGraphic.Shape.Panel);
        scrimBtn.Clay.cornerRadius = 0f;
        scrimBtn.Clay.shadowAlpha = 0f;
        scrimBtn.Clay.innerHighlight = 0f;
        scrimBtn.Clay.innerShadow = 0f;
        scrimBtn.Clay.gradientStrength = 0f;
        scrimBtn.onClick.AddListener(modal.OnScrimTapped);

        var card = UIFactory.Clay("Card", root, size, Vector2.zero, T.panel,
                                  ClayGraphic.Shape.Panel, T.radiusXl);
        card.shadowSpread = 60f;
        card.shadowAlpha = 0.5f;
        modal.card = (RectTransform)card.transform;
        return card.transform;
    }

    // â”€â”€ Build everything â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [MenuItem("Ludo NX/UI/Build ALL Screens", priority = 20)]
    public static void BuildAll()
    {
        CreateSupportAssets();
        BuildSplash();
        BuildHome();
        BuildPlayMenu();
        BuildQuickPlaySetup();
        BuildMatchIntro();
        BuildResult();
        BuildProfile();
        BuildSettings();
        BuildModals();
        Debug.Log("[LudoNX UI] ALL screens + modals rebuilt.");
    }

    // â”€â”€ Plumbing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static GameObject NewScreenRoot(string name, ScreenId id)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        return go;
    }

    /// <summary>Saves without touching a ScreenId â€” for modals, which have none.</summary>
    private static void SavePrefabRaw(GameObject root, string path)
    {
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("[LudoNX UI] wrote " + path);
    }

    private static void SavePrefab(GameObject root, string path)
    {
        var screen = root.GetComponent<UIScreen>();
        if (screen != null)
        {
            // Keep the serialized id in step with the file name.
            ScreenId parsed;
            if (System.Enum.TryParse(Path.GetFileNameWithoutExtension(path), out parsed)) screen.SetId(parsed);
        }

        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LudoNX UI] wrote " + path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}



