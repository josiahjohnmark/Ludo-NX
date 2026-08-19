using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The player's chosen profile picture.
    ///
    /// Stored as an index rather than a path so the pictures can be renamed, reordered or added
    /// to without invalidating anyone's saved choice, and so nothing has to be serialised beyond
    /// a single int.
    /// </summary>
    public static class AvatarService
    {
        private const string Key = "ludonx_avatar";

        private static int _index = -1;

        internal static void ResetStatics() { _index = -1; }

        /// <summary>Raised when the player picks a different picture, so live UI can refresh.</summary>
        public static event System.Action Changed;

        public static int Count
        {
            get
            {
                var set = UIIconSet.Current;
                return set != null && set.avatars != null ? set.avatars.Length : 0;
            }
        }

        public static int Index
        {
            get
            {
                if (_index < 0) _index = PlayerPrefs.GetInt(Key, 0);
                return Count > 0 ? Mathf.Clamp(_index, 0, Count - 1) : 0;
            }
            set
            {
                if (Count == 0) return;
                _index = Mathf.Clamp(value, 0, Count - 1);
                PlayerPrefs.SetInt(Key, _index);
                PlayerPrefs.Save();
                if (Changed != null) Changed();
            }
        }

        public static Sprite Current
        {
            get
            {
                var set = UIIconSet.Current;
                if (set == null || set.avatars == null || set.avatars.Length == 0) return null;
                return set.avatars[Index];
            }
        }

        public static Sprite At(int i)
        {
            var set = UIIconSet.Current;
            if (set == null || set.avatars == null || i < 0 || i >= set.avatars.Length) return null;
            return set.avatars[i];
        }

        /// <summary>
        /// Fits a square photo into a round slot.
        ///
        /// The photos are square and the slots are circular, so rather than re-cutting the art
        /// the picture is dropped inside a masked child: the mask supplies the circle, the photo
        /// keeps its own aspect via <c>preserveAspect</c>, and whatever falls outside the circle
        /// is simply not drawn. The same square file therefore works in a round profile ring and
        /// in the rounded-square slot on the in-match card.
        /// </summary>
        public static Image FitInto(RectTransform slot, Sprite circleMask, Sprite photo, float size)
        {
            if (slot == null) return null;

            // The mask itself: a circle whose alpha defines what shows through.
            var maskGo = new GameObject("AvatarMask", typeof(RectTransform), typeof(CanvasRenderer));
            maskGo.transform.SetParent(slot, false);
            var maskRt = (RectTransform)maskGo.transform;
            maskRt.anchorMin = maskRt.anchorMax = new Vector2(0.5f, 0.5f);
            maskRt.sizeDelta = new Vector2(size, size);
            maskRt.anchoredPosition = Vector2.zero;

            var maskImg = maskGo.AddComponent<Image>();
            maskImg.sprite = circleMask;
            maskImg.color = Color.white;
            maskImg.raycastTarget = false;

            var mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;      // the circle defines the hole, it is not drawn

            // The photo, filling the circle and cropped by it.
            var photoGo = new GameObject("AvatarPhoto", typeof(RectTransform), typeof(CanvasRenderer));
            photoGo.transform.SetParent(maskRt, false);
            var photoRt = (RectTransform)photoGo.transform;
            photoRt.anchorMin = Vector2.zero;
            photoRt.anchorMax = Vector2.one;
            photoRt.offsetMin = Vector2.zero;
            photoRt.offsetMax = Vector2.zero;

            var img = photoGo.AddComponent<Image>();
            img.sprite = photo;
            img.material = UIFactory.SpriteMaterial;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }
    }
}
