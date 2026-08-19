using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Insets a full-screen RectTransform to the device's safe area.
    ///
    /// Without this, content sits under the status bar at the top and the gesture pill or
    /// navigation bar at the bottom. On the phones this game targets that is 90-130px of the
    /// canvas at each end — enough to bury a back button or clip the bottom row of a screen.
    ///
    /// Applied to a full-screen child of the canvas rather than to the canvas itself, so the
    /// background still paints edge to edge while the controls stay clear of the hardware.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Ludo NX/UI/Safe Area Fitter")]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Leave the sides alone. Portrait phones only cut into the top and bottom, and " +
                 "insetting the sides needlessly narrows the playfield.")]
        public bool verticalOnly = true;

        private Rect _applied;
        private ScreenOrientation _orientation;
        private int _w, _h;

        private void OnEnable() { Apply(); }

        private void Update()
        {
            if (Screen.safeArea == _applied && Screen.orientation == _orientation
                && Screen.width == _w && Screen.height == _h) return;
            Apply();
        }

        public void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            _applied = Screen.safeArea;
            _orientation = Screen.orientation;
            _w = Screen.width;
            _h = Screen.height;

            Rect sa = _applied;
            var rt = (RectTransform)transform;

            // Anchors are the right tool here: expressed as a 0-1 fraction of the parent they
            // survive any canvas scale factor, so this needs no knowledge of the reference
            // resolution or the device's density.
            Vector2 min = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            Vector2 max = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);

            if (verticalOnly) { min.x = 0f; max.x = 1f; }

            if (float.IsNaN(min.x) || float.IsNaN(min.y) || float.IsNaN(max.x) || float.IsNaN(max.y)) return;

            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
