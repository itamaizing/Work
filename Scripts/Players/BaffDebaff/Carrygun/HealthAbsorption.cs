using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HealthPlayer;

public class HealthAbsorption : BaseEffect
{
    public float PercentageOfAbsorption;
    private HealthPlayer _healthPlayer;
    
    void Start()
    {
        Type = EffectType.Debuff;
        _healthPlayer = transform.parent.GetComponent<HealthPlayer>();
        if (_healthPlayer != null)
        {
            _healthPlayer.AddHealth += HandleHealAbsorption;
        }
        StartCoroutine(TimeAbsorption(6f));
    }

    private void OnDestroy()
    {
        if (_healthPlayer != null)
        {
            _healthPlayer.AddHealth -= HandleHealAbsorption;
        }
    }

    private HealInfo HandleHealAbsorption(HealInfo healInfo)
    {
        Debug.Log(healInfo.ModifiedHeal);
        healInfo.ModifiedHeal *= PercentageOfAbsorption;
        Debug.Log(healInfo.ModifiedHeal);

        return healInfo;
    }
    private IEnumerator TimeAbsorption(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(this);
    }
}