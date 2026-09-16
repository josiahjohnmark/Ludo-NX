using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Daily missions. Three every day, one bonus chest for finishing all three, and a countdown
    /// to the next set.
    ///
    /// Deliberately daily only: no weekly or monthly ladder to fall behind on. A player who misses
    /// a day loses nothing but that day, which is the gentlest retention loop there is and the
    /// only one that works without a server.
    ///
    /// The reward is paid the moment CLAIM is tapped — no spinner. A claim is a local write, and
    /// faking a round-trip on something instant only makes the game feel slower.
    /// </summary>
    public class DailyMissionsScreen : UIScreen
    {
        [Header("Header")]
        public Button backButton;
        public TextMeshProUGUI resetLabel;

        [Header("Panel")]
        public TextMeshProUGUI panelTitle;
        public ScrollRect scroll;
        public RectTransform content;

        [Header("Templates (kept inactive)")]
        public MissionRowView rowTemplate;
        public MissionRowView bonusTemplate;

        [Header("Art")]
        public Sprite claimEnabled;
        public Sprite claimDisabled;
        public Sprite[] icons;          // matched to DailyMissionsData icon names
        public string[] iconNames;

        [Header("Message")]
        public CanvasGroup toast;
        public TextMeshProUGUI toastText;

        [Header("Feel")]
        [Tooltip("How long a claimed card pulses.")]
        public float claimPulse = 0.28f;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<MissionRowView> _rows = new List<MissionRowView>();
        private MissionRowView _bonusRow;
        private Coroutine _toastRoutine;
        private Coroutine _ticker;

        private void Awake()
        {
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (bonusTemplate != null) bonusTemplate.gameObject.SetActive(false);
            if (toast != null) { toast.alpha = 0f; toast.blocksRaycasts = false; }
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
        }

        public override void OnShow(object args)
        {
            Rebuild();
            _ticker = StartCoroutine(TickCountdown());
        }

        public override void OnHide()
        {
            if (_ticker != null) { StopCoroutine(_ticker); _ticker = null; }
            if (_toastRoutine != null) { StopCoroutine(_toastRoutine); _toastRoutine = null; }
        }

        // ==================================================================================
        // build
        // ==================================================================================

        private void Rebuild()
        {
            foreach (var go in _spawned)
            {
                if (go != null) { go.SetActive(false); Destroy(go); }
            }
            _spawned.Clear();
            _rows.Clear();
            _bonusRow = null;

            var missions = DailyMissionsData.Today();
            foreach (var m in missions)
            {
                var row = Spawn(rowTemplate);
                row.mission = m;
                _rows.Add(row);
                var captured = m;
                row.claimButton.onClick.AddListener(() => OnClaim(captured));
            }

            _bonusRow = Spawn(bonusTemplate);
            _bonusRow.claimButton.onClick.AddListener(OnClaimBonus);

            Refresh();
            if (panelTitle != null) panelTitle.text = "DAILY MISSIONS";
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private MissionRowView Spawn(MissionRowView template)
        {
            var row = Instantiate(template, content);
            row.gameObject.SetActive(true);
            _spawned.Add(row.gameObject);
            return row;
        }

        // ==================================================================================
        // state
        // ==================================================================================

        private void Refresh()
        {
            foreach (var row in _rows) Paint(row);
            PaintBonus();
            if (resetLabel != null) resetLabel.text = DailyMissionsData.ResetLabel();
        }

        private void Paint(MissionRowView row)
        {
            var m = row.mission;
            int done = DailyMissionsData.Progress(m);
            bool complete = DailyMissionsData.IsComplete(m);
            bool claimed = DailyMissionsData.IsClaimed(m);

            row.title.text = m.title;
            row.progressLabel.text = $"{done}/{m.target}";
            row.SetProgress(m.target > 0 ? done / (float)m.target : 0f);
            row.rewardLabel.text = $"+{m.rewardCoins}   +{m.rewardNx} NX";
            if (row.icon != null) row.icon.sprite = IconFor(m.icon);

            PaintClaim(row, complete && !claimed, claimed);
        }

        private void PaintBonus()
        {
            if (_bonusRow == null) return;

            int claimedCount = DailyMissionsData.ClaimedCount();
            bool ready = DailyMissionsData.AllClaimed();
            bool taken = DailyMissionsData.BonusClaimed;

            _bonusRow.title.text = "Finish all 3 missions";
            _bonusRow.progressLabel.text = $"{claimedCount}/3";
            _bonusRow.SetProgress(claimedCount / 3f);
            _bonusRow.rewardLabel.text = $"+{DailyMissionsData.BonusCoins}   +{DailyMissionsData.BonusNx} NX";

            PaintClaim(_bonusRow, ready && !taken, taken);
        }

        /// <summary>
        /// Three states, and only one of them is a button: ready to claim, already claimed, or
        /// still locked. A greyed-out control that still looks pressable is the classic way to
        /// make a player think the game is broken.
        /// </summary>
        private void PaintClaim(MissionRowView row, bool ready, bool claimed)
        {
            row.claimButton.interactable = ready;
            row.claimBackground.sprite = ready ? claimEnabled : claimDisabled;
            row.claimLabel.text = claimed ? "CLAIMED" : "CLAIM";
            row.claimLabel.color = ready ? new Color32(255, 242, 220, 255) : new Color32(236, 230, 220, 190);
            row.claimLabel.fontSize = claimed ? 13.5f : 16f;
            if (row.tick != null) row.tick.gameObject.SetActive(claimed);
            var tint = row.baseTint;
            row.card.color = claimed ? new Color(tint.r, tint.g, tint.b, 0.78f) : tint;
        }

        private Sprite IconFor(string name)
        {
            if (icons == null || iconNames == null) return null;
            for (int i = 0; i < iconNames.Length && i < icons.Length; i++)
            {
                if (iconNames[i] == name) return icons[i];
            }
            return icons.Length > 0 ? icons[0] : null;
        }

        // ==================================================================================
        // claiming
        // ==================================================================================

        private void OnClaim(DailyMissionsData.Mission m)
        {
            if (!DailyMissionsData.Claim(m)) return;
            Toast($"+{m.rewardCoins} coins   +{m.rewardNx} NX");
            foreach (var row in _rows)
            {
                if (row.mission == m) StartCoroutine(Pulse(row.layout));
            }
            Refresh();
        }

        private void OnClaimBonus()
        {
            if (!DailyMissionsData.ClaimBonus()) return;
            Toast($"Bonus chest!  +{DailyMissionsData.BonusCoins} coins   +{DailyMissionsData.BonusNx} NX");
            if (_bonusRow != null) StartCoroutine(Pulse(_bonusRow.layout));
            Refresh();
        }

        private IEnumerator Pulse(RectTransform rt)
        {
            if (rt == null) yield break;
            float t = 0f;
            while (t < claimPulse)
            {
                t += Time.unscaledDeltaTime;
                float k = t / claimPulse;
                float s = 1f + 0.05f * Mathf.Sin(k * Mathf.PI);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        // ==================================================================================
        // countdown and messages
        // ==================================================================================

        private IEnumerator TickCountdown()
        {
            while (true)
            {
                if (resetLabel != null) resetLabel.text = DailyMissionsData.ResetLabel();
                // The label only shows whole minutes, so once a second is plenty and costs nothing.
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        public void Toast(string message)
        {
            if (toast == null) return;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            toastText.text = message;
            _toastRoutine = StartCoroutine(ShowToast());
        }

        private IEnumerator ShowToast()
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
