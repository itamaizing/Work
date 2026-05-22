using UnityEngine;

public class EnergyFirstHitDamageTalent : Talent
{
    private EnergyFirstHitDamageBooster _booster;
    public override void Enter()
    {
        _booster ??= new EnergyFirstHitDamageBooster(character);
        if(!_booster.IsEnabled)
            _booster.Enable(true);
    }

    public override void Exit()
    {
        _booster?.Enable(false);
    }
}