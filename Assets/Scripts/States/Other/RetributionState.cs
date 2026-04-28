using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class RetributionState : RefreshingState
{
    private Character _hero;
    private List<Skill> _baseLightSkills = new();
    private float _increaseDamageProcent = 0.15f;
    private float _baseDuration;

    private List<StatusEffect> _effects;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.Retribution;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _hero = characterState.Character;
        _baseDuration = durationToExit;
        duration = durationToExit;
        MaxStacksCount = 3;
        currentStacksCount = 1;
        GetLightSkills();
        IncreaseLightSkillDamage();

        foreach (var castingSkill in GetCastingSkills())
        {
            castingSkill.CastSuccess += CastingSkillOnCastEnded;
        }
    }

    private void CastingSkillOnCastEnded()
    {
        ExitState();
    }

    public override void UpdateState()
    {
    }

    private void GetLightSkills()
    {
        if (_baseLightSkills.Count > 0)
        {
            ReturnToBaseDamage();
        }

        var lightSkills = _hero.Abilities.Abilities.Where(c => c.Info.School == Schools.Light).ToList();
        foreach (var skill in lightSkills)
        {
            _baseLightSkills.Add(skill);
        }
    }

    private List<Skill> GetCastingSkills()
    {
        return _hero.Abilities.Abilities.Where(c => c.CastDeley > 0).ToList();
    }

    private void IncreaseLightSkillDamage()
    {
        foreach (var skill in _baseLightSkills)
        {
            skill.Buff.Damage.IncreasePercentage(1 + _increaseDamageProcent);
        }
    }

    private void ReturnToBaseDamage()
    {  
        foreach (var skill in _baseLightSkills)
        {
            skill.Buff.Damage.Reset();
        }
        _baseLightSkills.Clear();
    }
    
    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
            currentStacksCount++;

        duration = _baseDuration;
        RemainingDuration = _baseDuration;

        return true;
    }

    public override void ExitState()
    {
        duration = 0f;
        currentStacksCount = 0;
        ReturnToBaseDamage();
        characterState?.RemoveState(this);
        characterState = null;
    }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
        {
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
            currentStacksCount = 1;
        }
        else
        {
            Stack(durationToExit);
        }

        return this;
    }
}
