using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// Game mode carousel for the Ludo NX main menu.
    ///
    /// Every card occupies an identical base slot (see <see cref="GameModeCarousel.CardSize"/>);
    /// the only difference between the front card and the two behind it is runtime
    /// scale. Tapping a back card scrolls it to the front while the others scroll
    /// back and shrink. Tapping the front card plays that mode.
    ///
    /// Positions wrap circularly, so with three cards there is always exactly one
    /// card centred and one on each side - the arrangement in the design reference.
    ///
    /// Layout constants are measured from 'ui refence main menu.png' (402x874):
    ///   front card 180x280 at scale 1.0, back cards at scale 0.79, centres +/-97.
    /// </summary>
    public class GameModeCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Base slot every card is built at, in canvas units.</summary>
        public static readonly Vector2 CardSize = new Vector2(180f, 280f);

        public enum Mode
        {
            Online = 0,
            QuickPlay = 1,
            LocalPlay = 2
        }

        [Serializable]
        public class ModeCard
        {
            public Mode mode;
            public RectTransform rectTransform;
            public CanvasGroup canvasGroup;
            public Button button;
        }

        [Header("Cards (left to right, as authored)")]
        public ModeCard[] cards = new ModeCard[3];

        [Header("Layout")]
        [Tooltip("Horizontal distance between adjacent card centres, in canvas units.")]
        public float spacing = 97f;
        [Tooltip("Scale of the card at the front.")]
        public float frontScale = 1f;
        [Tooltip("Scale of the two cards behind. Measured 222/280 in the reference.")]
        public float backScale = 0.79f;
        public float frontAlpha = 1f;
        public float backAlpha = 1f;

        [Header("Motion")]
        public float transitionDuration = 0.3f;
        [Tooltip("Drag distance, in the carousel's own units, needed to advance one card.")]
        public float dragThreshold = 45f;

        [SerializeField] private int activeIndex = 1; // QuickPlay starts at the front

        /// <summary>Raised when the card already at the front is tapped.</summary>
        public event Action<Mode> ActiveCardConfirmed;

        /// <summary>Raised whenever a different card reaches the front.</summary>
        public event Action<Mode> ActiveModeChanged;

        private Coroutine _animation;
        private Vector2 _dragStart;
        private bool _dragging;

        public Mode CurrentMode =>
            IsValidIndex(activeIndex) ? cards[activeIndex].mode : Mode.QuickPlay;

        public int ActiveIndex => activeIndex;

        private bool IsValidIndex(int i) => cards != null && i >= 0 && i < cards.Length;

        private void Awake()
        {
            if (cards == null) return;

            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                var card = cards[i];
                if (card?.button == null) continue;
                card.button.onClick.AddListener(() => HandleCardTapped(index));
            }
        }

        private void OnEnable() => ApplyLayout(animated: false);

        /// <summary>Tap: a back card comes forward, the front card plays.</summary>
        private void HandleCardTapped(int index)
        {
            if (_dragging || !IsValidIndex(index)) return;

            if (index == activeIndex)
            {
                // A screen transition is already animating - a second confirm here is what used
                // to double-push the destination screen and, on a quick tap-tap, leave the
                // bottom nav's fade racing itself into a stuck-hidden state.
                if (ScreenManager.Instance != null && ScreenManager.Instance.IsBusy) return;
                ActiveCardConfirmed?.Invoke(cards[index].mode);
                return;
            }

            FocusCard(index);
        }

        public void SetActiveMode(Mode mode)
        {
            if (cards == null) return;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null && cards[i].mode == mode)
                {
                    FocusCard(i);
                    return;
                }
            }
        }

        /// <summary>Scrolls <paramref name="index"/> to the front, shrinking the rest.</summary>
        public void FocusCard(int index)
        {
            if (!IsValidIndex(index) || index == activeIndex) return;

            activeIndex = index;
            ActiveModeChanged?.Invoke(cards[index].mode);
            ApplyLayout(animated: true);
        }

        public void Next() => Step(+1);

        public void Previous() => Step(-1);

        private void Step(int direction)
        {
            if (cards == null || cards.Length == 0) return;
            int count = cards.Length;
            FocusCard(((activeIndex + direction) % count + count) % count);
        }

        /// <summary>
        /// Signed slot for a card relative to the front one, wrapped so the
        /// furthest card reappears on the opposite side instead of sliding off.
        /// For three cards this yields exactly -1, 0 and +1.
        /// </summary>
        private int SlotOf(int index)
        {
            int count = cards.Length;
            int half = count / 2;
            return ((index - activeIndex + half) % count + count) % count - half;
        }

        private void ApplyLayout(bool animated)
        {
            if (cards == null || cards.Length == 0) return;

            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }

            if (!animated || !isActiveAndEnabled)
            {
                for (int i = 0; i < cards.Length; i++) WriteCard(i);
                SortSiblings();
                return;
            }

            _animation = StartCoroutine(AnimateLayout());
        }

        private IEnumerator AnimateLayout()
        {
            int count = cards.Length;
            var fromPos = new Vector2[count];
            var fromScale = new float[count];
            var fromAlpha = new float[count];

            for (int i = 0; i < count; i++)
            {
                var card = cards[i];
                if (card?.rectTransform == null) continue;
                fromPos[i] = card.rectTransform.anchoredPosition;
                fromScale[i] = card.rectTransform.localScale.x;
                fromAlpha[i] = card.canvasGroup != null ? card.canvasGroup.alpha : 1f;
            }

            // Re-sort up front so the incoming card rises above the others for the
            // whole travel rather than popping over them at the end.
            SortSiblings();

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                t = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic

                for (int i = 0; i < count; i++)
                {
                    var card = cards[i];
                    if (card?.rectTransform == null) continue;

                    int slot = SlotOf(i);
                    bool front = slot == 0;

                    card.rectTransform.anchoredPosition =
                        Vector2.LerpUnclamped(fromPos[i], new Vector2(slot * spacing, 0f), t);

                    float scale = Mathf.LerpUnclamped(fromScale[i], front ? frontScale : backScale, t);
                    card.rectTransform.localScale = new Vector3(scale, scale, 1f);

                    if (card.canvasGroup != null)
                    {
                        card.canvasGroup.alpha =
                            Mathf.LerpUnclamped(fromAlpha[i], front ? frontAlpha : backAlpha, t);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < count; i++) WriteCard(i);
            _animation = null;
        }

        /// <summary>Snaps one card straight to its resting position, scale and alpha.</summary>
        private void WriteCard(int index)
        {
            var card = cards[index];
            if (card?.rectTransform == null) return;

            int slot = SlotOf(index);
            bool front = slot == 0;

            card.rectTransform.anchoredPosition = new Vector2(slot * spacing, 0f);
            float scale = front ? frontScale : backScale;
            card.rectTransform.localScale = new Vector3(scale, scale, 1f);

            if (card.canvasGroup != null) card.canvasGroup.alpha = front ? frontAlpha : backAlpha;
        }

        /// <summary>Front card draws last; the further back a card sits, the earlier it draws.</summary>
        private void SortSiblings()
        {
            // Order by depth (furthest first), then push each into place. Sorting
            // rather than assigning indices directly keeps ties stable.
            var byDepth = new List<int>(cards.Length);
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i]?.rectTransform != null) byDepth.Add(i);
            }
            byDepth.Sort((a, b) => Mathf.Abs(SlotOf(b)).CompareTo(Mathf.Abs(SlotOf(a))));

            foreach (int i in byDepth) cards[i].rectTransform.SetAsLastSibling();
        }

        // ---- swipe -----------------------------------------------------------

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _dragStart = eventData.position;
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            float dx = eventData.position.x - _dragStart.x;

            // Convert the screen-pixel swipe into this carousel's own units (design points
            // when it sits inside a DesignBand), so the threshold means the same physical
            // distance on every device and at every band scale.
            float pixelsPerUnit = Mathf.Abs(transform.lossyScale.x);
            if (pixelsPerUnit > 0f) dx /= pixelsPerUnit;

            if (Mathf.Abs(dx) >= dragThreshold) Step(dx < 0f ? +1 : -1);

            _dragging = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cards != null && cards.Length > 0)
            {
                activeIndex = Mathf.Clamp(activeIndex, 0, cards.Length - 1);
            }
            backScale = Mathf.Clamp(backScale, 0.1f, 1f);
            spacing = Mathf.Max(0f, spacing);
        }
#endif
    }
}
