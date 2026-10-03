using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Canned quick-message phrases opened from the match action bar's message button —
    /// deliberately not free-text chat (match-message-tray.json's design_decision: unmoderated
    /// text between strangers is a store-review risk and a casual Ludo game gets abused in the
    /// first week; free text can come later, for accepted friends only).
    ///
    /// A tray like the emoji one: no scrim, no close X. The whole tray shares one 5 s cooldown
    /// (longer than emotes — text lands harder) rather than a per-row one, per spec.
    /// </summary>
    public class MatchMessageTray : UIModal
    {
        [Header("Tabs")]
        public Button friendlyTab;
        public Button reactionsTab;
        public Image friendlyTabBackground;
        public Image reactionsTabBackground;
        public GameObject friendlyList;
        public GameObject reactionsList;

        [Header("Rows — 6 per tab")]
        public Button[] friendlyButtons;
        public Button[] reactionsButtons;

        public TextMeshProUGUI cooldownHeader;

        public static readonly string[] Friendly =
        {
            "Good luck!", "Nice move.", "Well played.", "Good game.", "Your turn.", "One more round?",
        };

        public static readonly string[] Reactions =
        {
            "So close!", "Oh no.", "Lucky roll.", "I needed a six.", "Wow.", "Ha!",
        };

        private MatchHudView _hud;
        private float _cooldownUntil;
        private const float CooldownSeconds = 5f;
        private const float BubbleHoldSeconds = 2.2f;

        public void Configure(MatchHudView hud)
        {
            _hud = hud;

            Wire(friendlyButtons, Friendly);
            Wire(reactionsButtons, Reactions);

            if (friendlyTab != null) { friendlyTab.onClick.RemoveAllListeners(); friendlyTab.onClick.AddListener(() => SelectTab(true)); }
            if (reactionsTab != null) { reactionsTab.onClick.RemoveAllListeners(); reactionsTab.onClick.AddListener(() => SelectTab(false)); }

            SelectTab(true);
        }

        private void Wire(Button[] buttons, string[] phrases)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length && i < phrases.Length; i++)
            {
                if (buttons[i] == null) continue;
                string phrase = phrases[i];
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() => Fire(phrase));
            }
        }

        private void SelectTab(bool friendly)
        {
            if (friendlyList != null) friendlyList.SetActive(friendly);
            if (reactionsList != null) reactionsList.SetActive(!friendly);

            var selected = new Color32(252, 171, 30, 255);   // option amber — matches RULES elsewhere
            var unselected = new Color32(237, 226, 208, 255); // cream, matches the row plates
            if (friendlyTabBackground != null) friendlyTabBackground.color = friendly ? selected : unselected;
            if (reactionsTabBackground != null) reactionsTabBackground.color = friendly ? unselected : selected;
        }

        private void Update()
        {
            float remaining = Mathf.Max(0f, _cooldownUntil - Time.unscaledTime);
            bool cooling = remaining > 0f;

            SetRowsInteractable(friendlyButtons, !cooling);
            SetRowsInteractable(reactionsButtons, !cooling);

            if (cooldownHeader != null)
            {
                cooldownHeader.gameObject.SetActive(cooling);
                if (cooling) cooldownHeader.text = $"Wait {Mathf.CeilToInt(remaining)}s";
            }
        }

        private static void SetRowsInteractable(Button[] buttons, bool interactable)
        {
            if (buttons == null) return;
            foreach (var b in buttons)
                if (b != null) b.interactable = interactable;
        }

        private void Fire(string phrase)
        {
            if (Time.unscaledTime < _cooldownUntil) return;
            if (_hud == null) return;

            var sender = _hud.LocalSender();
            _hud.CardFor(sender)?.ShowTextBubble(phrase, BubbleHoldSeconds);

            _cooldownUntil = Time.unscaledTime + CooldownSeconds;
            Dismiss();
        }
    }
}
