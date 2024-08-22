using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperInvisible : Skill
{
    [Header("Talents")]
    [SerializeField] private ReleaseFromSecrecy _releaseFromSecrecy;
    [SerializeField] private DesireToHide _desireToHide;
    [SerializeField] private FirstStrike _firstStrike;

    [Header("Ability Properties")]
    [SerializeField] private Character _player;

    private float _baseTimeToApplyInvisible = 6.0f;
    private float _baseTimeToApplyInvisibleWithTalent = 0.0f;

    public float StartTimeToApplyInvisible;

    public bool IsInvisible = false;

    protected override bool IsCanCast => true;

    protected override void ClearData()
    {
        Debug.Log("CreeperInvisible / ClearData");
        //CmdRemoveInvisible();
    }

    protected override IEnumerator PrepareJob()
    {
        Debug.Log("CreeperInvisible / PrepareJob");
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        Debug.Log("CreeperInvisible / CastJob");
        if (IsInvisible)
        {
            Debug.Log($"CreeperInvisible / CastJob / if (IsInvisible = {IsInvisible})");
            CmdRemoveInvisible();
            yield break;
        }
        else
        {
            Debug.Log($"CreeperInvisible / CastJob / else (IsInvisible = {IsInvisible})");
            EnteringInvisibleState();
        }
        Debug.Log("CreeperInvisible / CastJob / after cycle");
        yield return null;
    }

    public void EnteringInvisibleState()
    {
        Debug.Log("CreeperInvisible / EnteringInvisibleState");
        if (_desireToHide.IsActive && _desireToHide.IsCanApply)
        {
            CmdApplyInvisibleWithTalent();
        }
        else
        {
            CmdApplyInvis();
        }
    }

    #region CommandMethods

    [Command]
    private void CmdApplyInvis()
    {
        Debug.Log("CreeperInvisible / CmdApplyInvis");
        IsInvisible = true;
        Debug.Log($"CreeperInvisible / CmdApplyInvis / IsInvisible = {IsInvisible}");
        RpcApplyInvis();

        StartTimeToApplyInvisible = _baseTimeToApplyInvisible;
        CreeperInvisibleState.StartTimeWithoutDamage = StartTimeToApplyInvisible;
        Debug.Log($"CreeperInvisible / CmdApplyInvis / CreeperInvisibleState.StartTimeWithoutDamage = {CreeperInvisibleState.StartTimeWithoutDamage}");

        _player.CharacterState.CmdAddState(States.CreeperInvisible, 0, 0);
    }

    [Command]
    private void CmdApplyInvisibleWithTalent()
    {
        Debug.Log("CreeperInvisible / CmdApplyInvisibleWithTalent");
        IsInvisible = true;
        RpcApplyInvisibleWithTalent();

        StartTimeToApplyInvisible = _baseTimeToApplyInvisibleWithTalent;
        CreeperInvisibleState.StartTimeWithoutDamage = StartTimeToApplyInvisible;
        CreeperInvisibleState.IsDamagedPlayer = false;
        CreeperInvisibleState.IsPlayerSeen = false;

        _player.CharacterState.CmdAddState(States.CreeperInvisible, 0, 0);
    }

    [Command]
    private void CmdRemoveInvisible()
    {
        Debug.Log("CreeperInvisible / CmdRemoveInvisible");
        IsInvisible = false;
        if (_releaseFromSecrecy.IsActive)
        {
            _releaseFromSecrecy.ApplyBuff();
        }
        Debug.Log($"CreeperInvisible / CmdRemoveInvisible / IsInvisible = {IsInvisible}");
        RpcRemoveInvisible();
    }

    #endregion

    #region RpcMethods

    [ClientRpc]
    private void RpcApplyInvis()
    {
        Debug.Log("CreeperInvisible / RpcApplyInvis");
        IsInvisible = true;
        Debug.Log($"CreeperInvisible / RpcApplyInvis / IsInvisible = {IsInvisible}");
        StartTimeToApplyInvisible = _baseTimeToApplyInvisible;
        CreeperInvisibleState.StartTimeWithoutDamage = StartTimeToApplyInvisible;
        Debug.Log($"CreeperInvisible / RpcApplyInvis / CreeperInvisibleState.StartTimeWithoutDamage = {CreeperInvisibleState.StartTimeWithoutDamage}");
    }

    [ClientRpc]
    private void RpcApplyInvisibleWithTalent()
    {
        Debug.Log("CreeperInvisible / RpcApplyInvisibleWithTalent");
        IsInvisible = true;

        StartTimeToApplyInvisible = _baseTimeToApplyInvisibleWithTalent;
        CreeperInvisibleState.StartTimeWithoutDamage = StartTimeToApplyInvisible;
        CreeperInvisibleState.IsDamagedPlayer = false;
        CreeperInvisibleState.IsPlayerSeen = false;
    }

    [ClientRpc]
    private void RpcRemoveInvisible()
    {
        Debug.Log("CreeperInvisible / RpcRemoveInvisible");
        IsInvisible = false;
        if (_releaseFromSecrecy.IsActive)
        {
            _releaseFromSecrecy.ApplyBuff();
        }
        Debug.Log($"CreeperInvisible / RpcRemoveInvisible / IsInvisible = {IsInvisible}");
        if (_firstStrike.IsActive && !_firstStrike.IsCanIncreaseCrit)
        {
            _firstStrike.SetBoolTrue();
        }
    }

    #endregion
}
