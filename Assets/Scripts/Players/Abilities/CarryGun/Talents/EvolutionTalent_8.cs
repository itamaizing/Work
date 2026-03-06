using UnityEngine;

public class EvolutionTalent_8 : Talent
{
    [SerializeField] private DoubleCheliceraStrike doubleCheliceraStrike;
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(doubleCheliceraStrike);
        cheliceraStrike.ChanceCritDamageIncrease(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(doubleCheliceraStrike);
        cheliceraStrike.ChanceCritDamageIncrease(false);
    }
}
