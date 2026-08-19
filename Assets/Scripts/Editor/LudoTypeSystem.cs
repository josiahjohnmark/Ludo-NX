using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Applies the project type system and gives every text a solid, dimensional finish that matches
/// the bevelled UI art.
///
/// Two families, by role:
///   Baloo 2 ExtraBold — numbers and display: timer, points, win rate, level, ROLL.
///     Rounded and heavy, so it sits naturally against the glossy buttons and cards. Also ships
///     script siblings (Devanagari, Arabic, Bengali) for later localisation.
///   Nunito Bold — player names and labels. Rounded terminals echo the bevels.
///
/// "Solid" comes from TMP material presets rather than the font: a face dilate to thicken the
/// glyph, a dark outline to separate it from a busy background, and an underlay acting as a
/// contact shadow. Flat TMP text on this art reads as a sticker; these three together make it
/// read as part of the panel.
/// </summary>
public static class LudoTypeSystem
{
    private const string MatFolder = "Assets/Art/Generated/TextMaterials";

    // Ink colours. The card bodies are light tints, so card text is near-black warm brown for
    // contrast; anything sitting on the dark ground or on a coloured chip is white.
    private static readonly Color InkOnCard = new Color(0.152f, 0.094f, 0.094f);
    private static readonly Color InkValue = new Color(0.118f, 0.075f, 0.075f);
    private static readonly Color InkOnDark = Color.white;
    private static readonly Color TimerInk = new Color(0.847f, 0.949f, 1f);

    private class Preset
    {
        public string name;
        public TMP_FontAsset font;
        public Color face;
        public float faceDilate;
        public Color outline;
        public float outlineWidth;
        public Color shadow;
        public Vector2 shadowOffset;
        public float shadowSoftness;
        public float shadowDilate;
    }

    [MenuItem("Ludo NX/9. Apply Type System")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[LudoTypeSystem] Stop play mode first — changes would be discarded.");
            return;
        }

        TMP_FontAsset display = FindFont("Baloo2-ExtraBold");
        TMP_FontAsset body = FindFont("Nunito-Bold");

        if (display == null || body == null)
        {
            Debug.LogError("[LudoTypeSystem] Baloo2-ExtraBold or Nunito-Bold TMP asset missing. Run 'Ludo NX/0. Rebuild TMP Font Assets' first.");
            return;
        }

        Directory.CreateDirectory(MatFolder);

        // On the light card panels: a thin light halo lifts the glyph off the gradient without
        // the heavy black outline that would look like a sticker.
        Material cardName = GetMaterial(new Preset
        {
            name = "Text_CardName", font = body, face = InkOnCard, faceDilate = 0.12f,
            outline = new Color(1f, 1f, 1f, 0.55f), outlineWidth = 0.08f,
            shadow = new Color(0f, 0f, 0f, 0.30f), shadowOffset = new Vector2(0.3f, -0.3f),
            shadowSoftness = 0.20f, shadowDilate = 0.05f
        });

        Material cardValue = GetMaterial(new Preset
        {
            name = "Text_CardValue", font = display, face = InkValue, faceDilate = 0.15f,
            outline = new Color(1f, 1f, 1f, 0.45f), outlineWidth = 0.06f,
            shadow = new Color(0f, 0f, 0f, 0.32f), shadowOffset = new Vector2(0.3f, -0.35f),
            shadowSoftness = 0.18f, shadowDilate = 0.05f
        });

        // On the coloured level star and the cyan button: white face, dark outline, real shadow.
        Material onColour = GetMaterial(new Preset
        {
            name = "Text_OnColour", font = display, face = InkOnDark, faceDilate = 0.18f,
            outline = new Color(0.06f, 0.10f, 0.14f, 0.95f), outlineWidth = 0.22f,
            shadow = new Color(0f, 0f, 0f, 0.55f), shadowOffset = new Vector2(0.5f, -0.6f),
            shadowSoftness = 0.30f, shadowDilate = 0.10f
        });

        Material timer = GetMaterial(new Preset
        {
            name = "Text_Timer", font = display, face = TimerInk, faceDilate = 0.14f,
            outline = new Color(0.04f, 0.09f, 0.13f, 0.85f), outlineWidth = 0.18f,
            shadow = new Color(0f, 0.55f, 0.75f, 0.45f), shadowOffset = new Vector2(0f, -0.2f),
            shadowSoftness = 0.55f, shadowDilate = 0.25f    // soft cyan bloom, not a hard shadow
        });

        int touched = 0;

        // --- player cards ---
        foreach (string cardName_ in new[] { "Profile_TopLeft", "Profile_TopRight", "Profile_BottomLeft", "Profile_BottomRight" })
        {
            GameObject card = GameObject.Find(cardName_);
            if (card == null) continue;

            touched += Style(card, "Player_Name", body, cardName, 0.245f, FontStyles.Bold, TextAlignmentOptions.Center);
            touched += Style(card, "WinRate_Value", display, cardValue, 0.215f, FontStyles.Normal, TextAlignmentOptions.Center);
            touched += Style(card, "Points_Value", display, cardValue, 0.215f, FontStyles.Normal, TextAlignmentOptions.Center);
            touched += Style(card, "Level_Text", display, onColour, 0f, FontStyles.Normal, TextAlignmentOptions.Center);
        }

        // --- HUD ---
        touched += StyleByName("Timer_Text", display, timer, FontStyles.Normal);
        touched += StyleRollLabel(display, onColour);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[LudoTypeSystem] Styled {touched} text object(s). Save the scene to keep this.");
    }

    /// <summary>
    /// Finds a descendant by name and applies font, material and size. Size is expressed as a
    /// fraction of the card height so it tracks the card rather than being pinned to pixels.
    /// </summary>
    private static int Style(GameObject root, string childName, TMP_FontAsset font, Material mat,
                             float sizeFraction, FontStyles style, TextAlignmentOptions align)
    {
        TextMeshProUGUI tmp = FindChild(root.transform, childName);
        if (tmp == null) return 0;

        Undo.RecordObject(tmp, "Apply type system");
        tmp.font = font;
        tmp.fontSharedMaterial = mat;
        tmp.fontStyle = style;
        tmp.alignment = align;

        if (sizeFraction > 0f)
        {
            float cardH = root.GetComponent<RectTransform>().sizeDelta.y;
            tmp.fontSize = cardH * sizeFraction;
        }

        tmp.enableAutoSizing = false;
        EditorUtility.SetDirty(tmp);
        return 1;
    }

    private static int StyleByName(string objectName, TMP_FontAsset font, Material mat, FontStyles style)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null) return 0;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) return 0;

        Undo.RecordObject(tmp, "Apply type system");
        tmp.font = font;
        tmp.fontSharedMaterial = mat;
        tmp.fontStyle = style;
        EditorUtility.SetDirty(tmp);
        return 1;
    }

    private static int StyleRollLabel(TMP_FontAsset font, Material mat)
    {
        UIManager ui = Object.FindAnyObjectByType<UIManager>();
        GameObject roll = ui != null && ui.rollButton != null ? ui.rollButton.gameObject : GameObject.Find("Roll_Button");
        if (roll == null) return 0;

        TextMeshProUGUI tmp = roll.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null) return 0;

        Undo.RecordObject(tmp, "Apply type system");
        tmp.font = font;
        tmp.fontSharedMaterial = mat;
        tmp.fontSize = roll.GetComponent<RectTransform>().sizeDelta.y * 0.40f;
        tmp.characterSpacing = 6f;   // a little tracking so a short word fills the pill
        tmp.alignment = TextAlignmentOptions.Center;
        EditorUtility.SetDirty(tmp);
        return 1;
    }

    /// <summary>
    /// Builds (or reuses) a TMP material preset. The three effects that make text read as solid
    /// are face dilate, outline and underlay — all shader properties on the font's own material,
    /// so each preset is a variant of that material rather than a new shader.
    /// </summary>
    private static Material GetMaterial(Preset p)
    {
        string path = $"{MatFolder}/{p.name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(p.font.material);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.shader = p.font.material.shader;
        mat.SetTexture(ShaderUtilities.ID_MainTex, p.font.material.GetTexture(ShaderUtilities.ID_MainTex));

        mat.SetColor(ShaderUtilities.ID_FaceColor, p.face);
        mat.SetFloat(ShaderUtilities.ID_FaceDilate, p.faceDilate);

        mat.SetColor(ShaderUtilities.ID_OutlineColor, p.outline);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, p.outlineWidth);

        mat.SetColor(ShaderUtilities.ID_UnderlayColor, p.shadow);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, p.shadowOffset.x);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, p.shadowOffset.y);
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, p.shadowSoftness);
        mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, p.shadowDilate);

        // Without the keywords the outline and underlay values are ignored entirely.
        mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static TextMeshProUGUI FindChild(Transform root, string name)
    {
        foreach (TextMeshProUGUI t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (t.gameObject.name == name) return t;
        }
        return null;
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
}
