using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Drops the player's chosen profile picture into a round slot, and keeps it current.
    ///
    /// The pictures are square and the slots are circular, so instead of asking for re-cut art
    /// the photo is masked: a circle supplies the shape, the photo sits inside it keeping its own
    /// aspect, and anything outside the circle is not drawn. One square file therefore works in
    /// the round profile ring, the round top-bar button and the rounded slot on the match card.
    /// </summary>
    [AddComponentMenu("Ludo NX/UI/Profile Avatar Slot")]
    public class ProfileAvatarSlot : MonoBehaviour
    {
        [Tooltip("Diameter of the photo inside this slot, in canvas pixels.")]
        public float diameter = 96f;

        private Image _photo;

        private void OnEnable()
        {
            Build();
            AvatarService.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            AvatarService.Changed -= Refresh;
        }

        private void Build()
        {
            if (_photo != null) return;

            var circle = Resources.Load<Sprite>("ui_circle");
            if (circle == null)
            {
                var set = UIIconSet.Current;
                circle = set != null ? set.softGlow : null;   // last resort, still round
            }

            _photo = AvatarService.FitInto((RectTransform)transform, circle,
                                           AvatarService.Current, diameter);
        }

        public void Refresh()
        {
            if (_photo == null) return;
            Sprite s = AvatarService.Current;
            _photo.sprite = s;
            // No picture chosen yet: hide the slot rather than drawing an empty white disc.
            _photo.enabled = s != null;
        }
    }
}
