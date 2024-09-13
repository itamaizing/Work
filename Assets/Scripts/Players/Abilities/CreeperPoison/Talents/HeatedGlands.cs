using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatedGlands : Talent
{

    private void Start()
    {
        Enter();
        Debug.Log("HeatedGlands Started");
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
