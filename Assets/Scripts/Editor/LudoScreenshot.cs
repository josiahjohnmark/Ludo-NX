using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using LudoNX.UI;

/// <summary>
/// Renders the game to a PNG at an exact device size, without the Game view and without play mode.
///
/// ScreenCapture.CaptureScreenshot cannot be used from tooling: it writes whatever the Game view
/// presented at the end of a frame, and an unfocused editor stops ticking, so the file is simply
/// never written. That makes it useless for checking a layout from a script.
///
/// This renders the camera into a RenderTexture directly, which the editor will do on demand
/// whether or not it has focus. Screen Space - Overlay canvases never reach a camera, so each one
/// is temporarily switched to Screen Space - Camera for the render and put back afterwards — the
/// scene is left exactly as it was found.
///
/// The camera aspect is forced to the requested device size, so the capture is a true preview of
/// that phone rather than of the editor window's proportions.
/// </summary>
public static class LudoScreenshot
{
    private const string OutDir = "Assets/Screenshots";

    [MenuItem("Ludo NX/12. Capture 1080x2400")]
    public static void Capture20by9() => Capture("preview_1080x2400", 1080, 2400);

    [MenuItem("Ludo NX/12. Capture 1080x1920")]
    public static void Capture16by9() => Capture("preview_1080x1920", 1080, 1920);

    [MenuItem("Ludo NX/Capture Home Screen")]
    public static void CaptureHomeScreen()
    {
        CaptureScreenPrefab("Assets/Resources/UI/Screens/Home.prefab", "screen_home_game_feel", 1080, 1920);
    }

    [MenuItem("Ludo NX/Capture Shop Screen")]
    public static void CaptureShopScreen()
    {
        CaptureScreenPrefab("Assets/Resources/UI/Screens/Shop.prefab", "screen_shop_ash", 1080, 1920);
    }

    [MenuItem("Ludo NX/Capture Rules Modal")]
    public static void CaptureRulesModal()
    {
        CaptureScreenPrefab("Assets/Resources/UI/Modals/Rules.prefab", "rules_modal_1080x2400", 1080, 2400);
    }

    [MenuItem("Ludo NX/Capture Menu Modal")]
    public static void CaptureMenuModal()
    {
        CaptureScreenPrefab("Assets/Resources/UI/Modals/Menu.prefab", "menu_modal_1080x2400", 1080, 2400);
    }

    [MenuItem("Ludo NX/Capture Match HUD (All 4 House Turns)")]
    public static void CaptureMatchHudAllTurns()
    {
        CaptureMatchHudTurn(GameManager.PlayerColor.RED, "match_hud_red_turn_1080x2400");
        CaptureMatchHudTurn(GameManager.PlayerColor.GREEN, "match_hud_green_turn_1080x2400");
        CaptureMatchHudTurn(GameManager.PlayerColor.YELLOW, "match_hud_yellow_turn_1080x2400");
        CaptureMatchHudTurn(GameManager.PlayerColor.BLUE, "match_hud_blue_turn_1080x2400");
    }

    public static string CaptureMatchHudTurn(GameManager.PlayerColor turnHouse, string outputName)
    {
        string prefabPath = "Assets/Resources/UI/Components/MatchHud.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[LudoScreenshot] Prefab not found: {prefabPath}");
            return null;
        }

        var camGo = new GameObject("TempCam", typeof(Camera));
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        int width = 1080;
        int height = 2400;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        var instance = Object.Instantiate(prefab);
        var canvas = instance.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
        }
        var scaler = instance.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(440, 956);
            scaler.matchWidthOrHeight = 0f;
        }

        var view = instance.GetComponent<MatchHudView>();
        if (view != null)
        {
            Color tint = turnHouse switch
            {
                GameManager.PlayerColor.RED => new Color(0.906f, 0.216f, 0.208f, 0.48f),
                GameManager.PlayerColor.GREEN => new Color(0.302f, 0.682f, 0.318f, 0.45f),
                GameManager.PlayerColor.YELLOW => new Color(0.984f, 0.753f, 0.169f, 0.39f),
                _ => new Color(0.106f, 0.537f, 0.906f, 0.51f),
            };
            if (view.houseTintStrips != null)
                foreach (var strip in view.houseTintStrips)
                    if (strip != null) strip.color = tint;
            view.UpdateTintHole();

            if (view.cards != null)
            {
                string[] names = { "YOU", "ALEX", "SARAH", "LEO" };
                int[] levels = { 5, 3, 7, 2 };
                string[] points = { "3,450", "1,820", "4,100", "950" };

                for (int i = 0; i < view.cards.Length; i++)
                {
                    if (view.cards[i] != null)
                    {
                        var card = view.cards[i];
                        bool isTurn = card.house == turnHouse;
                        card.ApplyHouse(card.house, isBot: i > 0);
                        if (card.nameLabel != null) card.nameLabel.text = names[i % names.Length];
                        if (card.levelLabel != null) card.levelLabel.text = $"LEVEL {levels[i % levels.Length]}";
                        if (card.nxPointsLabel != null) card.nxPointsLabel.text = points[i % points.Length];
                        card.SetActive(isTurn, 0f);
                    }
                }
            }

            if (view.turnLabel != null) view.turnLabel.text = $"{turnHouse}'S TURN";
            if (view.rollSubLabel != null)
            {
                view.rollSubLabel.text = (turnHouse == GameManager.PlayerColor.RED) ? "TAP TO ROLL" : $"{turnHouse}'S TURN";
            }
        }

        Canvas.ForceUpdateCanvases();
        foreach (var band in instance.GetComponentsInChildren<DesignBand>(true))
        {
            band.Apply(force: true);
        }
        Canvas.ForceUpdateCanvases();

        cam.Render();

        RenderTexture.active = rt;
        Texture2D shot = new Texture2D(width, height, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        shot.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(instance);

        Directory.CreateDirectory(OutDir);
        string outPath = $"{OutDir}/{outputName}.png";
        File.WriteAllBytes(outPath, shot.EncodeToPNG());
        Object.DestroyImmediate(shot);

        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[LudoScreenshot] Captured Match HUD turn to {outPath}");
        return outPath;
    }

    public static string CaptureScreenPrefab(string prefabPath, string outputName, int width, int height)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[LudoScreenshot] Prefab not found: {prefabPath}");
            return null;
        }

        var camGo = new GameObject("TempCam", typeof(Camera));
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        var canvasGo = new GameObject("CaptureCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        var instance = Object.Instantiate(prefab);
        instance.transform.SetParent(canvasGo.transform, false);
        var rtInstance = instance.GetComponent<RectTransform>();
        if (rtInstance != null)
        {
            rtInstance.anchorMin = Vector2.zero;
            rtInstance.anchorMax = Vector2.one;
            rtInstance.offsetMin = Vector2.zero;
            rtInstance.offsetMax = Vector2.zero;
        }

        Canvas.ForceUpdateCanvases();

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D shot = new Texture2D(width, height, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        shot.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(canvasGo);
        Object.DestroyImmediate(instance);

        Directory.CreateDirectory(OutDir);
        string outPath = $"{OutDir}/{outputName}.png";
        File.WriteAllBytes(outPath, shot.EncodeToPNG());
        Object.DestroyImmediate(shot);

        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[LudoScreenshot] Captured screen prefab to {outPath}");
        return outPath;
    }

    /// <summary>Renders to Assets/Screenshots/&lt;name&gt;.png and returns the asset path.</summary>
    public static string Capture(string name, int width, int height)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[LudoScreenshot] No main camera.");
            return null;
        }

        Directory.CreateDirectory(OutDir);
        string path = $"{OutDir}/{name}.png";

        // ── force the requested device shape ──
        bool hadAspect = true;
        float prevAspect = cam.aspect;
        cam.aspect = width / (float)height;

        // Let the board framer and HUD re-fit for this exact device before we photograph it.
        BoardCameraFramer framer = Object.FindAnyObjectByType<BoardCameraFramer>();
        if (framer != null) framer.Frame();

        MatchHudLayout hud = Object.FindAnyObjectByType<MatchHudLayout>();
        if (hud != null && hud.isActiveAndEnabled) hud.Apply();

        // ── overlay canvases have to go through the camera to be captured ──
        List<(Canvas canvas, RenderMode mode, Camera worldCam, float plane)> restore =
            new List<(Canvas, RenderMode, Camera, float)>();

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1
        };

        RenderTexture prevTarget = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;
        Texture2D shot = null;

        try
        {
            cam.targetTexture = rt;

            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                restore.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 10f;
            }

            Canvas.ForceUpdateCanvases();
            foreach (var band in Object.FindObjectsByType<DesignBand>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                band.Apply(force: true);
            }
            Canvas.ForceUpdateCanvases();
            if (framer != null) framer.Frame();

            cam.Render();

            RenderTexture.active = rt;
            shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();
        }
        finally
        {
            // Restore in a finally so a render failure cannot leave the scene rewired.
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            if (hadAspect) cam.aspect = prevAspect;

            foreach (var r in restore)
            {
                r.canvas.renderMode = r.mode;
                r.canvas.worldCamera = r.worldCam;
                r.canvas.planeDistance = r.plane;
            }
            Canvas.ForceUpdateCanvases();
        }

        if (shot != null)
        {
            File.WriteAllBytes(path, shot.EncodeToPNG());
            Object.DestroyImmediate(shot);
        }

        rt.Release();
        Object.DestroyImmediate(rt);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[LudoScreenshot] Wrote {path} at {width}x{height}.");
        return path;
    }
}
