using UnityEngine;

public class RingOfFireTalent : Talent
{
    private RingOfFireSkill _ringOfFire;
    public override void Enter()
    {
        if (!_ringOfFire)
            _ringOfFire = character.Abilities.GetSkill<RingOfFireSkill>();
        
        character.Abilities.ActivateSkill(_ringOfFire);
    }

    public override void Exit()
    {
        if (!_ringOfFire)
            _ringOfFire = character.Abilities.GetSkill<RingOfFireSkill>();
        
        character.Abilities.DeactivateSkill(_ringOfFire);
    }
}
