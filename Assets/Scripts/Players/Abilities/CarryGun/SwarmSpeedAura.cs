using System;
using System.Collections;
using UnityEngine;

public class SwarmSpeedAura : Skill
{
    [SerializeField] private SwarmCapacity _swarmCapacity;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    private void Start()
    {
        if (_swarmCapacity != null)
        {
            _swarmCapacity.CounterChanged += OnCounterChanged;
            OnCounterChanged(_swarmCapacity.CurrentCounter);
        }
    }

    private void OnDestroy()
    {
        if (_swarmCapacity != null) _swarmCapacity.CounterChanged -= OnCounterChanged;
    }

    private void OnCounterChanged(float value) => Disactive = value <= 0;

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

    protected override IEnumerator CastJob()
    {
        if (Hero == null || Hero.CharacterState == null) yield break;
        Hero.CharacterState.CmdAddState(States.SwarmSpeed, 5, _swarmCapacity.CurrentCounter, Hero.gameObject, name);
    }
}
