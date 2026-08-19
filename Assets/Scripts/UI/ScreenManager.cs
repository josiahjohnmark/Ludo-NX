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
        internal static void ResetStatics() { Instance = null; }

        [Header("Startup")]
        [Tooltip("Open a screen automatically when the scene starts.")]
        public bool openOnStart = true;

        [Tooltip("Which screen to open. Set to Splash once the splash screen exists.")]
        public ScreenId startScreen = ScreenId.Home;

        private readonly Dictionary<ScreenId, UIScreen> _cache = new Dictionary<ScreenId, UIScreen>();
        private readonly List<ScreenId> _stack = new List<ScreenId>();

        private Canvas _canvas;
        private RectTransform _screenRoot;
        private RectTransform _modalRoot;
        private UIScreen _active;
        private bool _busy;

        public Canvas Canvas => _canvas;
        public RectTransform ModalRoot => _modalRoot;
        public UIScreen Active => _active;
        public int Depth => _stack.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
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
            // Match on WIDTH. Width is the dimension a portrait design is fixed to: at 1080
            // reference width the canvas is always 1080 units across, whatever the device, and a
            // taller phone simply gets more vertical room. Matching height instead scales by
            // deviceHeight/1920, so a 20:9 phone scales by 1.25 and squeezes the 1080-wide design
            // into 864 units - every element authored near an edge then renders off-screen.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            // Screens and modals live inside the safe area so a back button never hides under a
            // notch and a bottom row never sits beneath the gesture bar. The canvas itself stays
            // full-bleed, so page backgrounds still reach the physical edges.
            _screenRoot = MakeFullRect("ScreenRoot", transform);
            _screenRoot.gameObject.AddComponent<SafeAreaFitter>();
            _modalRoot = MakeFullRect("ModalRoot", transform);
            _modalRoot.gameObject.AddComponent<SafeAreaFitter>();
        }

        private void Start()
        {
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
        }

        private void Show(ScreenId id, object args, bool forward)
        {
            UIScreen next = Resolve(id);
            if (next == null)
            {
                Debug.LogError("[ScreenManager] No prefab for screen '" + id + "'. Expected Resources/UI/Screens/" + id);
                return;
            }
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
