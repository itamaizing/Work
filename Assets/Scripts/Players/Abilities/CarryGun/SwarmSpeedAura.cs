using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class SwarmSpeedAura : Skill
{
    [SerializeField] private SwarmCapacity _swarmCapacity;
    [SerializeField] private SwarmSpeedAuraStateHandler _auraStateHandler;

    private const float Duration = 5f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => _swarmCapacity != null && _swarmCapacity.CurrentCounter > 0;

    private void OnEnable()
    {
        if (_swarmCapacity != null)
            _swarmCapacity.CounterChanged += OnCounterChanged;
    }

    private void OnDisable()
    {
        if (_swarmCapacity != null)
            _swarmCapacity.CounterChanged -= OnCounterChanged;
    }

    private void OnCounterChanged(float value)
    {
        Disactive = value <= 0;
    }

    protected override IEnumerator CastJob()
    {
        if (_auraStateHandler != null)
        {
            _auraStateHandler.ActivateAura(true, Duration, isAffectOnOwner: true, fromSkill: this);
        }

        yield break;
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