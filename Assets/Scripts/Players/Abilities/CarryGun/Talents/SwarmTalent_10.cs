using UnityEngine;

public class SwarmTalent_10 : Talent
{
    [SerializeField] private WombSpawn _wombSpawn;

    public override void Enter()
    {
        _wombSpawn.WombSpreadsParasites(true);
    }

    public override void Exit()
    {
        _wombSpawn.WombSpreadsParasites(false);
    }
}