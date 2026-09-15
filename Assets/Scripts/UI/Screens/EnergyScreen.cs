using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Energy & Lives Full Screen.
    /// Retention/progression system screen showing life balances, recharge timer, ad refill, and packs.
    /// Uses UITheme design system and clay components.
    /// </summary>
    public class EnergyScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton backButton;

        [Header("Readouts")]
        public TextMeshProUGUI energyCountLabel;
        public TextMeshProUGUI nextTimerLabel;
        public TextMeshProUGUI fullTimerLabel;
        public ClayGraphic[] energyPips;

        [Header("Actions")]
        public ClayButton watchAdButton;
        public ClayButton refillCoinsButton;
        public ClayButton shopPassButton;

        private float _tick;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAd);
            if (refillCoinsButton != null) refillCoinsButton.onClick.AddListener(OnRefillCoins);
            if (shopPassButton != null) shopPassButton.onClick.AddListener(() => ScreenManager.Instance.Push(ScreenId.Shop));
        }

        public override void OnShow(object args)
        {
            RefreshUI();
        }

        private void OnEnable() { EnergyService.Changed += RefreshUI; }
        private void OnDisable() { EnergyService.Changed -= RefreshUI; }

        private void Update()
        {
            _tick += Time.unscaledDeltaTime;
            if (_tick < 1f) return;
            _tick = 0f;
            RefreshUI();
        }

        public void RefreshUI()
        {
            int current = EnergyService.Current;
            int max = EnergyService.Max;
            var theme = UITheme.Current;

            if (energyCountLabel != null) energyCountLabel.text = current + " / " + max;

            if (nextTimerLabel != null)
            {
                nextTimerLabel.text = EnergyService.IsFull
                    ? "MAX CAPACITY"
                    : "NEXT LIFE IN " + EnergyService.UntilNextText;
                nextTimerLabel.color = UITheme.Lin(EnergyService.IsFull ? theme.success : theme.inkMuted);
            }

            if (fullTimerLabel != null)
            {
                if (EnergyService.IsFull)
                {
                    fullTimerLabel.text = "All lives ready for match play!";
                }
                else
                {
                    int missing = max - current;
                    int totalMins = (int)(EnergyService.UntilNext.TotalMinutes + (missing - 1) * EnergyService.Rules.refillMinutes);
                    fullTimerLabel.text = "Full refill in ~" + totalMins + " mins";
                }
            }

            if (energyPips != null)
            {
                for (int i = 0; i < energyPips.Length; i++)
                {
                    if (energyPips[i] == null) continue;
                    bool lit = i < current;
                    energyPips[i].color = lit ? theme.primary : theme.locked;
                    energyPips[i].SetVerticesDirty();
                }
            }

            if (watchAdButton != null) watchAdButton.interactable = !EnergyService.IsFull;
            if (refillCoinsButton != null) refillCoinsButton.interactable = !EnergyService.IsFull;
        }

        private void OnWatchAd()
        {
            if (EnergyService.IsFull)
            {
                Toast.Instance.Show("Energy is already full!");
                return;
            }
            EnergyService.Grant(1, allowOverfill: false);
            Toast.Instance.Show("+1 Energy gained!");
            RefreshUI();
        }

        private void OnRefillCoins()
        {
            if (EnergyService.IsFull)
            {
                Toast.Instance.Show("Energy is already full!");
                return;
            }

            const int cost = 150;
            var session = LudoNXSession.Instance;
            if (session != null && session.TrySpendCoins(cost))
            {
                EnergyService.Grant(EnergyService.Max - EnergyService.Current, allowOverfill: false);
                Toast.Instance.Show("Energy fully restored!");
                RefreshUI();
            }
            else
            {
                Toast.Instance.Show("Not enough coins (requires " + cost + " coins).");
            }
        }
    }
}
