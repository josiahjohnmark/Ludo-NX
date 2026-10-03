using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The player's profile: who they are, the ten level badges, and the sixteen career stats.
    ///
    /// Built from the user's profile_page reference by <c>LudoNXProfileBuilder</c>, with the grids
    /// four tiles across. Detailed statistics and records live on their own screens; this one is
    /// the player's competitive identity at a glance.
    /// </summary>
    public class PlayerProfileScreen : UIScreen
    {
        [Header("Header")]
        public Button backButton;

        [Header("Identity")]
        public Image levelBadge;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI idLabel;
        public TextMeshProUGUI joinedLabel;
        public TextMeshProUGUI tenureLabel;
        public Button shareButton;

        [Header("Badges")]
        public Image[] badgeSlots;
        public TextMeshProUGUI[] badgeLabels;
        public Button[] badgeButtons;

        [Header("Stats (16, in display order)")]
        public TextMeshProUGUI[] statValues;

        [Header("Badge detail popup")]
        public GameObject badgePopup;
        public Button badgePopupBlocker;
        public Image badgePopupIcon;
        public TextMeshProUGUI badgePopupTitle;
        public TextMeshProUGUI badgePopupBody;

        [Header("Art")]
        public Sprite[] badgeSprites;

        [Header("Message")]
        public CanvasGroup toast;
        public TextMeshProUGUI toastText;

        /// <summary>
        /// Badge N is earned by reaching level N + 1 from cumulative NX points.
        /// </summary>
        private static bool BadgeEarned(int index)
        {
            var s = LudoNXSession.Instance;
            int level = s != null ? s.Level : 1;
            return index < level;
        }

        private static string BadgeName(int index) =>
            index >= 0 && index < NXLevelService.Badges.Length ? NXLevelService.Badges[index].title : "BADGE";

        /// <summary>Stat tile labels, in the order the builder lays the tiles out.</summary>
        public static readonly string[] StatLabels =
        {
            "TOTAL GAMES\nPLAYED",     "TOTAL\nWINS",               "TOTAL\nLOSSES",           "WIN\nRATE",
            "CURRENT\nWIN STREAK",     "LONGEST\nWIN STREAK",       "TOKENS\nCAPTURED",        "TOKENS\nBROUGHT HOME",
            "AVG. CAPTURES\nPER GAME", "MOST CAPTURES\nIN ONE GAME", "MOST TOKENS HOME\nIN ONE GAME", "TOTAL\nNX POINTS",
            "AVG. NX POINTS\nPER GAME", "HIGHEST NX\nIN ONE GAME",  "TOURNAMENT\nWINS",        "TIMES\nCAPTURED",
        };

        private const string JoinedKey = "ludonx_joined";

        private Coroutine _toastRoutine;

        private void Awake()
        {
            if (badgePopup != null) badgePopup.SetActive(false);
            if (toast != null) { toast.alpha = 0f; toast.blocksRaycasts = false; }

            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (shareButton != null) shareButton.onClick.AddListener(OnShare);
            if (badgePopupBlocker != null) badgePopupBlocker.onClick.AddListener(CloseBadge);

            if (badgeButtons == null) return;
            for (int i = 0; i < badgeButtons.Length; i++)
            {
                if (badgeButtons[i] == null) continue;
                int index = i;
                badgeButtons[i].onClick.AddListener(() => OpenBadge(index));
            }
        }

        public override void OnShow(object args)
        {
            var s = LudoNXSession.Instance;
            int level = s != null ? s.Level : 1;

            if (nameLabel != null) nameLabel.text = s != null ? s.PlayerName : "Player";
            if (idLabel != null) idLabel.text = PlayerId(s);

            DateTime joined = JoinDate();
            if (joinedLabel != null)
                joinedLabel.text = "Joined on " + joined.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
            if (tenureLabel != null) tenureLabel.text = "(" + Tenure(joined, DateTime.Now) + ")";

            if (levelBadge != null && badgeSprites != null && badgeSprites.Length > 0)
                levelBadge.sprite = badgeSprites[Mathf.Clamp(level - 1, 0, badgeSprites.Length - 1)];   // the current level's badge, as on Home and Levels

            PaintBadges(level);
            PaintStats();
        }

        /// <summary>A stable id from the device so two installs do not show the same number.</summary>
        private static string PlayerId(LudoNXSession s)
        {
            int hash = SystemInfo.deviceUniqueIdentifier.GetHashCode();
            if (s != null) hash ^= s.PlayerName.GetHashCode();
            return Mathf.Abs(hash % 1000000000).ToString("000000000");
        }

        /// <summary>The first day this install opened the profile, remembered from then on.</summary>
        private static DateTime JoinDate()
        {
            string stored = PlayerPrefs.GetString(JoinedKey, null);
            if (!string.IsNullOrEmpty(stored) && long.TryParse(stored, out long bin))
            {
                var dt = DateTime.FromBinary(bin);
                if (dt.Year >= DateTime.Now.Year) return dt;
            }

            DateTime today = DateTime.Now.Date;
            PlayerPrefs.SetString(JoinedKey, today.ToBinary().ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            return today;
        }

        /// <summary>"6 yrs", "3 mos", "12 days", "today".</summary>
        private static string Tenure(DateTime joined, DateTime now)
        {
            int months = (now.Year - joined.Year) * 12 + now.Month - joined.Month;
            if (now.Day < joined.Day) months--;
            if (months >= 12) { int y = months / 12; return y + (y == 1 ? " yr" : " yrs"); }
            if (months >= 1) return months + (months == 1 ? " mo" : " mos");
            int days = (int)(now.Date - joined.Date).TotalDays;
            if (days <= 0) return "today";
            return days + (days == 1 ? " day" : " days");
        }

        private void PaintBadges(int level)
        {
            if (badgeSlots == null) return;

            for (int i = 0; i < badgeSlots.Length; i++)
            {
                bool earned = BadgeEarned(i);
                if (badgeSlots[i] != null)
                {
                    if (badgeSprites != null && i < badgeSprites.Length) badgeSlots[i].sprite = badgeSprites[i];
                    // Locked badges stay visible but drained, so the grid reads as a ladder.
                    badgeSlots[i].color = earned ? Color.white : new Color(0.55f, 0.58f, 0.66f, 1f);
                }
                if (badgeLabels != null && i < badgeLabels.Length && badgeLabels[i] != null)
                    badgeLabels[i].text = (i + 1).ToString(CultureInfo.InvariantCulture);
            }
        }

        private void PaintStats()
        {
            if (statValues == null) return;

            var c = StatsService.Data ?? new CareerStats();
            var values = new[]
            {
                Count(c.matches),
                Count(c.wins),
                Count(c.losses),
                (c.WinRate * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%",

                Count(c.currentStreak),
                Count(c.bestStreak),
                Count(c.captures),
                Count(c.tokensHome),

                c.CapturesPerGame.ToString("0.0", CultureInfo.InvariantCulture),
                Count(c.mostCapturesInGame),
                Count(c.mostTokensHomeInGame),
                Count(c.nxEarned),

                Count(Mathf.RoundToInt(c.NxPerGame)),
                Count(c.bestNxInGame),
                Count(c.tournamentWins),
                Count(c.timesCaptured),
            };

            for (int i = 0; i < statValues.Length && i < values.Length; i++)
            {
                if (statValues[i] != null) statValues[i].text = values[i];
            }
        }

        /// <summary>1,248 up to 99,999; then 148K and 12.4M so every number fits its tile.</summary>
        public static string Count(int n)
        {
            if (n >= 10000000) return (n / 1000000f).ToString("0.#", CultureInfo.InvariantCulture) + "M";
            if (n >= 1000000) return (n / 1000000f).ToString("0.##", CultureInfo.InvariantCulture) + "M";
            if (n >= 100000) return (n / 1000) + "K";
            return n.ToString("#,0", CultureInfo.InvariantCulture);
        }

        // ==================================================================================
        // interactions
        // ==================================================================================

        private void OpenBadge(int index)
        {
            if (badgePopup == null || index >= MissionService.Sets.Length) return;

            var s = LudoNXSession.Instance;
            int level = s != null ? s.Level : 1;
            bool earned = BadgeEarned(index);

            if (badgePopupIcon != null && badgeSprites != null && index < badgeSprites.Length)
            {
                badgePopupIcon.sprite = badgeSprites[index];
                badgePopupIcon.color = earned ? Color.white : new Color(0.55f, 0.58f, 0.66f, 1f);
            }
            if (badgePopupTitle != null)
                badgePopupTitle.text = BadgeName(index);
            if (badgePopupBody != null)
                badgePopupBody.text = earned
                    ? $"Earned by finishing all 5 missions of LEVEL {index + 1}."
                    : $"Finish all 5 missions of LEVEL {index + 1} to earn this.\nYou are on LEVEL {level}.";

            badgePopup.SetActive(true);
        }

        private void CloseBadge()
        {
            if (badgePopup != null) badgePopup.SetActive(false);
        }

        /// <summary>
        /// Shares a plain invite. No link yet — there is no web page and no account to link to,
        /// and a share that posts a dead URL is worse than one that posts nothing.
        /// </summary>
        private void OnShare()
        {
            var s = LudoNXSession.Instance;
            string text = $"I'm {(s != null ? s.PlayerName : "playing")} on Ludo NX — level {(s != null ? s.Level : 1)}. Play me!";
            GUIUtility.systemCopyBuffer = text;
            Toast("Copied — paste it anywhere");
        }

        public override bool OnBack()
        {
            if (badgePopup != null && badgePopup.activeSelf) { CloseBadge(); return true; }
            return false;
        }

        public void Toast(string message)
        {
            if (toast == null) return;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            toastText.text = message;
            _toastRoutine = StartCoroutine(ShowToast());
        }

        private System.Collections.IEnumerator ShowToast()
        {
            toast.alpha = 0f;
            float t = 0f;
            while (t < 0.18f) { t += Time.unscaledDeltaTime; toast.alpha = t / 0.18f; yield return null; }
            toast.alpha = 1f;
            yield return new WaitForSecondsRealtime(1.6f);
            t = 0f;
            while (t < 0.25f) { t += Time.unscaledDeltaTime; toast.alpha = 1f - t / 0.25f; yield return null; }
            toast.alpha = 0f;
            _toastRoutine = null;
        }
    }
}
