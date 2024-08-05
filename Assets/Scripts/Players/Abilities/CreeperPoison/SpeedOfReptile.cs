using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedOfReptile : Ability
{
    [SerializeField] private Character _player;
    [SerializeField] private CreeperStrike _creeperStrike;

    private float _duration = 3f;
    private float _increaseMoveSpeed = 2f;
    private float _increaseAttackSpeed = 2f;
    private float _increaseEvasion = 2f;

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
        Debug.Log(" work");
        Cancel();
    }

    private void IncreaseValues()
    {
        _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_increaseAttackSpeed);

        CmdIncreaseValues();
    }

    private void ResetValues()
    {
        _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_increaseAttackSpeed);

        CmdResetValues();
    }

    [Command]
    private void CmdIncreaseValues()
    {
        _player.Health.EvadeMagicDamage *= _increaseEvasion;
        _player.Health.EvadeMeleeDamage *= _increaseEvasion;
        _player.Health.EvadeRangeDamage *= _increaseEvasion;

        _player.Move.ChangeMoveSpeed(_increaseMoveSpeed);
    }

    [Command]
    private void CmdResetValues()
    {
        _player.Health.EvadeMagicDamage /= _increaseEvasion;
        _player.Health.EvadeMeleeDamage /= _increaseEvasion;
        _player.Health.EvadeRangeDamage /= _increaseEvasion;

        _player.Move.SetDefaultSpeed();
    }

}
