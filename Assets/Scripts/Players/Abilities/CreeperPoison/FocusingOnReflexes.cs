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

    private readonly AttributeModifier _evadeMeleeModifier = new(EvadeMeleeBonus, ModifierType.Flat);
    private readonly AttributeModifier _evadeRangeModifier = new(EvadeRangeBonus, ModifierType.Flat);

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    private void OnDestroy()
    {
        if (Hero != null && Hero.Health != null)
        {
            Hero.Health.Evaded -= OnEvaded;
            Hero.Health.DamageTaken -= OnDamageTaken;
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
        SubscribeOnDamage();
        yield break;
    }

    private void SubscribeOnDamage()
    {
        if (Hero != null && Hero.Health != null)
        {
            Hero.Health.DamageTaken += OnDamageTaken;
        }
    }

    [Command]
    private void CmdApplyReflexesBuff()
    {
        if (_isBuffActive)
        {
            RemoveBuffLogic();
        }

        var attrs = Hero.AttributeSystem;
        _evadeMeleeModifier.Source = this;
        _evadeRangeModifier.Source = this;
        attrs[CharacterAttributeName.EvasionPhysicalMelee].AddModifier(_evadeMeleeModifier);
        attrs[CharacterAttributeName.EvasionPhysicalRange].AddModifier(_evadeRangeModifier);

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

    private void OnEvaded(Skill skill)
    {
        RemoveBuffLogic();
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (damage.Type == DamageType.DOTMag || damage.Type == DamageType.DOTPhys) return;
        CmdRemoveBuffLogic();
        Hero.Health.DamageTaken -= OnDamageTaken;
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

        var attrs = Hero.AttributeSystem;
        attrs[CharacterAttributeName.EvasionPhysicalMelee].RemoveModifier(_evadeMeleeModifier);
        attrs[CharacterAttributeName.EvasionPhysicalRange].RemoveModifier(_evadeRangeModifier);

        Hero.CharacterState.RemoveState(States.FocusingOnReflexesState);
    }

    [Command]
    private void CmdRemoveBuffLogic()
    {
        if (_isBuffActive)
        {
            RemoveBuffLogic();
        }
    }
}