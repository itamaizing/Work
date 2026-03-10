using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RechargeGlands : Skill
{
    [SerializeField] private float _durationDestructivePoison = 12f;
    [SerializeField] private CooldownEnergy _cooldownEnergy;
    [SerializeField] private float _cooldownEnergyCost = 6f;

    [SerializeField] private List<GameObject> _rechargeGlands;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast
    {
        get
        {
            if (_cooldownEnergy != null && _cooldownEnergy.CurrentValue < _cooldownEnergyCost) return false;
            if (_activeCoroutines >= MaxCharges) return false;
            if (_chargesGlands >= 1 && _activeCoroutines >= 1) return false;

            return base.IsCanCast;
        }
    }

    private const int ChargesToAdd = 1;
    private const float ChargesToAddTime = 3f;
    private const int MaxCharges = 2;

    private int _chargesGlands;
    private int _activeCoroutines;

    public int ChargesGlands => _chargesGlands;

    protected override IEnumerator CastJob()
    {
        if (_cooldownEnergy != null) _cooldownEnergy.CastCooldownEnergySkill(_cooldownEnergyCost, this);
        StartCoroutine(AddChargeAfterDelay());

        yield return null;
    }

    private IEnumerator AddChargeAfterDelay()
    {
        _activeCoroutines++;

        yield return new WaitForSeconds(ChargesToAddTime);

        foreach (GameObject rechargeGland in _rechargeGlands) rechargeGland.SetActive(true);

        if (_chargesGlands < MaxCharges)
        {
            _chargesGlands += ChargesToAdd;
            _chargesGlands = Mathf.Min(_chargesGlands, MaxCharges);

            CurrentCharge(_chargesGlands);
        }

        _activeCoroutines--;
    }

    public bool TryApplyDestructivePoison(Character target, float chance, Character caster)
    {
        if (_chargesGlands <= 0 || target == null) return false;

        foreach (GameObject rechargeGland in _rechargeGlands) rechargeGland.SetActive(false);

        float rand = UnityEngine.Random.Range(0f, 1f);

        _chargesGlands--;
        CurrentCharge(_chargesGlands);

        if (rand <= chance)
        {
            target.CharacterState.AddStateLogic(States.DestructivePoison, _durationDestructivePoison, 0, Schools.None, caster.gameObject, null);
            return true;
        }

        return false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _activeCoroutines = 0;
    }

    public void UseSwarmCharges(int value)
    {
        _chargesGlands -= value;
        _chargesGlands = Mathf.Max(0, _chargesGlands);

        CurrentCharge(_chargesGlands);
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