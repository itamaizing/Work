using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootInstincts : Talent
{
    [SerializeField] private LightningMovementUpdated _lightningMovement;
    private float _reductionCooldownTime = 2.0f;

    public override void Enter()
    {
        isActive = true;
    }

    public override void Exit()
    {
        isActive = false;
    }

    public void ReductionCooldownLightningMovement()
    {
        float newRemainingCooldownTime = _lightningMovement.Remaining—ooldownTime - _reductionCooldownTime;
        _lightningMovement.ReductionSetCooldown(newRemainingCooldownTime);
        Debug.Log("Reduction cooldown in FootInstincts work");
    }
}
