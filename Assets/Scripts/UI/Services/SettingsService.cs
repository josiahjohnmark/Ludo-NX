using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Player preferences, persisted.
    ///
    /// The project had none of this — <c>AudioManager</c> held a mute flag in memory that reset
    /// every launch, and there was nowhere for vibration or language to live. Everything here is
    /// a plain PlayerPrefs key so it survives a reinstall of nothing and costs nothing.
    /// </summary>
    public static class SettingsService
    {
        private const string KeySound = "ludonx_set_sound";
        private const string KeyMusic = "ludonx_set_music";
        private const string KeyVibration = "ludonx_set_vibrate";
        private const string KeyLanguage = "ludonx_set_lang";

        /// <summary>Raised whenever any preference changes, so live UI and audio can react.</summary>
        public static event System.Action Changed;

        private static bool _loaded;

        internal static void ResetStatics() { _loaded = false; }
        private static bool _sound, _music, _vibration;
        private static string _language;

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _sound = PlayerPrefs.GetInt(KeySound, 1) == 1;
            _music = PlayerPrefs.GetInt(KeyMusic, 1) == 1;
            _vibration = PlayerPrefs.GetInt(KeyVibration, 1) == 1;
            _language = PlayerPrefs.GetString(KeyLanguage, "English");
        }

        public static bool Sound
        {
            get { Load(); return _sound; }
            set { Load(); _sound = value; PlayerPrefs.SetInt(KeySound, value ? 1 : 0); Commit(); }
        }

        public static bool Music
        {
            get { Load(); return _music; }
            set { Load(); _music = value; PlayerPrefs.SetInt(KeyMusic, value ? 1 : 0); Commit(); }
        }

        public static bool Vibration
        {
            get { Load(); return _vibration; }
            set { Load(); _vibration = value; PlayerPrefs.SetInt(KeyVibration, value ? 1 : 0); Commit(); }
        }

        /// <summary>
        /// The language list — the one copy every picker reads (owner, 27 Sep 2026: "the major
        /// languages, 7 to 10"). Written in their own names where the UI font (Lilita One: Latin
        /// + Cyrillic) can draw them; Hindi, Arabic and Chinese use English names until a Noto
        /// fallback font ships with the real translations. The stored value is the label.
        /// </summary>
        public static readonly string[] Languages =
        {
            "English", "Español", "Français", "Português", "Deutsch",
            "Русский", "Hindi", "Arabic", "Chinese", "Hausa",
        };

        /// <summary>No translations exist yet; the choice is saved for when they do.</summary>
        public const string LanguagePendingNote = "Saved. Translations arrive in a later update.";

        /// <summary>
        /// Shown by the Privacy rows. Accurate for this build: local profile/stats, and purchases
        /// through Google Play + RevenueCat. Update when Firebase (Phase A) ships.
        /// </summary>
        public const string PrivacyText =
            "Your profile, settings and match statistics are stored on this device.\n\n" +
            "Purchases are processed by Google Play and RevenueCat, which receive your purchase " +
            "history and an anonymous app user ID so purchases can be verified and restored.\n\n" +
            "Replace this text with your published privacy policy before release — Google Play " +
            "requires a reachable privacy policy.";

        public static string Language
        {
            get { Load(); return _language; }
            set { Load(); _language = value; PlayerPrefs.SetString(KeyLanguage, value); Commit(); }
        }

        private static void Commit()
        {
            PlayerPrefs.Save();
            ApplyToEngine();
            if (Changed != null) Changed();
        }

        /// <summary>Pushes the current preferences at the engine. Call once on boot.</summary>
        public static void ApplyToEngine()
        {
            Load();
            AudioListener.volume = 1f;
            AudioListener.pause = false;
        }

        /// <summary>Short haptic tap, honouring the vibration preference. Mobile only.</summary>
        public static void Haptic()
        {
            Load();
            if (!_vibration) return;
#if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor) Handheld.Vibrate();
#endif
        }
    }
}
