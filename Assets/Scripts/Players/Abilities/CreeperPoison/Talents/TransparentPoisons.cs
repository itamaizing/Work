using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransparentPoisons : Talent
{
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison;

    private float _increaseManaCostValue = 1.3f; 

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void IncreaseManaCost()
    {
        CmdIncreaseManaCost();
    }

    [Command]
    private void CmdIncreaseManaCost()
    {
        _poisonBall.Buff.ManaCost.IncreasePercentage(_increaseManaCostValue);
        _spitPoison.Buff.ManaCost.IncreasePercentage(_increaseManaCostValue);
        Debug.Log($"TransparentPoisons / CmdIncreaseManaCost / _poisonBallManaCost = {_poisonBall.Buff.ManaCost.Multiplier}, _spitPoisonManaCost = {_spitPoison.Buff.ManaCost.Multiplier}");

        RpcIncreaseManaCost();
    }

    [ClientRpc]
    private void RpcIncreaseManaCost()
    {
        _poisonBall.Buff.ManaCost.IncreasePercentage(_increaseManaCostValue);
        _spitPoison.Buff.ManaCost.IncreasePercentage(_increaseManaCostValue);
        Debug.Log($"TransparentPoisons / RpcIncreaseManaCost / _poisonBallManaCost = {_poisonBall.ManaCost}, _spitPoisonManaCost = {_spitPoison.ManaCost}");
    }
}
