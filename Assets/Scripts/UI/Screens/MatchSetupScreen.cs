using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// One setup screen for all three ways to play, because the choices overlap almost entirely
    /// and three near-identical screens would drift apart the first time one of them changed.
    /// Sections show and hide per mode:
    ///
    ///   QUICK MATCH  — 1v1 over four houses. Houses are dealt by the game, so there is nothing
    ///                  to choose but the stake. The opponent comes from <see cref="BotRoster"/>
    ///                  until there is a server.
    ///   VS COMPUTER  — pick how many computers, which house is yours, how hard they play, and
    ///                  rename them if you like.
    ///   PASS &amp; PLAY  — one phone, a seat per person: house and name for each.
    ///
    /// Pass-and-play never costs energy and never stakes NX Points: it is people playing on one
    /// device, and charging for that fights the genre. The multiplier section hides entirely.
    /// </summary>
    public class MatchSetupScreen : UIScreen
    {
        public enum Flavour { QuickMatch, VsComputer, PassAndPlay }

        private static readonly GameManager.PlayerColor[] Houses =
        {
            GameManager.PlayerColor.RED,
            GameManager.PlayerColor.GREEN,
            GameManager.PlayerColor.YELLOW,
            GameManager.PlayerColor.BLUE,
        };

        private static readonly string[] HouseNames = { "RED", "GREEN", "YELLOW", "BLUE" };

        [Header("Header")]
        public Button backButton;
        public TextMeshProUGUI title;

        [Header("Sections")]
        public RectTransform playersSection;
        public RectTransform houseSection;
        public RectTransform difficultySection;
        public RectTransform seatsSection;
        public RectTransform multiplierSection;
        public RectTransform teamSection;

        [Header("Players (2 / 3 / 4)")]
        public Button[] playerCountButtons;
        public Image[] playerCountBackgrounds;
        public TextMeshProUGUI[] playerCountLabels;

        [Header("House")]
        public Button[] houseButtons;
        public Image[] houseBackgrounds;
        public Image[] houseGlows;

        [Header("Difficulty")]
        public Button[] difficultyButtons;
        public Image[] difficultyBackgrounds;
        public TextMeshProUGUI[] difficultyLabels;

        [Header("Seats")]
        public RectTransform seatContent;
        public SeatRow seatTemplate;

        [Header("Team readout (quick match)")]
        public Image[] teamYourPawns;
        public Image[] teamOpponentPawns;
        public TextMeshProUGUI teamBlurb;

        [Header("Multiplier")]
        public RectTransform multiplierGrid;
        public MultiplierTile multiplierTemplate;
        public TextMeshProUGUI stakeSummary;
        public TextMeshProUGUI capacityLabel;

        [Header("Footer")]
        public Button startButton;
        public TextMeshProUGUI startLabel;
        public TextMeshProUGUI energyLabel;

        [Header("Message")]
        public CanvasGroup toast;
        public TextMeshProUGUI toastText;

        [Header("Art")]
        public Sprite tileTan;
        public Sprite tileSelected;
        public Sprite tileLocked;
        public Sprite[] pawnSprites;        // red, green, yellow, blue

        [Tooltip("NX Points a match is worth before the multiplier.")]
        public int baseNxStake = 20;

        private Flavour _flavour = Flavour.VsComputer;
        private int _playerCount = 4;
        private int _houseIndex;
        private int _difficulty = 1;
        private int _multiplier = 1;
        private readonly List<SeatRow> _seats = new List<SeatRow>();
        private readonly List<MultiplierTile> _tiles = new List<MultiplierTile>();
        private Coroutine _toastRoutine;

        // ==================================================================================
        // lifecycle
        // ==================================================================================

        private void Awake()
        {
            if (seatTemplate != null) seatTemplate.gameObject.SetActive(false);
            if (multiplierTemplate != null) multiplierTemplate.gameObject.SetActive(false);
            if (toast != null) { toast.alpha = 0f; toast.blocksRaycasts = false; }

            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
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
                buttons[i].onClick.AddListener(() => onClick(index));
            }
        }

        public override void OnShow(object args)
        {
            if (args is Flavour f) _flavour = f;
            else if (args is LudoNXSession.Mode mode)
            {
                _flavour = mode == LudoNXSession.Mode.PassAndPlay ? Flavour.PassAndPlay
                         : mode == LudoNXSession.Mode.QuickMatch ? Flavour.QuickMatch
                         : Flavour.VsComputer;
            }

            _playerCount = _flavour == Flavour.QuickMatch ? 4 : 4;
            _multiplier = Mathf.Min(_multiplier, MultiplierService.Highest());
            if (_multiplier < 1) _multiplier = 1;

            BuildMultiplierTiles();
            RebuildSeats();
            Paint();
        }

        // ==================================================================================
        // seats
        // ==================================================================================

        /// <summary>
        /// One row per house that is not the player's. Names are pre-filled — with bot names for
        /// the computer, with "Player 2/3/4" for the phone — so the screen is playable without
        /// typing a thing, and typing is an option rather than a toll gate.
        /// </summary>
        private void RebuildSeats()
        {
            foreach (var row in _seats)
            {
                if (row != null) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
            }
            _seats.Clear();
            if (seatTemplate == null || seatContent == null) return;
            if (_flavour == Flavour.QuickMatch) return;

            var order = SeatOrder();
            var bots = _flavour == Flavour.VsComputer
                ? BotRoster.PickMany(order.Count, LudoNXSession.Instance != null ? LudoNXSession.Instance.NXPoints : 0)
                : null;

            for (int i = 0; i < order.Count; i++)
            {
                var row = Instantiate(seatTemplate, seatContent);
                row.gameObject.SetActive(true);
                int houseIndex = order[i];
                row.houseIndex = houseIndex;
                if (row.pawn != null && pawnSprites != null && houseIndex < pawnSprites.Length)
                    row.pawn.sprite = pawnSprites[houseIndex];
                if (row.houseLabel != null) row.houseLabel.text = HouseNames[houseIndex];

                string suggested = _flavour == Flavour.VsComputer && bots != null && i < bots.Count
                    ? bots[i].name
                    : $"Player {i + 2}";
                if (row.nameField != null)
                {
                    row.nameField.text = suggested;
                    row.nameField.characterLimit = 14;
                }
                if (row.styleLabel != null)
                {
                    row.styleLabel.text = _flavour == Flavour.VsComputer && bots != null && i < bots.Count
                        ? bots[i].StyleLabel
                        : "On this phone";
                }
                _seats.Add(row);
            }
        }

        /// <summary>Houses in play besides the player's, in board order.</summary>
        private List<int> SeatOrder()
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
        // multiplier
        // ==================================================================================

        private void BuildMultiplierTiles()
        {
            foreach (var t in _tiles)
            {
                if (t != null) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
            }
            _tiles.Clear();
            if (multiplierTemplate == null || multiplierGrid == null)
            {
                Debug.LogError($"[MatchSetup] Stake tiles cannot be built: template={multiplierTemplate}, grid={multiplierGrid}.");
                return;
            }

            foreach (int m in MultiplierService.All)
            {
                var tile = Instantiate(multiplierTemplate, multiplierGrid);
                tile.gameObject.SetActive(true);
                tile.multiplier = m;
                if (tile.label != null) tile.label.text = "x" + m;
                int captured = m;
                if (tile.button != null) tile.button.onClick.AddListener(() => OnPickMultiplier(captured));
                _tiles.Add(tile);
            }

            Debug.Log($"[MatchSetup] Built {_tiles.Count} stake tiles under {multiplierGrid.name} " +
                      $"(rect {multiplierGrid.rect.size}, active {multiplierGrid.gameObject.activeInHierarchy}).");
        }

        private void OnPickMultiplier(int m)
        {
            if (!MultiplierService.IsUnlocked(m))
            {
                Toast(MultiplierService.UnlockHint(m));
                return;
            }
            _multiplier = m;
            Paint();
        }

        // ==================================================================================
        // paint
        // ==================================================================================

        private void Paint()
        {
            bool quick = _flavour == Flavour.QuickMatch;
            bool local = _flavour == Flavour.PassAndPlay;

            if (title != null)
                title.text = quick ? "QUICK MATCH" : local ? "PASS & PLAY" : "VS COMPUTER";

            Show(playersSection, !quick);
            Show(houseSection, true);
            Show(difficultySection, _flavour == Flavour.VsComputer);
            Show(seatsSection, !quick);
            Show(teamSection, quick);
            Show(multiplierSection, !local);

            for (int i = 0; i < 3; i++)
            {
                bool on = _playerCount == i + 2;
                if (playerCountBackgrounds != null && i < playerCountBackgrounds.Length && playerCountBackgrounds[i] != null)
                    playerCountBackgrounds[i].sprite = on ? tileSelected : tileTan;
                if (playerCountLabels != null && i < playerCountLabels.Length && playerCountLabels[i] != null)
                    playerCountLabels[i].color = on ? new Color32(48, 32, 12, 255) : new Color32(255, 242, 220, 255);
            }

            for (int i = 0; i < 4; i++)
            {
                bool on = _houseIndex == i;
                if (houseGlows != null && i < houseGlows.Length && houseGlows[i] != null)
                    houseGlows[i].gameObject.SetActive(on);
                if (houseBackgrounds != null && i < houseBackgrounds.Length && houseBackgrounds[i] != null)
                    houseBackgrounds[i].sprite = on ? tileSelected : tileTan;
            }

            for (int i = 0; i < 3; i++)
            {
                bool on = _difficulty == i;
                if (difficultyBackgrounds != null && i < difficultyBackgrounds.Length && difficultyBackgrounds[i] != null)
                    difficultyBackgrounds[i].sprite = on ? tileSelected : tileTan;
                if (difficultyLabels != null && i < difficultyLabels.Length && difficultyLabels[i] != null)
                    difficultyLabels[i].color = on ? new Color32(48, 32, 12, 255) : new Color32(255, 242, 220, 255);
            }

            PaintTeam(quick);
            PaintMultiplier(local);
            PaintFooter(local);
        }

        /// <summary>Quick match is a diagonal each: you drive two houses, the opponent the other two.</summary>
        private void PaintTeam(bool quick)
        {
            if (!quick || teamSection == null) return;

            int a = _houseIndex, b = (_houseIndex + 2) % 4;
            int c = (_houseIndex + 1) % 4, d = (_houseIndex + 3) % 4;

            SetPawns(teamYourPawns, a, b);
            SetPawns(teamOpponentPawns, c, d);

            if (teamBlurb != null)
                teamBlurb.text = $"You play {HouseNames[a]} and {HouseNames[b]}.  Your opponent plays {HouseNames[c]} and {HouseNames[d]}.";
        }

        private void SetPawns(Image[] slots, int first, int second)
        {
            if (slots == null || pawnSprites == null) return;
            if (slots.Length > 0 && slots[0] != null && first < pawnSprites.Length) slots[0].sprite = pawnSprites[first];
            if (slots.Length > 1 && slots[1] != null && second < pawnSprites.Length) slots[1].sprite = pawnSprites[second];
        }

        private void PaintMultiplier(bool local)
        {
            if (local) return;

            foreach (var tile in _tiles)
            {
                bool unlocked = MultiplierService.IsUnlocked(tile.multiplier);
                bool chosen = unlocked && tile.multiplier == _multiplier;

                if (tile.background != null)
                    tile.background.sprite = !unlocked ? tileLocked : chosen ? tileSelected : tileTan;
                if (tile.label != null)
                    tile.label.color = chosen ? new Color32(48, 32, 12, 255)
                                    : unlocked ? new Color32(255, 242, 220, 255)
                                               : new Color32(226, 220, 210, 120);
                if (tile.lockIcon != null) tile.lockIcon.gameObject.SetActive(!unlocked);
                if (tile.glow != null) tile.glow.gameObject.SetActive(chosen);
            }

            if (stakeSummary != null)
            {
                int win = MultiplierService.PointsOnWin(baseNxStake, _multiplier);
                int loss = MultiplierService.PointsOnLoss(baseNxStake, _multiplier);
                stakeSummary.text = $"Win  +{win:n0} NX          Lose  -{loss:n0} NX";
            }

            if (capacityLabel != null)
            {
                int cap = MultiplierService.Capacity;
                int best = MultiplierService.Highest();
                capacityLabel.text = $"Max energy {cap:n0}  ·  your ceiling x{best}";
            }
        }

        private void PaintFooter(bool local)
        {
            if (energyLabel != null)
            {
                energyLabel.text = local
                    ? "Free — no energy on this phone"
                    : $"Costs 1 energy  ·  you have {EnergyService.Current}";
            }
            if (startLabel != null)
                startLabel.text = _flavour == Flavour.QuickMatch ? "FIND OPPONENT" : "START";
        }

        private static void Show(RectTransform rt, bool visible)
        {
            if (rt != null && rt.gameObject.activeSelf != visible) rt.gameObject.SetActive(visible);
        }

        // ==================================================================================
        // start
        // ==================================================================================

        private void OnStart()
        {
            var s = LudoNXSession.Instance;
            if (s == null) return;

            bool local = _flavour == Flavour.PassAndPlay;
            if (!local && EnergyService.Current < EnergyService.Rules.costPerMatch)
            {
                Toast("Out of energy — it refills over time, or play Pass & Play.");
                return;
            }

            s.pendingMode = _flavour == Flavour.PassAndPlay ? LudoNXSession.Mode.PassAndPlay
                          : _flavour == Flavour.QuickMatch ? LudoNXSession.Mode.QuickMatch
                          : LudoNXSession.Mode.OfflineQuick;

            s.pendingPlayerCount = _flavour == Flavour.QuickMatch ? 4 : _playerCount;
            s.pendingHumanColor = Houses[_houseIndex];
            s.pendingDifficulty = (LudoAI.Difficulty)Mathf.Clamp(_difficulty, 0, 2);
            s.pendingTeamPlay = _flavour == Flavour.QuickMatch;
            s.pendingMultiplier = local ? 1 : _multiplier;

            s.pendingSeatNames.Clear();
            foreach (var row in _seats)
            {
                string name = row.nameField != null ? row.nameField.text.Trim() : "";
                s.pendingSeatNames.Add(string.IsNullOrEmpty(name) ? HouseNames[row.houseIndex] : name);
            }

            if (_flavour == Flavour.QuickMatch)
            {
                // The searching screen picks the opponent, so the wait is doing real work.
                ScreenManager.Instance.Push(ScreenId.MatchFound, _multiplier);
                return;
            }

            s.pendingOpponentName = "";
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
