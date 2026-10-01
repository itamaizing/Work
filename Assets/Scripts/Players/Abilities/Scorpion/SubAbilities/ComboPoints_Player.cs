using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ComboPoints_Player : Resource
{
    [Header("Combo Points Settings")]
    [SerializeField] private int _maxComboPoints = 3;

    public int CurrentComboPoints { get; private set; } = 0;
    public int MaxComboPoints => _maxComboPoints;

    public int ComboAbilitiesUsed { get; private set; } = 0;

    public event Action<int> OnComboPointsChanged;

    private void Awake()
    {
        CurrentComboPoints = 0;
        ComboAbilitiesUsed = 0;
    }

    public override void Add(float value)
    {
        if (value <= 0) return;

        int added = Mathf.FloorToInt(value);
        int oldValue = CurrentComboPoints;

        CurrentComboPoints = Mathf.Clamp(CurrentComboPoints + added, 0, _maxComboPoints);

        if (CurrentComboPoints != oldValue)
        {
            OnComboPointsChanged?.Invoke(CurrentComboPoints);
        }
    }

    public bool TryUse(int amount = 1)
    {
        if (CurrentComboPoints < amount)
            return false;

        CurrentComboPoints -= amount;
        ComboAbilitiesUsed += amount;

        OnComboPointsChanged?.Invoke(CurrentComboPoints);
        return true;
    }

    public void RemoveAll()
    {
        if (CurrentComboPoints == 0) return;

        CurrentComboPoints = 0;
        OnComboPointsChanged?.Invoke(0);
    }

    public bool HasPoints(int amount = 1) => CurrentComboPoints >= amount;

    public void SetPoints(int value)
    {
        CurrentComboPoints = Mathf.Clamp(value, 0, _maxComboPoints);
        OnComboPointsChanged?.Invoke(CurrentComboPoints);
    }
}
