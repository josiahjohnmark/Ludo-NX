using System;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// An on/off switch for the PNG-chassis popups: a pill track that turns green, and a round
    /// knob that slides across. The Clay-system ToggleSwitch stays for the older screens.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PillToggle : MonoBehaviour
    {
        public Image track;
        public RectTransform knob;
        public Color onColour = new Color32(0x4D, 0xAE, 0x51, 255);
        public Color offColour = new Color32(0xB9, 0xA8, 0x92, 255);
        [Tooltip("Knob x at off and on, in the track's units.")]
        public float offX = -11f, onX = 11f;

        public event Action<bool> Changed;
        public bool IsOn { get; private set; }

        private Coroutine _slide;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => Set(!IsOn, true));
        }

        /// <summary>Show a value without raising Changed — for filling the popup on open.</summary>
        public void SetSilent(bool on) => Set(on, false, instant: true);

        private void Set(bool on, bool notify, bool instant = false)
        {
            IsOn = on;
            if (track != null) track.color = on ? onColour : offColour;
            if (knob != null)
            {
                float x = on ? onX : offX;
                if (instant || !isActiveAndEnabled) knob.anchoredPosition = new Vector2(x, knob.anchoredPosition.y);
                else
                {
                    if (_slide != null) StopCoroutine(_slide);
                    _slide = StartCoroutine(Slide(x));
                }
            }
            if (notify)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                Changed?.Invoke(on);
            }
        }

        private System.Collections.IEnumerator Slide(float to)
        {
            float from = knob.anchoredPosition.x, t0 = Time.unscaledTime;
            const float dur = 0.14f;   // buttonPress.releaseMs
            while (true)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - t0) / dur);
                float e = 1f - (1f - t) * (1f - t);
                knob.anchoredPosition = new Vector2(Mathf.Lerp(from, to, e), knob.anchoredPosition.y);
                if (t >= 1f) yield break;
                yield return null;
            }
        }
    }
}
