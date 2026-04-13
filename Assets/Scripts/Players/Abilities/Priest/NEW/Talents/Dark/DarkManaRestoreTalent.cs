using UnityEngine;

public class DarkManaRestoreTalent : Talent
{
    [SerializeField] private DarkManaRestoreSkill _darkSkill;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_darkSkill);
        
        if(!_darkSkill.DarkManaEnabled)
            _darkSkill.EnableDarkMana(true);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_darkSkill);
        
        _darkSkill.EnableDarkMana(false);
    }
}
