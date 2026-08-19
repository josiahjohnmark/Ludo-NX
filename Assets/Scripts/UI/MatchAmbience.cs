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

        [Tooltip("How far the page is deepened from the house colour. 0 = the colour itself, " +
                 "1 = black. Deepened rather than lightened so the page reads as the house " +
                 "colour and the player cards, which use that same colour, still stand off it.")]
        [Range(0f, 1f)] public float deepenBy = 0.52f;

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
        /// The page colour for a house: that house's own colour, taken down into a deep ground.
        ///
        /// Lightening toward white was the mistake. Any amount of it reads as "white with a hint
        /// of colour" — the washed-out pink that made a red turn look nothing like red. Taking
        /// the colour down instead keeps it unmistakably red, green, yellow or blue, gives the
        /// board something to sit on, and leaves the player cards — which are that same house
        /// colour at full strength — standing clearly off the page instead of dissolving into it.
        /// </summary>
        private Color TintFor(GameManager.PlayerColor c)
        {
            Color house = UITheme.Current.ForPlayer(c);
            return Color.Lerp(house, Color.black, deepenBy);
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
