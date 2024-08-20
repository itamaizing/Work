using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokesOfAspiration : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;

    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison;

    private const float _timeBetweenAttack = 0.1f;
    private const float _decreaseCooldownTime = 0.3f;

    public override void Enter()
    {
        SetActive(true);
        if (_creeperStrike.Buff.AttackSpeed.Multiplier > _timeBetweenAttack)
        {
            _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_timeBetweenAttack);
        }
    }

    public override void Exit()
    {
        SetActive(false);
        if (_creeperStrike.Buff.AttackSpeed.Multiplier < 1.0f)
        {
            _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_timeBetweenAttack);
        }
    }

    public void UseTalentStrokesOfAspiration()
    {
        Debug.Log($"StrokesOfAspiration / UseTalentStrokesOfAspiration / after updateRemainingCooldownTimeForSpitPoison = {_spitPoison.RemainingÑooldownTime}");
        float updateRemainingCooldownTimeForSpitPoison = _spitPoison.RemainingÑooldownTime - _decreaseCooldownTime;
        _spitPoison.ReductionSetCooldown(updateRemainingCooldownTimeForSpitPoison);
        Debug.Log($"StrokesOfAspiration / UseTalentStrokesOfAspiration / before updateRemainingCooldownTimeForSpitPoison = {_spitPoison.RemainingÑooldownTime}");

        //float updateRemainingCooldownTimeForPoisonBall = _poisonBall.RemainingCooldownCharges - _decreaseCooldownTime;
        //_poisonBall.ReductionSetCooldown(updateRemainingCooldownTimeForPoisonBall);
        //Debug.Log("ReductinCooldown SpitPoison == " + updateRemainingCooldownTimeForPoisonBall);
        //Debug.Log("SpitPoison Cooldown == " + _poisonBall.RemainingCooldownCharges);
    }
}
