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

    private void OnEnable()
    {
        LudoNX.UI.AvatarService.Changed += RefreshIdentity;
        RefreshIdentity();
    }

    private void OnDisable()
    {
        LudoNX.UI.AvatarService.Changed -= RefreshIdentity;
    }

    private void Update()
    {
        // The match can start after this card wakes, so the house the player controls is not
        // known at OnEnable, and it changes between matches. Comparing a cheap signature once a
        // frame keeps the card correct without anything having to remember to notify it.
        RefreshIdentity();
    }

    /// <summary>
    /// Puts the player's own name on the card for the house they are playing.
    ///
    /// SetPlayerName existed but nothing ever called it, so every card kept the literal the
    /// builder authored — "RED", "GREEN" — and the name you set in Edit Profile never appeared
    /// anywhere in a match. The other houses stay named by their colour: they are the computer,
    /// or in pass-and-play other people sharing the device, and giving them your name would be
    /// worse than giving them none.
    /// </summary>
    public void RefreshIdentity()
    {
        var gm = GameManager.Instance;
        if (gm == null || playerNameText == null) return;

        bool mine = gm.humanColor == playerColor && gm.IsHumanControlled(playerColor);
        var session = LudoNXSession.Instance;

        string wanted = mine && session != null && !string.IsNullOrWhiteSpace(session.PlayerName)
                      ? session.PlayerName
                      : playerColor.ToString();

        if (_appliedName != wanted)
        {
            playerNameText.text = wanted;
            _appliedName = wanted;
        }
    }

    private string _appliedName;

    private void Start()
    {
        // Default to inactive
        SetActiveTurn(false);
        UpdatePoints(0, 0);
        UpdateHomeTokens(0);
        SetFireStreak(0);

        // Empty and hidden until this card's turn actually starts; see SetActiveTurn.
        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = 0f;
            timerBarFill.enabled = false;
        }
    }

    /// <summary>
    /// Set this card as the active turn player (glow + full opacity)
    /// or inactive (dimmed).
    /// </summary>
    [Header("Active Turn Feel")]
    [Tooltip("Opacity of the card when it is not this player's turn.")]
    [Range(0.3f, 1f)] public float inactiveAlpha = 0.55f;
    [Tooltip("How much the active card lifts. Kept small on purpose.")]
    public float activeScale = 1.03f;
    [Tooltip("Peak opacity of the halo. Low by design — this is a hint, not an alarm.")]
    [Range(0f, 0.6f)] public float glowMaxAlpha = 0.22f;
    [Range(0f, 0.6f)] public float glowMinAlpha = 0.10f;
    [Tooltip("Seconds for one full breath. Slow reads as cinematic; fast reads as a warning.")]
    public float glowBreathPeriod = 3.2f;
    public float stateBlendSpeed = 6f;

    private Coroutine stateBlendCoroutine;

    // Countdown ring palette. Near-white for most of the turn so it reads as a quiet sweep, then
    // amber, then red only in the last few seconds.
    private static readonly Color TimerCalm = new Color32(0xF2, 0xF6, 0xFA, 0xF0);
    private static readonly Color TimerWarn = new Color32(0xF0, 0xB2, 0x3C, 0xFF);
    private static readonly Color TimerUrgent = new Color32(0xE5, 0x4B, 0x41, 0xFF);

    /// <summary>
    /// Marks this card as the player to move. Deliberately understated: a soft halo that breathes
    /// once every three seconds, a 3% lift, and the inactive cards falling back rather than this
    /// one shouting. No hard pulse.
    /// </summary>
    public void SetActiveTurn(bool active)
    {
        if (isActiveTurn == active && Application.isPlaying) return;
        isActiveTurn = active;

        // A card for a house that is not in this match is switched off, and Unity throws if you
        // start a coroutine on an inactive object. There is nothing to animate either way, so
        // record the state and stop — the card picks it up if it is ever shown again.
        if (!gameObject.activeInHierarchy)
        {
            ApplyStateImmediate(active);
            return;
        }

        if (glowBorder != null)
        {
            glowBorder.gameObject.SetActive(active);

            if (active)
            {
                if (glowPulseCoroutine != null) StopCoroutine(glowPulseCoroutine);
                glowPulseCoroutine = StartCoroutine(BreatheGlow());
            }
            else if (glowPulseCoroutine != null)
            {
                StopCoroutine(glowPulseCoroutine);
                glowPulseCoroutine = null;
            }
        }

        if (!Application.isPlaying)
        {
            ApplyStateImmediate(active);
        }
        else
        {
            if (stateBlendCoroutine != null) StopCoroutine(stateBlendCoroutine);
            stateBlendCoroutine = StartCoroutine(BlendState(active));
        }

        // The bar means "time left in THIS turn", so it only belongs on the card whose turn it
        // is. Leaving it full on the other three drew four bright rails across the HUD that
        // looked like progress meters for something that was not running. Hiding the renderer
        // outright is what makes this reliable — fillAmount alone can be left behind at 1 by
        // whichever code path last touched the card.
        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = active ? 1f : 0f;
            timerBarFill.enabled = active;
        }
    }

    private void ApplyStateImmediate(bool active)
    {
        if (cardCanvasGroup != null) cardCanvasGroup.alpha = active ? 1f : inactiveAlpha;
        transform.localScale = Vector3.one * (active ? activeScale : 1f);
        if (timerBarFill != null) timerBarFill.enabled = active;
        ApplyCardTint(active ? 0f : 1f);
    }

    /// <summary>
    /// Mutes a waiting card by mixing its own colour toward a neutral grey, rather than by
    /// fading it.
    ///
    /// Fading lets the page show through, and the page carries a wash of the current player's
    /// colour — so a faded blue card sitting on a red turn rendered lavender, and green rendered
    /// olive. Since the card's whole job is to say which house it belongs to, that is the one
    /// thing the waiting state must not take away. Mixing toward grey holds the hue at any page
    /// colour while still reading as clearly recessed.
    /// </summary>
    private void ApplyCardTint(float muted)
    {
        var clay = GetComponent<LudoNX.UI.ClayGraphic>();
        if (clay == null) return;

        if (!_baseColourCaptured)
        {
            _baseColour = clay.color;
            _baseColourCaptured = true;
        }

        var grey = new Color(0.62f, 0.60f, 0.60f, _baseColour.a);
        clay.color = Color.Lerp(_baseColour, grey, Mathf.Clamp01(muted) * inactiveMute);
        clay.SetVerticesDirty();
    }

    [Tooltip("How far a waiting card's colour is mixed toward grey. 0 = untouched, 1 = fully grey.")]
    [Range(0f, 1f)] public float inactiveMute = 0.42f;

    private Color _baseColour;
    private bool _baseColourCaptured;
    private float _mute;

    /// <summary>Eases between states so turn changes glide rather than snap.</summary>
    private IEnumerator BlendState(bool active)
    {
        float targetAlpha = active ? 1f : inactiveAlpha;
        float targetScale = active ? activeScale : 1f;

        while (true)
        {
            float step = Time.deltaTime * stateBlendSpeed;
            bool done = true;

            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = Mathf.MoveTowards(cardCanvasGroup.alpha, targetAlpha, step);
                if (!Mathf.Approximately(cardCanvasGroup.alpha, targetAlpha)) done = false;
            }

            float s = Mathf.MoveTowards(transform.localScale.x, targetScale, step * 0.15f);
            transform.localScale = Vector3.one * s;
            if (!Mathf.Approximately(s, targetScale)) done = false;

            _mute = Mathf.MoveTowards(_mute, active ? 0f : 1f, step);
            ApplyCardTint(_mute);
            if (!Mathf.Approximately(_mute, active ? 0f : 1f)) done = false;

            if (done) break;
            yield return null;
        }

        stateBlendCoroutine = null;
    }

    /// <summary>
    /// Update the points display on this card.
    /// </summary>
    public void UpdatePoints(int totalPoints, int delta)
    {
        currentPoints = totalPoints;

        if (pointsText != null)
        {
            // The card art already carries a "POINTS" label and a coin icon, so the value is just
            // the number. Colour is left to the designer-set ink rather than being overridden
            // per-frame — recolouring on every update is what made the card read as noisy.
            pointsText.text = totalPoints.ToString("N0");
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
                // Filled pips carry the player's colour; empty ones are a soft recess in the card
                // rather than a grey dot floating on it.
                homeTokenDots[i].color = (i < tokensHomeCount)
                    ? activeGlowColor
                    : new Color(0f, 0f, 0f, 0.16f);
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
            // "WIN%" is baked into the card art — printing it again read as "WIN WIN% 26%".
            winRateText.text = $"{winPercentage:F0}%";
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
            // Calm by default, urgent only at the end. The old green-to-red traffic light put a
            // bright green ring on the red player's card, which fought the house colours and read
            // as an alarm for the whole turn rather than a countdown.
            // Calm is the player's own colour rather than a fixed near-white. The old #F2F6FA
            // was chosen for a dark HUD; on a white card it made the running bar invisible.
            Color calm = activeGlowColor;

            if (normalizedTime > 0.30f)
            {
                timerBarFill.color = calm;
            }
            else if (normalizedTime > 0.12f)
            {
                timerBarFill.color = Color.Lerp(TimerWarn, calm, (normalizedTime - 0.12f) / 0.18f);
            }
            else
            {
                timerBarFill.color = Color.Lerp(TimerUrgent, TimerWarn, normalizedTime / 0.12f);
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
    private IEnumerator BreatheGlow()
    {
        while (isActiveTurn && glowBorder != null)
        {
            // A slow sine breath between two low alphas. The old version ping-ponged 0.6-1.0 at
            // 2 Hz, which read as a warning light rather than a turn indicator.
            float t = (Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, glowBreathPeriod)) + 1f) * 0.5f;
            Color c = activeGlowColor;
            c.a = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, t);
            glowBorder.color = c;
            yield return null;
        }
    }
}
