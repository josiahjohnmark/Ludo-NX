using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Final placement and what the match earned.
    ///
    /// Play Again returns to Quick Play Setup rather than restarting immediately, because after
    /// a loss the first thing a player usually wants to change is the difficulty.
    /// </summary>
    public class MatchResultScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton playAgainButton;
        public ClayButton homeButton;

        [Header("Readout")]
        public TextMeshProUGUI headlineLabel;
        public TextMeshProUGUI subheadLabel;
        public ClayGraphic headlineBanner;
        public TextMeshProUGUI tokensHomeValue;
        public TextMeshProUGUI capturesValue;
        public TextMeshProUGUI nxValue;

        [Header("Placement rows")]
        public ClayGraphic[] rowPanels;
        public ClayGraphic[] rowSwatches;
        public TextMeshProUGUI[] rowPlaces;
        public TextMeshProUGUI[] rowNames;
        public TextMeshProUGUI[] rowScores;

        private void Awake()
        {
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(() => ScreenManager.Instance.Replace(ScreenId.QuickPlaySetup));
            if (homeButton != null)
                homeButton.onClick.AddListener(() => ScreenManager.Instance.ResetTo(ScreenId.Home));
        }

        public override void OnShow(object args)
        {
            var board = args as List<GameManager.PlayerColor>;
            var theme = UITheme.Current;
            var gm = GameManager.Instance;
            var points = PointsManager.Instance;
            var session = LudoNXSession.Instance;

            GameManager.PlayerColor me = gm != null ? gm.humanColor : GameManager.PlayerColor.RED;
            int placement = board != null ? board.IndexOf(me) + 1 : 1;
            bool won = placement == 1;

            if (headlineLabel != null) headlineLabel.text = won ? "VICTORY!" : "DEFEAT";
            if (subheadLabel != null)
                subheadLabel.text = won ? "You dominated the board." : "Better luck next roll.";
            if (headlineBanner != null)
            {
                headlineBanner.color = won ? theme.gold : theme.neutral;
                headlineBanner.SetVerticesDirty();
            }

            // Headline tiles, counted up so the numbers feel earned.
            var mine = points != null ? points.GetStats(me) : null;
            int home = mine != null ? mine.tokensHome : 0;
            int caps = mine != null ? mine.captures : 0;
            int nx = session != null ? session.LastNXEarned : 0;

            if (tokensHomeValue != null) UITween.CountTo(0, home, 0.6f, v => tokensHomeValue.text = v + "/4");
            if (capturesValue != null) UITween.CountTo(0, caps, 0.6f, v => capturesValue.text = v.ToString());
            if (nxValue != null) UITween.CountTo(0, nx, 0.7f, v => nxValue.text = "+" + v);

            // Placement table.
            for (int i = 0; i < rowPanels.Length; i++)
            {
                bool has = board != null && i < board.Count;
                if (rowPanels[i] != null) rowPanels[i].gameObject.SetActive(has);
                if (!has) continue;

                GameManager.PlayerColor c = board[i];
                bool isMe = c == me;

                if (rowPanels[i] != null)
                {
                    rowPanels[i].color = isMe ? theme.panelHigh : theme.panel;
                    rowPanels[i].SetVerticesDirty();
                }
                if (i < rowSwatches.Length && rowSwatches[i] != null)
                {
                    rowSwatches[i].color = theme.ForPlayer(c);
                    rowSwatches[i].SetVerticesDirty();
                }
                if (i < rowPlaces.Length && rowPlaces[i] != null) rowPlaces[i].text = (i + 1).ToString();
                if (i < rowNames.Length && rowNames[i] != null)
                    rowNames[i].text = isMe ? "YOU" : c.ToString();
                if (i < rowScores.Length && rowScores[i] != null)
                    rowScores[i].text = points != null ? points.GetPoints(c).ToString() : "0";
            }
        }

        /// <summary>Back from the result screen goes Home — there is nothing sensible to pop to.</summary>
        public override bool OnBack()
        {
            ScreenManager.Instance.ResetTo(ScreenId.Home);
            return true;
        }
    }
}
