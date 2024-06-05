using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HealthComponent;

public class HealthAbsorption : BaseEffect
{
    public float PercentageOfAbsorption;
    private HealthComponent _healthComponent;
    
    void Start()
    {
        Type = EffectType.Debuff;
        _healthComponent = transform.parent.GetComponent<HealthComponent>();
        if (_healthComponent != null)
        {
            _healthComponent.AddHealth += HandleHealAbsorption;
        }
        StartCoroutine(TimeAbsorption(6f));
    }

    private void OnDestroy()
    {
        if (_healthComponent != null)
        {
            _healthComponent.AddHealth -= HandleHealAbsorption;
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