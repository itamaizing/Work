using Mirror;
using System.Collections;
using UnityEngine;

public class CheliceraStrike : AutoAttackSkill
{
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private JumpWithChelicera _jumpWithChelicera;
    [SerializeField] private float animSpeed = 1.4f;

    private Damage _dealDamage;

    private float _baseDamage;
    private float _criticalDamage;
    private float _additionalDamageFromSkill;

    private float _chanceCritDamage;
    private float _chanceApplyBleeding = 0.15f;

    private float _durationBleeding = 3.0f;

    private Coroutine _dealDamageWithAttackingPsiCoroutine;
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerAutoAttack => Animator.StringToHash("CheliceraStrikeTrigger");
   
    protected override void CastAction()
    {
        if (_target == null) return;
        DamageDeal(_target.gameObject);
    }

    private bool IsTargetInRange()
    {
        float maxDistance = Radius * 1.1f;
        return Vector3.Distance(_player.transform.position, _target.transform.position) <= maxDistance;
    }

    public void DealDamage(GameObject target, float additionalDamage)
    {
        _additionalDamageFromSkill = additionalDamage;
        DamageDeal(target);
    }

    private void DamageDeal(GameObject target)
    {
        Character targetCharacter = target.GetComponent<Character>();

        _baseDamage = Random.Range(11f, 13f);
        _chanceCritDamage = Random.Range(0.16f, 0.5f);
        float chanceCritValue = Random.Range(0f, 1f);
        float chanceBleedingValue = Random.Range(0f, 1f);

        if (_jumpWithChelicera.IsJumpDone)
        {
            float bonusDamage = _baseDamage * _additionalDamageFromSkill;
            _baseDamage += bonusDamage;
        }

        if (chanceBleedingValue <= _chanceApplyBleeding)
        {
            CmdAddState(targetCharacter);
        }

        if (chanceCritValue <= _chanceCritDamage)
        {
            _criticalDamage = CriticalDamageDeal(targetCharacter, _baseDamage);
        }

        _dealDamage = new Damage()
        {
            Value = _baseDamage + _criticalDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        if (_attackingPsionicEnergy.IsAttackingPsiEnergy && target != null)
        {
            DamageDealWithAttackingPsionicEnergy(target, _dealDamage.Value);
        }
        else
        {
            CmdApplyDamage(_dealDamage, target);
            CmdIncreaseEnergy(_dealDamage.Value);
        }

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

    private IEnumerator SearchingEnemiesAroundTarget(GameObject target, float baseDamage)
    {
        Character targetCharacter = target.GetComponent<Character>();

        #region DealDamageVariables
        float radiusAttack = 5.0f; // Then make it 1.0f
        float additionalDamage = _attackingPsionicEnergy.CurrentAttackingPsiEnergy;
        float multiplierDamageByMainTarget = 0.3f;
        float percentageDamageToNearestEnemies = 0.5f;
        #endregion

        if (additionalDamage > 10 && additionalDamage < 20)
        {
            targetCharacter.CharacterState.DispelStates(StateType.Magic, targetCharacter.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
        }

        while (_attackingPsionicEnergy.IsAttackingPsiEnergy)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(targetCharacter.transform.position, radiusAttack, _targetsLayers);
            foreach (var item in hitEnemies)
            {
                var itemCharacter = item.GetComponent<Character>();

                Vector2 direction = (itemCharacter.transform.position - _player.transform.position).normalized;

                if (item != null && item.gameObject != _player.gameObject)
                {
                    if (additionalDamage > 20 && additionalDamage < 30)
                    {
                        itemCharacter.CharacterState.DispelStates(StateType.Magic, itemCharacter.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
                    }
                    else if (additionalDamage >= 30)
                    {
                        itemCharacter.CharacterState.DispelStates(StateType.Magic, itemCharacter.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);

                        CmdPushTargets(item.gameObject, direction);
                    }

                    #region DamageDeal
                    Damage damageNearestEnemy = new Damage()
                    {
                        Value = (baseDamage + additionalDamage) * percentageDamageToNearestEnemies,
                        Type = DamageType.Physical,
                        PhysicAttackType = AttackRangeType.MeleeAttack,
                    };
                    CmdApplyDamage(damageNearestEnemy, item.gameObject);

                    Damage damageMainTarget = new Damage()
                    {
                        Value = baseDamage + (additionalDamage * multiplierDamageByMainTarget),
                        Type = DamageType.Physical,
                        PhysicAttackType = AttackRangeType.MeleeAttack,
                    };
                    CmdApplyDamage(damageMainTarget, targetCharacter.gameObject);
                    #endregion

                    CmdUseAttackingEnergy(additionalDamage);

                }

                if (_dealDamageWithAttackingPsiCoroutine != null)
                {
                    StopCoroutine(_dealDamageWithAttackingPsiCoroutine);
                    _dealDamageWithAttackingPsiCoroutine = null;
                }

                yield break;
            }
            
            yield return null;
        }
    }

    public void CheliceraStrikeSpeedAnim()
    {
        _player.Animator.SetFloat("CheliceraStrikeSpeed", 1f / animSpeed);
    }

    public void CheliceraStrikeCast()
    {
        AnimCastAction();
    }

    public void CheliceraStrikeEnded()
    {
        AnimCastEnded();
    }

    #region CommandMethods

    [Command]
    private void CmdIncreaseEnergy(float value)
    {
        _basePsionicEnergy.IncreasePsiEnergy(value);
    }

    [Command]
    private void CmdPushTargets(GameObject target, Vector2 direction)
    {
        MoveComponent targetMove = target.gameObject.GetComponent<MoveComponent>();
        float durationPush = 0.2f;
        float distancePush = 1.0f * GlobalVariable.cellSize;

        targetMove.TargetRpcDoMove((Vector2)targetMove.transform.position + direction * distancePush, durationPush);
    }

    [Command]
    private void CmdUseAttackingEnergy(float value)
    {
        _attackingPsionicEnergy.CurrentAttackingPsiEnergy -= value;
    }

    #endregion

    [Command]
    private void CmdAddState(Character character)
    {
        character.CharacterState.AddState(States.Bleeding, _durationBleeding, 0, _player.gameObject, null);
    }

    protected override void ClearData()
    {
        base.ClearData();
    }
}
