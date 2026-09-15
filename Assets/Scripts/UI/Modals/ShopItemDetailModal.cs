using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Modal for inspecting a specific shop item / dice skin / bundle in detail.
    /// Supports preview icon, stats/perks, and direct purchase or equip.
    /// </summary>
    public class ShopItemDetailModal : UIModal
    {
        [Header("Item Info")]
        public TextMeshProUGUI itemTitleLabel;
        public TextMeshProUGUI itemCategoryLabel;
        public TextMeshProUGUI itemDescriptionLabel;
        public TextMeshProUGUI perkListLabel;
        public Image itemIconImage;

        [Header("Action")]
        public ClayButton buyOrEquipButton;
        public TextMeshProUGUI buyOrEquipLabel;
        public ClayButton closeButton;

        private Action _onBuyOrEquip;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Dismiss);
            if (buyOrEquipButton != null) buyOrEquipButton.onClick.AddListener(OnActionClick);
        }

        public void Setup(string title, string category, string description, string perks,
                          Sprite icon, string actionLabel, Color actionAccent, Action onAction)
        {
            if (itemTitleLabel != null) itemTitleLabel.text = title;
            if (itemCategoryLabel != null) itemCategoryLabel.text = category.ToUpper();
            if (itemDescriptionLabel != null) itemDescriptionLabel.text = description;
            if (perkListLabel != null) perkListLabel.text = perks;
            if (itemIconImage != null && icon != null)
            {
                itemIconImage.sprite = icon;
                itemIconImage.enabled = true;
            }

            if (buyOrEquipLabel != null) buyOrEquipLabel.text = actionLabel;
            if (buyOrEquipButton != null && buyOrEquipButton.Clay != null)
            {
                buyOrEquipButton.Clay.color = actionAccent;
                buyOrEquipButton.Clay.SetVerticesDirty();
            }

            _onBuyOrEquip = onAction;
        }

        private void OnActionClick()
        {
            _onBuyOrEquip?.Invoke();
            Dismiss();
        }
    }
}
