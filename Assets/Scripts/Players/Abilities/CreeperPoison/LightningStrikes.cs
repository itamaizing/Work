using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LightningStrikes : AutoAttackSkill
{
    public bool Enabled;
    public bool IsCanDamageDeal = false;

    [SerializeField] private LightningMovement _lightningMovement;
    [SerializeField] private Character _player;
    [SerializeField] private CreeperStrike _creeperStrike;

    private Character _currentTarget;

    private int _countStrikes = 2;
    private float _attackSpeedDeacrease = 0.1f;
    private bool _isUsedLightningStrikes = false;
    private Coroutine _useCoroutine;
    //private Coroutine _decreaseAttackSpeedCoroutine;

    public bool IsUsedLightningStrikes => _isUsedLightningStrikes;

    protected override void ClearData()
    {
        base.ClearData();
        Debug.Log("LightningStrikes / ClearData");

        if (_useCoroutine != null)
        {
            StopCoroutine(UseAbilityCoroutine());
            _useCoroutine = null;
        }

        //if (_decreaseAttackSpeedCoroutine != null)
        //{
        //    StopCoroutine(DecreaseAttackSpeed());
        //    _decreaseAttackSpeedCoroutine = null;
        //}

        if (_isUsedLightningStrikes)
        {
            Invoke("ResetUsedLightningStrikes", 4f);
        }
    }

    protected override IEnumerator PrepareJob()
    {
        if (_lightningMovement.IsInMovement)
        {
            Debug.Log("LightningStrikes / PrepareJob / if");
            IsCanDamageDeal = true;
            yield break;
        }
        else
        {
            Debug.Log("LightningStrikes / PrepareJob / else");
            base.PrepareJob();
        }
    }

    protected override void CastAction()
    {
        _currentTarget = _target;
        Debug.Log("LightningStrikes / CastAction / Not LightningMovement");
        _useCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    public void UseLightningStrikes(Character target)
    {
        DecreaseAttackSpeed(target);
    }

    private void ResetUsedLightningStrikes()
    {
        _isUsedLightningStrikes = false;
        IsCanDamageDeal = false;
    }

    private IEnumerator UseAbilityCoroutine()
    {
        Debug.Log("LightningStrikes / UseAbilityCoroutine");
        _isUsedLightningStrikes = true;
        DecreaseAttackSpeed(_target);
        yield return null;
    }

    private void DecreaseAttackSpeed(Character target)
    {
        Debug.Log("LightningStrikes / DecreaseAttackSpeed");
        if (target != null)
        {
            _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_attackSpeedDeacrease);
            Debug.Log($"LightningStrikes / DecreaseAttackSpeed / _creeperStrike.Buff.AttackSpeed.Increase = {_creeperStrike.Buff.AttackSpeed.Multiplier}");

            for (int i = 0; i < _countStrikes; i++)
            {
                Debug.Log("LightningStrikes / DecreaseAttackSpeed / cycle For");
                _creeperStrike.DealingDamageFromHits(target);
                _creeperStrike.CurrentCountHit = 0;
            }

            _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_attackSpeedDeacrease);
            Debug.Log($"LightningStrikes / DecreaseAttackSpeed / _creeperStrike.Buff.AttackSpeed.Reduction = {_creeperStrike.Buff.AttackSpeed.Multiplier}");
        }
    }
}