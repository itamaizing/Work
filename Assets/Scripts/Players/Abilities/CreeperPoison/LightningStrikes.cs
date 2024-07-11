using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LightningStrikes : AutoAttackAbility
{
    [SerializeField] private Character _dad;
    private CreeperStrike _creeperStrike;

    private int _countStrikes = 2;

    private float _attackSpeedDeacrease = 0.1f;
    private float _attackSpeedStrikes;
    private float _currentDamage;

    private Coroutine _useCoroutine;
    private Coroutine _decreaseAttackSpeedCoroutine;

    private new void Start()
    {
        _creeperStrike = _dad.GetComponentInChildren<CreeperStrike>();
    }

    protected override void Cancel()
    {
        _attackSpeedStrikes = 1;
        _countStrikes = 2;

        if (_useCoroutine != null)
            StopCoroutine(UseAbilityCoroutine());

        if (_decreaseAttackSpeedCoroutine != null)
            StopCoroutine(DecreaseAttackSpeed());
    }

    protected override void CastAction()
    {
        _useCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    public IEnumerator UseAbilityCoroutine()
    {
        _decreaseAttackSpeedCoroutine = StartCoroutine(DecreaseAttackSpeed());
        yield return null;
    }

    private IEnumerator DecreaseAttackSpeed()
    {
        if (_creeperStrike.CurrentTarget != null)
        {
            _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_attackSpeedDeacrease);

            while (_countStrikes > 0)
            {
                _creeperStrike.DealingDamageFromHits(_currentDamage);
                _countStrikes--;
            }
        }
        _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_attackSpeedDeacrease);
        Cancel();
        yield return null;
    }
}