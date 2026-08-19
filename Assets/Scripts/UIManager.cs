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
    // 60s, shown as MM:SS, matching the 01:00 countdown in the board UI sketch. Animation time is
    // paused out of this, so it's 60s of actual thinking time.
    public float turnDuration = 60f;
    // Read against the light timer pill, not the old dark bar — mid-green on near-white was
    // effectively invisible. Deep tones keep the same safe/warn/danger reading with real contrast.
    public Color timerSafeColor = new Color(0.13f, 0.15f, 0.18f);
    public Color timerWarnColor = new Color(0.72f, 0.45f, 0.02f);
    public Color timerDangerColor = new Color(0.78f, 0.14f, 0.11f);

    [Header("Roll Button")]
    public Button rollButton;
    public Image rollButtonImage;
    public TextMeshProUGUI rollButtonText;
    // The scene currently has a second, duplicate Roll_Button (in BottomBar_Panel) that isn't
    // wired to UIManager at all, so it never reflects enabled/disabled state. Assign it (and any
    // other stray roll buttons) here so its visuals stay in sync until the scene duplicate is
    // cleaned up.
    public Button[] extraRollButtons;
    // These multiply the sprite. Now that the button carries its own artwork, the enabled tint
    // must be white or it stains the art — an orange tint over the cyan pill turned it olive.
    // Disabled just drains it rather than recolouring it.
    public Color rollButtonActiveColor = Color.white;
    public Color rollButtonDisabledColor = new Color(0.55f, 0.58f, 0.60f, 0.75f);

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
    private bool isTurnTimerPaused = false;
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
        if (extraRollButtons != null)
        {
            foreach (Button extra in extraRollButtons)
            {
                if (extra != null) extra.onClick.AddListener(OnRollClicked);
            }
        }
        if (chatButton != null) chatButton.onClick.AddListener(OnChatClicked);
        if (emojiButton != null) emojiButton.onClick.AddListener(OnEmojiClicked);

        UpdateSoundIcons();

        StartCoroutine(LiveCardRefreshRoutine());
    }

    // ===========================
    // TURN UI
    // ===========================

    /// <summary>
    /// Drives every per-turn visual: banner text/colour, player card highlighting, background
    /// ambience and roll-button state. Each concern is guarded independently on purpose — an
    /// unassigned Inspector reference must only disable its own piece, never silently take down
    /// the other three (which is exactly what a single early-return here used to do).
    /// </summary>
    public void UpdateTurnUI(GameManager.PlayerColor color, string actionMessage = "")
    {
        Color bannerColor = GetColorForPlayer(color);

        if (turnBannerHeader != null)
        {
            turnBannerHeader.color = bannerColor;
        }

        if (turnStatusText != null)
        {
            turnStatusText.text = string.IsNullOrEmpty(actionMessage)
                ? color.ToString() + "'S TURN"
                : color.ToString() + ": " + actionMessage;
        }

        // Update player cards — activate current, dim others
        SetActivePlayerCard(color);

        // Transition background ambience
        if (BackgroundAmbience.Instance != null)
        {
            BackgroundAmbience.Instance.TransitionToPlayerColor(color);
        }

        // Update roll button state
        SetRollButtonEnabled(actionMessage != null && actionMessage.Contains("ROLL"));
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
        isTurnTimerPaused = false;
        turnTimerCoroutine = StartCoroutine(TurnTimerRoutine());
    }

    /// <summary>
    /// Stop the turn timer outright. Called on game over and whenever the turn changes hands.
    /// </summary>
    public void StopTurnTimer()
    {
        isTurnTimerActive = false;
        isTurnTimerPaused = false;
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }
    }

    /// <summary>
    /// Freeze the countdown without ending it — used while the dice are spinning and while a
    /// token is hopping, so animation time is never charged against the player's thinking time.
    /// </summary>
    public void PauseTurnTimer() => isTurnTimerPaused = true;

    public void ResumeTurnTimer() => isTurnTimerPaused = false;

    private IEnumerator TurnTimerRoutine()
    {
        turnTimeRemaining = turnDuration;
        isTurnTimerActive = true;

        GameManager gm = GameManager.Instance;
        GameManager.PlayerColor timerForColor = gm != null ? gm.currentTurnColor : GameManager.PlayerColor.RED;
        int mySequence = gm != null ? gm.TurnSequence : 0;

        while (turnTimeRemaining > 0f && isTurnTimerActive)
        {
            // Abandon silently if the turn moved on while we were counting.
            if (gm != null && gm.TurnSequence != mySequence)
            {
                isTurnTimerActive = false;
                turnTimerCoroutine = null;
                yield break;
            }

            if (!isTurnTimerPaused)
            {
                turnTimeRemaining -= Time.deltaTime;
            }

            RenderTimer(turnTimeRemaining, timerForColor);
            yield return null;
        }

        // Timer expired.
        if (isTurnTimerActive && gm != null && gm.TurnSequence == mySequence)
        {
            bool canAct = gm.currentPhase == GameManager.TurnPhase.AwaitingRoll
                       || gm.currentPhase == GameManager.TurnPhase.AwaitingSelection;

            if (!canAct)
            {
                // Mid-animation. Restart the clock rather than firing into a running move — this
                // was the race that let two tokens move on one die.
                isTurnTimerActive = false;
                turnTimerCoroutine = null;
                StartTurnTimer();
                yield break;
            }

            // A human who runs out of time FORFEITS the turn — the AI does not step in and play
            // it for them. Having the computer make your move is worse than losing the turn:
            // it moves a token you did not choose and the board changes while you are still
            // deciding. AI-controlled houses keep auto-playing, since that IS their turn.
            bool humanControlled = gm.IsHumanControlled(timerForColor);

            if (humanControlled)
            {
                Debug.Log($"[UIManager] Turn timer expired for {timerForColor}. Skipping their turn.");
                UpdateTurnUI(timerForColor, "TIME UP — TURN SKIPPED");
                gm.ResetDiceState();
                gm.AdvanceToNextPlayer();
            }
            else
            {
                Debug.Log($"[UIManager] Turn timer expired for AI {timerForColor}. Auto-playing.");

                if (gm.currentPhase == GameManager.TurnPhase.AwaitingRoll)
                {
                    isTurnTimerActive = false;
                    turnTimerCoroutine = null;
                    gm.OnRollButtonClicked();
                    yield break;
                }

                bool moved = AutoPlayAI.Instance != null
                    && AutoPlayAI.Instance.AutoPlayBestMove(timerForColor, gm.dice1Value, gm.dice2Value);

                if (!moved)
                {
                    gm.ResetDiceState();
                    gm.AdvanceToNextPlayer();
                }
            }
        }

        isTurnTimerActive = false;
        turnTimerCoroutine = null;
    }

    /// <summary>
    /// MM:SS in the top-centre bar, per the board UI sketch, plus the active card's ring.
    /// </summary>
    private void RenderTimer(float remaining, GameManager.PlayerColor color)
    {
        float clamped = Mathf.Max(0f, remaining);

        if (timerText != null)
        {
            int total = Mathf.CeilToInt(clamped);
            timerText.text = $"{total / 60:00}:{total % 60:00}";

            float normalized = turnDuration > 0f ? clamped / turnDuration : 0f;
            timerText.color = normalized > 0.5f
                ? Color.Lerp(timerWarnColor, timerSafeColor, (normalized - 0.5f) * 2f)
                : Color.Lerp(timerDangerColor, timerWarnColor, normalized * 2f);
        }

        PlayerCardUI activeCard = GetPlayerCard(color);
        if (activeCard != null && turnDuration > 0f)
        {
            activeCard.UpdateTimerBar(clamped / turnDuration);
        }
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

        if (extraRollButtons != null)
        {
            foreach (Button extra in extraRollButtons)
            {
                if (extra != null) extra.interactable = enabled;
            }
        }
    }

    // ===========================
    // TOP BAR — QUIT & SOUND
    // ===========================

    /// <summary>
    /// Asks before leaving. Exit is the only irreversible tap in a match — the board is gone and
    /// the result is forfeited — so it is the one place a confirm earns its extra tap.
    /// </summary>
    private void OnQuitClicked()
    {
        // The styled dialog first. quitConfirmPanel is a bare placeholder in the scene with no
        // children, so switching it on showed nothing and Exit read as a dead button — check
        // that the panel actually has content before trusting it.
        if (LudoNX.UI.ScreenManager.Instance != null)
        {
            // Cancelling, tapping the scrim and the back key all route through Dismiss, so the
            // timer resumes however the player backs out. On confirm, Dismiss runs before the
            // callback, so ConfirmExit still gets the last word and stops the timer.
            var modal = LudoNX.UI.UIModal.Open("Confirm", ResumeTurnTimer);
            if (modal != null)
            {
                var confirm = modal.GetComponent<LudoNX.UI.ConfirmModal>();
                if (confirm != null)
                {
                    PauseTurnTimer();
                    confirm.Configure(
                        "LEAVE MATCH?",
                        "This match will be forfeited and the result will not be saved.",
                        "LEAVE", "KEEP PLAYING",
                        ConfirmExit);
                    return;
                }

                modal.Dismiss();
            }
        }

        if (quitConfirmPanel != null && quitConfirmPanel.transform.childCount > 0)
        {
            quitConfirmPanel.SetActive(true);
            PauseTurnTimer();
            return;
        }

        // No dialog in the scene: leave rather than do nothing, which is what this button used to do.
        Debug.LogWarning("[UIManager] No exit dialog assigned — leaving the match directly.");
        ConfirmExit();
    }

    /// <summary>Dismisses the exit dialog and hands the turn back.</summary>
    public void CancelExit()
    {
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
        ResumeTurnTimer();
    }

    /// <summary>Forfeits the match and returns to the home screen.</summary>
    public void ConfirmExit()
    {
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
        StopTurnTimer();

        // The new UI owns navigation when it is present; LudoMenuFlow is the legacy fallback.
        if (LudoNX.UI.MatchBridge.Instance != null)
        {
            LudoNX.UI.MatchBridge.Instance.LeaveMatch();
            if (LudoNX.UI.ScreenManager.Instance != null)
                LudoNX.UI.ScreenManager.Instance.ResetTo(LudoNX.UI.ScreenId.Home);
        }
        else if (LudoMenuFlow.Instance != null)
        {
            LudoMenuFlow.Instance.OnQuitToMenuPressed();
        }
        else
        {
            Debug.LogWarning("[UIManager] No menu flow in the scene — nothing to exit to.");
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

    /// <summary>
    /// Muted state is shown by striking the speaker through and draining the button, rather than
    /// swapping in a second icon — there is no muted sprite in the art set, and a strike reads as
    /// "off" instantly without adding another asset.
    /// </summary>
    private void UpdateSoundIcons()
    {
        if (soundOnIcon != null) soundOnIcon.gameObject.SetActive(isSoundOn);
        if (soundOffIcon != null) soundOffIcon.gameObject.SetActive(!isSoundOn);

        if (soundToggleButton == null) return;

        Transform strike = soundToggleButton.transform.Find(SoundStrikeName);
        if (strike != null) strike.gameObject.SetActive(!isSoundOn);

        // Fade the whole button when muted so the off state reads at a glance.
        float target = isSoundOn ? 1f : mutedButtonAlpha;

        Image face = soundToggleButton.GetComponent<Image>();
        if (face != null) face.color = new Color(1f, 1f, 1f, target);

        Transform icon = soundToggleButton.transform.Find("Icon");
        if (icon != null)
        {
            Image iconImage = icon.GetComponent<Image>();
            if (iconImage != null) iconImage.color = new Color(1f, 1f, 1f, target);
        }
    }

    public const string SoundStrikeName = "Strike";
    [Tooltip("Opacity of the sound button while muted.")]
    [Range(0.2f, 1f)] public float mutedButtonAlpha = 0.45f;

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
            // <mspace> forces fixed-width character cells so the padded columns below
            // actually line up — TMP's fonts (Lilita One / Plus Jakarta Sans) are
            // proportional, so plain padding alone renders as a ragged, non-aligned table.
            string stats = "<mspace=0.6em>MATCH STATS\n\n";
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
            stats += $"\n🎲 Luckiest Roller: {luckiest}</mspace>";

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

    /// <summary>
    /// Pushes live points, win chance and home-token counts into all four cards. WinCalculator
    /// already recomputes every frame but nothing consumed it, so the cards never updated.
    /// Driven on a slow tick rather than per-frame — these values only move on game events.
    /// </summary>
    private IEnumerator LiveCardRefreshRoutine()
    {
        WaitForSeconds tick = new WaitForSeconds(0.25f);

        while (true)
        {
            RefreshAllPlayerCards();
            yield return tick;
        }
    }

    /// <summary>
    /// Shows only the cards for houses in this match.
    ///
    /// RefreshAllPlayerCards only walks activePlayers, so an unused card is never updated — it
    /// would sit there frozen on whatever the last match left behind. Hiding it is the honest
    /// counterpart to hiding that house's tokens.
    /// </summary>
    public void ApplyActivePlayerCards()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.activePlayers == null) return;

        foreach (GameManager.PlayerColor color in System.Enum.GetValues(typeof(GameManager.PlayerColor)))
        {
            PlayerCardUI card = GetPlayerCard(color);
            if (card != null) card.gameObject.SetActive(gm.activePlayers.Contains(color));
        }
    }

    public void RefreshAllPlayerCards()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.activePlayers == null) return;

        WinCalculator win = WinCalculator.Instance;
        PointsManager pts = PointsManager.Instance;

        foreach (GameManager.PlayerColor color in gm.activePlayers)
        {
            PlayerCardUI card = GetPlayerCard(color);
            if (card == null) continue;

            if (pts != null)
            {
                card.UpdatePoints(pts.GetPoints(color), 0);
                card.UpdateHomeTokens(pts.GetTokensHome(color));
                card.SetFireStreak(pts.GetFireStreak(color));
            }

            if (win != null)
            {
                card.SetWinRate(GetWinPercentage(win, color));
            }

            card.SetActiveTurn(color == gm.currentTurnColor);
        }
    }

    private static float GetWinPercentage(WinCalculator win, GameManager.PlayerColor color)
    {
        return color switch
        {
            GameManager.PlayerColor.RED => win.redWinPercentage,
            GameManager.PlayerColor.GREEN => win.greenWinPercentage,
            GameManager.PlayerColor.YELLOW => win.yellowWinPercentage,
            GameManager.PlayerColor.BLUE => win.blueWinPercentage,
            _ => 0f
        };
    }

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