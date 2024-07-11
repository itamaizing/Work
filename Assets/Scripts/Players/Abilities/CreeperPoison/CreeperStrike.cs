using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreeperStrike : AutoAttackAbility
{
    [SerializeField] private StrokesOfAspiration _strokesOfAspiration; 
    [SerializeField] protected Character _dad;

    private BonePoison _bonePoisonDebuff;
    private GameObject _currentTarget;

    private float _currentDamage;
    private float _currentRadius;
    private float _multiplyCritDamage = 1.5f;
    private float _timeBetweenAttack = 0.1f;

    private Coroutine _useAbilityCoroutine;

    protected float ThisRadius => _currentRadius;
    public GameObject CurrentTarget => _currentTarget;

    protected override void Start()
    {
        base.Start();
        _strokesOfAspiration = _dad.GetComponentInChildren<StrokesOfAspiration>();

        Buff.AttackSpeed.IncreasePercentage(_timeBetweenAttack);
    }

    protected override void Cancel()
    {
        if (_useAbilityCoroutine != null)
            StopCoroutine(UseAbilityCoroutine());
    }

    protected override void CastAction()
    {
        _currentDamage = Random.Range(7.0f, 11.0f); 
        _strokesOfAspiration.StartJobTalentCoroutine = StartCoroutine(_strokesOfAspiration.StartJobTalent());
        _useAbilityCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    private IEnumerator UseAbilityCoroutine()
    {
        _currentTarget = Target.gameObject;
        DealingDamageFromHits(_currentDamage);
        yield return null;
    }

    public void DealingDamageFromHits(float currentDamage)
    {
        float chanceOfCriticalStrike = 0.5f;
        float numbersForChanceOfCriticalStrike = Random.Range(0.0f, 1.0f);

        if (numbersForChanceOfCriticalStrike <= chanceOfCriticalStrike)
        {
            CmdCriticalDamage(CurrentTarget, currentDamage);           
        }
        else
        {
            CmdApplyDamage(CurrentTarget, currentDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
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
