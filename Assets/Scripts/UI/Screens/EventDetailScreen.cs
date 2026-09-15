using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Event Detail Screen.
    /// In-depth view for a live event: rules, objective ladder, live leaderboard rank, and match entry.
    /// Uses UITheme design system and clay components.
    /// </summary>
    public class EventDetailScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton backButton;

        [Header("Event Header")]
        public TextMeshProUGUI eventTitleLabel;
        public TextMeshProUGUI eventDatesLabel;
        public TextMeshProUGUI eventCountdownLabel;
        public TextMeshProUGUI eventDescriptionLabel;

        [Header("Rules & Objectives")]
        public TextMeshProUGUI rulesSummaryLabel;
        public TextMeshProUGUI entryFeeLabel;
        public TextMeshProUGUI rewardsSummaryLabel;

        [Header("Leaderboard Readout")]
        public TextMeshProUGUI playerRankLabel;
        public TextMeshProUGUI playerPointsLabel;

        [Header("Action")]
        public ClayButton enterMatchButton;
        public TextMeshProUGUI enterMatchText;

        private EventsScreen.EventItem _currentEvent;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (enterMatchButton != null) enterMatchButton.onClick.AddListener(OnEnterMatch);
        }

        public override void OnShow(object args)
        {
            if (args is EventsScreen.EventItem item)
            {
                _currentEvent = item;
            }
            RefreshUI();
        }

        public void RefreshUI()
        {
            var theme = UITheme.Current;

            string title = _currentEvent != null ? _currentEvent.title : "GOLD RUSH CHAMPIONSHIP";
            string desc = _currentEvent != null ? _currentEvent.description : "Compete in high-speed matches with bonus coin multipliers!";
            string rules = _currentEvent != null ? _currentEvent.rulesSummary : "2-Player • No Safe Zones • 2x Multiplier";
            string countdown = _currentEvent != null ? _currentEvent.countdownText : "ENDS IN 3D 14H";
            int fee = _currentEvent != null ? _currentEvent.entryFeeCoins : 100;
            int reward = _currentEvent != null ? _currentEvent.rewardEventTokens : 500;

            if (eventTitleLabel != null) eventTitleLabel.text = title;
            if (eventDatesLabel != null) eventDatesLabel.text = "AUG 25 - SEP 05";
            if (eventCountdownLabel != null) eventCountdownLabel.text = countdown;
            if (eventDescriptionLabel != null) eventDescriptionLabel.text = desc;

            if (rulesSummaryLabel != null) rulesSummaryLabel.text = rules;
            if (entryFeeLabel != null) entryFeeLabel.text = "ENTRY FEE: " + fee + " COINS";
            if (rewardsSummaryLabel != null) rewardsSummaryLabel.text = "1ST PLACE: +" + reward + " EVENT TOKENS + GOLD MEDAL";

            if (playerRankLabel != null) playerRankLabel.text = "RANK #42 (TOP 5%)";
            if (playerPointsLabel != null) playerPointsLabel.text = "1,850 PTS";

            if (enterMatchButton != null && enterMatchText != null)
            {
                bool isActive = (_currentEvent == null || _currentEvent.status == EventsScreen.EventStatus.Active);
                enterMatchButton.interactable = isActive;
                if (enterMatchButton.Clay != null) enterMatchButton.Clay.color = isActive ? theme.primary : theme.locked;
                enterMatchText.text = isActive ? "ENTER EVENT MATCH" : "EVENT CLOSED";
            }
        }

        private void OnEnterMatch()
        {
            // Verify energy & entry fee
            if (!EnergyService.TrySpendForMatch())
            {
                UIModal.Open("EnergyModal");
                return;
            }

            int fee = _currentEvent != null ? _currentEvent.entryFeeCoins : 100;
            var session = LudoNXSession.Instance;
            if (session != null && !session.TrySpendCoins(fee))
            {
                Toast.Instance.Show("Not enough coins for entry fee (" + fee + " coins).");
                return;
            }

            if (session != null)
            {
                session.pendingMode = LudoNXSession.Mode.OfflineQuick;
                session.pendingPlayerCount = 2;
                session.pendingSafeZone = false;
            }

            ScreenManager.Instance.Push(ScreenId.MatchIntro);
        }
    }
}
