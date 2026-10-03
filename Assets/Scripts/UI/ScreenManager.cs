using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The navigation stack. Home is the hub; everything else is pushed on top of it and popped
    /// back off, which is why there is no tab bar anywhere in this interface.
    ///
    /// Screens are prefabs under <c>Resources/UI/Screens/</c>, named after their
    /// <see cref="ScreenId"/>. They are instantiated on first use and then kept, because the
    /// set is small and re-instantiating on every navigation costs more than the memory does.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        /// <summary>Cleared before each play session; see UIStaticReset.</summary>
        public static void ResetStatics() { Instance = null; }

        [Header("Startup")]
        [Tooltip("Open a screen automatically when the scene starts.")]
        public bool openOnStart = true;

        [Tooltip("Which screen to open. Set to Splash once the splash screen exists.")]
        public ScreenId startScreen = ScreenId.Home;

        private readonly Dictionary<ScreenId, UIScreen> _cache = new Dictionary<ScreenId, UIScreen>();
        private readonly List<ScreenId> _stack = new List<ScreenId>();

        private Canvas _canvas;
        private RectTransform _screenRoot;
        private RectTransform _navRoot;
        private RectTransform _modalRoot;
        private UIScreen _active;
        private HomeNavBar _bottomNav;
        private Coroutine _navFadeRoutine;
        private bool _bottomNavTargetVisible = true;
        private bool _busy;

        public Canvas Canvas => _canvas;
        public RectTransform ScreenRoot => _screenRoot;
        public RectTransform NavRoot => _navRoot;
        public RectTransform ModalRoot => _modalRoot;
        public UIScreen Active => _active;
        public HomeNavBar BottomNav => _bottomNav;
        public int Depth => _stack.Count;

        /// <summary>True while a screen transition is animating. Lets input owners (the home
        /// carousel, a modal) hold off on firing a second navigation mid-transition.</summary>
        public bool IsBusy => _busy;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Instance.gameObject == null) Instance = null;
                else { Destroy(gameObject); return; }
            }
            Instance = this;

            // Suppress any legacy preview canvas left over in the scene
            var previewCanvas = GameObject.Find("[PREVIEW] Home Canvas");
            if (previewCanvas != null)
            {
                previewCanvas.SetActive(false);
                Destroy(previewCanvas);
            }

            BuildCanvas();
        }

        private void BuildCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            // The clay shader reads its per-element parameters out of these streams.
            _canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                                             | AdditionalCanvasShaderChannels.TexCoord2
                                             | AdditionalCanvasShaderChannels.TexCoord3;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            var responsive = GetComponent<UIResponsiveScaler>();
            if (responsive == null) responsive = gameObject.AddComponent<UIResponsiveScaler>();
            responsive.Apply();

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            // ScreenRoot does NOT get a SafeAreaFitter — screens manage their own
            // safe area offsets internally. The Home screen's top bar bleeds past the
            // notch/camera cutout, with its inner content placed below the safe zone.
            _screenRoot = MakeFullRect("ScreenRoot", transform);

            // NavRoot does NOT get a SafeAreaFitter — the bottom navigation bar
            // bleeds to the very bottom edge of the screen (Bottom=0).
            // The bar manages its own internal layout and safe zone offsets.
            _navRoot = MakeFullRect("NavRoot", transform);

            _modalRoot = MakeFullRect("ModalRoot", transform);
            var sfModal = _modalRoot.gameObject.AddComponent<SafeAreaFitter>();
            sfModal.verticalOnly = false;
        }

        public void EnsureBottomNav()
        {
            if (_bottomNav != null) return;
            var prefab = Resources.Load<GameObject>("UI/Components/UniversalNavBar");
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("UI/Components/BottomNavBar");
            }
            if (prefab != null)
            {
                var go = Instantiate(prefab, _navRoot != null ? _navRoot : transform);
                go.name = "UniversalNavBar";
                _bottomNav = go.GetComponent<HomeNavBar>();
                var rt = go.transform as RectTransform;
                if (rt != null)
                {
                    // Normalise the anchoring so the bar always bleeds to the bottom
                    // edge, but keep the prefab's own height - it is authored to the
                    // design reference and hardcoding a height here squashes it.
                    float authoredHeight = rt.sizeDelta.y;
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(0f, authoredHeight > 0f ? authoredHeight : 180f);
                }
            }
        }

        public static bool IsHubScreen(ScreenId id)
        {
            switch (id)
            {
                case ScreenId.Home:
                case ScreenId.Shop:
                case ScreenId.ShopItemDetail:
                case ScreenId.PawnCollection:
                case ScreenId.Friends:
                case ScreenId.PlayerStats:
                case ScreenId.Missions:
                case ScreenId.DailyMissions:
                case ScreenId.Levels:
                case ScreenId.Events:
                case ScreenId.EventDetail:
                // Profile is deliberately not here: it has its own back arrow and no bottom nav.
                case ScreenId.Settings:
                case ScreenId.Wallet:
                case ScreenId.Energy:
                    return true;
                default:
                    return false;
            }
        }

        public void UpdateBottomNavVisibility(ScreenId id)
        {
            EnsureBottomNav();
            if (_bottomNav == null) return;

            bool isHub = IsHubScreen(id);
            SetBottomNavVisible(isHub);
            if (isHub)
            {
                _bottomNav.SyncToScreen(id);
            }
        }

        /// <summary>
        /// Shows or hides the bottom nav.
        ///
        /// Cancels any fade already in flight before starting a new one, and the "finish hiding"
        /// callback checks the LATEST requested state rather than the one it captured when it
        /// started. Without both of those, a quick hide-then-show (tap a card, the setup screen
        /// pushes and hides the bar, then something pops straight back to Home before the 0.22s
        /// fade finishes) lets the stale hide callback deactivate the bar after the newer "show"
        /// call already reactivated it - the bar goes dark and stays dark on a screen that still
        /// claims it should be visible. That is the exact symptom reported: the nav vanishing on
        /// Home itself with no screen change to explain it.
        /// </summary>
        public void SetBottomNavVisible(bool visible, bool immediate = false)
        {
            if (_bottomNav == null) return;
            var cg = _bottomNav.GetComponent<CanvasGroup>();
            if (cg == null) cg = _bottomNav.gameObject.AddComponent<CanvasGroup>();

            _bottomNavTargetVisible = visible;
            float targetAlpha = visible ? 1f : 0f;
            cg.blocksRaycasts = visible;
            cg.interactable = visible;

            if (_navFadeRoutine != null)
            {
                UITween.Stop(_navFadeRoutine);
                _navFadeRoutine = null;
            }

            if (immediate)
            {
                cg.alpha = targetAlpha;
                _bottomNav.gameObject.SetActive(visible);
                return;
            }

            if (visible) _bottomNav.gameObject.SetActive(true);
            _navFadeRoutine = UITween.Fade(cg, targetAlpha, 0.22f, () =>
            {
                _navFadeRoutine = null;
                if (!_bottomNavTargetVisible && _bottomNav != null) _bottomNav.gameObject.SetActive(false);
            });
        }

        private void Start()
        {
            EnsureBottomNav();
            if (openOnStart) ResetTo(startScreen);
        }

        private static RectTransform MakeFullRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        // ── Navigation ────────────────────────────────────────────────────────────────────

        /// <summary>Opens a screen on top of the current one. Adds a back step.</summary>
        public void Push(ScreenId id, object args = null) => Go(id, args, push: true);

        /// <summary>Swaps the current screen without adding a back step.</summary>
        public void Replace(ScreenId id, object args = null) => Go(id, args, push: false);

        /// <summary>Pops back one step. Does nothing at the root.</summary>
        public void Pop()
        {
            if (_busy || _stack.Count <= 1) return;
            _stack.RemoveAt(_stack.Count - 1);
            Show(_stack[_stack.Count - 1], null, forward: false);
        }

        /// <summary>Drops everything and makes <paramref name="id"/> the new root. Used by "Home".</summary>
        public void ResetTo(ScreenId id, object args = null)
        {
            if (_busy) return;
            _stack.Clear();
            _stack.Add(id);
            Show(id, args, forward: false);
        }

        private void Go(ScreenId id, object args, bool push)
        {
            if (_busy) return;
            if (push) _stack.Add(id);
            else if (_stack.Count == 0) _stack.Add(id);
            else _stack[_stack.Count - 1] = id;
            Show(id, args, forward: true);
        }

        /// <summary>
        /// Hides the whole screen stack without disturbing it, so the board can take the screen
        /// during a match and the exact same navigation history is still there afterwards.
        /// </summary>
        public void SetScreensVisible(bool visible)
        {
            if (_screenRoot != null) _screenRoot.gameObject.SetActive(visible);
            if (!visible) SetBottomNavVisible(false, immediate: true);
            else if (_active != null) UpdateBottomNavVisibility(_active.Id);
        }

        private void Show(ScreenId id, object args, bool forward)
        {
            UIScreen next = Resolve(id);
            if (next == null)
            {
                Debug.LogError("[ScreenManager] No prefab for screen '" + id + "'. Expected Resources/UI/Screens/" + id);
                return;
            }

            UpdateBottomNavVisibility(id);

            if (next == _active)
            {
                next.OnShow(args);
                return;
            }

            UIScreen previous = _active;
            _active = next;
            _busy = true;

            next.gameObject.SetActive(true);
            next.transform.SetAsLastSibling();
            next.OnShow(args);

            float dur = UITheme.Current.screenTransition;
            float from = forward ? 40f : -40f;

            next.Group.alpha = 0f;
            next.Rect.anchoredPosition = new Vector2(0f, from);
            next.Group.blocksRaycasts = false;

            if (previous != null)
            {
                previous.OnHide();
                previous.Group.blocksRaycasts = false;
                UITween.Fade(previous.Group, 0f, dur * 0.7f, () => previous.gameObject.SetActive(false));
            }

            UITween.Run(dur, UITween.OutQuint, t =>
            {
                next.Group.alpha = Mathf.Clamp01(t * 1.6f);
                next.Rect.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(from, 0f, t));
            }, () =>
            {
                next.Group.alpha = 1f;
                next.Rect.anchoredPosition = Vector2.zero;
                next.Group.blocksRaycasts = true;
                _busy = false;
            });
        }

        private UIScreen Resolve(ScreenId id)
        {
            UIScreen cached;
            if (_cache.TryGetValue(id, out cached) && cached != null) return cached;

            var prefab = Resources.Load<UIScreen>("UI/Screens/" + id);
            if (prefab == null) return null;

            UIScreen instance = Instantiate(prefab, _screenRoot);
            instance.name = prefab.name;
            var rt = instance.Rect;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            instance.gameObject.SetActive(false);
            _cache[id] = instance;
            return instance;
        }

        // ── Hardware back ─────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (_busy) return;

            // While a match is running the board owns the back gesture — MatchBridge turns it
            // into a pause dialog rather than letting it pop a screen out from under the game.
            if (MatchBridge.Instance != null && MatchBridge.Instance.MatchInProgress) return;

            // A modal always wins the back gesture before the screen underneath sees it.
            if (_modalRoot != null && _modalRoot.childCount > 0)
            {
                var top = _modalRoot.GetChild(_modalRoot.childCount - 1).GetComponent<UIModal>();
                if (top != null) { top.Dismiss(); return; }
            }

            if (_active != null && _active.OnBack()) return;
            Pop();
        }
    }
}
