using System;
using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared layout helpers for the screen builders.
///
/// The Friends and main-menu builders each grew their own copy of these; pulling them out here
/// means a fix to the safe-area maths or the 9-slice import settings lands on every screen at
/// once instead of on whichever builder was edited last.
///
/// Everything is measured in points of a 440 x 956 design frame (the frame the references are
/// exported from) and laid out inside <see cref="DesignBand"/>s, so screens scale uniformly on
/// every device and never stretch on one axis.
/// </summary>
public static class LudoNXUIKit
{
    public static readonly Vector2 Reference = new Vector2(440f, 956f);
    public const float RefW = 440f;

    /// <summary>Nav bar height minus the overlap the panel tucks under it, in nav (402x874) points.</summary>
    public const float NavReserve = 116f - 7f;
    public const float SafeTop = 45f;

    public static readonly Color Ink = new Color32(6, 5, 5, 255);
    public static readonly Color SubInk = new Color32(78, 74, 65, 255);
    public static readonly Color Brown = new Color32(75, 58, 42, 255);
    public static readonly Color Cream = new Color32(255, 242, 220, 255);
    public static readonly Color Slate = new Color32(40, 45, 56, 255);
    public static readonly Color Gold = new Color32(232, 185, 38, 255);

    public const string FriendsArt = "Assets/Art/UI/Friends";
    public const string HomeArt = "Assets/Art/UI/HomeScreen";
    public const string LilitaPath = "Assets/Fonts/LilitaOne-Regular.asset";

    public enum H { Left, Right, Centre }

    private static TMP_FontAsset _lilita;

    public static TMP_FontAsset Lilita
    {
        get
        {
            if (_lilita == null)
            {
                _lilita = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LilitaPath);
                if (_lilita == null) Debug.LogError($"[Ludo NX] Lilita One font asset missing at {LilitaPath}.");
            }
            return _lilita;
        }
    }

    // ==========================================================================================
    // construction
    // ==========================================================================================

    public static RectTransform Make(string name, Transform parent, params Type[] comps)
    {
        var types = new List<Type>(comps.Length + 1) { typeof(RectTransform) };
        foreach (var t in comps)
        {
            if (t != typeof(RectTransform)) types.Add(t);
        }
        var go = new GameObject(name, types.ToArray());
        var rt = go.GetComponent<RectTransform>();
        if (parent != null) rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        return rt;
    }

    public static RectTransform Band(RectTransform parent, string name, DesignBand.Edge edge, float height)
    {
        var rt = Make(name, parent, typeof(DesignBand));
        var band = rt.GetComponent<DesignBand>();
        band.referenceSize = Reference;
        band.edge = edge;
        band.designHeight = height;
        return rt;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ==========================================================================================
    // placement, in points of the 440x956 frame
    // ==========================================================================================

    /// <summary>Top-anchored, placed by centre. x runs across the 440pt frame.</summary>
    public static void Top(RectTransform rt, H h, float cx, float cy, float w, float hgt)
    {
        float ax = h == H.Left ? 0f : h == H.Right ? 1f : 0.5f;
        float px = h == H.Left ? cx : h == H.Right ? cx - RefW : cx - RefW / 2f;
        rt.anchorMin = rt.anchorMax = new Vector2(ax, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, hgt);
        rt.anchoredPosition = new Vector2(px, -cy);
    }

    /// <summary>Inside a card of <paramref name="cardW"/> points; x measured from its left edge.</summary>
    public static void InCard(RectTransform rt, float cardW, H h, float cx, float cy, float w, float hgt)
    {
        float ax = h == H.Right ? 1f : 0f;
        float px = h == H.Right ? cx - cardW : cx;
        rt.anchorMin = rt.anchorMax = new Vector2(ax, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, hgt);
        rt.anchoredPosition = new Vector2(px, -cy);
    }

    /// <summary>Left-aligned rect that grows rightward from <paramref name="left"/>.</summary>
    public static void LeftIn(RectTransform rt, float left, float cy, float w, float hgt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(w, hgt);
        rt.anchoredPosition = new Vector2(left, -cy);
    }

    public static void Centre(RectTransform rt, float dx, float dy, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(dx, dy);
    }

    // ==========================================================================================
    // widgets
    // ==========================================================================================

    public static TextMeshProUGUI Text(RectTransform parent, string name, string text, float size,
        Color color, TextAlignmentOptions align)
    {
        var rt = Make(name, parent, typeof(TextMeshProUGUI));
        var tmp = rt.GetComponent<TextMeshProUGUI>();
        if (Lilita != null) tmp.font = Lilita;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Image Picture(RectTransform parent, string name, string spritePath)
    {
        var rt = Make(name, parent, typeof(Image));
        var img = rt.GetComponent<Image>();
        if (!string.IsNullOrEmpty(spritePath)) img.sprite = Sprite(spritePath);
        img.raycastTarget = false;
        return img;
    }

    public static void Sliced(Image img, string spritePath, float pixelsPerPoint)
    {
        img.sprite = Sprite(spritePath);
        img.type = Image.Type.Sliced;
        img.fillCenter = true;
        img.pixelsPerUnitMultiplier = pixelsPerPoint;
    }

    public static void Invisible(Image img)
    {
        img.sprite = null;
        img.color = new Color(1f, 1f, 1f, 0f);
    }

    public static Sprite Sprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogError($"[Ludo NX] Missing sprite: {path}");
        return sprite;
    }

    // ==========================================================================================
    // import settings
    // ==========================================================================================

    /// <summary>
    /// Makes every PNG in a folder a crisp UI sprite, and applies 9-slice borders by file name.
    /// Uncompressed because these are flat, large-area UI plates: DXT banding on a tan gradient
    /// is far more visible than the memory it saves.
    /// </summary>
    public static void ConfigureSprites(string folder, Dictionary<string, Vector4> borders)
    {
        if (!Directory.Exists(folder)) { Debug.LogError($"[Ludo NX] Art folder missing: {folder}"); return; }

        int changed = 0;
        foreach (string file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
        {
            string asset = file.Replace('\\', '/');
            if (AssetImporter.GetAtPath(asset) is not TextureImporter importer) continue;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }
            if (importer.maxTextureSize < 2048) { importer.maxTextureSize = 2048; dirty = true; }

            if (borders != null && borders.TryGetValue(Path.GetFileName(asset), out Vector4 border)
                && importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                dirty = true;
            }

            if (dirty) { importer.SaveAndReimport(); changed++; }
        }
        if (changed > 0) Debug.Log($"[Ludo NX] Configured {changed} sprite(s) in {folder}.");
    }
}
