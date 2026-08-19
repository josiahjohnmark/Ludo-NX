using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LudoNX.UI
{
    /// <summary>
    /// A row of mutually exclusive options in a carved track, with a raised clay thumb that
    /// slides to the selection — 2/4 Players, Classic/Safe Zone, Easy/Normal/Hard.
    ///
    /// The recessed track plus extruded thumb is the whole point: it reads as a physical switch
    /// rather than a set of radio buttons, which is the difference between this feeling like a
    /// game and feeling like a settings form.
    /// </summary>
    [AddComponentMenu("Ludo NX/UI/Segmented Toggle")]
    public class SegmentedToggle : MonoBehaviour
    {
        [System.Serializable]
        public class IntEvent : UnityEvent<int> { }

        public ClayGraphic track;
        public ClayGraphic thumb;
        public List<RectTransform> slots = new List<RectTransform>();
        public List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        public List<ClayButton> hitboxes = new List<ClayButton>();

        public IntEvent onChanged = new IntEvent();

        [SerializeField] private int index;
        public int Index => index;

        private Coroutine _slide;

        private void Awake()
        {
            for (int i = 0; i < hitboxes.Count; i++)
            {
                int captured = i;
                if (hitboxes[i] != null) hitboxes[i].onClick.AddListener(() => Select(captured, true));
            }
            Apply(index, instant: true);
        }

        /// <summary>Moves the selection. Only fires <see cref="onChanged"/> when it really changes.</summary>
        public void Select(int i, bool animate)
        {
            if (i < 0 || i >= slots.Count) return;
            bool changed = i != index;
            index = i;
            Apply(i, !animate);
            if (changed) onChanged.Invoke(i);
        }

        /// <summary>Sets the selection without firing the callback — for restoring saved state.</summary>
        public void SetSilent(int i)
        {
            if (i < 0 || i >= slots.Count) return;
            index = i;
            Apply(i, instant: true);
        }

        private void Apply(int i, bool instant)
        {
            var theme = UITheme.Current;

            // The selected label sits on the thumb, so its colour has to follow the thumb's
            // luminance — white on a crimson or blue thumb, dark on a gold one. Unselected
            // labels sit on the track and always take the muted ink.
            Color selectedInk = thumb != null ? theme.InkOn(thumb.color) : theme.ink;
            for (int s = 0; s < labels.Count; s++)
            {
                if (labels[s] == null) continue;
                labels[s].color = UITheme.Lin(s == i ? selectedInk : theme.inkDim);
            }

            if (thumb == null || i >= slots.Count || slots[i] == null) return;

            var thumbRect = (RectTransform)thumb.transform;
            Vector2 target = slots[i].anchoredPosition;
            Vector2 size = slots[i].sizeDelta;
            thumbRect.sizeDelta = size;

            UITween.Stop(_slide);
            if (instant || !Application.isPlaying)
            {
                thumbRect.anchoredPosition = target;
                return;
            }
            Vector2 from = thumbRect.anchoredPosition;
            _slide = UITween.Run(0.24f, UITween.OutQuint,
                t => thumbRect.anchoredPosition = Vector2.LerpUnclamped(from, target, t));
        }
    }
}
