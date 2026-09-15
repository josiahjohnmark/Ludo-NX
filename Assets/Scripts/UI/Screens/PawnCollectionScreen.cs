using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Pawn & Token Collection Screen.
    /// Displays owned/locked pawns, rarity tiers, equipped item preview, and collection completion.
    /// Uses UITheme design system and clay components.
    /// </summary>
    public class PawnCollectionScreen : UIScreen
    {
        [System.Serializable]
        public class PawnItem
        {
            public string id;
            public string displayName;
            public string rarity;
            public Color rarityColor;
            public string acquisitionMethod;
            public Sprite sprite;
            public bool isOwned;
            public bool isEquipped;
        }

        [Header("Wiring")]
        public ClayButton backButton;
        public TextMeshProUGUI collectionProgressLabel;
        public RectTransform collectionProgressBarFill;

        [Header("Equipped Preview")]
        public TextMeshProUGUI equippedNameLabel;
        public TextMeshProUGUI equippedRarityLabel;
        public TextMeshProUGUI equippedPerkLabel;
        public Image equippedPreviewImage;
        public ClayButton equipOrInspectButton;
        public TextMeshProUGUI equipOrInspectLabel;

        [Header("Grid & Cards")]
        public Transform gridContainer;
        public RectTransform cardTemplate;

        [Header("Navigation Bar")]
        public HomeNavBar navBar;

        private readonly List<PawnItem> _items = new List<PawnItem>();
        private readonly List<GameObject> _spawnedCards = new List<GameObject>();
        private PawnItem _selectedItem;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (equipOrInspectButton != null) equipOrInspectButton.onClick.AddListener(OnEquipClicked);
            LoadPawnLibrary();
        }

        public override void OnShow(object args)
        {
            if (navBar != null) navBar.SetActiveTab(HomeNavBar.Tab.Collection);
            RefreshUI();
        }

        private void LoadPawnLibrary()
        {
            _items.Clear();
            var icons = UIIconSet.Current;
            var theme = UITheme.Current;

            Sprite redPawn = (icons != null && icons.pawns != null && icons.pawns.Length > 0) ? icons.pawns[0] : null;
            Sprite greenPawn = (icons != null && icons.pawns != null && icons.pawns.Length > 1) ? icons.pawns[1] : null;
            Sprite yellowPawn = (icons != null && icons.pawns != null && icons.pawns.Length > 2) ? icons.pawns[2] : null;
            Sprite bluePawn = (icons != null && icons.pawns != null && icons.pawns.Length > 3) ? icons.pawns[3] : null;

            _items.Add(new PawnItem
            {
                id = "pawn_classic_red",
                displayName = "Classic Crimson",
                rarity = "COMMON",
                rarityColor = theme.inkDim,
                acquisitionMethod = "Starter Token",
                sprite = redPawn,
                isOwned = true,
                isEquipped = true
            });

            _items.Add(new PawnItem
            {
                id = "pawn_classic_green",
                displayName = "Emerald Guard",
                rarity = "COMMON",
                rarityColor = theme.inkDim,
                acquisitionMethod = "Starter Token",
                sprite = greenPawn,
                isOwned = true,
                isEquipped = false
            });

            _items.Add(new PawnItem
            {
                id = "pawn_classic_yellow",
                displayName = "Solar Sunstone",
                rarity = "RARE",
                rarityColor = theme.gold,
                acquisitionMethod = "Level 5 Reward",
                sprite = yellowPawn,
                isOwned = true,
                isEquipped = false
            });

            _items.Add(new PawnItem
            {
                id = "pawn_classic_blue",
                displayName = "Oceanic Sapphire",
                rarity = "RARE",
                rarityColor = theme.secondary,
                acquisitionMethod = "Shop Bundle",
                sprite = bluePawn,
                isOwned = true,
                isEquipped = false
            });

            _items.Add(new PawnItem
            {
                id = "pawn_royal_gold",
                displayName = "Imperial Sovereign",
                rarity = "LEGENDARY",
                rarityColor = theme.gold,
                acquisitionMethod = "Win 25 Matches",
                sprite = redPawn,
                isOwned = false,
                isEquipped = false
            });

            _items.Add(new PawnItem
            {
                id = "pawn_obsidian_knight",
                displayName = "Obsidian Monarch",
                rarity = "EPIC",
                rarityColor = theme.primary,
                acquisitionMethod = "Season 1 Event",
                sprite = greenPawn,
                isOwned = false,
                isEquipped = false
            });

            _selectedItem = _items[0];
        }

        private void RefreshUI()
        {
            var theme = UITheme.Current;
            int ownedCount = 0;
            foreach (var item in _items) if (item.isOwned) ownedCount++;

            if (collectionProgressLabel != null)
            {
                collectionProgressLabel.text = ownedCount + " / " + _items.Count + " UNLOCKED (" +
                    Mathf.RoundToInt((ownedCount / (float)_items.Count) * 100f) + "%)";
            }

            if (collectionProgressBarFill != null)
            {
                float progress01 = Mathf.Clamp01(ownedCount / (float)_items.Count);
                collectionProgressBarFill.sizeDelta = new Vector2(400f * progress01, collectionProgressBarFill.sizeDelta.y);
            }

            UpdatePreview();
            PopulateGrid();
        }

        private void UpdatePreview()
        {
            if (_selectedItem == null) return;
            var theme = UITheme.Current;

            if (equippedNameLabel != null) equippedNameLabel.text = _selectedItem.displayName;
            if (equippedRarityLabel != null)
            {
                equippedRarityLabel.text = _selectedItem.rarity;
                equippedRarityLabel.color = UITheme.Lin(_selectedItem.rarityColor);
            }
            if (equippedPerkLabel != null)
            {
                equippedPerkLabel.text = _selectedItem.isOwned
                    ? "Unlocked via: " + _selectedItem.acquisitionMethod
                    : "Locked — " + _selectedItem.acquisitionMethod;
            }

            if (equippedPreviewImage != null && _selectedItem.sprite != null)
            {
                equippedPreviewImage.sprite = _selectedItem.sprite;
                equippedPreviewImage.enabled = true;
            }

            if (equipOrInspectButton != null && equipOrInspectLabel != null)
            {
                if (_selectedItem.isEquipped)
                {
                    equipOrInspectButton.interactable = false;
                    if (equipOrInspectButton.Clay != null) equipOrInspectButton.Clay.color = theme.locked;
                    equipOrInspectLabel.text = "EQUIPPED";
                }
                else if (_selectedItem.isOwned)
                {
                    equipOrInspectButton.interactable = true;
                    if (equipOrInspectButton.Clay != null) equipOrInspectButton.Clay.color = theme.primary;
                    equipOrInspectLabel.text = "EQUIP";
                }
                else
                {
                    equipOrInspectButton.interactable = true;
                    if (equipOrInspectButton.Clay != null) equipOrInspectButton.Clay.color = theme.gold;
                    equipOrInspectLabel.text = "INSPECT";
                }
            }
        }

        private void PopulateGrid()
        {
            foreach (var go in _spawnedCards) if (go != null) Destroy(go);
            _spawnedCards.Clear();

            if (cardTemplate == null || gridContainer == null) return;
            var theme = UITheme.Current;

            foreach (var item in _items)
            {
                var cardObj = Instantiate(cardTemplate.gameObject, gridContainer);
                cardObj.SetActive(true);
                _spawnedCards.Add(cardObj);

                var title = cardObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
                var rarity = cardObj.transform.Find("Rarity")?.GetComponent<TextMeshProUGUI>();
                var icon = cardObj.transform.Find("Icon")?.GetComponent<Image>();
                var status = cardObj.transform.Find("StatusPill/StatusLabel")?.GetComponent<TextMeshProUGUI>();
                var btn = cardObj.GetComponent<ClayButton>();

                if (title != null) title.text = item.displayName;
                if (rarity != null)
                {
                    rarity.text = item.rarity;
                    rarity.color = UITheme.Lin(item.rarityColor);
                }
                if (icon != null && item.sprite != null)
                {
                    icon.sprite = item.sprite;
                    icon.color = item.isOwned ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.8f);
                }
                if (status != null)
                {
                    status.text = item.isEquipped ? "EQUIPPED" : (item.isOwned ? "OWNED" : "LOCKED");
                }

                if (btn != null)
                {
                    PawnItem captured = item;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        _selectedItem = captured;
                        UpdatePreview();
                    });
                }
            }
        }

        private void OnEquipClicked()
        {
            if (_selectedItem == null) return;

            if (_selectedItem.isOwned)
            {
                foreach (var it in _items) it.isEquipped = (it == _selectedItem);
                Toast.Instance.Show("Equipped " + _selectedItem.displayName + "!");
                RefreshUI();
            }
            else
            {
                // Open detail inspection modal
                var detail = (ShopItemDetailModal)UIModal.Open("ShopItemDetailModal");
                if (detail != null)
                {
                    detail.Setup(_selectedItem.displayName, _selectedItem.rarity + " PAWN",
                                 "Acquisition: " + _selectedItem.acquisitionMethod,
                                 "• Custom 3D mesh\n• Smooth hop particle trail\n• Sound aura",
                                 _selectedItem.sprite, "VIEW IN SHOP", UITheme.Current.gold,
                                 () => ScreenManager.Instance.Push(ScreenId.Shop));
                }
            }
        }
    }
}
