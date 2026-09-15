using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Insets a RectTransform to the device's safe area across all mobile form factors:
    /// standard phones, notch/Dynamic Island devices, center punch-hole cameras,
    /// foldable inner/outer displays, trifolds, and tablets.
    ///
    /// Applied to container RectTransforms rather than the canvas itself, allowing
    /// background washes to remain edge-to-edge while interactive controls stay
    /// clear of hardware cutouts and system gesture indicators.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Ludo NX/UI/Safe Area Fitter")]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Axis Constraints")]
        [Tooltip("When true, only insets top/bottom. Recommended for standard portrait phones.")]
        public bool verticalOnly = false;

        [Header("Extra Hardware Padding (Pixels)")]
        [Tooltip("Additional top clearance for large Dynamic Islands and tall camera punch holes.")]
        public float extraTopPadding = 8f;

        [Tooltip("Additional bottom clearance for home gesture bars.")]
        public float extraBottomPadding = 6f;

        [Tooltip("Additional lateral clearance for curved waterfall edges or side fold creases.")]
        public float extraSidePadding = 0f;

        private Rect _appliedSafeArea;
        private ScreenOrientation _appliedOrientation;
        private int _appliedWidth;
        private int _appliedHeight;
        private RectTransform _rt;

        private void Awake()
        {
            _rt = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea == _appliedSafeArea &&
                Screen.orientation == _appliedOrientation &&
                Screen.width == _appliedWidth &&
                Screen.height == _appliedHeight)
            {
                return;
            }

            Apply();
        }

        /// <summary>
        /// Recalculates anchor offsets based on current safe area dimensions.
        /// </summary>
        public void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            if (_rt == null) _rt = (RectTransform)transform;

            _appliedSafeArea = Screen.safeArea;
            _appliedOrientation = Screen.orientation;
            _appliedWidth = Screen.width;
            _appliedHeight = Screen.height;

            Rect sa = _appliedSafeArea;

            // Normalized coordinates (0..1)
            float minX = sa.xMin / Screen.width;
            float minY = sa.yMin / Screen.height;
            float maxX = sa.xMax / Screen.width;
            float maxY = sa.yMax / Screen.height;

            if (verticalOnly)
            {
                minX = 0f;
                maxX = 1f;
            }

            if (float.IsNaN(minX) || float.IsNaN(minY) || float.IsNaN(maxX) || float.IsNaN(maxY)) return;

            // Convert extra padding from reference pixels to normalized screen space
            float topPadNorm = extraTopPadding / Mathf.Max(Screen.height, 1);
            float bottomPadNorm = extraBottomPadding / Mathf.Max(Screen.height, 1);
            float sidePadNorm = extraSidePadding / Mathf.Max(Screen.width, 1);

            _rt.anchorMin = new Vector2(Mathf.Clamp01(minX + (verticalOnly ? 0f : sidePadNorm)),
                                        Mathf.Clamp01(minY + bottomPadNorm));
            _rt.anchorMax = new Vector2(Mathf.Clamp01(maxX - (verticalOnly ? 0f : sidePadNorm)),
                                        Mathf.Clamp01(maxY - topPadNorm));
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
