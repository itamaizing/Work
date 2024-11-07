using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockdown : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;
    public override States State => States.Knockdown;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override StateType Type => StateType.Physical;

    public override List<StatusEffect> Effects => throw new System.NotImplementedException();

    public override float TEST_ChangeableValue { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering KnockdownDebuff State");
        _characterState = character;

        //effects.Add(StatusEffect.OutcomingDamage);

        _duration = durationToExit;
        _baseDuration = durationToExit;

        var abilities = _characterState.GetComponentInChildren<SkillManager>();

        foreach (var ability in abilities.Abilities)
        {
            ability.Buff.Damage.ReductionPercentage(1.1f);
        }
    }

    public override void ExitState()
    {
        Debug.Log("Exiting KnockdownDebuff State");

        //if (_characterState.Check(StatusEffect.OutcomingDamage))
        //{
        //    //return damage mulriplier

        //    var abilities = _characterState.GetComponentInChildren<SkillManager>();

        //    foreach (var ability in abilities.Abilities)
        //    {
        //        ability.Buff.Damage.IncreasePercentage(1.1f);
        //    }
        //}

        var abilities = _characterState.GetComponentInChildren<SkillManager>();

        foreach (var ability in abilities.Abilities)
        {
            ability.Buff.Damage.IncreasePercentage(1.1f);
        }

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        _duration = _baseDuration;
        return true;
    }

    public override void UpdateState()
    {
        Debug.Log("Updating KnockdownDebuff State");
        _duration -= Time.deltaTime;

        if (_duration < 0)
        {
            ExitState();
        }
    }
}
