using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// The hub. One hero action, two secondary ones, and a locked teaser — nothing else.
    ///
    /// There is deliberately no tab bar: a bottom nav is an app pattern, it costs ~90px of
    /// vertical space on every screen, and it would duplicate the buttons already on this page.
    /// Everything else in the game is pushed on top of this and popped back off.
    /// </summary>
    public class HomeScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton playButton;
        public ClayButton settingsButton;
        public ClayButton avatarButton;
        public ClayButton tournamentsButton;

        [Header("Readout")]
        public TextMeshProUGUI playerNameLabel;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            // The avatar in the top bar IS the way into the profile — a second button under
            // PLAY was a duplicate route to the same screen, and Stats now lives inside the
            // profile rather than as its own destination.
            if (avatarButton != null) avatarButton.onClick.AddListener(OnProfile);
            if (tournamentsButton != null) tournamentsButton.SetLocked(true);
        }

        public override void OnShow(object args)
        {
            var session = LudoNXSession.Instance;
            if (playerNameLabel != null)
            {
                playerNameLabel.text = session != null ? session.PlayerName : "Player";
            }
        }

        private void OnPlay() => ScreenManager.Instance.Push(ScreenId.PlayMenu);
        private void OnProfile() => ScreenManager.Instance.Push(ScreenId.Profile);
        private void OnSettings() => ScreenManager.Instance.Push(ScreenId.Settings);
    }
}
