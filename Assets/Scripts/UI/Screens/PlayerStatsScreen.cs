using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Detailed gameplay analytics screen.
    ///
    /// Displays overall totals at the top, then a horizontal tab strip that
    /// lets the player slice the same numbers by game mode:
    /// All · 2P · 4P · Casual · Ranked · Private.
    ///
    /// Each mode section is a scroll view with a grid of stat tiles so that
    /// adding or removing stats in the future only requires touching this
    /// component and the author — the layout regenerates automatically.
    /// </summary>
    public class PlayerStatsScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton backButton;

        // ── Filter tabs ───────────────────────────────────────────────────
        public ClayButton tabAll;
        public ClayButton tabTwoPlayer;
        public ClayButton tabFourPlayer;
        public ClayButton tabCasual;
        public ClayButton tabRanked;
        public ClayButton tabPrivate;

        /// <summary>Highlight strip that slides under the active tab.</summary>
        public RectTransform tabIndicator;

        // ── Overall section (always visible above the tab strip) ──────────
        [Header("Overall")]
        public TextMeshProUGUI totalGamesValue;
        public TextMeshProUGUI totalWinsValue;
        public TextMeshProUGUI totalLossesValue;
        public TextMeshProUGUI winPctValue;
        public TextMeshProUGUI totalPointsValue;
        public TextMeshProUGUI avgPointsValue;
        public TextMeshProUGUI totalCapturesValue;
        public TextMeshProUGUI timesCapturedValue;
        public TextMeshProUGUI tokensCompletedValue;
        public TextMeshProUGUI avgDurationValue;

        // ── Mode-specific scroll panels ───────────────────────────────────
        [Header("Mode Panels")]
        public GameObject panelAll;
        public GameObject panelTwoPlayer;
        public GameObject panelFourPlayer;
        public GameObject panelCasual;
        public GameObject panelRanked;
        public GameObject panelPrivate;

        // Per-panel stat labels — each array maps to the same logical stats
        // in the order: Games · Wins · Losses · WinPct · Points · AvgPts
        [Header("Per-Mode: All")]
        public TextMeshProUGUI[] allStats = new TextMeshProUGUI[6];

        [Header("Per-Mode: 2 Player")]
        public TextMeshProUGUI[] twoPlayerStats = new TextMeshProUGUI[6];

        [Header("Per-Mode: 4 Player")]
        public TextMeshProUGUI[] fourPlayerStats = new TextMeshProUGUI[6];

        [Header("Per-Mode: Casual")]
        public TextMeshProUGUI[] casualStats = new TextMeshProUGUI[6];

        [Header("Per-Mode: Ranked")]
        public TextMeshProUGUI[] rankedStats = new TextMeshProUGUI[6];

        [Header("Per-Mode: Private")]
        public TextMeshProUGUI[] privateStats = new TextMeshProUGUI[6];

        private int _activeTab;
        private GameObject[] _panels;
        private ClayButton[] _tabs;

        private void Awake()
        {
            _panels = new[] { panelAll, panelTwoPlayer, panelFourPlayer, panelCasual, panelRanked, panelPrivate };
            _tabs   = new[] { tabAll, tabTwoPlayer, tabFourPlayer, tabCasual, tabRanked, tabPrivate };

            if (backButton     != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (tabAll         != null) tabAll.onClick.AddListener(()        => SetTab(0));
            if (tabTwoPlayer   != null) tabTwoPlayer.onClick.AddListener(()  => SetTab(1));
            if (tabFourPlayer  != null) tabFourPlayer.onClick.AddListener(() => SetTab(2));
            if (tabCasual      != null) tabCasual.onClick.AddListener(()     => SetTab(3));
            if (tabRanked      != null) tabRanked.onClick.AddListener(()     => SetTab(4));
            if (tabPrivate     != null) tabPrivate.onClick.AddListener(()    => SetTab(5));
        }

        public override void OnShow(object args)
        {
            Refresh();
            SetTab(0);
        }

        private void SetTab(int index)
        {
            _activeTab = index;
            var theme = UITheme.Current;

            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i] == null) continue;
                bool active = i == index;
                _tabs[i].Clay.color = active ? theme.secondary : theme.neutral;
                _tabs[i].Clay.SetVerticesDirty();
                var lbl = _tabs[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (lbl != null)
                    lbl.color = UITheme.Lin(active ? theme.InkOn(theme.secondary) : theme.inkDim);
            }

            for (int i = 0; i < _panels.Length; i++)
                if (_panels[i] != null) _panels[i].SetActive(i == index);

            // Slide the indicator under the active tab.
            if (tabIndicator != null && index < _tabs.Length && _tabs[index] != null)
            {
                var tabRt = (RectTransform)_tabs[index].transform;
                tabIndicator.anchoredPosition = new Vector2(tabRt.anchoredPosition.x,
                                                            tabIndicator.anchoredPosition.y);
            }
        }

        private void Refresh()
        {
            var d = StatsService.Data;   // CareerStats

            Set(totalGamesValue,      d.matches.ToString());
            Set(totalWinsValue,       d.wins.ToString());
            Set(totalLossesValue,     d.losses.ToString());
            Set(winPctValue,          Mathf.RoundToInt(d.WinRate * 100f) + "%");
            Set(totalPointsValue,     d.totalPoints.ToString("N0"));
            Set(avgPointsValue,       d.matches > 0
                                        ? Mathf.RoundToInt((float)d.totalPoints / d.matches).ToString()
                                        : "0");
            Set(totalCapturesValue,   d.captures.ToString());
            Set(timesCapturedValue,   d.timesCaptured.ToString());
            Set(tokensCompletedValue, d.tokensHome.ToString());
            Set(avgDurationValue,     StatsService.FormatDuration(
                                        d.matches > 0 ? d.totalPlaySeconds / d.matches : 0));

            // Per-mode stats
            RefreshMode(allStats,        d.matches,        d.wins,        d.losses,        d.WinRate,         d.totalPoints);
            RefreshMode(twoPlayerStats,  d.twoPlayerMatches,  d.twoPlayerWins,  d.twoPlayerMatches  - d.twoPlayerWins,  d.TwoPlayerWinRate,  0);
            RefreshMode(fourPlayerStats, d.fourPlayerMatches, d.fourPlayerWins, d.fourPlayerMatches - d.fourPlayerWins, d.FourPlayerWinRate, 0);
            RefreshMode(casualStats,     d.casualMatches,  d.casualWins,  d.casualMatches  - d.casualWins,  d.CasualWinRate,  0);
            RefreshMode(rankedStats,     d.rankedMatches,  d.rankedWins,  d.rankedMatches  - d.rankedWins,  d.RankedWinRate,  0);
            RefreshMode(privateStats,    d.privateMatches, d.privateWins, d.privateMatches - d.privateWins, d.PrivateWinRate, 0);
        }

        private static void RefreshMode(TextMeshProUGUI[] arr, int games, int wins, int losses,
                                        float winRate, int points)
        {
            if (arr == null || arr.Length < 6) return;
            Set(arr[0], games.ToString());
            Set(arr[1], wins.ToString());
            Set(arr[2], losses.ToString());
            Set(arr[3], Mathf.RoundToInt(winRate * 100f) + "%");
            Set(arr[4], points.ToString("N0"));
            Set(arr[5], games > 0 ? Mathf.RoundToInt((float)points / games).ToString() : "0");
        }

        private static void Set(TextMeshProUGUI lbl, string v)
        {
            if (lbl != null) lbl.text = v;
        }
    }
}
