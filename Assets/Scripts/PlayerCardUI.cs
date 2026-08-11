using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI component for a single player's card in the game HUD.
/// Attach to each player card panel. Handles avatar, name, points,
/// win rate, home token indicators, turn timer bar, active glow, and fire streak.
/// </summary>
public class PlayerCardUI : MonoBehaviour
{
    [Header("Player Identity")]
    public GameManager.PlayerColor playerColor;

    [Header("UI References")]
    public Image avatarImage;
    public Image avatarBorder;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI pointsText;
    public TextMeshProUGUI winRateText;

    [Header("Home Token Indicators")]
    public Image[] homeTokenDots;  // Array of 4 dot images

    [Header("Turn Timer Bar")]
    public Image timerBarFill;

    [Header("Active Turn Visual")]
    public CanvasGroup cardCanvasGroup;
    public Image glowBorder;

    [Header("Fire Streak")]
    public GameObject fireEffectObject;  // Particle system or animated sprite

    [Header("Color Settings")]
    public Color activeGlowColor = Color.white;
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.6f);

    // Internal state
    private bool isActiveTurn = false;
    private Coroutine glowPulseCoroutine;
    private int currentPoints = 0;
    private int tokensHomeCount = 0;

    private void Start()
    {
        // Default to inactive
        SetActiveTurn(false);
        UpdatePoints(0, 0);
        UpdateHomeTokens(0);
        SetFireStreak(0);

        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = 1f;
        }
    }

    /// <summary>
    /// Set this card as the active turn player (glow + full opacity)
    /// or inactive (dimmed).
    /// </summary>
    public void SetActiveTurn(bool active)
    {
        isActiveTurn = active;

        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.alpha = active ? 1f : 0.6f;
        }

        if (glowBorder != null)
        {
            glowBorder.gameObject.SetActive(active);
            if (active)
            {
                if (glowPulseCoroutine != null) StopCoroutine(glowPulseCoroutine);
                glowPulseCoroutine = StartCoroutine(PulseGlow());
            }
            else
            {
                if (glowPulseCoroutine != null)
                {
                    StopCoroutine(glowPulseCoroutine);
                    glowPulseCoroutine = null;
                }
            }
        }

        // Reset timer bar when becoming active
        if (active && timerBarFill != null)
        {
            timerBarFill.fillAmount = 1f;
        }
    }

    /// <summary>
    /// Update the points display on this card.
    /// </summary>
    public void UpdatePoints(int totalPoints, int delta)
    {
        currentPoints = totalPoints;

        if (pointsText != null)
        {
            if (totalPoints < 0)
            {
                pointsText.text = $"{totalPoints} PTS";
                pointsText.color = new Color(1f, 0.3f, 0.3f); // Red for negative
            }
            else
            {
                pointsText.text = $"{totalPoints} PTS";
                pointsText.color = new Color(1f, 0.85f, 0.2f); // Gold for positive
            }
        }
    }

    /// <summary>
    /// Update the home token dot indicators (filled vs empty).
    /// </summary>
    public void UpdateHomeTokens(int count)
    {
        tokensHomeCount = Mathf.Clamp(count, 0, 4);

        if (homeTokenDots == null) return;

        for (int i = 0; i < homeTokenDots.Length; i++)
        {
            if (homeTokenDots[i] != null)
            {
                // Filled = bright, Empty = dim
                homeTokenDots[i].color = (i < tokensHomeCount)
                    ? activeGlowColor
                    : new Color(0.3f, 0.3f, 0.3f, 0.4f);
            }
        }
    }

    /// <summary>
    /// Update the win rate display.
    /// </summary>
    public void SetWinRate(float winPercentage)
    {
        if (winRateText != null)
        {
            winRateText.text = $"WIN {winPercentage:F0}%";
        }
    }

    /// <summary>
    /// Set the player's display name.
    /// </summary>
    public void SetPlayerName(string name)
    {
        if (playerNameText != null)
        {
            playerNameText.text = name;
        }
    }

    /// <summary>
    /// Update the turn timer bar fill (0 = empty, 1 = full).
    /// </summary>
    public void UpdateTimerBar(float normalizedTime)
    {
        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = Mathf.Clamp01(normalizedTime);

            // Color shifts from green → yellow → red as time runs out
            if (normalizedTime > 0.5f)
            {
                timerBarFill.color = Color.Lerp(Color.yellow, Color.green, (normalizedTime - 0.5f) * 2f);
            }
            else
            {
                timerBarFill.color = Color.Lerp(Color.red, Color.yellow, normalizedTime * 2f);
            }
        }
    }

    /// <summary>
    /// Show/hide the fire streak effect and control its intensity.
    /// </summary>
    public void SetFireStreak(int captureStreak)
    {
        if (fireEffectObject == null) return;

        if (captureStreak >= 2)
        {
            fireEffectObject.SetActive(true);

            // Scale intensity based on streak
            float scale = captureStreak switch
            {
                2 => 0.6f,
                3 => 0.8f,
                _ => 1.0f  // 4+
            };

            fireEffectObject.transform.localScale = Vector3.one * scale;
        }
        else
        {
            fireEffectObject.SetActive(false);
        }
    }

    /// <summary>
    /// Pulsing glow animation for the active turn player.
    /// </summary>
    private IEnumerator PulseGlow()
    {
        while (isActiveTurn && glowBorder != null)
        {
            float pulse = Mathf.PingPong(Time.time * 2f, 0.4f) + 0.6f;
            Color glowColor = activeGlowColor;
            glowColor.a = pulse;
            glowBorder.color = glowColor;
            yield return null;
        }
    }
}
