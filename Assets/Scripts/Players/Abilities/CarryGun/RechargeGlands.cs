using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RechargeGlands : Skill
{
    [SerializeField] private float durationDestructivePoison = 12f;
    [SerializeField] private List<GameObject> _rechargeGlands;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    private const int ChargesToAdd = 1;
    private const float ChargesToAddTime = 3f;
    private const int MaxCharges = 2;

    private int _chargesGlands;
    private int _activeCoroutines;

    public int ChargesGlands => _chargesGlands;

    protected override IEnumerator CastJob()
    {
        if (_chargesGlands >= 1 && _activeCoroutines >= 1) yield break;
        if (_activeCoroutines >= MaxCharges) yield break;

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

        if (rand <= chance)
        {
            _chargesGlands--;
            CurrentCharge(_chargesGlands);

            target.CharacterState.AddState(States.DestructivePoison, durationDestructivePoison, 0, caster.gameObject, null);

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