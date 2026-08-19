using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Makes the dice read as real dice: bigger, with rounded edges, sitting in their own tray.
///
/// The body mesh is replaced with a generated rounded cube. That is safe here because the pips are
/// six separate Face_N quads parented to the die at ±0.5 — rounding only pulls the edges and
/// corners inward, leaving the flat centre of each face exactly where it was, so the pips stay
/// flush and correctly oriented.
/// </summary>
public static class LudoDiceKit
{
    private const string MeshPath = "Assets/Art/Generated/RoundedDie.asset";
    private const string TrayPath = "Assets/Art/Generated/dice_tray.png";

    // Tuning
    private const float DieScale = 0.95f;        // was 0.5 — far too small for the primary control
    private const float DieSpacing = 0.72f;      // half-distance between the two dice
    // Sitting level with the player cards squeezed the dice into the gap between them. Dropping
    // the row puts them in the clear band between the cards and the roll button.
    private const float DiceRowY = -10.75f;
    private const float CornerRadius = 0.13f;    // as a fraction of the 1-unit cube
    private const int Subdivisions = 8;          // per face, per axis

    [MenuItem("Ludo NX/8. Build Dice & Tray")]
    public static void BuildDice()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[LudoDiceKit] Stop play mode first — scene edits are discarded on exit.");
            return;
        }

        Mesh rounded = GetRoundedDieMesh();
        Sprite tray = GetTraySprite();

        GameObject d1 = GameObject.Find("Dice_3D_1");
        GameObject d2 = GameObject.Find("Dice_3D_2");
        if (d1 == null || d2 == null)
        {
            Debug.LogError("[LudoDiceKit] Dice_3D_1 / Dice_3D_2 not found.");
            return;
        }

        // Keep the dice on the row they already occupy; only widen the gap and grow them.
        float y = DiceRowY;
        float z = d1.transform.position.z;
        float cx = 0f;   // centred under the board

        ApplyDie(d1, rounded, new Vector3(cx - DieSpacing, y, z));
        ApplyDie(d2, rounded, new Vector3(cx + DieSpacing, y, z));

        BuildTray(tray, new Vector3(cx, y, z + 0.9f));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[LudoDiceKit] Dice rebuilt at scale {DieScale} with a rounded mesh and tray. Save the scene.");
    }

    private static void ApplyDie(GameObject die, Mesh mesh, Vector3 position)
    {
        MeshFilter mf = die.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = mesh;

        die.transform.position = position;
        die.transform.localScale = Vector3.one * DieScale;

        // The collider should follow the visual, not the old unit cube.
        BoxCollider box = die.GetComponent<BoxCollider>();
        if (box != null) box.size = Vector3.one;

        EditorUtility.SetDirty(die);
    }

    private static void BuildTray(Sprite sprite, Vector3 position)
    {
        GameObject tray = GameObject.Find("Dice_Tray");
        if (tray == null)
        {
            tray = new GameObject("Dice_Tray");
            tray.AddComponent<SpriteRenderer>();
        }

        SpriteRenderer sr = tray.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        // Must match the Canvas order (0), not sit below it. The HUD canvas carries a full-screen
        // opaque Background_Image, so anything at a lower sorting order is painted over entirely —
        // which is why an order of -50 made the tray invisible. At order 0, depth decides: the tray
        // sits in front of the canvas plane and behind the dice.
        sr.sortingOrder = 0;

        tray.transform.position = position;        // z pushed back so the dice sit in front
        tray.transform.localScale = Vector3.one;

        EditorUtility.SetDirty(tray);
    }

    // ---------- rounded cube ----------

    private static Mesh GetRoundedDieMesh()
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (existing != null) return existing;

        Mesh mesh = GenerateRoundedCube(CornerRadius, Subdivisions);
        mesh.name = "RoundedDie";

        Directory.CreateDirectory(Path.GetDirectoryName(MeshPath));
        AssetDatabase.CreateAsset(mesh, MeshPath);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    /// <summary>
    /// A unit cube whose edges and corners are pulled in toward a rounded profile.
    ///
    /// For each vertex the position is clamped into an inner cube of half-extent (0.5 - r), then
    /// pushed back out by r along the direction from that clamped point. Face interiors are
    /// untouched (the clamp only moves the one axis that is already at the surface), edges bulge
    /// on two axes and corners on three — which is exactly the profile of a real die.
    /// </summary>
    private static Mesh GenerateRoundedCube(float radius, int subdivisions)
    {
        int n = Mathf.Max(2, subdivisions);
        float h = 0.5f - radius;

        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();

        // Six faces, each a grid built from its own axis basis.
        Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

        foreach (Vector3 normal in normals)
        {
            Vector3 axisU = new Vector3(normal.y, normal.z, normal.x);
            Vector3 axisV = Vector3.Cross(normal, axisU);
            int baseIndex = verts.Count;

            for (int iv = 0; iv <= n; iv++)
            {
                for (int iu = 0; iu <= n; iu++)
                {
                    float u = iu / (float)n - 0.5f;
                    float v = iv / (float)n - 0.5f;
                    Vector3 p = normal * 0.5f + axisU * u + axisV * v;

                    Vector3 clamped = new Vector3(
                        Mathf.Clamp(p.x, -h, h),
                        Mathf.Clamp(p.y, -h, h),
                        Mathf.Clamp(p.z, -h, h));

                    Vector3 offset = p - clamped;
                    verts.Add(offset.sqrMagnitude > 1e-8f
                        ? clamped + offset.normalized * radius
                        : p);
                }
            }

            for (int iv = 0; iv < n; iv++)
            {
                for (int iu = 0; iu < n; iu++)
                {
                    int a = baseIndex + iv * (n + 1) + iu;
                    int b = a + 1;
                    int c = a + (n + 1);
                    int d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ---------- tray ----------

    private static Sprite GetTraySprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(TrayPath);
        if (existing != null) return existing;

        const int w = 512, hgt = 256;
        const float corner = 62f;
        const float border = 11f;

        Texture2D tex = new Texture2D(w, hgt, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * hgt];

        Color fill = new Color(0.10f, 0.135f, 0.175f, 0.92f);    // recessed well, light enough to read
        Color rim = new Color(0.35f, 0.76f, 0.87f, 1f);          // cyan rim, ties to the roll button

        for (int y = 0; y < hgt; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = RoundedRectDistance(x + 0.5f, y + 0.5f, w, hgt, corner);

                Color c;
                if (d > 1.5f) c = Color.clear;
                else if (d > 0f) c = new Color(rim.r, rim.g, rim.b, rim.a * (1f - d / 1.5f));
                else if (d > -border) c = rim;
                else
                {
                    // Soften just inside the rim so it reads as a lip rather than a flat outline.
                    float t = Mathf.Clamp01((-d - border) / 10f);
                    c = Color.Lerp(rim, fill, t);
                }
                px[y * w + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(TrayPath));
        File.WriteAllBytes(TrayPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(TrayPath, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter imp = AssetImporter.GetAtPath(TrayPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.spritePixelsPerUnit = 160f;   // ~3.2 x 1.6 world units
            TextureImporterSettings s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(TrayPath);
    }

    /// <summary>Signed distance to a rounded rectangle: negative inside, positive outside.</summary>
    private static float RoundedRectDistance(float x, float y, float w, float h, float r)
    {
        float dx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r);
        float dy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
        float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
        float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
        return outside + inside - r;
    }
}
