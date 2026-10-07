using UnityEngine;

/// Tracks which object supplied a modifier so it can be removed later.
public class ActiveStatModifier
{
    public Object source;
    public StatModifier modifier;

    public ActiveStatModifier(
        Object source,
        StatModifier modifier)
    {
        this.source = source;
        this.modifier = modifier;
    }
}