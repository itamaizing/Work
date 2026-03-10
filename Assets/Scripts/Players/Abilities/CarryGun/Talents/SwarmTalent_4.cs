using UnityEngine;

public class SwarmTalent_4 : Talent
{
    [SerializeField] private Tentacles _tentacles;

    public override void Enter()
    {
        _tentacles.InjectionAdrenaline(true);
        _tentacles.WombSpreadsMucus(true);
    }

    public override void Exit()
    {
        _tentacles.InjectionAdrenaline(false);
        _tentacles.WombSpreadsMucus(false);
    }
}
