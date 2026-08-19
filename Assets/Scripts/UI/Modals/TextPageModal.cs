using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// A scrollable block of prose — About, Privacy Policy, Terms.
    ///
    /// Google Play requires a reachable privacy policy, so these rows have to lead somewhere
    /// real rather than being decorative chevrons.
    /// </summary>
    public class TextPageModal : UIModal
    {
        [Header("Wiring")]
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI bodyLabel;
        public ClayButton closeButton;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Dismiss);
        }

        public void Configure(string title, string body)
        {
            if (titleLabel != null) titleLabel.text = title;
            if (bodyLabel != null) bodyLabel.text = body;
        }
    }
}
