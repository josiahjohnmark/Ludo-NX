using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Turn Indicator UI")]
    public TextMeshProUGUI turnStatusText;
    public Image turnBannerHeader;

    [Header("Player Cards (assign one per corner)")]
    public PlayerCardUI redPlayerCard;
    public PlayerCardUI greenPlayerCard;
    public PlayerCardUI bluePlayerCard;
    public PlayerCardUI yellowPlayerCard;

    [Header("Player Colors")]
    public Color redColor = new Color(0.85f, 0.2f, 0.2f);
    public Color greenColor = new Color(0.2f, 0.75f, 0.3f);
    public Color yellowColor = new Color(0.95f, 0.8f, 0.2f);
    public Color blueColor = new Color(0.2f, 0.5f, 0.9f);

    [Header("Top Bar")]
    public Button quitButton;
    public Button soundToggleButton;
    public Image soundOnIcon;
    public Image soundOffIcon;

    [Header("Turn Timer (Center Top Bar)")]
    public TextMeshProUGUI timerText;
    public float turnDuration = 15f;

    [Header("Roll Button")]
    public Button rollButton;
    public Image rollButtonImage;
    public TextMeshProUGUI rollButtonText;
    public Color rollButtonActiveColor = new Color(0.96f, 0.65f, 0.14f);     // #F5A623
    public Color rollButtonDisabledColor = new Color(0.4f, 0.4f, 0.4f);

    [Header("Setup Menu Panel")]
    public GameObject playerSetupPanel;

    [Header("Victory / Leaderboard Panel")]
    public GameObject victoryPanel;
    public TextMeshProUGUI leaderboardText;

    [Header("Post-Match Stats Panel")]
    public GameObject statsPanel;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI mvpText;

    [Header("Quit Confirmation Popup")]
    public GameObject quitConfirmPanel;

    [Header("Chat / Emoji Placeholders")]
    public Button chatButton;
    public Button emojiButton;
    public GameObject chatPanel;      // Placeholder panel
    public GameObject emojiPanel;     // Placeholder panel

    // Internal state
    private bool isSoundOn = true;
    private float turnTimeRemaining;
    private bool isTurnTimerActive = false;
    private Coroutine turnTimerCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
        if (chatPanel != null) chatPanel.SetActive(false);
        if (emojiPanel != null) emojiPanel.SetActive(false);

        // Wire up button listeners
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        if (soundToggleButton != null) soundToggleButton.onClick.AddListener(OnSoundToggleClicked);
        if (rollButton != null) rollButton.onClick.AddListener(OnRollClicked);
        if (chatButton != null) chatButton.onClick.AddListener(OnChatClicked);
        if (emojiButton != null) emojiButton.onClick.AddListener(OnEmojiClicked);

        UpdateSoundIcons();
    }

    // ===========================
    // TURN UI
    // ===========================

    public void UpdateTurnUI(GameManager.PlayerColor color, string actionMessage = "")
    {
        if (turnStatusText == null) return;

        Color bannerColor = GetColorForPlayer(color);

        if (turnBannerHeader != null)
        {
            turnBannerHeader.color = bannerColor;
        }

        if (string.IsNullOrEmpty(actionMessage))
        {
            turnStatusText.text = color.ToString() + "'S TURN";
        }
        else
        {
            turnStatusText.text = color.ToString() + ": " + actionMessage;
        }

        // Update player cards — activate current, dim others
        SetActivePlayerCard(color);

        // Transition background ambience
        if (BackgroundAmbience.Instance != null)
        {
            BackgroundAmbience.Instance.TransitionToPlayerColor(color);
        }

        // Update roll button state
        SetRollButtonEnabled(actionMessage.Contains("ROLL"));
    }

    /// <summary>
    /// Called on turn change to update all player cards — glow active, dim inactive.
    /// </summary>
    private void SetActivePlayerCard(GameManager.PlayerColor activeColor)
    {
        PlayerCardUI[] cards = { redPlayerCard, greenPlayerCard, bluePlayerCard, yellowPlayerCard };
        GameManager.PlayerColor[] colors = {
            GameManager.PlayerColor.RED,
            GameManager.PlayerColor.GREEN,
            GameManager.PlayerColor.BLUE,
            GameManager.PlayerColor.YELLOW
        };

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null)
            {
                cards[i].SetActiveTurn(colors[i] == activeColor);
            }
        }
    }

    // ===========================
    // POINTS DISPLAY
    // ===========================

    /// <summary>
    /// Called by PointsManager when a player's points change.
    /// Updates the player card and spawns a floating text.
    /// </summary>
    public void OnPointsChanged(GameManager.PlayerColor color, int totalPoints, int delta)
    {
        PlayerCardUI card = GetPlayerCard(color);
        if (card != null)
        {
            card.UpdatePoints(totalPoints, delta);

            // Update fire streak
            if (PointsManager.Instance != null)
            {
                int streak = PointsManager.Instance.GetFireStreak(color);
                card.SetFireStreak(streak);

                // Update home token dots
                int homes = PointsManager.Instance.GetTokensHome(color);
                card.UpdateHomeTokens(homes);
            }
        }
    }

    /// <summary>
    /// Update a player card's home token count (called when a token reaches home).
    /// </summary>
    public void UpdateHomeTokenDisplay(GameManager.PlayerColor color, int count)
    {
        PlayerCardUI card = GetPlayerCard(color);
        if (card != null)
        {
            card.UpdateHomeTokens(count);
        }
    }

    // ===========================
    // TURN TIMER
    // ===========================

    /// <summary>
    /// Start the per-turn countdown timer.
    /// </summary>
    public void StartTurnTimer()
    {
        if (turnTimerCoroutine != null) StopCoroutine(turnTimerCoroutine);
        turnTimerCoroutine = StartCoroutine(TurnTimerRoutine());
    }

    /// <summary>
    /// Stop the turn timer (called when the player makes a move).
    /// </summary>
    public void StopTurnTimer()
    {
        isTurnTimerActive = false;
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }
    }

    private IEnumerator TurnTimerRoutine()
    {
        turnTimeRemaining = turnDuration;
        isTurnTimerActive = true;

        GameManager.PlayerColor timerForColor = GameManager.Instance != null
            ? GameManager.Instance.currentTurnColor
            : GameManager.PlayerColor.RED;

        while (turnTimeRemaining > 0f && isTurnTimerActive)
        {
            turnTimeRemaining -= Time.deltaTime;

            // Update timer text (center top bar)
            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(Mathf.Max(0, turnTimeRemaining));
                timerText.text = seconds.ToString();

                // Color: green → yellow → red
                float normalized = turnTimeRemaining / turnDuration;
                if (normalized > 0.5f)
                    timerText.color = Color.Lerp(Color.yellow, Color.green, (normalized - 0.5f) * 2f);
                else
                    timerText.color = Color.Lerp(Color.red, Color.yellow, normalized * 2f);
            }

            // Update active player card timer bar
            PlayerCardUI activeCard = GetPlayerCard(timerForColor);
            if (activeCard != null)
            {
                activeCard.UpdateTimerBar(turnTimeRemaining / turnDuration);
            }

            yield return null;
        }

        // Timer expired — auto-play
        if (isTurnTimerActive && GameManager.Instance != null)
        {
            Debug.Log($"[UIManager] Turn timer expired for {timerForColor}! Auto-playing...");

            if (AutoPlayAI.Instance != null)
            {
                bool moved = AutoPlayAI.Instance.AutoPlayBestMove(
                    timerForColor,
                    GameManager.Instance.dice1Value,
                    GameManager.Instance.dice2Value
                );

                if (!moved)
                {
                    // No legal moves — force pass
                    GameManager.Instance.ResetDiceState();
                    GameManager.Instance.AdvanceToNextPlayer();
                }
            }
            else
            {
                // No AI available — just pass
                GameManager.Instance.ResetDiceState();
                GameManager.Instance.AdvanceToNextPlayer();
            }
        }

        isTurnTimerActive = false;
        turnTimerCoroutine = null;
    }

    // ===========================
    // ROLL BUTTON
    // ===========================

    private void OnRollClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRollButtonClicked();
        }
    }

    public void SetRollButtonEnabled(bool enabled)
    {
        if (rollButton != null)
        {
            rollButton.interactable = enabled;
        }

        if (rollButtonImage != null)
        {
            rollButtonImage.color = enabled ? rollButtonActiveColor : rollButtonDisabledColor;
        }
    }

    // ===========================
    // TOP BAR — QUIT & SOUND
    // ===========================

    private void OnQuitClicked()
    {
        // Show quit confirmation popup instead of quitting immediately
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(true);
        }
    }

    public void OnQuitConfirmed()
    {
        // For now, just reload scene or return to menu
        Debug.Log("[UIManager] Quit confirmed — returning to menu.");
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void OnQuitCancelled()
    {
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }
    }

    private void OnSoundToggleClicked()
    {
        isSoundOn = !isSoundOn;
        AudioListener.volume = isSoundOn ? 1f : 0f;
        UpdateSoundIcons();
        Debug.Log($"[UIManager] Sound toggled: {(isSoundOn ? "ON" : "OFF")}");
    }

    private void UpdateSoundIcons()
    {
        if (soundOnIcon != null) soundOnIcon.gameObject.SetActive(isSoundOn);
        if (soundOffIcon != null) soundOffIcon.gameObject.SetActive(!isSoundOn);
    }

    // ===========================
    // CHAT & EMOJI (Placeholders)
    // ===========================

    private void OnChatClicked()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(!chatPanel.activeSelf);
        }
        if (emojiPanel != null) emojiPanel.SetActive(false);
    }

    private void OnEmojiClicked()
    {
        if (emojiPanel != null)
        {
            emojiPanel.SetActive(!emojiPanel.activeSelf);
        }
        if (chatPanel != null) chatPanel.SetActive(false);
    }

    // ===========================
    // GAME OVER / STATS
    // ===========================

    public void ShowGameOverUI(List<GameManager.PlayerColor> leaderBoard)
    {
        if (victoryPanel == null) return;

        StopTurnTimer();
        victoryPanel.SetActive(true);

        if (leaderboardText != null)
        {
            string summary = "FINAL PLACEMENTS\n\n";
            for (int i = 0; i < leaderBoard.Count; i++)
            {
                string rankSuffix = (i == 0) ? "1st Place 🏆" : ((i == 1) ? "2nd Place 🥈" : ((i == 2) ? "3rd Place 🥉" : "4th Place"));
                summary += rankSuffix + ": " + leaderBoard[i].ToString();

                // Show points
                if (PointsManager.Instance != null)
                {
                    int pts = PointsManager.Instance.GetPoints(leaderBoard[i]);
                    summary += $" — {pts} PTS";
                }

                summary += "\n";
            }

            // MVP
            if (PointsManager.Instance != null)
            {
                GameManager.PlayerColor mvp = PointsManager.Instance.DetermineMVP();
                summary += $"\n⭐ MVP: {mvp}";
            }

            leaderboardText.text = summary;
        }
    }

    public void ShowPostMatchStats()
    {
        if (statsPanel == null || PointsManager.Instance == null) return;

        statsPanel.SetActive(true);

        if (statsText != null)
        {
            string stats = "MATCH STATS\n\n";
            GameManager.PlayerColor[] colors = {
                GameManager.PlayerColor.RED,
                GameManager.PlayerColor.GREEN,
                GameManager.PlayerColor.BLUE,
                GameManager.PlayerColor.YELLOW
            };

            stats += "Player    | Captures | Homes | Doubles | 🔥 Fire | Captured\n";
            stats += "----------|----------|-------|---------|---------|--------\n";

            foreach (var color in colors)
            {
                var s = PointsManager.Instance.GetStats(color);
                if (s != null)
                {
                    stats += $"{color,-10}| {s.captures,-8} | {s.tokensHome,-5} | {s.doublesRolled,-7} | {s.fireStreakMax,-7} | {s.timesCaptured}\n";
                }
            }

            // Luckiest roller
            var luckiest = PointsManager.Instance.GetLuckiestRoller();
            stats += $"\n🎲 Luckiest Roller: {luckiest}";

            statsText.text = stats;
        }

        // MVP display
        if (mvpText != null)
        {
            var mvp = PointsManager.Instance.DetermineMVP();
            int mvpPts = PointsManager.Instance.GetPoints(mvp);
            mvpText.text = $"⭐ MVP: {mvp} — {mvpPts} PTS";
        }
    }

    public void OnSelectPlayerCount(int playerCount)
    {
        if (playerSetupPanel != null) playerSetupPanel.SetActive(false);
        GameManager.Instance.InitializeMatchWithPlayerCount(playerCount);
        UpdateTurnUI(GameManager.Instance.currentTurnColor, "ROLL DICE");
    }

    public void OnClickRestartMatch()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        if (playerSetupPanel != null) playerSetupPanel.SetActive(true);
    }

    // ===========================
    // HELPERS
    // ===========================

    public PlayerCardUI GetPlayerCard(GameManager.PlayerColor color)
    {
        return color switch
        {
            GameManager.PlayerColor.RED => redPlayerCard,
            GameManager.PlayerColor.GREEN => greenPlayerCard,
            GameManager.PlayerColor.BLUE => bluePlayerCard,
            GameManager.PlayerColor.YELLOW => yellowPlayerCard,
            _ => null
        };
    }

    private Color GetColorForPlayer(GameManager.PlayerColor color)
    {
        return color switch
        {
            GameManager.PlayerColor.RED => redColor,
            GameManager.PlayerColor.GREEN => greenColor,
            GameManager.PlayerColor.YELLOW => yellowColor,
            GameManager.PlayerColor.BLUE => blueColor,
            _ => Color.white
        };
    }
}