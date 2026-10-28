using UnityEngine;

public class ReptileTalent_10 : Talent
{ 
    [SerializeField] private MetabolismReptile _metabolismReptile;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter() => _skillManager.ActivateSkill(_metabolismReptile);
    public override void Exit()  => _skillManager.DeactivateSkill(_metabolismReptile);
}

