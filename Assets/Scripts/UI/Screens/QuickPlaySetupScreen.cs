using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Match configuration for the offline modes, all on one screen.
    ///
    /// The three questions are asked in the order they actually depend on each other: how many
    /// houses are in play, which of those the player wants, then how hard the AI should be. The
    /// colour choice only offers houses that are actually in the match, which is why it has to
    /// come after the count.
    /// </summary>
    public class QuickPlaySetupScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton backButton;
        public ClayButton startButton;
        public SegmentedToggle playersToggle;
        public SegmentedToggle modeToggle;
        public SegmentedToggle difficultyToggle;

        [Header("Colour choice")]
        public ClayButton[] colourButtons = new ClayButton[4];
        public ClayGraphic[] colourSwatches = new ClayGraphic[4];
        public ClayGraphic[] colourChecks = new ClayGraphic[4];

        [Header("Difficulty section — hidden in pass-and-play")]
        public GameObject difficultySection;

        [Header("Readout")]
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI modeBlurb;
        public TextMeshProUGUI colourHint;

        /// <summary>Board order, matching the four colour buttons left to right.</summary>
        private static readonly GameManager.PlayerColor[] Houses =
        {
            GameManager.PlayerColor.RED,
            GameManager.PlayerColor.GREEN,
            GameManager.PlayerColor.YELLOW,
            GameManager.PlayerColor.BLUE
        };

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (playersToggle != null) playersToggle.onChanged.AddListener(_ => OnPlayerCountChanged());
            if (difficultyToggle != null) difficultyToggle.onChanged.AddListener(_ => Sync());

            // Safe Zone is not in this release. The segment stays tappable on purpose: a dead
            // control that swallows taps reads as broken, so it snaps back to Classic and says
            // why. The padlock on the label warns before the tap; the toast explains after it.
            if (modeToggle != null)
            {
                modeToggle.SetSilent(0);
                modeToggle.onChanged.AddListener(i =>
                {
                    if (i == 0) return;
                    modeToggle.SetSilent(0);
                    Toast.ComingSoon("Safe Zone");
                });

                // No padlock glyph exists in the icon set, so this borrows the "SOON" wording the
                // Play menu already uses for locked modes — same language, no new asset.
                if (modeToggle.labels.Count > 1 && modeToggle.labels[1] != null)
                {
                    modeToggle.labels[1].text = "SAFE ZONE · SOON";
                    modeToggle.labels[1].color = UITheme.Lin(UITheme.Current.inkDim);
                }
            }

            for (int i = 0; i < colourButtons.Length; i++)
            {
                int index = i;
                if (colourButtons[i] != null)
                    colourButtons[i].onClick.AddListener(() => ChooseColour(index));
            }
        }

        public override void OnShow(object args)
        {
            var s = LudoNXSession.Instance;
            bool passAndPlay = s != null && s.pendingMode == LudoNXSession.Mode.PassAndPlay;

            if (titleLabel != null) titleLabel.text = passAndPlay ? "PASS AND PLAY" : "QUICK PLAY";

            // Difficulty is meaningless with no computer, but the player count is not: two people
            // passing one device is as ordinary as four, and forcing 4 here left the 2-player
            // option visible but dead.
            if (difficultySection != null) difficultySection.SetActive(!passAndPlay);

            if (s != null)
            {
                if (playersToggle != null)
                {
                    playersToggle.SetSilent(s.pendingPlayerCount >= 4 ? 1 : 0);
                    foreach (var h in playersToggle.hitboxes)
                        if (h != null) h.interactable = true;
                }
                if (difficultyToggle != null) difficultyToggle.SetSilent((int)s.pendingDifficulty);
            }

            OnPlayerCountChanged();
        }

        /// <summary>
        /// Two-player is played across a diagonal, so only red and yellow are offered there.
        /// Picking green or blue in a two-player match would put both houses on adjacent corners
        /// and hand one side a shorter run home.
        /// </summary>
        private void OnPlayerCountChanged()
        {
            var s = LudoNXSession.Instance;
            bool four = playersToggle == null || playersToggle.Index == 1;
            if (s != null) s.pendingPlayerCount = four ? 4 : 2;

            for (int i = 0; i < Houses.Length; i++)
            {
                bool available = four || Houses[i] == GameManager.PlayerColor.RED
                                      || Houses[i] == GameManager.PlayerColor.YELLOW;
                if (colourButtons[i] != null) colourButtons[i].interactable = available;
                if (colourSwatches[i] != null)
                {
                    colourSwatches[i].color = available
                        ? UITheme.Current.ForPlayer(Houses[i])
                        : UITheme.Current.locked;
                    colourSwatches[i].SetVerticesDirty();
                }
            }

            // If the current pick is no longer available, fall back to red.
            if (s != null)
            {
                bool stillValid = four || s.pendingHumanColor == GameManager.PlayerColor.RED
                                       || s.pendingHumanColor == GameManager.PlayerColor.YELLOW;
                if (!stillValid) s.pendingHumanColor = GameManager.PlayerColor.RED;
            }

            if (colourHint != null)
                colourHint.text = four ? "Pick the house you want to play"
                                       : "Two players use opposite corners";

            Sync();
        }

        private void ChooseColour(int index)
        {
            var s = LudoNXSession.Instance;
            if (s == null || colourButtons[index] == null || !colourButtons[index].interactable) return;
            s.pendingHumanColor = Houses[index];
            SettingsService.Haptic();
            Sync();
        }

        /// <summary>Pushes the current selection into the session and refreshes every readout.</summary>
        private void Sync()
        {
            var s = LudoNXSession.Instance;
            var theme = UITheme.Current;
            if (s == null) return;

            bool passAndPlay = s.pendingMode == LudoNXSession.Mode.PassAndPlay;
            s.pendingSafeZone = false;   // locked for this release
            s.pendingDifficulty = (LudoAI.Difficulty)(difficultyToggle != null ? difficultyToggle.Index : 1);

            for (int i = 0; i < Houses.Length; i++)
            {
                bool chosen = !passAndPlay && s.pendingHumanColor == Houses[i];
                if (colourChecks[i] != null)
                {
                    colourChecks[i].gameObject.SetActive(chosen);
                    colourChecks[i].color = theme.ink;
                    colourChecks[i].SetVerticesDirty();
                }
            }

            if (modeBlurb != null)
            {
                modeBlurb.text = passAndPlay
                    ? (s.pendingPlayerCount >= 4
                        ? "All four houses played by people on this device."
                        : "Two people on this device, playing opposite corners.")
                    : "Standard rules. May the best roller win.";
            }
        }

        private void OnStart()
        {
            Sync();
            ScreenManager.Instance.Push(ScreenId.MatchIntro);
        }
    }
}
