using UnityEngine;

public class GettingDispelTalent : Talent
{
    [SerializeField] private SkillManager _skillManager;
    [SerializeField] private GettingDispelPassive _gettingDispel;
    
    public override void Enter()
    {
        _skillManager.ActivateSkill(_gettingDispel);

        if (!_gettingDispel.GettingDispelEnabled)
        {
            _gettingDispel.EnableGettingDispel(true);
        }
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_gettingDispel);
        
        if (_gettingDispel.GettingDispelEnabled)
        {
            _gettingDispel.EnableGettingDispel(false);
        }
    }
}
