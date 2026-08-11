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
    public float transitionDuration = 0.6f;

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
        targetBgColor = GetBackgroundColor(color);
        targetVignetteColor = GetVignetteColor(color);

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(LerpTransition());
    }

    private IEnumerator LerpTransition()
    {
        Color startBg = currentBgColor;
        Color startVignette = currentVignetteColor;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            // Smooth ease-in-out
            float smooth = t * t * (3f - 2f * t);

            currentBgColor = Color.Lerp(startBg, targetBgColor, smooth);
            currentVignetteColor = Color.Lerp(startVignette, targetVignetteColor, smooth);

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
