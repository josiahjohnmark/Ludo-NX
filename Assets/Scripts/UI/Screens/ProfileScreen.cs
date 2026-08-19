using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Identity and achievements, as two tabs of one screen.
    ///
    /// The second tab used to be Statistics, which repeated the same numbers the identity tab
    /// already showed. Achievements earn the space instead: they are built from the same career
    /// stats, so nothing can drift, but they give the player something to chase rather than a
    /// second table of the same totals.
    /// </summary>
    public class ProfileScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton backButton;
        public ClayButton identityTab;
        public ClayButton achievementsTab;
        public ClayButton editButton;
        public GameObject identityPanel;
        public GameObject achievementsPanel;

        [Header("Identity")]
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI levelLabel;
        public TextMeshProUGUI winsValue;
        public TextMeshProUGUI matchesValue;
        public TextMeshProUGUI winRateValue;
        public TextMeshProUGUI streakValue;

        [Header("Achievements")]
        public RectTransform achievementContent;
        public RectTransform achievementRowTemplate;
        public TextMeshProUGUI achievementSummary;

        private bool _onAchievements;
        private bool _rowsBuilt;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (identityTab != null) identityTab.onClick.AddListener(() => SetTab(false));
            if (achievementsTab != null) achievementsTab.onClick.AddListener(() => SetTab(true));
            if (editButton != null) editButton.onClick.AddListener(OpenEditName);
        }

        public override void OnShow(object args)
        {
            SetTab((args as string) == "achievements");
            Refresh();
        }

        private void SetTab(bool achievements)
        {
            _onAchievements = achievements;
            var theme = UITheme.Current;
            if (identityPanel != null) identityPanel.SetActive(!achievements);
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

        private void Refresh()
        {
            var s = LudoNXSession.Instance;
            var d = StatsService.Data;

            if (nameLabel != null) nameLabel.text = s != null ? s.PlayerName : "Player";
            if (levelLabel != null) levelLabel.text = "LEVEL " + (s != null ? s.Level : 1);

            if (winsValue != null) winsValue.text = d.wins.ToString();
            if (matchesValue != null) matchesValue.text = d.matches.ToString();
            if (winRateValue != null) winRateValue.text = Mathf.RoundToInt(d.WinRate * 100f) + "%";
            if (streakValue != null) streakValue.text = d.bestStreak.ToString();

            if (achievementSummary != null)
                achievementSummary.text = AchievementService.UnlockedCount(d)
                                        + " of " + AchievementService.Total + " unlocked";

            if (_rowsBuilt) RefreshAchievementRows();
        }

        // ── Achievements ──────────────────────────────────────────────────────────────────

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
                row.pivot = new Vector2(0.5f, 1f);
                row.anchoredPosition = new Vector2(0f, -(i * (rowH + gap)));
            }

            // Size the scroll content to whatever the list actually needs.
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

                // Locked achievements are deliberately shaded right down. A goal you have not
                // reached should read as a silhouette of a reward, not as a live one — the
                // contrast between the two states is the whole point of showing them at all.
                var group = row.GetComponent<CanvasGroup>();
                // Shaded, but a goal you cannot read is not a goal. 0.45 pushed the titles to the
                // edge of legibility; this still reads as clearly "not yours yet" beside an
                // earned row at full strength.
                if (group != null) group.alpha = done ? 1f : 0.62f;

                var card = row.GetComponent<ClayGraphic>();
                if (card != null)
                {
                    card.color = done ? theme.panel : theme.locked;
                    card.SetVerticesDirty();
                }

                SetText(row, "Title_Text", a.Title, done ? theme.ink : theme.inkDim);
                SetText(row, "Desc_Text", a.Description, theme.inkDim);
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
                    // A padlock while locked, the reward star once earned.
                    var star = badge.Find("Star");
                    var padlock = badge.Find("Lock");
                    if (star != null) star.gameObject.SetActive(done);
                    if (padlock != null) padlock.gameObject.SetActive(!done);
                }

                var bar = row.Find("Bar");
                var fill = bar != null ? bar.Find("Fill") : null;
                if (fill != null)
                {
                    // Width, not fillAmount: the fill is a clay pill so its rounded ends match
                    // the track, and a zero-progress bar genuinely draws nothing. A uGUI Image
                    // with no sprite ignores fillAmount and paints the whole quad, which is why
                    // every locked row used to show a full bar.
                    var fillRt = (RectTransform)fill;
                    float track = ((RectTransform)bar).rect.width;
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
            label.text = value;
            label.color = UITheme.Lin(colour);
        }

        // ── Navigation ────────────────────────────────────────────────────────────────────

        private void OpenEditName()
        {
            var modal = UIModal.Open("EditProfile");
            if (modal == null) return;
            var edit = modal.GetComponent<EditProfileModal>();
            if (edit != null) edit.Configure(Refresh);
        }

        private void HandleBack()
        {
            if (_onAchievements) { SetTab(false); return; }
            ScreenManager.Instance.Pop();
        }

        public override bool OnBack()
        {
            if (!_onAchievements) return false;   // let the stack pop the screen
            SetTab(false);
            return true;                          // Achievements closes first
        }
    }
}
