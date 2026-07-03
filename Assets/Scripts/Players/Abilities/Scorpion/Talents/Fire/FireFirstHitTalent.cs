using UnityEngine;

public class FireFirstHitTalent : Talent
{
    [SerializeField] private NewPunch_Scorpion _punch;
    [SerializeField] private Kick_Scorpion _kick;
    [SerializeField] private CleavingBlade_Scorpion _cleavingBlade;
    [SerializeField] private ChainBlade _chainBlade;

    private NewTargetFireBooster _booster;

    public override void Enter()
    {
        _booster ??= new NewTargetFireBooster(character, _punch, _kick, _cleavingBlade,_chainBlade);
        if(!_booster.IsEnabled)
            _booster.Enable(true);
    }

    public override void Exit()
    {
        _booster?.Enable(false);
    }
}