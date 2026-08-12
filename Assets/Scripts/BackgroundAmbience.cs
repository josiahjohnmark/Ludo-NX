using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Controls the cinematic background color transition based on whose turn it is.
/// Creates a soft radial vignette glow in the active player's color,
/// emanating from their corner of the board.
/// </summary>
public class BackgroundAmbience : MonoBehaviour
{
    public static BackgroundAmbience Instance { get; private set; }

    [Header("Background Image (fullscreen overlay)")]
    public Image backgroundImage;

    [Header("Vignette Overlay (optional — for radial glow)")]
    public Image vignetteOverlay;

    [Header("Transition Settings")]
    public float transitionDuration = 1.1f;
    // Vignette fades slightly slower than the base color for a layered, cinematic feel
    // instead of both flatly cross-fading at the same rate.
    public float vignetteDurationMultiplier = 1.35f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Player Turn Colors — Dark Premium Theme")]
    public Color redTurnColor = new Color(0.102f, 0.039f, 0.059f, 1f);     // #1A0A0F
    public Color greenTurnColor = new Color(0.039f, 0.102f, 0.059f, 1f);   // #0A1A0F
    public Color blueTurnColor = new Color(0.039f, 0.059f, 0.102f, 1f);    // #0A0F1A
    public Color yellowTurnColor = new Color(0.102f, 0.090f, 0.039f, 1f);  // #1A170A

    [Header("Base Color")]
    public Color baseColor = new Color(0.051f, 0.067f, 0.090f, 1f);       // #0D1117 (dark navy)

    [Header("Vignette Glow Colors (brighter edge glow)")]
    public Color redGlowColor = new Color(0.176f, 0.063f, 0.082f, 0.8f);     // #2D1015
    public Color greenGlowColor = new Color(0.063f, 0.176f, 0.082f, 0.8f);   // #102D15
    public Color blueGlowColor = new Color(0.063f, 0.082f, 0.176f, 0.8f);    // #10152D
    public Color yellowGlowColor = new Color(0.176f, 0.145f, 0.063f, 0.8f);  // #2D2510

    // Internal
    private Color currentBgColor;
    private Color targetBgColor;
    private Color currentVignetteColor;
    private Color targetVignetteColor;
    private Coroutine transitionCoroutine;
    private GameManager.PlayerColor? activeTargetColor = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentBgColor = baseColor;
        targetBgColor = baseColor;
        currentVignetteColor = Color.clear;
        targetVignetteColor = Color.clear;

        if (backgroundImage != null) backgroundImage.color = baseColor;
        if (vignetteOverlay != null) vignetteOverlay.color = Color.clear;
    }

    /// <summary>
    /// Smoothly transition the background to the active player's color theme.
    /// Called when the turn changes.
    /// </summary>
    public void TransitionToPlayerColor(GameManager.PlayerColor color)
    {
        // Idempotent: both GameManager and UIManager announce turn changes, so a repeat call for
        // the colour we're already heading to must not restart the lerp and cause a visible hitch.
        if (activeTargetColor.HasValue && activeTargetColor.Value == color) return;
        activeTargetColor = color;

        EnsureVignetteSprite();

        targetBgColor = GetBackgroundColor(color);
        targetVignetteColor = GetVignetteColor(color);

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(LerpTransition());
    }

    private IEnumerator LerpTransition()
    {
        Color startBg = currentBgColor;
        Color startVignette = currentVignetteColor;

        float vignetteDuration = transitionDuration * vignetteDurationMultiplier;
        float longestDuration = Mathf.Max(transitionDuration, vignetteDuration);

        float elapsed = 0f;
        while (elapsed < longestDuration)
        {
            elapsed += Time.deltaTime;

            float bgT = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
            float vignetteT = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / vignetteDuration));

            currentBgColor = Color.Lerp(startBg, targetBgColor, bgT);
            currentVignetteColor = Color.Lerp(startVignette, targetVignetteColor, vignetteT);

            if (backgroundImage != null) backgroundImage.color = currentBgColor;
            if (vignetteOverlay != null) vignetteOverlay.color = currentVignetteColor;

            yield return null;
        }

        // Snap to exact values
        currentBgColor = targetBgColor;
        currentVignetteColor = targetVignetteColor;
        if (backgroundImage != null) backgroundImage.color = currentBgColor;
        if (vignetteOverlay != null) vignetteOverlay.color = currentVignetteColor;

        transitionCoroutine = null;
    }

    /// <summary>
    /// Creates the vignette overlay and its radial-gradient sprite if they were never assigned in
    /// the Inspector. Without this the ambience is a flat full-screen tint; with it the active
    /// player's colour falls off from the edges inward, leaving the board centre readable.
    /// </summary>
    private void EnsureVignetteSprite()
    {
        if (vignetteOverlay == null)
        {
            Transform parent = backgroundImage != null ? backgroundImage.transform.parent : transform;
            if (parent == null) return;

            GameObject go = new GameObject("VignetteOverlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            // Sit directly above the flat background, below everything else.
            if (backgroundImage != null)
            {
                go.transform.SetSiblingIndex(backgroundImage.transform.GetSiblingIndex() + 1);
            }
            else
            {
                go.transform.SetAsFirstSibling();
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            vignetteOverlay = go.GetComponent<Image>();
            vignetteOverlay.raycastTarget = false;
            vignetteOverlay.color = currentVignetteColor;
        }

        if (vignetteOverlay.sprite == null)
        {
            vignetteOverlay.sprite = GetRadialGradientSprite();
            vignetteOverlay.type = Image.Type.Simple;
        }
    }

    private static Sprite cachedRadialGradient;

    /// <summary>
    /// Builds a white texture whose alpha ramps from fully transparent at the centre to opaque at
    /// the corners, so tinting the Image produces a coloured edge glow. Cached — one texture is
    /// shared for the lifetime of the app.
    /// </summary>
    private static Sprite GetRadialGradientSprite(int size = 256)
    {
        if (cachedRadialGradient != null) return cachedRadialGradient;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float half = size * 0.5f;
        float maxDist = Mathf.Sqrt(2f);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;

                // Hold the centre clear, then ease outward so the board stays legible.
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - 0.25f) / 0.75f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedRadialGradient = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        cachedRadialGradient.name = "GeneratedRadialVignette";
        return cachedRadialGradient;
    }

    private Color GetBackgroundColor(GameManager.PlayerColor color)
    {
        return color switch
        {
            GameManager.PlayerColor.RED => redTurnColor,
            GameManager.PlayerColor.GREEN => greenTurnColor,
            GameManager.PlayerColor.BLUE => blueTurnColor,
            GameManager.PlayerColor.YELLOW => yellowTurnColor,
            _ => baseColor
        };
    }

    private Color GetVignetteColor(GameManager.PlayerColor color)
    {
        return color switch
        {
            GameManager.PlayerColor.RED => redGlowColor,
            GameManager.PlayerColor.GREEN => greenGlowColor,
            GameManager.PlayerColor.BLUE => blueGlowColor,
            GameManager.PlayerColor.YELLOW => yellowGlowColor,
            _ => Color.clear
        };
    }
}
