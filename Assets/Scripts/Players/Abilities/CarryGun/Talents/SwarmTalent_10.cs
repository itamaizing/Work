using UnityEngine;

public class SwarmTalent_10 : Talent
{
    [SerializeField] private Tentacles _tentacles;

    public override void Enter()
    {
        _tentacles.WombSpreadsParasites(true);
    }

    public override void Exit()
    {
        _tentacles.WombSpreadsParasites(false);
    }
}