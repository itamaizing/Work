using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeelingOfContinuation : Talent
{
    private float _manaRegenerationMultiplier = 2f;
    private float _remainingManaValue;

    private float _maxMana;
    private float _currentMana;
    private float _originalRegenerationMana;
    private float _baseTimeRegenMana;
    private float _reductionTimeRegenMana;

    private Character _player;

    private Coroutine _manaRegenerationCoroutine;

    public override void Enter()
    {
        SetActive(true);
        _player = character;
        _originalRegenerationMana = _player.TryGetResource(ResourceType.Mana).RegenerationValue;
        _baseTimeRegenMana = _player.TryGetResource(ResourceType.Mana).RegenerationDelay;
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void IncreaseRegenerationMana(float playerCriticalDamage)
    {
        _remainingManaValue = playerCriticalDamage;
        _reductionTimeRegenMana = _baseTimeRegenMana / 2;

        _player.TryGetResource(ResourceType.Mana).RegenerationDelay = _reductionTimeRegenMana;

        if (_manaRegenerationCoroutine == null)
        {
            _manaRegenerationCoroutine = StartCoroutine(ManaRegenerationJob(_remainingManaValue));
        }
    }

    private IEnumerator ManaRegenerationJob(float remainingManaValue)
    {
        float time = _reductionTimeRegenMana;
        float boostManaRegen = _originalRegenerationMana * _manaRegenerationMultiplier;
        _player.TryGetResource(ResourceType.Mana).RegenerationValue = boostManaRegen;


        while (time > 0)
        {
            time -= Time.deltaTime;
            if (remainingManaValue > 0)
            {
                remainingManaValue -= boostManaRegen;

                _maxMana = _player.TryGetResource(ResourceType.Mana).MaxValue;
                _currentMana = _player.TryGetResource(ResourceType.Mana).CurrentValue;

                if (_currentMana >= _maxMana)
                {
                    _currentMana = _maxMana;
                    yield break;
                }
                time = _reductionTimeRegenMana;
            }
            yield return null;
        }
        CancelCoroutine(_manaRegenerationCoroutine);
    }

    private void CancelCoroutine(Coroutine coroutine)
    {
        _player.TryGetResource(ResourceType.Mana).RegenerationDelay = _baseTimeRegenMana;
        StopCoroutine(coroutine);
        _manaRegenerationCoroutine = null;
    }

}
