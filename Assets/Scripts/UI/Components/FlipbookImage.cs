using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Plays a strip of sprites as a looping animation on a UI Image — the cheap, mobile-friendly
    /// way to show an "animated emoji" without a video texture or a Lottie runtime: the strip is a
    /// handful of frames thinned down from the source animation, sliced into sub-sprites at import,
    /// and this just swaps which one shows on a timer.
    ///
    /// Each instance randomises its starting frame by default, so a wall of identical emoji does
    /// not all blink in lockstep — it reads as a lively room instead of one asset repeated.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FlipbookImage : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 12f;
        public bool randomizePhase = true;

        private Image _image;
        private float _t;
        private int _frame;

        private void Awake() => _image = GetComponent<Image>();

        private void OnEnable()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (frames == null || frames.Length == 0) return;
            _frame = randomizePhase ? Random.Range(0, frames.Length) : 0;
            _t = 0f;
            _image.sprite = frames[_frame];
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2 || fps <= 0f) return;
            _t += Time.unscaledDeltaTime;
            float frameTime = 1f / fps;
            while (_t >= frameTime)
            {
                _t -= frameTime;
                _frame = (_frame + 1) % frames.Length;
                _image.sprite = frames[_frame];
            }
        }

        /// <summary>Swaps the whole strip — used to hand a slot a different emoji at runtime.</summary>
        public void SetFrames(Sprite[] newFrames, bool restart = true)
        {
            frames = newFrames;
            if (restart && isActiveAndEnabled) OnEnable();
        }
    }
}
