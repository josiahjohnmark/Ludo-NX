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
            public Image activeIndicator;
            [HideInInspector] public Vector2 defaultPos;
        }

        [Header("Tabs")]
        public List<NavItem> items = new List<NavItem>();

        [Header("Selected Tab Look")]
        [Tooltip("Units the selected tab rises.")]
        public float selectedRaise = 2f;
        [Tooltip("Scale of the selected icon.")]
        public float selectedScale = 1.12f;
        [Tooltip("Opacity of unselected icons.")]
        [Range(0f, 1f)] public float unselectedAlpha = 0.55f;

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
                    return; // Album locked under development
                case ScreenId.Friends:
                    targetTab = Tab.Friends;
                    break;
                case ScreenId.Shop:
                case ScreenId.ShopItemDetail:
                    targetTab = Tab.Shop;
                    break;
                case ScreenId.Levels:
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
            if (tab == Tab.Album || tab == Tab.Collection) tab = Tab.Home;
            activeTab = tab;
            UpdateVisuals();
        }

        private void OnTabClicked(NavItem item)
        {
            if (item == null) return;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();

            // Lock Album as it is under development
            if (item.tab == Tab.Album || item.tab == Tab.Collection || item.targetScreen == ScreenId.PawnCollection)
            {
                Toast.Instance.Show("Album is currently under development. Coming soon!");
                return;
            }

            if (activeTab == item.tab && ScreenManager.Instance.Active != null && ScreenManager.Instance.Active.Id == item.targetScreen)
                return;

            activeTab = item.tab;
            UpdateVisuals();

            // LEVELS is behind the vault doors (owner, 27 Sep 2026): they slam shut over the
            // current screen, the Levels screen is swapped in behind them, and they open on it.
            if (item.tab == Tab.Levels && ScreenManager.Instance != null)
            {
                if (DoorTransition.IsPlaying) return;
                DoorTransition.Play(() => GoTo(ScreenId.Levels));
                return;
            }

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

        private static void GoTo(ScreenId target)
        {
            var sm = ScreenManager.Instance;
            if (sm == null) return;
            if (sm.Active != null && sm.Active.Id == target) return;
            if (sm.Active != null && sm.Active.Id != ScreenId.Home) sm.Replace(target);
            else sm.Push(target);
        }

        public void UpdateVisuals()
        {
            var theme = UITheme.Current;

            foreach (var item in items)
            {
                if (item == null) continue;
                bool isAlbumLocked = (item.tab == Tab.Album || item.tab == Tab.Collection || item.targetScreen == ScreenId.PawnCollection);
                bool isSelected = (item.tab == activeTab) && !isAlbumLocked;

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

                if (item.activeIndicator == null && item.button != null)
                {
                    var existing = item.button.transform.Find("ActiveIndicator");
                    if (existing != null)
                    {
                        item.activeIndicator = existing.GetComponent<Image>();
                    }
                    else
                    {
                        var indGo = new GameObject("ActiveIndicator", typeof(RectTransform), typeof(Image));
                        indGo.transform.SetParent(item.button.transform, false);
                        var indRt = (RectTransform)indGo.transform;
                        indRt.anchorMin = new Vector2(0.5f, 0f);
                        indRt.anchorMax = new Vector2(0.5f, 0f);
                        indRt.pivot = new Vector2(0.5f, 0.5f);
                        indRt.sizeDelta = new Vector2(28f, 5f);
                        indRt.anchoredPosition = new Vector2(0f, -4f);
                        var indImg = indGo.GetComponent<Image>();
                        indImg.color = new Color(1f, 0.82f, 0.28f, 1f); // Warm gold indicator glow
                        indImg.raycastTarget = false;
                        item.activeIndicator = indImg;
                    }
                }

                if (item.activeIndicator != null)
                {
                    item.activeIndicator.gameObject.SetActive(isSelected);
                }

                if (item.label != null)
                {
                    if (isAlbumLocked)
                    {
                        item.label.color = new Color(0.7f, 0.7f, 0.7f, 0.40f);
                        item.label.fontStyle = FontStyles.Normal;
                    }
                    else
                    {
                        item.label.color = isSelected ? new Color(1f, 0.88f, 0.35f, 1f) : new Color(0.957f, 0.902f, 0.796f, 0.55f);
                        item.label.fontStyle = FontStyles.Bold;
                    }
                }

                if (item.iconImage != null)
                {
                    if (isAlbumLocked)
                    {
                        item.iconImage.color = new Color(0.6f, 0.6f, 0.6f, 0.40f);
                        item.iconImage.transform.localScale = Vector3.one * 0.90f;
                    }
                    else
                    {
                        item.iconImage.color = isSelected ? Color.white : new Color(0.85f, 0.85f, 0.85f, unselectedAlpha);
                        item.iconImage.transform.localScale = isSelected ? Vector3.one * selectedScale : Vector3.one;
                    }
                }

                if (isAlbumLocked && item.button != null)
                {
                    var existingLock = item.button.transform.Find("AlbumLockBadge");
                    if (existingLock == null)
                    {
                        var lockGo = new GameObject("AlbumLockBadge", typeof(RectTransform), typeof(Image));
                        lockGo.transform.SetParent(item.button.transform, false);
                        var lockRt = (RectTransform)lockGo.transform;
                        lockRt.anchorMin = new Vector2(0.5f, 0.5f);
                        lockRt.anchorMax = new Vector2(0.5f, 0.5f);
                        lockRt.pivot = new Vector2(0.5f, 0.5f);
                        lockRt.sizeDelta = new Vector2(20f, 20f);
                        lockRt.anchoredPosition = new Vector2(13f, 12f);
                        var lockImg = lockGo.GetComponent<Image>();
                        lockImg.sprite = Resources.Load<Sprite>("UI/icon_lock");
                        lockImg.color = new Color(1f, 1f, 1f, 0.85f);
                        lockImg.raycastTarget = false;
                    }
                }
            }
        }
    }
}
