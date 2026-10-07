using System;
using UnityEngine;

/// Stores flashlight energy and handles draining and recharging.
public class FlashlightBattery : MonoBehaviour
{
    [Header("Battery Settings")]
    [Min(1f)]
    [SerializeField] private float maxEnergy = 100f;

    private float currentEnergy;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public bool HasEnergy => currentEnergy > 0f;

    public event Action<float, float> EnergyChanged;

    private void Awake()
    {
        maxEnergy = Mathf.Max(1f, maxEnergy);
        currentEnergy = maxEnergy;
    }

    private void Start()
    {
        EnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    /// Removes energy and returns the amount actually consumed.
    public float ConsumeEnergy(float amount)
    {
        if (amount <= 0f || !HasEnergy)
            return 0f;

        float consumed = Mathf.Min(amount, currentEnergy);
        currentEnergy -= consumed;

        EnergyChanged?.Invoke(currentEnergy, maxEnergy);

        return consumed;
    }

    /// Adds energy and returns the amount actually restored.
    public float Recharge(float amount)
    {
        if (amount <= 0f)
            return 0f;

        float restored = Mathf.Min(
            amount,
            maxEnergy - currentEnergy
        );

        if (restored <= 0f)
            return 0f;

        currentEnergy += restored;

        EnergyChanged?.Invoke(currentEnergy, maxEnergy);

        return restored;
    }
}