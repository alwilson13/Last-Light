using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Handles Last Light's HUD, launch menu, pause, and end screens.
public class UIManager : MonoBehaviour
{
    [Header("HUD Text")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text batteryText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text enemyCountText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Game Over Text")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text waveReachedText;

    [Header("Victory Text")]
    [SerializeField] private TMP_Text victoryFinalScoreText;
    [SerializeField] private TMP_Text victoryWaveReachedText;

    [Header("Panels")]
    [SerializeField] private GameObject launchPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private FlashlightBattery flashlightBattery;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private EnemySpawner enemySpawner;

    private int currentWave;
    private int totalWaves = 5;

    private bool gameEnded;
    private bool isPaused;
    private bool isLaunchScreenOpen;

    private static bool skipLaunchPanelOnRestart;

    public bool IsGameEnded => gameEnded;

    public bool IsGameplayRunning =>
        !gameEnded && !isPaused && !isLaunchScreenOpen;

    // The WaveManager will subscribe to these events.
    public event Action RunStarted;
    public event Action<bool> RunEnded;

    private void Awake()
    {
        FindReferences();

        HideAllPanels();

        bool startImmediately = skipLaunchPanelOnRestart;
        skipLaunchPanelOnRestart = false;

        // Without a launch panel, enter gameplay immediately.
        isLaunchScreenOpen =
            launchPanel != null && !startImmediately;

        SetPanel(launchPanel, isLaunchScreenOpen);

        Time.timeScale = isLaunchScreenOpen ? 0f : 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void FindReferences()
    {
        if (playerHealth == null || flashlightBattery == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                if (playerHealth == null)
                    playerHealth = player.GetComponent<PlayerHealth>();

                if (flashlightBattery == null)
                {
                    flashlightBattery =
                        player.GetComponent<FlashlightBattery>();
                }
            }
        }

        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();

        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += UpdateHealth;
            playerHealth.Damaged += HandlePlayerDamaged;
            playerHealth.Died += ShowGameOver;
        }

        if (flashlightBattery != null)
            flashlightBattery.EnergyChanged += UpdateBattery;

        if (scoreManager != null)
            scoreManager.ScoreChanged += UpdateScore;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateHealth;
            playerHealth.Damaged -= HandlePlayerDamaged;
            playerHealth.Died -= ShowGameOver;
        }

        if (flashlightBattery != null)
            flashlightBattery.EnergyChanged -= UpdateBattery;

        if (scoreManager != null)
            scoreManager.ScoreChanged -= UpdateScore;
    }

    private void Start()
    {
        RefreshHUD();

        if (IsGameplayRunning)
            RunStarted?.Invoke();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    private void RefreshHUD()
    {
        if (playerHealth != null)
        {
            UpdateHealth(
                playerHealth.GetCurrentHealth(),
                playerHealth.GetMaxHealth()
            );
        }

        if (flashlightBattery != null)
        {
            UpdateBattery(
                flashlightBattery.CurrentEnergy,
                flashlightBattery.MaxEnergy
            );
        }

        if (scoreManager != null)
            UpdateScore(scoreManager.GetCurrentScore());

        UpdateWaveDisplay(currentWave, totalWaves);
        UpdateEnemyCount(0);
        UpdateCountdown(0f);
    }

    private void UpdateHealth(int current, int maximum)
    {
        if (healthText != null)
            healthText.text = $"Health: {current} / {maximum}";
    }

    private void UpdateBattery(float current, float maximum)
    {
        if (batteryText != null)
            batteryText.text = $"Battery: {current:0} / {maximum:0}";
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    /// Called by the WaveManager when the wave changes.
    public void UpdateWaveDisplay(int wave, int waveTotal)
    {
        currentWave = Mathf.Max(0, wave);
        totalWaves = Mathf.Max(0, waveTotal);

        if (waveText != null)
            waveText.text = $"Wave: {currentWave} / {totalWaves}";
    }

    /// Called when an enemy spawns or dies.
    public void UpdateEnemyCount(int livingEnemies)
    {
        if (enemyCountText != null)
        {
            enemyCountText.text =
                $"Enemies: {Mathf.Max(0, livingEnemies)}";
        }
    }

    /// Pass zero to hide the countdown.
    public void UpdateCountdown(float secondsRemaining)
    {
        if (countdownText == null)
            return;

        bool showCountdown = secondsRemaining > 0f;

        countdownText.gameObject.SetActive(showCountdown);

        if (showCountdown)
        {
            countdownText.text =
                $"Next wave in: {Mathf.CeilToInt(secondsRemaining)}";
        }
    }

    private void HandlePlayerDamaged(int damageAmount)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerHitSFX();
    }

    public void StartGame()
    {
        if (gameEnded || !isLaunchScreenOpen)
            return;

        isLaunchScreenOpen = false;
        isPaused = false;

        SetPanel(launchPanel, false);
        SetPanel(pausePanel, false);

        Time.timeScale = 1f;

        RunStarted?.Invoke();
    }

    public void TogglePause()
    {
        if (gameEnded || isLaunchScreenOpen)
            return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (gameEnded || isLaunchScreenOpen || isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        SetPanel(pausePanel, true);
    }

    public void ResumeGame()
    {
        if (gameEnded || isLaunchScreenOpen || !isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        SetPanel(pausePanel, false);
    }

    public void ShowGameOver()
    {
        EndRun(false);
    }

    public void ShowVictory()
    {
        // Death takes priority if the final enemy and player die together.
        if (playerHealth != null && playerHealth.IsDead())
        {
            ShowGameOver();
            return;
        }

        EndRun(true);
    }

    private void EndRun(bool victory)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        isPaused = false;
        isLaunchScreenOpen = false;

        Time.timeScale = 0f;

        // Notify the WaveManager before cancelling pending spawns.
        RunEnded?.Invoke(victory);

        if (enemySpawner != null)
            enemySpawner.CancelPendingSpawns();

        HideAllPanels();
        UpdateCountdown(0f);

        int finalScore =
            scoreManager != null
                ? scoreManager.GetCurrentScore()
                : 0;

        TMP_Text scoreLabel =
            victory ? victoryFinalScoreText : finalScoreText;

        TMP_Text waveLabel =
            victory ? victoryWaveReachedText : waveReachedText;

        if (scoreLabel != null)
            scoreLabel.text = $"Final Score: {finalScore}";

        if (waveLabel != null)
            waveLabel.text = $"Wave Reached: {currentWave} / {totalWaves}";

        SetPanel(victory ? victoryPanel : gameOverPanel, true);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();

            if (victory)
                AudioManager.Instance.PlayVictorySFX();
            else
                AudioManager.Instance.PlayGameOverSFX();
        }
    }

    public void RestartGame()
    {
        ReloadScene(true);
    }

    public void ReturnToMenu()
    {
        ReloadScene(false);
    }

    private void ReloadScene(bool skipLaunch)
    {
        skipLaunchPanelOnRestart = skipLaunch;

        if (enemySpawner != null)
            enemySpawner.CancelPendingSpawns();

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void PlayButtonClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClickSFX();
    }

    public void QuitGame()
    {
#if UNITY_WEBGL
        ReturnToMenu();
#elif UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HideAllPanels()
    {
        SetPanel(launchPanel, false);
        SetPanel(pausePanel, false);
        SetPanel(gameOverPanel, false);
        SetPanel(victoryPanel, false);
    }

    private void SetPanel(GameObject panel, bool visible)
    {
        if (panel == null)
            return;

        panel.SetActive(visible);

        if (visible)
            panel.transform.SetAsLastSibling();
    }
}