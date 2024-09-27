using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcceleratedSlap : Talent
{
    [SerializeField] private PoisonSlap _poisonSlap;
    private int _reductionCooldown = 2;

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void ReductionCooldown()
    {
        _poisonSlap.Buff.Cooldown.ReductionPercentage(_reductionCooldown);
    }

    public void ResetCooldown()
    {
        _poisonSlap.Buff.Cooldown.IncreasePercentage(_reductionCooldown);
    }
}
