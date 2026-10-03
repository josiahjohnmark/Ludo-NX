using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// A small persistent banner pinned under the top bar while the device has no connection at
    /// all, clearing the moment it returns — connection-states.json's "No network" state.
    ///
    /// Deliberately not built on <see cref="Toast"/>: a toast is a timed flash that always fades
    /// on its own, and this needs to stay up for as long as the outage lasts, however long that
    /// is. Same self-building, attach-to-the-topmost-canvas approach as Toast, so it works on
    /// every screen without being authored into any of them.
    ///
    /// Local and bot matches never touch the network, so this only ever matters for menus and
    /// (once it exists) the online match path — it does not gate or pause anything by itself.
    /// </summary>
    public class NetworkBanner : MonoBehaviour
    {
        private static NetworkBanner _instance;

        public static NetworkBanner Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("NetworkBanner");
                Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<NetworkBanner>();
                return _instance;
            }
        }

        /// <summary>Called once, e.g. from Splash, so the banner starts watching for the rest of
        /// the app's lifetime even before anything else has asked for it.</summary>
        public static void EnsureRunning() => _ = Instance;

        private RectTransform _root;
        private bool _built;
        private bool _lastOffline;
        private float _nextCheck;
        private const float CheckIntervalSeconds = 2f;

        private void Update()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + CheckIntervalSeconds;

            bool offline = Application.internetReachability == NetworkReachability.NotReachable;
            if (offline == _lastOffline) return;
            _lastOffline = offline;
            SetVisible(offline);
        }

        private void SetVisible(bool visible)
        {
            if (visible) EnsureBuilt();
            if (_root == null) return;
            _root.gameObject.SetActive(visible);
        }

        private void EnsureBuilt()
        {
            if (_built) return;

            Canvas host = null;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!c.gameObject.scene.IsValid()) continue;
                if (host == null || c.sortingOrder > host.sortingOrder) host = c;
            }
            if (host == null) return; // scene not ready yet — retried on the next 2 s check

            var go = new GameObject("NetworkBanner_Root", typeof(RectTransform));
            go.transform.SetParent(host.transform, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            _root.sizeDelta = new Vector2(320f, 44f);
            _root.anchoredPosition = new Vector2(0f, -6f);

            var plate = UIFactory.Clay("Plate", _root, new Vector2(320f, 44f), Vector2.zero,
                                       new Color32(183, 39, 31, 235), ClayGraphic.Shape.Pill); // ink.lose #B7271F
            plate.shadowSpread = 20f;
            plate.shadowAlpha = 0.25f;
            plate.raycastTarget = false;

            var label = UIFactory.Label("Label", _root, new Vector2(280f, 32f), Vector2.zero,
                                        "No connection", UIFontSet.Current.body, 18f, Color.white);
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;

            _built = true;
        }
    }
}
