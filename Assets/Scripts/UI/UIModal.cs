using System;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// A dialog layered over whatever screen is showing — quit confirmation, pause, language
    /// picker, profile editor.
    ///
    /// Modals live on their own root above the screen stack, so opening one never disturbs
    /// navigation history. They open with a scale-up rather than a slide, which is what makes
    /// them read as "on top of" rather than "instead of".
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public class UIModal : MonoBehaviour
    {
        [Tooltip("The panel that scales in. The scrim behind it stays put.")]
        public RectTransform card;

        [Tooltip("Whether tapping the darkened background dismisses the modal.")]
        public bool dismissOnScrimTap = true;

        private CanvasGroup _group;
        private Action _onDismissed;
        private bool _closing;

        public CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());

        /// <summary>Loads and opens a modal prefab from <c>Resources/UI/Modals/</c>.</summary>
        public static UIModal Open(string prefabName, Action onDismissed = null)
        {
            var manager = ScreenManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[UIModal] No ScreenManager in the scene.");
                return null;
            }

            var prefab = Resources.Load<UIModal>("UI/Modals/" + prefabName);
            if (prefab == null)
            {
                Debug.LogError("[UIModal] No prefab at Resources/UI/Modals/" + prefabName);
                return null;
            }

            UIModal modal = Instantiate(prefab, manager.ModalRoot);
            modal.name = prefab.name;
            var rt = (RectTransform)modal.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            modal._onDismissed = onDismissed;
            modal.PlayOpen();
            return modal;
        }

        private void PlayOpen()
        {
            float dur = UITheme.Current.modalTransition;
            Group.alpha = 0f;
            Group.blocksRaycasts = true;
            if (card != null) card.localScale = Vector3.one * 0.96f;

            UITween.Run(dur, UITween.OutQuint, t =>
            {
                Group.alpha = Mathf.Clamp01(t * 1.8f);
                if (card != null) card.localScale = Vector3.one * Mathf.LerpUnclamped(0.96f, 1f, t);
            });
        }

        /// <summary>Closes the modal and destroys it.</summary>
        public void Dismiss()
        {
            if (_closing) return;
            _closing = true;
            Group.blocksRaycasts = false;

            float dur = UITheme.Current.modalTransition * 0.8f;
            UITween.Run(dur, UITween.OutCubic, t =>
            {
                Group.alpha = 1f - t;
                if (card != null) card.localScale = Vector3.one * Mathf.LerpUnclamped(1f, 0.97f, t);
            }, () =>
            {
                Action cb = _onDismissed;
                Destroy(gameObject);
                cb?.Invoke();
            });
        }

        /// <summary>Wired to the scrim button by the builder.</summary>
        public void OnScrimTapped()
        {
            if (dismissOnScrimTap) Dismiss();
        }
    }
}
