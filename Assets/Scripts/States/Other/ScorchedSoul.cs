using System.Collections.Generic;
using UnityEngine;

public class ScorchedSoul : RefreshingState
{
    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };
    private float _reducePercentage = .5f;

    public override States State => States.ScorchedSoul;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering ScorchedSoulDebuff State");

        var abilities = characterState.GetComponentInChildren<SkillManager>();

        foreach (var ability in abilities.Abilities)
        {
            Debug.LogWarning($"Cast speed before: {ability.Buff.CastSpeed.Multiplier}");
            ability.Buff.CastSpeed.ReductionPercentage(_reducePercentage);
            Debug.LogWarning("Cast speed reduced!!!! - CharacterState.EnterState()");
            Debug.LogWarning($"Cast speed after: {ability.Buff.CastSpeed.Multiplier}");
            Debug.LogWarning($"Cast speed after: {ability.Buff.CastSpeed.GetBuffedValue(1f)}");
        }
    }

    public override void ExitState()
    {
        if (!characterState.Check(StatusEffect.AbilitySpeed))
        {
            //return cast speed
            if (characterState.TryGetComponent<SkillManager>(out SkillManager abilities))
            {
                foreach (var ability in abilities.Abilities)
                {

                    ability.Buff.CastSpeed.Reset();
                }
            }
        }
        //if (characterState.Check(StatusEffect.AbilityCooldownSpeed))
        //{
        //    //return abilitys' CD speed
        //}
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        duration = time;

        if (currentStacksCount < 3)
        {
            currentStacksCount++;
            foreach (var ability in abilities.Abilities)
            {
                Debug.LogWarning($"Cast speed before: {ability.Buff.CastSpeed.Multiplier}");
                ability.Buff.CastSpeed.ReductionPercentage(_reducePercentage * currentStacksCount);
                Debug.LogWarning("Cast speed reduced!!!! - CharacterState.EnterState()");
                Debug.LogWarning($"Cast speed after: {ability.Buff.CastSpeed.Multiplier}");
                Debug.LogWarning($"Cast speed after: {ability.Buff.CastSpeed.GetBuffedValue(1f)}");
            }
        }

        return true;
    }

    public override void UpdateState()
    {
    }
}
