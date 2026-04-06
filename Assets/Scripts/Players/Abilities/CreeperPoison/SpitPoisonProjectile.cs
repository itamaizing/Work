using Mirror;
using UnityEngine;

public class SpitPoisonProjectile : Test_Projectile
{
    #region Variables

    [SerializeField] private Material _projectileMaterialBase;

    [SerializeField] private Material _projectileMaterialEnemy;
    [SerializeField] private Material _projectileMaterialAllies;

    [SerializeField] private ParticleSystem _particleSystem;

    private ParticleSystemRenderer _particleSystemRenderer;

    private SpitPoison _spitPoison;
    private Skill _skill;

    private int _playerLayer;
    private int _poisonBoneStack;

    private int _ownerLayer;

    private float _energyDad;
    private float _damage;
    private float _lifeTimePoisonBoneStacks = 60.0f;
    private float _buffer = 0.5f;

    private bool _isPlayer;
    private bool _isAllies;
    private bool _isEnemy;
    private bool _isActiveHealingSpitPoison;
    private bool _isActiveRestorationOfGlands;
    private bool _isActiveEatingAcid;
    private bool _isPlayerInvisible;
    private bool _isFeelingPoisoning;
    private bool _isTransparentPoisons;
    private bool _isColdBloodCrit;

    private bool _isReflected;

    #endregion

    private bool IsEnemy(GameObject target)
    {
        if (_isReflected) return target != _player.gameObject;
        if (_player == null) return IsEnemyByLayer(target);
        if (!_player.TryGetComponent(out UserNetworkSettings ownerSettings) || !target.TryGetComponent(out UserNetworkSettings targetSettings)) return IsEnemyByLayer(target);
        if (!IsTeamAssigned(ownerSettings) || !IsTeamAssigned(targetSettings)) return IsEnemyByLayer(target);

        return ownerSettings.TeamIndex != targetSettings.TeamIndex;
    }

    private bool IsTeamAssigned(UserNetworkSettings settings)
    {
        return settings.TeamIndex != 0;
    }

    private bool IsEnemyByLayer(GameObject target)
    {
        return ((1 << target.layer) & _skill.Targeting.Layer) != 0;
    }

    #region OnTriggerEnter

    [Server]
    private void OnTriggerEnter(Collider collision)
    {
        if (!collision.TryGetComponent<Character>(out var targetHealth)) return;

        if (targetHealth.CharacterState.CheckForState(States.ReflectiveScales))
        {
            if (_isReflected) return;

            targetHealth.CharacterState.RemoveState(States.ReflectiveScales);
            Reflect(targetHealth);
            return;
        }

        if (!IsEnemy(collision.gameObject)) return;

        if (_isActiveHealingSpitPoison)
        {
            if (_isPlayer)
            {
                if (collision.gameObject == _player.gameObject)
                {
                    _player.CharacterState.AddState(States.RegeneratingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                    Destroy(gameObject);
                }
            }
            else if (_isAllies)
            {
                if (collision.gameObject != _player.gameObject && _playerLayer == LayerMask.NameToLayer("Allies"))
                {
                    if (collision.TryGetComponent<Character>(out var alliesHealth))
                    {
                        alliesHealth.CharacterState.AddState(States.RegeneratingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                        Destroy(gameObject);
                    }
                }
                else if (!_isEnemy && collision.gameObject != _player.gameObject)
                {
                    return;
                }
            }   
            else if (_isEnemy)
            {
                if (collision.transform != _player.transform && _playerLayer != LayerMask.NameToLayer("Enemy"))
                {
                    if (collision.TryGetComponent<Character>(out var target))
                    {
                        _target = target;
                        _damage = Random.Range(4.0f, 12.0f);

                        DamageDeal();
                    }
                }
                else if (!_isAllies && collision.gameObject != _player.gameObject)
                {
                    return;
                }
            }
            else
            {
                if (collision.gameObject != _player.gameObject && _playerLayer != LayerMask.NameToLayer("Enemy"))
                {
                    if (collision.transform != _player.transform)
                    {
                        if (collision.TryGetComponent<Character>(out var target))
                        {
                            _target = target;

                            _damage = Random.Range(4.0f, 12.0f);

                            DamageDeal();
                        }
                    }
                }
            }
        }
        else
        {
            if (collision.transform != _player.transform && _playerLayer != LayerMask.NameToLayer("Enemy"))
            {
                if (collision.TryGetComponent<Character>(out var target))
                {
                    _target = target;

                    _damage = Random.Range(4.0f, 12.0f);

                    DamageDeal();
                }
            }
            
        }

        if (_isFeelingPoisoning) _player.CharacterState.AddState(States.FeelingPoisoning, 2f, 0, _player.gameObject, _skill.Name);
    }

    #endregion

    #region MoveMethods

    public void MoveBallToTarget(Vector3 target)
    {
        Debug.Log("SpitPoisonProj / MoveBallToTarget / Start");

        MoveToTarget(target, _speed);
    }

    public void MoveBallOnMaxDistance(Vector3 point)
    {
        Debug.Log("SpitPoisonProj / MoveBallOnMaxDistance / point = " + point);

        MoveToPoint(point, _speed);
    }
    #endregion

    #region DamageMethods

    public override void DamageDeal()
    {
        float chanceOfBlindness = 0.3f;
        float numbersForChanceOfBlindness = Random.Range(0.0f, 1.0f);

        float finalDamage = _damage;

        if (_isColdBloodCrit) finalDamage *= 2.5f;

        Damage _baseDamage = new Damage
        {
            Value = _skill.Buff.Damage.GetBuffedValue(finalDamage),
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.RangeAttack,
        };

        _target.Health.TryTakeDamage(ref _baseDamage, _skill);
        _target.DamageTracker.AddDamage(_baseDamage, null, isServerRequest: isServer);

        _target.CharacterState.AddState(States.PoisonBone, _lifeTimePoisonBoneStacks, 0, _player.gameObject, _skill.Name);

        if (_isActiveRestorationOfGlands && _poisonBoneStack > 0 && _target.CharacterState.CheckForState(States.PoisonBone))
        {
            //ReductionCooldownFromRestorationOfGlands();
        }

        _target.CharacterState.AddState(States.Blind, 6f, 0, _player.gameObject, _skill.Name);

        DestroyProjectile();        
    }
    private void ReductionCooldownFromRestorationOfGlands()
    {
        RpcReductionCooldownFromRestorationOfGlands(_player.gameObject);
    }

    private void Reflect(Character reflector)
    {
        _isReflected = true;

        StopMovement();
        CancelInvoke(nameof(DestroyProjectile));

        Character oldOwner = _player;
        _player = reflector;

        if (oldOwner == null) return;

        Vector3 target = oldOwner.transform.position;

        Vector3 direction = (target - transform.position).normalized;
        float speed = _speed;

        _target = null;
        _playerLayer = reflector.gameObject.layer;

        _isEnemy = true;
        _isAllies = false;
        _isPlayer = false;

        MoveToTarget(target, speed);
        RpcInitTransparent(_isTransparentPoisons, _playerLayer);
    }
    #endregion

    #region InitializationMethods

    public void InitializationProjectile(Character dad, Skill skill, float energy,
        bool isActiveHealingSpitPoison, bool isActiveRestorationOfGlands, bool isPlayerInvisible, 
        bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies, int poisonBoneStack, bool isFeelingPoisoning, bool isTransparentPoisons, int ownerLayer, bool isColdBloodCrit)
    {
        _player = dad;
        _energyDad = energy;
        _skill = skill;
        _isPlayerInvisible = isPlayerInvisible;
        _isFeelingPoisoning = isFeelingPoisoning;

        _poisonBoneStack = poisonBoneStack;
        _isActiveRestorationOfGlands = isActiveRestorationOfGlands;
        _isActiveHealingSpitPoison = isActiveHealingSpitPoison;
        _isPlayer = isTargetPlayer;
        _isAllies = isTargetAllies;
        _isEnemy = isTargetEnemy;
        _ownerLayer = ownerLayer;
        _isTransparentPoisons = isTransparentPoisons;
        _isColdBloodCrit = isColdBloodCrit;

        if (_particleSystem != null) _particleSystemRenderer = _particleSystem.GetComponent<ParticleSystemRenderer>();

        Invoke("TransparentProjectileOnServer", 0.15f);
        InitializationComponents();
    }

    private void InitializationComponents()
    {
        _spitPoison = _player.GetComponentInChildren<SpitPoison>();
    }

    public void ScheduleAutoDestroy(Vector3 targetPoint, float speed)
    {
        float distance = Vector3.Distance(transform.position, targetPoint);
        float flightTime = (distance + _buffer) / speed;

        Invoke(nameof(DestroyProjectile), flightTime);
    }

    private void ApplyTransparentVisual()
    {
        if (!_particleSystemRenderer) return;

        if (!_isTransparentPoisons)
        {
            _particleSystemRenderer.material = _projectileMaterialBase;
            return;
        }

        if (_ownerLayer == LayerMask.NameToLayer("Allies")) _particleSystemRenderer.material = _projectileMaterialAllies;
        else if (_ownerLayer == LayerMask.NameToLayer("Enemy")) _particleSystemRenderer.material = _projectileMaterialEnemy;
        else _particleSystemRenderer.material = _projectileMaterialBase;
    }

    #endregion

    #region ServerMethods

    [Server]
    private void TransparentProjectileOnServer()
    {
        if (isServer)
        {
            LayerDefinition(_player.gameObject);
        }
        if (isServer && _isPlayerInvisible)
        {
            RpcNewTransparencySprite(_player.gameObject, this.gameObject);
        }
    }

    [Server]
    private void LayerDefinition(GameObject player)
    {
        _playerLayer = player.layer;
        RpcLayerDefinition(player.layer);
    }

    #endregion

    #region ClientRpcMethods

    [ClientRpc]
    private void RpcNewTransparencySprite(GameObject player, GameObject projectile)
    {
        MeshRenderer projectileMaterial = projectile.GetComponent<MeshRenderer>();
        Color originalColor = projectileMaterial.material.color;

        if (projectileMaterial != null)
        {
            if (player.layer == LayerMask.NameToLayer("Allies"))
            {
                Color newTransparencySprite = originalColor;
                newTransparencySprite.a = 0.5f;
                _projectileRenderer.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, newTransparencySprite.a);
            }
            else if (player.layer == LayerMask.NameToLayer("Enemy"))
            {
                Color newTransparencySprite = originalColor;
                newTransparencySprite.a = 0.0f;
                _projectileRenderer.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, newTransparencySprite.a);
            }
        }
    }

    [ClientRpc]
    public void RpcInitTransparent(bool isTransparentPoisons, int ownerLayer)
    {
        _isTransparentPoisons = isTransparentPoisons;
        _ownerLayer = ownerLayer;

        ApplyTransparentVisual();
    }

    [ClientRpc]
    private void RpcLayerDefinition(int layer)
    {
        _playerLayer = layer;
    }

    [ClientRpc]
    private void RpcReductionCooldownFromRestorationOfGlands(GameObject player)
    {
        var restorationOfGlands = player.GetComponentInChildren<RestorationOfGlands>();

        float baseChanceOfRestorationOfGlands = 0.1f;
        float chanceRestorationOfGlands = baseChanceOfRestorationOfGlands * _poisonBoneStack;

        if (Random.Range(0f, 1f) <= chanceRestorationOfGlands)
        {
            Debug.Log("SpitPoisonProj / If RestorationOfGlands.IsActive = true");
            restorationOfGlands.ReductionCooldown();
        }
    }
    #endregion
}

