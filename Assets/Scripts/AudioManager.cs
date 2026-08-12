using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips (Assign Later in Inspector)")]
    public AudioClip diceRollClip;
    public AudioClip tokenHopClip;
    public AudioClip tokenSpawnClip;
    public AudioClip tokenCaptureClip;
    public AudioClip homeStretchHopClip;
    public AudioClip victoryClip;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
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
}