using System;
using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// A two-button question — "quit this match?", "reset your profile?".
    ///
    /// The destructive choice is deliberately the quieter of the two buttons. The loud, obvious
    /// action is the one that keeps you playing, which is the right default when a mis-tap
    /// during a match would throw away the game.
    /// </summary>
    public class ConfirmModal : UIModal
    {
        [Header("Wiring")]
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI bodyLabel;
        public TextMeshProUGUI confirmLabel;
        public TextMeshProUGUI cancelLabel;
        public ClayButton confirmButton;
        public ClayButton cancelButton;

        private Action _onConfirm;

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(() =>
            {
                Action cb = _onConfirm;
                _onConfirm = null;
                Dismiss();
                if (cb != null) cb();
            });
            if (cancelButton != null) cancelButton.onClick.AddListener(Dismiss);
        }

        /// <summary>
        /// <paramref name="confirmText"/> is the destructive option; <paramref name="cancelText"/>
        /// is the loud one that keeps the player where they are.
        /// </summary>
        public void Configure(string title, string body, string confirmText, string cancelText, Action onConfirm)
        {
            if (titleLabel != null) titleLabel.text = title;
            if (bodyLabel != null) bodyLabel.text = body;
            if (confirmLabel != null) confirmLabel.text = confirmText;
            if (cancelLabel != null) cancelLabel.text = cancelText;
            _onConfirm = onConfirm;
        }
    }
}
