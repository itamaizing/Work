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
    }

    public override void Exit()
    {
        SetActive(false);
    }

}
