using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuationAmbush : Talent
{
    private bool _isCanApplyInvisible;
    public bool IsCanApplyInvisible { get => _isCanApplyInvisible; set => _isCanApplyInvisible = value; }

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

    public void CanApplyInvisible(bool isCanApplyInvisible)
    {
        Debug.Log("CanApplyInvisible");
        _isCanApplyInvisible = isCanApplyInvisible;
        RpcApplyInvisible(_isCanApplyInvisible);
        Invoke("CanNotApplyInvisible", 1.0f);
    }

    private void CanNotApplyInvisible()
    {
        Debug.Log("CanNotApplyInvisible");
        _isCanApplyInvisible = false;
        Debug.Log($"CanNotApplyInvisible / isCanApply = {_isCanApplyInvisible}");
        RpcCanNotApplyInvisible(false);
    }

    [Command]
    private void CmdSetActive(bool isActive)
    {
        SetActive(isActive);
    }

    [ClientRpc]
    private void RpcApplyInvisible(bool isCanApplyInvisible)
    {
        Debug.Log("RpcCanApplyInvisible");
        _isCanApplyInvisible = isCanApplyInvisible;
    }

    [ClientRpc]
    private void RpcCanNotApplyInvisible(bool isCanApplyInvisible)
    {
        Debug.Log("RpcCanNotApplyInvisible");
        _isCanApplyInvisible = isCanApplyInvisible; 
        Debug.Log($"RpcCanNotApplyInvisible / isCanApply = {_isCanApplyInvisible}");
    }
}
