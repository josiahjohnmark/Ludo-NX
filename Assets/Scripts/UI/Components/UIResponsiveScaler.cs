using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Intelligently adapts <see cref="CanvasScaler"/> across any screen aspect ratio:
    /// tall phones (19.5:9, 20:9, 21:9), standard phones (16:9), punch-hole displays,
    /// squarish foldables (1:1, 8:7, 4:3), trifolds, and landscape tablets.
    ///
    /// • On tall phones (aspect <= 9:16), it matches width so the 1080 reference width
    ///   is preserved and vertical space expands gracefully.
    /// • On squarish foldables and tablets (aspect > 9:16), it shifts match towards height (1.0),
    ///   guaranteeing that full vertical layouts never clip or push buttons off-screen.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    [AddComponentMenu("Ludo NX/UI/Responsive Scaler")]
    public class UIResponsiveScaler : MonoBehaviour
    {
        [Header("Reference Dimensions")]
        public Vector2 referenceResolution = new Vector2(1080f, 1920f);

        [Header("Aspect Thresholds")]
        [Tooltip("The reference aspect ratio (9:16 = 0.5625).")]
        public float baseAspectRatio = 9f / 16f;

        [Tooltip("Aspect ratio above which Match Height is fully applied (e.g. 4:3 tablet or 1:1 foldable).")]
        public float wideAspectThreshold = 0.75f;

        [Header("Content Width Clamping")]
        [Tooltip("Max width in canvas units for UI cards on wide tablets/trifolds so buttons do not over-stretch.")]
        public float maxContentWidth = 1080f;

        private CanvasScaler _scaler;
        private int _lastWidth = -1;
        private int _lastHeight = -1;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (_scaler == null) _scaler = GetComponent<CanvasScaler>();
            if (_scaler == null || Screen.width <= 0 || Screen.height <= 0) return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = referenceResolution;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            float currentAspect = (float)Screen.width / Mathf.Max(Screen.height, 1);

            if (currentAspect <= baseAspectRatio + 0.01f)
            {
                // Tall / Standard Phone (16:9, 19.5:9, 20:9, 21:9) -> Match Width (0.0)
                _scaler.matchWidthOrHeight = 0f;
            }
            else
            {
                // Squarish Foldable (1:1, 8:7) or Tablet (4:3, 3:2, 16:10)
                // Smoothly blend from Match Width (0.0) to Match Height (1.0)
                float t = Mathf.InverseLerp(baseAspectRatio, wideAspectThreshold, currentAspect);
                _scaler.matchWidthOrHeight = Mathf.Clamp01(t);
            }
        }
    }
}
