using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockdown : StackableState
{
    private float _baseDuration;
    private float _duration;

    public override States State => States.Knockdown;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => new List<StatusEffect> { StatusEffect.Strengthening };

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;

        float resistance = characterState.Character.Health.DefPhysDamage;
        float chanceToApply = 100f - resistance;

        if (Random.Range(0f, 100f) > chanceToApply)
        {
            Debug.Log("Knockdown was resisted due to high physical resistance");
            OnExitState();
            return;
        }

        Debug.Log("Entering Knockdown State");

        _duration = durationToExit;
        _baseDuration = durationToExit;
        MaxStacksCount = 3;
        currentStacksCount = 1;

        ApplyDebuff();
    }

    protected override void OnExitState()
    {
        Debug.Log("Exiting Knockdown State");

        RemoveDebuff();
        characterState.RemoveStateFromList(this);
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            _duration = _baseDuration;
            ApplyDebuff();
            return true;
        }

        _duration = _baseDuration;
        return false;
    }

    public override void OnUpdateState()
    {
    }

    private void ApplyDebuff()
    {
        var abilities = characterState.GetComponentInChildren<SkillManager>();
        foreach (var ability in abilities.Abilities)
        {
            float reduction = 1f + (0.01f * currentStacksCount);
            ability.Buff.Damage.ReductionPercentage(reduction);
        }
    }

    private void RemoveDebuff()
    {
        var abilities = characterState.GetComponentInChildren<SkillManager>();
        foreach (var ability in abilities.Abilities)
        {
            float reduction = 1f + (0.01f * currentStacksCount);
            ability.Buff.Damage.IncreasePercentage(reduction);
        }
    }
}