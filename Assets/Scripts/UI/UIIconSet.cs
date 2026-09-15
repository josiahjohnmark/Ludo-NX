using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Sprite references for the interface.
    ///
    /// The icon library is stored as two parallel arrays rather than a field per icon: there are
    /// fifty of them and they change often, so a named lookup means adding an icon is a matter of
    /// dropping an SVG in the folder and re-running the importer, with no code change anywhere.
    /// </summary>
    [CreateAssetMenu(fileName = "UIIconSet", menuName = "Ludo NX/UI Icon Set", order = 2)]
    public class UIIconSet : ScriptableObject
    {
        private static UIIconSet _current;

        internal static void ResetStatics() { _current = null; _lookup = null; }

        public static UIIconSet Current
        {
            get
            {
                if (_current == null) _current = Resources.Load<UIIconSet>("UIIconSet");
                return _current;
            }
        }

        [Tooltip("The safe-zone star from the board. Reused wherever 'safe' is the idea.")]
        public Sprite star;

        [Tooltip("Player pawn silhouettes, by house order Red/Green/Yellow/Blue.")]
        public Sprite[] pawns = new Sprite[4];

        [Tooltip("The 3D four-colour die from the loader reference.")]
        public Sprite diceLogo;

        [Tooltip("The crimson loading bar from the loader reference.")]
        public Sprite loaderLine;

        [Tooltip("Radial falloff, tinted for the soft corner washes on the page.")]
        public Sprite softGlow;

        [Tooltip("Full-bleed background illustration for all screens.")]
        public Sprite backgroundIllustration;

        [Header("Screen Backgrounds (Nano Banana 3D)")]
        public Sprite bgHomeStadium;
        public Sprite bgShopVault;
        public Sprite bgBattleArena;
        public Sprite bgTournamentHall;

        [Header("3D Game Props & Mascot")]
        [Tooltip("3D Royal Ludo mascot pawn.")]
        public Sprite mascot;
        public Sprite coin3D;
        public Sprite gem3D;
        public Sprite gemGreen3D;
        public Sprite shield3D;
        public Sprite heart3D;

        [Header("Icon library (names match the SVG filenames)")]
        public string[] iconNames = new string[0];
        public Sprite[] iconSprites = new Sprite[0];

        [Header("Profile pictures the player can choose from")]
        public Sprite[] avatars = new Sprite[0];

        private static Dictionary<string, Sprite> _lookup;

        /// <summary>
        /// An icon by its Material Symbols name, e.g. "timer" or "volume_up".
        /// Returns null when the icon is not in the set, so a missing icon leaves a gap rather
        /// than throwing during a build.
        /// </summary>
        public static Sprite Icon(string name)
        {
            var set = Current;
            if (set == null || string.IsNullOrEmpty(name)) return null;

            if (_lookup == null)
            {
                _lookup = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
                int n = Mathf.Min(set.iconNames.Length, set.iconSprites.Length);
                for (int i = 0; i < n; i++)
                {
                    if (string.IsNullOrEmpty(set.iconNames[i])) continue;
                    _lookup[set.iconNames[i]] = set.iconSprites[i];
                }
            }

            Sprite s;
            return _lookup.TryGetValue(name, out s) ? s : null;
        }

        /// <summary>The first icon in the list that exists, so callers can name fallbacks.</summary>
        public static Sprite IconAny(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Sprite s = Icon(names[i]);
                if (s != null) return s;
            }
            return null;
        }
    }
}
