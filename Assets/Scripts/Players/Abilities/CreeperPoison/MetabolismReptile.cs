using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetabolismReptile : Ability
{
    [SerializeField] private Character _player;

    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison; 
 
    private float _duration = 3f;

    private float _increaseHealthRegen = 2f;
    private float _increaseCastTime = 2f;
    private float _increaseCooldownTime = 2f;

    private Coroutine _useAbilityCoroutine;
    private Coroutine _increaseValuesCoroutine;
    public bool Enabled;
    protected override void Cast()
    {
        _useAbilityCoroutine = StartCoroutine(UseAbility());
    }

    protected override void Cancel()
    {
        ResetValues();

        if (_useAbilityCoroutine != null)
            StopCoroutine(UseAbility());
        
        if (_increaseValuesCoroutine != null)
            StopCoroutine(IncreaseValuesCoroutine());
    }

    private IEnumerator UseAbility()
    {
        PayCost();
        _increaseValuesCoroutine = StartCoroutine(IncreaseValuesCoroutine());
        yield return null;
    }

    private IEnumerator IncreaseValuesCoroutine()
    {
        IncreaseValues();

        yield return new WaitForSeconds(_duration);

        Cancel();
    }

    private void IncreaseValues()
    {
        float currentHpRegen = _player.Health.HpRegenerationValue;
        float increasedHealthRegen = currentHpRegen * _increaseHealthRegen;
        _player.Health.HpRegenerationValue = increasedHealthRegen;
        Debug.Log("HpRegen == " + _player.Health.HpRegenerationValue);

        float newRemainingCooldownForSpitPoison = _spitPoison.Remaining—ooldownTime / _increaseCooldownTime;
        _spitPoison.ReductionSetCooldown(newRemainingCooldownForSpitPoison);

        _poisonBall.Buff.CastSpeed.ReductionPercentage(_increaseCastTime);
        _spitPoison.Buff.CastSpeed.ReductionPercentage(_increaseCastTime);
    }

    private void ResetValues()
    {
        float currentHpRegen = _player.Health.HpRegenerationValue;
        float increasedHealthRegen = currentHpRegen / _increaseHealthRegen;
        _player.Health.HpRegenerationValue = increasedHealthRegen;
        Debug.Log("HpRegen == " + _player.Health.HpRegenerationValue);

        _poisonBall.Buff.CastSpeed.IncreasePercentage(_increaseCastTime);
        _spitPoison.Buff.CastSpeed.IncreasePercentage(_increaseCastTime);
    }

}
