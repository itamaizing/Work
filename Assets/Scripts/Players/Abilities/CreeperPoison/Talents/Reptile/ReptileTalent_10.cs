using UnityEngine;

public class ReptileTalent_10 : Talent
{ 
    [SerializeField] private MetabolismReptile _metabolismReptile;
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_metabolismReptile);
        _creeperPoisonAura.ActiveWitheringPoisonMetabolism(true);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_metabolismReptile);
        _creeperPoisonAura.ActiveWitheringPoisonMetabolism(false);
    }
}

