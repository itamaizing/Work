using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransparentPoisons : Talent
{
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison;

    private float _increaseManaCostValue = 1.3f;
    private bool _isPlayerInvisible;

    public bool IsPlayerInvisible { get => _isPlayerInvisible; }

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void IncreaseManaCost(bool isInvisible)
    {
        _isPlayerInvisible = isInvisible;
        CmdIncreaseManaCost(isInvisible);
    }

    [Command]
    private void CmdIncreaseManaCost(bool isInvisible)
    {
        _poisonBall.Buff.ManaCost.IncreasePercentage(_increaseManaCostValue);
        _spitPoison.Buff.ManaCost.IncreasePercentage(_increaseManaCostValue);

        RpcIncreaseManaCost(isInvisible);
    }

    [ClientRpc]
    private void RpcIncreaseManaCost(bool isInvisible)
    {
        _poisonBall.Buff.ManaCost.IncreasePercentage(_increaseManaCostValue);
        _spitPoison.Buff.ManaCost.IncreasePercentage(_increaseManaCostValue);
    }
}
