using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeelingOfContinuation : Talent
{
    private float _reductionTimeManaRegenMultiplier = 20f;
    private float _remainingManaValue;

    private float _maxMana;
    private float _currentMana;
    private float _originalRegenerationMana;
    private float _baseTimeRegenMana;
    private float _reductionTimeRegenMana;

    private Coroutine _manaRegenerationCoroutine;

    public override void Enter()
    {
        SetActive(true);
        _baseTimeRegenMana = character.TryGetResource(ResourceType.Mana).RegenerationDelay;
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void IncreaseRegenerationMana(Character player, float playerCriticalDamage)
    {
        _originalRegenerationMana = player.TryGetResource(ResourceType.Mana).RegenerationValue;

        _remainingManaValue = playerCriticalDamage;

        if (_manaRegenerationCoroutine != null)
        {
            StopCoroutine(_manaRegenerationCoroutine);
            _manaRegenerationCoroutine = null;
            _reductionTimeRegenMana = _baseTimeRegenMana;
        }

        _reductionTimeRegenMana = _baseTimeRegenMana / _reductionTimeManaRegenMultiplier;
        player.TryGetResource(ResourceType.Mana).RegenerationDelay = _reductionTimeRegenMana;

        _manaRegenerationCoroutine = StartCoroutine(ManaRegenerationJob(player, _remainingManaValue));
    }

    private IEnumerator ManaRegenerationJob(Character player, float remainingManaValue)
    {
        while (remainingManaValue > 0)
        {
            yield return new WaitForSeconds(_reductionTimeRegenMana);

            remainingManaValue -= _originalRegenerationMana;

            _maxMana = player.TryGetResource(ResourceType.Mana).MaxValue;
            _currentMana = player.TryGetResource(ResourceType.Mana).CurrentValue;

            if (_currentMana >= _maxMana)
            {
                _currentMana = _maxMana;
                CancelCoroutine(player, _manaRegenerationCoroutine);
                yield break;
            }
        }
        CancelCoroutine(player, _manaRegenerationCoroutine);
    }

    private void CancelCoroutine(Character player, Coroutine coroutine)
    {
        player.TryGetResource(ResourceType.Mana).RegenerationDelay = _baseTimeRegenMana;

        StopCoroutine(coroutine);
        _manaRegenerationCoroutine = null;
    }
}
