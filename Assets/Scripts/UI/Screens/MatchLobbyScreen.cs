using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Match Lobby Screen for online & local room matchmaking.
    /// Manages room code, player slots (House colours & ready status), and launch action.
    /// Uses UITheme design system and clay components.
    /// </summary>
    public class MatchLobbyScreen : UIScreen
    {
        [System.Serializable]
        public class LobbySlot
        {
            public RectTransform slotRoot;
            public ClayGraphic houseBadge;
            public TextMeshProUGUI playerNameLabel;
            public TextMeshProUGUI statusLabel;
            public ClayGraphic hostCrown;
            public bool isOccupied;
            public bool isReady;
        }

        [Header("Wiring")]
        public ClayButton backButton;
        public TextMeshProUGUI roomCodeLabel;
        public ClayButton copyCodeButton;
        public TextMeshProUGUI modeRulesChip;

        [Header("Player Slots")]
        public List<LobbySlot> playerSlots = new List<LobbySlot>();

        [Header("Action")]
        public ClayButton startOrReadyButton;
        public TextMeshProUGUI startOrReadyText;
        public ClayButton inviteButton;

        private string _roomCode = "#7842";
        private bool _isHost = true;
        private bool _isReady = true;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (copyCodeButton != null) copyCodeButton.onClick.AddListener(OnCopyCode);
            if (inviteButton != null) inviteButton.onClick.AddListener(OnInvite);
            if (startOrReadyButton != null) startOrReadyButton.onClick.AddListener(OnStartOrReady);
        }

        public override void OnShow(object args)
        {
            _roomCode = "#" + UnityEngine.Random.Range(1000, 9999);
            if (roomCodeLabel != null) roomCodeLabel.text = "ROOM " + _roomCode;

            var session = LudoNXSession.Instance;
            string hostName = session != null ? session.PlayerName : "Player (You)";

            var theme = UITheme.Current;
            GameManager.PlayerColor[] colors = {
                GameManager.PlayerColor.RED,
                GameManager.PlayerColor.GREEN,
                GameManager.PlayerColor.YELLOW,
                GameManager.PlayerColor.BLUE
            };

            for (int i = 0; i < playerSlots.Count; i++)
            {
                var slot = playerSlots[i];
                if (slot == null) continue;

                if (slot.houseBadge != null)
                {
                    slot.houseBadge.color = theme.ForPlayer(colors[i % 4]);
                    slot.houseBadge.SetVerticesDirty();
                }

                if (i == 0)
                {
                    // Host slot (local user)
                    slot.isOccupied = true;
                    slot.isReady = true;
                    if (slot.playerNameLabel != null) slot.playerNameLabel.text = hostName;
                    if (slot.statusLabel != null)
                    {
                        slot.statusLabel.text = "HOST / READY";
                        slot.statusLabel.color = UITheme.Lin(theme.success);
                    }
                    if (slot.hostCrown != null) slot.hostCrown.gameObject.SetActive(true);
                }
                else
                {
                    // Mock open / waiting slot
                    slot.isOccupied = false;
                    slot.isReady = false;
                    if (slot.playerNameLabel != null) slot.playerNameLabel.text = "WAITING FOR PLAYER...";
                    if (slot.statusLabel != null)
                    {
                        slot.statusLabel.text = "OPEN SLOT";
                        slot.statusLabel.color = UITheme.Lin(theme.inkDim);
                    }
                    if (slot.hostCrown != null) slot.hostCrown.gameObject.SetActive(false);
                }
            }

            if (modeRulesChip != null)
            {
                modeRulesChip.text = (session != null && session.pendingSafeZone) ? "SAFE ZONE • 4P" : "CLASSIC • 4P";
            }

            if (startOrReadyText != null)
            {
                startOrReadyText.text = _isHost ? "START MATCH" : "READY";
            }
        }

        private void OnCopyCode()
        {
            GUIUtility.systemCopyBuffer = _roomCode;
            Toast.Instance.Show("Room code copied to clipboard: " + _roomCode);
        }

        private void OnInvite()
        {
            GUIUtility.systemCopyBuffer = _roomCode;
            Toast.Instance.Show("Share code " + _roomCode + " with your friends!");
        }

        private void OnStartOrReady()
        {
            if (_isHost)
            {
                // Check energy
                if (!EnergyService.TrySpendForMatch())
                {
                    UIModal.Open("EnergyModal");
                    return;
                }

                // Proceed into match intro
                ScreenManager.Instance.Push(ScreenId.MatchIntro);
            }
            else
            {
                _isReady = !_isReady;
                if (startOrReadyText != null) startOrReadyText.text = _isReady ? "CANCEL READY" : "READY";
            }
        }

        private void OnBackClicked()
        {
            var modal = (ConfirmModal)UIModal.Open("Confirm");
            if (modal != null)
            {
                modal.titleLabel.text = "LEAVE LOBBY?";
                modal.bodyLabel.text = "Are you sure you want to leave the room?";
                modal.confirmButton.onClick.AddListener(() =>
                {
                    modal.Dismiss();
                    ScreenManager.Instance.Pop();
                });
            }
            else
            {
                ScreenManager.Instance.Pop();
            }
        }

        public override bool OnBack()
        {
            OnBackClicked();
            return true;
        }
    }
}
