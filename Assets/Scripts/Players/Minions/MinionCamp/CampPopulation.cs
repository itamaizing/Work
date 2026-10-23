using System.Collections.Generic;
using UnityEngine;

public sealed class CreepSlot
{
    public int Id;
    public int CreepIndex;
    public UnitData Data;
    public int Level;
    public MinionComponent Instance;
    public bool Controlled;
    public float RespawnTimer = -1f;

    public bool IsAlive => Instance != null && !Instance.IsDead;
    public bool IsPendingRespawn => Instance == null && RespawnTimer >= 0f;
}

public sealed class CampPopulation
{
    private readonly List<CreepSlot> _slots = new();

    public MinionComponent Leader;
    public IReadOnlyList<CreepSlot> Slots => _slots;

    private int _nextSlotId;

    public CreepSlot AddCreep(UnitData data, int level, int creepIndex, MinionComponent instance)
    {
        var slot = new CreepSlot
        {
            Id = ++_nextSlotId,
            CreepIndex = creepIndex,
            Data = data,
            Level = level,
            Instance = instance
        };
        _slots.Add(slot);
        return slot;
    }

    public CreepSlot FindSlot(MinionComponent minion)
    {
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i].Instance == minion) return _slots[i];
        return null;
    }

    public void RemoveSlot(CreepSlot slot) => _slots.Remove(slot);

    public int CountAlive(GameObject prefab)
    {
        int count = 0;
        foreach (var slot in _slots)
            if (slot.IsAlive && slot.Data.Prefab == prefab) count++;
        return count;
    }

    public void CollectAlive(List<MinionComponent> buffer)
    {
        buffer.Clear();
        if (Leader != null && !Leader.IsDead) buffer.Add(Leader);
        foreach (var slot in _slots)
            if (slot.IsAlive) buffer.Add(slot.Instance);
    }

    public float TotalCurrentHp()
    {
        float total = 0f;
        if (Leader != null && !Leader.IsDead) total += Leader.Health.CurrentValue;
        foreach (var slot in _slots)
            if (slot.IsAlive) total += slot.Instance.Health.CurrentValue;
        return total;
    }

    public float TotalMaxHp()
    {
        float total = 0f;
        if (Leader != null && !Leader.IsDead) total += Leader.Health.MaxValue;
        foreach (var slot in _slots)
            if (slot.IsAlive) total += slot.Instance.Health.MaxValue;
        return total;
    }

    public void Clear()
    {
        Leader = null;
        _slots.Clear();
    }
}