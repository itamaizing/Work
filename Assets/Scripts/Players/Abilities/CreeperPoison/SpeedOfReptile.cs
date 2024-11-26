using Mirror;
using System.Collections;
using UnityEngine;

public class SpeedOfReptile : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CreeperStrike _creeperStrike;

    private float _duration = 15f;
    private float _increaseMoveSpeed = 2f;
    private float _increaseAttackSpeed = 2f;
    private float _increaseEvasion = 2f;

    private bool _isCanCast = true;

    public bool Enabled;
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => _isCanCast;

    protected override IEnumerator PrepareJob()
    {
        Debug.Log("SpeedOfReptile / PrepareJob");
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        Debug.Log("SpeedOfReptile / CastJob");
        IncreaseValues();
        yield return null;
    }

    protected override void ClearData()
    {
        Debug.Log("ClearData work");
    }

    private void IncreaseValues()
    {
        Debug.Log("SpeedOfReptile / IncreaseValues");

        _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_increaseAttackSpeed);
        Debug.Log($"SpeedOfReptile / IncreaseValues / IncreaseAttackSpeed = {_creeperStrike.Buff.AttackSpeed.Multiplier}");
        CmdIncreaseValues();

        Invoke("ResetValues", _duration);
    }

    private void ResetValues()
    {
        Debug.Log("SpeedOfReptile / ResetValues");

        _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_increaseAttackSpeed);
        Debug.Log($"SpeedOfReptile / IncreaseValues / ReductionAttackSpeed = {_creeperStrike.Buff.AttackSpeed.Multiplier}");
        CmdResetValues();
    }

    [Command]
    private void CmdIncreaseValues()
    {
        Debug.Log("SpeedOfReptile / CmdIncreaseValues");

        _player.Health.ResistMagDamage *= _increaseEvasion;
        _player.Health.EvadeMeleeDamage *= _increaseEvasion; 
        _player.Health.EvadeRangeDamage *= _increaseEvasion; 

        Debug.Log($"SpeedOfReptile / CmdIncreaseValues / _player.Health.EvadeMagicDamage = {_player.Health.ResistMagDamage}");
        Debug.Log($"SpeedOfReptile / CmdIncreaseValues / _player.Health.EvadeMeleeDamage = {_player.Health.EvadeMeleeDamage}");
        Debug.Log($"SpeedOfReptile / CmdIncreaseValues / _player.Health.EvadeRangeDamage = {_player.Health.EvadeRangeDamage}");

        _player.Move.ChangeMoveSpeed(_increaseMoveSpeed);
    }

    [Command]
    private void CmdResetValues()
    {
        Debug.Log("SpeedOfReptile / CmdResetValues");

        _player.Health.ResistMagDamage /= _increaseEvasion;
        _player.Health.EvadeMeleeDamage /= _increaseEvasion;
        _player.Health.EvadeRangeDamage /= _increaseEvasion;

        Debug.Log($"SpeedOfReptile / CmdResetValues / _player.Health.EvadeMagicDamage = {_player.Health.ResistMagDamage}");
        Debug.Log($"SpeedOfReptile / CmdResetValues / _player.Health.EvadeMeleeDamage = {_player.Health.EvadeMeleeDamage}");
        Debug.Log($"SpeedOfReptile / CmdResetValues / _player.Health.EvadeRangeDamage = {_player.Health.EvadeRangeDamage}");

        _player.Move.SetDefaultSpeed();
    }

}
