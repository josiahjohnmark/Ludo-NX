using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// The type ramp, as asset references.
    ///
    /// Fonts live in <c>Assets/Fonts</c> rather than Resources, so this asset exists to carry
    /// references to them into a runtime-loadable place without duplicating the atlases.
    ///
    /// Baloo 2 for display and Nunito for body is a deliberate departure from the Stitch spec
    /// (Plus Jakarta Sans + Be Vietnam Pro). Those two read as a fintech app; Baloo 2 is rounded
    /// and chunky and reads as a game. Both are already in the project, so the font download
    /// list is empty.
    /// </summary>
    [CreateAssetMenu(fileName = "UIFontSet", menuName = "Ludo NX/UI Font Set", order = 1)]
    public class UIFontSet : ScriptableObject
    {
        private static UIFontSet _current;

        internal static void ResetStatics() { _current = null; }

        public static UIFontSet Current
        {
            get
            {
                if (_current == null) _current = Resources.Load<UIFontSet>("UIFontSet");
                return _current;
            }
        }

        [Tooltip("Baloo2-ExtraBold — headlines, the wordmark, big numbers.")]
        public TMP_FontAsset display;

        [Tooltip("Nunito-Bold — titles, button labels, emphasised body.")]
        public TMP_FontAsset body;

        [Tooltip("Nunito-SemiBold — running text, descriptions.")]
        public TMP_FontAsset bodyLight;

        [Tooltip("PlusJakartaSans-Bold — statistics, scores, anything tabular.")]
        public TMP_FontAsset numeric;

        // Sizes are in canvas pixels at the 1080×1920 reference resolution.
        public const float SizeDisplay = 92f;
        public const float SizeHeadline = 58f;
        public const float SizeTitle = 42f;
        public const float SizeBody = 32f;
        public const float SizeLabel = 26f;
        public const float SizeCaption = 22f;
    }
}
