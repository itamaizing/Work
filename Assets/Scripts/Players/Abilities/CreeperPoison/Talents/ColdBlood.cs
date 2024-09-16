using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColdBlood : Talent
{
    private void Start()
    {
        Debug.Log("ColdBloodTalent Started!");
        //Enter();
    }

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }
}
