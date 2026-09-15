using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Smooth ambient atmospheric scroll for game backgrounds.
    /// Animates the UV coordinates of one or more RawImages to create a living,
    /// drifting smoke / fog / game motif atmosphere behind the UI.
    /// </summary>
    [ExecuteAlways]
    public class UISmokeScroll : MonoBehaviour
    {
        [Header("Primary Smoke Layer")]
        public RawImage smokeImage;
        public float speedX = 0.012f;
        public float speedY = 0.006f;

        [Header("Ambient Motifs Layer (Optional)")]
        public RawImage motifsImage;
        public float motifsSpeedX = -0.005f;
        public float motifsSpeedY = 0.008f;

        private void Awake()
        {
            if (smokeImage == null)
                smokeImage = GetComponent<RawImage>();
        }

        private void Update()
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;

            if (smokeImage != null)
            {
                Rect r = smokeImage.uvRect;
                r.x = (r.x + speedX * dt) % 1f;
                r.y = (r.y + speedY * dt) % 1f;
                smokeImage.uvRect = r;
            }

            if (motifsImage != null)
            {
                Rect mr = motifsImage.uvRect;
                mr.x = (mr.x + motifsSpeedX * dt) % 1f;
                mr.y = (mr.y + motifsSpeedY * dt) % 1f;
                motifsImage.uvRect = mr;
            }
        }
    }
}
