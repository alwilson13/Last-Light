using UnityEngine;

/// Damages the player while they overlap the enemy's contact hitbox.
public class EnemyContactDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Min(1)]
    [SerializeField] private int contactDamage = 1;

    [Tooltip("Minimum time between successful contact hits.")]
    [Min(0f)]
    [SerializeField] private float damageCooldown = 0.75f;

    private EnemyHealth enemyHealth;
    private float nextDamageTime;
    private bool isStunned;

    private void Awake()
    {
        // Supports placing this script on a child hitbox.
        enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    private void OnEnable()
    {
        nextDamageTime = 0f;
        isStunned = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Allows another hit if the player remains in contact.
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider other)
    {
        if (isStunned || Time.timeScale <= 0f)
            return;

        if (enemyHealth != null && enemyHealth.IsDead())
            return;

        if (Time.time < nextDamageTime)
            return;

        // Finds health even when the player's collider is on a child.
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.IsDead() || playerHealth.IsInvincible())
            return;

        playerHealth.TakeDamage(contactDamage);

        nextDamageTime =
            Time.time + Mathf.Max(0f, damageCooldown);
    }

    /// Called by the enemy stun system.
    public void SetStunned(bool value)
    {
        isStunned = value;
    }
}