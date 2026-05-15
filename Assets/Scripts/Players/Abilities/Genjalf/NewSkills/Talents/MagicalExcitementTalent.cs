using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class MagicalExcitementTalent : Talent
{
    [SerializeField] private float _increaseManaRegenerationPercentages = 1.1f;
    [SerializeField] private float _increaseManaRegenerationDuration = 3f;

    private WaitForSeconds _increaseManaRegenerationDeley;
    private Resource _mana;
    private float _defaultDuration = 3f;
    private bool _isActive = false;

    public override void Enter()
    {
        if(_isActive) return;
        _isActive = true;
        _increaseManaRegenerationDeley = new(_increaseManaRegenerationDuration);
        _mana = character.TryGetResource(ResourceType.Mana);

        character.DamageGeted += OnDamageTaked;
    }

    public override void Exit()
    {
        if(!_isActive) return;
        _isActive = false;
        character.DamageGeted -= OnDamageTaked;
    }

    private void OnDamageTaked(Damage damage, GameObject target)
    {
        character.CharacterState.CmdAddState(States.MagicalExcitement, _increaseManaRegenerationDuration,0,character.gameObject,name);
        StartCoroutine(IncreaseManaRegeneration());
    }

    private IEnumerator IncreaseManaRegeneration()
    {
        _mana.IncreaseRegenerationPeriod(_increaseManaRegenerationPercentages);

        yield return _increaseManaRegenerationDeley;

        _mana.ReduceRegenerationPeriod(_increaseManaRegenerationPercentages);
    }

    public void IncreaseDuration(float newDuration)
    {
        _increaseManaRegenerationDuration = newDuration;
        _increaseManaRegenerationDeley = new(_increaseManaRegenerationDuration);
    }

    public void SetDefaultDuration()
    {
        _increaseManaRegenerationDuration = _defaultDuration;
        _increaseManaRegenerationDeley = new(_increaseManaRegenerationDuration);
    }
}
