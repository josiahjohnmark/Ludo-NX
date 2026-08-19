using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// The seam between the new interface and the existing match code.
    ///
    /// The old <c>LudoMenuFlow</c> did navigation and match lifecycle in one class, which is why
    /// it had to own every screen reference. This does only the lifecycle half: hand it the
    /// pending config and it puts the board on screen and starts a game. Navigation is the
    /// ScreenManager's job, and neither knows about the other's internals.
    /// </summary>
    [DisallowMultipleComponent]
    public class MatchBridge : MonoBehaviour
    {
        public static MatchBridge Instance { get; private set; }

        internal static void ResetStatics() { Instance = null; }

        [Tooltip("The existing in-match HUD canvas (player cards, roll button, timer).")]
        public GameObject hudCanvas;

        [Tooltip("Board, tokens and dice. Leave empty if the board is always visible.")]
        public GameObject boardRoot;

        public bool MatchInProgress { get; private set; }

        private float _startedAt;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (hudCanvas == null) hudCanvas = FindHudCanvas();
        }

        private void OnEnable() { LudoMenuFlow.MatchEnded += HandleMatchEnded; }
        private void OnDisable() { LudoMenuFlow.MatchEnded -= HandleMatchEnded; }

        /// <summary>
        /// The match is over. Bank the statistics that <c>PointsManager</c> is about to discard,
        /// hand the screen back to the menu stack, and show the result.
        /// </summary>
        private void HandleMatchEnded(List<GameManager.PlayerColor> leaderBoard)
        {
            MatchInProgress = false;
            RecordStats(leaderBoard);

            if (hudCanvas != null) hudCanvas.SetActive(false);
            if (ScreenManager.Instance == null) return;
            ScreenManager.Instance.SetScreensVisible(true);
            ScreenManager.Instance.Replace(ScreenId.Result, leaderBoard);
        }

        private void RecordStats(List<GameManager.PlayerColor> leaderBoard)
        {
            var gm = GameManager.Instance;
            var points = PointsManager.Instance;
            var session = LudoNXSession.Instance;
            if (gm == null || leaderBoard == null) return;

            GameManager.PlayerColor me = gm.humanColor;
            int placement = leaderBoard.IndexOf(me) + 1;
            if (placement <= 0) placement = leaderBoard.Count;

            int score = 0, captures = 0, taken = 0, home = 0;
            bool mvp = false;
            if (points != null)
            {
                var s = points.GetStats(me);
                if (s != null)
                {
                    score = s.points;
                    captures = s.captures;
                    taken = s.timesCaptured;
                    home = s.tokensHome;
                }
                mvp = points.DetermineMVP() == me;
            }

            int playerCount = session != null ? session.pendingPlayerCount : leaderBoard.Count;
            int duration = _startedAt > 0f ? Mathf.RoundToInt(Time.unscaledTime - _startedAt) : 0;

            StatsService.RecordMatch(placement, playerCount, score, captures, taken, home, mvp, duration);

            // The existing session still owns coins and NX; keep it in step.
            if (session != null) session.RecordMatchResult(placement == 1, score, true);
        }

        /// <summary>
        /// Finds the in-match HUD canvas.
        ///
        /// Deliberately NOT "the object with UIManager on it" — in this project UIManager is a
        /// component of the GameManager object, not of the canvas, so that search returns the
        /// game's brain and hiding it kills the match. Look for an actual Canvas instead.
        /// </summary>
        private static GameObject FindHudCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Preference 1: the canvas carrying the HUD layout driver.
            foreach (var c in canvases)
                if (c.GetComponent<MatchHudLayout>() != null) return c.gameObject;

            // Preference 2: the canvas that owns the match-only visual helpers.
            foreach (var c in canvases)
                if (c.GetComponent<BackgroundAmbience>() != null || c.GetComponent<FloatingTextFX>() != null)
                    return c.gameObject;

            // Preference 3: by name, excluding our own root and the legacy menu.
            foreach (var c in canvases)
                if (c.name == "Canvas") return c.gameObject;

            return null;
        }

        /// <summary>
        /// Starts a match from whatever <see cref="LudoNXSession"/> currently has pending.
        /// Mirrors what LudoMenuFlow.StartMatch did, minus the navigation.
        /// </summary>
        public void StartPendingMatch()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("[MatchBridge] No GameManager in the scene — cannot start a match.");
                return;
            }

            var session = LudoNXSession.Instance;
            LudoAI.Difficulty difficulty = session != null ? session.pendingDifficulty : LudoAI.Difficulty.Normal;

            // Set before initialising so the very first AI turn already uses it.
            LudoAI.CurrentDifficulty = difficulty;

            MatchInProgress = true;
            _startedAt = Time.unscaledTime;
            if (boardRoot != null) boardRoot.SetActive(true);
            if (hudCanvas != null) hudCanvas.SetActive(true);
            if (ScreenManager.Instance != null) ScreenManager.Instance.SetScreensVisible(false);

            if (session != null)
            {
                List<GameManager.PlayerColor> players = session.BuildPendingPlayers();

                // Entry zero is the human, and BuildPendingPlayers has already rotated the chosen
                // colour to the front — so picking green really does hand the player green.
                gm.humanColor = players[0];

                // Pass-and-play has no AI at all: every house is a person on this device.
                gm.aiPlaysOtherColors = !session.AllHousesHuman;

                gm.InitializeMatch(players, players[0]);
                Debug.Log("[MatchBridge] Match started — " + string.Join(", ", players) +
                          ", human is " + players[0] +
                          (session.AllHousesHuman ? ", pass-and-play (no AI)." : ", " + difficulty + " AI."));
            }
            else
            {
                gm.InitializeMatchWithPlayerCount(4);
                Debug.Log("[MatchBridge] Match started — 4 players, " + difficulty + " AI (no session).");
            }
        }

        private UIModal _pause;

        private void Update()
        {
            // During a match the back gesture belongs to the pause dialog, not the menu stack.
            if (!MatchInProgress || !Input.GetKeyDown(KeyCode.Escape)) return;
            if (_pause != null) return;
            OpenPause();
        }

        /// <summary>Resume / Quit Match. Also reachable from the HUD's pause button.</summary>
        public void OpenPause()
        {
            if (_pause != null) return;
            _pause = UIModal.Open("Pause", () => _pause = null);
            if (_pause == null) return;

            var confirm = _pause.GetComponent<ConfirmModal>();
            if (confirm == null) return;
            confirm.Configure("PAUSED", "The match is waiting for you.",
                              "QUIT MATCH", "RESUME",
                              () =>
                              {
                                  LeaveMatch();
                                  if (ScreenManager.Instance != null)
                                      ScreenManager.Instance.ResetTo(ScreenId.Home);
                              });
        }

        /// <summary>Tears the match down and hands the screen back to the menu stack.</summary>
        public void LeaveMatch()
        {
            MatchInProgress = false;
            if (hudCanvas != null) hudCanvas.SetActive(false);
            if (boardRoot != null) boardRoot.SetActive(false);
            if (ScreenManager.Instance != null) ScreenManager.Instance.SetScreensVisible(true);
        }
    }
}
