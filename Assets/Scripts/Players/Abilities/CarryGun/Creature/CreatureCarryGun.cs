using System.Collections.Generic;
using UnityEngine;

public class CreatureCarryGun : MonoBehaviour
{
    private readonly Dictionary<string, float> _speedSources = new();

    public float SpeedModifier { get; private set; } = 1f;

    public void SetSpeedModifier(string sourceId, float multiplier)
    {
        _speedSources[sourceId] = multiplier;
        RecalculateSpeed();
    }

    public void RemoveSpeedModifier(string sourceId)
    {
        if (_speedSources.ContainsKey(sourceId))
        {
            _speedSources.Remove(sourceId);
            RecalculateSpeed();
        }
    }

    private void RecalculateSpeed()
    {
        float total = 1f;

        foreach (var value in _speedSources.Values) total *= value;

        SpeedModifier = total;
    }
}