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

    private Coroutine _useCoroutine;
    private Coroutine _decreaseAttackSpeedCoroutine;

    private bool _isUsedLightningStrikes = false;

    public bool IsUsedLightningStrikes => _isUsedLightningStrikes;
    public bool Enabled;
    private new void Start()
    {
        _creeperStrike = _dad.GetComponentInChildren<CreeperStrike>();
    }

    protected override void Cancel()
    {
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
        _isUsedLightningStrikes = true;
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
                _creeperStrike.CurrentCountHit = 0;
                _creeperStrike.DealingDamageFromHits();
                _countStrikes--;
            }
        }
        _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_attackSpeedDeacrease);
        Cancel();
        yield return new WaitForSeconds(4f);
        _isUsedLightningStrikes = false;
    }
}