using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The Friends hub: a LIST tab (friends, with search that also finds new players) and a
    /// REQUESTS tab (incoming requests with accept / decline).
    ///
    /// All data is local sample data (<see cref="FriendsDemoData"/>) - the game has no online
    /// service yet. Accept / decline fake a short server round-trip so the loading state is
    /// visible, and Challenge honestly reports that challenges are coming soon.
    /// </summary>
    public class FriendsScreen : UIScreen
    {
        public enum Tab { List, Requests }

        [Header("Header")]
        public Button avatarButton;

        [Header("Tabs")]
        public Button listTab;
        public Button requestsTab;
        public Image listTabImage;
        public Image requestsTabImage;
        public Image listTabGlow;
        public Image requestsTabGlow;
        [Tooltip("Scale of the selected tab.")]
        public float selectedScale = 1.1f;
        [Tooltip("Brightness of the tab that is not selected.")]
        [Range(0f, 1f)] public float dimmedBrightness = 0.72f;
        public float tabAnimTime = 0.16f;

        [Header("Search (LIST tab)")]
        public GameObject searchRoot;
        public TMP_InputField searchInput;
        [Tooltip("Pause after typing before other players are looked up.")]
        public float searchDebounce = 0.35f;
        [Tooltip("Pretend server time for a player search.")]
        public float fakeSearchDelay = 0.5f;

        [Header("Panel")]
        [Tooltip("Stretched panel; its top offset changes with the tab.")]
        public RectTransform panel;
        [Tooltip("Panel top in points on the LIST tab (below the search bar).")]
        public float listPanelTop = 225f;
        [Tooltip("Panel top in points on the REQUESTS tab (no search bar).")]
        public float requestsPanelTop = 174f;
        public TextMeshProUGUI panelTitle;
        public ScrollRect scroll;
        public RectTransform content;
        public GameObject emptyState;
        public TextMeshProUGUI emptyTitle;
        public TextMeshProUGUI emptyHint;

        [Header("Templates (kept inactive)")]
        public FriendRowView rowTemplate;
        public RectTransform sectionTemplate;
        public RectTransform searchingTemplate;
        public RectTransform noteTemplate;

        [Header("Art")]
        public Sprite[] avatars;
        public Sprite onlineDot;
        public Sprite offlineDot;

        [Header("Row menu")]
        public GameObject menuRoot;
        public RectTransform menuCard;
        public Button menuBlocker;
        public Button menuChallenge;
        public Button menuRemove;
        public Button menuReport;

        [Header("Confirm dialog")]
        public GameObject dialogRoot;
        public TextMeshProUGUI dialogTitle;
        public TextMeshProUGUI dialogMessage;
        public TextMeshProUGUI dialogConfirmLabel;
        public Button dialogCancel;
        public Button dialogConfirm;

        [Header("Message")]
        public CanvasGroup toast;
        public TextMeshProUGUI toastText;

        [Header("Timing")]
        [Tooltip("Pretend server time for accepting or declining a request.")]
        public float requestProcessTime = 0.6f;

        private const float SlotHeight = 94f;
        private const float NameLeft = 94.5f;

        private List<FriendPerson> _friends;
        private List<FriendPerson> _requests;
        private List<FriendPerson> _players;

        private Tab _tab = Tab.List;
        private float _tabBlend;   // 0 = LIST selected, 1 = REQUESTS selected
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private Coroutine _searchRoutine;
        private Coroutine _toastRoutine;
        private Coroutine _tabRoutine;

        private FriendRowView _menuRow;
        private Action _dialogAction;

        private string Query => searchInput != null ? searchInput.text.Trim() : string.Empty;

        // ======================================================================================
        // lifecycle
        // ======================================================================================

        private void Awake()
        {
            _friends = FriendsDemoData.CreateFriends();
            _requests = FriendsDemoData.CreateRequests();
            _players = FriendsDemoData.CreatePlayers();

            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (sectionTemplate != null) sectionTemplate.gameObject.SetActive(false);
            if (searchingTemplate != null) searchingTemplate.gameObject.SetActive(false);
            if (noteTemplate != null) noteTemplate.gameObject.SetActive(false);

            if (listTab != null) listTab.onClick.AddListener(() => SelectTab(Tab.List, animate: true));
            if (requestsTab != null) requestsTab.onClick.AddListener(() => SelectTab(Tab.Requests, animate: true));
            if (avatarButton != null) avatarButton.onClick.AddListener(OpenProfile);
            if (searchInput != null) searchInput.onValueChanged.AddListener(_ => { if (_tab == Tab.List) RebuildList(); });

            if (menuBlocker != null) menuBlocker.onClick.AddListener(CloseMenu);
            if (menuChallenge != null) menuChallenge.onClick.AddListener(OnMenuChallenge);
            if (menuRemove != null) menuRemove.onClick.AddListener(OnMenuRemove);
            if (menuReport != null) menuReport.onClick.AddListener(OnMenuReport);

            if (dialogCancel != null) dialogCancel.onClick.AddListener(CloseDialog);
            if (dialogConfirm != null) dialogConfirm.onClick.AddListener(() =>
            {
                var action = _dialogAction;
                CloseDialog();
                action?.Invoke();
            });

            if (toast != null) { toast.alpha = 0f; toast.blocksRaycasts = false; }
            CloseMenu();
            CloseDialog();
        }

        public override void OnShow(object args)
        {
            CloseMenu();
            CloseDialog();
            SelectTab(_tab, animate: false);
        }

        public override void OnHide()
        {
            StopSearch();
            CloseMenu();
            CloseDialog();
        }

        public override bool OnBack()
        {
            if (menuRoot != null && menuRoot.activeSelf) { CloseMenu(); return true; }
            if (dialogRoot != null && dialogRoot.activeSelf) { CloseDialog(); return true; }
            return false;
        }

        // ======================================================================================
        // tabs
        // ======================================================================================

        public void SelectTab(Tab tab, bool animate)
        {
            _tab = tab;
            bool list = tab == Tab.List;

            if (searchRoot != null) searchRoot.SetActive(list);
            if (panel != null) panel.offsetMax = new Vector2(panel.offsetMax.x, -(list ? listPanelTop : requestsPanelTop));
            if (panelTitle != null) panelTitle.text = list ? "FRIEND LIST" : "FRIEND REQUEST";

            float target = list ? 0f : 1f;
            if (_tabRoutine != null) StopCoroutine(_tabRoutine);
            if (animate && isActiveAndEnabled) _tabRoutine = StartCoroutine(AnimateTabs(target));
            else { _tabBlend = target; ApplyTabLook(_tabBlend); }

            Rebuild();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private IEnumerator AnimateTabs(float to)
        {
            float from = _tabBlend;
            float t = 0f;
            while (t < tabAnimTime)
            {
                t += Time.unscaledDeltaTime;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / tabAnimTime), 3f);
                _tabBlend = Mathf.Lerp(from, to, e);
                ApplyTabLook(_tabBlend);
                yield return null;
            }
            _tabBlend = to;
            ApplyTabLook(_tabBlend);
            _tabRoutine = null;
        }

        /// <summary>blend 0 = LIST fully selected, 1 = REQUESTS fully selected.</summary>
        private void ApplyTabLook(float blend)
        {
            Style(listTabImage, listTabGlow, 1f - blend);
            Style(requestsTabImage, requestsTabGlow, blend);
        }

        private void Style(Image tab, Image glow, float selected)
        {
            if (tab != null)
            {
                float s = Mathf.Lerp(1f, selectedScale, selected);
                tab.transform.localScale = new Vector3(s, s, 1f);
                float b = Mathf.Lerp(dimmedBrightness, 1f, selected);
                tab.color = new Color(b, b, b, 1f);
                if (selected > 0.5f) tab.transform.SetAsLastSibling();
            }
            if (glow != null)
            {
                glow.color = new Color(1f, 1f, 1f, selected);
                float s = Mathf.Lerp(1f, selectedScale, selected);
                glow.transform.localScale = new Vector3(s, s, 1f);
            }
        }

        // ======================================================================================
        // building the list
        // ======================================================================================

        private void Rebuild()
        {
            if (_tab == Tab.List) RebuildList();
            else RebuildRequests();
        }

        private void RebuildRequests()
        {
            Clear();
            for (int i = 0; i < _requests.Count; i++) SpawnRow(_requests[i], FriendRowView.Kind.Request, i + 1);
            ShowEmpty(_requests.Count == 0, "No friend requests right now", string.Empty);
        }

        private void RebuildList()
        {
            Clear();
            string query = Query;

            if (query.Length == 0)
            {
                for (int i = 0; i < _friends.Count; i++) SpawnRow(_friends[i], FriendRowView.Kind.Friend, i + 1);
                ShowEmpty(_friends.Count == 0, "You have no friends yet", "Search above to find players");
                return;
            }

            // Friends match instantly - they are already on the device. Other players need a
            // (pretend) server lookup, so they appear after a short pause.
            int n = 0;
            foreach (var f in _friends)
            {
                if (!f.Matches(query)) continue;
                if (n == 0) SpawnSection("YOUR FRIENDS");
                SpawnRow(f, FriendRowView.Kind.Friend, ++n);
            }

            SpawnSection("PLAYERS");
            var searching = Spawn(searchingTemplate);
            ShowEmpty(false, null, null);
            _searchRoutine = StartCoroutine(SearchPlayers(query, n, searching));
        }

        private IEnumerator SearchPlayers(string query, int friendMatches, GameObject searching)
        {
            yield return Wait(searchDebounce + fakeSearchDelay);
            Despawn(searching);

            int n = 0;
            foreach (var p in _players)
            {
                if (!p.Matches(query)) continue;
                SpawnRow(p, FriendRowView.Kind.Player, ++n);
            }

            if (n == 0)
            {
                if (friendMatches == 0)
                {
                    Clear();
                    ShowEmpty(true, "No players found", "Check the spelling or search by player ID");
                }
                else
                {
                    var note = Spawn(noteTemplate);
                    var label = note.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label != null) label.text = "No other players found";
                }
            }
            _searchRoutine = null;
        }

        private void StopSearch()
        {
            if (_searchRoutine != null) StopCoroutine(_searchRoutine);
            _searchRoutine = null;
        }

        private void Clear()
        {
            StopSearch();
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
        }

        private GameObject Spawn(RectTransform template)
        {
            var go = Instantiate(template.gameObject, content);
            go.SetActive(true);
            _spawned.Add(go);
            return go;
        }

        private void Despawn(GameObject go)
        {
            if (go == null) return;
            _spawned.Remove(go);
            // Destroy only takes effect at the end of the frame; deactivate first so anything
            // that walks the list this frame (Renumber) no longer counts this row.
            go.SetActive(false);
            Destroy(go);
        }

        private void SpawnSection(string title)
        {
            var go = Spawn(sectionTemplate);
            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = title;
        }

        private void SpawnRow(FriendPerson person, FriendRowView.Kind kind, int number)
        {
            var row = Instantiate(rowTemplate, content);
            row.gameObject.SetActive(true);
            _spawned.Add(row.gameObject);

            row.kind = kind;
            row.person = person;
            row.busy = false;

            row.number.text = number.ToString();
            row.displayName.text = person.name;
            row.subtitle.text = person.subtitle;
            if (avatars != null && avatars.Length > 0) row.portrait.sprite = avatars[person.avatar % avatars.Length];
            row.statusDot.sprite = person.online ? onlineDot : offlineDot;

            bool request = kind == FriendRowView.Kind.Request;
            bool player = kind == FriendRowView.Kind.Player;

            row.moreButton.gameObject.SetActive(!player);
            row.acceptButton.gameObject.SetActive(request);
            row.declineButton.gameObject.SetActive(request);
            row.addButton.gameObject.SetActive(player);
            row.spinner.SetActive(false);

            // Long names end in an ellipsis before they reach whatever sits on the right.
            float nameRight = request ? 209f : player ? 236f : 306f;
            var nameRect = row.displayName.rectTransform;
            nameRect.sizeDelta = new Vector2(nameRight - NameLeft, nameRect.sizeDelta.y);

            if (player) SetSent(row, person.requestSent);

            row.avatarButton.onClick.AddListener(OpenProfile);
            row.moreButton.onClick.AddListener(() => OpenMenu(row));
            row.acceptButton.onClick.AddListener(() => StartCoroutine(Respond(row, accept: true)));
            row.declineButton.onClick.AddListener(() => StartCoroutine(Respond(row, accept: false)));
            row.addButton.onClick.AddListener(() => AddPlayer(row));
        }

        private void ShowEmpty(bool show, string title, string hint)
        {
            if (emptyState == null) return;
            emptyState.SetActive(show);
            if (!show) return;
            if (emptyTitle != null) emptyTitle.text = title;
            if (emptyHint != null)
            {
                emptyHint.text = hint;
                emptyHint.gameObject.SetActive(!string.IsNullOrEmpty(hint));
            }
        }

        /// <summary>Numbers rows 1..n again, restarting after each section header.</summary>
        private void Renumber()
        {
            int n = 0;
            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                if (!child.gameObject.activeSelf) continue;
                var row = child.GetComponent<FriendRowView>();
                if (row == null)
                {
                    if (child.name.StartsWith(sectionTemplate.name, StringComparison.Ordinal)) n = 0;
                    continue;
                }
                row.number.text = (++n).ToString();
            }
        }

        // ======================================================================================
        // row actions
        // ======================================================================================

        private IEnumerator Respond(FriendRowView row, bool accept)
        {
            if (row == null || row.busy) yield break;
            row.busy = true;

            row.acceptButton.gameObject.SetActive(false);
            row.declineButton.gameObject.SetActive(false);
            row.moreButton.interactable = false;
            row.spinner.SetActive(true);

            yield return Wait(requestProcessTime);

            row.spinner.SetActive(false);
            var person = row.person;
            _requests.Remove(person);

            if (accept)
            {
                _friends.Insert(0, new FriendPerson(person.name, person.playerId, person.avatar, person.online,
                    "Friends: Just Now"));
            }

            yield return RemoveRow(row);

            Toast(accept ? $"You and {person.name} are now friends!" : "Request declined");
            if (_tab == Tab.Requests && _requests.Count == 0) RebuildRequests();
        }

        private void AddPlayer(FriendRowView row)
        {
            if (row.person.requestSent) return;
            row.person.requestSent = true;
            SetSent(row, true);
            Toast("Friend request sent");
        }

        private static void SetSent(FriendRowView row, bool sent)
        {
            row.addLabel.text = sent ? "SENT" : "ADD";
            row.addGroup.alpha = sent ? 0.55f : 1f;
            row.addButton.interactable = !sent;
        }

        /// <summary>Slides the card out, then closes the gap it leaves.</summary>
        private IEnumerator RemoveRow(FriendRowView row)
        {
            float width = row.card.rect.width;
            Vector2 start = row.card.anchoredPosition;

            float t = 0f;
            const float slide = 0.25f;
            while (t < slide)
            {
                t += Time.unscaledDeltaTime;
                float e = Mathf.Clamp01(t / slide);
                e = e * e;
                row.card.anchoredPosition = start + new Vector2(-(width + 40f) * e, 0f);
                row.group.alpha = 1f - e;
                yield return null;
            }

            t = 0f;
            const float collapse = 0.2f;
            while (t < collapse)
            {
                t += Time.unscaledDeltaTime;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / collapse), 3f);
                row.layout.preferredHeight = Mathf.Lerp(SlotHeight, 0f, e);
                yield return null;
            }

            Despawn(row.gameObject);
            Renumber();
        }

        private void OpenProfile()
        {
            if (ScreenManager.Instance != null) ScreenManager.Instance.Push(ScreenId.Profile);
        }

        // ======================================================================================
        // row menu
        // ======================================================================================

        private void OpenMenu(FriendRowView row)
        {
            if (row == null || row.busy || menuRoot == null) return;
            _menuRow = row;

            bool friend = row.kind == FriendRowView.Kind.Friend;
            menuChallenge.gameObject.SetActive(friend);
            menuRemove.gameObject.SetActive(friend);
            menuReport.gameObject.SetActive(true);

            menuRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(menuCard);

            // Open beside the dots, below them if there is room, otherwise above.
            var parent = (RectTransform)menuCard.parent;
            Vector3 world = row.moreButton.transform.position;
            Vector2 local = parent.InverseTransformPoint(world);
            float height = menuCard.rect.height;
            float bottom = parent.rect.yMin + 140f; // keep clear of the nav bar
            bool below = local.y - 16f - height > bottom;

            menuCard.pivot = new Vector2(1f, below ? 1f : 0f);
            menuCard.anchorMin = menuCard.anchorMax = new Vector2(0.5f, 0.5f);
            menuCard.anchoredPosition = new Vector2(local.x + 6f, below ? local.y - 16f : local.y + 16f) -
                                        (Vector2)parent.rect.center;
        }

        private void CloseMenu()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
        }

        private void OnMenuChallenge()
        {
            CloseMenu();
            Toast("Challenges are coming soon");
        }

        private void OnMenuRemove()
        {
            var row = _menuRow;
            CloseMenu();
            if (row == null) return;
            OpenDialog("Remove friend?", $"Remove {row.person.name} from your friends?", "REMOVE", () =>
            {
                if (row == null) return;
                _friends.Remove(row.person);
                string name = row.person.name;
                StartCoroutine(RemoveThen(row, () =>
                {
                    Toast($"{name} was removed from your friends");
                    if (_tab == Tab.List && Query.Length == 0 && _friends.Count == 0) RebuildList();
                }));
            });
        }

        private void OnMenuReport()
        {
            var row = _menuRow;
            CloseMenu();
            if (row == null) return;
            OpenDialog("Report player?", $"Report {row.person.name}? Our team will review this player.", "REPORT",
                () => Toast("Thanks - we'll review this player"));
        }

        private IEnumerator RemoveThen(FriendRowView row, Action done)
        {
            row.busy = true;
            yield return RemoveRow(row);
            done?.Invoke();
        }

        // ======================================================================================
        // dialog and message
        // ======================================================================================

        private void OpenDialog(string title, string message, string confirm, Action onConfirm)
        {
            if (dialogRoot == null) { onConfirm?.Invoke(); return; }
            dialogTitle.text = title;
            dialogMessage.text = message;
            dialogConfirmLabel.text = confirm;
            _dialogAction = onConfirm;
            dialogRoot.SetActive(true);
        }

        private void CloseDialog()
        {
            _dialogAction = null;
            if (dialogRoot != null) dialogRoot.SetActive(false);
        }

        public void Toast(string message)
        {
            if (toast == null) return;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            toastText.text = message;
            _toastRoutine = StartCoroutine(ShowToast());
        }

        private IEnumerator ShowToast()
        {
            yield return Fade(toast, toast.alpha, 1f, 0.15f);
            yield return Wait(1.9f);
            yield return Fade(toast, 1f, 0f, 0.3f);
            _toastRoutine = null;
        }

        private static IEnumerator Fade(CanvasGroup g, float from, float to, float time)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / time));
                yield return null;
            }
            g.alpha = to;
        }

        private static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
