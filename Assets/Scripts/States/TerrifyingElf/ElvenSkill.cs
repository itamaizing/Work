using System.Collections.Generic;
using UnityEngine;

public class ElvenSkill : RefreshingState
{
    private MoveComponent _move;
    private GameObject _elvenSkillEffect;
    private TerrifyingElfAura _aura;
    private float _baseDuration;

    private const float PercentBonusPerStack = 0.1f;
    private const float ElvenBoostWindowChance = 0.3f;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.ElvenSkill;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    private List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public ElvenSkill()
    {
        MaxStacksCount = 3;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        AddStack();
        _move = character.GetComponent<MoveComponent>();
        _move.SetCanMoveState(true);

        _aura = character.GetComponent<TerrifyingElfAura>();
        if (_aura != null && _aura.ElvenSkillEffect != null)
        {
            _elvenSkillEffect = _aura.ElvenSkillEffect;
            _elvenSkillEffect.SetActive(true);
        }

        abilities.GetSkill<ElvenReflexes>().Disactive = false;
        
        if (abilities != null)
        {
            if (Random.value > ElvenBoostWindowChance) return;
            foreach (var skill in abilities.Abilities)
            {
                if (skill == null) continue;

                if (skill.Info.DamageType == DamageType.Physical || skill.Info.DamageType == DamageType.Both)
                    skill.CastStarted += OnPhysCastStarted;
                else
                    skill.CastStarted += NotPhysCastStarted;

                if (skill is ReconnaissanceFire rf) rf.TryStartElvenBoostWindow();
                if (skill is ShotIntoSky si) si.TryStartBoost();
                if (skill is ShotsIntoSky sis) sis.TryStartBoost();
                if (skill is GroundTrap gt) gt.TryStartBoost();
            }
        }
    }

    public override bool Stack(float time)
    {
        duration = time;
        AddStack();
        if (abilities != null)
        {
            if (Random.value > ElvenBoostWindowChance) return true;
            foreach (var skill in abilities.Abilities)
            {
                if (skill == null) continue;
                if (skill is ReconnaissanceFire rf) rf.TryStartElvenBoostWindow();
                if (skill is ShotIntoSky si) si.TryStartBoost();
                if (skill is ShotsIntoSky sis) sis.TryStartBoost();
                if (skill is GroundTrap gt) gt.TryStartBoost();
            }
        }

        return true;
    }

    private void AddStack()
    {
        if (abilities == null) return;
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
        }

        foreach (var skill in abilities.Abilities)
        {
            if (skill == null) continue;

            skill.Attributes[SkillAttributeName.Length].AddModifier(
                new AttributeModifier(PercentBonusPerStack, ModifierType.Percent, source: this));
            skill.Attributes[SkillAttributeName.Radius].AddModifier(
                new AttributeModifier(PercentBonusPerStack, ModifierType.Percent, source: this));
        }
    }
    
    public override void ReduceStack()
    {
        ReduceStackExternal();
    }

    public void ReduceStackExternal(bool isExternal = false)
    {
        currentStacksCount--;
        duration = _baseDuration;
        if (isExternal)
        {
            characterState.StateIcons.ActivateIco(States.ElvenSkill,duration,-1,true,MaxStacksCount);
        }
        
        if (currentStacksCount > 0)
        {
            RemoveOneStack();
            return;
        }
        ExitState();
    }

    private void RemoveOneStack()
    {
        if (abilities == null) return;

        foreach (var skill in abilities.Abilities)
        {
            if (skill == null) continue;

            skill.Attributes[SkillAttributeName.Length].RemoveBySource(this, all: false);
            skill.Attributes[SkillAttributeName.Radius].RemoveBySource(this, all: false);
        }
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        
        if (_move) _move.SetCanMoveState(false);

        if (abilities != null)
        {
            foreach (var skill in abilities.Abilities)
            {
                if (skill == null) continue;

                skill.Attributes[SkillAttributeName.Length].RemoveBySource(this, all: true);
                skill.Attributes[SkillAttributeName.Radius].RemoveBySource(this, all: true);

                if (skill.Info.DamageType == DamageType.Physical || skill.Info.DamageType == DamageType.Both)
                    skill.CastStarted -= OnPhysCastStarted;
                else
                    skill.CastStarted -= NotPhysCastStarted;
            }
        }

        if (_elvenSkillEffect != null)
            _elvenSkillEffect.SetActive(false);
        
        abilities.GetSkill<ElvenReflexes>().Disactive = true;
        base.ExitState();
    }

    public override void UpdateState()
    {
    }
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;
        _baseDuration = durationToExit;
        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
        {
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        }
        else
        {
            Stack(duration);
        }

        return this;
    }

    private void OnPhysCastStarted()
    {
        if (_move) _move.SetCanMoveState(true);
    }

    private void NotPhysCastStarted()
    {
        if (_move) _move.SetCanMoveState(false);
    }
}