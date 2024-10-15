using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BindingPoison : Talent
{
    public override void Enter()
    {
        SetActive(true);
        //CmdSetActive();
        Debug.Log($"BindingPoisonEnter IsActive = {Data.IsOpen}");
    }

    public override void Exit()
    {
        SetActive(false);
        Debug.Log($"BindingPoisonExit IsActive = {Data.IsOpen}");
    }

    //[Command]
    //private void CmdSetActive()
    //{
    //    SetActive(true);
    //}
}
