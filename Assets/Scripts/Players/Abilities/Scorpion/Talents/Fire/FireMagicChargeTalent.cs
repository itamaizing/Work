using UnityEngine;

public class FireMagicChargeTalent : Talent
{
    [SerializeField] private NewPunch_Scorpion _punch;
    [SerializeField] private Kick_Scorpion _kick;
    [SerializeField] private CleavingBlade_Scorpion _cleavingBlade;
    [SerializeField] private ChainBlade _chainBlade;

    private FireMagicActivationBooster _booster;

    public override void Enter()
    {
        _booster ??= new FireMagicActivationBooster(character, _punch, _kick, _cleavingBlade, _chainBlade, character.Abilities, character);
        if(!_booster.IsEnabled)
            _booster.Enable(true);
    }

    public override void Exit()
    {
        _booster?.Enable(false);
    }
}