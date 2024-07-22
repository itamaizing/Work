using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreeperStrike : AutoAttackAbility
{
    [SerializeField] protected Character _dad;

    [Header("Talents")]
    [SerializeField] private StrokesOfAspiration _strokesOfAspiration;
    [SerializeField] private LightweightSlap _lightweightSlap;

    private BonePoison _bonePoisonDebuff;

    private GameObject _currentTarget;
    private GameObject _lastTarget;

    private int _currentCountHit = 0;

    private float _currentDamage;
    private float _multiplyCritDamage = 1.5f;

    private bool _isTwoHit = false;

    private Coroutine _useAbilityCoroutine;

    public GameObject CurrentTarget => _currentTarget;
    public int CurrentCountHit { get => _currentCountHit; set => _currentCountHit = value; }
    public bool IsTwoHit { get => _isTwoHit; set => _isTwoHit = value; }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Cancel()
    {
        if (_useAbilityCoroutine != null)
            StopCoroutine(UseAbilityCoroutine());
    }

    protected override void CastAction()
    {
        _useAbilityCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    private IEnumerator UseAbilityCoroutine()
    {
        _currentTarget = Target.gameObject;
        DealingDamageFromHits();
        yield return null;
    }

    public void DealingDamageFromHits()
    {
        _currentCountHit++;
        Debug.Log("Current hit == " + _currentCountHit);
        _currentDamage = Random.Range(7.0f, 11.0f); 

        float chanceOfCriticalStrike = 0.5f;
        float numbersForChanceOfCriticalStrike = Random.Range(0.0f, 1.0f);

        if (_strokesOfAspiration.isActive && _currentCountHit == 2)
        {
            if (_lastTarget == _currentTarget)
            {
                _strokesOfAspiration.UseTalentStrokesOfAspiration();
            }
            else
            {
                _lastTarget = _currentTarget;
            }
        }

        if (_currentCountHit == 2)
        {
            if (!_isTwoHit)
            {
                _isTwoHit = true;
            }
            _currentCountHit = 0;
        }

        if (numbersForChanceOfCriticalStrike <= chanceOfCriticalStrike)
        {
            CmdCriticalDamage(CurrentTarget, _currentDamage);           
        }
        else
        {
            CmdApplyDamage(CurrentTarget, _currentDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
        }

        Cancel();
    }

    private float CalculateCriticalDamage(float baseDamage)
    {
        float criticalDamage = baseDamage;
        float multiplyDamage = _multiplyCritDamage;

        if (_bonePoisonDebuff != null)
        {
            for (int i = 1; i < _bonePoisonDebuff.CurrentStacks; i++)
            {
                multiplyDamage += 0.5f;
            }
        }
        return criticalDamage *= multiplyDamage;
    }

    [Command]
    private void CmdCriticalDamage(GameObject currentTarget, float criticalDamage)
    {
        _bonePoisonDebuff = currentTarget.GetComponentInChildren<BonePoison>();
        if (_bonePoisonDebuff != null)
        {
            criticalDamage = CalculateCriticalDamage(criticalDamage);
        }
        currentTarget.GetComponent<HealthComponent>().TryTakeDamage(criticalDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
    }
}
