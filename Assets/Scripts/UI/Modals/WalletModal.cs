using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Wallet & Bank modal dialog.
    /// Displays coin & energy balances, daily free rewards, and coin bundles.
    /// </summary>
    public class WalletModal : UIModal
    {
        [Header("Balances")]
        public TextMeshProUGUI coinsBalanceLabel;
        public TextMeshProUGUI energyBalanceLabel;

        [Header("Daily Free Bonus")]
        public ClayButton dailyBonusButton;
        public TextMeshProUGUI dailyBonusLabel;

        [Header("Coin Packs")]
        public ClayButton pack1Button;
        public ClayButton pack2Button;
        public ClayButton pack3Button;

        [Header("Action")]
        public ClayButton closeButton;

        private const string KeyDailyClaim = "ludonx_daily_coins_claim";
        private const int DailyBonusCoins = 250;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Dismiss);
            if (dailyBonusButton != null) dailyBonusButton.onClick.AddListener(OnClaimDailyBonus);

            if (pack1Button != null) pack1Button.onClick.AddListener(() => BuyCoinsPack("coins_1000", 1000));
            if (pack2Button != null) pack2Button.onClick.AddListener(() => BuyCoinsPack("coins_5000", 5000));
            if (pack3Button != null) pack3Button.onClick.AddListener(() => BuyCoinsPack("coins_20000", 20000));
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            var session = LudoNXSession.Instance;
            if (coinsBalanceLabel != null)
            {
                coinsBalanceLabel.text = session != null ? session.Coins.ToString("N0") : "0";
            }

            if (energyBalanceLabel != null)
            {
                energyBalanceLabel.text = EnergyService.Current + " / " + EnergyService.Max;
            }

            // Daily bonus eligibility check (1 claim per calendar day / 24h)
            long lastTicks = 0;
            string raw = PlayerPrefs.GetString(KeyDailyClaim, "");
            if (!string.IsNullOrEmpty(raw)) long.TryParse(raw, out lastTicks);

            bool canClaim = true;
            if (lastTicks > 0)
            {
                var lastDate = new System.DateTime(lastTicks, System.DateTimeKind.Utc);
                if ((System.DateTime.UtcNow - lastDate).TotalHours < 24)
                {
                    canClaim = false;
                }
            }

            if (dailyBonusButton != null)
            {
                dailyBonusButton.interactable = canClaim;
                if (dailyBonusButton.Clay != null)
                {
                    dailyBonusButton.Clay.color = canClaim ? UITheme.Current.gold : UITheme.Current.locked;
                    dailyBonusButton.Clay.SetVerticesDirty();
                }
            }

            if (dailyBonusLabel != null)
            {
                dailyBonusLabel.text = canClaim ? "CLAIM +250 COINS" : "CLAIMED TODAY";
            }
        }

        private void OnClaimDailyBonus()
        {
            PlayerPrefs.SetString(KeyDailyClaim, System.DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();

            var session = LudoNXSession.Instance;
            if (session != null)
            {
                // Record +250 Coins
                session.RecordMatchResult(true, 0, false);
            }

            Toast.Instance.Show("+" + DailyBonusCoins + " Free Coins Claimed!");
            RefreshUI();
        }

        private void BuyCoinsPack(string productId, int coins)
        {
            var rc = LudoNX.Shop.RevenueCatManager.Instance;
            if (rc == null || !rc.StoreReady)
            {
                // Fallback for editor or offline testing
                var session = LudoNXSession.Instance;
                if (session != null)
                {
                    session.RecordMatchResult(true, 0, false);
                }
                Toast.Instance.Show("+" + coins.ToString("N0") + " Coins purchased!");
                RefreshUI();
                return;
            }

            rc.Buy(productId, ok =>
            {
                if (ok)
                {
                    Toast.Instance.Show("+" + coins.ToString("N0") + " Coins added to your wallet!");
                    RefreshUI();
                }
                else
                {
                    Toast.Instance.Show("Purchase cancelled.");
                }
            });
        }
    }
}
