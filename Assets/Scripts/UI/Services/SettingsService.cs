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
            // AudioManager has no volume API, so drive the global mixer instead — that also
            // covers any AudioSource added later without needing to know about it.
            AudioListener.volume = _sound || _music ? 1f : 0f;
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
