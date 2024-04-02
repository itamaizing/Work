using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class DamageAbsorption : BaseEffect
{
    public float MaxAbsorption;
    public float Duration;
    private float summDamage;
    private float _damage;

    void Start()
    {
        StartCoroutine(TimeAbsorption(Duration));
        Type = EffectType.Buff;
    }

    public void Absorption(ref float modifiedDamage)
    {
        _damage = modifiedDamage;
        summDamage += _damage;

        if (summDamage < MaxAbsorption)
        {
            modifiedDamage = 0;
        }
        else
        {
            modifiedDamage = summDamage - MaxAbsorption;
            StopCoroutine(TimeAbsorption(0f));
            Destroy(this);
        }
    }

    private IEnumerator TimeAbsorption(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(this);
    }

    public void AddMaxAbsorbtion(float value)
    {
        Debug.Log("Do- " + MaxAbsorption);
		MaxAbsorption += value;
        Debug.Log("Posle- " + MaxAbsorption);
    }

    public void AddBuffAbsorbtion(float value)
    {
        Debug.Log("Do- " + MaxAbsorption);
        MaxAbsorption +=MaxAbsorption*value;
        Debug.Log("Posle- " + MaxAbsorption);
    }
}

