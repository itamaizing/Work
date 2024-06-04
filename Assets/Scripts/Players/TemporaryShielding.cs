using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TemporaryShielding : Shielding
{
    //private float shieldAmount;
    private float shieldDurationTime;
    public TemporaryShielding(HealthComponent healthComponent, float shieldAmount, DamageType damageType, float shieldDurationTime) : base(healthComponent, shieldAmount, damageType)
    {
        //StartCoroutine(ShieldLifeTime(shieldAmount,shieldDurationTime));
    }

    private void GainShieldForTime(float shield, float time)
    {
        shieldAmount = shield;
        shieldDurationTime = time;
    }

    ~TemporaryShielding() 
    {
        Debug.LogWarning("Из памяти удалился временный щит");
    }

    void Update()
    {
        
    }

    private IEnumerator ShieldLifeTime(float shield,float time)
    {
        shieldAmount = shield;
        yield return new WaitForSeconds(time);
        shieldAmount = 0;
    }
}
