using UnityEngine;

public class DispelMagicTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private DispelMagic _dispelMagic;
     
    public override void Enter()
    {
        _skillManager.ActivateSkill(_dispelMagic);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_dispelMagic);
    }
}
