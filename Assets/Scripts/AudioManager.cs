using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<AudioManager>();
                if (_instance == null)
                {
                    var go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }
    private static AudioManager _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var instance = Instance;
        if (instance != null)
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            LudoNX.UI.SettingsService.ApplyToEngine();
            instance.PlayMenuMusic();
        }
    }

    [Header("Effects")]
    public AudioClip diceRollClip;
    public AudioClip tokenHopClip;
    public AudioClip tokenSpawnClip;
    public AudioClip tokenCaptureClip;
    public AudioClip homeStretchHopClip;
    public AudioClip tokenHomeClip;      // a token finishing its run
    public AudioClip buttonClip;         // any button, anywhere
    public AudioClip turnChangeClip;
    public AudioClip victoryClip;

    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip matchMusic;
    [Range(0f, 1f)] public float musicVolume = 0.40f;
    [Tooltip("Seconds to cross-fade between tracks.")]
    public float musicFade = 0.9f;

    private AudioSource audioSource;
    private AudioSource musicSource;
    private AudioClip _wantedMusic;
    private Coroutine _fade;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // Pure 2D audio so camera distance does not attenuate sounds
            audioSource.volume = 1f;

            // Music gets its own source so a loop is never cut short by a one-shot, and so the
            // two can be muted independently — the settings screen offers exactly that.
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f; // Pure 2D audio
            musicSource.loop = true;
            musicSource.volume = musicVolume;

            // Ensure there is ALWAYS an active AudioListener in the scene so sounds output properly
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners == null || listeners.Length == 0)
            {
                gameObject.AddComponent<AudioListener>();
            }
            else if (listeners.Length > 1)
            {
                // If there's already one on Camera, remove any duplicate on gameObject
                var myListener = GetComponent<AudioListener>();
                if (myListener != null && listeners.Length > 1) Destroy(myListener);
            }

            AudioListener.volume = 1f;
            AudioListener.pause = false;

            LoadClips();

            LudoNX.UI.SettingsService.Changed += ApplySettings;
            LudoNX.UI.SettingsService.ApplyToEngine();
            ApplySettings();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        AudioListener.volume = 1f;
        AudioListener.pause = false;
        if (menuMusic != null && (musicSource == null || !musicSource.isPlaying))
        {
            PlayMenuMusic();
        }
    }

    private void Preload(AudioClip clip)
    {
        if (clip != null)
        {
            clip.LoadAudioData();
        }
    }

    private void LoadClips()
    {
        if (diceRollClip == null) diceRollClip = Resources.Load<AudioClip>("Audio/dice_roll");
        if (tokenHopClip == null) tokenHopClip = Resources.Load<AudioClip>("Audio/token_hop");
        if (tokenSpawnClip == null) tokenSpawnClip = Resources.Load<AudioClip>("Audio/dice_land");
        if (homeStretchHopClip == null) homeStretchHopClip = Resources.Load<AudioClip>("Audio/token_hop_home");
        if (tokenHomeClip == null) tokenHomeClip = Resources.Load<AudioClip>("Audio/token_home_chime");
        if (buttonClip == null) buttonClip = Resources.Load<AudioClip>("Audio/button_click");
        if (buttonClip == null) buttonClip = Resources.Load<AudioClip>("Audio/click1");
        if (turnChangeClip == null) turnChangeClip = Resources.Load<AudioClip>("Audio/turn_change");
        if (matchMusic == null) matchMusic = Resources.Load<AudioClip>("Audio/match_music");
        if (menuMusic == null) menuMusic = Resources.Load<AudioClip>("Audio/menu_music");
        if (victoryClip == null) victoryClip = Resources.Load<AudioClip>("Audio/victory_fanfare_RAW");
        if (tokenCaptureClip == null) tokenCaptureClip = Resources.Load<AudioClip>("Audio/token_capture");

#if UNITY_EDITOR
        if (diceRollClip == null) diceRollClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/dice_roll.ogg");
        if (tokenHopClip == null) tokenHopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/token_hop.ogg");
        if (tokenSpawnClip == null) tokenSpawnClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/dice_land.ogg");
        if (homeStretchHopClip == null) homeStretchHopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/token_hop_home.ogg");
        if (tokenHomeClip == null) tokenHomeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/token_home_chime.ogg");
        if (buttonClip == null) buttonClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/button_click.ogg");
        if (turnChangeClip == null) turnChangeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/turn_change.ogg");
        if (matchMusic == null) matchMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/match_music.mp3");
        if (menuMusic == null) menuMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/menu_music.mp3");
        if (victoryClip == null) victoryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/victory_fanfare_RAW.mp3");
        if (tokenCaptureClip == null) tokenCaptureClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/token_capture.ogg");
#endif

        Preload(diceRollClip);
        Preload(tokenHopClip);
        Preload(tokenSpawnClip);
        Preload(homeStretchHopClip);
        Preload(tokenHomeClip);
        Preload(buttonClip);
        Preload(turnChangeClip);
        Preload(matchMusic);
        Preload(menuMusic);
        Preload(victoryClip);
        Preload(tokenCaptureClip);

        Debug.Log($"[AudioManager] Loaded and preloaded audio clips. MenuMusic: {menuMusic != null}, DiceRoll: {diceRollClip != null}");
    }

    // Standard Hop Sound
    public void PlayHop()
    {
        PlayHop(1.0f);
    }

    // Hop with an explicit pitch, so a multi-tile move can climb instead of repeating one flat click.
    public void PlayHop(float pitch)
    {
        if (tokenHopClip == null) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(tokenHopClip);
    }

    // Diminishing Hop Sound along the Home Stretch (Pitch drops with each step closer to center)
    public void PlayHomeStretchHop(int homeStepIndex, int maxSteps)
    {
        AudioClip clipToPlay = homeStretchHopClip != null ? homeStretchHopClip : tokenHopClip;
        if (clipToPlay == null) return;

        // Calculate pitch reduction: 1.0 at step 0 down to ~0.4 at final square
        float progress = (float)homeStepIndex / Mathf.Max(1, maxSteps - 1);
        audioSource.pitch = Mathf.Lerp(1.0f, 0.4f, progress);
        
        audioSource.PlayOneShot(clipToPlay);
    }

    // Token Spawns out of Home Base (Rolled a 6)
    public void PlaySpawn()
    {
        if (tokenSpawnClip == null) return;
        audioSource.pitch = 1.0f;
        audioSource.PlayOneShot(tokenSpawnClip);
    }

    // Token Captures an Enemy
    public void PlayCapture()
    {
        // The haptic fires whether or not a clip is assigned: a capture is the one event that
        // should be felt even with the sound off.
        LudoNX.UI.SettingsService.Haptic();
        if (tokenCaptureClip != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(tokenCaptureClip);
        }
        else if (tokenSpawnClip != null || diceRollClip != null)
        {
            // Crisp wooden impact fallback until dedicated capture clip is present
            audioSource.pitch = 0.85f;
            audioSource.PlayOneShot(tokenSpawnClip != null ? tokenSpawnClip : diceRollClip);
        }
    }

    // Dice Roll FX
    public void PlayDiceRoll()
    {
        if (diceRollClip == null)
        {
            diceRollClip = Resources.Load<AudioClip>("Audio/dice_roll");
            Preload(diceRollClip);
        }
        if (diceRollClip == null)
        {
            Debug.LogWarning("[AudioManager] diceRollClip is null!");
            return;
        }
        audioSource.pitch = 1.0f;
        audioSource.volume = LudoNX.UI.SettingsService.Sound ? 1f : 0f;
        audioSource.PlayOneShot(diceRollClip);
        Debug.Log($"[AudioManager] Played diceRollClip. AudioListener.volume={AudioListener.volume}, audioSource.volume={audioSource.volume}");
    }

    // Player Finish / Game Win
    public void PlayVictory()
    {
        if (victoryClip == null)
        {
            victoryClip = Resources.Load<AudioClip>("Audio/victory_fanfare_RAW");
            Preload(victoryClip);
        }
        if (victoryClip == null) return;
        audioSource.pitch = 1.0f;
        audioSource.volume = LudoNX.UI.SettingsService.Sound ? 1f : 0f;
        audioSource.PlayOneShot(victoryClip);
    }

    // ── Settings ──────────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (Instance == this) LudoNX.UI.SettingsService.Changed -= ApplySettings;
    }

    /// <summary>Effects and music follow their own toggles; neither implies the other.</summary>
    private void ApplySettings()
    {
        AudioListener.volume = 1f;
        AudioListener.pause = false;

        if (audioSource != null)
        {
            audioSource.mute = false;
            audioSource.volume = LudoNX.UI.SettingsService.Sound ? 1f : 0f;
        }
        if (musicSource != null)
        {
            musicSource.mute = false;
            musicSource.volume = LudoNX.UI.SettingsService.Music ? musicVolume : 0f;
            if (LudoNX.UI.SettingsService.Music && !musicSource.isPlaying && menuMusic != null)
            {
                PlayMenuMusic();
            }
        }
    }

    // ── Music ─────────────────────────────────────────────────────────────────────────────

    public void PlayMenuMusic() { PlayMusic(menuMusic); }
    public void PlayMatchMusic() { PlayMusic(matchMusic); }

    /// <summary>
    /// Cross-fades to a track, and does nothing if it is already the one playing — so calling
    /// this on every screen change is safe and will not restart the loop.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null || _wantedMusic == clip) return;
        _wantedMusic = clip;
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeToMusic(clip));
    }

    private System.Collections.IEnumerator FadeToMusic(AudioClip clip)
    {
        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < musicFade * 0.5f && musicSource.isPlaying)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / (musicFade * 0.5f));
            yield return null;
        }

        musicSource.clip = clip;
        musicSource.Play();

        t = 0f;
        while (t < musicFade * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t / (musicFade * 0.5f));
            yield return null;
        }

        musicSource.volume = musicVolume;
        _fade = null;
    }

    // ── Added one-shots ───────────────────────────────────────────────────────────────────

    /// <summary>A token completing its run. The reward sound of the whole game.</summary>
    public void PlayTokenHome()
    {
        if (tokenHomeClip != null && audioSource != null) audioSource.PlayOneShot(tokenHomeClip);
    }

    /// <summary>Every button in the game. Kept quiet — it fires more than anything else.</summary>
    public void PlayButton()
    {
        if (buttonClip != null && audioSource != null) audioSource.PlayOneShot(buttonClip, 0.4f);
    }

    public void PlayTurnChange()
    {
        if (turnChangeClip != null && audioSource != null) audioSource.PlayOneShot(turnChangeClip, 0.7f);
    }
}
