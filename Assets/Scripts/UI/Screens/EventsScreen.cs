using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Live Events Hub Screen.
    /// Lists active, upcoming, and completed live events with countdowns, leaderboards, and access.
    /// Uses UITheme design system and clay components.
    /// </summary>
    public class EventsScreen : UIScreen
    {
        public enum EventStatus { Active, Upcoming, Completed }

        [System.Serializable]
        public class EventItem
        {
            public string id;
            public string title;
            public string subtitle;
            public string description;
            public string countdownText;
            public string rulesSummary;
            public int entryFeeCoins;
            public int rewardEventTokens;
            public EventStatus status;
        }

        [Header("Wiring")]
        public ClayButton backButton;

        [Header("Status Tabs")]
        public ClayButton activeTab;
        public ClayButton upcomingTab;
        public ClayButton completedTab;

        [Header("Container & Template")]
        public Transform eventsContainer;
        public RectTransform eventCardTemplate;

        [Header("Navigation Bar")]
        public HomeNavBar navBar;

        private EventStatus _selectedStatus = EventStatus.Active;
        private readonly List<EventItem> _events = new List<EventItem>();
        private readonly List<GameObject> _spawnedCards = new List<GameObject>();

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (activeTab != null) activeTab.onClick.AddListener(() => SwitchStatus(EventStatus.Active));
            if (upcomingTab != null) upcomingTab.onClick.AddListener(() => SwitchStatus(EventStatus.Upcoming));
            if (completedTab != null) completedTab.onClick.AddListener(() => SwitchStatus(EventStatus.Completed));

            LoadEventsData();
        }

        public override void OnShow(object args)
        {
            if (navBar != null) navBar.SetActiveTab(HomeNavBar.Tab.Events);
            RefreshUI();
        }

        private void LoadEventsData()
        {
            _events.Clear();
            _events.Add(new EventItem
            {
                id = "evt_gold_rush",
                title = "GOLD RUSH CHAMPIONSHIP",
                subtitle = "Double Coins & Safe Zone Frenzy",
                description = "Compete in fast 2-player matches on a golden board with no safe zones.",
                countdownText = "ENDS IN 3D 14H",
                rulesSummary = "2-Player • No Safe Zones • 2x Coins",
                entryFeeCoins = 100,
                rewardEventTokens = 500,
                status = EventStatus.Active
            });

            _events.Add(new EventItem
            {
                id = "evt_speed_dice",
                title = "TURBO DICE ARENA",
                subtitle = "10-Second Turn Clocks",
                description = "Test your reflexes with quick turn timers and bonus rolls on 6s.",
                countdownText = "ENDS IN 5D 22H",
                rulesSummary = "4-Player • 10s Turn Limit • Turbo Speed",
                entryFeeCoins = 250,
                rewardEventTokens = 1200,
                status = EventStatus.Active
            });

            _events.Add(new EventItem
            {
                id = "evt_grand_masters",
                title = "GRAND MASTERS INVITATIONAL",
                subtitle = "Top 100 Leaderboard Qualifier",
                description = "High stakes tournament against elite AI and veteran masters.",
                countdownText = "STARTS IN 8 DAYS",
                rulesSummary = "Classic 4P • Elo Ranked",
                entryFeeCoins = 500,
                rewardEventTokens = 3000,
                status = EventStatus.Upcoming
            });

            _events.Add(new EventItem
            {
                id = "evt_summer_kickoff",
                title = "SUMMER CARNIVAL CUP",
                subtitle = "Season Opening Tournament",
                description = "The inaugural summer cup has concluded.",
                countdownText = "COMPLETED",
                rulesSummary = "Classic 4P",
                entryFeeCoins = 0,
                rewardEventTokens = 0,
                status = EventStatus.Completed
            });
        }

        private void SwitchStatus(EventStatus status)
        {
            _selectedStatus = status;
            var theme = UITheme.Current;

            if (activeTab != null && activeTab.Clay != null)
                activeTab.Clay.color = (_selectedStatus == EventStatus.Active) ? theme.secondary : theme.neutral;
            if (upcomingTab != null && upcomingTab.Clay != null)
                upcomingTab.Clay.color = (_selectedStatus == EventStatus.Upcoming) ? theme.secondary : theme.neutral;
            if (completedTab != null && completedTab.Clay != null)
                completedTab.Clay.color = (_selectedStatus == EventStatus.Completed) ? theme.secondary : theme.neutral;

            RefreshUI();
        }

        public void RefreshUI()
        {
            foreach (var go in _spawnedCards) if (go != null) Destroy(go);
            _spawnedCards.Clear();

            if (eventCardTemplate == null || eventsContainer == null) return;
            var theme = UITheme.Current;

            foreach (var evt in _events)
            {
                if (evt.status != _selectedStatus) continue;

                var cardObj = Instantiate(eventCardTemplate.gameObject, eventsContainer);
                cardObj.SetActive(true);
                _spawnedCards.Add(cardObj);

                var title = cardObj.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                var sub = cardObj.transform.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
                var timer = cardObj.transform.Find("TimerPill/TimerLabel")?.GetComponent<TextMeshProUGUI>();
                var reward = cardObj.transform.Find("RewardLabel")?.GetComponent<TextMeshProUGUI>();
                var rules = cardObj.transform.Find("RulesChip/RulesLabel")?.GetComponent<TextMeshProUGUI>();
                var actionBtn = cardObj.transform.Find("ActionBtn")?.GetComponent<ClayButton>();
                var actionText = actionBtn != null ? actionBtn.GetComponentInChildren<TextMeshProUGUI>() : null;

                if (title != null) title.text = evt.title;
                if (sub != null) sub.text = evt.subtitle;
                if (timer != null) timer.text = evt.countdownText;
                if (reward != null) reward.text = "WIN: +" + evt.rewardEventTokens + " EVENT TOKENS";
                if (rules != null) rules.text = evt.rulesSummary;

                if (actionBtn != null && actionText != null)
                {
                    if (evt.status == EventStatus.Active)
                    {
                        actionBtn.interactable = true;
                        if (actionBtn.Clay != null) actionBtn.Clay.color = theme.primary;
                        actionText.text = "VIEW EVENT";
                        EventItem captured = evt;
                        actionBtn.onClick.RemoveAllListeners();
                        actionBtn.onClick.AddListener(() =>
                        {
                            ScreenManager.Instance.Push(ScreenId.EventDetail, captured);
                        });
                    }
                    else if (evt.status == EventStatus.Upcoming)
                    {
                        actionBtn.interactable = true;
                        if (actionBtn.Clay != null) actionBtn.Clay.color = theme.neutral;
                        actionText.text = "PREVIEW RULES";
                        EventItem captured = evt;
                        actionBtn.onClick.RemoveAllListeners();
                        actionBtn.onClick.AddListener(() =>
                        {
                            ScreenManager.Instance.Push(ScreenId.EventDetail, captured);
                        });
                    }
                    else
                    {
                        actionBtn.interactable = false;
                        if (actionBtn.Clay != null) actionBtn.Clay.color = theme.locked;
                        actionText.text = "ENDED";
                    }
                }
            }
        }
    }
}
