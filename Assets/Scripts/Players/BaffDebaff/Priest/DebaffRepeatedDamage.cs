using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebaffRepeatedDamage : BaseEffect
{
    private void Start()
    {
        Type = EffectType.Debuff;
    }
    public void CastDebaff(float TimeDebaff)
    {
        StartCoroutine(StartDebaff(TimeDebaff));
    }

    private IEnumerator StartDebaff(float time)
    {

        yield return new WaitForSeconds(time);

        Destroy(this);
    }
}
