using UnityEngine;

public class ReptileTalent_3 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private SpeedOfReptile speed;
    [SerializeField] private MetabolismReptile _metabolismReptile;
    [SerializeField] private PoisonSlap _poisonSlap;

    public override void Enter()
    {
        skillManager.ActivateSkill(speed);
        skillManager.ActivateSkill(_metabolismReptile);
        skillManager.ActivateSkill(_poisonSlap);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(speed);
        skillManager.DeactivateSkill(_metabolismReptile);
        skillManager.DeactivateSkill(_poisonSlap);
    }
}
