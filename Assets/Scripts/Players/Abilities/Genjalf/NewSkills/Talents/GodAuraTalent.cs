using UnityEngine;

public class GodAuraTalent : Talent
{
    [SerializeField] private GodAura _godAura;

    public override void Enter()
    {
        if(!_godAura.IsActive)
            _godAura.ActivateAura(true,isAffectOnOwner: true);
    }

    public override void Exit()
    {
        if(_godAura.IsActive)
            _godAura.ActivateAura(false, isAffectOnOwner: true);
    }
}
