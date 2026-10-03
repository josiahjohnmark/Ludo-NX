using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Plays an animated emoji by flipping through the frames packed into one horizontal sprite
    /// sheet — the flipbook equivalent of <see cref="UISpinner"/>, which spins a transform
    /// instead of swapping sprites.
    ///
    /// No Lottie/WebP plugin and no per-frame Sprite assets in the project: <see cref="texture"/>
    /// is sliced into <see cref="frameCount"/> equal frames at runtime the first time it plays,
    /// and every instance sharing that texture shares the sliced sprites too (see <see cref="_cache"/>),
    /// so ten emotes on screen at once still only cut each sheet up once.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class EmoteFlipbook : MonoBehaviour
    {
        [Tooltip("The packed sprite sheet, e.g. Assets/Art/UI/Emotes/emote_cry.png.")]
        public Texture2D texture;
        public int frameCount = 8;
        [Tooltip("The emoji's own size within each cell - smaller than the cell, which also "
                 + "carries a transparent gutter so filtering never bleeds a neighbour in.")]
        public int contentSize = 96;
        public float framesPerSecond = 10f;
        [Tooltip("Keeps looping while enabled. Off plays once and holds the last frame.")]
        public bool loop = true;
        [Tooltip("Runs on unscaled time so a reaction still animates through a paused match.")]
        public bool useUnscaledTime = true;

        private static readonly System.Collections.Generic.Dictionary<Texture2D, Sprite[]> _cache
            = new System.Collections.Generic.Dictionary<Texture2D, Sprite[]>();

        private Image _image;
        private Sprite[] _frames;
        private float _t;
        private bool _finished;

        private Image Image => _image != null ? _image : (_image = GetComponent<Image>());

        private void OnEnable()
        {
            _t = 0f;
            _finished = false;
            BuildFrames();
            if (_frames != null && _frames.Length > 0) Image.sprite = _frames[0];
        }

        /// <summary>Restarts from frame zero — for re-triggering the same reaction on a second tap.</summary>
        public void Play()
        {
            _t = 0f;
            _finished = false;
            enabled = true;
        }

        private void Update()
        {
            if (_frames == null || _frames.Length == 0 || _finished) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _t += dt * Mathf.Max(1f, framesPerSecond);

            int index = Mathf.FloorToInt(_t);
            if (index >= _frames.Length)
            {
                if (loop)
                {
                    index %= _frames.Length;
                    _t %= _frames.Length;
                }
                else
                {
                    index = _frames.Length - 1;
                    _finished = true;
                }
            }
            Image.sprite = _frames[index];
        }

        /// <summary>
        /// Slices <see cref="texture"/> into <see cref="frameCount"/> equal-width sprites, left
        /// to right, and caches the result per texture so re-playing the same emote (or showing
        /// several copies of it at once) never re-slices.
        /// </summary>
        private void BuildFrames()
        {
            if (texture == null) { _frames = null; return; }
            if (_cache.TryGetValue(texture, out var cached)) { _frames = cached; return; }

            int cellW = texture.width / Mathf.Max(1, frameCount);
            float gutter = (cellW - contentSize) / 2f;
            var frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                // The rect stays inside the gutter, not the full cell, so the sprite's own edge
                // sits on transparent pixels and rendering it small never samples a neighbour.
                var rect = new Rect(i * cellW + gutter, gutter, contentSize, contentSize);
                frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect);
                frames[i].name = $"{texture.name}_{i}";
            }
            _cache[texture] = frames;
            _frames = frames;
        }
    }
}
