using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

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
    [Range(0f, 1f)] public float musicVolume = 0.35f;
    [Tooltip("Seconds to cross-fade between tracks.")]
    public float musicFade = 0.9f;

    private AudioSource audioSource;
    private AudioSource musicSource;
    private AudioClip _wantedMusic;
    private Coroutine _fade;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            // Music gets its own source so a loop is never cut short by a one-shot, and so the
            // two can be muted independently — the settings screen offers exactly that.
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0f;

            LudoNX.UI.SettingsService.Changed += ApplySettings;
            ApplySettings();

            if (buttonClip == null)
            {
                buttonClip = Resources.Load<AudioClip>("Audio/click1");
            }
        }
        else
        {
            Destroy(gameObject);
        }
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
        if (tokenCaptureClip == null) return;
        audioSource.pitch = 1.0f;
        audioSource.PlayOneShot(tokenCaptureClip);
    }

    // Dice Roll FX
    public void PlayDiceRoll()
    {
        if (diceRollClip == null) return;
        audioSource.pitch = 1.0f;
        audioSource.PlayOneShot(diceRollClip);
    }

    // Player Finish / Game Win
    public void PlayVictory()
    {
        if (victoryClip == null) return;
        audioSource.pitch = 1.0f;
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
        if (audioSource != null) audioSource.mute = !LudoNX.UI.SettingsService.Sound;
        if (musicSource != null) musicSource.mute = !LudoNX.UI.SettingsService.Music;
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
