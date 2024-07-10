using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LightningStrikes : AutoAttackAbility
{
    [SerializeField] private Character _dad;
    private CreeperStrike creeperStrike;

    private int _countStrikes = 2;

    private float _attackSpeedDeacrease = 10f;
    private float _attackSpeedStrikes;

    private Coroutine _useCoroutine;
    private Coroutine _decreaseAttackSpeedCoroutine;

    private new void Start()
    {
        creeperStrike = _dad.GetComponentInChildren<CreeperStrike>();
    }

    protected override void Cancel()
    {
        creeperStrike.ResetAttackSpeed();
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
        if (creeperStrike.CurrentTarget != null)
        {
            _attackSpeedStrikes = creeperStrike.CurrentAttackSpeed / _attackSpeedDeacrease;
            creeperStrike.ModifyAttackSpeed(_attackSpeedStrikes);

            while (_countStrikes > 0)
            {
                StartCoroutine(creeperStrike.UseAbilityCoroutine());
                _countStrikes--;
            }
        }
        Cancel();
        yield return null;
    }
}