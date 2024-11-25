using UnityEngine;

public class FootInstincts : Talent
{
    [SerializeField] private LightningMovement _lightningMovement;
    private float _reductionCooldownTime = 2.0f;

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void ReductionCooldownLightningMovement()
    {
        float newRemainingCooldownTime = _lightningMovement.CooldownTime - _reductionCooldownTime;
        _lightningMovement.ReductionSetCooldown(newRemainingCooldownTime);
        Debug.Log("Reduction cooldown in FootInstincts work");
    }
}
