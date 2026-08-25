using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The shop. Sells energy now; dice skins once the art exists.
    ///
    /// Prices and titles come from RevenueCat when it has them, and fall back to the authored
    /// placeholders when it does not — which is always the case in the Editor, where the SDK runs
    /// a no-op backend. That fallback is deliberate: a shop that renders blank until a network
    /// call returns looks broken, and it would be untestable here.
    /// </summary>
    public class ShopScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton backButton;
        public TextMeshProUGUI titleLabel;

        [Header("Energy")]
        public TextMeshProUGUI energyCountLabel;
        public TextMeshProUGUI energyTimerLabel;
        // ClayGraphic, not Image: a GameObject carries exactly one Graphic and the pill
        // is already drawn by the clay shader.
        public ClayGraphic[] energyPips;

        [Header("Offers")]
        public List<ShopOfferRow> offers = new List<ShopOfferRow>();

        [Header("Restore")]
        public ClayButton restoreButton;

        private float _tick;

        public override void OnShow(object args)
        {
            RefreshEnergy();
            RefreshOffers();
        }

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (restoreButton != null) restoreButton.onClick.AddListener(OnRestore);

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
            // The countdown is the only thing on this screen that moves on its own. Once a
            // second is enough, and it keeps the string allocation off every frame.
            _tick += Time.unscaledDeltaTime;
            if (_tick < 1f) return;
            _tick = 0f;
            RefreshEnergy();
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

            for (int i = 0; i < energyPips.Length; i++)
            {
                if (energyPips[i] == null) continue;
                bool lit = i < now;
                energyPips[i].color = lit ? theme.primary : theme.locked;
                energyPips[i].SetVerticesDirty();
            }
        }

        private void RefreshOffers()
        {
            var rc = LudoNX.Shop.RevenueCatManager.Instance;

            foreach (var row in offers)
            {
                if (row == null) continue;

                string price = row.fallbackPrice;

                // Live price if the store gave us one. Never format it ourselves — the store
                // string already carries the player's own currency and local conventions.
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
                // Honest message rather than a dead button. In the Editor this is always the
                // path taken, because the SDK has no real backend there.
                Toast.Instance.Show("The store is not available right now.");
                return;
            }

            rc.Buy(row.productId, ok =>
            {
                if (!ok) { Toast.Instance.Show("Purchase cancelled."); return; }

                // Consumables are granted here because RevenueCat does not track them as
                // entitlements — an entitlement is a thing you own, and energy is spent.
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
            if (rc == null) { Toast.Instance.Show("The store is not available right now."); return; }

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
