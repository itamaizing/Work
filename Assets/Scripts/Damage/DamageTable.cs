using System;
using System.Collections;
using UnityEngine;

[Serializable]
public abstract class DamageTable
{
    public DamageType Type = DamageType.Magical;
    public bool IsDot;

    public abstract float GetValue();
    public abstract string GetDescriptionString();
}

[Serializable]
public class DamageTableSingle : DamageTable
{
    public float value;

    public override float GetValue() => value;
    public override string GetDescriptionString()
    {
        return $"<color=#ffaaff>{value}</color>";   //пока пример, потом цвета будет расставлять провайдер по типу урона
    }
}

[Serializable]
public class DamageTableRange : DamageTable
{
    public float min;
    public float max;

    public override float GetValue() => UnityEngine.Random.Range(min, max);
    public override string GetDescriptionString()
    {
        return $"<color=#ffaaff>{(min+max)/2}</color>";   //пример, можно отображать "10-15", можно брать среднее
    }
}
