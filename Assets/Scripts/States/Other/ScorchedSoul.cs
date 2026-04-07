using System.Collections.Generic;
using UnityEngine;

public class ScorchedSoul : RefreshingState
{
    private SkillManager abilities;
    
    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };
    private float _reducePercentage = .5f;
    private float _baseDuration;
    private float _duration;

    public override States State => States.ScorchedSoul;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering ScorchedSoulDebuff State");

        characterState = character;
        
        abilities = characterState.Character.Abilities;

        foreach (var ability in abilities.Abilities)
        {
            Debug.LogWarning($"Cast speed before: {ability.Buff.CastSpeed.Multiplier}");
            ability.Buff.CastSpeed.ReductionPercentage(_reducePercentage);
            Debug.LogWarning("Cast speed reduced!!!! - CharacterState.EnterState()");
            Debug.LogWarning($"Cast speed after: {ability.Buff.CastSpeed.Multiplier}");
            Debug.LogWarning($"Cast speed after: {ability.Buff.CastSpeed.GetBuffedValue(1f)}");
        }
        
        _duration = durationToExit;
        _baseDuration = durationToExit;
        MaxStacksCount = 3;
        currentStacksCount = 1;
    }

    public override void ExitState()
    {
        if (!characterState.Check(StatusEffect.AbilitySpeed))
        {
            //return cast speed
            if (abilities)
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
        if (currentStacksCount < 3)
        {
            currentStacksCount++;
            _duration = _baseDuration;
            foreach (var ability in abilities.Abilities)
            {
                Debug.LogWarning($"Cast speed before: {ability.Buff.CastSpeed.Multiplier}");
                ability.Buff.CastSpeed.ReductionPercentage(_reducePercentage * currentStacksCount);
                Debug.LogWarning("Cast speed reduced!!!! - CharacterState.EnterState()");
                Debug.LogWarning($"Cast speed after: {ability.Buff.CastSpeed.Multiplier}");
                Debug.LogWarning($"Cast speed after: {ability.Buff.CastSpeed.GetBuffedValue(1f)}");
            }
            return true;
        }
        _duration = _baseDuration;
        return false;
    }

    public override void UpdateState()
    {
        if (_duration <= 0)
        {
            ExitState();
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
