using UnityEngine;
using UnityEngine.Events;

namespace LudoNX.UI
{
    /// <summary>
    /// An on/off switch: a carved track with a clay knob that slides and changes colour.
    /// Used for the sound, music and vibration rows in Settings.
    /// </summary>
    [AddComponentMenu("Ludo NX/UI/Toggle Switch")]
    public class ToggleSwitch : MonoBehaviour
    {
        [System.Serializable]
        public class BoolEvent : UnityEvent<bool> { }

        public ClayGraphic track;
        public ClayGraphic knob;
        public ClayButton hitbox;

        public BoolEvent onChanged = new BoolEvent();

        [SerializeField] private bool isOn = true;
        public bool IsOn => isOn;

        private Coroutine _slide;
        private const float Travel = 34f;

        private void Awake()
        {
            if (hitbox != null) hitbox.onClick.AddListener(Flip);
            Apply(instant: true);
        }

        public void Flip()
        {
            isOn = !isOn;
            Apply(instant: false);
            // No haptic here — vibration is reserved for captures.
            onChanged.Invoke(isOn);
        }

        /// <summary>Sets state without firing the callback — for loading saved preferences.</summary>
        public void SetSilent(bool on)
        {
            isOn = on;
            Apply(instant: true);
        }

        private void Apply(bool instant)
        {
            var theme = UITheme.Current;
            if (track != null)
            {
                track.color = isOn ? theme.success : theme.recessed;
                track.SetVerticesDirty();
            }
            if (knob == null) return;

            var rt = (RectTransform)knob.transform;
            float target = isOn ? Travel : -Travel;

            UITween.Stop(_slide);
            if (instant || !Application.isPlaying)
            {
                rt.anchoredPosition = new Vector2(target, 0f);
                return;
            }
            float from = rt.anchoredPosition.x;
            _slide = UITween.Run(0.20f, UITween.OutQuint,
                t => rt.anchoredPosition = new Vector2(Mathf.LerpUnclamped(from, target, t), 0f));
        }
    }
}
