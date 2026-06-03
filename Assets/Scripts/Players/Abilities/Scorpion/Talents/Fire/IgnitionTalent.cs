using UnityEngine;

public class IgnitionTalent : Talent
{
    private IgnitionSkill _ignitionSkill;
    public override void Enter()
    {
        if (!_ignitionSkill)
            _ignitionSkill = character.Abilities.GetSkill<IgnitionSkill>();
        
        character.Abilities.ActivateSkill(_ignitionSkill);
    }

    public override void Exit()
    {
        if (!_ignitionSkill)
            _ignitionSkill = character.Abilities.GetSkill<IgnitionSkill>();
        
        character.Abilities.DeactivateSkill(_ignitionSkill);
    }
}
