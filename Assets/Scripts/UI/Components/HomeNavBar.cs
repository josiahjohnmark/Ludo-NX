using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Tactile Bottom Navigation Bar for rapid hub access across core game screens.
    /// Manages active tab state, pill highlights, and screen transitions.
    /// </summary>
    public class HomeNavBar : MonoBehaviour
    {
        public enum Tab
        {
            Album,
            Friends,
            Home,
            Shop,
            Levels,
            // Legacy aliases for backward compatibility
            Play,
            Collection,
            Missions,
            Events
        }

        [System.Serializable]
        public class NavItem
        {
            public Tab tab;
            public ScreenId targetScreen;
            public Button button;
            public ClayGraphic backgroundPanel;
            public ClayGraphic activeBorder;
            public ClayGraphic highlightPill;
            public TextMeshProUGUI label;
            public Image iconImage;
            public GameObject notificationBadge;
            [HideInInspector] public Vector2 defaultPos;
        }

        [Header("Tabs")]
        public List<NavItem> items = new List<NavItem>();

        [Header("Selected Tab Look")]
        [Tooltip("Units the selected tab rises. 0 matches the design reference, which shows no lift.")]
        public float selectedRaise = 0f;
        [Tooltip("Scale of the selected icon. 1 matches the design reference.")]
        public float selectedScale = 1f;
        [Tooltip("Opacity of unselected icons. 1 matches the design reference.")]
        [Range(0f, 1f)] public float unselectedAlpha = 1f;

        [Header("Current State")]
        [SerializeField] private Tab activeTab = Tab.Home;

        public Tab ActiveTab => activeTab;

        public void SyncToScreen(ScreenId screenId)
        {
            Tab targetTab = Tab.Home;
            switch (screenId)
            {
                case ScreenId.Home:
                    targetTab = Tab.Home;
                    break;
                case ScreenId.PawnCollection:
                    targetTab = Tab.Album;
                    break;
                case ScreenId.Friends:
                    targetTab = Tab.Friends;
                    break;
                case ScreenId.Shop:
                case ScreenId.ShopItemDetail:
                    targetTab = Tab.Shop;
                    break;
                case ScreenId.PlayerStats:
                case ScreenId.Events:
                case ScreenId.EventDetail:
                    targetTab = Tab.Levels;
                    break;
                default:
                    return; // keep current
            }
            SetActiveTab(targetTab);
        }

        private void Awake()
        {
            foreach (var item in items)
            {
                if (item == null || item.button == null) continue;
                NavItem captured = item;
                item.button.onClick.AddListener(() => OnTabClicked(captured));
            }
        }

        private void Start()
        {
            UpdateVisuals();
        }

        public void SetActiveTab(Tab tab)
        {
            activeTab = tab;
            UpdateVisuals();
        }

        private void OnTabClicked(NavItem item)
        {
            if (item == null) return;
            if (activeTab == item.tab && ScreenManager.Instance.Active != null && ScreenManager.Instance.Active.Id == item.targetScreen)
                return;

            activeTab = item.tab;
            UpdateVisuals();

            if (ScreenManager.Instance != null)
            {
                if (item.targetScreen == ScreenId.Home)
                {
                    ScreenManager.Instance.ResetTo(ScreenId.Home);
                }
                else
                {
                    // When navigating between hub screens, replace the current screen so the back stack remains clean
                    if (ScreenManager.Instance.Active != null && ScreenManager.Instance.Active.Id != ScreenId.Home)
                    {
                        ScreenManager.Instance.Replace(item.targetScreen);
                    }
                    else
                    {
                        ScreenManager.Instance.Push(item.targetScreen);
                    }
                }
            }
        }

        public void UpdateVisuals()
        {
            var theme = UITheme.Current;

            foreach (var item in items)
            {
                if (item == null) continue;
                bool isSelected = (item.tab == activeTab);

                if (item.button != null)
                {
                    var rt = item.button.transform as RectTransform;
                    if (rt != null)
                    {
                        if (item.defaultPos == Vector2.zero) item.defaultPos = rt.anchoredPosition;
                        rt.anchoredPosition = isSelected
                            ? new Vector2(item.defaultPos.x, item.defaultPos.y + selectedRaise)
                            : item.defaultPos;
                    }
                }

                if (item.activeBorder != null)
                {
                    item.activeBorder.gameObject.SetActive(isSelected);
                }

                if (item.backgroundPanel != null)
                {
                    item.backgroundPanel.color = isSelected ? theme.navPodActive : theme.navPodDefault;
                    item.backgroundPanel.innerHighlight = isSelected ? 0.35f : 0.18f;
                    item.backgroundPanel.SetVerticesDirty();
                }

                if (item.highlightPill != null)
                {
                    item.highlightPill.gameObject.SetActive(isSelected);
                    if (isSelected)
                    {
                        item.highlightPill.color = theme.primary;
                        item.highlightPill.SetVerticesDirty();
                    }
                }

                if (item.label != null)
                {
                    item.label.color = isSelected ? Color.white : new Color(0.957f, 0.902f, 0.796f, 0.95f);
                    item.label.fontStyle = FontStyles.Bold;
                }

                if (item.iconImage != null)
                {
                    item.iconImage.color = isSelected ? Color.white : new Color(1f, 1f, 1f, unselectedAlpha);
                    item.iconImage.transform.localScale = isSelected ? Vector3.one * selectedScale : Vector3.one;
                }
            }
        }
    }
}
