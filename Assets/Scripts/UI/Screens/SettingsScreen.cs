using TMPro;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Preferences and the legal rows. Everything here persists through
    /// <see cref="SettingsService"/> — before this existed, the sound flag reset every launch.
    /// </summary>
    public class SettingsScreen : UIScreen
    {
        [Header("Wiring")]
        public ClayButton backButton;
        public ToggleSwitch soundToggle;
        public ToggleSwitch musicToggle;
        public ToggleSwitch vibrationToggle;
        public ClayButton languageRow;
        public ClayButton aboutRow;
        public ClayButton privacyRow;
        public ClayButton termsRow;

        [Header("Readout")]
        public TextMeshProUGUI languageValue;
        public TextMeshProUGUI versionLabel;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());

            if (soundToggle != null) soundToggle.onChanged.AddListener(v => SettingsService.Sound = v);
            if (musicToggle != null) musicToggle.onChanged.AddListener(v => SettingsService.Music = v);
            if (vibrationToggle != null) vibrationToggle.onChanged.AddListener(v => SettingsService.Vibration = v);

            if (languageRow != null) languageRow.onClick.AddListener(OpenLanguage);
            if (aboutRow != null) aboutRow.onClick.AddListener(() => OpenText("ABOUT LUDO NX",
                "Ludo NX is a modern take on the classic board game.\n\n" +
                "Roll, capture, and race all four tokens home.\n\n" +
                "Built with Unity."));
            if (privacyRow != null) privacyRow.onClick.AddListener(() => OpenText("PRIVACY POLICY",
                "Ludo NX stores your profile, preferences and match statistics only on this device.\n\n" +
                "No personal data is collected or transmitted.\n\n" +
                "Replace this text with your published policy before release — Google Play " +
                "requires a reachable privacy policy."));
            if (termsRow != null) termsRow.onClick.AddListener(() => OpenText("TERMS OF SERVICE",
                "By playing Ludo NX you agree to use the game for personal, non-commercial " +
                "entertainment.\n\nReplace this text with your published terms before release."));

            if (versionLabel != null) versionLabel.text = "Version " + Application.version;
        }

        public override void OnShow(object args)
        {
            if (soundToggle != null) soundToggle.SetSilent(SettingsService.Sound);
            if (musicToggle != null) musicToggle.SetSilent(SettingsService.Music);
            if (vibrationToggle != null) vibrationToggle.SetSilent(SettingsService.Vibration);
            if (languageValue != null) languageValue.text = SettingsService.Language;
        }

        private void OpenLanguage()
        {
            var modal = UIModal.Open("LanguagePicker");
            if (modal == null) return;
            var picker = modal.GetComponent<ListPickerModal>();
            if (picker == null) return;
            picker.Configure("LANGUAGE",
                             new[] { "English", "Español", "Français", "Deutsch", "Português", "हिन्दी" },
                             SettingsService.Language,
                             chosen =>
                             {
                                 SettingsService.Language = chosen;
                                 if (languageValue != null) languageValue.text = chosen;
                             });
        }

        private void OpenText(string title, string body)
        {
            var modal = UIModal.Open("TextPage");
            if (modal == null) return;
            var page = modal.GetComponent<TextPageModal>();
            if (page != null) page.Configure(title, body);
        }
    }
}
