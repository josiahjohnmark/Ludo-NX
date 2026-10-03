using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The popup the top-bar burger opens, built pixel-proportionate to the owner's own Figma
    /// reference (`menu ui refrence and assets.zip`, 27 Sep): a riveted parchment panel under a
    /// "MENU" banner, three labelled groups — PLAYER, ACCOUNTS, SUPPORT — of real icon art.
    ///
    /// Not every label in the reference has a matching screen yet. Ranking, Showroom, Daily
    /// Rewards, Achievements and Inbox are the owner's reference content, not existing Ludo NX
    /// features, so each is pointed at the closest real thing instead of a screen that doesn't
    /// exist (see AI_HANDOFF NOW for the exact reasoning behind each choice). Inbox has nothing
    /// to point at at all, so it says so in a popup rather than faking a destination.
    ///
    /// Every row opens a popup, never a bare toast or a full-screen push with no way back except
    /// a nav bar — Settings/Account/Help/Privacy/Terms/About/Inbox open on `TextPageModal` or
    /// `SettingsModal`/`EditProfileModal`, Ranking opens `NxCupModal`; all four share the Menu's
    /// own riveted-panel chassis (`LudoNXMenuModalBuilder.Shell&lt;T&gt;`). Showroom, Daily Rewards
    /// and Achievements are the exception — they open the real Album/Levels/Player Stats screens,
    /// which have too much content (grids, a level ladder, full stats) to fit a small popup card.
    /// </summary>
    public class MenuModal : UIModal
    {
        [Header("Player")]
        public Button rankingButton;
        public Button showroomButton;
        public Button dailyRewardsButton;
        public Button achievementsButton;

        [Header("Accounts")]
        public Button settingsButton;
        public Button inboxButton;
        public Button accountButton;

        [Header("Support")]
        public Button helpSupportButton;
        public Button privacyButton;
        public Button termsButton;
        public Button aboutButton;

        private void Awake()
        {
            // RANKING has nothing of its own yet — the NX Cup popup is the closest real surface.
            if (rankingButton != null) rankingButton.onClick.AddListener(() => { Dismiss(); NxCupModal.Show(); });
            if (showroomButton != null) showroomButton.onClick.AddListener(() => OpenText("ALBUM",
                "The Album is currently under development. Collectible pawns, dice skins, and stickers will be available soon!"));
            // No separate daily-reward system exists — the mission ladder (Levels) replaced it.
            if (dailyRewardsButton != null) dailyRewardsButton.onClick.AddListener(() => Go(ScreenId.Levels));
            if (achievementsButton != null) achievementsButton.onClick.AddListener(() => Go(ScreenId.PlayerStats));

            if (settingsButton != null) settingsButton.onClick.AddListener(() => { Dismiss(); SettingsModal.Show(); });
            // No mail/notification system exists yet — a popup that says so plainly, not a
            // destination that fakes one, and not a toast either now that every Menu row opens
            // the same kind of popup (owner, 27 Sep: "use that same card style to do everything").
            if (inboxButton != null) inboxButton.onClick.AddListener(() => OpenText("INBOX",
                "Inbox is coming soon. This is where friend requests and game invites will show up."));
            if (accountButton != null) accountButton.onClick.AddListener(OpenEditProfile);

            if (helpSupportButton != null) helpSupportButton.onClick.AddListener(() => OpenText("HELP & SUPPORT",
                "Need help? Reach us in-app or at the store listing's support link.\n\n" +
                "Replace this text with your real support contact before release."));
            if (privacyButton != null) privacyButton.onClick.AddListener(() => OpenText("PRIVACY POLICY", SettingsService.PrivacyText));
            if (termsButton != null) termsButton.onClick.AddListener(() => OpenText("TERMS OF SERVICE", SettingsModal.TermsText));
            if (aboutButton != null) aboutButton.onClick.AddListener(() => OpenText("ABOUT LUDO NX", SettingsModal.AboutText));
        }

        /// <summary>Every row leaves the popup before it navigates, so the destination screen is
        /// never seen sitting under a closing modal.</summary>
        private void Go(ScreenId id)
        {
            Dismiss();
            ScreenManager.Instance.Push(id);
        }

        private void OpenEditProfile()
        {
            Dismiss();
            UIModal.Open("EditProfile")?.GetComponent<EditProfileModal>()?.Configure(null);
        }

        private void OpenText(string title, string body)
        {
            Dismiss();
            var modal = UIModal.Open("TextPage");
            var page = modal != null ? modal.GetComponent<TextPageModal>() : null;
            if (page != null) page.Configure(title, body);
        }
    }
}
