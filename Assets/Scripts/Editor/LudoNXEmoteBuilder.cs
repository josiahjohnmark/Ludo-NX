using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Import settings for the animated-emoji sprite sheets in Assets/Art/UI/Emotes, and a one-click
/// preview so all ten can be eyeballed together in Play mode.
///
/// These load as plain Texture2D, not Sprite: <see cref="EmoteFlipbook"/> slices its own frame
/// rects out of the raw texture at runtime, so the importer's sprite pipeline (border, pivot,
/// slice mode) is irrelevant here and left at its Default texture type.
/// </summary>
public static class LudoNXEmoteBuilder
{
    private const string EmoteArt = "Assets/Art/UI/Emotes";

    [MenuItem("Ludo NX/UI/Configure Emote Sprites", priority = 9)]
    public static void ConfigureImports()
    {
        if (!Directory.Exists(EmoteArt))
        {
            Debug.LogError($"[Ludo NX] {EmoteArt} does not exist yet - run the emote_pipeline.py script first.");
            return;
        }

        int changed = 0;
        foreach (string file in Directory.GetFiles(EmoteArt, "*.png", SearchOption.TopDirectoryOnly))
        {
            string asset = file.Replace('\\', '/');
            if (AssetImporter.GetAtPath(asset) is not TextureImporter importer) continue;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }
            if (importer.maxTextureSize < 2048) { importer.maxTextureSize = 2048; dirty = true; }
            if (!importer.isReadable) { importer.isReadable = false; dirty = true; } // Sprite.Create doesn't need CPU readback

            if (dirty) { importer.SaveAndReimport(); changed++; }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Configured {changed} emote sheet(s) in {EmoteArt}.");
    }

    /// <summary>
    /// Drops a temporary grid of every emote in the manifest onto the active canvas so they can
    /// all be watched looping at once. Play mode only; the grid is not saved into any prefab.
    /// </summary>
    [MenuItem("Ludo NX/UI/Preview All Emotes (play mode)", priority = 10)]
    public static void PreviewAll()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Ludo NX] Enter play mode first.");
            return;
        }

        var manifestPath = $"{EmoteArt}/manifest.json";
        var manifestAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath);
        if (manifestAsset == null)
        {
            Debug.LogError($"[Ludo NX] No manifest at {manifestPath}.");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Ludo NX] No Canvas in the scene to preview onto.");
            return;
        }

        var entries = JsonUtility.FromJson<Wrapper>("{\"items\":" + manifestAsset.text + "}").items;

        var root = new GameObject("EmotePreview (temp)", typeof(RectTransform), typeof(GridLayoutGroup));
        var rt = (RectTransform)root.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900f, 500f);
        rt.anchoredPosition = Vector2.zero;

        var grid = root.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(140f, 140f);
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.11f, 0.13f, 0.92f);

        foreach (var e in entries)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{EmoteArt}/{e.file}");
            if (tex == null) { Debug.LogWarning($"[Ludo NX] Missing texture for '{e.name}'."); continue; }

            var cell = new GameObject(e.name, typeof(RectTransform), typeof(Image), typeof(EmoteFlipbook));
            cell.transform.SetParent(root.transform, false);

            var flip = cell.GetComponent<EmoteFlipbook>();
            flip.texture = tex;
            flip.frameCount = e.frameCount;
            flip.contentSize = e.contentSize;
            flip.framesPerSecond = e.fps;
        }

        Debug.Log($"[Ludo NX] Previewing {entries.Length} emotes - delete 'EmotePreview (temp)' in the Hierarchy when done.");
    }

    [System.Serializable]
    private class ManifestEntry
    {
        public string name;
        public string file;
        public int frameCount;
        public int frameSize;
        public int contentSize;
        public float fps;
    }

    [System.Serializable]
    private class Wrapper
    {
        public ManifestEntry[] items;
    }

    // ======================================================================================
    // MatchEmoteTray.prefab + MatchMessageTray.prefab
    //
    // Vertical placement on both is a best estimate, not a verified fit: it needs a live check
    // against the dice tray and board centre once Unity is reachable (see match-emoji-tray.json's
    // "do not let the tray cover the board's centre cross or the dice tray"). The message list is
    // scrollable specifically so its card height stays fixed regardless of how that check goes —
    // only the scroll window's position needs adjusting, not the content.
    // ======================================================================================

    private const string CardsDir = "Assets/Art/UI/MatchSetup/Cards";
    private const string TrayPrefabDir = "Assets/Resources/UI/Modals";
    private static readonly Color32 InkBlack = new Color32(30, 22, 12, 255);

    [MenuItem("Ludo NX/UI/Build Match Trays (Emoji + Message)", priority = 11)]
    public static void BuildTrays()
    {
        BuildEmoteTray();
        BuildMessageTray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static ManifestEntry[] LoadManifest()
    {
        var manifestAsset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{EmoteArt}/manifest.json");
        if (manifestAsset == null)
        {
            Debug.LogError($"[Ludo NX] No manifest at {EmoteArt}/manifest.json — cannot build the trays.");
            return new ManifestEntry[0];
        }
        return JsonUtility.FromJson<Wrapper>("{\"items\":" + manifestAsset.text + "}").items;
    }

    [MenuItem("Ludo NX/UI/Build Match Emoji Tray", priority = 12)]
    public static void BuildEmoteTray()
    {
        var manifest = LoadManifest();
        if (manifest.Length == 0) return;

        // A fixed, sensible default loadout of 8 — there is no per-player equip/unlock system yet
        // (that is the shop's job, not built), so every match uses the same eight for now.
        string[] loadout = { "thumbsup", "laugh", "fire", "party", "cool", "wow", "cry", "angry" };

        var root = Make("MatchEmoteTray", null, typeof(CanvasGroup), typeof(MatchEmoteTray));
        var tray = root.GetComponent<MatchEmoteTray>();
        Stretch(root);

        var band = Band(root, "Frame", DesignBand.Edge.Fill, 0f);
        band.GetComponent<DesignBand>().referenceSize = Reference;
        var content = Make("Content", band);
        Stretch(content);
        // No scrim, no raycast-blocking layer at all: the board must stay tappable underneath.

        const float cardW = 356f, cell = 56f, gap = 8f, cols = 4;
        float gridW = cols * cell + (cols - 1) * gap;
        float dividerGap = 12f;
        float gridH = 2f * cell + dividerGap;
        float pad = 18f;
        float cardH = pad * 2f + gridH;

        var card = Make("Card", content, typeof(Image));
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(cardW, cardH);
        card.anchoredPosition = new Vector2(0f, 90f + cardH / 2f); // see class-header caveat
        Sliced(card.GetComponent<Image>(), "Assets/Art/UI/Menu/panel_menu.png", 1f);
        card.GetComponent<Image>().raycastTarget = true;
        tray.card = card;

        // Divider line between Top Row (recently/frequently used) and Bottom Row
        var divider = Picture(card, "RecentDivider", "Assets/Art/UI/Menu/divider.png");
        Centre((RectTransform)divider.transform, 0f, 0f, gridW + 20f, 2f);
        divider.raycastTarget = false;

        var textures = new Texture2D[8];
        var frameCounts = new int[8];
        var contentSizes = new int[8];
        var fpsValues = new float[8];
        var buttons = new Button[8];
        var overlays = new Image[8];

        float startX = -gridW / 2f + cell / 2f;
        float topRowY = (cell + dividerGap) / 2f;
        float bottomRowY = -(cell + dividerGap) / 2f;

        for (int i = 0; i < 8; i++)
        {
            var entry = System.Array.Find(manifest, m => m.name == loadout[i]);
            if (entry == null) { Debug.LogWarning($"[Ludo NX] Emote '{loadout[i]}' not in manifest."); continue; }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{EmoteArt}/{entry.file}");
            textures[i] = tex;
            frameCounts[i] = entry.frameCount;
            contentSizes[i] = entry.contentSize;
            fpsValues[i] = entry.fps;

            int col = i % 4, row = i / 4;
            float cx = startX + col * (cell + gap);
            float cy = row == 0 ? topRowY : bottomRowY;

            var well = Make($"Slot{i}", card, typeof(Image), typeof(Button));
            Centre(well, cx, cy, cell, cell);
            Sliced(well.GetComponent<Image>(), $"{CardsDir}/pill_cream.png", 3f);
            buttons[i] = well.GetComponent<Button>();

            var preview = Make("Preview", well, typeof(Image), typeof(EmoteFlipbook));
            Centre((RectTransform)preview.transform, 0f, 0f, cell - 8f, cell - 8f);
            var flip = preview.GetComponent<EmoteFlipbook>();
            flip.texture = tex;
            flip.frameCount = entry.frameCount;
            flip.contentSize = entry.contentSize;
            flip.framesPerSecond = entry.fps;
            preview.GetComponent<Image>().raycastTarget = false;

            var overlay = Make("Cooldown", well, typeof(Image));
            Stretch((RectTransform)overlay.transform);
            var overlayImg = overlay.GetComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.55f);
            overlayImg.type = Image.Type.Filled;
            overlayImg.fillMethod = Image.FillMethod.Radial360;
            overlayImg.fillOrigin = (int)Image.Origin360.Top;
            overlayImg.raycastTarget = false;
            overlay.gameObject.SetActive(false);
            overlays[i] = overlayImg;
        }

        tray.slotButtons = buttons;
        tray.slotCooldownOverlays = overlays;
        tray.slotTextures = textures;
        tray.slotFrameCounts = frameCounts;
        tray.slotContentSizes = contentSizes;
        tray.slotFps = fpsValues;

        Directory.CreateDirectory(TrayPrefabDir);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, $"{TrayPrefabDir}/MatchEmoteTray.prefab");
        Object.DestroyImmediate(root.gameObject);
        Debug.Log($"[Ludo NX] Match emoji tray built -> {TrayPrefabDir}/MatchEmoteTray.prefab.");
    }

    [MenuItem("Ludo NX/UI/Build Match Message Tray", priority = 13)]
    public static void BuildMessageTray()
    {
        var root = Make("MatchMessageTray", null, typeof(CanvasGroup), typeof(MatchMessageTray));
        var tray = root.GetComponent<MatchMessageTray>();
        Stretch(root);

        var band = Band(root, "Frame", DesignBand.Edge.Fill, 0f);
        band.GetComponent<DesignBand>().referenceSize = Reference;
        var content = Make("Content", band);
        Stretch(content);

        const float cardW = 356f;
        const float tabsH = 40f;
        const float viewportH = 160f; // ~3 of 6 rows visible; scroll reaches the rest
        const float headerH = 22f;
        const float pad = 14f;
        float cardH = pad * 2f + tabsH + 8f + viewportH + headerH;

        var card = Make("Card", content, typeof(Image));
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(cardW, cardH);
        card.anchoredPosition = new Vector2(0f, 90f + cardH / 2f); // see class-header caveat
        Sliced(card.GetComponent<Image>(), $"{CardsDir}/card_shell.png", 4f);
        card.GetComponent<Image>().raycastTarget = true;
        tray.card = card;

        // Tabs
        float tabW = (cardW - pad * 2f - 8f) / 2f;
        var friendlyTab = Make("FriendlyTab", card, typeof(Image), typeof(Button));
        Top(friendlyTab, H.Left, pad, pad, tabW, tabsH);
        Sliced(friendlyTab.GetComponent<Image>(), $"{CardsDir}/pill_cream.png", 3f);
        var friendlyLbl = Text(friendlyTab, "Label", "FRIENDLY", 14f, Color.white, TextAlignmentOptions.Center);
        Stretch(friendlyLbl.rectTransform);
        tray.friendlyTab = friendlyTab.GetComponent<Button>();
        tray.friendlyTabBackground = friendlyTab.GetComponent<Image>();

        var reactionsTab = Make("ReactionsTab", card, typeof(Image), typeof(Button));
        Top(reactionsTab, H.Left, pad + tabW + 8f, pad, tabW, tabsH);
        Sliced(reactionsTab.GetComponent<Image>(), $"{CardsDir}/pill_cream.png", 3f);
        var reactionsLbl = Text(reactionsTab, "Label", "REACTIONS", 14f, InkBlack, TextAlignmentOptions.Center);
        Stretch(reactionsLbl.rectTransform);
        tray.reactionsTab = reactionsTab.GetComponent<Button>();
        tray.reactionsTabBackground = reactionsTab.GetComponent<Image>();

        // Cooldown header, straddling the top edge
        var header = Text(card, "CooldownHeader", "Wait 5s", 13f, new Color32(183, 39, 31, 255), TextAlignmentOptions.Center);
        header.rectTransform.anchorMin = header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        header.rectTransform.pivot = new Vector2(0.5f, 1f);
        header.rectTransform.sizeDelta = new Vector2(cardW - pad * 2f, headerH);
        header.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        header.gameObject.SetActive(false);
        tray.cooldownHeader = header;

        tray.friendlyList = BuildPhraseList(card, "FriendlyList", MatchMessageTray.Friendly, cardW, pad,
            tabsH + 16f, viewportH, out tray.friendlyButtons);
        tray.reactionsList = BuildPhraseList(card, "ReactionsList", MatchMessageTray.Reactions, cardW, pad,
            tabsH + 16f, viewportH, out tray.reactionsButtons);

        Directory.CreateDirectory(TrayPrefabDir);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, $"{TrayPrefabDir}/MatchMessageTray.prefab");
        Object.DestroyImmediate(root.gameObject);
        Debug.Log($"[Ludo NX] Match message tray built -> {TrayPrefabDir}/MatchMessageTray.prefab.");
    }

    /// <summary>A scrollable list of phrase rows. Fixed viewport height regardless of row count,
    /// so the card's own size never depends on how many phrases are in the list.</summary>
    private static GameObject BuildPhraseList(RectTransform card, string name, string[] phrases,
        float cardW, float pad, float topOffset, float viewportH, out Button[] buttons)
    {
        var viewport = Make(name, card, typeof(Image), typeof(RectMask2D));
        viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 1f);
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.sizeDelta = new Vector2(cardW - pad * 2f, viewportH);
        viewport.anchoredPosition = new Vector2(0f, -topOffset);
        Invisible(viewport.GetComponent<Image>());
        viewport.GetComponent<Image>().raycastTarget = true;

        var list = Make("List", viewport, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        list.anchorMin = new Vector2(0f, 1f);
        list.anchorMax = new Vector2(1f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        var vlg = list.GetComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.spacing = 8f;
        list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = list;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;

        float rowW = cardW - pad * 2f;
        buttons = new Button[phrases.Length];
        for (int i = 0; i < phrases.Length; i++)
        {
            var row = Make($"Row{i}", list, typeof(LayoutElement), typeof(Image), typeof(Button));
            row.sizeDelta = new Vector2(rowW, 40f);
            var le = row.GetComponent<LayoutElement>();
            le.preferredWidth = rowW;
            le.preferredHeight = 40f;
            Sliced(row.GetComponent<Image>(), $"{CardsDir}/pill_cream.png", 3f);
            buttons[i] = row.GetComponent<Button>();

            var label = Text(row, "Label", phrases[i], 15f, InkBlack, TextAlignmentOptions.Left);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(16f, 0f);
            label.rectTransform.offsetMax = Vector2.zero;
        }

        return viewport.gameObject;
    }
}
