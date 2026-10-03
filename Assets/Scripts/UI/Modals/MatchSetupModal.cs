using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The VS Computer and Local Play pre-match cards — a popup over Home, built from the user's
    /// own reference (LOCAL_PLAY_CARD_ASSESTS / -1): mascot avatar, PLAYERS count, a compact house
    /// colour picker, a HOUSES list where row 1 is always the player, and — VS Computer only — a
    /// DIFFICULTY row. No stake here: stakes are an online-only concept, so this card never shows
    /// one, matching Pass &amp; Play's existing "free, no energy" rule.
    ///
    /// Row 1 of the HOUSES list is always the player (fixed name, tagged HM for VS Computer);
    /// every other row is a seat the game fills — with a bot for VS Computer, with another local
    /// player for Pass &amp; Play — and can be renamed by tapping it.
    /// </summary>
    public class MatchSetupModal : UIModal
    {
        public enum Flavour { VsComputer, PassAndPlay }

        private static readonly GameManager.PlayerColor[] Houses =
        {
            GameManager.PlayerColor.RED,
            GameManager.PlayerColor.GREEN,
            GameManager.PlayerColor.YELLOW,
            GameManager.PlayerColor.BLUE,
        };

        private static readonly string[] HouseNames = { "RED", "GREEN", "YELLOW", "BLUE" };

        [Header("Identity")]
        public TextMeshProUGUI title;

        [Header("Players (2 / 3 / 4)")]
        public Button[] playerCountButtons;
        public Image[] playerCountBackgrounds;
        public TextMeshProUGUI[] playerCountLabels;

        [Header("Your house")]
        public Button[] houseButtons;
        public Image[] houseSwatches;
        public Image[] houseRings;

        [Header("Difficulty (VS Computer only)")]
        public RectTransform difficultySection;
        public Button[] difficultyButtons;
        public Image[] difficultyBackgrounds;
        public TextMeshProUGUI[] difficultyLabels;

        [Header("Seats")]
        public RectTransform seatContent;
        public SeatRow seatTemplate;

        [Header("Footer")]
        public Button startButton;
        public TextMeshProUGUI startLabel;
        public TextMeshProUGUI energyLabel;

        [Header("Message")]
        public CanvasGroup toast;
        public TextMeshProUGUI toastText;

        [Header("Art")]
        public Sprite pillNormal;
        public Sprite pillSelected;
        public Sprite[] pawnSprites; // red, green, yellow, blue

        private Flavour _flavour = Flavour.VsComputer;
        private int _playerCount = 4;
        private int _houseIndex;
        private int _difficulty = 1;
        private readonly List<SeatRow> _seats = new List<SeatRow>();
        private Coroutine _toastRoutine;

        private void Awake()
        {
            if (seatTemplate != null) seatTemplate.gameObject.SetActive(false);
            if (toast != null) { toast.alpha = 0f; toast.blocksRaycasts = false; }
            if (startButton != null) startButton.onClick.AddListener(OnStart);

            Wire(playerCountButtons, i => { _playerCount = i + 2; RebuildSeats(); Paint(); });
            Wire(houseButtons, i => { _houseIndex = i; RebuildSeats(); Paint(); });
            Wire(difficultyButtons, i => { _difficulty = i; Paint(); });
        }

        private void Wire(Button[] buttons, System.Action<int> onClick)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                int index = i;
                buttons[i].onClick.AddListener(() =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                    onClick(index);
                });
            }
        }

        /// <summary>Opens the modal for one flavour. Loads Resources/UI/Modals/MatchSetup.prefab.</summary>
        public static MatchSetupModal OpenFor(Flavour flavour)
        {
            var modal = Open("MatchSetup") as MatchSetupModal;
            if (modal != null) modal.Configure(flavour);
            return modal;
        }

        private void Configure(Flavour flavour)
        {
            _flavour = flavour;
            _playerCount = 4;
            RebuildSeats();
            Paint();
        }

        // ==================================================================================
        // seats — row 1 is always the player; every other row is filled by the game
        // ==================================================================================

        private void RebuildSeats()
        {
            foreach (var row in _seats)
            {
                if (row != null) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
            }
            _seats.Clear();
            if (seatTemplate == null || seatContent == null) return;

            var others = OtherHouseOrder();
            var bots = _flavour == Flavour.VsComputer
                ? BotRoster.PickMany(others.Count, LudoNXSession.Instance != null ? LudoNXSession.Instance.NXPoints : 0)
                : null;

            for (int seat = 0; seat < _playerCount; seat++)
            {
                bool isYou = seat == 0;
                int houseIndex = isYou ? _houseIndex : others[seat - 1];

                var row = Instantiate(seatTemplate, seatContent);
                row.gameObject.SetActive(true);
                row.houseIndex = houseIndex;
                row.transform.SetAsLastSibling();

                if (row.numberLabel != null) row.numberLabel.text = (seat + 1) + ".";
                if (row.pawn != null && pawnSprites != null && houseIndex < pawnSprites.Length)
                    row.pawn.sprite = pawnSprites[houseIndex];

                if (row.roleTag != null)
                    row.roleTag.gameObject.SetActive(_flavour == Flavour.VsComputer);
                if (row.roleTag != null && _flavour == Flavour.VsComputer)
                    row.roleTag.text = isYou ? "HM" : "CP";

                bool editable = !isYou;
                if (row.nameLabel != null) row.nameLabel.gameObject.SetActive(!editable);
                if (row.nameField != null) row.nameField.gameObject.SetActive(editable);
                if (row.hintLabel != null) row.hintLabel.gameObject.SetActive(editable);

                if (isYou)
                {
                    var s = LudoNXSession.Instance;
                    if (row.nameLabel != null) row.nameLabel.text = s != null ? s.PlayerName : "You";
                }
                else
                {
                    int botIdx = seat - 1;
                    string suggested = _flavour == Flavour.VsComputer && bots != null && botIdx < bots.Count
                        ? bots[botIdx].name
                        : $"Player {seat + 1}";
                    if (row.nameField != null)
                    {
                        row.nameField.text = suggested;
                        row.nameField.characterLimit = 14;
                    }
                }

                _seats.Add(row);
            }
        }

        /// <summary>Houses in play besides the player's, in board order.</summary>
        private List<int> OtherHouseOrder()
        {
            var order = new List<int>();
            if (_playerCount >= 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i != _houseIndex) order.Add(i);
                }
                return order;
            }

            if (_playerCount == 2)
            {
                // Opposite corners: equal distance home, so neither side starts ahead.
                order.Add((_houseIndex + 2) % 4);
                return order;
            }

            for (int i = 0; i < 4 && order.Count < 2; i++)
            {
                if (i != _houseIndex) order.Add(i);
            }
            return order;
        }

        // ==================================================================================
        // paint
        // ==================================================================================

        private void Paint()
        {
            bool vsComputer = _flavour == Flavour.VsComputer;

            if (title != null) title.text = vsComputer ? "VS COMPUTER" : "LOCAL PLAY";
            if (difficultySection != null) difficultySection.gameObject.SetActive(vsComputer);

            for (int i = 0; i < 3; i++)
            {
                bool on = _playerCount == i + 2;
                if (playerCountBackgrounds != null && i < playerCountBackgrounds.Length && playerCountBackgrounds[i] != null)
                    playerCountBackgrounds[i].sprite = on ? pillSelected : pillNormal;
                if (playerCountLabels != null && i < playerCountLabels.Length && playerCountLabels[i] != null)
                    playerCountLabels[i].color = Color.black;
            }

            for (int i = 0; i < 4; i++)
            {
                bool on = _houseIndex == i;
                if (houseRings != null && i < houseRings.Length && houseRings[i] != null)
                    houseRings[i].gameObject.SetActive(on);
            }

            for (int i = 0; i < 3; i++)
            {
                bool on = _difficulty == i;
                if (difficultyBackgrounds != null && i < difficultyBackgrounds.Length && difficultyBackgrounds[i] != null)
                    difficultyBackgrounds[i].sprite = on ? pillSelected : pillNormal;
            }

            if (energyLabel != null)
            {
                energyLabel.text = vsComputer
                    ? $"Costs 1 energy  ·  you have {EnergyService.Current}"
                    : "Free — no energy on this phone";
            }
            if (startLabel != null) startLabel.text = "PLAY";
        }

        // ==================================================================================
        // start
        // ==================================================================================

        private void OnStart()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
            var s = LudoNXSession.Instance;
            if (s == null) return;

            bool local = _flavour == Flavour.PassAndPlay;
            if (!local && EnergyService.Current < EnergyService.Rules.costPerMatch)
            {
                Toast("Out of energy — it refills over time, or play Local Play.");
                return;
            }

            s.pendingMode = local ? LudoNXSession.Mode.PassAndPlay : LudoNXSession.Mode.OfflineQuick;
            s.pendingPlayerCount = _playerCount;
            s.pendingHumanColor = Houses[_houseIndex];
            s.pendingDifficulty = (LudoAI.Difficulty)Mathf.Clamp(_difficulty, 0, 2);
            s.pendingTeamPlay = false;
            s.pendingMultiplier = 1;

            s.pendingSeatNames.Clear();
            foreach (var row in _seats)
            {
                string name = row.nameField != null && row.nameField.gameObject.activeSelf
                    ? row.nameField.text.Trim()
                    : (row.nameLabel != null ? row.nameLabel.text.Trim() : "");
                s.pendingSeatNames.Add(string.IsNullOrEmpty(name) ? HouseNames[row.houseIndex] : name);
            }

            s.pendingOpponentName = "";
            Dismiss();
            ScreenManager.Instance.Push(ScreenId.MatchIntro);
        }

        // ==================================================================================
        // message
        // ==================================================================================

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
            yield return new WaitForSecondsRealtime(1.8f);
            t = 0f;
            while (t < 0.25f) { t += Time.unscaledDeltaTime; toast.alpha = 1f - t / 0.25f; yield return null; }
            toast.alpha = 0f;
            _toastRoutine = null;
        }
    }
}
