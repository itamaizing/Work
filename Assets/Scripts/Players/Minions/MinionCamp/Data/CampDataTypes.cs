using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct UnitData
{
    public GameObject Prefab;
    [Min(0)] public int XpReward;
    [Tooltip("Лидер: время до респавна лагеря после смерти/захвата лидера.Рядовой: время возрождения, пока лагерь захвачен.")]
    [Min(0f)] public float RespawnTime;
    public Sprite Icon;
}

[Serializable]
public struct CreepData
{
    public UnitData Unit;
    [Min(1)] public int Count;
}

[Serializable]
public class CampWave
{
    public UnitData Leader;
    public List<CreepData> Creeps = new();
}
