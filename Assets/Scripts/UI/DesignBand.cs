using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Lays out a full-width strip of UI that was authored in design points, so it scales
    /// evenly on every screen and never stretches along one axis.
    ///
    /// Everything inside a band is authored in points of the reference frame (1 unit = 1 point
    /// of, say, the 402x874 main-menu design). The band computes
    ///
    ///     k = min(parentWidth / reference.x, parentHeight / reference.y)
    ///
    /// canvas units per point, then sizes itself to (parentWidth / k) points wide and scales by
    /// k. The result:
    ///  - every child keeps its proportions, because the only scale applied is uniform;
    ///  - the band always spans the whole screen width;
    ///  - on screens wider than the reference the band is wider than the reference in points,
    ///    so children anchored at a fraction of the width spread out, children pinned to an edge
    ///    stay at that edge, and stretch-anchored plates (9-sliced) extend their flat middle.
    ///
    /// This works from the parent rect alone, so it is unaffected by how the CanvasScaler
    /// matches width or height - which is exactly what made the bars break on tablets before.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class DesignBand : MonoBehaviour
    {
        /// <summary>
        /// Top and Bottom hug that edge at a fixed design height. Fill covers the whole parent,
        /// centred. TopFill starts at the top edge and runs down to a reserved strip at the
        /// bottom (the nav bar), so content anchored to its bottom always meets that strip.
        /// </summary>
        public enum Edge { Top, Bottom, Fill, TopFill }

        [Tooltip("Size of the design frame the band's children were measured in, in points.")]
        public Vector2 referenceSize = new Vector2(402f, 874f);

        [Tooltip("Top and Bottom bands hug that screen edge; Fill covers the whole parent.")]
        public Edge edge = Edge.Fill;

        [Tooltip("Height of a Top or Bottom band in points. Ignored for Fill.")]
        public float designHeight = 100f;

        [Header("Top safe area (Top bands only)")]
        [Tooltip("Points the design already leaves free above its content for the status bar.")]
        public float safeAreaBaseline;

        [Tooltip("Stretched child holding the content. Pushed down when a cutout is deeper than the baseline.")]
        public RectTransform safeAreaContent;

        [Header("Bottom reserve (TopFill bands only)")]
        [Tooltip("Height left free at the bottom, in points of Bottom Reserve Reference - e.g. the nav bar's " +
                 "design height, so this band ends exactly where the nav begins on every device.")]
        public float bottomReservePoints;

        [Tooltip("Design frame the reserve height is measured in. Use the nav bar's reference so the reserve " +
                 "scales exactly as the nav bar does.")]
        public Vector2 bottomReserveReference = new Vector2(402f, 874f);

        /// <summary>Canvas units per design point, as last applied.</summary>
        public float Scale { get; private set; } = 1f;

        /// <summary>Band width in points, as last applied. Equals reference.x at the reference aspect.</summary>
        public float WidthPoints { get; private set; }

        /// <summary>Extra points added above the content for a deep cutout, as last applied.</summary>
        public float SafeAreaExtra { get; private set; }

        private RectTransform _rt;
        private Vector2 _lastParentSize = new Vector2(-1f, -1f);
        private Rect _lastSafeArea;
        private float _lastCanvasScale = -1f;

        private void OnEnable() => Apply(force: true);

        private void OnRectTransformDimensionsChange() => Apply(force: false);

        private void LateUpdate() => Apply(force: false);

        /// <summary>Canvas units per point for a parent of this size and a design of this size.</summary>
        public static float ScaleFor(Vector2 parentSize, Vector2 reference)
        {
            if (reference.x <= 0f || reference.y <= 0f) return 1f;
            return Mathf.Min(parentSize.x / reference.x, parentSize.y / reference.y);
        }

        public void Apply(bool force)
        {
            if (_rt == null) _rt = (RectTransform)transform;
            if (_rt.parent is not RectTransform parent) return;

            Vector2 size = parent.rect.size;
            if (size.x <= 0f || size.y <= 0f) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            float canvasScale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            Rect safe = Screen.safeArea;

            // LateUpdate runs every frame; only redo the layout when an input changed or when
            // someone else rewrote our rect. ScreenManager.EnsureBottomNav does exactly that right
            // after instantiating the nav bar, with the screen size unchanged.
            if (!force && size == _lastParentSize && safe == _lastSafeArea &&
                Mathf.Approximately(canvasScale, _lastCanvasScale) && !RectChangedExternally())
            {
                return;
            }

            _lastParentSize = size;
            _lastSafeArea = safe;
            _lastCanvasScale = canvasScale;

            float k = ScaleFor(size, referenceSize);
            if (k <= 0f) return;

            Scale = k;
            WidthPoints = size.x / k;
            _rt.localScale = new Vector3(k, k, 1f);
            _rt.localRotation = Quaternion.identity;

            switch (edge)
            {
                case Edge.Top:
                    SafeAreaExtra = TopSafeAreaExtra(k, canvasScale);
                    Pin(new Vector2(0.5f, 1f), WidthPoints, designHeight + SafeAreaExtra);
                    PushContent(SafeAreaExtra);
                    break;

                case Edge.Bottom:
                    SafeAreaExtra = 0f;
                    Pin(new Vector2(0.5f, 0f), WidthPoints, designHeight);
                    break;

                case Edge.TopFill:
                {
                    SafeAreaExtra = TopSafeAreaExtra(k, canvasScale);
                    float reserve = bottomReservePoints * ScaleFor(size, bottomReserveReference);
                    Pin(new Vector2(0.5f, 1f), WidthPoints, Mathf.Max(0f, size.y - reserve) / k);
                    PushContent(SafeAreaExtra);
                    break;
                }

                default:
                    SafeAreaExtra = 0f;
                    Pin(new Vector2(0.5f, 0.5f), WidthPoints, size.y / k);
                    break;
            }
        }

        private void Pin(Vector2 anchor, float widthPoints, float heightPoints)
        {
            _rt.anchorMin = anchor;
            _rt.anchorMax = anchor;
            _rt.pivot = anchor;
            _rt.anchoredPosition = Vector2.zero;
            _rt.sizeDelta = new Vector2(widthPoints, heightPoints);

            _appliedAnchor = anchor;
            _appliedSize = _rt.sizeDelta;
            _appliedScale = _rt.localScale.x;
        }

        private Vector2 _appliedAnchor = new Vector2(float.NaN, float.NaN);
        private Vector2 _appliedSize;
        private float _appliedScale;

        private bool RectChangedExternally()
        {
            return _rt.anchorMin != _appliedAnchor || _rt.anchorMax != _appliedAnchor ||
                   _rt.anchoredPosition != Vector2.zero || _rt.sizeDelta != _appliedSize ||
                   !Mathf.Approximately(_rt.localScale.x, _appliedScale);
        }

        /// <summary>Points by which a cutout is deeper than the gap the design already leaves.</summary>
        private float TopSafeAreaExtra(float k, float canvasScale)
        {
            if (safeAreaBaseline <= 0f || canvasScale <= 0f || Screen.height <= 0) return 0f;

            float insetPixels = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax);
            float insetPoints = insetPixels / canvasScale / k;
            return Mathf.Max(0f, insetPoints - safeAreaBaseline);
        }

        private void PushContent(float extra)
        {
            if (safeAreaContent == null) return;

            safeAreaContent.anchorMin = Vector2.zero;
            safeAreaContent.anchorMax = Vector2.one;
            safeAreaContent.pivot = new Vector2(0.5f, 1f);
            safeAreaContent.offsetMin = Vector2.zero;
            safeAreaContent.offsetMax = new Vector2(0f, -extra);
        }
    }
}
