using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Tints the match background with a soft wash of whoever's turn it is, and eases between
    /// colours when the turn passes.
    ///
    /// This replaces the photographic background: a fixed image says nothing about the state of
    /// the game, whereas the page quietly taking on the current player's colour tells you whose
    /// turn it is from the corner of your eye, without another label competing for attention.
    ///
    /// It drives the camera's clear colour rather than a UI image, because the HUD canvas is a
    /// Screen-Space-Overlay — anything drawn there would cover the board instead of sitting
    /// behind it.
    /// </summary>
    [ExecuteAlways]
    public class MatchAmbience : MonoBehaviour
    {
        public Camera targetCamera;

        [Tooltip("How far the page is lightened from the pure house colour. 0 = full colour, 1 = white.")]
        [Range(0f, 1f)] public float lightenToward = 0.70f;

        [Tooltip("Seconds to cross-fade when the turn changes.")]
        public float blendSeconds = 0.7f;

        private Color _current;
        private Color _target;
        private bool _initialised;

        private void OnEnable()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            _current = _target = Neutral;
            _initialised = false;
        }

        private static Color Neutral => UITheme.Current.groundTop;

        /// <summary>
        /// The page colour for a house: that house's own colour, lightened into a soft pastel.
        ///
        /// Built from the colour outwards rather than from the near-white page inwards. Mixing a
        /// little colour into #F8F9FA reads as "white with a hint", which is what looked washed
        /// out; lightening the actual house colour keeps it unmistakably red, green, yellow or
        /// blue while staying soft enough to sit behind a board.
        /// </summary>
        private Color TintFor(GameManager.PlayerColor c)
        {
            return Color.Lerp(UITheme.Current.ForPlayer(c), Color.white, lightenToward);
        }

        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            var gm = GameManager.Instance;
            bool inMatch = gm != null && gm.activePlayers != null && gm.activePlayers.Count > 0;
            _target = inMatch ? TintFor(gm.currentTurnColor) : Neutral;

            if (!_initialised)
            {
                _current = _target;
                _initialised = true;
            }
            else if (blendSeconds > 0f)
            {
                // Unscaled so the wash keeps easing even while a pause dialog holds the game.
                _current = Color.Lerp(_current, _target, Time.unscaledDeltaTime / blendSeconds);
            }
            else
            {
                _current = _target;
            }

            targetCamera.backgroundColor = _current;
        }
    }
}
