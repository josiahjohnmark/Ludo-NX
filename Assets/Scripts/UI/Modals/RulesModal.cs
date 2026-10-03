using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Rules / How to play popup for Ludo NX.
    /// Opens from match action bar, pause modal, settings, and drawer.
    /// Three tabs: BASICS, MOVES, SCORING.
    /// When opened mid-match, visibly pauses the turn timer and resumes on dismiss.
    /// Auto-opens once on the player's first match.
    /// </summary>
    public class RulesModal : UIModal
    {
        private const string PrefKeyAutoOpened = "LudoNX_Rules_FirstMatchAutoOpened";

        [System.Serializable]
        public struct RuleItem
        {
            public string title;
            public string body;
            public Sprite icon;
        }

        [Header("Header")]
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI pausedCaption;

        [Header("Tabs")]
        public Button[] tabButtons; // 0: BASICS, 1: MOVES, 2: SCORING
        public Image[] tabBackgrounds;
        public TextMeshProUGUI[] tabLabels;
        public RectTransform[] tabRects;

        [Header("Content")]
        public ScrollRect scrollRect;
        public RectTransform contentContainer;
        public GameObject ruleRowPrefab;

        [Header("Action & Close")]
        public Button gotItButton;
        public Button closeButton;

        private int _currentTab = 0;
        private bool _pausedTurnTimer = false;

        public static readonly Color32 AmberSelected = new Color32(239, 161, 43, 255); // #EFA12B (token option amber)
        public static readonly Color32 CreamUnselected = new Color32(245, 235, 218, 255);
        public static readonly Color32 InkBlack = new Color32(30, 22, 12, 255);
        public static readonly Color32 MutedBrown = new Color32(120, 96, 70, 255);

        public static readonly RuleItem[] BasicsRules = new[]
        {
            new RuleItem { title = "Get all four home", body = "First player to bring all four tokens into the centre wins the match." },
            new RuleItem { title = "Roll a six to start", body = "A token only leaves its yard on a six." },
            new RuleItem { title = "Take turns clockwise", body = "Red, green, yellow, then blue." },
            new RuleItem { title = "Watch the timer", body = "If your turn runs out, it passes automatically." }
        };

        public static readonly RuleItem[] MovesRules = new[]
        {
            new RuleItem { title = "A six rolls again", body = "But three sixes in a row loses the turn." },
            new RuleItem { title = "Land on an opponent", body = "Their token goes back to its yard. Yours stays." },
            new RuleItem { title = "Safe squares", body = "The starred squares cannot be captured. Two tokens can share one." },
            new RuleItem { title = "Exact count home", body = "You need the exact roll to enter the centre. Anything over and the move is not allowed." }
        };

        public static readonly RuleItem[] ScoringRules = new[]
        {
            new RuleItem { title = "NX Points", body = "Earned for finishing, capturing and getting tokens home. NX Points are your score - they are never spent." },
            new RuleItem { title = "Multipliers", body = "The multiplier you picked before the match multiplies what you win and what you lose." },
            new RuleItem { title = "Coins", body = "Coins are separate. Spend them in the shop." }
        };

        private readonly List<GameObject> _spawnedRows = new List<GameObject>();

        private void Awake()
        {
            if (gotItButton != null) gotItButton.onClick.AddListener(Dismiss);
            if (closeButton != null) closeButton.onClick.AddListener(Dismiss);

            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    int tabIdx = i;
                    if (tabButtons[i] != null)
                    {
                        tabButtons[i].onClick.AddListener(() => SwitchTab(tabIdx));
                    }
                }
            }

            SwitchTab(_currentTab);
        }

        private void OnEnable()
        {
            SwitchTab(_currentTab);
        }

        private void Start()
        {
            // Check if match is in progress
            bool isMidMatch = (ScreenManager.Instance != null && ScreenManager.Instance.Active != null && ScreenManager.Instance.Active.Id == ScreenId.Match)
                           || (GameManager.Instance != null && GameManager.Instance.activePlayers != null && GameManager.Instance.activePlayers.Count > 0);

            if (isMidMatch)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.PauseTurnTimer();
                    _pausedTurnTimer = true;
                }
                if (pausedCaption != null)
                {
                    pausedCaption.gameObject.SetActive(true);
                    pausedCaption.text = "Your turn is paused.";
                }
            }
            else
            {
                if (pausedCaption != null)
                {
                    pausedCaption.gameObject.SetActive(false);
                }
            }

            SwitchTab(0);
        }

        public void SwitchTab(int index)
        {
            _currentTab = Mathf.Clamp(index, 0, 2);

            // Update tab visuals
            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    bool selected = (i == _currentTab);
                    if (tabBackgrounds != null && i < tabBackgrounds.Length && tabBackgrounds[i] != null)
                    {
                        tabBackgrounds[i].color = selected ? (Color)AmberSelected : (Color)CreamUnselected;
                    }
                    if (tabLabels != null && i < tabLabels.Length && tabLabels[i] != null)
                    {
                        tabLabels[i].color = selected ? Color.white : (Color)MutedBrown;
                    }
                    if (tabRects != null && i < tabRects.Length && tabRects[i] != null)
                    {
                        float height = selected ? 42f : 36f;
                        tabRects[i].sizeDelta = new Vector2(tabRects[i].sizeDelta.x, height);
                    }
                }
            }

            // Populate rows
            RuleItem[] items = _currentTab switch
            {
                0 => BasicsRules,
                1 => MovesRules,
                _ => ScoringRules
            };

            PopulateRows(items);

            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = new Vector2(0f, 1f);
            }
        }

        private void PopulateRows(RuleItem[] items)
        {
            foreach (var r in _spawnedRows)
            {
                if (r != null) Destroy(r);
            }
            _spawnedRows.Clear();

            if (contentContainer == null || ruleRowPrefab == null) return;

            for (int i = 0; i < items.Length; i++)
            {
                var rowObj = Instantiate(ruleRowPrefab, contentContainer);
                rowObj.SetActive(true);
                _spawnedRows.Add(rowObj);

                // Configure row
                var titleText = rowObj.transform.Find("TextGroup/Title")?.GetComponent<TextMeshProUGUI>();
                var bodyText = rowObj.transform.Find("TextGroup/Body")?.GetComponent<TextMeshProUGUI>();
                var numberText = rowObj.transform.Find("IconWell/Number")?.GetComponent<TextMeshProUGUI>();
                var iconImg = rowObj.transform.Find("IconWell/Icon")?.GetComponent<Image>();

                if (titleText != null) titleText.text = items[i].title;
                if (bodyText != null) bodyText.text = items[i].body;
                if (numberText != null) numberText.text = (i + 1).ToString();

                if (iconImg != null)
                {
                    if (items[i].icon != null)
                    {
                        iconImg.sprite = items[i].icon;
                        iconImg.gameObject.SetActive(true);
                        if (numberText != null) numberText.gameObject.SetActive(false);
                    }
                    else
                    {
                        iconImg.gameObject.SetActive(false);
                        if (numberText != null) numberText.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_pausedTurnTimer && UIManager.Instance != null)
            {
                UIManager.Instance.ResumeTurnTimer();
                _pausedTurnTimer = false;
            }
        }

        /// <summary>Opens the Rules modal.</summary>
        public static RulesModal Open(Action onDismissed = null)
        {
            return (RulesModal)UIModal.Open("Rules", onDismissed);
        }

        /// <summary>Checks whether this is the player's first match and auto-opens rules if so.</summary>
        public static bool CheckFirstMatchAutoOpen()
        {
            if (PlayerPrefs.GetInt(PrefKeyAutoOpened, 0) == 0)
            {
                PlayerPrefs.SetInt(PrefKeyAutoOpened, 1);
                PlayerPrefs.Save();
                Open();
                return true;
            }
            return false;
        }
    }
}
