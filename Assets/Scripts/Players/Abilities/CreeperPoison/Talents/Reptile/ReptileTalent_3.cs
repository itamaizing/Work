using UnityEngine;

public class ReptileTalent_3 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private PoisonSlap _poisonSlap;
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;

    public override void Enter()
    {
        skillManager.ActivateSkill(_poisonSlap);
        _creeperPoisonAura.ActiveWitheringPoison(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(_poisonSlap);
        _creeperPoisonAura.ActiveWitheringPoison(false);
    }
}
