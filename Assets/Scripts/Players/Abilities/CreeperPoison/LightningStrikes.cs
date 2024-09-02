using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LightningStrikes : AutoAttackSkill
{
    [SerializeField] private Character _player;
    [SerializeField] private CreeperStrike _creeperStrike;

    private int _countStrikes = 2;
    private float _attackSpeedDeacrease = 0.1f;
    private bool _isUsedLightningStrikes = false;

    private Coroutine _useCoroutine;
    private Coroutine _decreaseAttackSpeedCoroutine;

    public bool IsUsedLightningStrikes => _isUsedLightningStrikes;
    public bool Enabled;

    protected override void ClearData()
    {
        base.ClearData();
        Debug.Log("LightningStrikes / ClearData");

        if (_useCoroutine != null)
        {
            StopCoroutine(UseAbilityCoroutine());
            _useCoroutine = null;
        }

        if (_decreaseAttackSpeedCoroutine != null)
        {
            StopCoroutine(DecreaseAttackSpeed());
            _decreaseAttackSpeedCoroutine = null;
        }
    }

    protected override void CastAction()
    {
        Debug.Log("LightningStrikes / CastAction");
        _useCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    private IEnumerator UseAbilityCoroutine()
    {
        Debug.Log("LightningStrikes / UseAbilityCoroutine");
        _isUsedLightningStrikes = true;
        _countStrikes = 2;
        _decreaseAttackSpeedCoroutine = StartCoroutine(DecreaseAttackSpeed());
        yield return null;
    }

    private IEnumerator DecreaseAttackSpeed()
    {
        Debug.Log("LightningStrikes / DecreaseAttackSpeed");
        Debug.Log($"LightningStrikes / DecreaseAttackSpeed / CreeperStrike.CurrentTarget = {_target}");
        if (_target != null)
        {
            Debug.Log("LightningStrikes / DecreaseAttackSpeed / if (CreeperStrike.CurrentTarget != null)");
            _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_attackSpeedDeacrease);
            Debug.Log($"LightningStrikes / DecreaseAttackSpeed / _creeperStrike.Buff.AttackSpeed.Increase = {_creeperStrike.Buff.AttackSpeed.Multiplier}");

            while (_countStrikes > 0)
            {
                Debug.Log($"LightningStrikes / DecreaseAttackSpeed / while (countStrikes = {_countStrikes})");
                _creeperStrike.DealingDamageFromHits(_target);
                _countStrikes--;
                _creeperStrike.CurrentCountHit = 0;
            }
            Debug.Log("LightningStrikes / DecreaseAttackSpeed / after while");
            _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_attackSpeedDeacrease);
            Debug.Log($"LightningStrikes / DecreaseAttackSpeed / _creeperStrike.Buff.AttackSpeed.Reduction = {_creeperStrike.Buff.AttackSpeed.Multiplier}");
            yield return new WaitForSeconds(4f);
            _isUsedLightningStrikes = false;
        }
    }
}