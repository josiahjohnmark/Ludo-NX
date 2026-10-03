using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Builds the burger-menu popup into Resources/UI/Modals/Menu.prefab: a riveted panel under a
/// "MENU" banner, three labelled groups — PLAYER, ACCOUNTS, SUPPORT — of 4/3/4 icon slots, no
/// scrolling. Built pixel-proportionate to the owner's own Figma export,
/// `menu ui refrence and assets.zip` (27 Sep) — every plate, pill, badge and icon is real art
/// from that export (`Assets/Art/UI/Menu/`), replacing the previous version's Material-Symbol
/// glyphs on plain tan chips.
///
/// The reference's own flattened label PNGs (SETTINGS.png, ACCOUNTS.png, ...) are deliberately
/// NOT used — bible DoD §11 forbids baked text, and the 26 Sep decision keeps all UI text live so
/// languages can be added later. Every caption below is real TMP text instead.
///
/// Five of the reference's eleven labels don't have a matching Ludo NX feature yet (Ranking,
/// Showroom, Daily Rewards, Achievements, Inbox — the owner's reference content, not existing
/// screens). Each is pointed at the closest real destination instead of inventing one; Inbox has
/// no real analogue at all, so it shows a "coming soon" toast. Full reasoning for every choice is
/// in AI_HANDOFF's 27 Sep NOW entry and in MenuModal.cs's own header comment — flagged there as a
/// judgment call for the owner to correct, not a silent decision.
/// </summary>
public static class LudoNXMenuModalBuilder
{
    private const string Cards = "Assets/Art/UI/MatchSetup/Cards";
    private const string CircleSprite = "Assets/Resources/ui_circle.png";
    private const string MenuPrefab = "Assets/Resources/UI/Modals/Menu.prefab";

    private static readonly Color32 InkBlack = new Color32(30, 22, 12, 255);
    private static readonly Color32 ChipTan = new Color32(192, 150, 102, 255);   // action.neutral.face #C09666
    private static readonly Color32 GlyphCream = new Color32(255, 243, 225, 255); // #FFF3E1

    private const float PerceivedDim = 0.55f;
    private static readonly float ScrimAlpha = 1f - Mathf.Pow(1f - PerceivedDim, 2.2f);

    // ======================================================================================
    // Menu popup — built from the owner's own Figma export (menu ui refrence and assets.zip,
    // 27 Sep), not the flat chip grid this used to be. Every plate, pill and icon below is real
    // art from that export (Assets/Art/UI/Menu/); nothing here is a Material Symbol glyph.
    // ======================================================================================

    private const string MenuArt = "Assets/Art/UI/Menu";
    private const float MenuCardW = 360f;
    private const float MenuCardH = 386f; // 576 * (360 / 537) = 386.15 pt

    private static readonly System.Collections.Generic.Dictionary<string, Vector4> MenuBorders =
        new System.Collections.Generic.Dictionary<string, Vector4>
        {
            { "panel_menu.png", Vector4.zero },
            { "pill_section_header.png", new Vector4(30, 8, 20, 8) },
            { "slot_button.png", new Vector4(16, 16, 16, 16) },
        };

    [MenuItem("Ludo NX/UI/Build Menu Popup", priority = 9)]
    public static void Build()
    {
        ConfigureSprites(MenuArt, MenuBorders);

        var root = Make("MenuModal", null, typeof(CanvasGroup), typeof(MenuModal));
        var modal = root.GetComponent<MenuModal>();
        Stretch(root);

        var band = Band(root, "Frame", DesignBand.Edge.Fill, 0f);
        band.GetComponent<DesignBand>().referenceSize = Reference;

        var content = Make("Content", band);
        Stretch(content);

        var scrim = Make("Scrim", content, typeof(Image), typeof(Button));
        Stretch(scrim);
        scrim.offsetMin = new Vector2(-3000f, -3000f);
        scrim.offsetMax = new Vector2(3000f, 3000f);
        scrim.GetComponent<Image>().color = new Color(0f, 0f, 0f, ScrimAlpha);
        scrim.GetComponent<Button>().transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(scrim.GetComponent<Button>().onClick, modal.OnScrimTapped);

        // Fixed card dimensions matching the exact 537x576 Figma export (360x386 pt).
        // Uses Image.Type.Simple so the parchment texture and all four embossed corner dice
        // are preserved at 100% vector-faithful fidelity without any 9-slice distortion.
        var card = Make("Card", content, typeof(Image));
        Centre(card, 0f, (CloseSize + CloseGap) / 2f, MenuCardW, MenuCardH);
        var cardImg = card.GetComponent<Image>();
        cardImg.sprite = Sprite($"{MenuArt}/panel_menu.png");
        cardImg.type = Image.Type.Simple;
        cardImg.preserveAspect = true;
        cardImg.raycastTarget = true;
        modal.card = card;

        // "MENU" banner straddling the top edge — real art from the export (319x123 px).
        var banner = Picture(card, "Banner", $"{MenuArt}/banner_menu_title.png");
        banner.preserveAspect = true;
        var bannerRt = (RectTransform)banner.transform;
        bannerRt.anchorMin = bannerRt.anchorMax = new Vector2(0.5f, 1f);
        bannerRt.pivot = new Vector2(0.5f, 0.5f);
        bannerRt.sizeDelta = new Vector2(214f, 82.5f);
        bannerRt.anchoredPosition = new Vector2(0f, -13f);

        // Section 1: PLAYER (4 items: Ranking, Showroom, Daily Rewards, Achievements)
        MenuRibbon(card, "PlayerRibbon", "PLAYER", "badge_player.png", 52f);
        modal.rankingButton = MenuSlot(card, "SlotRanking", -118.5f, 90f, "icon_ranking.png", "RANKING");
        modal.showroomButton = MenuSlot(card, "SlotShowroom", -39.5f, 90f, "icon_showroom.png", "SHOWROOM");
        modal.dailyRewardsButton = MenuSlot(card, "SlotDailyRewards", 39.5f, 90f, "icon_daily_rewards.png", "DAILY\nREWARDS");
        modal.achievementsButton = MenuSlot(card, "SlotAchievements", 118.5f, 90f, "icon_achievements.png", "ARCHIEVMENTS");

        // Divider 1
        Divider(card, "DividerPlayerAccounts", 144f);

        // Section 2: ACCOUNTS (3 items: Settings, Inbox, Account — centered across card width)
        MenuRibbon(card, "AccountsRibbon", "ACCOUNTS", "badge_accounts.png", 153f);
        modal.settingsButton = MenuSlot(card, "SlotSettings", -88f, -9f, "icon_settings.png", "SETTINGS");
        modal.inboxButton = MenuSlot(card, "SlotInbox", 0f, -9f, "icon_inbox.png", "INBOX");
        modal.accountButton = MenuSlot(card, "SlotAccount", 88f, -9f, "icon_account.png", "ACCOUNT");

        // Divider 2
        Divider(card, "DividerAccountsSupport", 240f);

        // Section 3: SUPPORT (4 items: Help & Support, Privacy Policy, Terms of Service, About Ludo NX)
        MenuRibbon(card, "SupportRibbon", "SUPPORT", "badge_support.png", 249f);
        modal.helpSupportButton = MenuSlot(card, "SlotHelpSupport", -118.5f, -106f, "icon_help_support.png", "HELP &\nSUPPORT");
        modal.privacyButton = MenuSlot(card, "SlotPrivacy", -39.5f, -106f, "icon_privacy.png", "PRIVACY\nPOLICY");
        modal.termsButton = MenuSlot(card, "SlotTerms", 39.5f, -106f, "icon_terms.png", "TERMS OF\nSERVICE");
        modal.aboutButton = MenuSlot(card, "SlotAbout", 118.5f, -106f, "icon_about.png", "ABOUT\nLUDO NX");

        CloseButton(card, modal);

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MenuPrefab));
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, MenuPrefab);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Menu popup built -> {MenuPrefab}.");
    }

    private static void MenuRibbon(RectTransform card, string name, string heading, string badgeFile, float yFromTop)
    {
        const float ribbonW = 92f;
        const float ribbonH = 18f;
        var ribbon = Make(name, card, typeof(Image));
        // Left margin is 28 pt from card left (-180f + 28f + ribbonW / 2f = -106f)
        float cx = -106f;
        float cy = MenuCardH / 2f - yFromTop - ribbonH / 2f;
        Centre(ribbon, cx, cy, ribbonW, ribbonH);
        Sliced(ribbon.GetComponent<Image>(), $"{MenuArt}/pill_section_header.png", 2f);

        var badge = Picture(ribbon, "Badge", $"{MenuArt}/{badgeFile}");
        badge.preserveAspect = true;
        Centre((RectTransform)badge.transform, -ribbonW / 2f + 11f, 0f, 13f, 13f);

        var label = Text(ribbon, "Label", heading, 9.5f, InkBlack, TextAlignmentOptions.MidlineLeft);
        if (Lilita != null) label.font = Lilita;
        label.fontStyle = FontStyles.Bold;
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        label.rectTransform.pivot = new Vector2(0f, 0.5f);
        label.rectTransform.anchoredPosition = new Vector2(21f, 0f);
        label.rectTransform.sizeDelta = new Vector2(ribbonW - 24f, ribbonH);
    }

    private static Button MenuSlot(RectTransform card, string name, float cx, float cy, string iconFile, string caption)
    {
        const float slotSize = 58f;
        var slot = Make(name, card, typeof(Image), typeof(Button));
        Centre(slot, cx, cy, slotSize, slotSize);
        Sliced(slot.GetComponent<Image>(), $"{MenuArt}/slot_button.png", 2f);
        slot.GetComponent<Button>().targetGraphic = slot.GetComponent<Image>();

        var icon = Picture(slot, "Icon", $"{MenuArt}/{iconFile}");
        icon.preserveAspect = true;
        Centre((RectTransform)icon.transform, 0f, 8f, 32f, 32f);

        var cap = Text(slot, "Label", caption, 7.5f, InkBlack, TextAlignmentOptions.Center);
        if (Lilita != null) cap.font = Lilita;
        cap.fontStyle = FontStyles.Bold;
        Centre(cap.rectTransform, 0f, -18f, 54f, 18f);
        cap.textWrappingMode = TextWrappingModes.Normal;
        cap.lineSpacing = -25f;
        cap.enableAutoSizing = true;
        cap.fontSizeMin = 5.5f;
        cap.fontSizeMax = 7.5f;

        return slot.GetComponent<Button>();
    }

    private static void Divider(RectTransform card, string name, float yFromTop)
    {
        var line = Make(name, card, typeof(Image));
        float cy = MenuCardH / 2f - yFromTop;
        Centre(line, 0f, cy, 306f, 2f);
        var img = line.GetComponent<Image>();
        img.sprite = Sprite($"{MenuArt}/divider.png");
        img.color = Color.white;
        img.raycastTarget = false;
    }

    // ======================================================================================
    // Language picker — same chassis as the Menu (replaces the older Clay-system popup)
    // ======================================================================================

    private const string PickerPrefab = "Assets/Resources/UI/Modals/LanguagePicker.prefab";
    private const float PickerW = 360f, PickerH = 372f;
    private const float LangRowW = 150f, LangRowH = 40f, LangRowGap = 8f;

    [MenuItem("Ludo NX/UI/Build Language Picker", priority = 10)]
    public static void BuildLanguagePicker()
    {
        var root = Make("LanguagePicker", null, typeof(CanvasGroup), typeof(ListPickerModal));
        var modal = root.GetComponent<ListPickerModal>();
        Stretch(root);

        var band = Band(root, "Frame", DesignBand.Edge.Fill, 0f);
        band.GetComponent<DesignBand>().referenceSize = Reference;
        var content = Make("Content", band);
        Stretch(content);

        var scrim = Make("Scrim", content, typeof(Image), typeof(Button));
        Stretch(scrim);
        scrim.offsetMin = new Vector2(-3000f, -3000f);
        scrim.offsetMax = new Vector2(3000f, 3000f);
        scrim.GetComponent<Image>().color = new Color(0f, 0f, 0f, ScrimAlpha);
        scrim.GetComponent<Button>().transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(scrim.GetComponent<Button>().onClick, modal.OnScrimTapped);

        var card = Make("Card", content, typeof(Image));
        Centre(card, 0f, (CloseSize + CloseGap) / 2f, PickerW, PickerH);
        Sliced(card.GetComponent<Image>(), $"{Cards}/card_shell.png", 4f);
        card.GetComponent<Image>().raycastTarget = true;
        modal.card = card;

        var nameplate = Make("Nameplate", card, typeof(Image));
        nameplate.anchorMin = nameplate.anchorMax = new Vector2(0.5f, 1f);
        nameplate.pivot = new Vector2(0.5f, 0.5f);
        nameplate.sizeDelta = new Vector2(180f, 38f);
        nameplate.anchoredPosition = Vector2.zero;
        var plateImg = nameplate.GetComponent<Image>();
        Sliced(plateImg, $"{Cards}/pill_cream.png", 4f);
        plateImg.color = new Color32(245, 235, 218, 255);
        var title = Text(nameplate, "Title", "LANGUAGE", 18f, InkBlack, TextAlignmentOptions.Center);
        title.characterSpacing = 2f;
        Stretch(title.rectTransform);
        modal.titleLabel = title;

        var rows = Make("Rows", card);
        rows.anchorMin = rows.anchorMax = new Vector2(0.5f, 0.5f);
        rows.pivot = new Vector2(0.5f, 0.5f);
        rows.sizeDelta = new Vector2(PickerW - 40f, 5 * LangRowH + 4 * LangRowGap);
        rows.anchoredPosition = new Vector2(0f, 6f);
        modal.rowContainer = rows;

        var template = Make("RowTemplate", rows, typeof(Image), typeof(Button));
        template.anchorMin = template.anchorMax = template.pivot = new Vector2(0.5f, 0.5f);
        template.sizeDelta = new Vector2(LangRowW, LangRowH);
        Sliced(template.GetComponent<Image>(), $"{Cards}/pill_cream.png", 4f);
        template.GetComponent<Button>().targetGraphic = template.GetComponent<Image>();
        var label = Text(template, "Label", "English", 15f, InkBlack, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = 15f;
        template.gameObject.SetActive(false);

        modal.plainRowTemplate = template.GetComponent<Button>();
        modal.rowIdleTint = new Color32(245, 235, 218, 255);   // cream pill, as the nameplate
        modal.rowSelectedTint = ChipTan;                        // action.neutral.face, as the Menu chips
        modal.labelIdle = new Color32(96, 72, 48, 255);
        modal.labelSelected = GlyphCream;
        modal.rowHeight = LangRowH;
        modal.rowGap = LangRowGap;
        modal.singleColumnWidth = PickerW - 60f;
        modal.twoColumnWidth = LangRowW;
        modal.columnOffset = LangRowW / 2f + 6f;

        // Say it plainly: nothing is translated yet, the choice is only remembered.
        var note = Text(card, "Note", "Only English is translated so far. Your choice is saved.", 10.5f,
            new Color32(120, 96, 70, 255), TextAlignmentOptions.Center);
        note.rectTransform.anchorMin = note.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        note.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        note.rectTransform.sizeDelta = new Vector2(PickerW - 40f, 16f);
        note.rectTransform.anchoredPosition = new Vector2(0f, 30f);
        note.enableAutoSizing = true;
        note.fontSizeMin = 8f;
        note.fontSizeMax = 10.5f;

        CloseButton(card, modal);

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PickerPrefab);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Ludo NX] Language picker built -> {PickerPrefab}.");
    }

    // ======================================================================================
    // Settings family on the Menu chassis: Settings, Edit Profile, Text page
    // (replaces the older Clay-system popups; LudoNXScreenAuthor now calls these)
    // ======================================================================================

    private const string BodyFontPath = "Assets/Fonts/Nunito-Bold.asset";
    private static readonly Color32 SubInk = new Color32(96, 72, 48, 255);
    private static readonly Color32 PillCream = new Color32(245, 235, 218, 255);
    private static readonly Color32 SwitchTrackOff = new Color32(0xB9, 0xA8, 0x92, 255);

    /// <summary>Scrim, card, nameplate and X — the shared shell every chassis popup starts from.
    /// Rebuilt on the same riveted `panel_menu.png` card and dark-plaque nameplate as the Menu
    /// popup itself (27 Sep, owner: "use that same card style to do everything"), replacing the
    /// old MatchSetup `card_shell.png`/cream-pill chassis these popups used before. The nameplate
    /// can't reuse the Menu's own `banner_menu_title.png` verbatim — that art has the word "MENU"
    /// baked into it — so it approximates the same dark-plaque-with-diamond-accents look with a
    /// tinted pill and live TMP text instead, which also means every title here stays real text.</summary>
    private static RectTransform Shell<T>(string name, string title, float w, float h,
        out RectTransform card, out T modal) where T : UIModal
    {
        ConfigureSprites(MenuArt, MenuBorders);

        var root = Make(name, null, typeof(CanvasGroup), typeof(T));
        modal = root.GetComponent<T>();
        Stretch(root);

        var band = Band(root, "Frame", DesignBand.Edge.Fill, 0f);
        band.GetComponent<DesignBand>().referenceSize = Reference;
        var content = Make("Content", band);
        Stretch(content);

        var scrim = Make("Scrim", content, typeof(Image), typeof(Button));
        Stretch(scrim);
        scrim.offsetMin = new Vector2(-3000f, -3000f);
        scrim.offsetMax = new Vector2(3000f, 3000f);
        scrim.GetComponent<Image>().color = new Color(0f, 0f, 0f, ScrimAlpha);
        scrim.GetComponent<Button>().transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(scrim.GetComponent<Button>().onClick, modal.OnScrimTapped);

        card = Make("Card", content, typeof(Image));
        Centre(card, 0f, (CloseSize + CloseGap) / 2f, w, h);
        Sliced(card.GetComponent<Image>(), $"{MenuArt}/panel_menu.png", 3f);
        card.GetComponent<Image>().raycastTarget = true;
        modal.card = card;

        float plateW = Mathf.Max(170f, title.Length * 12f + 60f);
        var nameplate = Make("Nameplate", card, typeof(Image));
        nameplate.anchorMin = nameplate.anchorMax = new Vector2(0.5f, 1f);
        nameplate.pivot = new Vector2(0.5f, 0.5f);
        nameplate.sizeDelta = new Vector2(plateW, 40f);
        nameplate.anchoredPosition = Vector2.zero;
        var plateImg = nameplate.GetComponent<Image>();
        Sliced(plateImg, $"{Cards}/pill_cream.png", 4f);
        plateImg.color = Brown;

        var diamondL = Text(nameplate, "DiamondL", "♦", 12f, Gold, TextAlignmentOptions.Center);
        InCard(diamondL.rectTransform, plateW, H.Left, 16f, 20f, 16f, 20f);
        var diamondR = Text(nameplate, "DiamondR", "♦", 12f, Gold, TextAlignmentOptions.Center);
        InCard(diamondR.rectTransform, plateW, H.Right, plateW - 16f, 20f, 16f, 20f);

        var t = Text(nameplate, "Title", title, 16f, Cream, TextAlignmentOptions.Center);
        t.characterSpacing = 2f;
        t.enableAutoSizing = true; t.fontSizeMin = 10f; t.fontSizeMax = 16f;
        // Inset clear of the two diamonds instead of a full Stretch, so a long title like
        // "TERMS OF SERVICE" shrinks to fit between them rather than overlapping them.
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = new Vector2(32f, 0f);
        t.rectTransform.offsetMax = new Vector2(-32f, 0f);

        CloseButton(card, modal);
        return root;
    }

    private static TextMeshProUGUI Heading(RectTransform card, float w, string text, float cy)
    {
        var h = Text(card, "Heading_" + text, text, 13f, new Color32(120, 96, 70, 255), TextAlignmentOptions.Center);
        h.characterSpacing = 2f;
        InCard(h.rectTransform, w, H.Left, w / 2f, cy, 220f, 18f);
        return h;
    }

    private static RectTransform PillRow(RectTransform card, float w, string name, float cy, float rowW, float rowH)
    {
        var row = Make(name, card, typeof(Image));
        InCard(row, w, H.Left, w / 2f, cy, rowW, rowH);
        Sliced(row.GetComponent<Image>(), $"{Cards}/pill_cream.png", 4f);
        row.GetComponent<Image>().color = PillCream;
        return row;
    }

    private static void RowIcon(RectTransform row, float rowW, params string[] names)
    {
        var sprite = UIIconSet.IconAny(names);
        // Not every glyph is in UIIconSet (music_note and vibration were drawn for this popup);
        // fall back to the PNG of the same name in the icons folder.
        for (int i = 0; sprite == null && i < names.Length; i++) sprite = IconPng(names[i]);
        if (sprite == null) return;
        var img = Picture(row, "Icon", null);
        img.sprite = sprite;
        img.color = SubInk;
        img.preserveAspect = true;
        InCard((RectTransform)img.transform, rowW, H.Left, 22f, 20f, 20f, 20f);
    }

    private static Sprite IconPng(string name)
    {
        string path = $"Assets/Art/UI/Icons/{name}.png";
        if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) return null;
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static TextMeshProUGUI RowLabel(RectTransform row, float rowW, string text)
    {
        var l = Text(row, "Label", text, 15f, InkBlack, TextAlignmentOptions.MidlineLeft);
        LeftIn(l.rectTransform, 40f, 20f, rowW - 110f, 24f);
        return l;
    }

    private static PillToggle Switch(RectTransform row, float rowW)
    {
        var go = Make("Switch", row, typeof(Image), typeof(Button), typeof(PillToggle));
        InCard(go, rowW, H.Right, rowW - 34f, 20f, 48f, 26f);
        var track = go.GetComponent<Image>();
        Sliced(track, $"{Cards}/pill_cream.png", 6f);
        track.color = SwitchTrackOff;
        go.GetComponent<Button>().transition = Selectable.Transition.None;

        var knob = Make("Knob", go, typeof(Image));
        knob.anchorMin = knob.anchorMax = knob.pivot = new Vector2(0.5f, 0.5f);
        knob.sizeDelta = new Vector2(20f, 20f);
        knob.anchoredPosition = new Vector2(-11f, 0f);
        knob.GetComponent<Image>().sprite = Sprite(CircleSprite);
        knob.GetComponent<Image>().raycastTarget = false;

        var toggle = go.GetComponent<PillToggle>();
        toggle.track = track;
        toggle.knob = knob;
        toggle.offColour = SwitchTrackOff;
        toggle.onColour = new Color32(0x4D, 0xAE, 0x51, 255);
        toggle.offX = -11f; toggle.onX = 11f;
        return toggle;
    }

    private static Button SmallPill(RectTransform card, float w, string name, string label, float cx, float cy, float pw)
    {
        var b = Make(name, card, typeof(Image), typeof(Button));
        InCard(b, w, H.Left, cx, cy, pw, 34f);
        Sliced(b.GetComponent<Image>(), $"{Cards}/pill_cream.png", 4f);
        b.GetComponent<Image>().color = ChipTan;
        b.GetComponent<Button>().targetGraphic = b.GetComponent<Image>();
        var t = Text(b, "Label", label, 13f, GlyphCream, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
        return b.GetComponent<Button>();
    }

    [MenuItem("Ludo NX/UI/Build Settings Popups", priority = 10)]
    public static void BuildSettingsFamily()
    {
        BuildSettingsPopup();
        BuildEditProfile();
        BuildTextPage();
        BuildLanguagePicker();
        BuildNxCup();
        AssetDatabase.SaveAssets();
        Debug.Log("[Ludo NX] Settings, Edit Profile, Text Page and Language popups built on the Menu chassis.");
    }

    public static void BuildSettingsPopup()
    {
        const float W = 360f, RowW = 320f, RowH = 40f, CardHeight = 404f;
        var root = Shell<SettingsModal>("Settings", "SETTINGS", W, CardHeight, out var card, out var modal);
        float y = 46f;

        Heading(card, W, "SOUND", y); y += 28f;
        var sound = PillRow(card, W, "SoundRow", y + RowH / 2f, RowW, RowH);
        RowIcon(sound, RowW, "volume_up", "music_note"); RowLabel(sound, RowW, "Sound Effects");
        modal.soundToggle = Switch(sound, RowW); y += RowH + 6f;
        var music = PillRow(card, W, "MusicRow", y + RowH / 2f, RowW, RowH);
        RowIcon(music, RowW, "music_note"); RowLabel(music, RowW, "Music");
        modal.musicToggle = Switch(music, RowW); y += RowH + 6f;
        var vib = PillRow(card, W, "VibrationRow", y + RowH / 2f, RowW, RowH);
        RowIcon(vib, RowW, "vibration"); RowLabel(vib, RowW, "Vibration");
        modal.vibrationToggle = Switch(vib, RowW); y += RowH + 18f;

        Heading(card, W, "GAME", y); y += 28f;
        var lang = PillRow(card, W, "LanguageRow", y + RowH / 2f, RowW, RowH);
        lang.gameObject.AddComponent<Button>().targetGraphic = lang.GetComponent<Image>();
        RowIcon(lang, RowW, "public", "language"); RowLabel(lang, RowW, "Language");
        var value = Text(lang, "Value", "English", 13f, SubInk, TextAlignmentOptions.MidlineRight);
        InCard(value.rectTransform, RowW, H.Right, RowW - 84f, 20f, 110f, 22f);   // ends 12pt clear of the chevron
        value.enableAutoSizing = true; value.fontSizeMin = 9f; value.fontSizeMax = 13f;
        var chevron = Text(lang, "Chevron", "›", 22f, SubInk, TextAlignmentOptions.Center);
        InCard(chevron.rectTransform, RowW, H.Right, RowW - 14f, 18f, 16f, 26f);
        modal.languageRow = lang.GetComponent<Button>();
        modal.languageValue = value;
        y += RowH + 18f;

        Heading(card, W, "ABOUT", y); y += 30f;
        modal.aboutButton = SmallPill(card, W, "AboutButton", "ABOUT", W / 2f - 106f, y, 96f);
        modal.privacyButton = SmallPill(card, W, "PrivacyButton", "PRIVACY", W / 2f, y, 96f);
        modal.termsButton = SmallPill(card, W, "TermsButton", "TERMS", W / 2f + 106f, y, 96f);
        y += 38f;

        var version = Text(card, "Version", "VERSION 1.0", 10.5f, new Color32(120, 96, 70, 255), TextAlignmentOptions.Center);
        InCard(version.rectTransform, W, H.Left, W / 2f, y, 200f, 14f);
        modal.versionLabel = version;

        SaveShell(root, "Assets/Resources/UI/Modals/Settings.prefab");
    }

    public static void BuildEditProfile()
    {
        const float W = 360f, CardHeight = 350f;
        var root = Shell<EditProfileModal>("EditProfile", "PROFILE", W, CardHeight, out var card, out var modal);

        Heading(card, W, "YOUR NAME", 46f);

        // TMP's own factory builds the text area, viewport and caret children correctly.
        var fieldGo = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        fieldGo.name = "NameField";
        var field = (RectTransform)fieldGo.transform;
        field.SetParent(card, false);
        InCard(field, W, H.Left, W / 2f, 80f, 300f, 42f);
        var bg = fieldGo.GetComponent<Image>();
        Sliced(bg, $"{Cards}/pill_cream.png", 4f);
        bg.color = Color.white;
        var input = fieldGo.GetComponent<TMP_InputField>();
        input.characterLimit = 16;
        input.lineType = TMP_InputField.LineType.SingleLine;
        foreach (var tmp in fieldGo.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            bool placeholder = tmp.gameObject.name == "Placeholder";
            tmp.font = Lilita;
            tmp.fontSize = 18f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = placeholder ? new Color32(150, 130, 105, 255) : (Color)(Color32)InkBlack;
            if (placeholder) tmp.text = "Enter your name";
        }
        modal.nameField = input;

        Heading(card, W, "YOUR PICTURE", 126f);
        var picker = Make("Pictures", card);
        InCard(picker, W, H.Left, W / 2f, 194f, 320f, 120f);
        modal.pickerRoot = picker;
        modal.Cell = 50f;
        modal.Gap = 10f;
        modal.PerRow = 5;

        // SAVE: green is buy/confirm (D3). The X below the card is cancel.
        var save = Make("SaveButton", card, typeof(Image), typeof(Button));
        InCard(save, W, H.Left, W / 2f, 292f, 200f, 50f);
        var saveImg = save.GetComponent<Image>();
        saveImg.sprite = Sprite("Assets/Art/UI/Shop/price_pill.png");
        saveImg.type = saveImg.sprite != null && saveImg.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        saveImg.pixelsPerUnitMultiplier = 2.4f;
        save.GetComponent<Button>().targetGraphic = saveImg;
        var saveLabel = Text(save, "Label", "SAVE", 20f, Color.white, TextAlignmentOptions.Center);
        Stretch(saveLabel.rectTransform);
        modal.saveButton = save.GetComponent<Button>();

        SaveShell(root, "Assets/Resources/UI/Modals/EditProfile.prefab");
    }

    public static void BuildTextPage()
    {
        const float W = 360f, CardHeight = 440f;
        var root = Shell<TextPageModal>("TextPage", "PRIVACY POLICY", W, CardHeight, out var card, out var modal);
        modal.titleLabel = card.Find("Nameplate/Title").GetComponent<TextMeshProUGUI>();

        var panel = Make("BodyPanel", card, typeof(Image));
        InCard(panel, W, H.Left, W / 2f, 236f, 316f, 364f);
        Sliced(panel.GetComponent<Image>(), $"{Cards}/pill_cream.png", 4f);
        panel.GetComponent<Image>().color = PillCream;

        var body = Text(panel, "Body", "", 13f, InkBlack, TextAlignmentOptions.TopLeft);
        body.rectTransform.anchorMin = Vector2.zero; body.rectTransform.anchorMax = Vector2.one;
        body.rectTransform.offsetMin = new Vector2(16f, 14f); body.rectTransform.offsetMax = new Vector2(-16f, -14f);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.enableAutoSizing = true; body.fontSizeMin = 9f; body.fontSizeMax = 13f;
        var bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);
        if (bodyFont != null) body.font = bodyFont;
        modal.bodyLabel = body;

        SaveShell(root, "Assets/Resources/UI/Modals/TextPage.prefab");
    }

    [MenuItem("Ludo NX/UI/Build NX Cup Popup", priority = 11)]
    public static void BuildNxCup()
    {
        const float W = 340f, CardHeight = 360f;
        var root = Shell<NxCupModal>("NxCup", "NX CUP", W, CardHeight, out var card, out var modal);

        var glow = Picture(card, "Glow", "Assets/Art/UI/MatchSetup/tile_glow.png");
        glow.color = new Color(1f, 0.86f, 0.4f, 0.9f);
        InCard((RectTransform)glow.transform, W, H.Left, W / 2f, 108f, 190f, 190f);
        var cup = Picture(card, "Cup", "Assets/Art/UI/HomeScreen/Badges/refrence_tournament_btn.png");
        cup.preserveAspect = true;
        InCard((RectTransform)cup.transform, W, H.Left, W / 2f, 108f, 90f, 150f);

        var headline = Text(card, "Headline", "TOURNAMENTS ARE COMING", 17f, InkBlack, TextAlignmentOptions.Center);
        InCard(headline.rectTransform, W, H.Left, W / 2f, 204f, W - 40f, 24f);
        headline.enableAutoSizing = true; headline.fontSizeMin = 11f; headline.fontSizeMax = 17f;

        var body = Text(card, "Body", "The NX Cup opens when online play launches. Until then, climb the mission ladder and get ready.", 13f, SubInk, TextAlignmentOptions.Top);
        InCard(body.rectTransform, W, H.Left, W / 2f, 246f, W - 50f, 52f);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.enableAutoSizing = true; body.fontSizeMin = 9f; body.fontSizeMax = 13f;
        var bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);
        if (bodyFont != null) body.font = bodyFont;

        modal.levelsButton = SmallPill(card, W, "LevelsButton", "GO TO LEVELS", W / 2f, 312f, 200f);

        SaveShell(root, "Assets/Resources/UI/Modals/NxCup.prefab");
        AssetDatabase.SaveAssets();
        Debug.Log("[Ludo NX] NX Cup popup built.");
    }

    private static void SaveShell(RectTransform root, string path)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
        Object.DestroyImmediate(root.gameObject);
    }

    // ── Close X: black circle outline with centered 'X', no fill, compact ────────────────
    private const float CloseSize = 34f;
    private const float CloseGap = 16f;

    private static void CloseButton(RectTransform card, UIModal modal)
    {
        var btn = Make("CloseButton", card, typeof(Image), typeof(Button));
        btn.anchorMin = btn.anchorMax = new Vector2(0.5f, 0f);
        btn.pivot = new Vector2(0.5f, 0.5f);
        btn.sizeDelta = new Vector2(50f, 50f);
        btn.anchoredPosition = new Vector2(0f, -(CloseGap + CloseSize / 2f));
        Invisible(btn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(btn.GetComponent<Button>().onClick, modal.Dismiss);

        var disc = Picture(btn, "Icon", "Assets/Art/UI/Shared/btn_cancel_black_circle.png");
        Centre((RectTransform)disc.transform, 0f, 0f, CloseSize, CloseSize);
        disc.preserveAspect = true;
        btn.GetComponent<Button>().targetGraphic = disc;
    }
}
