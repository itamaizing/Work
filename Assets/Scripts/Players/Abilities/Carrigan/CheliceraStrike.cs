using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheliceraStrike : AutoAttackSkill
{
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;

    private Damage _dealDamage;

    private float _baseDamage;
    private float _criticalDamage;

    private float _chanceCritDamage;
    private float _chanceApplyBleeding = 0.15f;

    private float _durationBleeding = 3.0f;

    private Coroutine _dealDamageWithAttackingPsiCoroutine;

    protected override void CastAction()
    {
        DamageDeal(_target.gameObject);
    }

    public void DealDamage(GameObject target)
    {
        DamageDeal(target);
    }

    private void DamageDeal(GameObject target)
    {
        Character targetCharacter = target.GetComponent<Character>();

        _baseDamage = Random.Range(11f, 13f);
        _chanceCritDamage = Random.Range(0.16f, 0.5f);
        float _currentChanceCritDamage = Random.Range(0f, 1f);
        float _currentChanceToApplyBleeding = Random.Range(0f, 1f);


        if (_currentChanceToApplyBleeding <= _chanceApplyBleeding)
        {
            targetCharacter.CharacterState.CmdAddState(States.Bleeding, _durationBleeding, 0, _player.gameObject, null);
        }

        if (_currentChanceCritDamage <= _chanceCritDamage)
        {
            _criticalDamage = CriticalDamageDeal(targetCharacter, _baseDamage);
        }

        _dealDamage = new Damage()
        {
            Value = _baseDamage + _criticalDamage,
            Type = DamageType.Physical,
            Range = AttackRangeType.MeleeAttack,
        };

        if (_basePsionicEnergy.IsAttackingPsiEnergyActive && target != null)
        {
            DamageDealWithAttackingPsionicEnergy(target, _dealDamage.Value);
        }
        else
        {
            CmdApplyDamage(_dealDamage, target);
        }
        _basePsionicEnergy.Add(_dealDamage.Value);
        _criticalDamage = 0f;
        _dealDamage.Value = 0f;
    }

    private float CriticalDamageDeal(Character target, float criticalDamage)
    {
        criticalDamage = CalculationCriticalDamage(criticalDamage);

        return criticalDamage;
    }

    private float CalculationCriticalDamage(float baseDamage)
    {
        float criticalDamage = baseDamage;
        float multiplierCrit = 1.8f;

        criticalDamage *= multiplierCrit;

        return criticalDamage;
    }

    private void DamageDealWithAttackingPsionicEnergy(GameObject target, float currentDamage)
    {
        _dealDamageWithAttackingPsiCoroutine = StartCoroutine(SearchingEnemiesAroundTarget(target, currentDamage));
    }

    private IEnumerator SearchingEnemiesAroundTarget(GameObject target, float currentDamage)
    {
        Character targetCharacter = target.GetComponent<Character>();

        #region DealDamageVariables
        float radiusAttack = 5.0f; // Then make it 1.0f
        float baseDamage = _basePsionicEnergy.CurrentAttackingPsiEnergy;
        float multiplierDamageByMainTarget = 0.3f;
        float percentageDamageToNearestEnemies = 0.5f;
        #endregion

        if (baseDamage > 10 && baseDamage < 20)
        {
            targetCharacter.CharacterState.DispelOneState(StateType.Magic);
        }

        while (_basePsionicEnergy.IsAttackingPsiEnergyActive)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(targetCharacter.transform.position, radiusAttack, _targetsLayers);
            foreach (var item in hitEnemies)
            {
                var itemCharacter = item.GetComponent<Character>();

                Vector2 direction = (itemCharacter.transform.position - _player.transform.position).normalized;

                if (item != null && item.gameObject != _player.gameObject)
                {
                    if (baseDamage > 20 && baseDamage < 30)
                    {
                        itemCharacter.CharacterState.DispelOneState(StateType.Magic);
                    }
                    else if (baseDamage >= 30)
                    {
                        itemCharacter.CharacterState.DispelOneState(StateType.Magic);

                        CmdPushTargets(item.gameObject, direction);
                    }

                    #region DamageDeal
                    Damage damageNearestEnemy = new Damage()
                    {
                        Value = (currentDamage + baseDamage) * percentageDamageToNearestEnemies,
                        Type = DamageType.Physical,
                        Range = AttackRangeType.MeleeAttack,
                    };
                    CmdApplyDamage(damageNearestEnemy, item.gameObject);

                    Damage damageMainTarget = new Damage()
                    {
                        Value = currentDamage + (baseDamage * multiplierDamageByMainTarget),
                        Type = DamageType.Physical,
                        Range = AttackRangeType.MeleeAttack,
                    };
                    CmdApplyDamage(damageMainTarget, targetCharacter.gameObject);
                    #endregion

                    yield break;
                }
            }
            
            yield return null;
        }
    }

    [Command]
    private void CmdPushTargets(GameObject target, Vector2 direction)
    {
        MoveComponent targetMove = target.gameObject.GetComponent<MoveComponent>();
        float durationPush = 0.2f;
        float distancePush = 1.0f * GlobalVariable.cellSize;

        targetMove.TargetRpcDoMove((Vector2)targetMove.transform.position + direction * distancePush, durationPush);
    }

}
