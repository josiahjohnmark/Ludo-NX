using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Energy & Lives modal dialog.
    /// Shows current life balance, live refill timer, watch-ad bonus option, and store refill packs.
    /// Uses the existing deep table theme and clay styling.
    /// </summary>
    public class EnergyModal : UIModal
    {
        [Header("Readout")]
        public TextMeshProUGUI energyCountLabel;
        public TextMeshProUGUI timerLabel;
        public ClayGraphic[] energyPips;

        [Header("Buttons")]
        public ClayButton watchAdButton;
        public ClayButton refillCoinsButton;
        public ClayButton shopButton;
        public ClayButton closeButton;

        private float _tick;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Dismiss);
            if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAd);
            if (refillCoinsButton != null) refillCoinsButton.onClick.AddListener(OnRefillCoins);
            if (shopButton != null) shopButton.onClick.AddListener(OnOpenShop);
        }

        private void OnEnable()
        {
            EnergyService.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            EnergyService.Changed -= Refresh;
        }

        private void Update()
        {
            _tick += Time.unscaledDeltaTime;
            if (_tick < 1f) return;
            _tick = 0f;
            Refresh();
        }

        public void Refresh()
        {
            int current = EnergyService.Current;
            int max = EnergyService.Max;
            var theme = UITheme.Current;

            if (energyCountLabel != null)
                energyCountLabel.text = current + " / " + max;

            if (timerLabel != null)
            {
                timerLabel.text = EnergyService.IsFull
                    ? "MAX CAPACITY"
                    : "NEXT LIFE IN " + EnergyService.UntilNextText;
                timerLabel.color = UITheme.Lin(EnergyService.IsFull ? theme.success : theme.inkMuted);
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

            if (watchAdButton != null)
            {
                watchAdButton.interactable = !EnergyService.IsFull;
            }
        }

        private void OnWatchAd()
        {
            if (EnergyService.IsFull)
            {
                Toast.Instance.Show("Energy is already full!");
                return;
            }

            // Simulate ad completion & grant 1 energy
            EnergyService.Grant(1, allowOverfill: false);
            Toast.Instance.Show("+1 Energy granted!");
            Refresh();
        }

        private void OnRefillCoins()
        {
            if (EnergyService.IsFull)
            {
                Toast.Instance.Show("Energy is already full!");
                return;
            }

            const int refillCost = 150;
            var session = LudoNXSession.Instance;
            if (session != null && session.TrySpendCoins(refillCost))
            {
                EnergyService.Grant(EnergyService.Max - EnergyService.Current, allowOverfill: false);
                Toast.Instance.Show("Energy refilled to maximum!");
                Refresh();
            }
            else
            {
                Toast.Instance.Show("Not enough coins (requires " + refillCost + " coins).");
            }
        }

        private void OnOpenShop()
        {
            Dismiss();
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.Push(ScreenId.Shop);
            }
        }
    }
}
