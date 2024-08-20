using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedOfReptile : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CreeperStrike _creeperStrike;

    private float _duration = 3f;
    private float _increaseMoveSpeed = 2f;
    private float _increaseAttackSpeed = 2f;
    private float _increaseEvasion = 2f;

    private bool _isCanCast = true;

    public bool Enabled;

    protected override bool IsCanCast => _isCanCast;

    protected override IEnumerator PrepareJob()
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        _isCanCast = false;
        TryPayCost();
        IncreaseValues();

        yield return new WaitForSeconds(_duration);

        Debug.Log($"After _duration called ClearData()");
        ClearData();
    }

    protected override void ClearData()
    {
        Debug.Log("ClearData work");
        ResetValues();
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

        _isCanCast = true;
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
