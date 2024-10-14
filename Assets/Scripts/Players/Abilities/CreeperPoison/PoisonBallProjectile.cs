using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PoisonBallProjectile : NetworkBehaviour
{
    #region Variables

    [SerializeField] protected PoisonBall _poisonBall;

    [SerializeField] protected GameObject _hitEffect;
    [SerializeField] protected SpriteRenderer _spriteRenderer;
    [SerializeField] private Transform _transformBall;
    [SerializeField] protected Collider2D _collider;
    [SerializeField] private Rigidbody2D _rbBall;
    [SerializeField] private Character _player;

    private Skill _skill;
    private HealingPoisonBall _healingPoisonBall;
    private FootInstincts _footInstincts;

    private int _currentCountBall;
    [SyncVar] private int _teamIndex;

    #region FloatVariables

    private float _energyDad;
    private float _fastMovementSpeed = 0.1f;
    private float _slowMovementSpeed = 0.2f;
    private float _baseDistancePush = 1.2f;
    private float _distancePush;
    private float _maxDistance = 6f;
    private float _durationPush = 1.0f;
    private float _durationStun = 1.0f;
    private float _currentDamageForPoisonBall = 35f;
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
            RpcNewTransparencySprite();
        }
        _durationPush = 1.0f;
    }

    private void InitializationComponentsForCountProjectile()
    {
        _poisonBall = _player.GetComponentInChildren<PoisonBall>();
        _currentCountBall = _poisonBall.CurrentCountBall;
        _footInstincts = _poisonBall.FootInstinctsTalent;
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
                    //TargetCheckForState(_player);

                    if (_isFast)
                    {
                        _player.CharacterState.AddState(States.HealingPoisonPerSecond, 6.0f, 0, _player.gameObject, _skill.Name);
                    }
                    else
                    {
                        _player.CharacterState.AddState(States.InstantHealingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                    }

                    Destroy(gameObject);
                }
            }
            else if (_isAllies)
            {
                if (_isAlly && collision.transform != _player.transform)
                {
                    if (collision.TryGetComponent<Character>(out var alliesHealth))
                    {
                        //TargetCheckForState(alliesHealth);

                        if (_isFast)
                        {
                            alliesHealth.CharacterState.AddState(States.HealingPoisonPerSecond, 6.0f, 0, _player.gameObject, _skill.Name);
                        }
                        else
                        {
                            alliesHealth.CharacterState.AddState(States.InstantHealingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                        }

                        Destroy(gameObject);
                    }
                }
                else if (!_isAlly && collision.transform != _player.transform)
                {
                    return;
                }
            }
            else if (_isEnemy)
            {
                if (collision.gameObject != _player.gameObject && !_isAlly)
                {
                    if (collision.TryGetComponent<HeroComponent>(out var targetHealth))
                    {
                        DealDamage(targetHealth, _currentDamageForPoisonBall, DamageType.Magical, AttackRangeType.RangeAttack);

                        if (_footInstincts.Data.IsOpen)
                        {
                            _footInstincts.ReductionCooldownLightningMovement();
                        }

                        _poisonBall.LastTarget = targetHealth.gameObject;

                        Destroy(gameObject);
                    }
                }
                else if (_isAlly && collision.gameObject != _player.gameObject)
                {
                    return;
                }
            }
            else
            {
                if (collision.gameObject != _player.gameObject && !_isAlly)
                {
                    if (collision.TryGetComponent<HeroComponent>(out var targetHealth))
                    {
                        DealDamage(targetHealth, _currentDamageForPoisonBall, DamageType.Magical, AttackRangeType.RangeAttack);

                        if (_footInstincts.Data.IsOpen)
                        {
                            _footInstincts.ReductionCooldownLightningMovement();
                        }

                        _poisonBall.LastTarget = targetHealth.gameObject;

                        Destroy(gameObject);
                    }
                }
            }
        }
        else
        {
            if (collision.gameObject != _player.gameObject && !_isAlly)
            {
                if (collision.TryGetComponent<HeroComponent>(out var targetHealth))
                {
                    DealDamage(targetHealth, _currentDamageForPoisonBall, DamageType.Magical, AttackRangeType.RangeAttack);
                    
                    if (_footInstincts.Data.IsOpen)
                    {
                        _footInstincts.ReductionCooldownLightningMovement();
                    }

                    _poisonBall.LastTarget = targetHealth.gameObject;

                    Destroy(gameObject);
                }
            }
        }
    }

    #region MovementBall

    public void MoveBallToTarget(Vector3 target, bool isFast)
    {
        _isFast = isFast;

        float speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

        _rbBall.DOMove(target, speed * _maxDistance / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(DestroyProjectile);
    }

    public void MoveBallOnMaxDistance(Vector3 point, bool isFast)
    {
        _isFast = isFast;

        float speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

        Vector3 direction = (point - transform.position).normalized;

        StartCoroutine(MoveBallOnMaxDistanceCoroutine(direction, speed));
    }

    private IEnumerator MoveBallOnMaxDistanceCoroutine(Vector3 direction, float speed)
    {
        while (true)
        {
            transform.position += direction * (speed * 40f) * Time.deltaTime;
            if (Vector3.Distance(transform.position, _player.transform.position) > _maxDistance * GlobalVariable.cellSize)
            {
                DestroyProjectile();
            }
            yield return null;
        }
    }

    #endregion

    #region MakingDamageAndDebuffs

    private void DealDamage(HeroComponent targetHealth, float currentDamage, DamageType damageType, AttackRangeType attackRangeType)
    {
        Damage damage = new Damage
        {
            Value = _skill.Buff.Damage.GetBuffedValue(currentDamage),
            Type = DamageType.Physical,
            Range = AttackRangeType.RangeAttack,
        };

        targetHealth.Health.TryTakeDamage(ref damage, _skill);

        if (_isActvieWitheringPoison)
        {
            targetHealth.CharacterState.AddState(States.WitheringPoison, 6f, 0, _player.gameObject, _skill.Name);
        }

        targetHealth.CharacterState.AddState(States.InAir, _durationStun, 0, _player.gameObject, _skill.Name);

        PushEnemyDependingOnCountProjectile(targetHealth, _durationPush);

        Destroy(this.gameObject);
    }

    private void PushEnemyDependingOnCountProjectile(HeroComponent target, float durationPush)
    {
        if (_currentCountBall >= 2)
        {
            float multiplierPush = _currentCountBall * _distanceIncreaseMultiplier;
            _distancePush = _baseDistancePush + multiplierPush + _multiplierDistanceFromTalent;
            Debug.Log("PoisonBallProjectile / if currentBall distancePush = " + _distancePush);
        }
        else
        {
            _distancePush = _baseDistancePush;
        }
        PushEnemy(target, durationPush, _distancePush);
    }

    private void PushEnemy(HeroComponent target, float durationPush, float distancePush)
    {
        Vector2 directionPush = (target.transform.position - transform.position);

        distancePush = ((distancePush * GlobalVariable.cellSize) * durationPush) / GlobalVariable.cellSize;

        if (_isPushTarget)
        {
            //target.transform.DOMove((Vector2)target.transform.position + directionPush * distancePush, durationPush).SetEase(Ease.Linear);
            target.GetComponent<MoveComponent>().TargetRpcDoMove((Vector2)target.transform.position + directionPush * distancePush, durationPush);
        }
        else
        {
            //target.transform.DOMove((Vector2)target.transform.position - directionPush * distancePush, durationPush).SetEase(Ease.Linear);
            target.GetComponent<MoveComponent>().TargetRpcDoMove((Vector2)target.transform.position - directionPush * distancePush, durationPush);
        }

        target.Move.CanMove = true;
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }

    #endregion

    #region InitializationProjectiles

    public void InitializationProjectileForPoisonBall(Character dad, float energyDad, float multiplierDistance, Skill skill,
        bool isActiveTalentHealingPoisonBall, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies,
        bool isActiveTalentWitheringPoison, bool isPushTarget, bool isActiveVoluminousBall, bool isPlayerInvisible)
    {
        _player = dad;
        _energyDad = energyDad;
        _skill = skill;

        _isPushTarget = isPushTarget;
        _isPlayer = isTargetPlayer;
        _isAllies = isTargetAllies;
        _isEnemy = isTargetEnemy;

        _isActiveHealingPoisonBall = isActiveTalentHealingPoisonBall;
        _isActvieWitheringPoison = isActiveTalentWitheringPoison;
        _isActiveVoluminousBall = isActiveVoluminousBall;
        _isPlayerInvisible = isPlayerInvisible;

        _multiplierDistanceFromTalent = multiplierDistance;

        int teamIndex = _player.GetComponentInParent<UserNetworkSettings>().TeamIndex;
        _teamIndex = teamIndex;

        #region VoluminousBallTalentIsActvie

        if (_isActiveVoluminousBall)
        {
            _transformBall.localScale = new Vector2(1.2f, 1.2f);
        }
        else
        {
            _transformBall.localScale = new Vector2(1.0f, 1.0f);
        }

        #endregion

        InitializationComponentsForCountProjectile();
    }

    [ClientRpc]
    private void RpcNewTransparencySprite()
    {
        var localPlayer = NetworkClient.connection.identity.GetComponent<UserNetworkSettings>();
        _isAlly = localPlayer.TeamIndex == _teamIndex;

        Color originalColor = _spriteRenderer.color;

        if (_spriteRenderer != null)
        {
            if (_isAlly)
            {
                Color newTransparencySprite = originalColor;
                newTransparencySprite.a = 0.5f;
                _spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, newTransparencySprite.a);
            }
            else
            {
                Color newTransparencySprite = originalColor;
                newTransparencySprite.a = 0.0f;
                _spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, newTransparencySprite.a);
            }
        }
    }    
    #endregion
}