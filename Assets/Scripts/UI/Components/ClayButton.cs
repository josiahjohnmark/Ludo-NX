using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// A pressable clay surface.
    ///
    /// The press is the whole point: the button sinks into the page on touch down and springs
    /// back on release, driven by <see cref="ClayGraphic.press01"/> which pulls the drop shadow
    /// in tight and deepens the inner shadow. Content nudges down with it so the label looks
    /// attached to the surface rather than floating above it.
    ///
    /// Built on <see cref="Selectable"/> for the interactable/disabled plumbing, but the colour
    /// tint transition is turned off — the clay shader owns all visual state.
    /// </summary>
    /// <remarks>
    /// Derives from <see cref="Button"/> rather than <see cref="Selectable"/> on purpose.
    /// Existing game code — UIManager's roll, quit and sound references among them — stores
    /// these in <c>Button</c> fields, and a plain Selectable is not a Button, so
    /// <c>GetComponent&lt;Button&gt;()</c> returned null and those buttons silently did nothing.
    /// Inheriting Button also means <c>onClick</c> is the standard one every other Unity tool
    /// already understands.
    /// </remarks>
    [AddComponentMenu("Ludo NX/UI/Clay Button")]
    [RequireComponent(typeof(ClayGraphic))]
    public class ClayButton : Button
    {
        [Tooltip("Moves down by this many pixels while held, so the label sinks with the surface.")]
        public RectTransform content;

        [Tooltip("Optional idle 'breathe' pulse. For the single hero action on a screen only.")]
        public bool breathe;

        private ClayGraphic _clay;
        private Coroutine _pressTween;
        private float _contentRestY;
        private float _breathePhase;

        private const float SinkPixels = 3f;

        public ClayGraphic Clay => _clay != null ? _clay : (_clay = GetComponent<ClayGraphic>());

        protected override void Awake()
        {
            base.Awake();
            transition = Transition.None;   // the shader is the transition
            if (content != null) _contentRestY = content.anchoredPosition.y;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (content != null) _contentRestY = content.anchoredPosition.y;
            SetPress(0f, instant: true);
        }

        private void Update()
        {
            if (!breathe || !IsInteractable()) return;
            _breathePhase += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Sin(_breathePhase * (Mathf.PI * 2f / 2.4f)) * 0.014f;
            transform.localScale = new Vector3(s, s, 1f);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (!IsInteractable()) return;
            Animate(1f, UITheme.Current.pressDown, UITween.OutQuint);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (!IsInteractable()) return;
            Animate(0f, UITheme.Current.pressUp, UITween.OutQuint);
        }

        private void Animate(float target, float duration, System.Func<float, float> ease)
        {
            UITween.Stop(_pressTween);
            float start = Clay != null ? Clay.press01 : 0f;
            _pressTween = UITween.Run(duration, ease,
                t => SetPress(Mathf.LerpUnclamped(start, target, t), instant: false));
        }

        private void SetPress(float v, bool instant)
        {
            if (Clay != null)
            {
                Clay.press01 = v;
                Clay.SetVerticesDirty();
            }
            if (content != null)
            {
                content.anchoredPosition = new Vector2(
                    content.anchoredPosition.x, _contentRestY - SinkPixels * Mathf.Clamp01(v));
            }
            if (instant) UITween.Stop(_pressTween);
        }

        /// <summary>Greys the surface out and stops it taking taps. Used for locked modes.</summary>
        public void SetLocked(bool locked)
        {
            interactable = !locked;
            if (Clay == null) return;
            Clay.color = locked ? UITheme.Current.locked : Clay.color;
            Clay.glowAlpha = locked ? 0f : Clay.glowAlpha;
            Clay.SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            transition = Transition.None;
        }
#endif
    }
}
