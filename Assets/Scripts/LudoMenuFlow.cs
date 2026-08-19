using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Everything that happens before and after a match: the home screen, the play-mode hub, player
/// count, AI difficulty, and the result screen.
///
/// One state machine rather than a set of panels that toggle each other. Screens do not know about
/// each other — they call <see cref="Go"/> and the flow decides what is visible. That is what makes
/// the back button a single line instead of a web of SetActive calls, and it is what lets online
/// modes slot in later without rewiring the offline path.
///
/// The menu lives on its own canvas above the board. While a screen is showing, the in-match HUD
/// canvas is hidden outright rather than layered under a dim, so a stray tap can never roll a die
/// behind the menu.
///
/// The scene wiring is built by Ludo NX/10. Build Home &amp; Play Menu.
/// </summary>
public class LudoMenuFlow : MonoBehaviour
{
    public static LudoMenuFlow Instance { get; private set; }

    public enum Screen
    {
        Home,
        Modes,
        PlayerCount,
        Difficulty,
        Result,
        InMatch
    }

    [Header("Screens")]
    public GameObject screenHome;
    public GameObject screenModes;
    public GameObject screenPlayerCount;
    public GameObject screenDifficulty;
    public GameObject screenResult;

    [Header("Canvases")]
    [Tooltip("The menu's own canvas root. Hidden entirely once a match begins.")]
    public GameObject menuCanvas;
    [Tooltip("The existing in-match HUD canvas (Roll button, player cards, timer).")]
    public GameObject gameHudCanvas;
    [Tooltip("Board, tokens and dice. Hidden behind the menu so the first frame of a match is clean.")]
    public GameObject boardRoot;

    [Header("Home — profile readout")]
    public TextMeshProUGUI homePlayerNameText;
    public TextMeshProUGUI homeLevelText;
    public TextMeshProUGUI homeCoinsText;
    public TextMeshProUGUI homeNXText;
    public Image homeLevelProgressFill;

    [Header("Player count screen")]
    public Button twoPlayerButton;          // Red & Yellow
    public Button twoPlayerAltButton;       // Green & Blue
    public Button fourPlayerButton;

    [Header("Difficulty screen")]
    public Button easyButton;
    public Button normalButton;
    public Button hardButton;
    public TextMeshProUGUI difficultySummaryText;

    [Header("Result screen")]
    public TextMeshProUGUI resultHeadlineText;
    public TextMeshProUGUI resultPlacementText;
    public TextMeshProUGUI resultRewardText;

    [Header("Feel")]
    [Tooltip("Fade time between screens. Kept short — a menu that makes you wait feels cheap.")]
    public float transitionSeconds = 0.12f;

    private Screen current = Screen.Home;
    private CanvasGroup activeGroup;
    private bool matchInProgress;

    public Screen CurrentScreen => current;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Force every screen off before showing Home.
        //
        // Editor tooling drives these same methods to build and photograph screens, and each call
        // leaves its screen's GameObject active in the saved scene. Go() only switches the screens
        // it knows about, so a screen left active by tooling could still be sitting on top of Home
        // on the first frame — which is exactly how the game booted onto the difficulty screen with
        // nothing responding to taps.
        foreach (GameObject screen in new[] { screenHome, screenModes, screenPlayerCount, screenDifficulty, screenResult })
        {
            if (screen == null) continue;
            screen.SetActive(false);
            CanvasGroup group = screen.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 0f;
        }

        matchInProgress = false;
        Go(Screen.Home);
        Debug.Log($"[LudoMenuFlow] Booted to {CurrentScreen}.");
    }

    private void Update()
    {
        // Android back button. Without this the only way out of a sub-screen is the on-screen
        // back arrow, which players on Android will not look for first.
        if (Input.GetKeyDown(KeyCode.Escape) && !matchInProgress)
        {
            Back();
        }
    }

    // ── Navigation ─────────────────────────────────────────────────────────────────────────

    public void Go(Screen screen)
    {
        current = screen;

        bool inMatch = screen == Screen.InMatch;
        if (menuCanvas != null) menuCanvas.SetActive(!inMatch);
        if (gameHudCanvas != null) gameHudCanvas.SetActive(inMatch);
        if (boardRoot != null) boardRoot.SetActive(inMatch);

        Show(screenHome, screen == Screen.Home);
        Show(screenModes, screen == Screen.Modes);
        Show(screenPlayerCount, screen == Screen.PlayerCount);
        Show(screenDifficulty, screen == Screen.Difficulty);
        Show(screenResult, screen == Screen.Result);

        if (screen == Screen.Home) RefreshProfile();
        if (screen == Screen.Difficulty) RefreshDifficultySummary();
    }

    /// <summary>One step back toward Home. Home itself is the floor — it never exits the app.</summary>
    public void Back()
    {
        switch (current)
        {
            case Screen.Modes: Go(Screen.Home); break;
            case Screen.PlayerCount: Go(Screen.Modes); break;
            case Screen.Difficulty: Go(Screen.PlayerCount); break;
            case Screen.Result: Go(Screen.Home); break;
            default: break;
        }
    }

    private void Show(GameObject screen, bool visible)
    {
        if (screen == null) return;

        CanvasGroup group = screen.GetComponent<CanvasGroup>();
        screen.SetActive(visible);

        if (group == null) return;

        group.interactable = visible;
        group.blocksRaycasts = visible;

        if (!visible)
        {
            group.alpha = 0f;
            return;
        }

        if (transitionSeconds <= 0f)
        {
            group.alpha = 1f;
            return;
        }

        if (activeGroup != null && activeGroup != group) activeGroup.alpha = 0f;
        activeGroup = group;
        StopAllCoroutines();
        StartCoroutine(FadeIn(group));
    }

    private IEnumerator FadeIn(CanvasGroup group)
    {
        float t = 0f;
        group.alpha = 0f;
        while (t < transitionSeconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / transitionSeconds);
            yield return null;
        }
        group.alpha = 1f;
    }

    // ── Button targets ─────────────────────────────────────────────────────────────────────

    public void OnPlayPressed() => Go(Screen.Modes);
    public void OnBackPressed() => Back();
    public void OnHomePressed() => Go(Screen.Home);

    /// <summary>Quick Match — the only mode wired up. Online modes call this with their own Mode.</summary>
    public void OnOfflineQuickMatchPressed()
    {
        if (LudoNXSession.Instance != null)
        {
            LudoNXSession.Instance.pendingMode = LudoNXSession.Mode.OfflineQuick;
        }
        Go(Screen.PlayerCount);
    }

    public void OnPlayerCountPressed(int count)
    {
        if (LudoNXSession.Instance != null)
        {
            LudoNXSession.Instance.pendingPlayerCount = count >= 4 ? 4 : 2;
        }
        Go(Screen.Difficulty);
    }

    /// <summary>
    /// Picks the two-player diagonal. 0 = Red &amp; Yellow, 1 = Green &amp; Blue. Int rather than the
    /// enum so a Button can call it directly.
    /// </summary>
    public void OnTwoPlayerSidePressed(int side)
    {
        if (LudoNXSession.Instance != null)
        {
            LudoNXSession.Instance.pendingPlayerCount = 2;
            LudoNXSession.Instance.pendingSide = side == 1
                ? LudoNXSession.TwoPlayerSide.GreenBlue
                : LudoNXSession.TwoPlayerSide.RedYellow;
        }
        Go(Screen.Difficulty);
    }

    /// <summary>0 = Easy, 1 = Normal, 2 = Hard. Int rather than the enum so a Button can call it.</summary>
    public void OnDifficultyPressed(int difficulty)
    {
        LudoAI.Difficulty picked = (LudoAI.Difficulty)Mathf.Clamp(difficulty, 0, 2);

        if (LudoNXSession.Instance != null)
        {
            LudoNXSession.Instance.pendingDifficulty = picked;
        }

        StartMatch();
    }

    /// <summary>Shown on a locked tile. Kept as a stub so the button is honest rather than dead.</summary>
    public void OnLockedFeaturePressed()
    {
        Debug.Log("[LudoMenuFlow] That mode is not in this build yet.");
    }

    // ── Match lifecycle ────────────────────────────────────────────────────────────────────

    public void StartMatch()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[LudoMenuFlow] No GameManager in the scene — cannot start a match.");
            return;
        }

        LudoNXSession session = LudoNXSession.Instance;
        LudoAI.Difficulty difficulty = session != null ? session.pendingDifficulty : LudoAI.Difficulty.Normal;

        // Difficulty is set before initialise so the very first AI turn already uses it.
        LudoAI.CurrentDifficulty = difficulty;

        matchInProgress = true;
        Go(Screen.InMatch);

        if (session != null)
        {
            List<GameManager.PlayerColor> players = session.BuildPendingPlayers();

            // The player controls the first house, so picking Green & Blue really does hand them
            // green rather than quietly leaving them on red with nothing on the board.
            gm.humanColor = players[0];
            gm.InitializeMatch(players, players[0]);

            Debug.Log($"[LudoMenuFlow] Offline match started — {string.Join(", ", players)}, " +
                      $"player is {players[0]}, {difficulty} AI.");
        }
        else
        {
            gm.InitializeMatchWithPlayerCount(4);
            Debug.Log($"[LudoMenuFlow] Offline match started — 4 players, {difficulty} AI (no session).");
        }
    }

    /// <summary>
    /// Called by UIManager when the final placement is known. Banks rewards and shows the result
    /// screen. Safe to call when no menu exists — a scene without the flow simply keeps its old
    /// game-over behaviour.
    /// </summary>
    /// <summary>
    /// Raised the moment a match's final placement is known, before any legacy handling.
    ///
    /// This exists so the new UI can react without this class having to know it exists. When
    /// the LudoMenuFlow object is deactivated, <see cref="Instance"/> is null and the old
    /// result screen never runs — but this still fires, so the new stack picks it up.
    /// </summary>
    public static event System.Action<List<GameManager.PlayerColor>> MatchEnded;

    public static void NotifyMatchEnded(List<GameManager.PlayerColor> leaderBoard)
    {
        if (MatchEnded != null) MatchEnded(leaderBoard);
        if (Instance == null) return;
        Instance.HandleMatchEnded(leaderBoard);
    }

    private void HandleMatchEnded(List<GameManager.PlayerColor> leaderBoard)
    {
        matchInProgress = false;

        GameManager gm = GameManager.Instance;
        GameManager.PlayerColor human = gm != null ? gm.humanColor : GameManager.PlayerColor.RED;

        int placement = leaderBoard != null ? leaderBoard.IndexOf(human) + 1 : 0;
        bool won = placement == 1;

        int matchPoints = 0;
        if (PointsManager.Instance != null)
        {
            matchPoints = PointsManager.Instance.GetPoints(human);
        }

        bool offline = LudoNXSession.Instance == null ||
                       LudoNXSession.Instance.pendingMode == LudoNXSession.Mode.OfflineQuick;

        if (LudoNXSession.Instance != null)
        {
            LudoNXSession.Instance.RecordMatchResult(won, matchPoints, offline);
        }

        PopulateResult(placement, won, matchPoints);
        Go(Screen.Result);
    }

    private void PopulateResult(int placement, bool won, int matchPoints)
    {
        if (resultHeadlineText != null)
        {
            resultHeadlineText.text = won ? "VICTORY" : "MATCH OVER";
        }

        if (resultPlacementText != null)
        {
            resultPlacementText.text = placement > 0
                ? $"{Ordinal(placement)} place  ·  {matchPoints} pts"
                : $"{matchPoints} pts";
        }

        if (resultRewardText != null && LudoNXSession.Instance != null)
        {
            resultRewardText.text =
                $"+{LudoNXSession.Instance.LastCoinsEarned} coins    +{LudoNXSession.Instance.LastNXEarned} NX";
        }
    }

    private static string Ordinal(int n)
    {
        switch (n)
        {
            case 1: return "1st";
            case 2: return "2nd";
            case 3: return "3rd";
            default: return n + "th";
        }
    }

    /// <summary>Result screen → straight back into the same mode, no re-picking.</summary>
    public void OnPlayAgainPressed()
    {
        StartMatch();
    }

    /// <summary>Abandons the running match and returns to Home. Wired to the in-match exit button.</summary>
    public void OnQuitToMenuPressed()
    {
        matchInProgress = false;
        Go(Screen.Home);
    }

    // ── Readouts ───────────────────────────────────────────────────────────────────────────

    private void RefreshProfile()
    {
        LudoNXSession s = LudoNXSession.Instance;
        if (s == null) return;

        if (homePlayerNameText != null) homePlayerNameText.text = s.PlayerName;
        if (homeLevelText != null) homeLevelText.text = s.Level.ToString();
        if (homeCoinsText != null) homeCoinsText.text = Compact(s.Coins);
        if (homeNXText != null) homeNXText.text = Compact(s.NXPoints);
        if (homeLevelProgressFill != null) homeLevelProgressFill.fillAmount = s.LevelProgress01;
    }

    private void RefreshDifficultySummary()
    {
        if (difficultySummaryText == null) return;

        LudoNXSession s = LudoNXSession.Instance;
        if (s == null)
        {
            difficultySummaryText.text = "Offline";
            return;
        }

        // Confirms the colour they are about to play, one screen before it matters.
        List<GameManager.PlayerColor> players = s.BuildPendingPlayers();
        difficultySummaryText.text = $"Offline  ·  {players.Count} players  ·  you play {players[0]}";
    }

    /// <summary>1,240 rather than 1240 — and 12.4K once it stops fitting.</summary>
    private static string Compact(int value)
    {
        if (value >= 1000000) return (value / 1000000f).ToString("0.#") + "M";
        if (value >= 10000) return (value / 1000f).ToString("0.#") + "K";
        return value.ToString("N0");
    }
}
