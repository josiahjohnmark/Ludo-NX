using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// How the match ended: where everyone finished, what it paid, and what to do next.
    ///
    /// The rewards count up rather than appearing. It costs a second and it is the only moment in
    /// the loop that says "that was worth playing" — on a staked match the NX line is the whole
    /// point of having chosen a multiplier.
    ///
    /// PLAY AGAIN goes back to setup rather than restarting instantly, because the first thing a
    /// player wants to change after a loss is usually the difficulty or the stake.
    /// </summary>
    public class ResultScreen : UIScreen
    {
        [Header("Headline")]
        public TextMeshProUGUI headline;
        public TextMeshProUGUI subhead;
        public Image crest;
        public Sprite winCrest;
        public Sprite loseCrest;

        [Header("Placements")]
        public RectTransform placementContent;
        public ResultRow rowTemplate;

        [Header("Rewards")]
        public TextMeshProUGUI coinsLabel;
        public TextMeshProUGUI nxLabel;
        public TextMeshProUGUI stakeNote;

        [Header("Match stats")]
        public TextMeshProUGUI tokensHomeValue;
        public TextMeshProUGUI capturesValue;
        public TextMeshProUGUI turnsValue;

        [Header("Buttons")]
        public Button playAgainButton;
        public Button homeButton;

        [Header("Art")]
        public Sprite[] pawnSprites;        // red, green, yellow, blue

        [Header("Feel")]
        public float countSeconds = 0.9f;

        private static readonly GameManager.PlayerColor[] Order =
        {
            GameManager.PlayerColor.RED,
            GameManager.PlayerColor.GREEN,
            GameManager.PlayerColor.YELLOW,
            GameManager.PlayerColor.BLUE,
        };

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private Coroutine _count;

        private void Awake()
        {
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(PlayAgain);
            if (homeButton != null)
                homeButton.onClick.AddListener(() => ScreenManager.Instance.ResetTo(ScreenId.Home));
        }

        /// <summary>
        /// Setup is a popup over Home now, not its own screen — go home, then reopen whichever
        /// card the player actually came from.
        /// </summary>
        private static void PlayAgain()
        {
            ScreenManager.Instance.ResetTo(ScreenId.Home);
            var s = LudoNXSession.Instance;
            var mode = s != null ? s.pendingMode : LudoNXSession.Mode.OfflineQuick;
            if (mode == LudoNXSession.Mode.QuickMatch) OnlineMatchModal.Open();
            else if (mode == LudoNXSession.Mode.PassAndPlay) MatchSetupModal.OpenFor(MatchSetupModal.Flavour.PassAndPlay);
            else MatchSetupModal.OpenFor(MatchSetupModal.Flavour.VsComputer);
        }

        public override void OnShow(object args)
        {
            var board = args as List<GameManager.PlayerColor>;
            var gm = GameManager.Instance;
            var session = LudoNXSession.Instance;
            var points = PointsManager.Instance;

            GameManager.PlayerColor me = gm != null ? gm.humanColor : GameManager.PlayerColor.RED;
            int placement = board != null ? board.IndexOf(me) + 1 : 1;
            if (placement <= 0) placement = board != null ? board.Count : 1;
            bool won = placement == 1;

            if (headline != null) headline.text = won ? "VICTORY!" : Ordinal(placement) + " PLACE";
            if (subhead != null)
                subhead.text = won ? "Every token home first." : "So close — one more round?";
            if (crest != null && winCrest != null && loseCrest != null)
                crest.sprite = won ? winCrest : loseCrest;

            BuildPlacements(board, me);

            var mine = points != null ? points.GetStats(me) : null;
            if (tokensHomeValue != null) tokensHomeValue.text = mine != null ? mine.tokensHome.ToString() : "0";
            if (capturesValue != null) capturesValue.text = mine != null ? mine.captures.ToString() : "0";
            if (turnsValue != null) turnsValue.text = gm != null ? gm.TurnSequence.ToString() : "0";

            int coins = session != null ? session.LastCoinsEarned : 0;
            int nx = session != null ? session.LastNXEarned : 0;
            int stake = session != null ? Mathf.Max(1, session.pendingMultiplier) : 1;

            if (stakeNote != null)
            {
                stakeNote.gameObject.SetActive(stake > 1);
                stakeNote.text = won ? $"x{stake} stake paid out" : $"x{stake} stake — half returned";
            }

            if (_count != null) StopCoroutine(_count);
            _count = StartCoroutine(CountUp(coins, nx));
        }

        public override void OnHide()
        {
            if (_count != null) { StopCoroutine(_count); _count = null; }
        }

        // ==================================================================================
        // placements
        // ==================================================================================

        private void BuildPlacements(List<GameManager.PlayerColor> board, GameManager.PlayerColor me)
        {
            foreach (var go in _spawned)
            {
                if (go != null) { go.SetActive(false); Destroy(go); }
            }
            _spawned.Clear();
            if (board == null || rowTemplate == null || placementContent == null) return;

            var points = PointsManager.Instance;
            var session = LudoNXSession.Instance;
            var gm = GameManager.Instance;

            for (int i = 0; i < board.Count; i++)
            {
                var row = Instantiate(rowTemplate, placementContent);
                row.gameObject.SetActive(true);
                _spawned.Add(row.gameObject);

                var colour = board[i];
                int houseIndex = System.Array.IndexOf(Order, colour);
                bool isMe = colour == me;

                if (row.place != null) row.place.text = Ordinal(i + 1);
                if (row.pawn != null && pawnSprites != null && houseIndex >= 0 && houseIndex < pawnSprites.Length)
                    row.pawn.sprite = pawnSprites[houseIndex];
                if (row.nameLabel != null)
                    row.nameLabel.text = isMe
                        ? (session != null && !string.IsNullOrEmpty(session.PlayerName) ? session.PlayerName : "YOU")
                        : NameFor(colour, gm, session);

                var stats = points != null ? points.GetStats(colour) : null;
                if (row.scoreLabel != null) row.scoreLabel.text = stats != null ? stats.points + " pts" : "";
                row.SetHighlighted(isMe);
            }
        }

        /// <summary>The seat names chosen at setup, so the loser on screen is the one they played.</summary>
        private string NameFor(GameManager.PlayerColor colour, GameManager gm, LudoNXSession session)
        {
            if (session == null || session.pendingSeatNames == null || gm == null) return colour.ToString();

            int human = System.Array.IndexOf(Order, gm.humanColor);
            int seat = 0;
            for (int i = 0; i < Order.Length; i++)
            {
                if (i == human) continue;
                if (gm.activePlayers != null && !gm.activePlayers.Contains(Order[i])) continue;
                if (Order[i] == colour)
                    return seat < session.pendingSeatNames.Count && !string.IsNullOrEmpty(session.pendingSeatNames[seat])
                        ? session.pendingSeatNames[seat]
                        : colour.ToString();
                seat++;
            }
            return colour.ToString();
        }

        private static string Ordinal(int n) => n switch
        {
            1 => "1ST", 2 => "2ND", 3 => "3RD", _ => n + "TH"
        };

        // ==================================================================================
        // rewards
        // ==================================================================================

        private IEnumerator CountUp(int coins, int nx)
        {
            float t = 0f;
            while (t < countSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / countSeconds);
                float eased = 1f - (1f - k) * (1f - k);
                Paint(Mathf.RoundToInt(coins * eased), Mathf.RoundToInt(nx * eased));
                yield return null;
            }
            Paint(coins, nx);
            _count = null;
        }

        private void Paint(int coins, int nx)
        {
            if (coinsLabel != null) coinsLabel.text = "+" + coins.ToString("n0");
            if (nxLabel != null)
            {
                nxLabel.text = (nx >= 0 ? "+" : "") + nx.ToString("n0") + " NX";
                nxLabel.color = nx >= 0 ? new Color32(232, 185, 38, 255) : new Color32(226, 92, 80, 255);
            }
        }
    }
}
