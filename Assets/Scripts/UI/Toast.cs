using System.Collections;
using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// A brief message that explains why nothing happened.
    ///
    /// Locked features were disabled buttons with no feedback: tapping SAFE ZONE or PLAY ONLINE
    /// did nothing at all, which reads as a broken control rather than a deliberate one. A
    /// padlock says "not yet" before the tap; this says it after.
    ///
    /// Built in code and parented to the topmost canvas so it works on every screen, including
    /// the generated ones, without being authored into any of them.
    /// </summary>
    public class Toast : MonoBehaviour
    {
        private static Toast _instance;

        public static Toast Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindFirstObjectByType<Toast>();
                if (_instance == null) _instance = new GameObject("Toast").AddComponent<Toast>();
                return _instance;
            }
        }

        public float holdSeconds = 1.6f;

        private CanvasGroup _group;
        private RectTransform _root;
        private TextMeshProUGUI _label;
        private Coroutine _playing;

        /// <summary>Standard wording for anything not in this release.</summary>
        public static void ComingSoon(string feature)
        {
            Instance.Show(feature + " is coming in a later update.");
        }

        public void Show(string message)
        {
            Build();
            if (_root == null) return;

            _label.text = message;
            if (_playing != null) StopCoroutine(_playing);
            _playing = StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            const float fade = 0.18f;

            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fade);
                _group.alpha = k;
                _root.anchoredPosition = new Vector2(0f, Mathf.Lerp(-260f, -200f, k));
                yield return null;
            }
            _group.alpha = 1f;

            float hold = 0f;
            while (hold < holdSeconds)
            {
                hold += Time.unscaledDeltaTime;
                yield return null;
            }

            t = 0f;
            while (t < fade * 1.6f)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = 1f - Mathf.Clamp01(t / (fade * 1.6f));
                yield return null;
            }

            _group.alpha = 0f;
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

            var go = new GameObject("Toast_Root", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(host.transform, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(880f, 120f);
            _root.anchoredPosition = new Vector2(0f, -200f);

            _group = go.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var plate = UIFactory.Clay("Plate", _root, new Vector2(860f, 104f), Vector2.zero,
                                       new Color(0.07f, 0.08f, 0.10f, 1f), ClayGraphic.Shape.Pill);
            plate.shadowSpread = 34f;
            plate.shadowAlpha = 0.30f;
            plate.raycastTarget = false;

            _label = UIFactory.Label("Label", _root, new Vector2(800f, 90f), Vector2.zero,
                                     "", UIFontSet.Current.body, 32f, Color.white);
            _label.fontStyle = FontStyles.Bold;
            _label.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
