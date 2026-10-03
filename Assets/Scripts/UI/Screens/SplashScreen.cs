using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Brand beat on launch.
    ///
    /// A Ludo match has nothing to stream in, so the bar is honest about being a minimum
    /// display time rather than pretending to measure a load. It still earns its place: it is
    /// the only moment the game gets to introduce itself.
    /// </summary>
    public class SplashScreen : UIScreen
    {
        [Header("Wiring")]
        public RectTransform logoMark;
        public ClayGraphic progressFill;
        public TextMeshProUGUI statusLabel;
        public TextMeshProUGUI versionLabel;

        [Tooltip("Minimum time on screen. Long enough to register, short enough not to annoy.")]
        public float minimumSeconds = 1.6f;

        private float _fillWidth;

        private void Awake()
        {
            if (progressFill != null) _fillWidth = ((RectTransform)progressFill.transform).sizeDelta.x;
            if (versionLabel != null) versionLabel.text = "v" + Application.version;
        }

        public override void OnShow(object args)
        {
            SettingsService.ApplyToEngine();
            NetworkBanner.EnsureRunning();
            if (statusLabel != null) statusLabel.text = "Loading...";
            StartCoroutine(Run());
        }

        private System.Collections.IEnumerator Run()
        {
            var fillRect = progressFill != null ? (RectTransform)progressFill.transform : null;

            if (fillRect != null)
            {
                fillRect.sizeDelta = new Vector2(4f, fillRect.sizeDelta.y);
                UITween.Run(minimumSeconds, UITween.Smooth, t =>
                    fillRect.sizeDelta = new Vector2(Mathf.Max(4f, _fillWidth * t), fillRect.sizeDelta.y));
            }

            // The logo settles once and then holds still. An idle bob here would both fight the
            // reference (which is static) and drift, because offsetting anchoredPosition every
            // frame accumulates rather than oscillating around a fixed origin.
            if (logoMark != null)
            {
                Vector2 rest = logoMark.anchoredPosition;
                logoMark.anchoredPosition = rest + new Vector2(0f, 18f);
                logoMark.localScale = Vector3.one * 0.96f;
                UITween.Run(0.55f, UITween.OutQuint, t =>
                {
                    logoMark.anchoredPosition = Vector2.LerpUnclamped(rest + new Vector2(0f, 18f), rest, t);
                    logoMark.localScale = Vector3.one * Mathf.LerpUnclamped(0.96f, 1f, t);
                });
            }

            float elapsed = 0f;
            while (elapsed < minimumSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Maintenance and version checks are local flags today (no remote config yet — see
            // MaintenanceService), but the gate itself is real: either one blocks Home outright.
            if (MaintenanceService.IsUnderMaintenance)
                ScreenManager.Instance.ResetTo(ScreenId.Maintenance);
            else if (!MaintenanceService.IsVersionSupported())
                ScreenManager.Instance.ResetTo(ScreenId.VersionTooOld);
            else
                ScreenManager.Instance.ResetTo(ScreenId.Home);
        }

        /// <summary>Nothing to go back to during a splash.</summary>
        public override bool OnBack() => true;
    }
}
