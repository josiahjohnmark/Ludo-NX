using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Full takeover blocking play when the installed build is older than
    /// <see cref="MaintenanceService.MinimumVersion"/>. The only way past it is the store listing —
    /// there is deliberately no skip.
    /// </summary>
    public class VersionTooOldScreen : UIScreen
    {
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI messageLabel;
        public ClayButton updateButton;

        private void Awake()
        {
            if (updateButton != null) updateButton.onClick.AddListener(OpenStoreListing);
        }

        public override void OnShow(object args)
        {
            if (titleLabel != null) titleLabel.text = "Update required";
            if (messageLabel != null)
                messageLabel.text = "A new version of Ludo NX is available. Update to keep playing.";
        }

        private void OpenStoreListing()
        {
            // Application.identifier is the real build identifier at runtime — nothing to
            // hand-configure or get stale when the package name changes.
            Application.OpenURL($"https://play.google.com/store/apps/details?id={Application.identifier}");
        }

        public override bool OnBack() => true;
    }
}
