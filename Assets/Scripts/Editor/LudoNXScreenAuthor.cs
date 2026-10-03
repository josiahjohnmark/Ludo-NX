using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
        fonts.display = LoadFont("LilitaOne-Regular");
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
        icons.backgroundIllustration = LoadAsSprite("Assets/Art/UI/Backgrounds/Tournament_banner.jpeg");
        icons.bgHomeStadium = LoadAsSprite("Assets/Art/UI/Backgrounds/bg_match_emboss.jpg");
        icons.bgShopVault = LoadAsSprite("Assets/Art/UI/Backgrounds/bg_match_emboss.jpg");
        icons.bgBattleArena = LoadAsSprite("Assets/Art/UI/Backgrounds/bg_match_emboss.jpg");
        icons.bgTournamentHall = LoadAsSprite("Assets/Art/UI/Backgrounds/bg_match_emboss.jpg");
        icons.mascot = LoadAsSprite("Assets/Art/UI/Icons/cool_ludo_mascot.png");
        icons.coin3D = LoadAsSprite("Assets/Art/UI/Icons/coin_3d.png");
        icons.gem3D = LoadAsSprite("Assets/Art/UI/Icons/gem_3d.png");
        icons.gemGreen3D = LoadAsSprite("Assets/Art/UI/Icons/gem_green_3d.png");
        icons.heart3D = LoadAsSprite("Assets/Art/UI/Icons/heart_3d.png");
        icons.shield3D = LoadAsSprite("Assets/Art/UI/Icons/shield_3d.png");

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

    private static Color Hex(string rrggbb) => UITheme.Hex(rrggbb);

    // ── Home ─────────────────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/1. Build Home Screen (Figma UI)", priority = 1)]
    public static void BuildHome()
    {
        LudoHomeScreenFigmaImporter.ImportAndAssemble();
    }

    [MenuItem("Ludo NX/UI/Build Legacy Procedural Home", priority = 25)]
    public static void BuildLegacyHome()
    {
        CreateSupportAssets();
        var T = UITheme.Current;
        var icons = UIIconSet.Current;

        GameObject root = NewScreenRoot("Home", ScreenId.Home);
        var home = root.AddComponent<HomeScreen>();
        Transform p = root.transform;

        // Background: Deep emerald felt table with midnight navy leather frame & gold Art Deco accents
        UIFactory.Page(p);

        // ── Top Bar (Mobile Safe Zone: y = 848f) ──────────────────────────────────
        float topY = 848f;

        var topBarBg = UIFactory.Clay("TopBarBg", p, new Vector2(1040f, 96f), new Vector2(0f, topY),
                                      T.navBarBg, ClayGraphic.Shape.Pill);
        topBarBg.raycastTarget = false;
        topBarBg.shadowSpread = 20f;
        topBarBg.shadowAlpha = 0.35f;
        topBarBg.innerHighlight = 0.20f;

        // Avatar Capsule with Ranked Border & Level Crest (Left)
        var avatar = UIFactory.CircleButton("Avatar", p, 84f, new Vector2(-410f, topY), T.panelHigh);
        avatar.Clay.shadowSpread = 20f;
        avatar.Clay.shadowAlpha = 0.25f;

        // Dynamic Ranked Avatar Frame as outer border ring behind avatar face
        var avatarRankBorder = UIFactory.Clay("RankFrame", avatar.content, new Vector2(88f, 88f),
                                              Vector2.zero, T.gold, ClayGraphic.Shape.Circle);
        avatarRankBorder.raycastTarget = false;
        avatarRankBorder.shadowSpread = 14f;
        avatarRankBorder.shadowAlpha = 0.35f;
        avatarRankBorder.innerHighlight = 0.45f;
        avatarRankBorder.transform.SetAsFirstSibling();

        var avatarInner = UIFactory.Clay("AvatarInner", avatar.content, new Vector2(72f, 72f),
                                         Vector2.zero, T.secondary, ClayGraphic.Shape.Circle);
        avatarInner.raycastTarget = false;
        avatarInner.shadowAlpha = 0f;
        avatar.gameObject.AddComponent<ProfileAvatarSlot>().diameter = 72f;

        // Level Crest with Stage Badge (1-12) overlapping bottom-right of avatar
        var levelBadge = UIFactory.Clay("LevelBadge", avatar.transform, new Vector2(34f, 34f),
                                        new Vector2(26f, -24f), T.gold, ClayGraphic.Shape.Circle);
        levelBadge.raycastTarget = false;
        levelBadge.shadowSpread = 10f;
        levelBadge.shadowAlpha = 0.35f;
        var lvlNum = UIFactory.Label("LvlNum", levelBadge.transform, new Vector2(34f, 34f), Vector2.zero,
                                    "12", UIFontSet.Current.display, 18f, Hex("0A1128"), TextAlignmentOptions.Center);

        var nameLabel = UIFactory.Body("PlayerName", p, new Vector2(160f, 30f), new Vector2(-255f, topY + 14f),
                                       "Josiah", Color.white, 28f);
        nameLabel.font = UIFontSet.Current.display;
        nameLabel.alignment = TextAlignmentOptions.Left;

        var levelLabel = UIFactory.Label("LevelLabel", p, new Vector2(160f, 22f), new Vector2(-255f, topY - 14f),
                                         "PRO • LVL 12", UIFontSet.Current.body, 18f, T.gold, TextAlignmentOptions.Left);

        // Energy Chip with 3D Heart/Bolt + Live Timer + Plus Button
        var energyChip = UIFactory.Button("EnergyChip", p, new Vector2(190f, 64f), new Vector2(-35f, topY),
                                          T.panel, null, shape: ClayGraphic.Shape.Pill);
        energyChip.Clay.shadowSpread = 16f;
        energyChip.Clay.shadowAlpha = 0.25f;
        energyChip.Clay.innerHighlight = 0.25f;
        if (icons != null && icons.heart3D != null)
            UIFactory.Icon("HeartIcon", energyChip.content, new Vector2(40f, 40f), new Vector2(-60f, 0f),
                           icons.heart3D, Color.white);
        else
            UIFactory.Icon("TimerIcon", energyChip.content, new Vector2(28f, 28f), new Vector2(-60f, 0f),
                           UIIconSet.Icon("timer"), T.primary);

        var energyLabel = UIFactory.Label("EnergyText", energyChip.content, new Vector2(75f, 26f), new Vector2(-10f, 10f),
                                          "18/20", UIFontSet.Current.numeric, 22f, Color.white, TextAlignmentOptions.Center);
        var energyTimer = UIFactory.Label("EnergyTimer", energyChip.content, new Vector2(75f, 18f), new Vector2(-10f, -12f),
                                          "04:12", UIFontSet.Current.numeric, 15f, T.gold, TextAlignmentOptions.Center);

        var energyPlus = UIFactory.Clay("Plus", energyChip.content, new Vector2(32f, 32f), new Vector2(65f, 0f),
                                        T.success, ClayGraphic.Shape.Circle);
        energyPlus.raycastTarget = false;
        UIFactory.Label("PlusTxt", energyPlus.transform, new Vector2(32f, 32f), new Vector2(0f, 2f),
                        "+", UIFontSet.Current.display, 22f, Color.white, TextAlignmentOptions.Center);

        // Coins Chip with 3D Coin + Plus Button
        var coinsChip = UIFactory.Button("CoinsChip", p, new Vector2(200f, 64f), new Vector2(185f, topY),
                                         T.panel, null, shape: ClayGraphic.Shape.Pill);
        coinsChip.Clay.shadowSpread = 16f;
        coinsChip.Clay.shadowAlpha = 0.25f;
        coinsChip.Clay.innerHighlight = 0.25f;
        if (icons != null && icons.coin3D != null)
            UIFactory.Icon("Coin3D", coinsChip.content, new Vector2(44f, 44f), new Vector2(-68f, 0f),
                           icons.coin3D, Color.white);
        else
            UIFactory.Icon("CoinIcon", coinsChip.content, new Vector2(30f, 30f), new Vector2(-68f, 0f),
                           UIIconSet.Icon("emoji_events"), T.gold);
        var coinsLabel = UIFactory.Label("CoinsText", coinsChip.content, new Vector2(95f, 36f), new Vector2(-5f, 0f),
                                         "12,450", UIFontSet.Current.numeric, 24f, T.gold, TextAlignmentOptions.Center);
        var coinsPlus = UIFactory.Clay("Plus", coinsChip.content, new Vector2(32f, 32f), new Vector2(70f, 0f),
                                       T.success, ClayGraphic.Shape.Circle);
        coinsPlus.raycastTarget = false;
        UIFactory.Label("PlusTxt", coinsPlus.transform, new Vector2(32f, 32f), new Vector2(0f, 2f),
                        "+", UIFontSet.Current.display, 22f, Color.white, TextAlignmentOptions.Center);

        // Settings Button (Right)
        var settings = UIFactory.CircleButton("SettingsButton", p, 68f, new Vector2(415f, topY), T.panelHigh);
        settings.Clay.shadowSpread = 18f;
        settings.Clay.shadowAlpha = 0.30f;
        settings.Clay.innerHighlight = 0.35f;
        UIFactory.Icon("GearIcon", settings.content, new Vector2(36f, 36f), Vector2.zero,
                       UIIconSet.Icon("settings"), Color.white);

        // ── Floating Live-Ops Badges (Monopoly GO Signature) ─────────────────────
        // Left: Grand Tournament Arena Capsule
        var tour = UIFactory.Button("TournamentsButton", p, new Vector2(230f, 130f), new Vector2(-390f, 570f),
                                    T.panel, null, shape: ClayGraphic.Shape.Panel);
        tour.Clay.cornerRadius = T.radiusMd;
        tour.Clay.shadowAlpha = 0.30f;
        tour.Clay.shadowSpread = 18f;
        tour.Clay.innerHighlight = 0.25f;
        var tourGold = UIFactory.Clay("GoldAccent", tour.content, new Vector2(210f, 3f), new Vector2(0f, 58f),
                                      T.gold, ClayGraphic.Shape.Pill);
        tourGold.raycastTarget = false;
        UIFactory.Icon("TrophyIcon", tour.content, new Vector2(36f, 36f), new Vector2(0f, 28f),
                       UIIconSet.Icon("emoji_events"), T.gold);
        var tourTitle = UIFactory.Label("TourTitle", tour.content, new Vector2(220f, 22f), new Vector2(0f, -4f),
                                       "TOURNAMENT", UIFontSet.Current.body, 16f, Color.white, TextAlignmentOptions.Center);
        var tourSub = UIFactory.Label("TourSub", tour.content, new Vector2(220f, 20f), new Vector2(0f, -24f),
                                      "Rank #3 • 18h left", UIFontSet.Current.bodyLight, 14f, T.gold, TextAlignmentOptions.Center);

        // Right: Daily Fortune Streak Capsule
        var streakWidget = UIFactory.Button("StreakButton", p, new Vector2(230f, 130f), new Vector2(390f, 570f),
                                            T.panel, null, shape: ClayGraphic.Shape.Panel);
        streakWidget.Clay.cornerRadius = T.radiusMd;
        streakWidget.Clay.shadowAlpha = 0.30f;
        streakWidget.Clay.shadowSpread = 18f;
        streakWidget.Clay.innerHighlight = 0.25f;
        var streakGold = UIFactory.Clay("GoldAccent", streakWidget.content, new Vector2(210f, 3f), new Vector2(0f, 58f),
                                        T.gold, ClayGraphic.Shape.Pill);
        streakGold.raycastTarget = false;
        UIFactory.Icon("FlameIcon", streakWidget.content, new Vector2(36f, 36f), new Vector2(0f, 28f),
                       UIIconSet.Icon("timer"), Hex("F97316"));
        UIFactory.Label("StreakTitle", streakWidget.content, new Vector2(220f, 22f), new Vector2(0f, -4f),
                        "DAILY STREAK", UIFontSet.Current.body, 16f, Color.white, TextAlignmentOptions.Center);
        UIFactory.Label("StreakSub", streakWidget.content, new Vector2(220f, 20f), new Vector2(0f, -24f),
                        "5-Day • 24h left", UIFontSet.Current.bodyLight, 14f, Hex("F97316"), TextAlignmentOptions.Center);

        // ── Center Stage: Pure Typographic 3D LUDO NX Logo (Independent UI Text Layer) ────
        var logoRoot = UIFactory.Rect("WordmarkLogo", p, new Vector2(600f, 110f), new Vector2(0f, 570f));

        // 3D Shadow extrusion layer for the text logo
        var logoShadow = UIFactory.Label("WordmarkShadow", logoRoot.transform, new Vector2(600f, 110f), new Vector2(0f, -6f),
                                         "LUDO NX", UIFontSet.Current.display, 92f, Hex("020A05"), TextAlignmentOptions.Center);
        logoShadow.characterSpacing = 6f;
        logoShadow.raycastTarget = false;

        // Foreground metallic gold & platinum wordmark text logo
        var logoForeground = UIFactory.Label("WordmarkText", logoRoot.transform, new Vector2(600f, 110f), Vector2.zero,
                                             "LUDO NX", UIFontSet.Current.display, 92f, T.gold, TextAlignmentOptions.Center);
        logoForeground.characterSpacing = 6f;
        logoForeground.fontStyle = FontStyles.Bold;
        logoForeground.raycastTarget = false;

        // Season subtitle pill (Obsidian capsule with gold hairline border & gold text)
        var tagBadge = UIFactory.Clay("SeasonPill", p, new Vector2(430f, 40f), new Vector2(0f, 430f),
                                      T.gold, ClayGraphic.Shape.Pill);
        tagBadge.raycastTarget = false;
        tagBadge.shadowSpread = 14f;
        tagBadge.shadowAlpha = 0.35f;

        var tagInner = UIFactory.Clay("TagInner", tagBadge.transform, new Vector2(424f, 34f), Vector2.zero,
                                      Hex("0B1728"), ClayGraphic.Shape.Pill);
        tagInner.raycastTarget = false;

        var tagLabel = UIFactory.Label("Tagline", tagInner.transform, new Vector2(400f, 30f), Vector2.zero,
                                       "SEASON 1 • CHAMPIONSHIP", UIFontSet.Current.body, 17f, T.gold, TextAlignmentOptions.Center);
        tagLabel.characterSpacing = 2f;
        tagLabel.fontStyle = FontStyles.Bold;

        // ── Hero Action: Weighted Luxury Ruby & Gold PLAY Button ────────────────
        var playGoldRing = UIFactory.Clay("PlayGoldRing", p, new Vector2(934f, 167f), new Vector2(0f, 260f),
                                          T.gold, ClayGraphic.Shape.Pill);
        playGoldRing.raycastTarget = false;
        playGoldRing.shadowSpread = 28f;
        playGoldRing.shadowAlpha = 0.45f;
        playGoldRing.innerHighlight = 0.50f;

        var play = UIFactory.Button("PlayButton", p, new Vector2(920f, 155f), new Vector2(0f, 260f),
                                    T.primary, null, 64f, ClayGraphic.Shape.Pill, glow: 0.45f, hero: true);
        play.Clay.glowSpread = 50f;
        play.Clay.innerHighlight = 0.45f;
        play.breathe = true;
        var playIcon = UIFactory.Icon("PlayIcon", play.content, new Vector2(52f, 52f), new Vector2(-110f, 10f),
                                      UIIconSet.Icon("play_arrow"), Color.white);
        var playTxt = UIFactory.Label("PlayText", play.content, new Vector2(300f, 65f), new Vector2(25f, 12f),
                                      "PLAY", UIFontSet.Current.display, 64f, Color.white);
        playTxt.characterSpacing = 4f;

        var playSubPill = UIFactory.Clay("PlaySubPill", play.content, new Vector2(380f, 34f), new Vector2(0f, -42f),
                                         new Color(0f, 0f, 0f, 0.45f), ClayGraphic.Shape.Pill);
        playSubPill.raycastTarget = false;
        UIFactory.Label("SubText", playSubPill.transform, new Vector2(370f, 30f), Vector2.zero,
                        "CLASSIC 4P  •  1x MULTIPLIER", UIFontSet.Current.body, 19f, new Color(1f, 0.95f, 0.8f));

        // ── Daily Objective Card (Clean White Linen Slab with Gold Accent) ────────
        var missionsCard = UIFactory.Button("MissionsCard", p, new Vector2(920f, 120f), new Vector2(0f, 90f),
                                            Hex("FFFFFF"), null, shape: ClayGraphic.Shape.Panel);
        missionsCard.Clay.cornerRadius = T.radiusLg;
        missionsCard.Clay.shadowAlpha = 0.30f;
        missionsCard.Clay.shadowSpread = 24f;
        missionsCard.Clay.innerHighlight = 0.15f;

        var missGold = UIFactory.Clay("GoldAccent", missionsCard.content, new Vector2(880f, 4f), new Vector2(0f, 56f),
                                      T.gold, ClayGraphic.Shape.Pill);
        missGold.raycastTarget = false;

        if (icons != null && icons.gemGreen3D != null)
            UIFactory.Icon("TaskIcon", missionsCard.content, new Vector2(46f, 46f), new Vector2(-380f, 0f),
                           icons.gemGreen3D, Color.white);
        else
            UIFactory.Icon("TaskIcon", missionsCard.content, new Vector2(44f, 44f), new Vector2(-380f, 0f),
                           UIIconSet.Icon("check"), T.success);

        var missTitle = UIFactory.Body("MissTitle", missionsCard.content, new Vector2(520f, 32f), new Vector2(-60f, 14f),
                                       "DAILY OBJECTIVES", Hex("121826"), 22f);
        missTitle.font = UIFontSet.Current.body;
        missTitle.fontStyle = FontStyles.Bold;
        missTitle.alignment = TextAlignmentOptions.Left;

        var missSub = UIFactory.Label("MissSub", missionsCard.content, new Vector2(520f, 26f), new Vector2(-60f, -14f),
                                      "Capture 3 Pawns • 2/3 Complete", UIFontSet.Current.body, 18f, Hex("64748B"), TextAlignmentOptions.Left);

        var claimPill = UIFactory.Clay("ClaimPill", missionsCard.content, new Vector2(130f, 48f), new Vector2(360f, 0f),
                                       T.success, ClayGraphic.Shape.Pill);
        claimPill.raycastTarget = false;
        var claimTxt = UIFactory.Body("ClaimLabel", claimPill.transform, new Vector2(130f, 48f), Vector2.zero,
                                      "+50 NX", Color.white, 21f);
        claimTxt.characterSpacing = 2f;
        claimTxt.fontStyle = FontStyles.Bold;

        // ── Dual Mode Shortcuts (Classic vs Quick Rush) ──────────────────────────
        var classicBtn = UIFactory.Button("ClassicModeCard", p, new Vector2(450f, 136f), new Vector2(-238f, -75f),
                                          T.secondary, null, shape: ClayGraphic.Shape.Panel);
        classicBtn.Clay.cornerRadius = T.radiusMd;
        classicBtn.Clay.shadowAlpha = 0.35f;
        classicBtn.Clay.shadowSpread = 22f;
        classicBtn.Clay.innerHighlight = 0.35f;

        var classicGoldLine = UIFactory.Clay("GoldAccent", classicBtn.content, new Vector2(420f, 4f), new Vector2(0f, 62f),
                                             T.gold, ClayGraphic.Shape.Pill);
        classicGoldLine.raycastTarget = false;

        if (icons != null && icons.pawns != null && icons.pawns.Length > 0 && icons.pawns[0] != null)
            UIFactory.Icon("PawnIcon", classicBtn.content, new Vector2(50f, 50f), new Vector2(-145f, 0f),
                           icons.pawns[0], Color.white);
        var classicTitle = UIFactory.Label("ClassicTitle", classicBtn.content, new Vector2(250f, 36f), new Vector2(30f, 16f),
                                           "CLASSIC", UIFontSet.Current.display, 28f, Color.white, TextAlignmentOptions.Left);
        var classicSub = UIFactory.Label("ClassicSub", classicBtn.content, new Vector2(250f, 28f), new Vector2(30f, -16f),
                                         "4 Players • Classic", UIFontSet.Current.body, 19f, new Color(0.85f, 0.92f, 1f), TextAlignmentOptions.Left);

        var quickRushBtn = UIFactory.Button("QuickRushCard", p, new Vector2(450f, 136f), new Vector2(238f, -75f),
                                            T.neutral, null, shape: ClayGraphic.Shape.Panel);
        quickRushBtn.Clay.cornerRadius = T.radiusMd;
        quickRushBtn.Clay.shadowAlpha = 0.35f;
        quickRushBtn.Clay.shadowSpread = 22f;
        quickRushBtn.Clay.innerHighlight = 0.35f;

        var rushGoldLine = UIFactory.Clay("GoldAccent", quickRushBtn.content, new Vector2(420f, 4f), new Vector2(0f, 62f),
                                          T.gold, ClayGraphic.Shape.Pill);
        rushGoldLine.raycastTarget = false;

        UIFactory.Icon("RushIcon", quickRushBtn.content, new Vector2(46f, 46f), new Vector2(-145f, 0f),
                       UIIconSet.Icon("timer"), Color.white);
        var rushTitle = UIFactory.Label("RushTitle", quickRushBtn.content, new Vector2(250f, 36f), new Vector2(30f, 16f),
                                        "QUICK RUSH", UIFontSet.Current.display, 28f, Color.white, TextAlignmentOptions.Left);
        var rushSub = UIFactory.Label("RushSub", quickRushBtn.content, new Vector2(250f, 28f), new Vector2(30f, -16f),
                                      "2 Pawns • Blitz", UIFontSet.Current.body, 19f, new Color(0.85f, 1f, 0.90f), TextAlignmentOptions.Left);

        // ── Bottom Nav Bar (5 Tabs: Shop, Missions, Home, Collections, Friends) ──
        home.navBar = BuildNavBar(p, T, HomeNavBar.Tab.Home);

        // Wire
        home.playButton = play;
        home.settingsButton = settings;
        home.avatarButton = avatar;
        home.avatarRankBorder = avatarRankBorder;
        home.levelBadge = levelBadge;
        home.levelStageLabel = lvlNum;
        home.tournamentsButton = tour;
        home.streakButton = streakWidget;
        home.missionsButton = missionsCard;
        home.classicModeButton = classicBtn;
        home.quickModeButton = quickRushBtn;
        home.playerNameLabel = nameLabel;
        home.playerLevelLabel = levelLabel;
        home.energyChipButton = energyChip;
        home.energyChipLabel = energyLabel;
        home.energyTimerLabel = energyTimer;
        home.coinsChipButton = coinsChip;
        home.coinsChipLabel = coinsLabel;

        SavePrefab(root, ScreenDir + "/Home.prefab");
    }

    private static HomeNavBar BuildNavBar(Transform parent, UITheme T, HomeNavBar.Tab currentTab)
    {
        var icons = UIIconSet.Current;
        var barRoot = UIFactory.Rect("NavBar", parent, new Vector2(980f, 120f), new Vector2(0f, -885f));

        // Dock base panel (Brushed obsidian glass console deck)
        var dockBg = UIFactory.Clay("NavDock", barRoot, new Vector2(980f, 112f), Vector2.zero,
                                    T.navBarBg, ClayGraphic.Shape.Panel, 26f);
        dockBg.raycastTarget = false;
        dockBg.shadowSpread = 20f;
        dockBg.shadowAlpha = 0.35f;
        dockBg.innerHighlight = 0.15f;

        // Sleek 24k gold hairline top rim
        var goldRim = UIFactory.Clay("GoldRim", barRoot, new Vector2(940f, 3f), new Vector2(0f, 55f),
                                     T.navGoldBorder, ClayGraphic.Shape.Pill);
        goldRim.raycastTarget = false;

        var nav = barRoot.gameObject.AddComponent<HomeNavBar>();
        nav.items = new List<HomeNavBar.NavItem>();

        // 5 User-requested tabs: Shop, Missions, Home, Collections, Friends
        HomeNavBar.Tab[] tabs = {
            HomeNavBar.Tab.Shop,
            HomeNavBar.Tab.Missions,
            HomeNavBar.Tab.Home,
            HomeNavBar.Tab.Collection,
            HomeNavBar.Tab.Friends
        };

        ScreenId[] screens = {
            ScreenId.Shop,
            ScreenId.Missions,
            ScreenId.Home,
            ScreenId.PawnCollection,
            ScreenId.Friends
        };

        string[] labels = { "SHOP", "MISSIONS", "HOME", "COLLECTIONS", "FRIENDS" };
        Sprite[] customIcons = {
            icons != null && icons.coin3D != null ? icons.coin3D : UIIconSet.Icon("inventory"),
            icons != null && icons.gemGreen3D != null ? icons.gemGreen3D : UIIconSet.Icon("check"),
            UIIconSet.Icon("home"),
            UIIconSet.Icon("collections") ?? UIIconSet.Icon("inventory"),
            UIIconSet.Icon("group") ?? UIIconSet.Icon("groups")
        };

        float podW = 172f;
        float podH = 88f;
        float startX = -360f;
        float stepX = 180f;

        for (int i = 0; i < 5; i++)
        {
            float x = startX + i * stepX;
            bool isCenter = (tabs[i] == HomeNavBar.Tab.Home);
            bool isSel = (tabs[i] == currentTab);
            float yPos = isCenter ? 14f : (isSel ? 6f : 0f);
            float curW = isCenter ? 186f : podW;
            float curH = isCenter ? 104f : podH;

            var itemBtn = UIFactory.Button("Nav_" + tabs[i], barRoot, new Vector2(curW, curH),
                                           new Vector2(x, yPos), new Color(0, 0, 0, 0), null,
                                           shape: ClayGraphic.Shape.Panel);
            itemBtn.Clay.shadowAlpha = 0f;
            itemBtn.Clay.innerHighlight = 0f;
            itemBtn.Clay.innerShadow = 0f;

            // Elevated Center Arch / Active Gold Border Ring
            var activeBorder = UIFactory.Clay("ActiveBorder", itemBtn.content, new Vector2(curW + 4f, curH + 4f),
                                              Vector2.zero, T.navGoldBorder, ClayGraphic.Shape.Panel, 22f);
            activeBorder.raycastTarget = false;
            activeBorder.shadowSpread = 16f;
            activeBorder.shadowAlpha = 0.40f;
            activeBorder.innerHighlight = 0.45f;
            activeBorder.gameObject.SetActive(isSel || isCenter);

            // Tactile Pod Panel
            Color podCol = isSel ? T.navPodActive : T.navPodDefault;
            var podPanel = UIFactory.Clay("PodPanel", itemBtn.content, new Vector2(curW, curH),
                                          Vector2.zero, podCol, ClayGraphic.Shape.Panel, 18f);
            podPanel.raycastTarget = false;
            podPanel.shadowSpread = isSel ? 18f : 12f;
            podPanel.shadowAlpha = isSel ? 0.35f : 0.18f;
            podPanel.innerHighlight = isSel ? 0.35f : 0.18f;
            podPanel.innerShadow = 0.15f;
            if (isSel)
            {
                podPanel.glowSpread = 22f;
                podPanel.glowAlpha = 0.30f;
            }

            // Icon
            var iconSprite = customIcons[i];
            Image iconImg = null;
            Color unselectedInk = new Color(0.85f, 0.89f, 0.96f, 0.95f);
            Color labelUnsel = new Color(0.72f, 0.80f, 0.90f, 1f);
            if (iconSprite != null)
            {
                bool is3D = (iconSprite == (icons != null ? icons.coin3D : null)) ||
                            (iconSprite == (icons != null ? icons.gemGreen3D : null));

                Color iconCol = isSel ? Color.white : (is3D ? Color.white : unselectedInk);
                float iconSize = isCenter ? 46f : 38f;
                iconImg = UIFactory.Icon("NavIcon", itemBtn.content, new Vector2(iconSize, iconSize),
                                         new Vector2(0f, isCenter ? 14f : 12f),
                                         iconSprite, iconCol);
                if (isSel) iconImg.transform.localScale = Vector3.one * 1.08f;
            }

            // Label
            var txt = UIFactory.Label("Label", itemBtn.content, new Vector2(curW - 8f, 20f),
                                      new Vector2(0f, isCenter ? -18f : -16f), labels[i], UIFontSet.Current.body,
                                      isCenter ? 17f : 14f,
                                      isSel ? T.gold : labelUnsel);
            txt.fontStyle = (isSel || isCenter) ? FontStyles.Bold : FontStyles.Normal;

            var navItem = new HomeNavBar.NavItem
            {
                tab = tabs[i],
                targetScreen = screens[i],
                button = itemBtn,
                backgroundPanel = podPanel,
                activeBorder = activeBorder,
                label = txt,
                iconImage = iconImg,
                defaultPos = new Vector2(x, isCenter ? 14f : 0f)
            };
            nav.items.Add(navItem);
        }

        return nav;
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
        var icons = UIIconSet.Current;

        GameObject root = NewScreenRoot("PlayMenu", ScreenId.PlayMenu);
        var screen = root.AddComponent<PlayMenuScreen>();
        Transform p = root.transform;

        UIFactory.Page(p, icons != null ? icons.bgBattleArena : null);
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
                        UIFontSet.Current.body, 26f, T.inkOnGroundDim, TextAlignmentOptions.Left).characterSpacing = 8f;

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

        UIFactory.Page(p, icons != null ? icons.bgBattleArena : null);
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
                        UIFontSet.Current.body, 26f, T.inkOnGroundDim, TextAlignmentOptions.Left).characterSpacing = 8f;

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
                        UIFontSet.Current.body, 26f, T.inkOnGroundDim, TextAlignmentOptions.Left).characterSpacing = 8f;

        screen.aboutRow = LinkRow(p, "AboutRow", "About Ludo NX", -150f, T);
        screen.privacyRow = LinkRow(p, "PrivacyRow", "Privacy Policy", -294f, T);
        screen.termsRow = LinkRow(p, "TermsRow", "Terms of Service", -438f, T);

        screen.versionLabel = UIFactory.Label("Version", p, new Vector2(880f, 44f), new Vector2(0f, -600f),
                                              "Version 1.0", UIFontSet.Current.bodyLight, 26f, T.inkOnGroundDim);

        SavePrefab(root, ScreenDir + "/Settings.prefab");
    }

    [MenuItem("Ludo NX/UI/12. Build Shop", priority = 112)]
    public static void BuildShop()
    {
        CreateSupportAssets();
        var T = UITheme.Current;
        var icons = UIIconSet.Current;

        GameObject root = NewScreenRoot("Shop", ScreenId.Shop);
        var screen = root.AddComponent<ShopScreen>();
        Transform p = root.transform;

        UIFactory.Page(p, icons != null ? icons.bgShopVault : null);
        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "SHOP", out title);
        screen.titleLabel = title;

        // Category Tabs
        var catRow = UIFactory.Rect("CatRow", p, new Vector2(920f, 70f), new Vector2(0f, 710f));
        float tabW = 145f;
        screen.featuredTab = UIFactory.Button("TabFeatured", catRow, new Vector2(tabW, 64f),
                                              new Vector2(-375f + 0 * tabW, 0f), T.primary, "FEATURED", 20f,
                                              ClayGraphic.Shape.Pill);
        screen.diceTab = UIFactory.Button("TabDice", catRow, new Vector2(tabW, 64f),
                                          new Vector2(-375f + 1 * tabW, 0f), T.neutral, "DICE", 20f,
                                          ClayGraphic.Shape.Pill);
        screen.pawnsTab = UIFactory.Button("TabPawns", catRow, new Vector2(tabW, 64f),
                                           new Vector2(-375f + 2 * tabW, 0f), T.neutral, "PAWNS", 20f,
                                           ClayGraphic.Shape.Pill);
        screen.boardsTab = UIFactory.Button("TabBoards", catRow, new Vector2(tabW, 64f),
                                            new Vector2(-375f + 3 * tabW, 0f), T.neutral, "BOARDS", 20f,
                                            ClayGraphic.Shape.Pill);
        screen.bundlesTab = UIFactory.Button("TabBundles", catRow, new Vector2(tabW, 64f),
                                             new Vector2(-375f + 4 * tabW, 0f), T.neutral, "BUNDLES", 20f,
                                             ClayGraphic.Shape.Pill);
        screen.currencyTab = UIFactory.Button("TabCurrency", catRow, new Vector2(tabW, 64f),
                                              new Vector2(-375f + 5 * tabW, 0f), T.neutral, "COINS", 20f,
                                              ClayGraphic.Shape.Pill);

        // ── Energy panel ──────────────────────────────────────────────────────────────────
        var energyCard = UIFactory.Clay("EnergyCard", p, new Vector2(880f, 260f), new Vector2(0f, 520f),
                                        T.panel, ClayGraphic.Shape.Panel, T.radiusLg, UISkin.Role.PanelCard);

        UIFactory.Label("EnergyHeader", energyCard.transform, new Vector2(760f, 44f), new Vector2(0f, 106f),
                        "ENERGY", UIFontSet.Current.body, 26f, T.inkDim,
                        TextAlignmentOptions.Left).characterSpacing = 8f;

        screen.energyCountLabel = UIFactory.Display("EnergyCount", energyCard.transform,
                                                    new Vector2(400f, 90f), new Vector2(-180f, 34f),
                                                    "5 / 5", T.ink);
        screen.energyCountLabel.fontSize = 62f;
        screen.energyCountLabel.alignment = TextAlignmentOptions.Left;

        screen.energyTimerLabel = UIFactory.Label("EnergyTimer", energyCard.transform,
                                                  new Vector2(360f, 50f), new Vector2(190f, 34f),
                                                  "FULL", UIFontSet.Current.body, 28f, T.inkMuted,
                                                  TextAlignmentOptions.Right);

        // One pip per life — a bar the player can read without counting digits.
        var pips = new List<ClayGraphic>();
        const int pipCount = 5;
        float pipW = 140f, pipGap = 16f;
        float rowW = pipCount * pipW + (pipCount - 1) * pipGap;
        for (int i = 0; i < pipCount; i++)
        {
            float x = -rowW * 0.5f + pipW * 0.5f + i * (pipW + pipGap);
            var pip = UIFactory.Clay("Pip" + i, energyCard.transform, new Vector2(pipW, 26f),
                                     new Vector2(x, -74f), T.primary, ClayGraphic.Shape.Pill);
            pip.shadowAlpha = 0f;
            pips.Add(pip);
        }
        screen.energyPips = pips.ToArray();

        // ── Offers ────────────────────────────────────────────────────────────────────────
        UIFactory.Label("OffersHeader", p, new Vector2(880f, 44f), new Vector2(0f, 366f), "GET ENERGY",
                        UIFontSet.Current.body, 26f, T.inkOnGroundDim,
                        TextAlignmentOptions.Left).characterSpacing = 8f;

        screen.offers.Add(OfferRow(p, T, "ludonx_energy_5",  "5 Energy",  "Refill your tank",  5,  230f, T.secondary));
        screen.offers.Add(OfferRow(p, T, "ludonx_energy_15", "15 Energy", "Best value",        15,  70f, T.primary));
        screen.offers.Add(OfferRow(p, T, "ludonx_energy_40", "40 Energy", "Play all week",     40, -90f, T.gold));

        // ── Dice skins, advertised but not yet sellable ────────────────────────────────────
        UIFactory.Label("SkinsHeader", p, new Vector2(880f, 44f), new Vector2(0f, -210f), "DICE SKINS",
                        UIFontSet.Current.body, 26f, T.inkOnGroundDim,
                        TextAlignmentOptions.Left).characterSpacing = 8f;

        var soon = UIFactory.Clay("SkinsSoon", p, new Vector2(880f, 150f), new Vector2(0f, -310f),
                                  T.panel, ClayGraphic.Shape.Panel, T.radiusLg, UISkin.Role.PanelCard);
        UIFactory.Label("SoonLabel", soon.transform, new Vector2(760f, 50f), new Vector2(0f, 14f),
                        "Coming in a later update", UIFontSet.Current.body, 30f, T.inkMuted);
        UIFactory.Label("SoonSub", soon.transform, new Vector2(760f, 40f), new Vector2(0f, -30f),
                        "Collect and equip custom dice", UIFontSet.Current.bodyLight, 24f, T.inkDim);

        // ── Restore ───────────────────────────────────────────────────────────────────────
        // Google Play requires a reachable restore path for anything non-consumable, so this is
        // a store requirement rather than a nicety.
        screen.restoreButton = UIFactory.Button("RestoreButton", p, new Vector2(880f, 96f),
                                                new Vector2(0f, -450f), T.neutral,
                                                "RESTORE PURCHASES", 26f);

        screen.navBar = BuildNavBar(p, T, HomeNavBar.Tab.Shop);

        SavePrefab(root, ScreenDir + "/Shop.prefab");
    }

    /// <summary>One purchasable row: what you get, why, and the price on a coloured button.</summary>
    private static ShopOfferRow OfferRow(Transform p, UITheme T, string productId, string title,
                                         string blurb, int energy, float y, Color accent)
    {
        var card = UIFactory.Clay("Offer_" + productId, p, new Vector2(880f, 140f), new Vector2(0f, y),
                                  T.panel, ClayGraphic.Shape.Panel, T.radiusLg, UISkin.Role.PanelCard);

        UIFactory.Label("Title", card.transform, new Vector2(480f, 44f), new Vector2(-190f, 22f),
                        title, UIFontSet.Current.body, 34f, T.ink, TextAlignmentOptions.Left);
        UIFactory.Label("Blurb", card.transform, new Vector2(480f, 36f), new Vector2(-190f, -20f),
                        blurb, UIFontSet.Current.bodyLight, 24f, T.inkDim, TextAlignmentOptions.Left);

        var buy = UIFactory.Button("Buy", card.transform, new Vector2(240f, 92f), new Vector2(280f, 0f),
                                   accent, null, shape: ClayGraphic.Shape.Pill, glow: 0.12f);

        var price = UIFactory.Label("Price", buy.content, new Vector2(220f, 60f), Vector2.zero,
                                    "—", UIFontSet.Current.body, 30f, T.InkOn(accent));
        price.fontStyle = FontStyles.Bold;

        return new ShopOfferRow
        {
            productId = productId,
            buyButton = buy,
            priceLabel = price,
            fallbackPrice = "—",
            energyGranted = energy
        };
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

        // ── Tab row ───────────────────────────────────────────────────────────
        screen.identityTab = UIFactory.Button("IdentityTab", p, new Vector2(424f, 96f),
                                              new Vector2(-226f, 640f), T.secondary, "OVERVIEW", 28f);
        screen.achievementsTab = UIFactory.Button("AchievementsTab", p, new Vector2(424f, 96f),
                                                  new Vector2(226f, 640f), T.neutral, "AWARDS", 28f);

        // ── IDENTITY PANEL ─────────────────────────────────────────────────────
        var identity = UIFactory.Rect("IdentityPanel", p, new Vector2(1080f, 1600f), new Vector2(0f, -60f));
        screen.identityPanel = identity.gameObject;

        // Avatar ring
        var ring = UIFactory.Clay("AvatarRing", identity, new Vector2(224f, 224f), new Vector2(0f, 480f),
                                  T.panelHigh, ClayGraphic.Shape.Circle);
        ring.raycastTarget = false;
        var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/art_assets/generated/ui_circle.png");
        var inner = UIFactory.Icon("Avatar", ring.transform, new Vector2(196f, 196f), Vector2.zero,
                                   circleSprite, T.secondary);
        inner.raycastTarget = false;
        ring.gameObject.AddComponent<ProfileAvatarSlot>().diameter = 196f;

        // Name + Player ID
        screen.nameLabel = UIFactory.Display("Name", identity, new Vector2(880f, 80f), new Vector2(0f, 330f),
                                             "Player", T.ink);
        screen.nameLabel.fontSize = 58f;

        screen.playerIdLabel = UIFactory.Body("PlayerId", identity, new Vector2(880f, 42f),
                                              new Vector2(0f, 262f), "#000000", T.inkMuted, 26f);
        screen.playerIdLabel.characterSpacing = 4f;

        // Level + rank badge on same row
        screen.levelLabel = UIFactory.Body("Level", identity, new Vector2(400f, 44f),
                                           new Vector2(-220f, 196f), "LEVEL 1", T.inkMuted, 26f);
        screen.levelLabel.characterSpacing = 6f;
        screen.rankLabel = UIFactory.Body("Rank", identity, new Vector2(400f, 44f),
                                          new Vector2(220f, 196f), "BRONZE I", T.gold, 26f);
        screen.rankLabel.characterSpacing = 6f;

        // XP progress bar track
        var xpTrack = UIFactory.Clay("XPTrack", identity, new Vector2(760f, 20f), new Vector2(0f, 138f),
                                     T.recessed, ClayGraphic.Shape.Pill);
        xpTrack.raycastTarget = false;
        xpTrack.shadowAlpha = 0f;

        var xpFill = UIFactory.Clay("XPFill", xpTrack.transform, new Vector2(0f, 20f), Vector2.zero,
                                    T.secondary, ClayGraphic.Shape.Pill);
        xpFill.raycastTarget = false;
        xpFill.shadowAlpha = 0f;
        var xpFillRt = xpFill.rectTransform;
        xpFillRt.anchorMin = new Vector2(0f, 0.5f);
        xpFillRt.anchorMax = new Vector2(0f, 0.5f);
        xpFillRt.pivot     = new Vector2(0f, 0.5f);
        xpFillRt.anchoredPosition = Vector2.zero;
        screen.xpBarFill = xpFillRt;

        screen.xpLabel = UIFactory.Body("XPLabel", identity, new Vector2(760f, 36f),
                                        new Vector2(0f, 100f), "0 / 100 XP", T.inkMuted, 22f);

        // League + Country + Edit button
        screen.leagueLabel = UIFactory.Body("League", identity, new Vector2(380f, 40f),
                                            new Vector2(-240f, 40f), "OPEN LEAGUE", T.inkDim, 24f);
        screen.countryLabel = UIFactory.Body("Country", identity, new Vector2(280f, 40f),
                                             new Vector2(180f, 40f), "—", T.inkDim, 24f);
        screen.editButton = UIFactory.Button("EditButton", identity, new Vector2(280f, 82f),
                                            new Vector2(360f, 40f), T.neutral, "EDIT", 24f);

        // ── Career stat grid (2 × 4 tiles) ───────────────────────────────────
        // Row 1
        screen.winsValue = UIFactory.StatTile("WinsTile", identity, new Vector2(390f, 190f),
                                              new Vector2(-315f, -120f), "0", "WINS", T.gold);
        screen.matchesValue = UIFactory.StatTile("MatchesTile", identity, new Vector2(390f, 190f),
                                                 new Vector2(-105f, -120f), "0", "MATCHES", T.secondary);
        screen.winRateValue = UIFactory.StatTile("RateTile", identity, new Vector2(390f, 190f),
                                                 new Vector2(105f, -120f), "0%", "WIN RATE", T.success);
        screen.streakValue = UIFactory.StatTile("StreakTile", identity, new Vector2(390f, 190f),
                                                new Vector2(315f, -120f), "0", "BEST STREAK", T.primary);
        // Row 2
        screen.totalPointsValue = UIFactory.StatTile("PtsTile", identity, new Vector2(390f, 190f),
                                                     new Vector2(-315f, -340f), "0", "TOTAL PTS", T.gold);
        screen.totalCapturesValue = UIFactory.StatTile("CapTile", identity, new Vector2(390f, 190f),
                                                       new Vector2(-105f, -340f), "0", "CAPTURES", T.secondary);
        screen.bestScoreValue = UIFactory.StatTile("BestTile", identity, new Vector2(390f, 190f),
                                                   new Vector2(105f, -340f), "0", "BEST SCORE", T.primary);

        // VIEW FULL STATS chevron button
        screen.viewStatsButton = UIFactory.Button("ViewStatsBtn", identity, new Vector2(840f, 128f),
                                                  new Vector2(0f, -540f), T.panel, "VIEW FULL STATISTICS  ›", 30f,
                                                  ClayGraphic.Shape.Panel, glow: 0f);
        screen.viewStatsButton.Clay.cornerRadius = T.radiusMd;
        screen.viewStatsButton.Clay.innerHighlight = 0.04f;
        var vsLabel = screen.viewStatsButton.GetComponentInChildren<TextMeshProUGUI>();
        if (vsLabel != null) { vsLabel.color = UITheme.Lin(T.ink); vsLabel.alignment = TextAlignmentOptions.Left; }

        // ── ACHIEVEMENTS PANEL ────────────────────────────────────────────────
        var awards = UIFactory.Rect("AchievementsPanel", p, new Vector2(1080f, 1200f), new Vector2(0f, -120f));
        screen.achievementsPanel = awards.gameObject;

        screen.achievementSummary = UIFactory.Body("Summary", awards, new Vector2(880f, 50f),
                                                   new Vector2(0f, 500f), "0 of 20 unlocked", T.inkMuted, 28f);

        var viewport = UIFactory.Rect("Viewport", awards, new Vector2(940f, 980f), new Vector2(0f, -40f));
        var vpImage = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
        vpImage.color = new Color(1f, 1f, 1f, 0.004f);
        var mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();
        mask.showMaskGraphic = false;

        var content = UIFactory.Rect("Content", viewport, new Vector2(900f, 2000f), Vector2.zero);
        content.anchorMin = new Vector2(0.5f, 1f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot     = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        screen.achievementContent = content;

        var scroll = awards.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.content         = content;
        scroll.viewport        = viewport;
        scroll.horizontal      = false;
        scroll.vertical        = true;
        scroll.movementType    = UnityEngine.UI.ScrollRect.MovementType.Elastic;
        scroll.elasticity      = 0.1f;
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
        LudoNXMatchIntroBuilder.Build();
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
                                   "Ludo NX", UIFontSet.Current.numeric, 100f, T.inkOnGround);
        word.characterSpacing = -2f;

        UIFactory.Label("Tagline", p, new Vector2(1000f, 70f), new Vector2(0f, -190f),
                        "Roll into a new world", UIFontSet.Current.bodyLight, 44f, T.inkOnGroundDim);

        // â”€â”€ Status row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Flush with the ends of the bar below (Â±485), as in the reference.
        screen.statusLabel = UIFactory.Label("Status", p, new Vector2(500f, 50f), new Vector2(-235f, -391f),
                                             "Loading...", UIFontSet.Current.body, 34f, T.inkOnGroundDim,
                                             TextAlignmentOptions.Left);
        screen.versionLabel = UIFactory.Label("Version", p, new Vector2(400f, 50f), new Vector2(285f, -391f),
                                              "v1.0", UIFontSet.Current.bodyLight, 30f, T.inkOnGroundDim,
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

    [MenuItem("Ludo NX/UI/8b. Build Maintenance + Update Required", priority = 8)]
    public static void BuildSystemTakeovers()
    {
        BuildMaintenance();
        BuildVersionTooOld();
    }

    /// <summary>Full takeover for MaintenanceService.IsUnderMaintenance — connection-states.json.
    /// Same 1080x1920 frame and Page/Clay/Label conventions as Splash, since Splash is what
    /// routes here.</summary>
    public static void BuildMaintenance()
    {
        CreateSupportAssets();
        var T = UITheme.Current;
        var icons = UIIconSet.Current;

        GameObject root = NewScreenRoot("Maintenance", ScreenId.Maintenance);
        var screen = root.AddComponent<MaintenanceScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        if (icons != null && icons.mascot != null)
            UIFactory.Icon("Mascot", p, new Vector2(520f, 520f), new Vector2(0f, 380f), icons.mascot, Color.white);

        screen.titleLabel = UIFactory.Label("Title", p, new Vector2(900f, 110f), new Vector2(0f, 20f),
                                            "Back soon", UIFontSet.Current.display, 76f, T.inkOnGround);

        screen.messageLabel = UIFactory.Label("Message", p, new Vector2(820f, 220f), new Vector2(0f, -140f),
                                              "", UIFontSet.Current.body, 38f, T.inkOnGroundDim);
        screen.messageLabel.textWrappingMode = TextWrappingModes.Normal;

        screen.supportButton = UIFactory.Button("SupportButton", p, new Vector2(480f, 110f), new Vector2(0f, -500f),
                                                T.gold, "SUPPORT");

        SavePrefab(root, ScreenDir + "/Maintenance.prefab");
    }

    /// <summary>Full takeover blocking play when the client is older than
    /// MaintenanceService.MinimumVersion — connection-states.json's "version too old".</summary>
    public static void BuildVersionTooOld()
    {
        CreateSupportAssets();
        var T = UITheme.Current;
        var icons = UIIconSet.Current;

        GameObject root = NewScreenRoot("VersionTooOld", ScreenId.VersionTooOld);
        var screen = root.AddComponent<VersionTooOldScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        if (icons != null && icons.diceLogo != null)
            UIFactory.Icon("Mark", p, new Vector2(420f, 420f), new Vector2(0f, 380f), icons.diceLogo, Color.white);

        screen.titleLabel = UIFactory.Label("Title", p, new Vector2(900f, 110f), new Vector2(0f, 20f),
                                            "Update required", UIFontSet.Current.display, 68f, T.inkOnGround);

        screen.messageLabel = UIFactory.Label("Message", p, new Vector2(820f, 220f), new Vector2(0f, -140f),
                                              "", UIFontSet.Current.body, 38f, T.inkOnGroundDim);
        screen.messageLabel.textWrappingMode = TextWrappingModes.Normal;

        screen.updateButton = UIFactory.Button("UpdateButton", p, new Vector2(480f, 110f), new Vector2(0f, -500f),
                                               T.primary, "UPDATE");

        SavePrefab(root, ScreenDir + "/VersionTooOld.prefab");
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

        // Pause — rebuilt on the modern card_shell chassis with Lucky Six dice
        LudoNXMatchModalsBuilder.BuildPauseModal();

        // Language picker — now built on the Menu popup's PNG chassis, not the Clay system.
        LudoNXMenuModalBuilder.BuildLanguagePicker();

        // Text page (About / Privacy / Terms)
        // TextPage: now built on the Menu popup's PNG chassis, not the Clay system.
        LudoNXMenuModalBuilder.BuildTextPage();

        // Edit profile
        // EditProfile: now built on the Menu popup's PNG chassis, not the Clay system.
        LudoNXMenuModalBuilder.BuildEditProfile();

        // Energy Modal
        {
            GameObject root = NewModalRoot("EnergyModal");
            var modal = root.AddComponent<EnergyModal>();
            var card = ModalCard(root.transform, new Vector2(880f, 1140f), T, modal);

            UIFactory.Display("Title", card, new Vector2(760f, 90f), new Vector2(0f, 470f),
                              "ENERGY & LIVES", T.ink).fontSize = 50f;

            var pipsGroup = UIFactory.Rect("PipsGroup", card, new Vector2(500f, 80f), new Vector2(0f, 360f));
            modal.energyPips = new ClayGraphic[5];
            for (int i = 0; i < 5; i++)
            {
                float x = -160f + i * 80f;
                var pip = UIFactory.Clay("Pip_" + i, pipsGroup, new Vector2(60f, 60f),
                                         new Vector2(x, 0f), T.primary, ClayGraphic.Shape.Circle);
                pip.raycastTarget = false;
                modal.energyPips[i] = pip;
            }

            modal.energyCountLabel = UIFactory.Display("Count", card, new Vector2(760f, 80f),
                                                       new Vector2(0f, 260f), "5 / 5", T.ink);
            modal.energyCountLabel.fontSize = 58f;

            modal.timerLabel = UIFactory.Body("Timer", card, new Vector2(760f, 50f),
                                              new Vector2(0f, 190f), "FULL CAPACITY", T.success, 28f);

            var adBtn = UIFactory.Button("WatchAdBtn", card, new Vector2(760f, 140f),
                                         new Vector2(0f, 60f), T.gold, "WATCH AD (+1 LIFE)", 34f,
                                         ClayGraphic.Shape.Pill, glow: 0.12f);
            modal.watchAdButton = adBtn;

            var coinsBtn = UIFactory.Button("RefillCoinsBtn", card, new Vector2(760f, 140f),
                                            new Vector2(0f, -110f), T.success, "REFILL (150 COINS)", 34f,
                                            ClayGraphic.Shape.Pill, glow: 0.12f);
            modal.refillCoinsButton = coinsBtn;

            var shopBtn = UIFactory.Button("ShopBtn", card, new Vector2(760f, 120f),
                                           new Vector2(0f, -270f), T.secondary, "GET UNLIMITED IN SHOP", 30f);
            modal.shopButton = shopBtn;

            var closeBtn = UIFactory.Button("CloseBtn", card, new Vector2(760f, 110f),
                                            new Vector2(0f, -420f), T.neutral, "CLOSE", 32f);
            modal.closeButton = closeBtn;

            SavePrefabRaw(root, ModalDir + "/EnergyModal.prefab");
        }

        // Missions Modal
        {
            GameObject root = NewModalRoot("MissionsModal");
            var modal = root.AddComponent<MissionsModal>();
            var card = ModalCard(root.transform, new Vector2(920f, 1280f), T, modal);

            modal.headerTitleLabel = UIFactory.Display("Title", card, new Vector2(760f, 80f),
                                                       new Vector2(0f, 550f), "MISSIONS & QUESTS", T.ink);
            modal.headerTitleLabel.fontSize = 48f;

            modal.timerResetLabel = UIFactory.Body("Timer", card, new Vector2(760f, 40f),
                                                  new Vector2(0f, 480f), "RESETS IN 18H 30M", T.inkMuted, 24f);

            var dailyTab = UIFactory.Button("DailyTab", card, new Vector2(370f, 84f),
                                            new Vector2(-195f, 400f), T.secondary, "DAILY", 28f);
            modal.dailyTabButton = dailyTab;

            var weeklyTab = UIFactory.Button("WeeklyTab", card, new Vector2(370f, 84f),
                                             new Vector2(195f, 400f), T.neutral, "WEEKLY", 28f);
            modal.weeklyTabButton = weeklyTab;

            var list = UIFactory.Rect("MissionList", card, new Vector2(820f, 540f), new Vector2(0f, 50f));
            modal.missionListContainer = list;

            // Template row
            var tRow = UIFactory.Clay("TemplateRow", list, new Vector2(800f, 140f), Vector2.zero,
                                      T.panelHigh, ClayGraphic.Shape.Panel, T.radiusMd);
            tRow.raycastTarget = false;

            UIFactory.Label("Title", tRow.transform, new Vector2(460f, 36f), new Vector2(-130f, 34f),
                            "Mission Title", UIFontSet.Current.body, 28f, T.ink, TextAlignmentOptions.Left);
            UIFactory.Label("Desc", tRow.transform, new Vector2(460f, 30f), new Vector2(-130f, 0f),
                            "Mission Description", UIFontSet.Current.bodyLight, 22f, T.inkMuted, TextAlignmentOptions.Left);
            UIFactory.Label("Count", tRow.transform, new Vector2(180f, 30f), new Vector2(270f, 34f),
                            "0/1", UIFontSet.Current.numeric, 24f, T.inkDim, TextAlignmentOptions.Right);

            var bar = UIFactory.Clay("Bar", tRow.transform, new Vector2(460f, 14f), new Vector2(-130f, -34f),
                                     T.recessed, ClayGraphic.Shape.Pill);
            bar.raycastTarget = false;
            bar.shadowAlpha = 0f;
            var fill = UIFactory.Clay("Fill", bar.transform, new Vector2(200f, 14f), Vector2.zero,
                                      T.success, ClayGraphic.Shape.Pill);
            fill.raycastTarget = false;
            fill.shadowAlpha = 0f;
            var fillRt = fill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(0f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;

            var claim = UIFactory.Button("ClaimButton", tRow.transform, new Vector2(180f, 70f),
                                         new Vector2(270f, -14f), T.success, "CLAIM", 24f, ClayGraphic.Shape.Pill);

            UIFactory.Label("Reward", tRow.transform, new Vector2(180f, 26f), new Vector2(270f, -54f),
                            "+100 COINS", UIFontSet.Current.numeric, 18f, T.gold, TextAlignmentOptions.Center);

            tRow.gameObject.SetActive(false);
            modal.missionRowTemplate = (RectTransform)tRow.transform;

            var claimAll = UIFactory.Button("ClaimAllBtn", card, new Vector2(780f, 110f),
                                            new Vector2(0f, -340f), T.success, "CLAIM ALL COMPLETED", 32f,
                                            ClayGraphic.Shape.Pill, glow: 0.12f);
            modal.claimAllButton = claimAll;

            var close = UIFactory.Button("CloseBtn", card, new Vector2(780f, 96f),
                                         new Vector2(0f, -470f), T.neutral, "CLOSE", 28f);
            modal.closeButton = close;

            SavePrefabRaw(root, ModalDir + "/MissionsModal.prefab");
        }

        // Wallet Modal
        {
            GameObject root = NewModalRoot("WalletModal");
            var modal = root.AddComponent<WalletModal>();
            var card = ModalCard(root.transform, new Vector2(880f, 1220f), T, modal);

            UIFactory.Display("Title", card, new Vector2(760f, 80f), new Vector2(0f, 510f),
                              "WALLET & BANK", T.ink).fontSize = 48f;

            // Balance box
            var balCard = UIFactory.Clay("BalanceCard", card, new Vector2(760f, 120f), new Vector2(0f, 380f),
                                         T.panelHigh, ClayGraphic.Shape.Panel, T.radiusMd);
            balCard.raycastTarget = false;
            UIFactory.Label("CoinsHeader", balCard.transform, new Vector2(300f, 30f), new Vector2(-180f, 24f),
                            "COIN BALANCE", UIFontSet.Current.bodyLight, 22f, T.inkDim, TextAlignmentOptions.Left);
            modal.coinsBalanceLabel = UIFactory.Label("CoinsVal", balCard.transform, new Vector2(300f, 50f),
                                                      new Vector2(-180f, -18f), "1,250", UIFontSet.Current.numeric,
                                                      42f, T.gold, TextAlignmentOptions.Left);

            UIFactory.Label("EnergyHeader", balCard.transform, new Vector2(300f, 30f), new Vector2(180f, 24f),
                            "ENERGY", UIFontSet.Current.bodyLight, 22f, T.inkDim, TextAlignmentOptions.Right);
            modal.energyBalanceLabel = UIFactory.Label("EnergyVal", balCard.transform, new Vector2(300f, 50f),
                                                       new Vector2(180f, -18f), "5 / 5", UIFontSet.Current.numeric,
                                                       42f, T.primary, TextAlignmentOptions.Right);

            // Free daily reward
            var dailyBonus = UIFactory.Button("DailyBonusBtn", card, new Vector2(760f, 120f),
                                              new Vector2(0f, 220f), T.gold, "CLAIM +250 FREE COINS", 32f,
                                              ClayGraphic.Shape.Pill, glow: 0.14f);
            modal.dailyBonusButton = dailyBonus;
            modal.dailyBonusLabel = dailyBonus.GetComponentInChildren<TextMeshProUGUI>();

            // Coin packs
            var p1 = UIFactory.Button("Pack1", card, new Vector2(760f, 108f), new Vector2(0f, 70f),
                                      T.panelHigh, null, shape: ClayGraphic.Shape.Panel);
            UIFactory.Label("P1Title", p1.transform, new Vector2(400f, 40f), new Vector2(-140f, 0f),
                            "POUCH: 1,000 COINS", UIFontSet.Current.body, 28f, T.ink, TextAlignmentOptions.Left);
            UIFactory.Label("P1Price", p1.transform, new Vector2(200f, 40f), new Vector2(220f, 0f),
                            "$0.99", UIFontSet.Current.numeric, 28f, T.primary, TextAlignmentOptions.Right);
            modal.pack1Button = p1;

            var p2 = UIFactory.Button("Pack2", card, new Vector2(760f, 108f), new Vector2(0f, -60f),
                                      T.panelHigh, null, shape: ClayGraphic.Shape.Panel);
            UIFactory.Label("P2Title", p2.transform, new Vector2(400f, 40f), new Vector2(-140f, 0f),
                            "SACK: 5,000 COINS", UIFontSet.Current.body, 28f, T.ink, TextAlignmentOptions.Left);
            UIFactory.Label("P2Price", p2.transform, new Vector2(200f, 40f), new Vector2(220f, 0f),
                            "$2.99", UIFontSet.Current.numeric, 28f, T.primary, TextAlignmentOptions.Right);
            modal.pack2Button = p2;

            var p3 = UIFactory.Button("Pack3", card, new Vector2(760f, 108f), new Vector2(0f, -190f),
                                      T.panelHigh, null, shape: ClayGraphic.Shape.Panel);
            UIFactory.Label("P3Title", p3.transform, new Vector2(400f, 40f), new Vector2(-140f, 0f),
                            "CHEST: 20,000 COINS", UIFontSet.Current.body, 28f, T.ink, TextAlignmentOptions.Left);
            UIFactory.Label("P3Price", p3.transform, new Vector2(200f, 40f), new Vector2(220f, 0f),
                            "$9.99", UIFontSet.Current.numeric, 28f, T.gold, TextAlignmentOptions.Right);
            modal.pack3Button = p3;

            var close = UIFactory.Button("CloseBtn", card, new Vector2(760f, 100f),
                                         new Vector2(0f, -360f), T.neutral, "CLOSE", 30f);
            modal.closeButton = close;

            SavePrefabRaw(root, ModalDir + "/WalletModal.prefab");
        }

        // Shop Item Detail Modal
        {
            GameObject root = NewModalRoot("ShopItemDetailModal");
            var modal = root.AddComponent<ShopItemDetailModal>();
            var card = ModalCard(root.transform, new Vector2(880f, 1160f), T, modal);

            modal.itemCategoryLabel = UIFactory.Label("Category", card, new Vector2(760f, 40f),
                                                      new Vector2(0f, 470f), "DICE SKIN • EPIC",
                                                      UIFontSet.Current.bodyLight, 24f, T.gold);
            modal.itemTitleLabel = UIFactory.Display("Title", card, new Vector2(760f, 80f),
                                                     new Vector2(0f, 400f), "OBSIDIAN DIE", T.ink);
            modal.itemTitleLabel.fontSize = 50f;

            // Preview box
            var preview = UIFactory.Clay("PreviewBox", card, new Vector2(300f, 300f),
                                         new Vector2(0f, 180f), T.panelHigh, ClayGraphic.Shape.Panel, T.radiusLg);
            preview.raycastTarget = false;
            var iconRt = UIFactory.Rect("IconImg", preview.transform, new Vector2(240f, 240f), Vector2.zero);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.color = Color.white;
            modal.itemIconImage = iconImg;

            modal.itemDescriptionLabel = UIFactory.Label("Desc", card, new Vector2(760f, 80f),
                                                         new Vector2(0f, -30f),
                                                         "Hand-crafted dark crystal with shimmering edges.",
                                                         UIFontSet.Current.bodyLight, 26f, T.inkMuted);

            modal.perkListLabel = UIFactory.Label("Perks", card, new Vector2(760f, 60f),
                                                  new Vector2(0f, -110f),
                                                  "• Custom roll sound\n• Gold trail particles",
                                                  UIFontSet.Current.body, 24f, T.ink);

            var actionBtn = UIFactory.Button("ActionBtn", card, new Vector2(760f, 140f),
                                             new Vector2(0f, -260f), T.primary, "BUY NOW — $1.99", 36f,
                                             ClayGraphic.Shape.Pill, glow: 0.14f);
            modal.buyOrEquipButton = actionBtn;
            modal.buyOrEquipLabel = actionBtn.GetComponentInChildren<TextMeshProUGUI>();

            var close = UIFactory.Button("CloseBtn", card, new Vector2(760f, 100f),
                                         new Vector2(0f, -420f), T.neutral, "CLOSE", 30f);
            modal.closeButton = close;

            SavePrefabRaw(root, ModalDir + "/ShopItemDetailModal.prefab");
        }

        Debug.Log("[LudoNX UI] modals written.");
    }

    // ── Match Lobby ──────────────────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/10. Build Match Lobby", priority = 10)]
    public static void BuildMatchLobby()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("MatchLobby", ScreenId.MatchLobby);
        var screen = root.AddComponent<MatchLobbyScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "MATCH LOBBY", out title);

        // Room code card
        var roomCard = UIFactory.Clay("RoomCard", p, new Vector2(880f, 130f), new Vector2(0f, 620f),
                                      T.panel, ClayGraphic.Shape.Panel, T.radiusLg);
        roomCard.raycastTarget = false;

        screen.roomCodeLabel = UIFactory.Display("Code", roomCard.transform, new Vector2(440f, 70f),
                                                 new Vector2(-150f, 0f), "ROOM #7842", T.ink);
        screen.roomCodeLabel.fontSize = 44f;
        screen.roomCodeLabel.alignment = TextAlignmentOptions.Left;

        screen.copyCodeButton = UIFactory.Button("CopyBtn", roomCard.transform, new Vector2(200f, 80f),
                                                 new Vector2(280f, 0f), T.secondary, "COPY", 28f);

        screen.modeRulesChip = UIFactory.Chip("ModeChip", p, new Vector2(400f, 66f),
                                              new Vector2(0f, 500f), "CLASSIC • 4 PLAYERS", T.panelHigh, T.ink);

        // Player slots
        screen.playerSlots = new List<MatchLobbyScreen.LobbySlot>();
        for (int i = 0; i < 4; i++)
        {
            float y = 350f - i * 160f;
            var slotCard = UIFactory.Clay("Slot_" + i, p, new Vector2(880f, 136f), new Vector2(0f, y),
                                          T.panel, ClayGraphic.Shape.Panel, T.radiusMd);
            slotCard.raycastTarget = false;

            var badge = UIFactory.Clay("HouseBadge", slotCard.transform, new Vector2(72f, 72f),
                                       new Vector2(-360f, 0f), T.playerRed, ClayGraphic.Shape.Circle);
            badge.raycastTarget = false;

            var nameLabel = UIFactory.Label("PlayerName", slotCard.transform, new Vector2(440f, 44f),
                                            new Vector2(-50f, 18f), i == 0 ? "Player (You)" : "WAITING FOR PLAYER...",
                                            UIFontSet.Current.body, 32f, T.ink, TextAlignmentOptions.Left);

            var statusLabel = UIFactory.Label("Status", slotCard.transform, new Vector2(440f, 36f),
                                              new Vector2(-50f, -22f), i == 0 ? "HOST / READY" : "OPEN SLOT",
                                              UIFontSet.Current.bodyLight, 24f, i == 0 ? T.success : T.inkDim, TextAlignmentOptions.Left);

            var crown = UIFactory.Clay("Crown", slotCard.transform, new Vector2(44f, 44f),
                                       new Vector2(360f, 0f), T.gold, ClayGraphic.Shape.Circle);
            crown.raycastTarget = false;
            crown.gameObject.SetActive(i == 0);

            var slot = new MatchLobbyScreen.LobbySlot
            {
                slotRoot = (RectTransform)slotCard.transform,
                houseBadge = badge,
                playerNameLabel = nameLabel,
                statusLabel = statusLabel,
                hostCrown = crown,
                isOccupied = (i == 0),
                isReady = (i == 0)
            };
            screen.playerSlots.Add(slot);
        }

        // Action buttons
        screen.startOrReadyButton = UIFactory.Button("StartBtn", p, new Vector2(840f, 180f),
                                                     new Vector2(0f, -440f), T.primary, "START MATCH", 48f,
                                                     ClayGraphic.Shape.Pill, glow: 0.16f, hero: true);
        screen.startOrReadyButton.Clay.glowSpread = 40f;
        screen.startOrReadyText = screen.startOrReadyButton.GetComponentInChildren<TextMeshProUGUI>();

        screen.inviteButton = UIFactory.Button("InviteBtn", p, new Vector2(840f, 120f),
                                               new Vector2(0f, -620f), T.neutral, "INVITE FRIENDS", 32f);

        SavePrefab(root, ScreenDir + "/MatchLobby.prefab");
    }

    // ── Pawn / Token Collection ──────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/11. Build Pawn Collection", priority = 11)]
    public static void BuildPawnCollection()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("PawnCollection", ScreenId.PawnCollection);
        var screen = root.AddComponent<PawnCollectionScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "PAWN COLLECTION", out title);

        // Collection progress card
        var progCard = UIFactory.Clay("ProgressCard", p, new Vector2(880f, 120f), new Vector2(0f, 620f),
                                      T.panel, ClayGraphic.Shape.Panel, T.radiusMd);
        progCard.raycastTarget = false;

        screen.collectionProgressLabel = UIFactory.Label("ProgressLabel", progCard.transform,
                                                         new Vector2(400f, 40f), new Vector2(-180f, 18f),
                                                         "4 / 6 UNLOCKED (66%)", UIFontSet.Current.body,
                                                         26f, T.ink, TextAlignmentOptions.Left);

        var bar = UIFactory.Clay("ProgressBar", progCard.transform, new Vector2(460f, 16f),
                                 new Vector2(-150f, -22f), T.recessed, ClayGraphic.Shape.Pill);
        bar.raycastTarget = false;
        bar.shadowAlpha = 0f;

        var fill = UIFactory.Clay("Fill", bar.transform, new Vector2(300f, 16f), Vector2.zero,
                                  T.success, ClayGraphic.Shape.Pill);
        fill.raycastTarget = false;
        fill.shadowAlpha = 0f;
        var fillRt = fill.rectTransform;
        fillRt.anchorMin = new Vector2(0f, 0.5f);
        fillRt.anchorMax = new Vector2(0f, 0.5f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;
        screen.collectionProgressBarFill = fillRt;

        // Equipped Preview Box
        var prevCard = UIFactory.Clay("PreviewCard", p, new Vector2(880f, 320f), new Vector2(0f, 360f),
                                      T.panelHigh, ClayGraphic.Shape.Panel, T.radiusLg);
        prevCard.raycastTarget = false;

        var pedestal = UIFactory.Clay("Pedestal", prevCard.transform, new Vector2(160f, 160f),
                                      new Vector2(-280f, 0f), T.panel, ClayGraphic.Shape.Circle);
        pedestal.raycastTarget = false;
        var imgRt = UIFactory.Rect("PreviewImg", pedestal.transform, new Vector2(120f, 120f), Vector2.zero);
        var iconImg = imgRt.gameObject.AddComponent<Image>();
        iconImg.color = Color.white;
        screen.equippedPreviewImage = iconImg;

        screen.equippedNameLabel = UIFactory.Display("EquippedName", prevCard.transform,
                                                     new Vector2(460f, 60f), new Vector2(120f, 70f),
                                                     "Classic Crimson", T.ink);
        screen.equippedNameLabel.fontSize = 42f;
        screen.equippedNameLabel.alignment = TextAlignmentOptions.Left;

        screen.equippedRarityLabel = UIFactory.Label("EquippedRarity", prevCard.transform,
                                                     new Vector2(460f, 36f), new Vector2(120f, 26f),
                                                     "COMMON STARTER", UIFontSet.Current.body,
                                                     22f, T.inkDim, TextAlignmentOptions.Left);

        screen.equippedPerkLabel = UIFactory.Label("EquippedPerk", prevCard.transform,
                                                   new Vector2(460f, 40f), new Vector2(120f, -14f),
                                                   "Unlocked via Starter Token", UIFontSet.Current.bodyLight,
                                                   22f, T.inkMuted, TextAlignmentOptions.Left);

        screen.equipOrInspectButton = UIFactory.Button("EquipBtn", prevCard.transform, new Vector2(260f, 74f),
                                                       new Vector2(120f, -80f), T.primary, "EQUIPPED", 26f,
                                                       ClayGraphic.Shape.Pill);
        screen.equipOrInspectLabel = screen.equipOrInspectButton.GetComponentInChildren<TextMeshProUGUI>();

        // Grid Container
        var grid = UIFactory.Rect("GridContainer", p, new Vector2(880f, 600f), new Vector2(0f, -160f));
        screen.gridContainer = grid;

        // Template Card
        var tCard = UIFactory.Button("TemplateCard", grid, new Vector2(420f, 160f), Vector2.zero,
                                     T.panel, null, shape: ClayGraphic.Shape.Panel);
        tCard.Clay.cornerRadius = T.radiusMd;

        var tIconRt = UIFactory.Rect("Icon", tCard.content, new Vector2(90f, 90f), new Vector2(-130f, 0f));
        var tImg = tIconRt.gameObject.AddComponent<Image>();
        tImg.color = Color.white;

        UIFactory.Label("Name", tCard.content, new Vector2(240f, 40f), new Vector2(70f, 30f),
                        "Pawn Name", UIFontSet.Current.body, 26f, T.ink, TextAlignmentOptions.Left);
        UIFactory.Label("Rarity", tCard.content, new Vector2(240f, 30f), new Vector2(70f, 0f),
                        "COMMON", UIFontSet.Current.bodyLight, 20f, T.inkDim, TextAlignmentOptions.Left);

        var statusPill = UIFactory.Clay("StatusPill", tCard.content, new Vector2(140f, 36f),
                                        new Vector2(70f, -34f), T.panelHigh, ClayGraphic.Shape.Pill);
        statusPill.raycastTarget = false;
        statusPill.shadowAlpha = 0f;
        UIFactory.Label("StatusLabel", statusPill.transform, new Vector2(140f, 36f), Vector2.zero,
                        "OWNED", UIFontSet.Current.bodyLight, 18f, T.inkMuted);

        tCard.gameObject.SetActive(false);
        screen.cardTemplate = (RectTransform)tCard.transform;

        screen.navBar = BuildNavBar(p, T, HomeNavBar.Tab.Collection);

        SavePrefab(root, ScreenDir + "/PawnCollection.prefab");
    }

    // ── Wallet / Currency Screen ─────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/13. Build Wallet", priority = 13)]
    public static void BuildWallet()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("Wallet", ScreenId.Wallet);
        var screen = root.AddComponent<WalletScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "WALLET & CURRENCY", out title);

        // Balances card
        var balCard = UIFactory.Clay("BalancesCard", p, new Vector2(880f, 240f), new Vector2(0f, 560f),
                                     T.panel, ClayGraphic.Shape.Panel, T.radiusLg);
        balCard.raycastTarget = false;

        // Coins
        UIFactory.Label("CoinsHeader", balCard.transform, new Vector2(380f, 30f), new Vector2(-190f, 66f),
                        "COINS", UIFontSet.Current.body, 22f, T.inkDim, TextAlignmentOptions.Left);
        screen.coinsBalanceLabel = UIFactory.Display("CoinsVal", balCard.transform, new Vector2(380f, 50f),
                                                     new Vector2(-190f, 24f), "1,250", T.gold);
        screen.coinsBalanceLabel.fontSize = 44f;
        screen.coinsBalanceLabel.alignment = TextAlignmentOptions.Left;

        // NX Points
        UIFactory.Label("NXHeader", balCard.transform, new Vector2(380f, 30f), new Vector2(190f, 66f),
                        "NX POINTS", UIFontSet.Current.body, 22f, T.inkDim, TextAlignmentOptions.Left);
        screen.nxPointsLabel = UIFactory.Display("NXVal", balCard.transform, new Vector2(380f, 50f),
                                                 new Vector2(190f, 24f), "350 NX", T.secondary);
        screen.nxPointsLabel.fontSize = 44f;
        screen.nxPointsLabel.alignment = TextAlignmentOptions.Left;

        // Event Tokens & Gold
        screen.eventTokensLabel = UIFactory.Label("EventTokensVal", balCard.transform, new Vector2(380f, 36f),
                                                  new Vector2(-190f, -44f), "450 EVENT TOKENS",
                                                  UIFontSet.Current.numeric, 24f, T.primary, TextAlignmentOptions.Left);
        screen.eventExpiryLabel = UIFactory.Label("EventExpiry", balCard.transform, new Vector2(380f, 30f),
                                                  new Vector2(-190f, -74f), "EXPIRES IN 4 DAYS",
                                                  UIFontSet.Current.bodyLight, 18f, T.inkMuted, TextAlignmentOptions.Left);

        screen.goldBalanceLabel = UIFactory.Label("GoldVal", balCard.transform, new Vector2(380f, 36f),
                                                  new Vector2(190f, -44f), "120 GEMS",
                                                  UIFontSet.Current.numeric, 24f, T.gold, TextAlignmentOptions.Left);

        // Level & NX progression
        var lvlCard = UIFactory.Clay("LevelCard", p, new Vector2(880f, 130f), new Vector2(0f, 340f),
                                     T.panelHigh, ClayGraphic.Shape.Panel, T.radiusMd);
        lvlCard.raycastTarget = false;

        screen.levelLabel = UIFactory.Label("LevelLabel", lvlCard.transform, new Vector2(300f, 40f),
                                            new Vector2(-250f, 18f), "LEVEL 1", UIFontSet.Current.body,
                                            26f, T.ink, TextAlignmentOptions.Left);

        var lvlBar = UIFactory.Clay("ProgressBar", lvlCard.transform, new Vector2(500f, 16f),
                                    new Vector2(-150f, -22f), T.recessed, ClayGraphic.Shape.Pill);
        lvlBar.raycastTarget = false;
        lvlBar.shadowAlpha = 0f;

        var lvlFill = UIFactory.Clay("Fill", lvlBar.transform, new Vector2(250f, 16f), Vector2.zero,
                                     T.secondary, ClayGraphic.Shape.Pill);
        lvlFill.raycastTarget = false;
        lvlFill.shadowAlpha = 0f;
        var lvlFillRt = lvlFill.rectTransform;
        lvlFillRt.anchorMin = new Vector2(0f, 0.5f);
        lvlFillRt.anchorMax = new Vector2(0f, 0.5f);
        lvlFillRt.pivot = new Vector2(0f, 0.5f);
        lvlFillRt.anchoredPosition = Vector2.zero;
        screen.levelProgressBarFill = lvlFillRt;

        // Free daily bonus
        screen.dailyBonusButton = UIFactory.Button("DailyBonusBtn", p, new Vector2(880f, 130f),
                                                   new Vector2(0f, 170f), T.gold, "CLAIM +250 FREE COINS", 34f,
                                                   ClayGraphic.Shape.Pill, glow: 0.14f);
        screen.dailyBonusLabel = screen.dailyBonusButton.GetComponentInChildren<TextMeshProUGUI>();

        // Purchase packs
        UIFactory.Label("PacksHeader", p, new Vector2(880f, 40f), new Vector2(0f, 50f),
                        "COIN PACKS", UIFontSet.Current.body, 26f, T.inkOnGroundDim, TextAlignmentOptions.Left).characterSpacing = 8f;

        screen.pack1Button = UIFactory.Button("Pack1", p, new Vector2(880f, 110f), new Vector2(0f, -40f),
                                              T.panel, null, shape: ClayGraphic.Shape.Panel);
        UIFactory.Label("Title", screen.pack1Button.content, new Vector2(500f, 40f), new Vector2(-150f, 0f),
                        "POUCH: 1,000 COINS", UIFontSet.Current.body, 30f, T.ink, TextAlignmentOptions.Left);
        UIFactory.Label("Price", screen.pack1Button.content, new Vector2(200f, 40f), new Vector2(280f, 0f),
                        "$0.99", UIFontSet.Current.numeric, 30f, T.primary, TextAlignmentOptions.Right);

        screen.pack2Button = UIFactory.Button("Pack2", p, new Vector2(880f, 110f), new Vector2(0f, -170f),
                                              T.panel, null, shape: ClayGraphic.Shape.Panel);
        UIFactory.Label("Title", screen.pack2Button.content, new Vector2(500f, 40f), new Vector2(-150f, 0f),
                        "SACK: 5,000 COINS", UIFontSet.Current.body, 30f, T.ink, TextAlignmentOptions.Left);
        UIFactory.Label("Price", screen.pack2Button.content, new Vector2(200f, 40f), new Vector2(280f, 0f),
                        "$2.99", UIFontSet.Current.numeric, 30f, T.primary, TextAlignmentOptions.Right);

        screen.pack3Button = UIFactory.Button("Pack3", p, new Vector2(880f, 110f), new Vector2(0f, -300f),
                                              T.panel, null, shape: ClayGraphic.Shape.Panel);
        UIFactory.Label("Title", screen.pack3Button.content, new Vector2(500f, 40f), new Vector2(-150f, 0f),
                        "CHEST: 20,000 COINS", UIFontSet.Current.body, 30f, T.ink, TextAlignmentOptions.Left);
        UIFactory.Label("Price", screen.pack3Button.content, new Vector2(200f, 40f), new Vector2(280f, 0f),
                        "$9.99", UIFontSet.Current.numeric, 30f, T.gold, TextAlignmentOptions.Right);

        // Event Shop Link
        screen.eventShopButton = UIFactory.Button("EventShopBtn", p, new Vector2(880f, 120f),
                                                  new Vector2(0f, -460f), T.secondary, "GO TO EVENT STORE", 32f);

        SavePrefab(root, ScreenDir + "/Wallet.prefab");
    }

    // ── Energy / Lives Screen ────────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/14. Build Energy Screen", priority = 14)]
    public static void BuildEnergy()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("Energy", ScreenId.Energy);
        var screen = root.AddComponent<EnergyScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "ENERGY & LIVES", out title);

        var energyCard = UIFactory.Clay("EnergyCard", p, new Vector2(880f, 320f), new Vector2(0f, 480f),
                                        T.panel, ClayGraphic.Shape.Panel, T.radiusLg);
        energyCard.raycastTarget = false;

        UIFactory.Label("EnergyHeader", energyCard.transform, new Vector2(760f, 40f), new Vector2(0f, 110f),
                        "LIVES REMAINING", UIFontSet.Current.body, 24f, T.inkDim);

        screen.energyCountLabel = UIFactory.Display("Count", energyCard.transform, new Vector2(760f, 90f),
                                                    new Vector2(0f, 46f), "5 / 5", T.ink);
        screen.energyCountLabel.fontSize = 64f;

        // Pips
        var pipsGroup = UIFactory.Rect("PipsGroup", energyCard.transform, new Vector2(500f, 40f), new Vector2(0f, -20f));
        screen.energyPips = new ClayGraphic[5];
        for (int i = 0; i < 5; i++)
        {
            float x = -160f + i * 80f;
            var pip = UIFactory.Clay("Pip_" + i, pipsGroup, new Vector2(60f, 26f),
                                     new Vector2(x, 0f), T.primary, ClayGraphic.Shape.Pill);
            pip.raycastTarget = false;
            pip.shadowAlpha = 0f;
            screen.energyPips[i] = pip;
        }
        screen.nextTimerLabel = UIFactory.Body("NextTimer", energyCard.transform, new Vector2(760f, 40f),
                                               new Vector2(0f, -74f), "FULL CAPACITY", T.success, 26f);

        screen.fullTimerLabel = UIFactory.Label("FullTimer", energyCard.transform, new Vector2(760f, 34f),
                                                new Vector2(0f, -112f), "All lives ready for match play!",
                                                UIFontSet.Current.bodyLight, 20f, T.inkMuted);

        // Actions
        screen.watchAdButton = UIFactory.Button("WatchAdBtn", p, new Vector2(880f, 150f), new Vector2(0f, 190f),
                                                T.gold, "WATCH AD (+1 LIFE)", 36f, ClayGraphic.Shape.Pill, glow: 0.14f);
        screen.watchAdButton.Clay.glowSpread = 36f;

        screen.refillCoinsButton = UIFactory.Button("RefillCoinsBtn", p, new Vector2(880f, 150f), new Vector2(0f, 10f),
                                                    T.success, "REFILL ALL (150 COINS)", 36f, ClayGraphic.Shape.Pill, glow: 0.14f);
        screen.refillCoinsButton.Clay.glowSpread = 36f;

        screen.shopPassButton = UIFactory.Button("ShopPassBtn", p, new Vector2(880f, 130f), new Vector2(0f, -160f),
                                                 T.secondary, "GET UNLIMITED PASS IN SHOP", 32f);

        SavePrefab(root, ScreenDir + "/Energy.prefab");
    }

    // ── Missions Screen ──────────────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/15. Build Missions Screen", priority = 15)]
    public static void BuildMissions()
    {
        CreateSupportAssets();
        var T = UITheme.Current;
        var icons = UIIconSet.Current;

        GameObject root = NewScreenRoot("Missions", ScreenId.Missions);
        var screen = root.AddComponent<MissionsScreen>();
        Transform p = root.transform;

        UIFactory.Page(p, icons != null ? icons.bgTournamentHall : null);

        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "MISSIONS & QUESTS", out title);

        screen.resetCountdownLabel = UIFactory.Label("ResetLabel", p, new Vector2(880f, 40f), new Vector2(0f, 650f),
                                                     "RESETS IN 18H 30M", UIFontSet.Current.body, 24f, T.inkOnGroundDim);

        // Tabs
        var tabRow = UIFactory.Rect("TabRow", p, new Vector2(880f, 80f), new Vector2(0f, 570f));
        screen.dailyTab = UIFactory.Button("DailyTab", tabRow, new Vector2(275f, 74f), new Vector2(-295f, 0f),
                                           T.secondary, "DAILY", 26f);
        screen.weeklyTab = UIFactory.Button("WeeklyTab", tabRow, new Vector2(275f, 74f), new Vector2(0f, 0f),
                                            T.neutral, "WEEKLY", 26f);
        screen.seasonTab = UIFactory.Button("SeasonTab", tabRow, new Vector2(275f, 74f), new Vector2(295f, 0f),
                                            T.neutral, "SEASON", 26f);

        // List container
        var list = UIFactory.Rect("ListContainer", p, new Vector2(880f, 820f), new Vector2(0f, 80f));
        screen.listContainer = list;

        // Template row
        var tRow = UIFactory.Clay("TemplateRow", list, new Vector2(880f, 150f), Vector2.zero,
                                  T.panel, ClayGraphic.Shape.Panel, T.radiusMd);
        tRow.raycastTarget = false;

        UIFactory.Label("Title", tRow.transform, new Vector2(500f, 40f), new Vector2(-150f, 36f),
                        "Mission Title", UIFontSet.Current.body, 30f, T.ink, TextAlignmentOptions.Left);
        UIFactory.Label("Desc", tRow.transform, new Vector2(500f, 34f), new Vector2(-150f, 0f),
                        "Mission Description", UIFontSet.Current.bodyLight, 24f, T.inkMuted, TextAlignmentOptions.Left);
        UIFactory.Label("Count", tRow.transform, new Vector2(180f, 34f), new Vector2(290f, 36f),
                        "0 / 1", UIFontSet.Current.numeric, 26f, T.inkDim, TextAlignmentOptions.Right);

        var bar = UIFactory.Clay("Bar", tRow.transform, new Vector2(500f, 16f), new Vector2(-150f, -40f),
                                 T.recessed, ClayGraphic.Shape.Pill);
        bar.raycastTarget = false;
        bar.shadowAlpha = 0f;

        var fill = UIFactory.Clay("Fill", bar.transform, new Vector2(250f, 16f), Vector2.zero,
                                  T.success, ClayGraphic.Shape.Pill);
        fill.raycastTarget = false;
        fill.shadowAlpha = 0f;
        var fillRt = fill.rectTransform;
        fillRt.anchorMin = new Vector2(0f, 0.5f);
        fillRt.anchorMax = new Vector2(0f, 0.5f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;

        var claim = UIFactory.Button("ClaimButton", tRow.transform, new Vector2(200f, 74f),
                                     new Vector2(290f, -16f), T.success, "CLAIM", 26f, ClayGraphic.Shape.Pill);

        UIFactory.Label("Reward", tRow.transform, new Vector2(500f, 30f), new Vector2(-150f, -66f),
                        "+100 COINS • +20 NX", UIFontSet.Current.numeric, 20f, T.gold, TextAlignmentOptions.Left);

        tRow.gameObject.SetActive(false);
        screen.rowTemplate = (RectTransform)tRow.transform;

        // Claim All button
        screen.claimAllButton = UIFactory.Button("ClaimAll", p, new Vector2(880f, 140f),
                                                 new Vector2(0f, -460f), T.success, "CLAIM ALL COMPLETED", 36f,
                                                 ClayGraphic.Shape.Pill, glow: 0.14f);

        screen.navBar = BuildNavBar(p, T, HomeNavBar.Tab.Missions);

        SavePrefab(root, ScreenDir + "/Missions.prefab");
    }

    // ── Events Screen ────────────────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/16. Build Events Screen", priority = 16)]
    public static void BuildEvents()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("Events", ScreenId.Events);
        var screen = root.AddComponent<EventsScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "LIVE EVENTS", out title);

        // Status Tabs
        var tabRow = UIFactory.Rect("TabRow", p, new Vector2(880f, 80f), new Vector2(0f, 620f));
        screen.activeTab = UIFactory.Button("ActiveTab", tabRow, new Vector2(275f, 74f), new Vector2(-295f, 0f),
                                            T.secondary, "ACTIVE", 26f);
        screen.upcomingTab = UIFactory.Button("UpcomingTab", tabRow, new Vector2(275f, 74f), new Vector2(0f, 0f),
                                              T.neutral, "UPCOMING", 26f);
        screen.completedTab = UIFactory.Button("CompletedTab", tabRow, new Vector2(275f, 74f), new Vector2(295f, 0f),
                                               T.neutral, "PAST", 26f);

        // Container
        var container = UIFactory.Rect("EventsContainer", p, new Vector2(880f, 1100f), new Vector2(0f, -40f));
        screen.eventsContainer = container;

        // Card template
        var card = UIFactory.Clay("EventCardTemplate", container, new Vector2(880f, 260f), Vector2.zero,
                                  T.panel, ClayGraphic.Shape.Panel, T.radiusLg);
        card.raycastTarget = false;

        UIFactory.Label("Title", card.transform, new Vector2(540f, 44f), new Vector2(-130f, 74f),
                        "EVENT TITLE", UIFontSet.Current.body, 32f, T.ink, TextAlignmentOptions.Left);

        UIFactory.Label("Subtitle", card.transform, new Vector2(540f, 36f), new Vector2(-130f, 32f),
                        "Event Subtitle", UIFontSet.Current.bodyLight, 24f, T.inkMuted, TextAlignmentOptions.Left);

        var timerPill = UIFactory.Clay("TimerPill", card.transform, new Vector2(220f, 44f),
                                       new Vector2(270f, 74f), T.panelHigh, ClayGraphic.Shape.Pill);
        timerPill.raycastTarget = false;
        timerPill.shadowAlpha = 0f;
        UIFactory.Label("TimerLabel", timerPill.transform, new Vector2(220f, 44f), Vector2.zero,
                        "ENDS IN 3D", UIFontSet.Current.numeric, 20f, T.primary);

        var rulesChip = UIFactory.Clay("RulesChip", card.transform, new Vector2(540f, 40f),
                                       new Vector2(-130f, -20f), T.recessed, ClayGraphic.Shape.Pill);
        rulesChip.raycastTarget = false;
        rulesChip.shadowAlpha = 0f;
        UIFactory.Label("RulesLabel", rulesChip.transform, new Vector2(520f, 36f), Vector2.zero,
                        "2-Player • No Safe Zones", UIFontSet.Current.bodyLight, 20f, T.inkMuted, TextAlignmentOptions.Left);

        UIFactory.Label("RewardLabel", card.transform, new Vector2(540f, 36f), new Vector2(-130f, -74f),
                        "WIN: +500 EVENT TOKENS", UIFontSet.Current.numeric, 22f, T.gold, TextAlignmentOptions.Left);

        var actBtn = UIFactory.Button("ActionBtn", card.transform, new Vector2(220f, 88f),
                                      new Vector2(270f, -40f), T.primary, "VIEW EVENT", 26f, ClayGraphic.Shape.Pill);

        card.gameObject.SetActive(false);
        screen.eventCardTemplate = (RectTransform)card.transform;

        screen.navBar = BuildNavBar(p, T, HomeNavBar.Tab.Events);

        SavePrefab(root, ScreenDir + "/Events.prefab");
    }

    // ── Event Detail Screen ──────────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/17. Build Event Detail Screen", priority = 17)]
    public static void BuildEventDetail()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("EventDetail", ScreenId.EventDetail);
        var screen = root.AddComponent<EventDetailScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "EVENT DETAILS", out title);

        // Header Card
        var headerCard = UIFactory.Clay("HeaderCard", p, new Vector2(880f, 260f), new Vector2(0f, 540f),
                                        T.panelHigh, ClayGraphic.Shape.Panel, T.radiusLg);
        headerCard.raycastTarget = false;

        screen.eventTitleLabel = UIFactory.Display("Title", headerCard.transform, new Vector2(800f, 60f),
                                                   new Vector2(0f, 70f), "GOLD RUSH CHAMPIONSHIP", T.ink);
        screen.eventTitleLabel.fontSize = 44f;

        screen.eventDatesLabel = UIFactory.Label("Dates", headerCard.transform, new Vector2(380f, 36f),
                                                 new Vector2(-200f, 16f), "AUG 25 - SEP 05",
                                                 UIFontSet.Current.bodyLight, 22f, T.inkDim, TextAlignmentOptions.Left);

        screen.eventCountdownLabel = UIFactory.Label("Countdown", headerCard.transform, new Vector2(380f, 36f),
                                                     new Vector2(200f, 16f), "ENDS IN 3D 14H",
                                                     UIFontSet.Current.numeric, 22f, T.primary, TextAlignmentOptions.Right);

        screen.eventDescriptionLabel = UIFactory.Label("Desc", headerCard.transform, new Vector2(800f, 60f),
                                                       new Vector2(0f, -44f),
                                                       "Compete in high-speed matches with bonus coin multipliers!",
                                                       UIFontSet.Current.bodyLight, 24f, T.inkMuted);

        // Rules & Rewards card
        var rulesCard = UIFactory.Clay("RulesCard", p, new Vector2(880f, 240f), new Vector2(0f, 240f),
                                       T.panel, ClayGraphic.Shape.Panel, T.radiusMd);
        rulesCard.raycastTarget = false;

        UIFactory.Label("RulesTitle", rulesCard.transform, new Vector2(800f, 36f), new Vector2(0f, 74f),
                        "MATCH RULES & REWARDS", UIFontSet.Current.body, 24f, T.inkDim);

        screen.rulesSummaryLabel = UIFactory.Label("RulesSummary", rulesCard.transform, new Vector2(800f, 40f),
                                                   new Vector2(0f, 24f), "2-Player • No Safe Zones • 2x Multiplier",
                                                   UIFontSet.Current.body, 28f, T.ink);

        screen.entryFeeLabel = UIFactory.Label("Fee", rulesCard.transform, new Vector2(800f, 34f),
                                               new Vector2(0f, -24f), "ENTRY FEE: 100 COINS",
                                               UIFontSet.Current.numeric, 22f, T.gold);

        screen.rewardsSummaryLabel = UIFactory.Label("Reward", rulesCard.transform, new Vector2(800f, 34f),
                                                     new Vector2(0f, -66f), "1ST PLACE: +500 EVENT TOKENS + GOLD MEDAL",
                                                     UIFontSet.Current.numeric, 22f, T.primary);

        // Leaderboard preview card
        var lbCard = UIFactory.Clay("LeaderboardCard", p, new Vector2(880f, 180f), new Vector2(0f, -20f),
                                    T.panel, ClayGraphic.Shape.Panel, T.radiusMd);
        lbCard.raycastTarget = false;

        UIFactory.Label("LBTitle", lbCard.transform, new Vector2(760f, 36f), new Vector2(0f, 44f),
                        "YOUR TOURNAMENT RANK", UIFontSet.Current.body, 24f, T.inkDim);

        screen.playerRankLabel = UIFactory.Display("Rank", lbCard.transform, new Vector2(760f, 50f),
                                                   new Vector2(0f, -6f), "RANK #42 (TOP 5%)", T.secondary);
        screen.playerRankLabel.fontSize = 38f;

        screen.playerPointsLabel = UIFactory.Label("Points", lbCard.transform, new Vector2(760f, 30f),
                                                   new Vector2(0f, -48f), "1,850 PTS",
                                                   UIFontSet.Current.numeric, 22f, T.gold);

        // Action
        screen.enterMatchButton = UIFactory.Button("EnterBtn", p, new Vector2(840f, 180f),
                                                   new Vector2(0f, -380f), T.primary, "ENTER EVENT MATCH", 48f,
                                                   ClayGraphic.Shape.Pill, glow: 0.16f, hero: true);
        screen.enterMatchButton.Clay.glowSpread = 40f;
        screen.enterMatchText = screen.enterMatchButton.GetComponentInChildren<TextMeshProUGUI>();

        SavePrefab(root, ScreenDir + "/EventDetail.prefab");
    }
    // ── Shop Item Detail Screen ──────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/18. Build Shop Item Detail Screen", priority = 18)]
    public static void BuildShopItemDetail()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("ShopItemDetail", ScreenId.ShopItemDetail);
        var screen = root.AddComponent<ShopItemDetailScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);

        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "ITEM DETAILS", out title);

        screen.categoryLabel = UIFactory.Label("Category", p, new Vector2(880f, 40f), new Vector2(0f, 640f),
                                               "DICE SKIN", UIFontSet.Current.body, 24f, T.gold);

        screen.titleLabel = UIFactory.Display("Title", p, new Vector2(880f, 80f), new Vector2(0f, 570f),
                                              "OBSIDIAN DRAGON DIE", T.inkOnGround);
        screen.titleLabel.fontSize = 54f;

        var prevCard = UIFactory.Clay("PreviewCard", p, new Vector2(600f, 400f), new Vector2(0f, 270f),
                                      T.panelHigh, ClayGraphic.Shape.Panel, T.radiusLg);
        prevCard.raycastTarget = false;

        var imgRt = UIFactory.Rect("PreviewImg", prevCard.transform, new Vector2(240f, 240f), Vector2.zero);
        var img = imgRt.gameObject.AddComponent<Image>();
        img.color = Color.white;
        screen.previewImage = img;

        screen.rarityLabel = UIFactory.Label("Rarity", prevCard.transform, new Vector2(500f, 40f),
                                             new Vector2(0f, -140f), "LEGENDARY ITEM",
                                             UIFontSet.Current.body, 26f, T.gold);

        var infoCard = UIFactory.Clay("InfoCard", p, new Vector2(880f, 280f), new Vector2(0f, -140f),
                                      T.panel, ClayGraphic.Shape.Panel, T.radiusMd);
        infoCard.raycastTarget = false;

        screen.descriptionLabel = UIFactory.Label("Desc", infoCard.transform, new Vector2(780f, 90f),
                                                  new Vector2(0f, 60f),
                                                  "Forged in volcanic depths. Features luminous gold engravings and custom roll physics.",
                                                  UIFontSet.Current.bodyLight, 26f, T.inkMuted);

        screen.perksLabel = UIFactory.Label("Perks", infoCard.transform, new Vector2(780f, 80f),
                                            new Vector2(0f, -50f),
                                            "• Custom Roll Sound FX\n• Gold Sparkle Particle Trail",
                                            UIFontSet.Current.body, 24f, T.ink);

        screen.actionButton = UIFactory.Button("ActionBtn", p, new Vector2(840f, 170f),
                                               new Vector2(0f, -440f), T.primary, "PURCHASE — $1.99", 44f,
                                               ClayGraphic.Shape.Pill, glow: 0.16f, hero: true);
        screen.actionButton.Clay.glowSpread = 36f;
        screen.actionButtonText = screen.actionButton.GetComponentInChildren<TextMeshProUGUI>();

        SavePrefab(root, ScreenDir + "/ShopItemDetail.prefab");
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

    // ── Player Statistics Screen ──────────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/19. Build Player Stats Screen", priority = 19)]
    public static void BuildPlayerStats()
    {
        CreateSupportAssets();
        var T = UITheme.Current;

        GameObject root = NewScreenRoot("PlayerStats", ScreenId.PlayerStats);
        var screen = root.AddComponent<PlayerStatsScreen>();
        Transform p = root.transform;

        UIFactory.Page(p);
        TextMeshProUGUI title;
        screen.backButton = UIFactory.TopBar(p, "STATISTICS", out title);

        // ── Overall header card ─────────────────────────────────────────────────────
        var headerCard = UIFactory.Clay("OverallCard", p, new Vector2(880f, 400f), new Vector2(0f, 440f),
                                        T.panel, ClayGraphic.Shape.Panel, T.radiusMd);
        headerCard.raycastTarget = false;

        UIFactory.Label("OverallTitle", headerCard.transform, new Vector2(760f, 44f),
                        new Vector2(0f, 140f), "OVERALL CAREER",
                        UIFontSet.Current.body, 24f, T.inkMuted);

        // 2×4 overall tiles
        screen.totalGamesValue   = UIFactory.StatTile("GamesTile",  headerCard.transform, new Vector2(340f, 150f), new Vector2(-275f,  30f), "0",  "GAMES",    T.secondary);
        screen.totalWinsValue    = UIFactory.StatTile("WinsTile",   headerCard.transform, new Vector2(340f, 150f), new Vector2( -91f,  30f), "0",  "WINS",     T.gold);
        screen.winPctValue       = UIFactory.StatTile("WinPctTile", headerCard.transform, new Vector2(340f, 150f), new Vector2(  91f,  30f), "0%", "WIN RATE", T.success);
        screen.totalLossesValue  = UIFactory.StatTile("LossesTile", headerCard.transform, new Vector2(340f, 150f), new Vector2( 275f,  30f), "0",  "LOSSES",   T.recessed);
        screen.totalPointsValue  = UIFactory.StatTile("PtsTile",    headerCard.transform, new Vector2(340f, 150f), new Vector2(-275f, -130f), "0", "TOTAL PTS", T.gold);
        screen.avgPointsValue    = UIFactory.StatTile("AvgPtsTile", headerCard.transform, new Vector2(340f, 150f), new Vector2( -91f, -130f), "0", "AVG PTS",   T.secondary);
        screen.totalCapturesValue= UIFactory.StatTile("CapTile",    headerCard.transform, new Vector2(340f, 150f), new Vector2(  91f, -130f), "0", "CAPTURES",  T.primary);
        screen.timesCapturedValue= UIFactory.StatTile("CapdTile",   headerCard.transform, new Vector2(340f, 150f), new Vector2( 275f, -130f), "0", "CAPTURED",  T.inkDim);

        // Tokens + avg duration row below header
        screen.tokensCompletedValue = UIFactory.StatTile("TknTile", p, new Vector2(420f, 140f),
                                                          new Vector2(-220f, 100f), "0",  "TOKENS HOME",  T.success);
        screen.avgDurationValue     = UIFactory.StatTile("DurTile", p, new Vector2(420f, 140f),
                                                          new Vector2( 220f, 100f), "—", "AVG DURATION", T.inkMuted);

        // ── Mode tab strip ──────────────────────────────────────────────────────────
        float tabW = 140f, tabH = 72f, tabY = -90f;
        float[] tabX   = { -490f, -350f, -210f, -70f, 70f, 210f };
        string[] tabNames = { "ALL", "2P", "4P", "CAS", "RNK", "PVT" };
        ClayButton[] tabs = new ClayButton[6];
        for (int i = 0; i < 6; i++)
        {
            tabs[i] = UIFactory.Button("Tab" + tabNames[i], p, new Vector2(tabW, tabH),
                                        new Vector2(tabX[i], tabY),
                                        i == 0 ? T.secondary : T.neutral,
                                        tabNames[i], 22f);
        }
        screen.tabAll        = tabs[0];
        screen.tabTwoPlayer  = tabs[1];
        screen.tabFourPlayer = tabs[2];
        screen.tabCasual     = tabs[3];
        screen.tabRanked     = tabs[4];
        screen.tabPrivate    = tabs[5];

        // Active-tab indicator line
        var indicator = UIFactory.Clay("TabIndicator", p, new Vector2(tabW - 20f, 6f),
                                        new Vector2(tabX[0], tabY - tabH * 0.5f - 6f),
                                        T.secondary, ClayGraphic.Shape.Pill);
        indicator.raycastTarget = false;
        indicator.shadowAlpha = 0f;
        screen.tabIndicator = indicator.rectTransform;

        // ── Per-mode stat panels ────────────────────────────────────────────────────
        string[] modeLabels = { "ALL MODES", "2-PLAYER", "4-PLAYER", "CASUAL", "RANKED", "PRIVATE" };
        GameObject[] panels = new GameObject[6];
        TextMeshProUGUI[][] modeSets = new TextMeshProUGUI[6][];

        for (int m = 0; m < 6; m++)
        {
            var panel = UIFactory.Rect("Panel" + tabNames[m], p,
                                       new Vector2(880f, 600f), new Vector2(0f, -420f));
            panels[m] = panel.gameObject;

            var pCard = UIFactory.Clay("ModeCard", panel, new Vector2(880f, 580f), Vector2.zero,
                                        T.panel, ClayGraphic.Shape.Panel, T.radiusMd);
            pCard.raycastTarget = false;

            UIFactory.Label("ModeTitle", pCard.transform, new Vector2(760f, 40f), new Vector2(0f, 220f),
                            modeLabels[m], UIFontSet.Current.body, 26f, T.inkMuted);

            // 2×3 tiles: Games, Wins, Losses | WinPct, Points, AvgPts
            string[] tileLabels = { "GAMES", "WINS", "LOSSES", "WIN RATE", "POINTS", "AVG PTS" };
            Color[]  tileColors = { T.secondary, T.gold, T.inkDim, T.success, T.gold, T.secondary };
            float[]  xs = { -270f, 0f, 270f, -270f, 0f, 270f };
            float[]  ys = {  100f, 100f, 100f, -100f, -100f, -100f };

            modeSets[m] = new TextMeshProUGUI[6];
            for (int t = 0; t < 6; t++)
                modeSets[m][t] = UIFactory.StatTile("Stat" + t, pCard.transform,
                                                    new Vector2(240f, 160f),
                                                    new Vector2(xs[t], ys[t]),
                                                    "0", tileLabels[t], tileColors[t]);

            if (m > 0) panels[m].SetActive(false);
        }

        screen.panelAll        = panels[0];
        screen.panelTwoPlayer  = panels[1];
        screen.panelFourPlayer = panels[2];
        screen.panelCasual     = panels[3];
        screen.panelRanked     = panels[4];
        screen.panelPrivate    = panels[5];

        screen.allStats        = modeSets[0];
        screen.twoPlayerStats  = modeSets[1];
        screen.fourPlayerStats = modeSets[2];
        screen.casualStats     = modeSets[3];
        screen.rankedStats     = modeSets[4];
        screen.privateStats    = modeSets[5];

        SavePrefab(root, ScreenDir + "/PlayerStats.prefab");
    }

    // ── Build everything ──────────────────────────────────────────────────────────────────────

    [MenuItem("Ludo NX/UI/Build ALL Screens (Legacy)", priority = 20)]
    public static void BuildAll()
    {
        LudoNX.Editor.LudoNXMasterRebuilder.RebuildAll();
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



