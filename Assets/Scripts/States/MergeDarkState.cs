using System.Collections.Generic;
using UnityEngine;

public class MergeDarkState : AbstractCharacterState
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

    protected override void EnterState(CharacterState characterStateComp, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = characterStateComp;
        _character     = characterStateComp.Character;
        _skillManager  = _character.Abilities;
        _duration      = durationToExit;
        //MaxStacksCount = 1;

        _character.Health.SetEvadeAll(_evadeBonus);
        _character.Health.SetEvadeMagic(_character.Health.ResistMagDamage + _magResBonus);

        foreach (var skill in _skillManager.Abilities)
        {
            if (!IsInstantSkill(skill) && !skill.Disactive && skill is not MergeWithDarknessSkill)
                skill.Disactive = true;
        }
    }

    public override void UpdateState()
    {
    }

    protected override void ExitState()
    {
        _character.Health.SetEvadeAll(-_evadeBonus);
        _character.Health.SetEvadeMagic(_character.Health.ResistMagDamage - _magResBonus);

        foreach (var skill in _skillManager.Abilities)
        {
            if (!IsInstantSkill(skill) && skill.Disactive)
                skill.Disactive = false;
        }

        _character.IsInvisible = false;
    }


    private bool IsInstantSkill(Skill skill)
    {
        return skill.CastDeley <= 0f && skill.Channeling.CastDuration <= 0f;
    }
}
