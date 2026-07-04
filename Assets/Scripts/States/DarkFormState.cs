using System.Collections.Generic;
using UnityEngine;

public class DarkFormState : AbstractCharacterState
{
    private Character _character;
    private SkillManager _skillManager;
    private AttributeModifier _speedModifier = new AttributeModifier(-0.1f, ModifierType.Percent);

    private const float _speedBonus = 0.15f;

    public override States State => States.DarkFormState;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    protected override void EnterState(CharacterState characterStateComp, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = characterStateComp;
        _character     = characterStateComp.Character;
        _skillManager  = _character.Abilities;
        MaxStacksCount = 1;

        _speedModifier.Value = _speedBonus;
        _character.Move.AddModifier(_speedModifier);

        SetShadowSkillActive(true);
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        _character.Move.RemoveModifier(_speedModifier);

        SetShadowSkillActive(false);

        characterState.RemoveState(this);
    }

    public override bool Stack(float time) => false;

    private void SetShadowSkillActive(bool value)
    {
        foreach (var skill in _skillManager.Abilities)
        {
            if (skill is ShadowSkill shadowSkill)
            {
                if (value)
                {
                    _skillManager.ActivateSkill(shadowSkill);
                }
                else
                {
                    _skillManager.DeactivateSkill(shadowSkill);
                }
                break;
            }
        }
    }
}
