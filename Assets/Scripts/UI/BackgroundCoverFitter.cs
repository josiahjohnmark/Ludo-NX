using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Sizes a full-screen UI <see cref="Image"/> so its sprite covers the parent
    /// on any aspect ratio without distorting.
    ///
    /// A plain stretched Image squashes the artwork whenever the screen aspect
    /// differs from the source. This keeps the sprite's own aspect and overflows
    /// whichever axis needs to overflow - the same "cover" behaviour as CSS
    /// background-size: cover.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class BackgroundCoverFitter : MonoBehaviour
    {
        [Tooltip("Extra coverage beyond the exact fit, so no edge can show.")]
        [Range(1f, 1.2f)] public float overscan = 1.01f;

        [Tooltip("Which point of the image stays pinned to the same point of the parent. " +
                 "(0.5, 0.5) crops evenly; (0.5, 0) keeps the bottom edge and crops the top.")]
        public Vector2 alignment = new Vector2(0.5f, 0.5f);

        private Image _image;
        private RectTransform _rt;
        private Vector2 _lastParentSize = new Vector2(-1f, -1f);
        private Sprite _lastSprite;

        private void OnEnable() => Fit();

        private void OnRectTransformDimensionsChange() => Fit();

        private void Update()
        {
            // Cheap guard: only recompute when the parent actually resized or the
            // sprite was swapped.
            var parent = _rt != null ? _rt.parent as RectTransform : null;
            if (parent == null) { Fit(); return; }
            if (parent.rect.size != _lastParentSize || (_image != null && _image.sprite != _lastSprite)) Fit();
        }

        public void Fit()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_image == null || _rt == null || _image.sprite == null) return;

            if (_rt.parent is not RectTransform parent) return;

            Vector2 parentSize = parent.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f) return;

            Rect sr = _image.sprite.rect;
            if (sr.width <= 0f || sr.height <= 0f) return;

            _lastParentSize = parentSize;
            _lastSprite = _image.sprite;

            // Anchored and pivoted on the same point, so sizeDelta is the absolute size we
            // control and any overflow falls on the side away from that point.
            _rt.anchorMin = _rt.anchorMax = _rt.pivot = alignment;
            _rt.anchoredPosition = Vector2.zero;

            float spriteAspect = sr.width / sr.height;
            float parentAspect = parentSize.x / parentSize.y;

            // Cover: take the axis that would leave a gap and overflow the other.
            Vector2 size = spriteAspect > parentAspect
                ? new Vector2(parentSize.y * spriteAspect, parentSize.y)
                : new Vector2(parentSize.x, parentSize.x / spriteAspect);

            _rt.sizeDelta = size * overscan;
            _image.preserveAspect = false;
        }
    }
}
