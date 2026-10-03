using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The online / quick-match pre-match card, from the user's reference (ONLINE MATCH SCREEN):
    /// mascot avatar, an ONLINE title, the EMOJI SELECTED room of animated emoji, one PLAY button
    /// on its cream plate with the stake badge in the plate's corner, and energy underneath.
    ///
    /// The badge IS the multiplier control — each tap steps the stake up the NX ladder
    /// (x1, x2, x5, x10 …) as far as the player's energy capacity allows, then wraps back to x1.
    /// There is no separate picker. Stakes exist only on this card; VS Computer and Local Play
    /// never stake NX.
    ///
    /// The emoji room is ambient, not a real matchmaking readout — there is no server yet (see
    /// <see cref="LudoNXSession.Mode.QuickMatch"/>), so all eight slots animate throughout.
    /// </summary>
    public class OnlineMatchModal : UIModal
    {
        /// <summary>One emoji's frame strip. A wrapper because Unity cannot serialise a jagged
        /// array (Sprite[][]) straight onto a field the Inspector or a builder script can fill.</summary>
        [System.Serializable]
        public class EmojiSet
        {
            public Sprite[] frames;
        }

        [Header("Identity")]
        public TextMeshProUGUI title;

        [Header("Room")]
        public FlipbookImage[] emojiSlots;
        public EmojiSet[] emojiSets;

        [Header("Stake")]
        public Button multiplierBadgeButton;
        public TextMeshProUGUI multiplierBadgeLabel;

        [Header("Footer")]
        public Button startButton;
        public TextMeshProUGUI energyLabel;

        [Header("Message")]
        public CanvasGroup toast;
        public TextMeshProUGUI toastText;

        private int _multiplier = 1;
        private Coroutine _toastRoutine;

        private void Awake()
        {
            if (toast != null) { toast.alpha = 0f; toast.blocksRaycasts = false; }
            if (multiplierBadgeButton != null) multiplierBadgeButton.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                StepMultiplier();
            });
            if (startButton != null) startButton.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                OnStart();
            });
        }

        public static OnlineMatchModal Open() => Open("OnlineMatch") as OnlineMatchModal;

        private void OnEnable()
        {
            // A stake left over from last time may have been locked since (capacity can't shrink
            // today, but the rules can change); never open above the current ceiling.
            _multiplier = Mathf.Clamp(_multiplier, 1, MultiplierService.Highest());
            SeedRoom();
            Paint();
        }

        // ==================================================================================
        // emoji room — ambient, always animating; see class remarks
        // ==================================================================================

        private void SeedRoom()
        {
            if (emojiSlots == null || emojiSets == null || emojiSets.Length == 0) return;

            // A fresh shuffle each time the card opens, so the room does not always look the same.
            var order = new List<int>();
            for (int i = 0; i < emojiSets.Length; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            for (int i = 0; i < emojiSlots.Length; i++)
            {
                if (emojiSlots[i] == null) continue;
                var set = emojiSets[order[i % order.Count]];
                emojiSlots[i].SetFrames(set != null ? set.frames : null);
            }
        }

        // ==================================================================================
        // stake — the badge steps the ladder
        // ==================================================================================

        /// <summary>
        /// Next rung up, or back to x1 once the player's ceiling is reached. When the ceiling is
        /// a lock rather than the top of the ladder, say what opens the next rung, so the wrap
        /// reads as "that's your limit for now" rather than as the button misbehaving.
        /// </summary>
        private void StepMultiplier()
        {
            int[] ladder = MultiplierService.All;
            int index = System.Array.IndexOf(ladder, _multiplier);
            int next = index >= 0 && index + 1 < ladder.Length ? ladder[index + 1] : ladder[0];

            if (next != ladder[0] && !MultiplierService.IsUnlocked(next))
            {
                Toast($"x{next}: {MultiplierService.UnlockHint(next)}");
                next = ladder[0];
            }

            _multiplier = next;
            Paint();
        }

        // ==================================================================================
        // paint
        // ==================================================================================

        private void Paint()
        {
            if (title != null) title.text = "QUICK MATCH";   // never ONLINE until real online play ships
            if (multiplierBadgeLabel != null) multiplierBadgeLabel.text = "X" + _multiplier;
            if (energyLabel != null) energyLabel.text = EnergyService.CountText;
        }

        // ==================================================================================
        // start
        // ==================================================================================

        private void OnStart()
        {
            var s = LudoNXSession.Instance;
            if (s == null) return;

            if (EnergyService.Current < EnergyService.Rules.costPerMatch)
            {
                Toast("Out of energy — it refills over time, or play Local Play.");
                return;
            }

            s.pendingMode = LudoNXSession.Mode.QuickMatch;
            s.pendingPlayerCount = 2;
            s.pendingTeamPlay = true;
            s.pendingMultiplier = _multiplier;
            s.pendingOpponentName = "";

            Dismiss();
            // The searching screen picks the opponent, so the wait is doing real work.
            ScreenManager.Instance.Push(ScreenId.MatchFound, _multiplier);
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
