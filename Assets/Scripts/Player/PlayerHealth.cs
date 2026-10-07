using System;
using System.Collections;
using UnityEngine;

/// Handles health, hit invincibility, healing, and death for Last Light.
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Min(1)]
    [SerializeField] private int maxHealth = 5;

    [Min(0f)]
    [SerializeField] private float invincibilityDuration = 1f;

    [Header("Damage Feedback")]
    [Tooltip("Assign the player's visible model renderers.")]
    [SerializeField] private Renderer[] playerRenderers;

    [Min(0.01f)]
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Death Settings")]
    [Tooltip("Scripts to disable on death, such as flashlight combat.")]
    [SerializeField] private MonoBehaviour[] disableOnDeath;

    private int currentHealth;
    private bool isDead;

    // Separate flags prevent external invincibility from
    // accidentally cancelling the protection after a hit.
    private bool hitInvincible;
    private bool externalInvincible;

    private Coroutine invincibilityCoroutine;
    private bool[] originalRendererStates;

    public event Action<int, int> HealthChanged;
    public event Action<int> Damaged;
    public event Action<int> Healed;
    public event Action Died;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;

        if (playerRenderers == null || playerRenderers.Length == 0)
        {
            playerRenderers = GetComponentsInChildren<Renderer>();
        }

        originalRendererStates = new bool[playerRenderers.Length];
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount <= 0 || isDead || IsInvincible())
            return;

        int actualDamage = Mathf.Min(damageAmount, currentHealth);
        currentHealth -= actualDamage;

        // Set protection before notifying other scripts.
        if (currentHealth > 0 && invincibilityDuration > 0f)
        {
            hitInvincible = true;
            invincibilityCoroutine =
                StartCoroutine(InvincibilityRoutine());
        }

        HealthChanged?.Invoke(currentHealth, maxHealth);
        Damaged?.Invoke(actualDamage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        // Remember visibility so hidden renderers stay hidden.
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
            {
                originalRendererStates[i] =
                    playerRenderers[i].enabled;
            }
        }

        float elapsed = 0f;
        bool visible = false;
        float interval = Mathf.Max(0.01f, blinkInterval);

        while (elapsed < invincibilityDuration)
        {
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null)
                {
                    playerRenderers[i].enabled =
                        originalRendererStates[i] && visible;
                }
            }

            float waitTime = Mathf.Min(
                interval,
                invincibilityDuration - elapsed
            );

            yield return new WaitForSeconds(waitTime);

            elapsed += waitTime;
            visible = !visible;
        }

        RestoreRenderers();

        hitInvincible = false;
        invincibilityCoroutine = null;
    }

    private void RestoreRenderers()
    {
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
            {
                playerRenderers[i].enabled =
                    originalRendererStates[i];
            }
        }
    }

    private void OnDisable()
    {
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
            invincibilityCoroutine = null;

            RestoreRenderers();
        }

        hitInvincible = false;
        externalInvincible = false;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        PlayerAiming aiming = GetComponent<PlayerAiming>();
        if (aiming != null)
            aiming.enabled = false;

        if (disableOnDeath != null)
        {
            foreach (MonoBehaviour behaviour in disableOnDeath)
            {
                if (behaviour != null && behaviour != this)
                {
                    behaviour.enabled = false;
                }
            }
        }

        Debug.Log("Game Over! Player died.", this);
        Died?.Invoke();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || isDead)
            return;

        int actualHeal = Mathf.Min(amount, maxHealth - currentHealth);

        if (actualHeal <= 0)
            return;

        currentHealth += actualHeal;

        HealthChanged?.Invoke(currentHealth, maxHealth);
        Healed?.Invoke(actualHeal);
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0 || isDead)
            return;

        maxHealth += amount;
        currentHealth += amount;

        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetInvincible(bool value)
    {
        externalInvincible = value;
    }

    public bool IsInvincible()
    {
        return hitInvincible || externalInvincible;
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
}