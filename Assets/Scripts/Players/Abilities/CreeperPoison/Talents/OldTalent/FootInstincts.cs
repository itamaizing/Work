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
        if (_lightningMovement.Cooldown.RemainingTime > 0)
        {
            Debug.Log("FootInstincts / ReductionCooldown / baseRemainingCooldown = " + _lightningMovement.Cooldown.RemainingTime);
            float newRemainingCooldownTime = _lightningMovement.Cooldown.RemainingTime - _reductionCooldownTime;
            Debug.Log("FootInstincts / ReductionCooldown / newRemainingTime = " + newRemainingCooldownTime);
            _lightningMovement.Cooldown.SetReduced(newRemainingCooldownTime, shouldModify: false);
            Debug.Log("FootInstincts / ReductionCooldown / _lightningMovement.RemainingCooldown = " + _lightningMovement.Cooldown.RemainingTime);
        }
    }
}
