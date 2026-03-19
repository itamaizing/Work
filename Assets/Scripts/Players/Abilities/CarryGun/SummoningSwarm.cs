using System;
using System.Collections;
using UnityEngine;

public class SummoningSwarm : Skill
{
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    private const int ChargesToAdd = 3;
    private const float ChargesLifetime = 6f;

    private Coroutine _removeChargesCoroutine;

    private int _chargesSwarm;

    public int ChargesSwarm => _chargesSwarm;

    private void OnEnable()
    {
        Hero.Reset += ResettSwarmCharges;
    }

    private void OnDisable()
    {
        if (_removeChargesCoroutine != null)
        {
            StopCoroutine(_removeChargesCoroutine);
            _removeChargesCoroutine = null;
        }

        Hero.Reset -= ResettSwarmCharges;
    }

    protected override IEnumerator CastJob()
    {
        SetSwarmCharges(ChargesToAdd);

        if (_removeChargesCoroutine != null) StopCoroutine(_removeChargesCoroutine);
        _removeChargesCoroutine = StartCoroutine(RemoveChargesAfterTime());

        yield return null;
    }

    private IEnumerator RemoveChargesAfterTime()
    {
        yield return new WaitForSeconds(ChargesLifetime);
        SetSwarmCharges(0);
    }

    private void ResettSwarmCharges() => Charges.CurrentCharges = _chargesSwarm;

    private void SetSwarmCharges(int value)
    {
        _chargesSwarm = value;
        Charges.CurrentCharges = _chargesSwarm;
    }

    public void UseSwarmCharges(int value)
    {
        _chargesSwarm -= value;
        Charges.CurrentCharges = _chargesSwarm;
    }

    protected override void ClearData()
    {

    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.GetTargets().Contains(Hero)) return;

        targetInfo.AddTarget(Hero);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);

        callbackDataSaved(targetInfo);
        yield break;
    }
}