using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class FocusingOnReflexes : Skill
{
    [SerializeField] private float duration = 1f;
    private const float EvadeMeleeBonus = 60f;
    private const float EvadeRangeBonus = 100f;

    private Coroutine _buffTimerCoroutine;
    private bool _isBuffActive;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    private void OnDestroy()
    {
        if (Hero != null && Hero.Health != null)
        {
            Hero.Health.Evaded -= OnEvaded;
        }
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

    protected override IEnumerator CastJob()
    {
        CmdApplyReflexesBuff();
        yield break;
    }

    [Command]
    private void CmdApplyReflexesBuff()
    {
        if (_isBuffActive)
        {
            RemoveBuffLogic();
        }

        Hero.Health.EvadeMeleeDamage += EvadeMeleeBonus;
        Hero.Health.EvadeRangeDamage += EvadeRangeBonus;

        Hero.Health.Evaded += OnEvaded;
        _isBuffActive = true;

        Hero.CharacterState.AddState(States.FocusingOnReflexesState, duration, 0f, Hero.gameObject, name);

        if (_buffTimerCoroutine != null) StopCoroutine(_buffTimerCoroutine);
        _buffTimerCoroutine = StartCoroutine(BuffTimer());
    }

    private IEnumerator BuffTimer()
    {
        yield return new WaitForSeconds(duration);
        
        if (_isBuffActive)
        {
            RemoveBuffLogic();
        }
    }

    private void OnEvaded()
    {
        if (_isBuffActive)
        {
            RemoveBuffLogic();
        }
    }

    private void RemoveBuffLogic()
    {
        _isBuffActive = false;

        if (_buffTimerCoroutine != null)
        {
            StopCoroutine(_buffTimerCoroutine);
            _buffTimerCoroutine = null;
        }

        Hero.Health.Evaded -= OnEvaded;

        Hero.Health.EvadeMeleeDamage -= EvadeMeleeBonus;
        Hero.Health.EvadeRangeDamage -= EvadeRangeBonus;
        
        Hero.CharacterState.RemoveState(States.FocusingOnReflexesState);
    }
}