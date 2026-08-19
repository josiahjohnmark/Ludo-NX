using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Shows the player's chosen picture on their own in-match card.
    ///
    /// Only the house the human is actually playing gets a photo — the AI houses keep their pawn
    /// silhouette, because a stock portrait on a bot would imply another person is in the match.
    /// In pass-and-play every house is a person, but they share one device and one profile, so
    /// the same rule still gives the clearest read.
    /// </summary>
    [AddComponentMenu("Ludo NX/UI/Match HUD Avatar")]
    public class MatchHudAvatar : MonoBehaviour
    {
        public GameManager.PlayerColor playerColour;

        [Tooltip("Photo size inside the card's avatar slot, in canvas pixels.")]
        public float diameter = 96f;

        private Image _photo;
        private bool _built;

        private void OnEnable()
        {
            AvatarService.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            AvatarService.Changed -= Refresh;
        }

        private void Update()
        {
            // The match can start after this component wakes, so the owning house is not known
            // at OnEnable, and it changes between matches. Guarding this on !_built meant a card
            // that had once belonged to the player kept his photo forever: switch from red to
            // blue and both cards showed the same face, because the red one had already built
            // its photo and stopped looking. Refresh is idempotent, so just run it.
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            bool mine = gm != null && gm.humanColor == playerColour;

            if (!mine)
            {
                // Hand the slot back to the pawn silhouette. Hiding the photo alone left the
                // card showing an empty disc, because the pawn was switched off when the photo
                // was first put in and nothing ever switched it back on.
                if (_photo != null) _photo.enabled = false;
                var ownPawn = transform.Find("Pawn");
                if (ownPawn != null) ownPawn.gameObject.SetActive(true);
                return;
            }

            if (_photo == null)
            {
                var circle = Resources.Load<Sprite>("ui_circle");
                _photo = AvatarService.FitInto((RectTransform)transform, circle,
                                               AvatarService.Current, diameter);
                _built = true;
            }

            if (_photo == null) return;
            Sprite s = AvatarService.Current;
            _photo.sprite = s;
            _photo.enabled = s != null;

            // Hide the pawn silhouette behind a real photo.
            var pawn = transform.Find("Pawn");
            if (pawn != null) pawn.gameObject.SetActive(s == null);
        }
    }
}
