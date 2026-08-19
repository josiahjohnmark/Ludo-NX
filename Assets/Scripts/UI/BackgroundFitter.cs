using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Scales a world-space sprite so it always covers the orthographic camera's view.
    ///
    /// The match background has to be a world object rather than a UI image: the HUD canvas is
    /// a Screen-Space-Overlay, so anything drawn there would sit on top of the board instead of
    /// behind it. Sitting in world space at a low sorting order puts it under the board and the
    /// tokens while still filling the screen on any aspect ratio.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundFitter : MonoBehaviour
    {
        public Camera targetCamera;

        [Tooltip("Extra coverage beyond the exact view, so no edge shows during camera moves.")]
        public float overscan = 1.02f;

        private SpriteRenderer _sr;
        private float _lastSize = -1f;
        private float _lastAspect = -1f;

        private void OnEnable() { Fit(); }
        private void LateUpdate() { Fit(); }

        public void Fit()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (targetCamera == null) targetCamera = Camera.main;
            if (_sr == null || _sr.sprite == null || targetCamera == null || !targetCamera.orthographic) return;

            if (Mathf.Approximately(_lastSize, targetCamera.orthographicSize) &&
                Mathf.Approximately(_lastAspect, targetCamera.aspect)) return;

            _lastSize = targetCamera.orthographicSize;
            _lastAspect = targetCamera.aspect;

            float viewH = targetCamera.orthographicSize * 2f;
            float viewW = viewH * targetCamera.aspect;

            // Sprite size in world units at scale 1.
            Vector2 spriteWorld = _sr.sprite.bounds.size;
            if (spriteWorld.x <= 0f || spriteWorld.y <= 0f) return;

            // Cover, not fit — take the larger ratio so neither axis leaves a gap.
            float scale = Mathf.Max(viewW / spriteWorld.x, viewH / spriteWorld.y) * overscan;
            transform.localScale = new Vector3(scale, scale, 1f);

            Vector3 camPos = targetCamera.transform.position;
            transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }
    }
}
