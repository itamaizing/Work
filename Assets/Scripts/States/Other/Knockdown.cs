using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockdown : RefreshingState
{
    private float _baseDuration;
    private float _duration;

    public override States State => States.Knockdown;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => new List<StatusEffect> { StatusEffect.Strengthening };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;

        float resistance = characterState.Character.Health.DefPhysDamage;
        float chanceToApply = 100f - resistance;

        if (Random.Range(0f, 100f) > chanceToApply)
        {
            Debug.Log("Knockdown was resisted due to high physical resistance");
            ExitState();
            return;
        }

        Debug.Log("Entering Knockdown State");

        _duration = durationToExit;
        _baseDuration = durationToExit;
        MaxStacksCount = 3;
        currentStacksCount = 1;

        ApplyDebuff();
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Knockdown State");

        RemoveDebuff();
        characterState.RemoveState(this);
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

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
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
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        if (currentStacksCount == 0)
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        else
            Stack(duration);

        return this;
    }
}