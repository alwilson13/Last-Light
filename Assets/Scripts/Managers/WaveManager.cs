using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Controls Last Light's waves, spawn warnings, and battery rewards.
public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyGroup
    {
        public GameObject enemyPrefab;

        [Min(1)]
        public int enemyCount = 5;

        [Min(0f)]
        public float spawnDelay = 0.5f;
    }

    [System.Serializable]
    public class WaveData
    {
        public string waveName;
        public EnemyGroup[] enemyGroups;
    }

    [Header("Wave Settings")]
    [SerializeField] private WaveData[] waves;

    [Min(0f)]
    [SerializeField] private float firstWaveDelay = 1f;

    [Min(0f)]
    [SerializeField] private float delayBetweenWaves = 2f;

    [Tooltip("Wait before retrying a spawn with no safe location.")]
    [Min(0.1f)]
    [SerializeField] private float spawnRetryDelay = 1f;

    [Header("Wave Rewards")]
    [Min(0f)]
    [SerializeField] private float batteryReward = 20f;

    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private FlashlightBattery flashlightBattery;

    private readonly HashSet<EnemyHealth> activeEnemies =
        new HashSet<EnemyHealth>();

    private int currentWaveIndex = -1;
    private bool runStarted;
    private bool runActive;

    private void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (flashlightBattery == null && playerHealth != null)
        {
            flashlightBattery =
                playerHealth.GetComponent<FlashlightBattery>();
        }
    }

    private void OnEnable()
    {
        if (uiManager != null)
        {
            uiManager.RunStarted += StartRun;
            uiManager.RunEnded += HandleRunEnded;
        }

        if (playerHealth != null)
            playerHealth.Died += StopRun;
    }

    private void Start()
    {
        if (uiManager != null)
        {
            uiManager.UpdateWaveDisplay(0, GetTotalWaves());
            uiManager.UpdateEnemyCount(activeEnemies.Count);

            // Handles either ordering of UIManager.Start
            // and WaveManager.Start.
            if (uiManager.IsGameplayRunning)
                StartRun();
        }
    }

    private void OnDisable()
    {
        if (uiManager != null)
        {
            uiManager.RunStarted -= StartRun;
            uiManager.RunEnded -= HandleRunEnded;
        }

        if (playerHealth != null)
            playerHealth.Died -= StopRun;

        StopRun();
    }

    public void StartRun()
    {
        if (runStarted || !isActiveAndEnabled)
            return;

        if (uiManager == null || !uiManager.IsGameplayRunning)
            return;

        if (playerHealth != null && playerHealth.IsDead())
            return;

        if (!ValidateSetup())
            return;

        runStarted = true;
        runActive = true;

        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        yield return Countdown(firstWaveDelay);

        for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
        {
            if (!runActive)
                yield break;

            currentWaveIndex = waveIndex;

            uiManager.UpdateWaveDisplay(
                GetCurrentWaveNumber(),
                GetTotalWaves()
            );

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayWaveStartSFX();

            WaveData wave = waves[waveIndex];

            foreach (EnemyGroup group in wave.enemyGroups)
            {
                for (int i = 0; i < group.enemyCount; i++)
                {
                    // Finish this enemy's warning before requesting
                    // the next enemy.
                    yield return SpawnWithRetry(group.enemyPrefab);

                    if (!runActive)
                        yield break;

                    if (group.spawnDelay > 0f)
                        yield return new WaitForSeconds(group.spawnDelay);
                }
            }

            // Reached only after every planned enemy has spawned.
            while (runActive)
            {
                // Handles enemies removed without calling Die().
                activeEnemies.RemoveWhere(
                    enemy => enemy == null || enemy.IsDead()
                );

                uiManager.UpdateEnemyCount(activeEnemies.Count);

                if (activeEnemies.Count == 0)
                    break;

                yield return null;
            }

            if (!runActive)
                yield break;

            if (flashlightBattery != null)
                flashlightBattery.Recharge(batteryReward);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayWaveCompleteSFX();

            if (waveIndex == waves.Length - 1)
            {
                uiManager.ShowVictory();
                yield break;
            }

            yield return Countdown(delayBetweenWaves);
        }
    }

    private IEnumerator SpawnWithRetry(GameObject enemyPrefab)
    {
        bool spawnedSuccessfully = false;

        while (runActive && !spawnedSuccessfully)
        {
            bool requestFinished = false;

            enemySpawner.SpawnEnemyWithIndicator(
                enemyPrefab,
                spawnedEnemy =>
                {
                    requestFinished = true;

                    if (!runActive)
                    {
                        if (spawnedEnemy != null)
                            Destroy(spawnedEnemy);

                        return;
                    }

                    if (spawnedEnemy == null)
                        return;

                    EnemyHealth health =
                        spawnedEnemy.GetComponent<EnemyHealth>();

                    if (health == null)
                    {
                        Debug.LogError(
                            "Spawned enemy is missing EnemyHealth.",
                            spawnedEnemy
                        );

                        Destroy(spawnedEnemy);
                        return;
                    }

                    spawnedSuccessfully = true;

                    if (!health.IsDead())
                    {
                        activeEnemies.Add(health);
                        health.Died += HandleEnemyDied;
                    }

                    uiManager.UpdateEnemyCount(activeEnemies.Count);
                }
            );

            while (runActive && !requestFinished)
                yield return null;

            if (runActive && !spawnedSuccessfully)
            {
                yield return new WaitForSeconds(
                    Mathf.Max(0.1f, spawnRetryDelay)
                );
            }
        }
    }

    private IEnumerator Countdown(float duration)
    {
        float remaining = Mathf.Max(0f, duration);

        while (runActive && remaining > 0f)
        {
            uiManager.UpdateCountdown(remaining);

            yield return null;

            remaining -= Time.deltaTime;
        }

        uiManager.UpdateCountdown(0f);
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        enemy.Died -= HandleEnemyDied;
        activeEnemies.Remove(enemy);

        if (runActive && uiManager != null)
            uiManager.UpdateEnemyCount(activeEnemies.Count);
    }

    private void HandleRunEnded(bool victory)
    {
        StopRun();
    }

    private void StopRun()
    {
        // Set this first so cancelled spawn callbacks cannot retry.
        runActive = false;

        StopAllCoroutines();

        if (enemySpawner != null)
            enemySpawner.CancelPendingSpawns();

        foreach (EnemyHealth enemy in activeEnemies)
        {
            if (enemy != null)
                enemy.Died -= HandleEnemyDied;
        }

        activeEnemies.Clear();
    }

    private bool ValidateSetup()
    {
        if (enemySpawner == null ||
            !enemySpawner.isActiveAndEnabled ||
            playerHealth == null ||
            flashlightBattery == null ||
            waves == null ||
            waves.Length == 0)
        {
            Debug.LogError(
                "WaveManager needs an active spawner, player health, " +
                "battery, and at least one wave.",
                this
            );

            return false;
        }

        foreach (WaveData wave in waves)
        {
            if (wave == null ||
                wave.enemyGroups == null ||
                wave.enemyGroups.Length == 0)
            {
                Debug.LogError("A wave has no enemy groups.", this);
                return false;
            }

            foreach (EnemyGroup group in wave.enemyGroups)
            {
                if (group == null ||
                    group.enemyCount <= 0 ||
                    group.enemyPrefab == null ||
                    group.enemyPrefab.GetComponent<EnemyHealth>() == null)
                {
                    Debug.LogError(
                        "Each group needs a positive count and an " +
                        "enemy prefab with EnemyHealth on its root.",
                        this
                    );

                    return false;
                }
            }
        }

        return true;
    }

    public int GetCurrentWaveNumber()
    {
        return currentWaveIndex + 1;
    }

    public int GetTotalWaves()
    {
        return waves != null ? waves.Length : 0;
    }
}