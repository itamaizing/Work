using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BindingPoison : Talent
{
    private void Start()
    {
        //Enter();
    }
    public override void Enter()
    {
        SetActive(true);
        //CmdSetActive();
        Debug.Log($"BindingPoisonEnter IsActive = {IsActive}");
    }

    public override void Exit()
    {
        SetActive(false);
        Debug.Log($"BindingPoisonExit IsActive = {IsActive}");
    }

    //[Command]
    //private void CmdSetActive()
    //{
    //    SetActive(true);
    //}
}
