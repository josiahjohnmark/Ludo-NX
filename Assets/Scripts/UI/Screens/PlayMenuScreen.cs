using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Mode select: two offline modes that work, and the online modes shown but locked.
    ///
    /// The locked cards stay visible rather than being hidden, so the roadmap is legible — but
    /// they are non-interactive, because a card that looks live and then does nothing is worse
    /// than one that plainly says it is coming.
    /// </summary>
    public class PlayMenuScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton backButton;
        public ClayButton quickPlayCard;
        public ClayButton passAndPlayCard;
        public ClayButton onlineRandomCard;
        public ClayButton onlineFriendsCard;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (quickPlayCard != null) quickPlayCard.onClick.AddListener(OnQuickPlay);
            if (passAndPlayCard != null) passAndPlayCard.onClick.AddListener(OnPassAndPlay);

            // Online is not in this release.
            if (onlineRandomCard != null) onlineRandomCard.SetLocked(true);
            if (onlineFriendsCard != null) onlineFriendsCard.SetLocked(true);
        }

        /// <summary>One human against AI houses.</summary>
        private void OnQuickPlay()
        {
            var session = LudoNXSession.Instance;
            if (session != null) session.pendingMode = LudoNXSession.Mode.OfflineQuick;
            ScreenManager.Instance.Push(ScreenId.QuickPlaySetup);
        }

        /// <summary>Four humans on one device, no AI.</summary>
        private void OnPassAndPlay()
        {
            var session = LudoNXSession.Instance;
            if (session != null)
            {
                session.pendingMode = LudoNXSession.Mode.PassAndPlay;
                session.pendingPlayerCount = 4;
            }
            ScreenManager.Instance.Push(ScreenId.QuickPlaySetup);
        }
    }
}
