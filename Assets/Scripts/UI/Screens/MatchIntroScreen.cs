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
        public ClayButton backButton;
        public ClayButton skipArea;

        [Header("Readout")]
        public TextMeshProUGUI countdownLabel;
        public TextMeshProUGUI modeChipLabel;
        public TextMeshProUGUI difficultyChipLabel;
        public TextMeshProUGUI youColourLabel;
        public ClayGraphic youColourSwatch;
        public ClayGraphic[] opponentSwatches;
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

            if (modeChipLabel != null)
                modeChipLabel.text = (s != null && s.pendingSafeZone) ? "SAFE ZONE" : "CLASSIC";
            if (difficultyChipLabel != null)
                difficultyChipLabel.text = (s != null ? s.pendingDifficulty : LudoAI.Difficulty.Normal)
                                           .ToString().ToUpperInvariant();

            if (players != null && players.Count > 0)
            {
                if (youColourSwatch != null)
                {
                    youColourSwatch.color = theme.ForPlayer(players[0]);
                    youColourSwatch.SetVerticesDirty();
                }
                if (youColourLabel != null) youColourLabel.text = "YOU — " + players[0];

                for (int i = 0; i < opponentSwatches.Length; i++)
                {
                    bool has = players.Count > i + 1;
                    if (opponentSwatches[i] != null)
                    {
                        opponentSwatches[i].gameObject.SetActive(has);
                        if (has)
                        {
                            opponentSwatches[i].color = theme.ForPlayer(players[i + 1]);
                            opponentSwatches[i].SetVerticesDirty();
                        }
                    }
                    if (opponentLabels != null && i < opponentLabels.Length && opponentLabels[i] != null)
                    {
                        opponentLabels[i].gameObject.SetActive(has);
                        if (has) opponentLabels[i].text = "BOT " + (i + 1);
                    }
                }
            }

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
