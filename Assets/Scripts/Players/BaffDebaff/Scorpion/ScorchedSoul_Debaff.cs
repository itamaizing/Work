using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScorchedSoul_Debaff : BaseEffect
{
    private int _currentStacks = 1;
    private int _maxStacks;
    private float _stackDuration;
    private float _ReduseCastSpeedPerStack;

    private float _damageValue;
    private DamageType _damageType = DamageType.Magical;
    private AttackRangeType _attackRangeType = AttackRangeType.Inner;

    private void AddStack()
    {
        _currentStacks++;
        if(_currentStacks == 1)
        {
            StartCoroutine(StackTimer());
        }
        StartCoroutine(StackTimer());
    }

    private void DeleteStack()
    {
        _currentStacks--;
    }

    public void DealDamage(HealthComponent targetHealth)
    {
        targetHealth.TryTakeDamage(_damageValue, _damageType, _attackRangeType);
    }

    private void ResetTimer()
    {
        StopCoroutine(StackTimer());
    }
    private IEnumerator StackTimer()
    {
        yield return new WaitForSeconds(_stackDuration);
        DeleteStack();

    }
}
