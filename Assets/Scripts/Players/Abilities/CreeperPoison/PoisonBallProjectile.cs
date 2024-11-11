using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System.Security.Cryptography;

public class PoisonBallProjectile : Test_Projectile
{
    #region Variables
    [Header("PoisonBallProjectile Parameters")]
    [SerializeField] private Transform _transformBall;
    [SerializeField] private float _damage;
    [SerializeField] private float _baseDistancePush;
    [SerializeField] private float _baseDurationPush;
    [SerializeField] private float _durationInAir;
    [SerializeField] private float _fastMovementSpeed;
    [SerializeField] private float _slowMovementSpeed;
    [SerializeField] private float _baseSizeBall;
    
    private PoisonBall _poisonBall;
    private Skill _skill;
    private HealingPoisonBall _healingPoisonBall;
    private FootInstincts _footInstincts;
    private RestorationOfGlands _restorationOfGlands;

    private int _currentCountBall;
    private int _poisonBoneStack;
    private int _playerLayer;

    #region FloatVariables
    private float _newDistancePush;
    private float _energyDad;
    private float _distanceIncreaseMultiplier = 0.5f;
    private float _multiplierDistanceFromTalent;
    #endregion

    #region BoolVaribales

    private bool _isFast;
    private bool _isPlayer;
    private bool _isAllies;
    private bool _isEnemy;
    private bool _isAlly;
    private bool _isActiveHealingPoisonBall;
    private bool _isActiveWitheringPoison;
    private bool _isActiveVoluminousBall;
    private bool _isPushTarget;
    private bool _isPlayerInvisible;

    #endregion

    #endregion

    #region OnTriggerEnter

    [Server]
    private void OnTriggerEnter(Collider collision)
    {

        if (_isActiveHealingPoisonBall)
        {
            if (_isPlayer)
            {
                if (collision.gameObject == _player.gameObject)
                {
                    if (_isFast)
                    {
                        _player.CharacterState.AddState(States.HealingPoisonPerSecond, 6.0f, 0, _player.gameObject, _skill.Name);
                    }
                    else
                    {
                        _player.CharacterState.AddState(States.InstantHealingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                    }

                    DestroyProjectile();
                }
            }
            else if (_isAllies)
            {
                if (collision.transform != _player.transform && _playerLayer == LayerMask.NameToLayer("Allies"))
                {
                    if (collision.TryGetComponent<Character>(out var alliesHealth))
                    {
                        if (_isFast)
                        {
                            alliesHealth.CharacterState.AddState(States.HealingPoisonPerSecond, 6.0f, 0, _player.gameObject, _skill.Name);
                        }
                        else
                        {
                            alliesHealth.CharacterState.AddState(States.InstantHealingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                        }

                        DestroyProjectile();
                    }
                }
                else if (!_isEnemy && collision.transform != _player.transform)
                {
                    return;
                }
            }
            else if (_isEnemy)
            {
                if (collision.gameObject != _player.gameObject && _playerLayer != LayerMask.NameToLayer("Enemy"))
                {
                    if (collision.TryGetComponent<Character>(out var targetHealth))
                    {
                        _target = targetHealth;
                        DamageDeal();

                        if (_footInstincts.Data.IsOpen)
                        {
                            _footInstincts.ReductionCooldownLightningMovement();
                        }

                        _poisonBall.LastTarget = targetHealth.gameObject;
                    }
                }
                else if (_isAlly && collision.gameObject != _player.gameObject)
                {
                    return;
                }
            }
            else
            {
                if (collision.gameObject != _player.gameObject && _playerLayer != LayerMask.NameToLayer("Enemy"))
                {
                    if (collision.TryGetComponent<Character>(out var targetHealth))
                    {
                        _target = targetHealth;

                        DamageDeal();

                        if (_footInstincts.Data.IsOpen)
                        {
                            _footInstincts.ReductionCooldownLightningMovement();
                        }

                        _poisonBall.LastTarget = targetHealth.gameObject;
                    }
                }
            }
        }
        else
        {
            if (collision.gameObject != _player.gameObject && _playerLayer != LayerMask.NameToLayer("Enemy"))
            {
                if (collision.TryGetComponent<Character>(out var targetHealth))
                {
                    _target = targetHealth;

                    DamageDeal();
                    
                    if (_footInstincts.Data.IsOpen)
                    {
                        _footInstincts.ReductionCooldownLightningMovement();
                    }

                    _poisonBall.LastTarget = targetHealth.gameObject;
                }
            }
        }
    }

    #endregion

    #region MovementBall

    public void MoveBallToTarget(Vector3 target, bool isFast)
    {
        _isFast = isFast;

        float speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

        MoveToTarget(target, speed);
    }

    public void MoveBallOnMaxDistance(Vector3 point, bool isFast)
    {
        Debug.Log("PoisonBallProjectile / MoveBallOnMaxDistance");
        _isFast = isFast;

        float speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;
        Debug.Log("PoisonBallProjectile / MoveBallOnMaxDistance / speed = " + speed);
        Debug.Log("PoisonBallProjectile / MoveBallOnMaxDistance / point = " + point);

        MoveToPoint(point, speed);
    }

    #endregion

    #region MakingDamageAndDebuffs

    public override void DamageDeal()
    {
        Damage _baseDamage = new Damage
        {
            Value = _skill.Buff.Damage.GetBuffedValue(_damage),
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.RangeAttack,
        };

        _target.Health.TryTakeDamage(ref _baseDamage, _skill);
        _target.DamageTracker.AddDamage(_baseDamage, isServerRequest: isServer);

        if (_isActiveWitheringPoison)
        {
            _target.CharacterState.AddState(States.WitheringPoison, 6f, 0, _player.gameObject, _skill.Name);
        }

        if (_restorationOfGlands.Data.IsOpen && _poisonBoneStack > 0 && _target.CharacterState.CheckForState(States.PoisonBone))
        {
            ReductionCooldownFromRestorationOfGlands();
        }

        _target.CharacterState.AddState(States.InAir, _durationInAir, 0, _player.gameObject, _skill.Name);

        PushEnemyDependingOnCountProjectile(_target, _baseDurationPush);
        
        DestroyProjectile();
    }

    private void PushEnemyDependingOnCountProjectile(Character target, float durationPush)
    {
        if (_currentCountBall >= 2)
        {
            float multiplierPush = _currentCountBall * _distanceIncreaseMultiplier;
            _newDistancePush = _baseDistancePush + multiplierPush + _multiplierDistanceFromTalent;
        }
        else
        {
            _newDistancePush = _baseDistancePush;
        }
        PushEnemy(target, durationPush, _newDistancePush);
    }

    private void PushEnemy(Character target, float durationPush, float newDistancePush)
    {
        Vector3 directionPush = (target.transform.position - transform.position);

        newDistancePush = ((newDistancePush * GlobalVariable.cellSize) * durationPush) / GlobalVariable.cellSize;

        if (_isPushTarget)
        {
            //target.transform.DOMove((Vector2)target.transform.position + directionPush * distancePush, durationPush).SetEase(Ease.Linear);
            target.GetComponent<MoveComponent>().TargetRpcDoMove((Vector3)target.transform.position + directionPush * newDistancePush, durationPush);
        }
        else
        {
            //target.transform.DOMove((Vector2)target.transform.position - directionPush * distancePush, durationPush).SetEase(Ease.Linear);
            target.GetComponent<MoveComponent>().TargetRpcDoMove((Vector3)target.transform.position - directionPush * newDistancePush, durationPush);
        }

        target.Move.CanMove = true;
    }

    private void ReductionCooldownFromRestorationOfGlands()
    {
        float baseChanceOfRestorationOfGlands = 0.1f;
        float chanceRestorationOfGlands = baseChanceOfRestorationOfGlands * _poisonBoneStack;

        if (Random.Range(0f, 1f) <= chanceRestorationOfGlands)
        {
            Debug.Log("SpitPoisonProj / If RestorationOfGlands.IsActive = true");
            _restorationOfGlands.ReductionCooldown();
        }
    }
    #endregion

    #region InitializationProjectiles

    public void InitializationProjectileForPoisonBall(Character dad, float energyDad, 
        float multiplierDistance, float sizeBallWithTalent,
        Skill skill,
        bool isActiveTalentHealingPoisonBall, bool isTargetPlayer, 
        bool isTargetEnemy, bool isTargetAllies,
        bool isActiveTalentWitheringPoison, bool isPushTarget, 
        bool isActiveVoluminousBall, bool isPlayerInvisible, int poisonBoneStack)
    {
        _player = dad;
        _skill = skill;

        InitializationNumericVariables(energyDad, multiplierDistance, poisonBoneStack);

        InitializationBoolVariables(isActiveTalentHealingPoisonBall, 
            isTargetPlayer, isTargetEnemy, isTargetAllies, 
            isActiveTalentWitheringPoison, isPushTarget, 
            isActiveVoluminousBall, isPlayerInvisible);

        CheckActiveTalent(sizeBallWithTalent);

        Invoke("TransparentProjectileOnServer", 0.15f);
        InitializationComponentsForCountProjectile();
    }

    private void CheckActiveTalent(float sizeBallWithTalent)
    {
        if (_isActiveVoluminousBall)
        {
            _transformBall.localScale = new Vector3(sizeBallWithTalent, sizeBallWithTalent, sizeBallWithTalent);
        }
        else
        {
            _transformBall.localScale = new Vector3(_baseSizeBall, _baseSizeBall, _baseSizeBall);
        }
    }

    private void InitializationNumericVariables(float energyDad, float multiplierDistance, int poisonBoneStack)
    {
        _energyDad = energyDad;
        _multiplierDistanceFromTalent = multiplierDistance;
        _poisonBoneStack = poisonBoneStack;
    }

    private void InitializationBoolVariables(bool isActiveTalentHealingPoisonBall, bool isTargetPlayer, 
        bool isTargetEnemy, bool isTargetAllies,
        bool isActiveTalentWitheringPoison, bool isPushTarget, 
        bool isActiveVoluminousBall, bool isPlayerInvisible)
    {
        _isPushTarget = isPushTarget;
        _isPlayer = isTargetPlayer;
        _isAllies = isTargetAllies;
        _isEnemy = isTargetEnemy;

        _isActiveHealingPoisonBall = isActiveTalentHealingPoisonBall;
        _isActiveWitheringPoison = isActiveTalentWitheringPoison;
        _isActiveVoluminousBall = isActiveVoluminousBall;
        _isPlayerInvisible = isPlayerInvisible;
    }

    private void InitializationComponentsForCountProjectile()
    {
        _poisonBall = _player.GetComponentInChildren<PoisonBall>();
        _currentCountBall = _poisonBall.CurrentCountBall;
        _footInstincts = _poisonBall.FootInstinctsTalent;
        _restorationOfGlands = _poisonBall.RestorationOfGlandsTalent;
        Debug.Log("PoisonBallProjectile / restorationOfGlands = " + _restorationOfGlands);
    }

    #endregion

    #region ServerMethods

    [Server]
    private void TransparentProjectileOnServer()
    {
        Debug.Log("PoisonBallProj / TransparentProjectileOnServer / isServer = " + isServer);

        if (isServer)
        {
            LayerDefinition(_player.gameObject);
        }
        if (isServer && _isPlayerInvisible)
        {
            Debug.Log("isServer && _isPlayerInvisible / _player = " + _player);

            RpcNewTransparencySprite(_player.gameObject);
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
    private void RpcNewTransparencySprite(GameObject player)
    {
        Color originalColor = _projectileRenderer.material.color;

        if (_projectileRenderer != null)
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
    private void RpcLayerDefinition(int layer)
    {
        _playerLayer = layer;
    }
    #endregion

}