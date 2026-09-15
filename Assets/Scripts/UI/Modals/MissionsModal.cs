using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Daily and Weekly Missions modal dialog.
    /// Lists active player objectives, progress bars, rewards, and claim action.
    /// Uses UITheme design system and clay components.
    /// </summary>
    public class MissionsModal : UIModal
    {
        [System.Serializable]
        public class MissionData
        {
            public string id;
            public string title;
            public string description;
            public int currentProgress;
            public int targetProgress;
            public int rewardCoins;
            public int rewardNX;
            public bool isClaimed;
            public bool isWeekly;
        }

        [Header("Tabs")]
        public ClayButton dailyTabButton;
        public ClayButton weeklyTabButton;

        [Header("Containers")]
        public Transform missionListContainer;
        public RectTransform missionRowTemplate;

        [Header("Readout & Action")]
        public TextMeshProUGUI headerTitleLabel;
        public TextMeshProUGUI timerResetLabel;
        public ClayButton claimAllButton;
        public ClayButton closeButton;

        private bool _isWeeklyView;
        private readonly List<MissionData> _missions = new List<MissionData>();
        private readonly List<GameObject> _spawnedRows = new List<GameObject>();

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Dismiss);
            if (dailyTabButton != null) dailyTabButton.onClick.AddListener(() => SwitchTab(false));
            if (weeklyTabButton != null) weeklyTabButton.onClick.AddListener(() => SwitchTab(true));
            if (claimAllButton != null) claimAllButton.onClick.AddListener(ClaimAllCompleted);

            LoadMockMissions();
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void LoadMockMissions()
        {
            _missions.Clear();
            var session = LudoNXSession.Instance;
            int played = session != null ? session.MatchesPlayed : 0;
            int won = session != null ? session.MatchesWon : 0;

            // Daily Missions
            _missions.Add(new MissionData
            {
                id = "d_play_1",
                title = "Roll into the Arena",
                description = "Play 1 match in any game mode",
                currentProgress = Mathf.Min(1, played),
                targetProgress = 1,
                rewardCoins = 100,
                rewardNX = 20,
                isClaimed = PlayerPrefs.GetInt("mission_d_play_1_claimed", 0) == 1,
                isWeekly = false
            });

            _missions.Add(new MissionData
            {
                id = "d_win_1",
                title = "Champion's Circle",
                description = "Win 1 match against opponents",
                currentProgress = Mathf.Min(1, won),
                targetProgress = 1,
                rewardCoins = 250,
                rewardNX = 50,
                isClaimed = PlayerPrefs.GetInt("mission_d_win_1_claimed", 0) == 1,
                isWeekly = false
            });

            _missions.Add(new MissionData
            {
                id = "d_play_3",
                title = "Dice Enthusiast",
                description = "Play 3 matches in total",
                currentProgress = Mathf.Min(3, played),
                targetProgress = 3,
                rewardCoins = 300,
                rewardNX = 75,
                isClaimed = PlayerPrefs.GetInt("mission_d_play_3_claimed", 0) == 1,
                isWeekly = false
            });

            // Weekly Missions
            _missions.Add(new MissionData
            {
                id = "w_win_5",
                title = "Master of Ludo",
                description = "Win 5 matches this week",
                currentProgress = Mathf.Min(5, won),
                targetProgress = 5,
                rewardCoins = 1000,
                rewardNX = 250,
                isClaimed = PlayerPrefs.GetInt("mission_w_win_5_claimed", 0) == 1,
                isWeekly = true
            });

            _missions.Add(new MissionData
            {
                id = "w_play_10",
                title = "Grand Tournament",
                description = "Complete 10 total games",
                currentProgress = Mathf.Min(10, played),
                targetProgress = 10,
                rewardCoins = 1500,
                rewardNX = 350,
                isClaimed = PlayerPrefs.GetInt("mission_w_play_10_claimed", 0) == 1,
                isWeekly = true
            });
        }

        private void SwitchTab(bool weekly)
        {
            _isWeeklyView = weekly;
            var theme = UITheme.Current;

            if (dailyTabButton != null && dailyTabButton.Clay != null)
            {
                dailyTabButton.Clay.color = !_isWeeklyView ? theme.secondary : theme.neutral;
                dailyTabButton.Clay.SetVerticesDirty();
            }

            if (weeklyTabButton != null && weeklyTabButton.Clay != null)
            {
                weeklyTabButton.Clay.color = _isWeeklyView ? theme.secondary : theme.neutral;
                weeklyTabButton.Clay.SetVerticesDirty();
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (timerResetLabel != null)
            {
                timerResetLabel.text = _isWeeklyView ? "RESETS IN 4 DAYS" : "RESETS IN 18H 30M";
            }

            foreach (var go in _spawnedRows)
            {
                if (go != null) Destroy(go);
            }
            _spawnedRows.Clear();

            if (missionRowTemplate == null || missionListContainer == null) return;

            var theme = UITheme.Current;
            bool anyClaimable = false;

            foreach (var m in _missions)
            {
                if (m.isWeekly != _isWeeklyView) continue;

                var row = Instantiate(missionRowTemplate.gameObject, missionListContainer);
                row.SetActive(true);
                _spawnedRows.Add(row);

                var title = row.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                var desc = row.transform.Find("Desc")?.GetComponent<TextMeshProUGUI>();
                var count = row.transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
                var reward = row.transform.Find("Reward")?.GetComponent<TextMeshProUGUI>();
                var fill = row.transform.Find("Bar/Fill")?.GetComponent<RectTransform>();
                var claimBtn = row.transform.Find("ClaimButton")?.GetComponent<ClayButton>();
                var claimText = claimBtn != null ? claimBtn.GetComponentInChildren<TextMeshProUGUI>() : null;

                if (title != null) title.text = m.title;
                if (desc != null) desc.text = m.description;
                if (count != null) count.text = m.currentProgress + " / " + m.targetProgress;
                if (reward != null) reward.text = "+" + m.rewardCoins + " COINS";

                float progress01 = Mathf.Clamp01(m.currentProgress / (float)Mathf.Max(1, m.targetProgress));
                if (fill != null)
                {
                    fill.sizeDelta = new Vector2(400f * progress01, fill.sizeDelta.y);
                }

                bool isComplete = m.currentProgress >= m.targetProgress;
                if (isComplete && !m.isClaimed) anyClaimable = true;

                if (claimBtn != null)
                {
                    if (m.isClaimed)
                    {
                        claimBtn.interactable = false;
                        if (claimBtn.Clay != null) claimBtn.Clay.color = theme.locked;
                        if (claimText != null) claimText.text = "CLAIMED";
                    }
                    else if (isComplete)
                    {
                        claimBtn.interactable = true;
                        if (claimBtn.Clay != null) claimBtn.Clay.color = theme.success;
                        if (claimText != null) claimText.text = "CLAIM";
                        MissionData captured = m;
                        claimBtn.onClick.RemoveAllListeners();
                        claimBtn.onClick.AddListener(() => ClaimMission(captured));
                    }
                    else
                    {
                        claimBtn.interactable = false;
                        if (claimBtn.Clay != null) claimBtn.Clay.color = theme.neutral;
                        if (claimText != null) claimText.text = "IN PROGRESS";
                    }
                }
            }

            if (claimAllButton != null)
            {
                claimAllButton.interactable = anyClaimable;
            }
        }

        private void ClaimMission(MissionData m)
        {
            if (m.isClaimed || m.currentProgress < m.targetProgress) return;

            m.isClaimed = true;
            PlayerPrefs.SetInt("mission_" + m.id + "_claimed", 1);
            PlayerPrefs.Save();

            // Grant rewards
            var session = LudoNXSession.Instance;
            if (session != null)
            {
                session.RecordMatchResult(false, m.rewardNX, false);
            }

            Toast.Instance.Show("Claimed +" + m.rewardCoins + " Coins & +" + m.rewardNX + " NX!");
            RefreshUI();
        }

        private void ClaimAllCompleted()
        {
            int totalCoins = 0;
            int totalNX = 0;

            foreach (var m in _missions)
            {
                if (m.isWeekly == _isWeeklyView && !m.isClaimed && m.currentProgress >= m.targetProgress)
                {
                    m.isClaimed = true;
                    PlayerPrefs.SetInt("mission_" + m.id + "_claimed", 1);
                    totalCoins += m.rewardCoins;
                    totalNX += m.rewardNX;
                }
            }
            PlayerPrefs.Save();

            if (totalCoins > 0 || totalNX > 0)
            {
                var session = LudoNXSession.Instance;
                if (session != null) session.RecordMatchResult(false, totalNX, false);
                Toast.Instance.Show("Claimed All: +" + totalCoins + " Coins & +" + totalNX + " NX!");
                RefreshUI();
            }
        }
    }
}
