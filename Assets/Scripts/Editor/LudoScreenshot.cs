using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

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
        if (hud != null) hud.ApplyFor(width, height);

        // ── overlay canvases have to go through the camera to be captured ──
        List<(Canvas canvas, RenderMode mode, Camera worldCam, float plane)> restore =
            new List<(Canvas, RenderMode, Camera, float)>();

        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            restore.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = 1f;
        }

        Canvas.ForceUpdateCanvases();

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
