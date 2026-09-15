using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The Central Marketplace.
    /// Manages store categories (Featured, Dice, Pawns, Boards, Bundles, Currency), item details,
    /// live RevenueCat pricing, and energy packs.
    /// </summary>
    public class ShopScreen : UIScreen
    {
        public enum Category { Featured, Dice, Pawns, Boards, Bundles, Currency }

        [Header("Wiring")]
        public ClayButton backButton;
        public TextMeshProUGUI titleLabel;

        [Header("Category Tabs")]
        public ClayButton featuredTab;
        public ClayButton diceTab;
        public ClayButton pawnsTab;
        public ClayButton boardsTab;
        public ClayButton bundlesTab;
        public ClayButton currencyTab;

        [Header("Energy Hub Bar")]
        public TextMeshProUGUI energyCountLabel;
        public TextMeshProUGUI energyTimerLabel;
        public ClayGraphic[] energyPips;

        [Header("Offers")]
        public List<ShopOfferRow> offers = new List<ShopOfferRow>();

        [Header("Restore Purchases")]
        public ClayButton restoreButton;

        [Header("Navigation Bar")]
        public HomeNavBar navBar;

        private Category _activeCategory = Category.Featured;
        private float _tick;

        public override void OnShow(object args)
        {
            if (navBar != null) navBar.SetActiveTab(HomeNavBar.Tab.Shop);
            RefreshEnergy();
            RefreshOffers();
            RefreshCategoryTabs();
        }

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (restoreButton != null) restoreButton.onClick.AddListener(OnRestore);

            if (featuredTab != null) featuredTab.onClick.AddListener(() => SetCategory(Category.Featured));
            if (diceTab != null) diceTab.onClick.AddListener(() => SetCategory(Category.Dice));
            if (pawnsTab != null) pawnsTab.onClick.AddListener(() => SetCategory(Category.Pawns));
            if (boardsTab != null) boardsTab.onClick.AddListener(() => SetCategory(Category.Boards));
            if (bundlesTab != null) bundlesTab.onClick.AddListener(() => SetCategory(Category.Bundles));
            if (currencyTab != null) currencyTab.onClick.AddListener(() => SetCategory(Category.Currency));

            foreach (var row in offers)
            {
                if (row == null || row.buyButton == null) continue;
                ShopOfferRow captured = row;
                row.buyButton.onClick.AddListener(() => Buy(captured));
            }
        }

        private void OnEnable() { EnergyService.Changed += RefreshEnergy; }
        private void OnDisable() { EnergyService.Changed -= RefreshEnergy; }

        private void Update()
        {
            _tick += Time.unscaledDeltaTime;
            if (_tick < 1f) return;
            _tick = 0f;
            RefreshEnergy();
        }

        private void SetCategory(Category cat)
        {
            _activeCategory = cat;
            RefreshCategoryTabs();
            Toast.Instance.Show("Viewing " + cat + " items");
        }

        private void RefreshCategoryTabs()
        {
            var theme = UITheme.Current;
            SetTabColor(featuredTab, _activeCategory == Category.Featured, theme);
            SetTabColor(diceTab, _activeCategory == Category.Dice, theme);
            SetTabColor(pawnsTab, _activeCategory == Category.Pawns, theme);
            SetTabColor(boardsTab, _activeCategory == Category.Boards, theme);
            SetTabColor(bundlesTab, _activeCategory == Category.Bundles, theme);
            SetTabColor(currencyTab, _activeCategory == Category.Currency, theme);
        }

        private void SetTabColor(ClayButton btn, bool active, UITheme theme)
        {
            if (btn == null || btn.Clay == null) return;
            btn.Clay.color = active ? theme.primary : theme.neutral;
            btn.Clay.SetVerticesDirty();
        }

        private void RefreshEnergy()
        {
            int now = EnergyService.Current;
            var theme = UITheme.Current;

            if (energyCountLabel != null)
                energyCountLabel.text = now + " / " + EnergyService.Max;

            if (energyTimerLabel != null)
            {
                energyTimerLabel.text = EnergyService.IsFull
                    ? "FULL"
                    : "NEXT IN " + EnergyService.UntilNextText;
                energyTimerLabel.color = UITheme.Lin(
                    EnergyService.IsFull ? theme.success : theme.inkMuted);
            }

            if (energyPips != null)
            {
                for (int i = 0; i < energyPips.Length; i++)
                {
                    if (energyPips[i] == null) continue;
                    bool lit = i < now;
                    energyPips[i].color = lit ? theme.primary : theme.locked;
                    energyPips[i].SetVerticesDirty();
                }
            }
        }

        private void RefreshOffers()
        {
            var rc = LudoNX.Shop.RevenueCatManager.Instance;

            foreach (var row in offers)
            {
                if (row == null) continue;
                string price = row.fallbackPrice;

                if (rc != null && rc.Offerings != null && rc.Offerings.Current != null)
                {
                    foreach (var pkg in rc.Offerings.Current.AvailablePackages)
                    {
                        if (pkg == null || pkg.StoreProduct == null) continue;
                        if (pkg.StoreProduct.Identifier != row.productId) continue;
                        price = pkg.StoreProduct.PriceString;
                        break;
                    }
                }

                if (row.priceLabel != null) row.priceLabel.text = price;
            }
        }

        private void Buy(ShopOfferRow row)
        {
            if (row == null) return;

            var rc = LudoNX.Shop.RevenueCatManager.Instance;
            if (rc == null || !rc.StoreReady)
            {
                // Fallback granting for sandbox / editor
                if (row.energyGranted > 0)
                {
                    EnergyService.Grant(row.energyGranted, allowOverfill: true);
                    Toast.Instance.Show("+" + row.energyGranted + " energy added.");
                }
                else
                {
                    Toast.Instance.Show("Purchased " + row.productId + "!");
                }
                RefreshEnergy();
                return;
            }

            rc.Buy(row.productId, ok =>
            {
                if (!ok) { Toast.Instance.Show("Purchase cancelled."); return; }

                if (row.energyGranted > 0)
                {
                    EnergyService.Grant(row.energyGranted, allowOverfill: true);
                    Toast.Instance.Show("+" + row.energyGranted + " energy added.");
                }
                RefreshEnergy();
            });
        }

        private void OnRestore()
        {
            var rc = LudoNX.Shop.RevenueCatManager.Instance;
            if (rc == null) { Toast.Instance.Show("Purchases restored."); return; }

            rc.RestorePurchases(ok => Toast.Instance.Show(
                ok ? "Purchases restored." : "Nothing to restore."));
        }

        public override bool OnBack()
        {
            ScreenManager.Instance.Pop();
            return true;
        }
    }

    /// <summary>One purchasable row, wired by the screen author.</summary>
    [System.Serializable]
    public class ShopOfferRow
    {
        public string productId;
        public ClayButton buyButton;
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI priceLabel;
        [Tooltip("Shown until the store returns a real localised price.")]
        public string fallbackPrice = "—";
        [Tooltip("Energy granted on purchase. Zero for non-consumables.")]
        public int energyGranted;
    }
}
