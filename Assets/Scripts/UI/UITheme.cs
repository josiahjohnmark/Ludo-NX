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
        [Header("Ground — the felt table")]
        // Green baize, not burgundy. Burgundy was tried first and looked good on its own, but
        // crimson is both the brand accent and one of the four house colours, so a red ground
        // left the PLAY button as red-on-red with nowhere to stand out. Green gives every
        // accent — crimson, gold, blue — somewhere to sit.
        // The page is a deep burgundy felt, not a near-white sheet. The old ground was #F8F9FA
        // with white cards on it: about 2% apart in lightness, which left the drop shadow as the
        // only thing separating a card from the page — and a soft shadow is the first thing to
        // disappear on a phone in daylight. Every surface below now sits at least 12% clear of
        // the one behind it, so depth survives a cheap screen in bright light.
        public Color groundTop = Hex("1E3F36");
        public Color groundBottom = Hex("122A24");
        public Color panel = Hex("F5F1EC");
        public Color panelHigh = Hex("FFFFFF");
        public Color recessed = Hex("E4DDD6");
        public Color hairline = Hex("3A2D2A");

        [Header("Corner washes — light pooling on the felt")]
        // Warm pools rather than pastel tints: the ground is dark now, so these lift it toward
        // the lamp rather than washing it toward white.
        public Color washTopLeft = Hex("2E5A4C");
        public Color washBottomRight = Hex("2A4E48");

        // ── Actions ────────────────────────────────────────────────────────────────────────
        [Header("Actions")]
        // Muted rather than bright. Saturated primaries at full strength are what make an
        // interface read as a toy; the same hues pulled back read as premium, and they sit
        // properly against a dark ground instead of vibrating on it.
        public Color primary = Hex("A8324A");
        public Color primaryDeep = Hex("7E2136");
        public Color secondary = Hex("2E5C80");
        public Color secondaryDeep = Hex("1F415C");
        public Color success = Hex("3C6B4C");
        public Color successDeep = Hex("2A4E37");
        public Color gold = Hex("D9A441");
        public Color goldDeep = Hex("9A6E14");
        public Color neutral = Hex("EFE9E1");
        public Color neutralDeep = Hex("DCD3C8");
        public Color locked = Hex("CFC6BD");

        // ── Ink ────────────────────────────────────────────────────────────────────────────
        [Header("Ink")]
        public Color ink = Hex("1A1614");
        public Color inkMuted = Hex("6B625B");
        public Color inkDim = Hex("877C73");

        /// <summary>Text sitting directly on the felt, where dark ink would vanish.</summary>
        public Color inkOnGround = Hex("F5F1EC");

        /// <summary>Secondary text on the felt. inkMuted is tuned for light cards and is
        /// unreadable here; this is its counterpart for the dark ground.</summary>
        public Color inkOnGroundDim = Hex("C2CFC7");

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
