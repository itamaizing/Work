using System.Collections.Generic;
using UnityEngine;

public class MergeDarkState : StateBasic
{
    private float _duration;
    private Character _character;
    private SkillManager _skillManager;

    private const float _evadeBonus = 30f;
    private const float _magResBonus = 30f;

    public override States State => States.MergeDark;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void Apply(CharacterState characterStateComp, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = characterStateComp;
        _character     = characterStateComp.Character;
        _skillManager  = _character.Abilities;
        _duration      = durationToExit;

        var attrs = _character.AttributeSystem;
        attrs[CharacterAttributeName.ResistancePhysical].AddModifier(new AttributeModifier(_evadeBonus, ModifierType.Flat, this));
        attrs[CharacterAttributeName.ResistanceMagical].AddModifier(new AttributeModifier(_evadeBonus, ModifierType.Flat, this));
        attrs[CharacterAttributeName.EvasionPhysicalMelee].AddModifier(new AttributeModifier(_evadeBonus, ModifierType.Flat, this));
        attrs[CharacterAttributeName.EvasionPhysicalRange].AddModifier(new AttributeModifier(_evadeBonus, ModifierType.Flat, this));
        attrs[CharacterAttributeName.EvasionMagical].AddModifier(new AttributeModifier(_magResBonus, ModifierType.Flat, this));

        foreach (var skill in _skillManager.Abilities)
        {
            if (!IsInstantSkill(skill) && !skill.Disactive && skill is not MergeWithDarknessSkill)
                skill.Disactive = true;
        }
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0f)
            ExitState();
    }

    public override void ExitState()
    {
        foreach (var attr in _character.AttributeSystem.Attributes.Values)
            attr.RemoveBySource(this);

        foreach (var skill in _skillManager.Abilities)
        {
            if (!IsInstantSkill(skill) && skill.Disactive)
                skill.Disactive = false;
        }

        _character.IsInvisible = false;

        characterState.RemoveState(this);
    }

    private bool IsInstantSkill(Skill skill)
    {
        return skill.CastDeley <= 0f && skill.Channeling.CastDuration <= 0f;
    }
}