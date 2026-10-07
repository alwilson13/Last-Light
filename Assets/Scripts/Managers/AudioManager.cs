using UnityEngine;

/// Handles music and shared sound effects for Last Light.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip gameplayMusic;

    [Header("Flashlight Sounds")]
    [SerializeField] private AudioClip flashlightOnSFX;
    [SerializeField] private AudioClip flashlightOffSFX;
    [SerializeField] private AudioClip lowBatterySFX;
    [SerializeField] private AudioClip batteryPickupSFX;

    [Header("Combat Sounds")]
    [SerializeField] private AudioClip playerHitSFX;
    [SerializeField] private AudioClip enemyDeathSFX;
    [SerializeField] private AudioClip enemyStunSFX;

    [Header("Wave Sounds")]
    [SerializeField] private AudioClip waveStartSFX;
    [SerializeField] private AudioClip waveCompleteSFX;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip victorySFX;
    [SerializeField] private AudioClip gameOverSFX;
    [SerializeField] private AudioClip buttonClickSFX;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        PlayGameplayMusic();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayGameplayMusic()
    {
        if (musicSource == null || gameplayMusic == null)
            return;

        // Keep the current track playing if it is already active.
        if (musicSource.clip == gameplayMusic && musicSource.isPlaying)
            return;

        musicSource.clip = gameplayMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    public void PlayFlashlightOnSFX()
    {
        PlaySFX(flashlightOnSFX);
    }

    public void PlayFlashlightOffSFX()
    {
        PlaySFX(flashlightOffSFX);
    }

    public void PlayLowBatterySFX()
    {
        PlaySFX(lowBatterySFX);
    }

    public void PlayBatteryPickupSFX()
    {
        PlaySFX(batteryPickupSFX);
    }

    public void PlayPlayerHitSFX()
    {
        PlaySFX(playerHitSFX);
    }

    public void PlayEnemyDeathSFX()
    {
        PlaySFX(enemyDeathSFX);
    }

    public void PlayEnemyStunSFX()
    {
        PlaySFX(enemyStunSFX);
    }

    public void PlayWaveStartSFX()
    {
        PlaySFX(waveStartSFX);
    }

    public void PlayWaveCompleteSFX()
    {
        PlaySFX(waveCompleteSFX);
    }

    public void PlayVictorySFX()
    {
        PlaySFX(victorySFX);
    }

    public void PlayGameOverSFX()
    {
        PlaySFX(gameOverSFX);
    }

    public void PlayButtonClickSFX()
    {
        PlaySFX(buttonClickSFX);
    }
}