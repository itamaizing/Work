using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ShotIntoSky : Skill
{
    [SerializeField] private SkillRenderer skillRenderer;
    [SerializeField] private float minDamage;
    [SerializeField] private float maxDamage;
    [SerializeField] private float delay;
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    [SerializeField, Range(0f, 100f)] private float criticalChance = 30f;
    [SerializeField, Range(0f, 100f)] private float stunChance = 30f;
    private const float CriticalMultiplier = 2.4f;

    protected override bool IsCanCast => true;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private void Start()
    {
       Damage = Random.Range(minDamage, maxDamage + 1);
    }

    protected override IEnumerator PrepareJob()
    {
        while (float.IsPositiveInfinity(_targetPoint.x))
        {
            if (GetMouseButton && IsCanCast)
            {
                Vector3 clickedPoint = GetMousePoint();

                if (IsPointInRadius(Radius, clickedPoint))
                {
                    _targetPoint = clickedPoint;
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        DrawDamageZone(_targetPoint);

        yield return new WaitForSeconds(delay);

        ApplyDamageToEnemiesInZone();
        StopDamageZone();
    }

    private void ApplyDamageToEnemiesInZone()
    {
        CircleArea damageZone = skillRenderer.TempDamageZone;

        if (damageZone != null)
        {
            Collider[] hitColliders = Physics.OverlapSphere(damageZone.transform.position, Area, TargetsLayers);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.TryGetComponent<HeroComponent>(out HeroComponent enemy))
                {
                    float finalDamage = CalculateDamage(Damage);
                    ApplyDamage(finalDamage, DamageType.Magical, enemy);

                    if (Random.Range(0f, 100f) <= stunChance)
                    {
                        var targetState = enemy.CharacterState;
                        if (targetState != null)
                        {
                            CmdAddState(targetState);
                        }
                    }
                }

                else if  (hitCollider.TryGetComponent<Object>(out Object target))
                {
                    float finalDamage = CalculateDamage(Damage);
                    ApplyDamage(finalDamage, DamageType.Magical, target);
                }
            }
        }
    }

    [Command]
    private void CmdAddState(CharacterState targetState)
    {
        targetState.AddState(States.Stun, 2.0f, 0, Hero.gameObject, this.name);
    }

    private float CalculateDamage(float baseDamage)
    {
        bool isCriticalHit = Random.Range(0f, 100f) <= criticalChance;

        if (isCriticalHit)
        {
            return baseDamage * CriticalMultiplier;
        }

        return baseDamage;
    }

    private void ApplyDamage(float damage, DamageType damageType, Character target)
    {
        Damage _damage = new Damage
        {
            Value = damage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.RangeAttack,
        };

        CmdApplyDamage(_damage, target.gameObject);
    }

    private void ApplyDamage(float damage, DamageType damageType, Object target)
    {
        Damage _damage = new Damage
        {
            Value = damage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.RangeAttack,
        };

        CmdTryTakeDamage(target, _damage, null);
    }

    [Command]
    private void CmdTryTakeDamage(Object target, Damage damage, Skill skill)
    {
        target.ObjectHealth.TryTakeDamage(ref damage, null);
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
    }
}