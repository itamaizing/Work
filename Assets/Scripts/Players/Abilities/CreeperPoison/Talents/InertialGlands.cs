using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InertialGlands : Talent
{
    public override void Enter()
    {
        SetActive(true);
        CmdSetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
        CmdSetActive(false);
    }

    [Command]
    private void CmdSetActive(bool active)
    {
        SetActive(active);
    }

}
