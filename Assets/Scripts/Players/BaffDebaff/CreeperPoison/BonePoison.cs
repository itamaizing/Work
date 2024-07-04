using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonePoison : BaseEffect
{
    [SerializeField] private int _baseDamage = 1;
    [SerializeField] private int _stackDuration = 6;
    private float _currentDamage;
    private float _timeBetweenAttack = 1.0f;
    private int _currentStacks;
    private int _maxStacks = 4;

    private DamageType _damageType = DamageType.Physical;
    private AttackRangeType _attackRangeType = AttackRangeType.RangeAttack;

    private Coroutine _lifeTimeStacksCoroutine;
    private Coroutine _damageDealCoroutine;

    public void AddStacks(HealthComponent targetHealth)
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            _currentDamage = _currentStacks * _baseDamage;
            //targetHealth.GetComponent<Character>().CharacterState.AddState(new AbilityFormDebuff(), 6.0f, 0, States.SchoolDebuff);

            Debug.Log("AddStacks _currentStacks == " + _currentStacks);

            _damageDealCoroutine = StartCoroutine(DamageDealCoroutine(targetHealth, _currentStacks));
        }
         _lifeTimeStacksCoroutine = StartCoroutine(LifeTimeStacksCoroutine(targetHealth, _currentStacks));     
    }

    private IEnumerator DamageDealCoroutine(HealthComponent targetHealth, int currentStacks)
    {
        Debug.Log("DamageDealCoroutine _currentStacks == " + currentStacks);
        while (currentStacks > 0)
        {
            targetHealth.TryTakeDamage(_currentDamage, _damageType, _attackRangeType);

            yield return new WaitForSeconds(_timeBetweenAttack);
        }
    }

    private IEnumerator LifeTimeStacksCoroutine(HealthComponent targetHealth, int currentStacks)
    {
        if (currentStacks > 0)
        {
            yield return new WaitForSeconds(_stackDuration);
            currentStacks--;
            Debug.Log("CurrentStacks-- == " + currentStacks);
            StartCoroutine(LifeTimeStacksCoroutine(targetHealth, currentStacks));
        }
        else if (currentStacks == 0) 
        {
            Debug.Log("else if coroutine");
            Destroy(gameObject);
            if (_damageDealCoroutine != null && _lifeTimeStacksCoroutine != null)
            {
                StopCoroutine(DamageDealCoroutine(targetHealth, currentStacks));
                _damageDealCoroutine = null;

                StopCoroutine(LifeTimeStacksCoroutine(targetHealth, currentStacks));
                _lifeTimeStacksCoroutine = null;
            }
        }
    }
}
