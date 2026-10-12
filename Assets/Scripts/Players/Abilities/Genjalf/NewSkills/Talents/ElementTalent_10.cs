using UnityEngine;

public class ElementTalent_10 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private MagicVarietySkill skill;

    public override void Enter()
    {
        skillManager.ActivateSkill(skill); 
        skill.SetEnabled(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(skill); 
        skill.SetEnabled(false);
    }
}
