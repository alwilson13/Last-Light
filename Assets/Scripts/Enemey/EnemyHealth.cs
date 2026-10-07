using System;
using UnityEngine;

/// Handles enemy health, death, and battery drops for Last Light.
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Min(0.01f)]
    [SerializeField] private float maxHealth = 30f;

    [Header("Death Effects")]
    [Tooltip("Optional burst or dissolve effect prefab.")]
    [SerializeField] private GameObject deathEffectPrefab;

    [Tooltip("Seconds before the spawned effect is removed.")]
    [Min(0.01f)]
    [SerializeField] private float deathEffectLifetime = 3f;

    [Header("Score Reward")]
    [Min(0)]
    [SerializeField] private int scoreValue = 100;

    private float currentHealth;
    private bool isDead;

    public event Action<float, float> HealthChanged;
    public event Action<float> Damaged;
    public event Action<EnemyHealth> Died;

    private void Awake()
    {
        maxHealth = Mathf.Max(0.01f, maxHealth);
        currentHealth = maxHealth;
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// Accepts fractional damage for continuous flashlight attacks.
    public void TakeDamage(float damageAmount)
    {
        if (isDead || damageAmount <= 0f)
            return;

        float actualDamage = Mathf.Min(
            damageAmount,
            currentHealth
        );

        currentHealth -= actualDamage;

        HealthChanged?.Invoke(currentHealth, maxHealth);
        Damaged?.Invoke(actualDamage);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        // Mark dead first to prevent duplicate drops or death events.
        isDead = true;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }

        TryDropBattery();
        SpawnDeathEffect();

        // The WaveManager can listen for this enemy's defeat.
        Died?.Invoke(this);

        Destroy(gameObject);
    }

    private void TryDropBattery()
    {
        if (PickupDropManager.Instance != null)
        {
            PickupDropManager.Instance.TryDropBatteryPickup(
                transform.position
            );
        }
    }

    private void SpawnDeathEffect()
    {
        if (deathEffectPrefab == null)
            return;

        GameObject effect = Instantiate(
            deathEffectPrefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(effect, Mathf.Max(0.01f, deathEffectLifetime));
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
}