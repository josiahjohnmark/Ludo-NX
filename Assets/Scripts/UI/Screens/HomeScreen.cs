using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The hub. Connects to all game screens: Play, Shop, Collection, Wallet, Energy, Missions, and Events.
    /// </summary>
    public class HomeScreen : UIScreen
    {
        [Header("Wiring")]
        public Button playButton;
        public Button settingsButton;
        public Button avatarButton;
        public Button tournamentsButton;
        public Button missionsButton;
        public Button classicModeButton;
        public Button quickModeButton;

        [Header("Top Bar Status Chips")]
        public Button energyChipButton;
        public TextMeshProUGUI energyChipLabel;
        public Button coinsChipButton;
        public TextMeshProUGUI coinsChipLabel;

        [Header("Player Rank Frame & Level Stages")]
        public ClayGraphic avatarRankBorder;
        public ClayGraphic levelBadge;
        public TextMeshProUGUI levelStageLabel;
        public TextMeshProUGUI energyTimerLabel;
        [Tooltip("The pill on the NX MISSIONS badge: this level's missions done, n/5.")]
        public TextMeshProUGUI missionsProgressLabel;
        public ClayButton streakButton;

        [Header("10-Tier Prestige Ranking Badges")]
        public UnityEngine.UI.Image playerRankBadgeImage;
        public Sprite[] rankBadgeSprites = new Sprite[10];

        [Header("Readout")]
        public TextMeshProUGUI playerNameLabel;
        public TextMeshProUGUI playerLevelLabel;

        [Header("Mode Carousel")]
        public GameModeCarousel modeCarousel;

        [Header("Bottom Nav Bar")]
        public HomeNavBar navBar;

        public enum RankTier
        {
            Bronze,
            Silver,
            Gold,
            Platinum,
            Diamond,
            Master
        }

        private Coroutine _timerCoroutine;

        private void Awake()
        {
            if (PlayerPrefs.GetInt("ludonx_fresh_start_v6", 0) == 0)
            {
                PlayerPrefs.SetInt("ludonx_fresh_start_v6", 1);
                PlayerPrefs.Save();
                LudoNXSession.ResetAllToFreshStart();
            }

            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (avatarButton != null) avatarButton.onClick.AddListener(OnProfile);

            if (energyChipButton != null) energyChipButton.onClick.AddListener(OnEnergy);
            if (coinsChipButton != null)
            {
                coinsChipButton.onClick.AddListener(OnWallet);
                // Ensure no [+] button remains on the coins chip
                for (int i = 0; i < coinsChipButton.transform.childCount; i++)
                {
                    var child = coinsChipButton.transform.GetChild(i);
                    if (child.name.ToLowerInvariant().Contains("plus") || child.name.ToLowerInvariant().Contains("add"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            if (tournamentsButton != null)
            {
                tournamentsButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButton(); NxCupModal.Show(); });
            }
            if (missionsButton != null)
            {
                missionsButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButton(); DoorTransition.Play(() => ScreenManager.Instance.Push(ScreenId.Levels)); });
            }
            if (streakButton != null)
            {
                streakButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButton(); ScreenManager.Instance.Push(ScreenId.Events); });
            }
            if (classicModeButton != null)
            {
                classicModeButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButton(); ScreenManager.Instance.Push(ScreenId.QuickPlaySetup); });
            }
            if (quickModeButton != null)
            {
                quickModeButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButton(); ScreenManager.Instance.Push(ScreenId.QuickPlaySetup); });
            }
            if (modeCarousel != null)
            {
                modeCarousel.ActiveCardConfirmed += OnModeCardClicked;
            }
        }

        private void OnEnable()
        {
            EnergyService.Changed += RefreshTopBar;
            _timerCoroutine = StartCoroutine(EnergyTimerLoop());
        }

        private void OnDisable()
        {
            EnergyService.Changed -= RefreshTopBar;
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        public override void OnShow(object args)
        {
            RefreshTopBar();
            if (navBar != null) navBar.SetActiveTab(HomeNavBar.Tab.Home);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
        }

        public void SetRankTier(RankTier tier)
        {
            if (avatarRankBorder == null) return;
            switch (tier)
            {
                case RankTier.Bronze: avatarRankBorder.color = UITheme.Hex("CD7F32"); break;
                case RankTier.Silver: avatarRankBorder.color = UITheme.Hex("D1D5DB"); break;
                case RankTier.Gold: avatarRankBorder.color = UITheme.Hex("F5C842"); break;
                case RankTier.Platinum: avatarRankBorder.color = UITheme.Hex("38BDF8"); break;
                case RankTier.Diamond: avatarRankBorder.color = UITheme.Hex("A855F7"); break;
                case RankTier.Master: avatarRankBorder.color = UITheme.Hex("E11D48"); break;
            }
            avatarRankBorder.SetVerticesDirty();
        }

        public void SetLevelStage(int level)
        {
            level = Mathf.Clamp(level, 1, 10);
            if (playerLevelLabel != null) playerLevelLabel.text = "LVL " + level;
            if (levelStageLabel != null) levelStageLabel.text = level.ToString();

            int badgeIdx = level - 1;
            if (playerRankBadgeImage != null && rankBadgeSprites != null && badgeIdx < rankBadgeSprites.Length && rankBadgeSprites[badgeIdx] != null)
            {
                playerRankBadgeImage.sprite = rankBadgeSprites[badgeIdx];
                playerRankBadgeImage.gameObject.SetActive(true);
            }
        }

        private System.Collections.IEnumerator EnergyTimerLoop()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (true)
            {
                UpdateEnergyCountdown();
                yield return wait;
            }
        }

        private void UpdateEnergyCountdown()
        {
            if (energyTimerLabel == null) return;
            energyTimerLabel.text = EnergyService.TimerText;
        }

        private void RefreshTopBar()
        {
            var session = LudoNXSession.Instance;
            if (playerNameLabel != null)
            {
                playerNameLabel.text = session != null ? session.PlayerName : "Player";
            }

            int lvl = session != null ? Mathf.Clamp(session.Level, 1, 12) : 1;
            SetLevelStage(lvl);

            // Default rank tier is Gold
            SetRankTier(RankTier.Gold);

            if (missionsProgressLabel != null)
                missionsProgressLabel.text = MissionService.IsChampion ? "MAX" : MissionService.DoneCount + "/5";

            if (energyChipLabel != null)
            {
                energyChipLabel.text = EnergyService.CountText;
                energyChipLabel.color = Color.black;
            }

            UpdateEnergyCountdown();

            if (coinsChipLabel != null)
            {
                int coins = session != null ? session.Coins : 0;
                coinsChipLabel.text = coins >= 10000 ? (coins / 1000f).ToString("0.#") + "k" : coins.ToString("N0");
                coinsChipLabel.color = Color.black;
            }

            if (coinsChipButton != null)
            {
                for (int i = 0; i < coinsChipButton.transform.childCount; i++)
                {
                    var child = coinsChipButton.transform.GetChild(i);
                    if (child.name.ToLowerInvariant().Contains("plus") || child.name.ToLowerInvariant().Contains("add"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnPlay() { AudioManager.Instance?.PlayButton(); ScreenManager.Instance.Push(ScreenId.PlayMenu); }
        private void OnProfile() { AudioManager.Instance?.PlayButton(); ScreenManager.Instance.Push(ScreenId.Profile); }
        // The top-bar burger opens the quick-jump Menu popup, not Settings directly — Settings
        // is one of the destinations inside it now (see MenuModal.cs).
        private void OnSettings() { AudioManager.Instance?.PlayButton(); UIModal.Open("Menu"); }
        private void OnEnergy() { AudioManager.Instance?.PlayButton(); ScreenManager.Instance.Push(ScreenId.Energy); }
        private void OnWallet() { AudioManager.Instance?.PlayButton(); ScreenManager.Instance.Push(ScreenId.Wallet); }

        /// <summary>
        /// Every card leads to the one setup screen, which shapes itself to the mode. The old
        /// Quick Play Setup and Match Lobby screens are left in place but no longer reachable
        /// from here — they are the previous art style, and a player must never cross between
        /// the two looks mid-flow.
        /// </summary>
        private void OnModeCardClicked(GameModeCarousel.Mode mode)
        {
            AudioManager.Instance?.PlayButton();
            switch (mode)
            {
                case GameModeCarousel.Mode.QuickPlay:
                    MatchSetupModal.OpenFor(MatchSetupModal.Flavour.VsComputer);
                    break;
                case GameModeCarousel.Mode.Online:
                    OnlineMatchModal.Open();
                    break;
                case GameModeCarousel.Mode.LocalPlay:
                    MatchSetupModal.OpenFor(MatchSetupModal.Flavour.PassAndPlay);
                    break;
            }
        }
    }
}
