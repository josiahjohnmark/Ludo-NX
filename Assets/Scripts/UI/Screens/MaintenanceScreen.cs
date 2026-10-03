using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Full takeover shown instead of Home when <see cref="MaintenanceService.IsUnderMaintenance"/>
    /// is set. connection-states.json's rule for this one: no dead ends — always a way to at
    /// least reach Support, even with play itself blocked.
    /// </summary>
    public class MaintenanceScreen : UIScreen
    {
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI messageLabel;
        public ClayButton supportButton;

        private void Awake()
        {
            if (supportButton != null) supportButton.onClick.AddListener(OpenSupport);
        }

        public override void OnShow(object args)
        {
            if (titleLabel != null) titleLabel.text = "Back soon";
            if (messageLabel != null) messageLabel.text = MaintenanceService.MaintenanceMessage;
        }

        private void OpenSupport()
        {
            var modal = UIModal.Open("TextPage");
            var page = modal != null ? modal.GetComponent<TextPageModal>() : null;
            page?.Configure("SUPPORT",
                "Need help? Reach us in-app or at the store listing's support link.\n\n" +
                "Replace this text with your real support contact before release.");
        }

        /// <summary>No screen to go back to — maintenance blocks the whole app, not just one page.</summary>
        public override bool OnBack() => true;
    }
}
