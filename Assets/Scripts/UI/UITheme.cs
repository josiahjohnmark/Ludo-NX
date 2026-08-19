using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// "Deep Table" — the single source of truth for every colour, radius and spacing value
    /// in the interface.
    ///
    /// This exists as an asset so the palette can be tuned in the Inspector without a recompile,
    /// but every field carries a working default, so <see cref="Current"/> is always safe to read
    /// even if the asset is missing. Nothing in the UI should hard-code a colour.
    ///
    /// Why dark: the board is a bright, high-saturation object. A near-white ground (the Stitch
    /// reference) drains its contrast and reads as an app; a deep ground makes it pop and reads
    /// as a game.
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "Ludo NX/UI Theme", order = 0)]
    public class UITheme : ScriptableObject
    {
        private static UITheme _current;

        internal static void ResetStatics() { _current = null; }

        /// <summary>The active theme. Loads <c>Resources/UITheme</c>, or falls back to defaults.</summary>
        public static UITheme Current
        {
            get
            {
                if (_current != null) return _current;
                _current = Resources.Load<UITheme>("UITheme");
                if (_current == null) _current = CreateInstance<UITheme>();
                return _current;
            }
        }

        // ── Ground ─────────────────────────────────────────────────────────────────────────
        // Sampled straight off the loader reference: the page is #F8F9FA with a soft pink wash
        // in the top-left corner and a warm cream wash in the bottom-right. Cards sit on it as
        // plain white — that contrast is what gives light claymorphism its lift, and it is also
        // why drop shadows finally do real work here (a dark shadow on a light page reads,
        // where on the previous near-black ground it could only ever glow).
        [Header("Ground")]
        public Color groundTop = Hex("F8F9FA");
        public Color groundBottom = Hex("F8F9FA");
        public Color panel = Hex("FFFFFF");
        public Color panelHigh = Hex("FFFFFF");
        public Color recessed = Hex("EDEEEF");
        public Color hairline = Hex("E2BEC0");

        [Header("Corner washes (sampled from the loader reference)")]
        public Color washTopLeft = Hex("F3D7DE");
        public Color washBottomRight = Hex("EEEADF");

        // ── Actions ────────────────────────────────────────────────────────────────────────
        [Header("Actions")]
        public Color primary = Hex("B60E3D");
        public Color primaryDeep = Hex("91002D");
        public Color secondary = Hex("2B4CDA");
        public Color secondaryDeep = Hex("0031C4");
        public Color success = Hex("2E9E4B");
        public Color successDeep = Hex("1F7A38");
        public Color gold = Hex("FFBA27");
        public Color goldDeep = Hex("996C00");
        public Color neutral = Hex("E7E8E9");
        public Color neutralDeep = Hex("D9DADB");
        public Color locked = Hex("EDEEEF");

        // ── Ink ────────────────────────────────────────────────────────────────────────────
        [Header("Ink")]
        public Color ink = Hex("191C1D");
        public Color inkMuted = Hex("5A4042");
        public Color inkDim = Hex("8E6F71");

        /// <summary>Text and icons sitting ON a saturated fill. Always white.</summary>
        public Color onAccent = Hex("FFFFFF");

        /// <summary>
        /// Readable text colour for a given background. On a light theme a button can be either
        /// a saturated crimson or a pale grey, and a single hard-coded label colour is illegible
        /// on one of them — so pick by luminance instead of guessing per call site.
        /// </summary>
        public Color InkOn(Color background)
        {
            float lum = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            return lum < 0.55f ? onAccent : ink;
        }

        // ── Player colours (must match the board — do not restyle these) ───────────────────
        [Header("Players")]
        // Read off the strip you supplied. These are deliberately muted rather than the bright
        // primaries the board art uses: they have to work as a full-screen page colour behind
        // the board, and a saturated primary at that size is unreadable to sit a match on.
        public Color playerRed = Hex("A62D35");
        public Color playerGreen = Hex("3C6B4C");
        public Color playerYellow = Hex("E0A526");
        public Color playerBlue = Hex("2E5C80");

        // ── Shape ──────────────────────────────────────────────────────────────────────────
        [Header("Radii (px @ 1080 wide)")]
        public float radiusSm = 16f;
        public float radiusMd = 28f;
        public float radiusLg = 40f;
        public float radiusXl = 56f;

        [Header("Spacing (px @ 1080 wide)")]
        public float spaceXs = 8f;
        public float spaceSm = 16f;
        public float spaceMd = 28f;
        public float spaceLg = 48f;
        public float spaceXl = 72f;
        public float marginSide = 56f;

        // ── Clay defaults ──────────────────────────────────────────────────────────────────
        // Softer and wider than the dark theme used. On a light page the shadow is the entire
        // depth cue, so it wants to be large, low-opacity and barely visible up close.
        [Header("Clay")]
        public float shadowSpread = 38f;
        [Range(0f, 1f)] public float shadowAlpha = 0.16f;
        [Range(0f, 1f)] public float innerHighlight = 0.18f;
        [Range(0f, 1f)] public float innerShadow = 0.12f;

        // ── Motion (seconds) ───────────────────────────────────────────────────────────────
        // Slightly longer than before and, more importantly, no overshoot anywhere. Every
        // easing curve in the interface is now monotonic — nothing springs past its target
        // and settles back, which is what read as a shake on tap.
        [Header("Motion")]
        public float pressDown = 0.10f;
        public float pressUp = 0.20f;
        public float screenTransition = 0.30f;
        public float modalTransition = 0.24f;

        /// <summary>The player colour for a board house, so HUD and menus never drift from the board.</summary>
        public Color ForPlayer(GameManager.PlayerColor c)
        {
            switch (c)
            {
                case GameManager.PlayerColor.RED: return playerRed;
                case GameManager.PlayerColor.GREEN: return playerGreen;
                case GameManager.PlayerColor.YELLOW: return playerYellow;
                default: return playerBlue;
            }
        }

        /// <summary>
        /// Every colour here is plain sRGB — exactly the hex a designer would type, and exactly
        /// what the Inspector swatch will show.
        ///
        /// The project renders in Linear colour space, so these have to become linear somewhere.
        /// That conversion happens in the <c>ClayPanel</c> shader rather than here, and the
        /// reason is precision: a Canvas uploads vertex colours as 8-bit. Storing linear floats
        /// means #0E1729 quantises to Color32(1,2,6) and collapses to black, whereas the same
        /// colour kept in sRGB quantises to (14,23,41) and survives. sRGB is perceptually
        /// spaced, which is precisely what 8 bits are designed for.
        ///
        /// For anything NOT drawn by the clay shader — a plain Image, TMP text — pass the
        /// colour through <see cref="Lin"/> so it gets the same conversion.
        /// </summary>
        private static Color Hex(string rrggbb)
        {
            return new Color(
                int.Parse(rrggbb.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                int.Parse(rrggbb.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                int.Parse(rrggbb.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                1f);
        }

        /// <summary>
        /// Render-ready form of a theme colour, for Graphics that are not the clay shader
        /// (plain <c>Image</c>, <c>TextMeshProUGUI</c>). Alpha is preserved.
        /// </summary>
        public static Color Lin(Color srgb)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear) return srgb;
            Color c = srgb.linear;
            c.a = srgb.a;
            return c;
        }

        /// <summary>Same as <see cref="Lin(Color)"/> but with an explicit alpha override.</summary>
        public static Color Lin(Color srgb, float alpha)
        {
            Color c = Lin(srgb);
            c.a = alpha;
            return c;
        }
    }
}
