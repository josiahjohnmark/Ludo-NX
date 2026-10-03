using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// The beat between pressing Start and the board appearing.
    ///
    /// Technically unnecessary — the match could begin instantly. It earns its place because it
    /// tells the player which colour they are before they have to work it out from the board,
    /// and because a match that begins on a countdown feels deliberate rather than abrupt.
    /// Tapping anywhere skips it.
    /// </summary>
    public class MatchIntroScreen : UIScreen
    {
        [Header("Wiring")]
        public UnityEngine.UI.Button backButton;
        public UnityEngine.UI.Button skipArea;

        [Header("Readout")]
        public TextMeshProUGUI countdownLabel;
        public TextMeshProUGUI modeChipLabel;
        public TextMeshProUGUI difficultyChipLabel;
        public TextMeshProUGUI youColourLabel;
        public UnityEngine.UI.Image youColourSwatch;
        public UnityEngine.UI.Image[] opponentSwatches;
        public TextMeshProUGUI[] opponentLabels;

        private const float CountdownSeconds = 2.4f;
        private Coroutine _timer;
        private bool _launched;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(Cancel);
            if (skipArea != null) skipArea.onClick.AddListener(Launch);
        }

        public override void OnShow(object args)
        {
            _launched = false;
            var s = LudoNXSession.Instance;
            var theme = UITheme.Current;

            var players = s != null ? s.BuildPendingPlayers() : null;
            bool isTwoPlayerFourHouse = s != null && (s.pendingTeamPlay || (s.pendingMode == LudoNXSession.Mode.QuickMatch && s.pendingPlayerCount == 2));

            if (modeChipLabel != null)
                modeChipLabel.text = isTwoPlayerFourHouse ? "2-PLAYER (4 HOUSES)" : ((s != null && s.pendingSafeZone) ? "SAFE ZONE" : "CLASSIC");
            if (difficultyChipLabel != null)
                difficultyChipLabel.text = (s != null ? s.pendingDifficulty : LudoAI.Difficulty.Normal)
                                           .ToString().ToUpperInvariant();

            if (isTwoPlayerFourHouse)
            {
                if (youColourSwatch != null) youColourSwatch.color = new Color32(231, 55, 53, 255); // Red
                if (youColourLabel != null) youColourLabel.text = string.IsNullOrEmpty(s?.PlayerName) ? "YOU — RED & YELLOW" : $"{s.PlayerName.ToUpper()} — RED & YELLOW";

                if (opponentSwatches != null && opponentSwatches.Length > 0 && opponentSwatches[0] != null)
                {
                    opponentSwatches[0].gameObject.SetActive(true);
                    opponentSwatches[0].color = new Color32(77, 174, 81, 255); // Green
                }
                if (opponentLabels != null && opponentLabels.Length > 0 && opponentLabels[0] != null)
                {
                    opponentLabels[0].gameObject.SetActive(true);
                    opponentLabels[0].text = "OPPONENT — GREEN & BLUE";
                }
                if (opponentSwatches != null)
                {
                    for (int i = 1; i < opponentSwatches.Length; i++)
                    {
                        if (opponentSwatches[i] != null) opponentSwatches[i].gameObject.SetActive(false);
                        if (opponentLabels != null && i < opponentLabels.Length && opponentLabels[i] != null)
                            opponentLabels[i].gameObject.SetActive(false);
                    }
                }
            }
            else if (players != null && players.Count > 0)
            {
                if (youColourSwatch != null)
                    youColourSwatch.color = theme.ForPlayer(players[0]);
                if (youColourLabel != null) youColourLabel.text = "YOU — " + players[0];

                if (opponentSwatches != null)
                {
                    for (int i = 0; i < opponentSwatches.Length; i++)
                    {
                        bool has = players.Count > i + 1;
                        if (opponentSwatches[i] != null)
                        {
                            opponentSwatches[i].gameObject.SetActive(has);
                            if (has) opponentSwatches[i].color = theme.ForPlayer(players[i + 1]);
                        }
                        if (opponentLabels != null && i < opponentLabels.Length && opponentLabels[i] != null)
                        {
                            opponentLabels[i].gameObject.SetActive(has);
                            if (has) opponentLabels[i].text = "BOT " + (i + 1);
                        }
                    }
                }
            }

            bool isOnline = s != null && s.pendingMode == LudoNXSession.Mode.QuickMatch;

            if (!isOnline)
            {
                // Non-online game modes (VS Computer, Pass & Play) do NOT have a countdown.
                // Launch immediately into the match!
                if (countdownLabel != null) countdownLabel.gameObject.SetActive(false);
                Launch();
                return;
            }

            if (countdownLabel != null) countdownLabel.gameObject.SetActive(true);
            if (_timer != null) StopCoroutine(_timer);
            _timer = StartCoroutine(CountDown());
        }

        public override void OnHide()
        {
            if (_timer != null) { StopCoroutine(_timer); _timer = null; }
        }

        private System.Collections.IEnumerator CountDown()
        {
            float t = CountdownSeconds;
            while (t > 0f)
            {
                if (countdownLabel != null) countdownLabel.text = Mathf.CeilToInt(t).ToString();
                t -= Time.unscaledDeltaTime;
                yield return null;
            }
            Launch();
        }

        private void Launch()
        {
            if (_launched) return;
            _launched = true;
            if (MatchBridge.Instance != null) MatchBridge.Instance.StartPendingMatch();
        }

        private void Cancel()
        {
            _launched = true;
            if (_timer != null) { StopCoroutine(_timer); _timer = null; }
            ScreenManager.Instance.Pop();
        }

        public override bool OnBack()
        {
            Cancel();
            return true;
        }
    }
}
