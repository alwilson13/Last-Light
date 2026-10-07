using System;
using System.Collections.Generic;
using UnityEngine;

/// Stores adjustable player stats for Last Light.
/// Current health and battery energy belong to their own scripts.
public class PlayerStats : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float baseMovementSpeed = 6f;

    [Header("Flashlight Combat")]
    [Tooltip("Damage per second. Used by the flashlight combat script.")]
    [Min(0f)]
    [SerializeField] private float baseDamage = 10f;

    [Header("Calculated Stats - Runtime")]
    [SerializeField] private float displayedMovementSpeed;
    [SerializeField] private float displayedDamage;

    private readonly List<ActiveStatModifier> activeModifiers =
        new List<ActiveStatModifier>();

    public event Action StatsChanged;

    public float MovementSpeed =>
        Calculate(StatType.MovementSpeed, baseMovementSpeed);

    public float Damage =>
        Calculate(StatType.Damage, baseDamage);

    private void Awake()
    {
        RefreshDisplayedStats();
    }

    public float Calculate(StatType statType, float baseValue)
    {
        float flatBonus = 0f;
        float percentageBonus = 0f;

        foreach (ActiveStatModifier activeModifier in activeModifiers)
        {
            StatModifier modifier = activeModifier.modifier;

            if (modifier == null || modifier.statType != statType)
                continue;

            switch (modifier.modifierType)
            {
                case StatModifierType.Flat:
                    flatBonus += modifier.value;
                    break;

                case StatModifierType.Percentage:
                    percentageBonus += modifier.value;
                    break;
            }
        }

        return Mathf.Max(
            0f,
            (baseValue + flatBonus) * (1f + percentageBonus)
        );
    }

    /// Adds one modifier without replacing earlier ones.
    /// Useful for repeated runtime speed increases.
    public void AddModifier(
        UnityEngine.Object source,
        StatModifier modifier)
    {
        if (source == null || modifier == null)
            return;

        activeModifiers.Add(
            new ActiveStatModifier(source, modifier)
        );

        NotifyStatsChanged();
    }

    /// Replaces all modifiers belonging to this source.
    /// Useful when updating an existing item's or effect's stats.
    public void AddModifiers(
        UnityEngine.Object source,
        IEnumerable<StatModifier> modifiers)
    {
        if (source == null || modifiers == null)
            return;

        // Copy first in case the supplied collection is lazy.
        List<StatModifier> replacement =
            new List<StatModifier>(modifiers);

        RemoveModifiersFromList(source);

        foreach (StatModifier modifier in replacement)
        {
            if (modifier == null)
                continue;

            activeModifiers.Add(
                new ActiveStatModifier(source, modifier)
            );
        }

        NotifyStatsChanged();
    }

    /// Removes all modifiers belonging to a specific source.
    public void RemoveModifiers(UnityEngine.Object source)
    {
        if (source == null)
            return;

        if (RemoveModifiersFromList(source))
            NotifyStatsChanged();
    }

    private bool RemoveModifiersFromList(UnityEngine.Object source)
    {
        return activeModifiers.RemoveAll(
            modifier => modifier.source == source
        ) > 0;
    }

    private void NotifyStatsChanged()
    {
        RefreshDisplayedStats();
        StatsChanged?.Invoke();
    }

    private void RefreshDisplayedStats()
    {
        displayedMovementSpeed = MovementSpeed;
        displayedDamage = Damage;
    }

    private void OnValidate()
    {
        baseMovementSpeed = Mathf.Max(0f, baseMovementSpeed);
        baseDamage = Mathf.Max(0f, baseDamage);

        // Refresh Inspector values without firing gameplay
        // events from an editor validation callback.
        RefreshDisplayedStats();
    }
}