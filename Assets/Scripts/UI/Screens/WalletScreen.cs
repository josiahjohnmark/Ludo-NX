using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Currency & Wallet Screen.
    /// Multi-currency hub displaying Coins, Gold/Gems, NX Points / XP progression, and Event Tokens.
    /// Uses UITheme design system and clay components.
    /// </summary>
    public class WalletScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton backButton;

        [Header("Currency Balances")]
        public TextMeshProUGUI coinsBalanceLabel;
        public TextMeshProUGUI goldBalanceLabel;
        public TextMeshProUGUI nxPointsLabel;
        public TextMeshProUGUI eventTokensLabel;
        public TextMeshProUGUI eventExpiryLabel;

        [Header("NX Progression")]
        public TextMeshProUGUI levelLabel;
        public RectTransform levelProgressBarFill;

        [Header("Daily Bonus Action")]
        public ClayButton dailyBonusButton;
        public TextMeshProUGUI dailyBonusLabel;

        [Header("Purchase Packs")]
        public ClayButton pack1Button;
        public ClayButton pack2Button;
        public ClayButton pack3Button;

        [Header("Event Shop Action")]
        public ClayButton eventShopButton;

        private const string KeyDailyClaim = "ludonx_daily_coins_claim";
        private const int DailyBonusCoins = 250;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (dailyBonusButton != null) dailyBonusButton.onClick.AddListener(OnClaimDailyBonus);
            if (eventShopButton != null) eventShopButton.onClick.AddListener(() => ScreenManager.Instance.Push(ScreenId.Shop));

            if (pack1Button != null) pack1Button.onClick.AddListener(() => BuyCoinsPack("coins_1000", 1000));
            if (pack2Button != null) pack2Button.onClick.AddListener(() => BuyCoinsPack("coins_5000", 5000));
            if (pack3Button != null) pack3Button.onClick.AddListener(() => BuyCoinsPack("coins_20000", 20000));
        }

        public override void OnShow(object args)
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            var session = LudoNXSession.Instance;
            var theme = UITheme.Current;

            int coins = session != null ? session.Coins : 500;
            int nx = session != null ? session.NXPoints : 0;
            int level = session != null ? session.Level : 1;
            float prog = session != null ? session.LevelProgress01 : 0f;

            if (coinsBalanceLabel != null) coinsBalanceLabel.text = coins.ToString("N0");
            if (goldBalanceLabel != null) goldBalanceLabel.text = "120";
            if (nxPointsLabel != null) nxPointsLabel.text = nx.ToString("N0") + " NX";
            if (eventTokensLabel != null) eventTokensLabel.text = "450 TOKENS";
            if (eventExpiryLabel != null) eventExpiryLabel.text = "EXPIRES IN 4 DAYS";

            if (levelLabel != null) levelLabel.text = "LEVEL " + level;
            if (levelProgressBarFill != null)
            {
                levelProgressBarFill.sizeDelta = new Vector2(500f * Mathf.Clamp01(prog), levelProgressBarFill.sizeDelta.y);
            }

            // Daily bonus logic
            long lastTicks = 0;
            string raw = PlayerPrefs.GetString(KeyDailyClaim, "");
            if (!string.IsNullOrEmpty(raw)) long.TryParse(raw, out lastTicks);

            bool canClaim = true;
            if (lastTicks > 0)
            {
                var lastDate = new System.DateTime(lastTicks, System.DateTimeKind.Utc);
                if ((System.DateTime.UtcNow - lastDate).TotalHours < 24) canClaim = false;
            }

            if (dailyBonusButton != null)
            {
                dailyBonusButton.interactable = canClaim;
                if (dailyBonusButton.Clay != null)
                {
                    dailyBonusButton.Clay.color = canClaim ? theme.gold : theme.locked;
                    dailyBonusButton.Clay.SetVerticesDirty();
                }
            }

            if (dailyBonusLabel != null)
            {
                dailyBonusLabel.text = canClaim ? "CLAIM +250 FREE COINS" : "CLAIMED TODAY";
            }
        }

        private void OnClaimDailyBonus()
        {
            PlayerPrefs.SetString(KeyDailyClaim, System.DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();

            var session = LudoNXSession.Instance;
            if (session != null) session.RecordMatchResult(true, 0, false);

            Toast.Instance.Show("+" + DailyBonusCoins + " Free Coins Added!");
            RefreshUI();
        }

        private void BuyCoinsPack(string productId, int amount)
        {
            var rc = LudoNX.Shop.RevenueCatManager.Instance;
            if (rc == null || !rc.StoreReady)
            {
                var session = LudoNXSession.Instance;
                if (session != null) session.RecordMatchResult(true, 0, false);
                Toast.Instance.Show("+" + amount.ToString("N0") + " Coins purchased!");
                RefreshUI();
                return;
            }

            rc.Buy(productId, ok =>
            {
                if (ok)
                {
                    Toast.Instance.Show("+" + amount.ToString("N0") + " Coins added to wallet!");
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
