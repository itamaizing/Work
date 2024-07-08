using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperPosionSphere : AbilityBase
{
    protected override KeyCode ActivationKey => throw new System.NotImplementedException();

    public delegate void ThirdAbilityHandler(float value);

    public event ThirdAbilityHandler ThirdAbilityEvent;

    public override void ChangeBoolAndValues()
    {
        throw new System.NotImplementedException();
    }

    public override void OnLeftDoubleClick()
    {
        throw new System.NotImplementedException();
    }

    public override void OnRightDoubleClick()
    {
        throw new System.NotImplementedException();
    }

    public override void HandleDealDamageOrHeal()
    {
        throw new System.NotImplementedException();
    }
}
