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

    private const float _radiusAttackPsi = 2.0f;
    private const float _pushDuration = 0.2f;
    private const float _pushDistance = 1.0f;

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

        if (chanceBleedingValue <= _chanceApplyBleeding) CmdAddState(targetCharacter);

        if (chanceCritValue <= _chanceCritDamage) _criticalDamage = CriticalDamageDeal(targetCharacter, _baseDamage);

        _dealDamage = new Damage()
        {
            Value = _baseDamage + _criticalDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        if (_attackingPsionicEnergy.IsAttackingPsiEnergy && target != null) DamageDealWithAttackingPsionicEnergy(targetCharacter, _dealDamage.Value);

        CmdApplyDamage(_dealDamage, target);

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

    private void DamageDealWithAttackingPsionicEnergy(Character targetCharacter, float baseDamage)
    {
        float attackingPsi = _attackingPsionicEnergy.CurrentValue;

        if (attackingPsi <= 0) return;

        if (attackingPsi >= 10)
        {
            targetCharacter.CharacterState.DispelStates(StateType.Magic, targetCharacter.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
        }

        if (attackingPsi >= 20)
        {
            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, _radiusAttackPsi, _targetsLayers);
            foreach (var enemyCollider in nearbyEnemies)
            {
                if (enemyCollider.TryGetComponent<Character>(out var enemy) && enemy != targetCharacter)
                {
                    enemy.CharacterState.DispelStates(StateType.Magic, enemy.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
                }
            }
        }

        if (attackingPsi >= 30)
        {
            Collider[] enemiesToPush = Physics.OverlapSphere(transform.position, _radiusAttackPsi, _targetsLayers);
            foreach (var enemyCollider in enemiesToPush)
            {
                if (enemyCollider.TryGetComponent<Character>(out var enemy))
                {
                    Vector2 direction = (enemy.transform.position - _player.transform.position).normalized;
                    CmdPushTargets(enemy.gameObject, direction);
                }
            }
        }

        float magicDamagePerPsiMainTarget = 0.3f;
        float magicDamagePerPsiNearby = 0.5f;

        float totalMagicDamageMainTarget = attackingPsi * magicDamagePerPsiMainTarget;

        var magicDamageMainTarget = new Damage
        {
            Value = totalMagicDamageMainTarget,
            Type = DamageType.Magical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        CmdApplyDamage(magicDamageMainTarget, targetCharacter.gameObject);

        Collider[] nearbyEnemiesToDamage = Physics.OverlapSphere(transform.position, _radiusAttackPsi, _targetsLayers);

        foreach (var enemyCollider in nearbyEnemiesToDamage)
        {
            if (enemyCollider.TryGetComponent<Character>(out var enemy) &&
                enemy != targetCharacter && enemy != _player)
            {
                float totalMagicDamageEnemy = attackingPsi * magicDamagePerPsiNearby;

                var magicDamageNearby = new Damage
                {
                    Value = totalMagicDamageEnemy,
                    Type = DamageType.Magical,
                    PhysicAttackType = AttackRangeType.MeleeAttack,
                };

                CmdApplyDamage(magicDamageNearby, enemy.gameObject);
            }
        }

        CmdUseAttackingEnergy(attackingPsi);
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
    private void CmdPushTargets(GameObject target, Vector3 direction)
    {
        if (target.TryGetComponent<MoveComponent>(out var targetMove))
        {
            Vector3 currentPos = targetMove.transform.position;
            Vector3 pushPos = currentPos + direction.normalized * _pushDistance;
 
            if (targetMove.connectionToClient != null) targetMove.TargetRpcDoPush(pushPos, _pushDuration);
            else targetMove.RpcDoPush(pushPos, _pushDuration);
        }
    }

    [Command]
    private void CmdUseAttackingEnergy(float value)
    {
        _attackingPsionicEnergy.CurrentValue -= value;
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
