using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// SETTINGS as a popup on the Menu's chassis (it replaces the older Clay-system Settings
    /// screen as the Menu's destination). Sound, music and vibration switches, the language
    /// row, and the About / Privacy / Terms pages.
    /// </summary>
    public class SettingsModal : UIModal
    {
        [Header("Switches")]
        public PillToggle soundToggle;
        public PillToggle musicToggle;
        public PillToggle vibrationToggle;

        [Header("Rows")]
        public Button languageRow;
        public TextMeshProUGUI languageValue;
        public Button aboutButton;
        public Button privacyButton;
        public Button termsButton;
        public TextMeshProUGUI versionLabel;

        public const string AboutText =
            "Ludo NX is a modern take on the classic board game.\n\n" +
            "Roll, capture, and race all four tokens home.\n\n" +
            "Built with Unity.";

        public const string TermsText =
            "By playing Ludo NX you agree to use the game for personal, non-commercial " +
            "entertainment.\n\nReplace this text with your published terms before release.";

        private void Awake()
        {
            if (soundToggle != null) soundToggle.Changed += v => SettingsService.Sound = v;
            if (musicToggle != null) musicToggle.Changed += v => SettingsService.Music = v;
            if (vibrationToggle != null) vibrationToggle.Changed += v => SettingsService.Vibration = v;

            if (languageRow != null) languageRow.onClick.AddListener(OpenLanguage);
            if (aboutButton != null) aboutButton.onClick.AddListener(() => OpenText("ABOUT LUDO NX", AboutText));
            if (privacyButton != null) privacyButton.onClick.AddListener(() => OpenText("PRIVACY POLICY", SettingsService.PrivacyText));
            if (termsButton != null) termsButton.onClick.AddListener(() => OpenText("TERMS OF SERVICE", TermsText));
        }

        private void Start()
        {
            if (soundToggle != null) soundToggle.SetSilent(SettingsService.Sound);
            if (musicToggle != null) musicToggle.SetSilent(SettingsService.Music);
            if (vibrationToggle != null) vibrationToggle.SetSilent(SettingsService.Vibration);
            if (languageValue != null) languageValue.text = SettingsService.Language;
            if (versionLabel != null) versionLabel.text = "VERSION " + Application.version;
        }

        public static SettingsModal Show() => Open("Settings") as SettingsModal;

        private void OpenLanguage()
        {
            // Stacks over this popup, so the player comes back to Settings after choosing.
            var picker = Open("LanguagePicker")?.GetComponent<ListPickerModal>();
            if (picker == null) return;
            picker.Configure("LANGUAGE", SettingsService.Languages, SettingsService.Language, chosen =>
            {
                SettingsService.Language = chosen;
                if (languageValue != null) languageValue.text = chosen;
                if (chosen != "English" && Toast.Instance != null) Toast.Instance.Show(SettingsService.LanguagePendingNote);
            });
        }

        private static void OpenText(string title, string body)
        {
            Open("TextPage")?.GetComponent<TextPageModal>()?.Configure(title, body);
        }
    }
}
