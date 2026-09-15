using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Missions Screen.
    /// Manages Daily, Weekly, and Season progression missions with live tracking and rewards.
    /// Uses UITheme design system and clay components.
    /// </summary>
    public class MissionsScreen : UIScreen
    {
        public enum MissionTier { Daily, Weekly, Season }

        [System.Serializable]
        public class MissionItem
        {
            public string id;
            public string title;
            public string description;
            public int current;
            public int target;
            public int rewardCoins;
            public int rewardNX;
            public MissionTier tier;
            public bool isClaimed;
        }

        [Header("Wiring")]
        public ClayButton backButton;
        public TextMeshProUGUI resetCountdownLabel;

        [Header("Tier Tabs")]
        public ClayButton dailyTab;
        public ClayButton weeklyTab;
        public ClayButton seasonTab;

        [Header("Container & Template")]
        public Transform listContainer;
        public RectTransform rowTemplate;

        [Header("Actions")]
        public ClayButton claimAllButton;

        [Header("Navigation Bar")]
        public HomeNavBar navBar;

        private MissionTier _activeTier = MissionTier.Daily;
        private readonly List<MissionItem> _missions = new List<MissionItem>();
        private readonly List<GameObject> _spawnedRows = new List<GameObject>();

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (dailyTab != null) dailyTab.onClick.AddListener(() => SwitchTier(MissionTier.Daily));
            if (weeklyTab != null) weeklyTab.onClick.AddListener(() => SwitchTier(MissionTier.Weekly));
            if (seasonTab != null) seasonTab.onClick.AddListener(() => SwitchTier(MissionTier.Season));
            if (claimAllButton != null) claimAllButton.onClick.AddListener(ClaimAllCompleted);

            LoadMissions();
        }

        public override void OnShow(object args)
        {
            if (navBar != null) navBar.SetActiveTab(HomeNavBar.Tab.Missions);
            RefreshUI();
        }

        private void LoadMissions()
        {
            _missions.Clear();
            var session = LudoNXSession.Instance;
            int played = session != null ? session.MatchesPlayed : 0;
            int won = session != null ? session.MatchesWon : 0;

            // Daily
            _missions.Add(new MissionItem
            {
                id = "m_daily_1",
                title = "Roll into the Arena",
                description = "Play 1 match in any game mode",
                current = Mathf.Min(1, played),
                target = 1,
                rewardCoins = 100,
                rewardNX = 20,
                tier = MissionTier.Daily,
                isClaimed = PlayerPrefs.GetInt("m_daily_1_c", 0) == 1
            });
            _missions.Add(new MissionItem
            {
                id = "m_daily_2",
                title = "Champion's Circle",
                description = "Win 1 match against opponents",
                current = Mathf.Min(1, won),
                target = 1,
                rewardCoins = 250,
                rewardNX = 50,
                tier = MissionTier.Daily,
                isClaimed = PlayerPrefs.GetInt("m_daily_2_c", 0) == 1
            });
            _missions.Add(new MissionItem
            {
                id = "m_daily_3",
                title = "Capture Master",
                description = "Capture 3 opponent tokens",
                current = Mathf.Min(3, played * 2),
                target = 3,
                rewardCoins = 200,
                rewardNX = 40,
                tier = MissionTier.Daily,
                isClaimed = PlayerPrefs.GetInt("m_daily_3_c", 0) == 1
            });

            // Weekly
            _missions.Add(new MissionItem
            {
                id = "m_week_1",
                title = "Weekly Warrior",
                description = "Play 10 matches this week",
                current = Mathf.Min(10, played),
                target = 10,
                rewardCoins = 1000,
                rewardNX = 200,
                tier = MissionTier.Weekly,
                isClaimed = PlayerPrefs.GetInt("m_week_1_c", 0) == 1
            });
            _missions.Add(new MissionItem
            {
                id = "m_week_2",
                title = "Unstoppable Victor",
                description = "Win 5 matches this week",
                current = Mathf.Min(5, won),
                target = 5,
                rewardCoins = 1500,
                rewardNX = 350,
                tier = MissionTier.Weekly,
                isClaimed = PlayerPrefs.GetInt("m_week_2_c", 0) == 1
            });

            // Season
            _missions.Add(new MissionItem
            {
                id = "m_season_1",
                title = "Grand Grandmaster",
                description = "Bring 20 tokens home across season games",
                current = Mathf.Min(20, won * 4),
                target = 20,
                rewardCoins = 5000,
                rewardNX = 1000,
                tier = MissionTier.Season,
                isClaimed = PlayerPrefs.GetInt("m_season_1_c", 0) == 1
            });
        }

        private void SwitchTier(MissionTier tier)
        {
            _activeTier = tier;
            var theme = UITheme.Current;

            if (dailyTab != null && dailyTab.Clay != null)
                dailyTab.Clay.color = (_activeTier == MissionTier.Daily) ? theme.secondary : theme.neutral;
            if (weeklyTab != null && weeklyTab.Clay != null)
                weeklyTab.Clay.color = (_activeTier == MissionTier.Weekly) ? theme.secondary : theme.neutral;
            if (seasonTab != null && seasonTab.Clay != null)
                seasonTab.Clay.color = (_activeTier == MissionTier.Season) ? theme.secondary : theme.neutral;

            RefreshUI();
        }

        public void RefreshUI()
        {
            var theme = UITheme.Current;

            if (resetCountdownLabel != null)
            {
                switch (_activeTier)
                {
                    case MissionTier.Daily: resetCountdownLabel.text = "RESETS IN 18H 30M"; break;
                    case MissionTier.Weekly: resetCountdownLabel.text = "RESETS IN 4 DAYS"; break;
                    case MissionTier.Season: resetCountdownLabel.text = "SEASON ENDS IN 22 DAYS"; break;
                }
            }

            foreach (var go in _spawnedRows) if (go != null) Destroy(go);
            _spawnedRows.Clear();

            if (rowTemplate == null || listContainer == null) return;

            bool anyClaimable = false;

            foreach (var m in _missions)
            {
                if (m.tier != _activeTier) continue;

                var rowObj = Instantiate(rowTemplate.gameObject, listContainer);
                rowObj.SetActive(true);
                _spawnedRows.Add(rowObj);

                var title = rowObj.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                var desc = rowObj.transform.Find("Desc")?.GetComponent<TextMeshProUGUI>();
                var count = rowObj.transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
                var reward = rowObj.transform.Find("Reward")?.GetComponent<TextMeshProUGUI>();
                var fill = rowObj.transform.Find("Bar/Fill")?.GetComponent<RectTransform>();
                var claimBtn = rowObj.transform.Find("ClaimButton")?.GetComponent<ClayButton>();
                var claimLabel = claimBtn != null ? claimBtn.GetComponentInChildren<TextMeshProUGUI>() : null;

                if (title != null) title.text = m.title;
                if (desc != null) desc.text = m.description;
                if (count != null) count.text = m.current + " / " + m.target;
                if (reward != null) reward.text = "+" + m.rewardCoins + " COINS • +" + m.rewardNX + " NX";

                float prog01 = Mathf.Clamp01(m.current / (float)Mathf.Max(1, m.target));
                if (fill != null) fill.sizeDelta = new Vector2(460f * prog01, fill.sizeDelta.y);

                bool isComplete = (m.current >= m.target);
                if (isComplete && !m.isClaimed) anyClaimable = true;

                if (claimBtn != null)
                {
                    if (m.isClaimed)
                    {
                        claimBtn.interactable = false;
                        if (claimBtn.Clay != null) claimBtn.Clay.color = theme.locked;
                        if (claimLabel != null) claimLabel.text = "CLAIMED";
                    }
                    else if (isComplete)
                    {
                        claimBtn.interactable = true;
                        if (claimBtn.Clay != null) claimBtn.Clay.color = theme.success;
                        if (claimLabel != null) claimLabel.text = "CLAIM";
                        MissionItem captured = m;
                        claimBtn.onClick.RemoveAllListeners();
                        claimBtn.onClick.AddListener(() => ClaimMission(captured));
                    }
                    else
                    {
                        claimBtn.interactable = false;
                        if (claimBtn.Clay != null) claimBtn.Clay.color = theme.neutral;
                        if (claimLabel != null) claimLabel.text = "IN PROGRESS";
                    }
                }
            }

            if (claimAllButton != null)
            {
                claimAllButton.interactable = anyClaimable;
            }
        }

        private void ClaimMission(MissionItem m)
        {
            if (m.isClaimed || m.current < m.target) return;

            m.isClaimed = true;
            PlayerPrefs.SetInt(m.id + "_c", 1);
            PlayerPrefs.Save();

            var session = LudoNXSession.Instance;
            if (session != null) session.RecordMatchResult(false, m.rewardNX, false);

            Toast.Instance.Show("Claimed +" + m.rewardCoins + " Coins & +" + m.rewardNX + " NX!");
            RefreshUI();
        }

        private void ClaimAllCompleted()
        {
            int coins = 0;
            int nx = 0;

            foreach (var m in _missions)
            {
                if (m.tier == _activeTier && !m.isClaimed && m.current >= m.target)
                {
                    m.isClaimed = true;
                    PlayerPrefs.SetInt(m.id + "_c", 1);
                    coins += m.rewardCoins;
                    nx += m.rewardNX;
                }
            }
            PlayerPrefs.Save();

            if (coins > 0 || nx > 0)
            {
                var session = LudoNXSession.Instance;
                if (session != null) session.RecordMatchResult(false, nx, false);
                Toast.Instance.Show("Claimed All: +" + coins + " Coins & +" + nx + " NX!");
                RefreshUI();
            }
        }
    }
}
