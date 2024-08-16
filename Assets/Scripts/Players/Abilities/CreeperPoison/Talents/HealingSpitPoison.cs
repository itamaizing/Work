using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingSpitPoison : Talent
{
    public bool IsCanTargetHimself;

    public override void Enter()
    {
        SetActive(true);
        IsCanTargetHimself = true;
    }

    public override void Exit()
    {
        SetActive(false);
        IsCanTargetHimself = false;
    }
}
