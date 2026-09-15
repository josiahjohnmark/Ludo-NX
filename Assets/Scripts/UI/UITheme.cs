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
        [Header("Ground — the felt table")]
        public Color groundTop = Hex("0B2A1D");
        public Color groundBottom = Hex("04140D");
        public Color panel = Hex("0A1C16");
        public Color panelHigh = Hex("122920");
        public Color recessed = Hex("06120D");
        public Color hairline = Hex("1E4D38");

        [Header("Corner washes — light pooling on the felt")]
        public Color washTopLeft = Hex("164E35");
        public Color washBottomRight = Hex("030D08");

        // ── Actions ────────────────────────────────────────────────────────────────────────
        [Header("Actions")]
        public Color primary = Hex("A61B34");
        public Color primaryDeep = Hex("680D1D");
        public Color secondary = Hex("1B3B6F");
        public Color secondaryDeep = Hex("0F2242");
        public Color success = Hex("16A34A");
        public Color successDeep = Hex("14532D");
        public Color gold = Hex("F5C842");
        public Color goldDeep = Hex("B48218");
        public Color neutral = Hex("1C2735");
        public Color neutralDeep = Hex("101824");
        public Color locked = Hex("1E2734");

        [Header("Navigation Bar")]
        public Color navBarBg = Hex("0B1320");
        public Color navPodDefault = Hex("131B2A");
        public Color navPodActive = Hex("1A2840");
        public Color navGoldBorder = Hex("F5C842");

        // ── Ink ────────────────────────────────────────────────────────────────────────────
        [Header("Ink")]
        public Color ink = Hex("FFFFFF");
        public Color inkMuted = Hex("94A3B8");
        public Color inkDim = Hex("64748B");

        /// <summary>Text sitting directly on the felt, where dark ink would vanish.</summary>
        public Color inkOnGround = Hex("FFFFFF");

        /// <summary>Secondary text on the felt.</summary>
        public Color inkOnGroundDim = Hex("94A3B8");

        /// <summary>Text and icons sitting ON a saturated fill. Always white.</summary>
        public Color onAccent = Hex("FFFFFF");

        /// <summary>
        /// Readable text colour for a given background.
        /// </summary>
        public Color InkOn(Color background)
        {
            float lum = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            return lum < 0.65f ? onAccent : Hex("0F172A");
        }

        // ── Player colours (must match the board — do not restyle these) ───────────────────
        [Header("Players")]
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
        [Header("Clay")]
        public float shadowSpread = 32f;
        [Range(0f, 1f)] public float shadowAlpha = 0.35f;
        [Range(0f, 1f)] public float innerHighlight = 0.32f;
        [Range(0f, 1f)] public float innerShadow = 0.18f;

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
        public static Color Hex(string rrggbb)
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
