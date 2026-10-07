using System;

/// Defines a change to one player stat.
[Serializable]
public class StatModifier
{
    public StatType statType;
    public StatModifierType modifierType;

    // Flat: 1 means +1.
    // Percentage: 0.2 means +20%.
    public float value;
}