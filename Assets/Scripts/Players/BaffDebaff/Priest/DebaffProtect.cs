using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebaffProtect : BaseEffect
{ 
    public bool IsProtectDebaff;

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
        IsProtectDebaff = true;
        yield return new WaitForSeconds(time);
        IsProtectDebaff = false;

        yield return new WaitForSeconds(0.2f);
        Destroy(this);
    }
}
