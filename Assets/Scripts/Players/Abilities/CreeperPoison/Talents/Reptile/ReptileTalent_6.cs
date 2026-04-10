using UnityEngine;

public class ReptileTalent_6 : Talent
{
    [SerializeField] private LightningMovement _lightningMovement;

    public override void Enter()
    {
        _lightningMovement.LightningEvade(true);
    }

    public override void Exit()
    {
        _lightningMovement.LightningEvade(false);
    }
}

