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

    private Character _player;

    private Coroutine _manaRegenerationCoroutine;

    private void Start()
    {
        //Enter();
    }

    public override void Enter()
    {
        SetActive(true);
        _player = Character;
        _originalRegenerationMana = _player.TryGetResource(ResourceType.Mana).RegenerationValue;
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void IncreaseRegenerationMana(float playerCriticalDamage)
    {
        _remainingManaValue = playerCriticalDamage;
        if (_manaRegenerationCoroutine == null)
        {
            _manaRegenerationCoroutine = StartCoroutine(ManaRegenerationJob(_remainingManaValue));
        }
    }

    private IEnumerator ManaRegenerationJob(float remainingManaValue)
    {
        float boostManaRegen = _originalRegenerationMana * _manaRegenerationMultiplier;
        _player.TryGetResource(ResourceType.Mana).RegenerationValue = boostManaRegen;

        while (remainingManaValue > 0)
        {
            remainingManaValue -= boostManaRegen;
            Debug.Log("RemainingManaValue = " + remainingManaValue);
            _maxMana = _player.TryGetResource(ResourceType.Mana).MaxValue;
            _currentMana = _player.TryGetResource(ResourceType.Mana).CurrentValue;
            Debug.Log("_maxMana = " + _maxMana);
            Debug.Log("_currentMana = " + _currentMana);

            if (_currentMana >= _maxMana)
            {
                Debug.Log("If (currentMana > maxMana)");
                _currentMana = _maxMana;
                yield break;
            }

            yield return null;
        }

        CancelCoroutine(_manaRegenerationCoroutine);
    }

    private void CancelCoroutine(Coroutine coroutine)
    {
        StopCoroutine(coroutine);
        _manaRegenerationCoroutine = null;
    }

}
