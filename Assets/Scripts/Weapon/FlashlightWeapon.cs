using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Handles normal illumination, boosted beam damage,
/// and a radial ultimate pulse for Last Light.
public class FlashlightWeapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform beamOrigin;
    [SerializeField] private Light flashlight;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private FlashlightBattery battery;

    [Header("Target Layers")]
    [SerializeField] private LayerMask enemyLayers;

    [Tooltip("Walls and cover only. Exclude player and enemy layers.")]
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Normal Light")]
    [Min(0f)]
    [SerializeField] private float normalIntensity = 2f;

    [Min(0f)]
    [SerializeField] private float emergencyIntensity = 0.4f;

    [Header("Boosted Beam")]
    [Min(0.1f)]
    [SerializeField] private float beamRange = 10f;

    [Range(1f, 120f)]
    [SerializeField] private float beamAngle = 30f;

    [Min(0f)]
    [SerializeField] private float boostedIntensity = 6f;

    [Tooltip("Used if PlayerStats is missing.")]
    [Min(0f)]
    [SerializeField] private float fallbackDamagePerSecond = 10f;

    [Tooltip("Battery energy consumed each second while boosting.")]
    [Min(0f)]
    [SerializeField] private float boostDrainPerSecond = 10f;

    [Header("Ultimate Pulse")]
    [SerializeField] private KeyCode pulseKey = KeyCode.Q;

    [Min(0.1f)]
    [SerializeField] private float pulseRadius = 6f;

    [Min(0f)]
    [SerializeField] private float pulseDamage = 40f;

    [Min(0f)]
    [SerializeField] private float pulseBatteryCost = 35f;

    [Min(0f)]
    [SerializeField] private float pulseCooldown = 12f;

    [Header("Pulse Effects")]
    [Tooltip("Optional upward-facing particle effect.")]
    [SerializeField] private ParticleSystem skywardPulseEffect;

    [Tooltip("Optional Point Light used for the surrounding flash.")]
    [SerializeField] private Light pulseLight;

    [Min(0f)]
    [SerializeField] private float pulseIntensity = 10f;

    [Min(0.01f)]
    [SerializeField] private float pulseFlashDuration = 0.25f;

    private readonly HashSet<EnemyHealth> damagedEnemies =
        new HashSet<EnemyHealth>();

    private bool isBoosting;
    private float nextPulseTime;
    private Coroutine pulseFlashRoutine;

    public bool IsBoosting => isBoosting;

    public float PulseCooldownRemaining =>
        Mathf.Max(0f, nextPulseTime - Time.time);

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponentInParent<PlayerStats>();

        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>();

        if (battery == null)
            battery = GetComponentInParent<FlashlightBattery>();

        if (beamOrigin == null)
        {
            beamOrigin = flashlight != null
                ? flashlight.transform
                : transform;
        }

        if (pulseLight != null)
            pulseLight.enabled = false;

        SetBoosting(false);
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f)
            return;

        if (playerHealth != null && playerHealth.IsDead())
        {
            SetBoosting(false);
            return;
        }

        if (Input.GetKeyDown(pulseKey))
            TryUltimatePulse();

        UpdateBoostedBeam();
    }

    private void UpdateBoostedBeam()
    {
        bool wantsBoost =
            Input.GetMouseButton(0) &&
            battery != null &&
            battery.HasEnergy;

        if (!wantsBoost)
        {
            SetBoosting(false);
            return;
        }

        float activeSeconds = Time.deltaTime;

        if (boostDrainPerSecond > 0f)
        {
            float consumed = battery.ConsumeEnergy(
                boostDrainPerSecond * Time.deltaTime
            );

            // Only deal damage for the time paid for by the battery.
            activeSeconds = consumed / boostDrainPerSecond;
        }

        if (activeSeconds > 0f)
            DamageEnemiesInBeam(activeSeconds);

        SetBoosting(battery.HasEnergy && activeSeconds > 0f);
    }

    private void DamageEnemiesInBeam(float activeSeconds)
    {
        Vector3 origin = beamOrigin.position;

        Collider[] targets = Physics.OverlapSphere(
            origin,
            beamRange,
            enemyLayers,
            QueryTriggerInteraction.Ignore
        );

        damagedEnemies.Clear();

        float damagePerSecond = playerStats != null
            ? playerStats.Damage
            : fallbackDamagePerSecond;

        foreach (Collider target in targets)
        {
            EnemyHealth enemy =
                target.GetComponentInParent<EnemyHealth>();

            if (enemy == null ||
                enemy.IsDead() ||
                damagedEnemies.Contains(enemy))
            {
                continue;
            }

            Vector3 direction = target.bounds.center - origin;
            float distance = direction.magnitude;

            if (distance > beamRange)
                continue;

            if (distance > 0.001f)
            {
                if (Vector3.Angle(
                    beamOrigin.forward,
                    direction
                ) > beamAngle * 0.5f)
                {
                    continue;
                }

                // Cover prevents the beam from reaching this enemy.
                if (Physics.Raycast(
                    origin,
                    direction / distance,
                    distance,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
            }

            damagedEnemies.Add(enemy);

            enemy.TakeDamage(
                Mathf.Max(0f, damagePerSecond) * activeSeconds
            );
        }
    }

    public void TryUltimatePulse()
    {
        if (!isActiveAndEnabled ||
            Time.timeScale <= 0f ||
            Time.time < nextPulseTime ||
            battery == null)
        {
            return;
        }

        if (playerHealth != null && playerHealth.IsDead())
            return;

        float cost = Mathf.Max(0f, pulseBatteryCost);

        if (battery.CurrentEnergy < cost)
            return;

        nextPulseTime = Time.time + Mathf.Max(0f, pulseCooldown);
        battery.ConsumeEnergy(cost);

        // Center the blast on the player, not the flashlight tip.
        Vector3 origin = playerHealth != null
            ? playerHealth.transform.position
            : transform.position;

        Collider[] targets = Physics.OverlapSphere(
            origin,
            pulseRadius,
            enemyLayers,
            QueryTriggerInteraction.Ignore
        );

        damagedEnemies.Clear();

        foreach (Collider target in targets)
        {
            EnemyHealth enemy =
                target.GetComponentInParent<EnemyHealth>();

            if (enemy == null ||
                enemy.IsDead() ||
                !damagedEnemies.Add(enemy))
            {
                continue;
            }

            // The radial pulse reaches surrounding enemies,
            // including those behind cover.
            enemy.TakeDamage(pulseDamage);
        }

        if (skywardPulseEffect != null)
            skywardPulseEffect.Play();

        if (pulseLight != null)
        {
            if (pulseFlashRoutine != null)
                StopCoroutine(pulseFlashRoutine);

            pulseFlashRoutine = StartCoroutine(FlashPulseLight());
        }
    }

    private IEnumerator FlashPulseLight()
    {
        pulseLight.range = pulseRadius;
        pulseLight.intensity = pulseIntensity;
        pulseLight.enabled = true;

        yield return new WaitForSeconds(
            Mathf.Max(0.01f, pulseFlashDuration)
        );

        pulseLight.enabled = false;
        pulseFlashRoutine = null;
    }

    private void SetBoosting(bool value)
    {
        if (isBoosting != value)
        {
            isBoosting = value;

            if (AudioManager.Instance != null)
            {
                if (value)
                    AudioManager.Instance.PlayFlashlightOnSFX();
                else
                    AudioManager.Instance.PlayFlashlightOffSFX();
            }
        }

        if (flashlight == null)
            return;

        flashlight.range = beamRange;
        flashlight.spotAngle = beamAngle;

        flashlight.intensity = value
            ? boostedIntensity
            : battery != null && battery.HasEnergy
                ? normalIntensity
                : emergencyIntensity;
    }

    private void OnDisable()
    {
        if (pulseFlashRoutine != null)
        {
            StopCoroutine(pulseFlashRoutine);
            pulseFlashRoutine = null;
        }

        if (pulseLight != null)
            pulseLight.enabled = false;

        if (skywardPulseEffect != null)
        {
            skywardPulseEffect.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        SetBoosting(false);
    }
}