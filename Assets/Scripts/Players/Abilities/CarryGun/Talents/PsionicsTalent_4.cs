using UnityEngine;

public class PsionicsTalent_4 : Talent
{
    [SerializeField] private Tentacles _tentacles;

    public override void Enter()
    {
        _tentacles.ProtectiveCooconSpawnAttack(true);
    }

    public override void Exit()
    {
        _tentacles.ProtectiveCooconSpawnAttack(false);
    }
}
