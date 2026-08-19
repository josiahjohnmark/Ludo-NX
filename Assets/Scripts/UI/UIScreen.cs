using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>Every full-screen destination in the game. Order is not meaningful.</summary>
    public enum ScreenId
    {
        Splash,
        Home,
        PlayMenu,
        QuickPlaySetup,
        MatchIntro,
        Match,
        Result,
        Profile,
        Settings
    }

    /// <summary>
    /// Base class for a screen. <see cref="ScreenManager"/> owns the lifecycle; a screen never
    /// shows or hides itself, it only reacts.
    ///
    /// <see cref="OnBack"/> returning true means the screen consumed the back gesture — used by
    /// Profile to close its Stats tab before the whole screen pops.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] private ScreenId screenId;

        public ScreenId Id => screenId;

        /// <summary>Set by the builder so the field is correct on the prefab.</summary>
        public void SetId(ScreenId id) => screenId = id;

        private CanvasGroup _group;
        public CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());

        private RectTransform _rect;
        public RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

        /// <summary>Called every time the screen becomes visible, including on back-navigation.</summary>
        public virtual void OnShow(object args) { }

        /// <summary>Called as the screen starts leaving. Stop coroutines and timers here.</summary>
        public virtual void OnHide() { }

        /// <summary>Return true to swallow the back gesture and stay open.</summary>
        public virtual bool OnBack() => false;
    }
}
