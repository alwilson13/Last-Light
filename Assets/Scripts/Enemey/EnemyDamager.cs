using System.Collections.Generic;
using UnityEngine;

/// Applies damage to enemies inside a 3D trigger.
public class EnemyDamager : MonoBehaviour
{
    public enum DamageType
    {
        Contact,
        DamageOverTime
    }

    [Header("Damage Settings")]
    [SerializeField]
    private DamageType damageType =
        DamageType.DamageOverTime;

    [Tooltip("Damage dealt per hit or damage tick.")]
    [Min(0f)]
    [SerializeField] private float damageAmount = 5f;

    [Tooltip("Minimum time between damage ticks on the same enemy.")]
    [Min(0.01f)]
    [SerializeField] private float damageInterval = 0.5f;

    private readonly Dictionary<EnemyHealth, float> nextDamageTimes =
        new Dictionary<EnemyHealth, float>();

    private void OnTriggerEnter(Collider other)
    {
        TryDamageEnemy(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (damageType == DamageType.DamageOverTime)
        {
            TryDamageEnemy(other);
        }
    }

    private void TryDamageEnemy(Collider other)
    {
        // Trigger callbacks can reach disabled MonoBehaviours.
        if (!isActiveAndEnabled || Time.timeScale <= 0f)
            return;

        if (damageAmount <= 0f)
            return;

        EnemyHealth enemyHealth =
            other.GetComponentInParent<EnemyHealth>();

        if (enemyHealth == null || enemyHealth.IsDead())
            return;

        if (nextDamageTimes.TryGetValue(
            enemyHealth,
            out float nextDamageTime))
        {
            if (Time.time < nextDamageTime)
                return;
        }

        // Record the cooldown first to prevent duplicate hits
        // from multiple colliders belonging to the same enemy.
        nextDamageTimes[enemyHealth] =
            Time.time + Mathf.Max(0.01f, damageInterval);

        enemyHealth.TakeDamage(damageAmount);
    }

    public void SetDamage(float amount)
    {
        damageAmount = Mathf.Max(0f, amount);
    }

    private void OnDisable()
    {
        nextDamageTimes.Clear();
    }
}