using UnityEngine;

public class LowHealthReductionTalent : Talent
{
    [SerializeField] private LowHealthReductionPassive _lowHealthReductionPassive;
    
    public override void Enter()
    {
        character.Abilities.ActivateSkill(_lowHealthReductionPassive);
        _lowHealthReductionPassive.EnableHealthReduction(true);
    }

    public override void Exit()
    {
        _lowHealthReductionPassive.EnableHealthReduction(false);
        character.Abilities.DeactivateSkill(_lowHealthReductionPassive);
    }
}
