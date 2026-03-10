using System.Collections.Generic;
using UnityEngine;

public class MoveCreature : MoveComponent
{
   [SerializeField] protected float _moveDurationPerUnit = 0.2f;

   private readonly Dictionary<string, float> _speedSources = new();

    public float  MoveDurationPerUnit { get => _moveDurationPerUnit; set => _moveDurationPerUnit = value; }
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
