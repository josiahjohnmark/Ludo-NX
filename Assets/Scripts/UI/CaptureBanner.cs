using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The big moment when a token is sent home.
    ///
    /// A capture is the most consequential thing that happens in a game of Ludo, and it was
    /// being reported by a small "+3" drifting off a token — the same treatment as any other
    /// score tick, gone before you could read it. This puts it in the middle of the screen at
    /// display size, holds it long enough to actually land, and gets out of the way.
    ///
    /// Built in code rather than authored as a prefab so it survives the screen generators, and
    /// parented to whichever canvas is already carrying the match HUD.
    /// </summary>
    public class CaptureBanner : MonoBehaviour
    {
        private static CaptureBanner _instance;

        public static CaptureBanner Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindFirstObjectByType<CaptureBanner>();
                if (_instance == null)
                {
                    var go = new GameObject("CaptureBanner");
                    _instance = go.AddComponent<CaptureBanner>();
                }
                return _instance;
            }
        }

        [Tooltip("Seconds the banner holds at full size before it leaves.")]
        public float holdSeconds = 0.95f;
        public float riseSeconds = 0.22f;
        public float fallSeconds = 0.34f;

        private CanvasGroup _group;
        private RectTransform _root;
        private TextMeshProUGUI _headline;
        private TextMeshProUGUI _sub;
        private Coroutine _playing;

        /// <summary>Announces a capture in the capturing house's colour.</summary>
        public void Show(GameManager.PlayerColor capturer, int points)
        {
            Build();
            if (_root == null) return;

            var theme = UITheme.Current;
            Color house = theme.ForPlayer(capturer);

            _headline.text = "CAPTURE!";
            _headline.color = UITheme.Lin(Color.Lerp(house, Color.white, 0.45f));
            _sub.text = points > 0 ? "+" + points + " POINTS" : "SENT HOME";
            // Near-white, not theme.ink: the plate behind this is near-black, and the light
            // theme's ink is a dark grey that vanished against it completely.
            _sub.color = UITheme.Lin(new Color(0.90f, 0.92f, 0.94f));

            if (_playing != null) StopCoroutine(_playing);
            _playing = StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            // Unscaled throughout: a capture resolves while the board is mid-animation and the
            // banner must not inherit any slow-motion the move is using.
            float t = 0f;
            while (t < riseSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / riseSeconds);
                float eased = 1f - Mathf.Pow(1f - k, 3f);      // ease-out cubic, no overshoot
                _group.alpha = eased;
                _root.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, eased);
                yield return null;
            }

            _group.alpha = 1f;
            _root.localScale = Vector3.one;

            float hold = 0f;
            while (hold < holdSeconds)
            {
                hold += Time.unscaledDeltaTime;
                yield return null;
            }

            t = 0f;
            while (t < fallSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fallSeconds);
                _group.alpha = 1f - k;
                // Drifts up slightly as it goes, which reads as the moment passing rather than
                // the element being switched off.
                _root.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 46f, k));
                yield return null;
            }

            _group.alpha = 0f;
            _root.anchoredPosition = Vector2.zero;
            _playing = null;
        }

        private void Build()
        {
            if (_root != null) return;

            Canvas host = null;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!c.gameObject.scene.IsValid()) continue;
                if (host == null || c.sortingOrder > host.sortingOrder) host = c;
            }
            if (host == null) return;

            var theme = UITheme.Current;

            var go = new GameObject("CaptureBanner_Root", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(host.transform, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(900f, 260f);
            _root.anchoredPosition = Vector2.zero;

            _group = go.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            // A soft dark plate so the words hold up over the board's own colour, whatever tile
            // the capture happened on.
            var plate = UIFactory.Clay("Plate", _root, new Vector2(760f, 210f), Vector2.zero,
                                       new Color(0.06f, 0.07f, 0.09f, 1f), ClayGraphic.Shape.Panel);
            plate.cornerRadius = 44f;
            plate.shadowSpread = 46f;
            plate.shadowAlpha = 0.34f;
            plate.raycastTarget = false;

            _headline = UIFactory.Label("Headline", _root, new Vector2(760f, 96f), new Vector2(0f, 34f),
                                        "CAPTURE!", UIFontSet.Current.display, 76f, Color.white);
            _headline.fontStyle = FontStyles.Bold;
            _headline.characterSpacing = 4f;
            _headline.raycastTarget = false;

            _sub = UIFactory.Label("Sub", _root, new Vector2(760f, 54f), new Vector2(0f, -46f),
                                   "+3 POINTS", UIFontSet.Current.body, 38f, Color.white);
            _sub.fontStyle = FontStyles.Bold;
            _sub.characterSpacing = 6f;
            _sub.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
