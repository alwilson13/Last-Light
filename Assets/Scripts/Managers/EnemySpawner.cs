using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Spawns Last Light enemies at arena spawn points.
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject defaultEnemyPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Player Safety")]
    [SerializeField] private Transform playerTarget;

    [Min(0f)]
    [SerializeField] private float minimumPlayerDistance = 3f;

    [Header("Spawn Warning")]
    [Tooltip("A 3D warning effect placed at the spawn location.")]
    [SerializeField] private GameObject spawnIndicatorPrefab;

    [Min(0f)]
    [SerializeField] private float indicatorDuration = 1f;

    private readonly List<Transform> validSpawnPoints =
        new List<Transform>();

    private readonly List<SpawnRequest> pendingSpawns =
        new List<SpawnRequest>();

    private class SpawnRequest
    {
        public GameObject indicator;
        public Action<GameObject> callback;
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }
    }

    public GameObject SpawnDefaultEnemy()
    {
        return SpawnEnemy(defaultEnemyPrefab);
    }

    public GameObject SpawnEnemy(GameObject enemyPrefab)
    {
        if (!isActiveAndEnabled || enemyPrefab == null)
            return null;

        Transform point = GetRandomSpawnPoint();

        if (point == null)
        {
            Debug.LogWarning("No safe spawn point is available.", this);
            return null;
        }

        return Instantiate(
            enemyPrefab,
            point.position,
            Quaternion.identity
        );
    }

    public Coroutine SpawnEnemyWithIndicator(
        GameObject enemyPrefab,
        Action<GameObject> onEnemySpawned)
    {
        if (!isActiveAndEnabled || enemyPrefab == null)
        {
            onEnemySpawned?.Invoke(null);
            return null;
        }

        Transform point = GetRandomSpawnPoint();

        if (point == null)
        {
            Debug.LogWarning("No safe spawn point is available.", this);
            onEnemySpawned?.Invoke(null);
            return null;
        }

        // Reserve a fixed position for the warning and enemy.
        Vector3 spawnPosition = point.position;

        SpawnRequest request = new SpawnRequest
        {
            callback = onEnemySpawned
        };

        pendingSpawns.Add(request);

        return StartCoroutine(
            SpawnRoutine(enemyPrefab, spawnPosition, request)
        );
    }

    private IEnumerator SpawnRoutine(
        GameObject enemyPrefab,
        Vector3 spawnPosition,
        SpawnRequest request)
    {
        if (spawnIndicatorPrefab != null)
        {
            request.indicator = Instantiate(
                spawnIndicatorPrefab,
                spawnPosition,
                Quaternion.identity
            );
        }

        SpawnIndicatorRing ring =
            request.indicator.GetComponent<SpawnIndicatorRing>();

        if (ring != null)
        {
            ring.StartIndicator(indicatorDuration);
        }
        else
        {
            SpawnIndicator indicator =
                request.indicator.GetComponent<SpawnIndicator>();

            if (indicator != null)
                indicator.StartIndicator(indicatorDuration);
        }

        yield return new WaitForSeconds(
            Mathf.Max(0f, indicatorDuration)
        );

        if (request.indicator != null)
        {
            Destroy(request.indicator);
        }

        GameObject enemy = null;

        // Check again in case the player moved into the warning area.
        if (enemyPrefab != null && IsSafePosition(spawnPosition))
        {
            enemy = Instantiate(
                enemyPrefab,
                spawnPosition,
                Quaternion.identity
            );
        }

        pendingSpawns.Remove(request);
        request.callback?.Invoke(enemy);
    }

    private Transform GetRandomSpawnPoint()
    {
        validSpawnPoints.Clear();

        if (spawnPoints == null)
            return null;

        foreach (Transform point in spawnPoints)
        {
            if (point != null && IsSafePosition(point.position))
            {
                validSpawnPoints.Add(point);
            }
        }

        if (validSpawnPoints.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(
            0,
            validSpawnPoints.Count
        );

        return validSpawnPoints[index];
    }

    private bool IsSafePosition(Vector3 position)
    {
        if (playerTarget == null)
            return true;

        Vector3 difference = position - playerTarget.position;
        difference.y = 0f;

        float safeDistance = Mathf.Max(0f, minimumPlayerDistance);

        return difference.sqrMagnitude >=
            safeDistance * safeDistance;
    }

    /// Cancels warning timers and removes their indicators.
    public void CancelPendingSpawns()
    {
        StopAllCoroutines();

        SpawnRequest[] cancelled = pendingSpawns.ToArray();
        pendingSpawns.Clear();

        foreach (SpawnRequest request in cancelled)
        {
            if (request.indicator != null)
            {
                Destroy(request.indicator);
            }

            request.callback?.Invoke(null);
        }
    }

    private void OnDisable()
    {
        CancelPendingSpawns();
    }
}