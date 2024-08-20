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
    
    private Coroutine _startCoroutine;

    public float StartTimeToApplyInvisible;

    public bool IsInvisible = false;

    protected override bool IsCanCast => true;

    protected override IEnumerator PrepareJob()
    {
        throw new System.NotImplementedException();
    }

    protected override IEnumerator CastJob()
    {
        if (_startCoroutine != null)
        {
            ClearData();
            yield break;
        }

        if (_startCoroutine == null)
        {
            EnteringInvisibleState();
        }
        yield return null;
    }

    protected override void ClearData()
    {
        CmdRemoveInvisible();

        if (_releaseFromSecrecy.IsActive)
        {
            _releaseFromSecrecy.ApplyBuff();
        }
    }

    public void EnteringInvisibleState()
    {
        TryPayCost();
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
        IsInvisible = true;
        RpcApplyInvis();

        StartTimeToApplyInvisible = _baseTimeToApplyInvisible;
        CreeperInvisibleState.StartTimeWithoutDamage = StartTimeToApplyInvisible;

        _player.CharacterState.CmdAddState(States.CreeperInvisible, 0, 0);
    }

    [Command]
    private void CmdApplyInvisibleWithTalent()
    {
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
        IsInvisible = false;
        RpcRemoveInvisible();
    }

    #endregion

    #region RpcMethods

    [ClientRpc]
    private void RpcApplyInvis()
    {
        IsInvisible = true;
        StartTimeToApplyInvisible = _baseTimeToApplyInvisible;
        CreeperInvisibleState.StartTimeWithoutDamage = StartTimeToApplyInvisible;
    }

    [ClientRpc]
    private void RpcApplyInvisibleWithTalent()
    {
        IsInvisible = true;

        StartTimeToApplyInvisible = _baseTimeToApplyInvisibleWithTalent;
        CreeperInvisibleState.StartTimeWithoutDamage = StartTimeToApplyInvisible;
        CreeperInvisibleState.IsDamagedPlayer = false;
        CreeperInvisibleState.IsPlayerSeen = false;
    }

    [ClientRpc]
    private void RpcRemoveInvisible()
    {
        IsInvisible = false;
        if (_firstStrike.IsActive && !_firstStrike.IsCanIncreaseCrit)
        {
            _firstStrike.SetBoolTrue();
        }
    }

    #endregion
}
