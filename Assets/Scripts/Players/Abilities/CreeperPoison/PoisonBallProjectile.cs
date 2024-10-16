using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

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
    
    private PoisonBall _poisonBall;
    private Skill _skill;
    private HealingPoisonBall _healingPoisonBall;
    private FootInstincts _footInstincts;

    private int _currentCountBall;
    private int _playerLayer;

    #region FloatVariables
    private float _newDistancePush;
    private float _energyDad;
    private float _baseSizeBall = 1.0f;
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
    private bool _isActvieWitheringPoison;
    private bool _isActiveVoluminousBall;
    private bool _isPushTarget;
    private bool _isPlayerInvisible;

    #endregion

    #endregion

    private void Start()
    {
        if (isServer && _isPlayerInvisible)
        {
            RpcNewTransparencySprite(_player.gameObject);
        }
        else if (isServer)
        {
            LayerDefinition(_player.gameObject);
        }

    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
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

    #region MovementBall

    public void MoveBallToTarget(Vector3 target, bool isFast)
    {
        _isFast = isFast;

        float speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

        MoveToTarget(target, speed);
    }

    public void MoveBallOnMaxDistance(Vector3 point, bool isFast)
    {
        _isFast = isFast;

        float speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

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
            Range = AttackRangeType.RangeAttack,
        };

        _target.Health.TryTakeDamage(ref _baseDamage, _skill);

        if (_isActvieWitheringPoison)
        {
            _target.CharacterState.AddState(States.WitheringPoison, 6f, 0, _player.gameObject, _skill.Name);
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
        Vector2 directionPush = (target.transform.position - transform.position);

        newDistancePush = ((newDistancePush * GlobalVariable.cellSize) * durationPush) / GlobalVariable.cellSize;

        if (_isPushTarget)
        {
            //target.transform.DOMove((Vector2)target.transform.position + directionPush * distancePush, durationPush).SetEase(Ease.Linear);
            target.GetComponent<MoveComponent>().TargetRpcDoMove((Vector2)target.transform.position + directionPush * newDistancePush, durationPush);
        }
        else
        {
            //target.transform.DOMove((Vector2)target.transform.position - directionPush * distancePush, durationPush).SetEase(Ease.Linear);
            target.GetComponent<MoveComponent>().TargetRpcDoMove((Vector2)target.transform.position - directionPush * newDistancePush, durationPush);
        }

        target.Move.CanMove = true;
    }

    #endregion

    #region InitializationProjectiles

    public void InitializationProjectileForPoisonBall(Character dad, float energyDad, 
        float multiplierDistance, float sizeBallWithTalent,
        Skill skill,
        bool isActiveTalentHealingPoisonBall, bool isTargetPlayer, 
        bool isTargetEnemy, bool isTargetAllies,
        bool isActiveTalentWitheringPoison, bool isPushTarget, 
        bool isActiveVoluminousBall, bool isPlayerInvisible)
    {
        _player = dad;
        _skill = skill;

        InitializationNumericVariables(energyDad, multiplierDistance);

        InitializationBoolVariables(isActiveTalentHealingPoisonBall, 
            isTargetPlayer, isTargetEnemy, isTargetAllies, 
            isActiveTalentWitheringPoison, isPushTarget, 
            isActiveVoluminousBall, isPlayerInvisible);

        CheckActiveTalent(sizeBallWithTalent);

        InitializationComponentsForCountProjectile();
    }

    private void CheckActiveTalent(float sizeBallWithTalent)
    {
        if (_isActiveVoluminousBall)
        {
            _transformBall.localScale = new Vector2(sizeBallWithTalent, sizeBallWithTalent);
        }
        else
        {
            _transformBall.localScale = new Vector2(_baseSizeBall, _baseSizeBall);
        }
    }

    private void InitializationNumericVariables(float energyDad, float multiplierDistance)
    {
        _energyDad = energyDad;
        _multiplierDistanceFromTalent = multiplierDistance;
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
        _isActvieWitheringPoison = isActiveTalentWitheringPoison;
        _isActiveVoluminousBall = isActiveVoluminousBall;
        _isPlayerInvisible = isPlayerInvisible;
    }

    private void InitializationComponentsForCountProjectile()
    {
        _poisonBall = _player.GetComponentInChildren<PoisonBall>();
        _currentCountBall = _poisonBall.CurrentCountBall;
        _footInstincts = _poisonBall.FootInstinctsTalent;
    }

    [ClientRpc]
    private void RpcNewTransparencySprite(GameObject player)
    {
        Color originalColor = _projectileSprite.color;

        if (_projectileSprite != null)
        {
            if (_playerLayer == LayerMask.NameToLayer("Allies"))
            {
                Color newTransparencySprite = originalColor;
                newTransparencySprite.a = 0.5f;
                _projectileSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, newTransparencySprite.a);
            }
            else if (_playerLayer == LayerMask.NameToLayer("Enemy"))
            {
                Color newTransparencySprite = originalColor;
                newTransparencySprite.a = 0.0f;
                _projectileSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, newTransparencySprite.a);
            }
        }
    }

    [Server]
    private void LayerDefinition(GameObject player)
    {
        _playerLayer = player.layer;

        RpcLayerDefinition(player.layer);
    }

    [ClientRpc]
    private void RpcLayerDefinition(int layer)
    {
        _playerLayer = layer;
    }

    #endregion
}