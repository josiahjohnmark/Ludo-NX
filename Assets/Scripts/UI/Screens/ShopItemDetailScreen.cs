using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Shop Item Detail Screen.
    /// Full-screen high-resolution inspection for store cosmetics, boards, dice, and bundles.
    /// Uses UITheme design system and clay components.
    /// </summary>
    public class ShopItemDetailScreen : UIScreen
    {
        [System.Serializable]
        public class ItemData
        {
            public string id;
            public string title;
            public string category;
            public string rarity;
            public Color rarityColor;
            public string description;
            public string perks;
            public string price;
            public Sprite icon;
            public bool isOwned;
            public bool isEquipped;
        }

        [Header("Wiring")]
        public ClayButton backButton;

        [Header("Item Info")]
        public TextMeshProUGUI categoryLabel;
        public TextMeshProUGUI rarityLabel;
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI descriptionLabel;
        public TextMeshProUGUI perksLabel;
        public Image previewImage;

        [Header("Action")]
        public ClayButton actionButton;
        public TextMeshProUGUI actionButtonText;

        private ItemData _data;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (actionButton != null) actionButton.onClick.AddListener(OnActionClick);
        }

        public override void OnShow(object args)
        {
            if (args is ItemData item)
            {
                _data = item;
            }
            else if (_data == null)
            {
                var theme = UITheme.Current;
                _data = new ItemData
                {
                    id = "dice_obsidian",
                    title = "OBSIDIAN DRAGON DIE",
                    category = "DICE SKIN",
                    rarity = "LEGENDARY",
                    rarityColor = theme.gold,
                    description = "Forged in volcanic depths. Features luminous gold engravings and custom roll physics.",
                    perks = "• Custom Roll Sound FX\n• Gold Sparkle Particle Trail\n• Unique Six-Face Burst",
                    price = "$1.99",
                    isOwned = false,
                    isEquipped = false
                };
            }

            RefreshUI();
        }

        public void RefreshUI()
        {
            if (_data == null) return;
            var theme = UITheme.Current;

            if (categoryLabel != null) categoryLabel.text = _data.category.ToUpper();
            if (rarityLabel != null)
            {
                rarityLabel.text = _data.rarity.ToUpper();
                rarityLabel.color = UITheme.Lin(_data.rarityColor);
            }
            if (titleLabel != null) titleLabel.text = _data.title;
            if (descriptionLabel != null) descriptionLabel.text = _data.description;
            if (perksLabel != null) perksLabel.text = _data.perks;

            if (previewImage != null && _data.icon != null)
            {
                previewImage.sprite = _data.icon;
                previewImage.enabled = true;
            }

            if (actionButton != null && actionButtonText != null)
            {
                if (_data.isEquipped)
                {
                    actionButton.interactable = false;
                    if (actionButton.Clay != null) actionButton.Clay.color = theme.locked;
                    actionButtonText.text = "EQUIPPED";
                }
                else if (_data.isOwned)
                {
                    actionButton.interactable = true;
                    if (actionButton.Clay != null) actionButton.Clay.color = theme.secondary;
                    actionButtonText.text = "EQUIP NOW";
                }
                else
                {
                    actionButton.interactable = true;
                    if (actionButton.Clay != null) actionButton.Clay.color = theme.primary;
                    actionButtonText.text = "PURCHASE — " + _data.price;
                }
            }
        }

        private void OnActionClick()
        {
            if (_data == null) return;

            if (_data.isOwned)
            {
                _data.isEquipped = true;
                Toast.Instance.Show("Equipped " + _data.title + "!");
                RefreshUI();
            }
            else
            {
                // Simulate buy / RevenueCat
                _data.isOwned = true;
                _data.isEquipped = true;
                Toast.Instance.Show("Purchased " + _data.title + "!");
                RefreshUI();
            }
        }
    }
}
