using System;
using System.Collections;
using UnityEngine;

public class SummoningSwarm : Skill
{
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    private const int ChargesToAdd = 3;

    private Coroutine _buffRoutine;

    protected override IEnumerator CastJob()
    {
        if (_buffRoutine != null)
            StopCoroutine(_buffRoutine);

        _buffRoutine = StartCoroutine(ChargesBuffRoutine());

        yield break;
    }

    private IEnumerator ChargesBuffRoutine()
    {
        // добавляем текущие заряды
        Chargers += ChargesToAdd;

        yield return new WaitForSeconds(CooldownTime);

        // убираем заряды
        Chargers = Mathf.Max(0, Chargers - ChargesToAdd);

        _buffRoutine = null;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.GetTargets().Contains(Hero)) return;

        targetInfo.AddTarget(Hero);
    }

    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);

        callbackDataSaved(targetInfo);
        yield break;
    }
}