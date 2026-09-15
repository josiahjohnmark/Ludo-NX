using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Identity, achievements, and quick stats — the player's complete profile card.
    ///
    /// Two tabs are presented at the top:
    ///   • Overview  – avatar, name, rank, XP bar, and career highlight tiles.
    ///   • Awards    – scrollable achievement list with progress bars.
    ///
    /// From the Overview the player can tap "VIEW STATS" to navigate to
    /// <see cref="PlayerStatsScreen"/> for the full analytics breakdown.
    /// </summary>
    public class ProfileScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton backButton;
        public ClayButton identityTab;
        public ClayButton achievementsTab;
        public ClayButton editButton;
        public ClayButton viewStatsButton;
        public GameObject identityPanel;
        public GameObject achievementsPanel;

        // ── Identity / Overview ───────────────────────────────────────────
        [Header("Identity")]
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI playerIdLabel;
        public TextMeshProUGUI levelLabel;
        public TextMeshProUGUI rankLabel;
        public TextMeshProUGUI leagueLabel;
        public TextMeshProUGUI countryLabel;
        public TextMeshProUGUI joinDateLabel;

        /// <summary>XP bar fill — driven by its RectTransform width.</summary>
        public RectTransform xpBarFill;
        public TextMeshProUGUI xpLabel;

        // Career highlight tiles (shown in identity panel)
        public TextMeshProUGUI winsValue;
        public TextMeshProUGUI matchesValue;
        public TextMeshProUGUI winRateValue;
        public TextMeshProUGUI streakValue;
        public TextMeshProUGUI totalPointsValue;
        public TextMeshProUGUI totalCapturesValue;
        public TextMeshProUGUI bestScoreValue;

        // ── Achievements ──────────────────────────────────────────────────
        [Header("Achievements")]
        public RectTransform achievementContent;
        public RectTransform achievementRowTemplate;
        public TextMeshProUGUI achievementSummary;

        // ── Private state ─────────────────────────────────────────────────
        private bool _onAchievements;
        private bool _rowsBuilt;

        private void Awake()
        {
            if (backButton      != null) backButton.onClick.AddListener(HandleBack);
            if (identityTab     != null) identityTab.onClick.AddListener(() => SetTab(false));
            if (achievementsTab != null) achievementsTab.onClick.AddListener(() => SetTab(true));
            if (editButton      != null) editButton.onClick.AddListener(OpenEditName);
            if (viewStatsButton != null) viewStatsButton.onClick.AddListener(OpenStats);
        }

        public override void OnShow(object args)
        {
            SetTab((args as string) == "achievements");
            Refresh();
        }

        // ── Tab switching ─────────────────────────────────────────────────

        private void SetTab(bool achievements)
        {
            _onAchievements = achievements;
            var theme = UITheme.Current;

            if (identityPanel     != null) identityPanel.SetActive(!achievements);
            if (achievementsPanel != null) achievementsPanel.SetActive(achievements);

            if (identityTab != null)
            {
                identityTab.Clay.color = achievements ? theme.neutral : theme.secondary;
                identityTab.Clay.SetVerticesDirty();
                TintTabLabel(identityTab, achievements ? theme.neutral : theme.secondary);
            }
            if (achievementsTab != null)
            {
                achievementsTab.Clay.color = achievements ? theme.secondary : theme.neutral;
                achievementsTab.Clay.SetVerticesDirty();
                TintTabLabel(achievementsTab, achievements ? theme.secondary : theme.neutral);
            }

            if (achievements) BuildAchievementRows();
        }

        private static void TintTabLabel(ClayButton tab, Color fill)
        {
            var label = tab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.color = UITheme.Lin(UITheme.Current.InkOn(fill));
        }

        // ── Data refresh ──────────────────────────────────────────────────

        private void Refresh()
        {
            var s = LudoNXSession.Instance;
            var d = StatsService.Data;   // CareerStats

            // Identity
            if (nameLabel    != null) nameLabel.text    = s != null ? s.PlayerName   : "Player";
            if (playerIdLabel!= null) playerIdLabel.text= "#" + (s != null ? s.PlayerId    : "000000");
            if (levelLabel   != null) levelLabel.text   = "LEVEL " + (s != null ? s.Level.ToString() : "1");
            if (rankLabel    != null) rankLabel.text    = s != null ? s.RankTitle    : "BRONZE I";
            if (leagueLabel  != null) leagueLabel.text  = s != null ? s.LeagueName  : "OPEN LEAGUE";
            if (countryLabel != null) countryLabel.text = s != null ? s.Country     : "—";
            if (joinDateLabel!= null) joinDateLabel.text= s != null ? s.JoinDate    : "—";

            // XP bar
            if (xpBarFill != null)
            {
                float pct = s != null ? s.XpFraction : 0f;
                var track = xpBarFill.parent as RectTransform;
                float trackW = track != null ? track.rect.width : 760f;
                xpBarFill.sizeDelta = new Vector2(trackW * Mathf.Clamp01(pct), xpBarFill.sizeDelta.y);
            }
            if (xpLabel != null)
            {
                int cur = s != null ? s.Xp : 0;
                int max = s != null ? s.XpToNextLevel : 1000;
                xpLabel.text = cur.ToString("N0") + " / " + max.ToString("N0") + " XP";
            }

            // Career tiles
            if (winsValue          != null) winsValue.text          = d.wins.ToString();
            if (matchesValue       != null) matchesValue.text       = d.matches.ToString();
            if (winRateValue       != null) winRateValue.text       = Mathf.RoundToInt(d.WinRate * 100f) + "%";
            if (streakValue        != null) streakValue.text        = d.bestStreak.ToString();
            if (totalPointsValue   != null) totalPointsValue.text   = d.totalPoints.ToString("N0");
            if (totalCapturesValue != null) totalCapturesValue.text = d.captures.ToString();
            if (bestScoreValue     != null) bestScoreValue.text     = d.bestScore.ToString("N0");

            // Achievement summary
            if (achievementSummary != null)
                achievementSummary.text = AchievementService.UnlockedCount(d)
                                        + " of " + AchievementService.Total + " unlocked";

            if (_rowsBuilt) RefreshAchievementRows();
        }

        // ── Achievements ──────────────────────────────────────────────────

        private void BuildAchievementRows()
        {
            if (_rowsBuilt || achievementContent == null || achievementRowTemplate == null) return;
            _rowsBuilt = true;

            achievementRowTemplate.gameObject.SetActive(false);
            var list = AchievementService.All;

            const float rowH = 132f, gap = 14f;
            for (int i = 0; i < list.Count; i++)
            {
                RectTransform row = Instantiate(achievementRowTemplate, achievementContent);
                row.gameObject.SetActive(true);
                row.name = "Ach_" + list[i].Id;
                row.anchorMin = new Vector2(0.5f, 1f);
                row.anchorMax = new Vector2(0.5f, 1f);
                row.pivot     = new Vector2(0.5f, 1f);
                row.anchoredPosition = new Vector2(0f, -(i * (rowH + gap)));
            }

            achievementContent.sizeDelta = new Vector2(achievementContent.sizeDelta.x,
                                                       list.Count * (rowH + gap) + gap);
            RefreshAchievementRows();
        }

        private void RefreshAchievementRows()
        {
            if (achievementContent == null) return;
            var theme = UITheme.Current;
            var d = StatsService.Data;
            var list = AchievementService.All;

            for (int i = 0; i < list.Count; i++)
            {
                Transform row = achievementContent.Find("Ach_" + list[i].Id);
                if (row == null) continue;

                Achievement a = list[i];
                bool done = a.IsUnlocked(d);

                var group = row.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = done ? 1f : 0.62f;

                var card = row.GetComponent<ClayGraphic>();
                if (card != null)
                {
                    card.color = done ? theme.panel : theme.locked;
                    card.SetVerticesDirty();
                }

                SetText(row, "Title_Text", a.Title,       done ? theme.ink    : theme.inkDim);
                SetText(row, "Desc_Text",  a.Description, theme.inkDim);
                SetText(row, "Count_Text", a.Current(d) + " / " + a.Target,
                        done ? theme.success : theme.inkDim);

                var badge = row.Find("Badge");
                if (badge != null)
                {
                    var g = badge.GetComponent<ClayGraphic>();
                    if (g != null)
                    {
                        g.color = done ? theme.gold : theme.recessed;
                        g.SetVerticesDirty();
                    }
                    var star    = badge.Find("Star");
                    var padlock = badge.Find("Lock");
                    if (star    != null) star.gameObject.SetActive(done);
                    if (padlock != null) padlock.gameObject.SetActive(!done);
                }

                var bar  = row.Find("Bar");
                var fill = bar != null ? bar.Find("Fill") : null;
                if (fill != null)
                {
                    var fillRt   = (RectTransform)fill;
                    float track  = ((RectTransform)bar).rect.width;
                    fillRt.sizeDelta = new Vector2(track * Mathf.Clamp01(a.Fraction(d)),
                                                   fillRt.sizeDelta.y);

                    var fillClay = fill.GetComponent<ClayGraphic>();
                    if (fillClay != null)
                    {
                        fillClay.color = done ? theme.success : theme.secondary;
                        fillClay.SetVerticesDirty();
                    }
                }
            }
        }

        private static void SetText(Transform row, string child, string value, Color colour)
        {
            var t = row.Find(child);
            if (t == null) return;
            var label = t.GetComponent<TextMeshProUGUI>();
            if (label == null) return;
            label.text  = value;
            label.color = UITheme.Lin(colour);
        }

        // ── Navigation ────────────────────────────────────────────────────

        private void OpenEditName()
        {
            var modal = UIModal.Open("EditProfile");
            if (modal == null) return;
            var edit = modal.GetComponent<EditProfileModal>();
            if (edit != null) edit.Configure(Refresh);
        }

        private void OpenStats()
        {
            ScreenManager.Instance.Push(ScreenId.PlayerStats);
        }

        private void HandleBack()
        {
            if (_onAchievements) { SetTab(false); return; }
            ScreenManager.Instance.Pop();
        }

        public override bool OnBack()
        {
            if (!_onAchievements) return false;
            SetTab(false);
            return true;
        }
    }
}
