using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The Shop, rebuilt from the user's own reference: a wood top bar, a category strip of four
    /// tabs, and a 3-column grid of item cards below it.
    ///
    /// Named <see cref="StoreScreen"/> rather than reusing <c>ShopScreen</c> - that class is the
    /// old pastel-era shop (Featured/Pawns/Boards/Bundles categories, ClayButton widgets) and is
    /// left in place but no longer built into <c>Resources/UI/Screens/Shop.prefab</c>, the same
    /// pattern as <see cref="ResultScreen"/> replacing the old match-result screen.
    ///
    /// Only Dice Skins is stocked for this pass, per the user's request to complete the shop with
    /// what they gave: 10 dice skins, one flat price each. Tokens/Energy/Emoji show an honest
    /// "coming soon" rather than empty silence, so tapping a tab never looks broken.
    /// </summary>
    public class StoreScreen : UIScreen
    {
        public enum Tab { Dice, Tokens, Energy, Emoji }

        [Header("Header")]
        public TextMeshProUGUI coinsLabel;
        public TextMeshProUGUI energyLabel;
        public Button menuButton;

        [Header("Tabs")]
        public Button[] tabButtons;      // Dice, Tokens, Energy, Emoji, in that order
        public Image[] tabBackgrounds;
        public TextMeshProUGUI[] tabLabels;
        public Sprite tabSelected;
        public Sprite tabIdle;

        [Header("Grid")]
        public RectTransform gridContent;
        public StoreItemCard cardTemplate;
        public GameObject comingSoonState;
        public TextMeshProUGUI comingSoonLabel;

        [Header("Purchase confirm (placeholder — real RevenueCat flow comes later)")]
        public GameObject buyPopup;
        public Button buyPopupBlocker;
        public Image buyPopupIcon;
        public TextMeshProUGUI buyPopupTitle;
        public Button buyPopupConfirm;
        public Button buyPopupCancel;
        public TextMeshProUGUI buyPopupPrice;

        [Header("Message")]
        public CanvasGroup toast;
        public TextMeshProUGUI toastText;

        private static readonly (string id, string name, string file)[] DiceSkins =
        {
            ("dice_01", "CLASSIC",   "dice_01_classic"),
            ("dice_02", "RUBY",      "dice_02_ruby"),
            ("dice_03", "SAPPHIRE",  "dice_03_sapphire"),
            ("dice_04", "EMERALD",   "dice_04_emerald"),
            ("dice_05", "GOLD",      "dice_05_gold"),
            ("dice_06", "AMETHYST",  "dice_06_amethyst"),
            ("dice_07", "ONYX",      "dice_07_onyx"),
            ("dice_08", "BLOSSOM",   "dice_08_blossom"),
            ("dice_09", "TEAL",      "dice_09_teal"),
            ("dice_10", "AMBER",     "dice_10_amber"),
        };

        /// <summary>
        /// Every price is $0.99 for this pass — the user's explicit instruction, to be tuned per
        /// item later. Kept as one constant rather than scattered literals so that changing it
        /// changes the whole catalogue at once.
        /// </summary>
        public const string FlatPrice = "$0.99";

        [Header("Art")]
        public Sprite[] diceSprites;     // in DiceSkins order
        public Sprite energyIcon;        // energy pack cards and their confirm popup
        [Tooltip("Parallel to StickerPackService.Stickers — animates each pack's cover sticker.")]
        public Texture2D[] stickerTextures;

        private Tab _tab = Tab.Dice;
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private Coroutine _toastRoutine;
        private string _pendingPurchaseId;
        private string _pendingPurchaseName;
        private string _pendingProductId;   // set only for real-money store products
        private StickerPackService.Pack _pendingPack;   // set only for energy-priced sticker packs

        private void OnEnable()
        {
            if (LudoNX.Shop.RevenueCatManager.Instance != null)
                LudoNX.Shop.RevenueCatManager.Instance.OfferingsLoaded += OnOfferingsLoaded;
        }

        private void OnDisable()
        {
            if (LudoNX.Shop.RevenueCatManager.Instance != null)
                LudoNX.Shop.RevenueCatManager.Instance.OfferingsLoaded -= OnOfferingsLoaded;
        }

        // Prices arrive after the screen may already be showing; repaint with the localised ones.
        private void OnOfferingsLoaded() { if (_tab == Tab.Energy) BuildEnergyGrid(); }

        private void Awake()
        {
            if (cardTemplate != null) cardTemplate.gameObject.SetActive(false);
            if (buyPopup != null) buyPopup.SetActive(false);
            if (toast != null) { toast.alpha = 0f; toast.blocksRaycasts = false; }

            if (menuButton != null)
                menuButton.onClick.AddListener(() => Toast("Settings menu — coming soon"));
            if (buyPopupBlocker != null) buyPopupBlocker.onClick.AddListener(ClosePurchase);
            if (buyPopupCancel != null) buyPopupCancel.onClick.AddListener(ClosePurchase);
            if (buyPopupConfirm != null) buyPopupConfirm.onClick.AddListener(ConfirmPurchase);

            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    if (tabButtons[i] == null) continue;
                    var tab = (Tab)i;
                    tabButtons[i].onClick.AddListener(() => SetTab(tab));
                }
            }
        }

        public override void OnShow(object args)
        {
            var s = LudoNXSession.Instance;
            if (coinsLabel != null) coinsLabel.text = s != null ? s.Coins.ToString("n0") : "0";
            if (energyLabel != null) energyLabel.text = EnergyService.CountText;

            // The Album opens the shop on the tab that sells what was tapped.
            if (args is Tab requested) _tab = requested;
            SetTab(_tab);
        }

        // ==================================================================================
        // tabs
        // ==================================================================================

        private void SetTab(Tab tab)
        {
            _tab = tab;
            PaintTabs();

            if (tab == Tab.Dice) BuildDiceGrid();
            else if (tab == Tab.Energy) BuildEnergyGrid();
            else if (tab == Tab.Emoji) BuildEmojiGrid();
            else ShowComingSoon(tab);
        }

        private void PaintTabs()
        {
            if (tabBackgrounds == null) return;
            for (int i = 0; i < tabBackgrounds.Length; i++)
            {
                bool on = (int)_tab == i;
                if (tabBackgrounds[i] != null && tabSelected != null && tabIdle != null)
                    tabBackgrounds[i].sprite = on ? tabSelected : tabIdle;
                if (tabLabels != null && i < tabLabels.Length && tabLabels[i] != null)
                    tabLabels[i].color = on ? new Color32(48, 32, 12, 255) : new Color32(244, 230, 203, 230);
            }
        }

        // ==================================================================================
        // dice grid
        // ==================================================================================

        private void BuildDiceGrid()
        {
            Clear();
            if (comingSoonState != null) comingSoonState.SetActive(false);
            if (gridContent != null) gridContent.gameObject.SetActive(true);
            if (cardTemplate == null || gridContent == null) return;

            for (int i = 0; i < DiceSkins.Length; i++)
            {
                var (id, name, _) = DiceSkins[i];
                var card = Instantiate(cardTemplate, gridContent);
                card.gameObject.SetActive(true);
                _spawned.Add(card.gameObject);

                if (card.icon != null && diceSprites != null && i < diceSprites.Length)
                    card.icon.sprite = diceSprites[i];
                if (card.nameLabel != null) card.nameLabel.text = name;
                if (card.priceLabel != null) card.priceLabel.text = FlatPrice;

                string capturedId = id, capturedName = name;
                if (card.buyButton != null)
                    card.buyButton.onClick.AddListener(() => OpenPurchase(capturedId, capturedName, i));
            }
        }

        // ==================================================================================
        // energy packs — real money through RevenueCat (EnergyCatalog is the only price list)
        // ==================================================================================

        private void BuildEnergyGrid()
        {
            Clear();
            if (comingSoonState != null) comingSoonState.SetActive(false);
            if (gridContent != null) gridContent.gameObject.SetActive(true);
            if (cardTemplate == null || gridContent == null) return;

            foreach (var pack in LudoNX.Shop.EnergyCatalog.Packs)
            {
                var card = Instantiate(cardTemplate, gridContent);
                card.gameObject.SetActive(true);
                _spawned.Add(card.gameObject);

                if (card.icon != null && energyIcon != null) { card.icon.sprite = energyIcon; card.icon.preserveAspect = true; }
                if (card.nameLabel != null)
                    card.nameLabel.text = pack.energy.ToString("n0") + " ENERGY";   // tags need their own ribbon; never hide the amount
                string price = LudoNX.Shop.EnergyCatalog.PriceFor(pack);
                if (card.priceLabel != null) card.priceLabel.text = price;

                var captured = pack;
                if (card.buyButton != null)
                    card.buyButton.onClick.AddListener(() => OpenEnergyPurchase(captured, price));
            }
        }

        private void OpenEnergyPurchase(LudoNX.Shop.EnergyCatalog.Pack pack, string price)
        {
            if (buyPopup == null) return;
            _pendingPurchaseId = null;
            _pendingPack = null;
            _pendingProductId = pack.productId;
            _pendingPurchaseName = pack.energy.ToString("n0") + " ENERGY";

            if (buyPopupIcon != null && energyIcon != null) buyPopupIcon.sprite = energyIcon;
            if (buyPopupTitle != null) buyPopupTitle.text = _pendingPurchaseName;
            if (buyPopupPrice != null) buyPopupPrice.text = price;
            buyPopup.SetActive(true);
        }

        private void BuyEnergy(string productId, string name)
        {
            int amount = LudoNX.Shop.EnergyCatalog.EnergyFor(productId);
            var rc = LudoNX.Shop.RevenueCatManager.Instance;

            if (rc == null || !rc.StoreReady)
            {
#if UNITY_EDITOR
                // The SDK is a no-op in the Editor; grant so the flow can be seen end to end here.
                GrantEnergy(amount, name + " added (Editor test — no store)");
#else
                // Never grant without a confirmed purchase on a device.
                if (rc != null && !string.IsNullOrEmpty(rc.LastError))
                    Toast("Store error: " + rc.LastError);
                else
                    Toast("Store is still loading — try again in a moment");
#endif
                return;
            }

            rc.Buy(productId, (ok, errorMsg) =>
            {
                if (!ok)
                {
                    if (errorMsg == "UserCancelled")
                    {
                        Toast("Purchase cancelled");
                    }
                    else
                    {
                        Toast(!string.IsNullOrEmpty(errorMsg) ? errorMsg : "Purchase failed");
                    }
                    return;
                }
                // TEST-ONLY: the phone grants until the revenueCatWebhook (GO_LIVE_PLAN T-B2)
                // exists; then the server grants and this line becomes a wallet refresh.
                GrantEnergy(amount, "+" + amount.ToString("n0") + " energy added");
            });
        }

        private void GrantEnergy(int amount, string message)
        {
            if (amount > 0) EnergyService.Grant(amount, allowOverfill: true);
            if (energyLabel != null) energyLabel.text = EnergyService.CountText;
            Toast(message);
        }

        // ==================================================================================
        // emoji sticker packs — priced in energy (owner, 27 Sep 2026)
        // ==================================================================================

        private void BuildEmojiGrid()
        {
            Clear();
            if (comingSoonState != null) comingSoonState.SetActive(false);
            if (gridContent != null) gridContent.gameObject.SetActive(true);
            if (cardTemplate == null || gridContent == null) return;

            foreach (var pack in StickerPackService.Packs)
            {
                var card = Instantiate(cardTemplate, gridContent);
                card.gameObject.SetActive(true);
                _spawned.Add(card.gameObject);

                if (card.icon != null) AnimateCover(card.icon, pack);
                if (card.nameLabel != null) card.nameLabel.text = pack.name;
                bool owned = StickerPackService.Owns(pack);
                if (card.priceLabel != null)
                    card.priceLabel.text = owned ? "OPEN" : pack.energyCost + " ENERGY";

                var captured = pack;
                if (card.buyButton != null)
                    card.buyButton.onClick.AddListener(() =>
                    {
                        if (StickerPackService.Owns(captured)) StickerPackOpener.Open(captured, false);
                        else OpenPackPurchase(captured);
                    });
            }
        }

        /// <summary>The card's icon plays the pack's first sticker, so the shelf is alive.</summary>
        private void AnimateCover(Image icon, StickerPackService.Pack pack)
        {
            int idx = pack.stickers.Length > 0 ? StickerPackService.IndexOf(pack.stickers[0]) : -1;
            if (idx < 0 || stickerTextures == null || idx >= stickerTextures.Length || stickerTextures[idx] == null) return;
            var s = StickerPackService.Stickers[idx];
            var go = icon.gameObject;
            bool was = go.activeSelf;
            go.SetActive(false);   // configure before OnEnable slices the sheet
            var fb = go.GetComponent<EmoteFlipbook>();
            if (fb == null) fb = go.AddComponent<EmoteFlipbook>();
            fb.texture = stickerTextures[idx];
            fb.frameCount = s.frames;
            fb.contentSize = s.content;
            fb.framesPerSecond = s.fps;
            fb.loop = true;
            icon.preserveAspect = true;
            go.SetActive(was);
        }

        private void OpenPackPurchase(StickerPackService.Pack pack)
        {
            if (buyPopup == null) return;
            _pendingPurchaseId = null;
            _pendingProductId = null;
            _pendingPack = pack;
            _pendingPurchaseName = pack.name;
            if (buyPopupTitle != null) buyPopupTitle.text = pack.name;
            if (buyPopupPrice != null) buyPopupPrice.text = pack.energyCost + " ENERGY";
            if (buyPopupIcon != null && energyIcon != null) buyPopupIcon.sprite = energyIcon;
            buyPopup.SetActive(true);
        }

        private void BuyPack(StickerPackService.Pack pack)
        {
            switch (StickerPackService.TryBuy(pack))
            {
                case StickerPackService.BuyResult.NotEnoughEnergy:
                    Toast("Not enough energy — get more in the Energy tab");
                    return;
                case StickerPackService.BuyResult.AlreadyOwned:
                    StickerPackOpener.Open(pack, false);
                    return;
            }
            if (energyLabel != null) energyLabel.text = EnergyService.CountText;
            StickerPackOpener.Open(pack, true, () => { if (_tab == Tab.Emoji) BuildEmojiGrid(); });
        }

        private void ShowComingSoon(Tab tab)
        {
            Clear();
            if (gridContent != null) gridContent.gameObject.SetActive(false);
            if (comingSoonState != null) comingSoonState.SetActive(true);
            if (comingSoonLabel != null)
            {
                string what = tab switch
                {
                    Tab.Tokens => "Token skins",
                    Tab.Energy => "Energy packs",
                    _ => "Emoji reactions",
                };
                comingSoonLabel.text = $"{what} are coming soon";
            }
        }

        private void Clear()
        {
            foreach (var go in _spawned)
            {
                if (go != null) { go.SetActive(false); Destroy(go); }
            }
            _spawned.Clear();
        }

        // ==================================================================================
        // purchase (placeholder popup — swapped for the real RevenueCat + win-reveal flow later)
        // ==================================================================================

        private void OpenPurchase(string id, string name, int spriteIndex)
        {
            if (buyPopup == null) return;
            _pendingProductId = null;
            _pendingPack = null;
            _pendingPurchaseId = id;
            _pendingPurchaseName = name;

            if (buyPopupIcon != null && diceSprites != null && spriteIndex < diceSprites.Length)
                buyPopupIcon.sprite = diceSprites[spriteIndex];
            if (buyPopupTitle != null) buyPopupTitle.text = name;
            if (buyPopupPrice != null) buyPopupPrice.text = FlatPrice;

            buyPopup.SetActive(true);
        }

        private void ConfirmPurchase()
        {
            if (_pendingPack != null)
            {
                var pack = _pendingPack;
                _pendingPack = null;
                ClosePurchase();
                BuyPack(pack);
                return;
            }
            if (!string.IsNullOrEmpty(_pendingProductId))
            {
                string id = _pendingProductId, name = _pendingPurchaseName;
                _pendingProductId = null;
                ClosePurchase();
                BuyEnergy(id, name);
                return;
            }

            // Placeholder: no RevenueCat call yet, no coin/currency spend yet - this exists so the
            // card grid is fully tappable and testable before the real purchase pipeline is wired.
            Toast($"{_pendingPurchaseName} — coming soon! Buy Energy in the Energy tab.");
            ClosePurchase();
        }

        private void ClosePurchase()
        {
            if (buyPopup != null) buyPopup.SetActive(false);
        }

        public override bool OnBack()
        {
            if (buyPopup != null && buyPopup.activeSelf) { ClosePurchase(); return true; }
            return false;
        }

        // ==================================================================================
        // toast
        // ==================================================================================

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
