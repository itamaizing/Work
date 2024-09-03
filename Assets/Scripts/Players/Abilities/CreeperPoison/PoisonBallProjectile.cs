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

    #region FloatVariables

    private float _energyDad;
    private float _fastMovementSpeed = 0.6f;
    private float _slowMovementSpeed = 1.7f;
    private float _distancePush = 1.0f;
    private float _maxDistance = 6f;
    private float _durationPush = 1.2f;
    private float _durationStun = 1.0f;
    private float _currentDamageForPoisonBall = 35f;

    #endregion

    #region BoolVaribales

    private bool _isFast;
    private bool _isPlayer;
    private bool _isAllies;
    private bool _isEnemy;
    private bool _isActiveHealingPoisonBall;
    private bool _isActvieWitheringPoison;
    private bool _isActiveVoluminousBall;
    private bool _isPushTarget;

    #endregion

    #endregion

    private void Start()
    {
        _durationPush = 1.0f;
        //InitializationComponentsForCountProjectile();
    }

    private void InitializationComponentsForCountProjectile()
    {
        _poisonBall = _player.GetComponentInChildren<PoisonBall>();

        _footInstincts = _poisonBall.FootInstinctsTalent;
    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("OnTriggerEnter2D PoisonBall");
        if (_isActiveHealingPoisonBall)
        {
            if (_isPlayer)
            {
                if (collision.gameObject == _player.gameObject)
                {
                    //TargetCheckForState(_player);

                    if (_isFast)
                    {
                        _player.CharacterState.CmdAddState(States.HealingPoisonPerSecond, 6.0f, 0, _player.gameObject, _skill.Name);
                    }
                    else
                    {
                        _player.CharacterState.CmdAddState(States.InstantHealingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                    }

                    Destroy(gameObject);
                }
            }
            else if (_isAllies)
            {
                Debug.Log("IsAllies in PoisBallProj");
                if (collision.gameObject.layer == LayerMask.NameToLayer("Allies") && collision.gameObject != _player.gameObject)
                {
                    Debug.Log($"collision PoisBallProj");
                    if (collision.TryGetComponent<Character>(out var alliesHealth))
                    {
                        //TargetCheckForState(alliesHealth);

                        if (_isFast)
                        {
                            Debug.Log($"_isfast PoisBallProj= {_isFast}"); 
                            alliesHealth.CharacterState.CmdAddState(States.HealingPoisonPerSecond, 6.0f, 0, _player.gameObject, _skill.Name);
                        }
                        else
                        {
                            Debug.Log($"_isfast PoisBallProj = {_isFast}"); 
                            alliesHealth.CharacterState.CmdAddState(States.InstantHealingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                        }

                        Destroy(gameObject);
                    }
                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") && collision.gameObject != _player.gameObject)
                {
                    return;
                }
            }
            else if (_isEnemy)
            {
                if (collision.gameObject.transform != _player.transform && collision.gameObject.layer != LayerMask.NameToLayer("Allies"))
                {
                    if (collision.TryGetComponent<HeroComponent>(out var targetHealth))
                    {
                        DealDamage(targetHealth, _currentDamageForPoisonBall, DamageType.Magical, AttackRangeType.RangeAttack);

                        if (_footInstincts.IsActive)
                        {
                            _footInstincts.ReductionCooldownLightningMovement();
                        }

                        _poisonBall.LastTarget = targetHealth.gameObject;

                        Destroy(gameObject);
                    }
                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Allies") && collision.gameObject != _player.gameObject)
                {
                    return;
                }
            }
            else
            {
                if (collision.gameObject.transform != _player.transform && collision.gameObject.layer != LayerMask.NameToLayer("Allies"))
                {
                    if (collision.TryGetComponent<HeroComponent>(out var targetHealth))
                    {
                        DealDamage(targetHealth, _currentDamageForPoisonBall, DamageType.Magical, AttackRangeType.RangeAttack);

                        if (_footInstincts.IsActive)
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
            if (collision.gameObject.transform != _player.transform && collision.gameObject.layer != LayerMask.NameToLayer("Allies"))
            {
                if (collision.TryGetComponent<HeroComponent>(out var targetHealth))
                {
                    DealDamage(targetHealth, _currentDamageForPoisonBall, DamageType.Magical, AttackRangeType.RangeAttack);
                    
                    if (_footInstincts.IsActive)
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
            transform.position += direction * (speed * 15f) * Time.deltaTime;
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

        PushEnemyDependingOnCountProjectile(targetHealth, _durationPush, _distancePush);

        Destroy(this.gameObject);
    }

    private void PushEnemyDependingOnCountProjectile(HeroComponent target, float durationPush, float distancePush)
    {
        distancePush = 1.0f;
        PushEnemy(target, durationPush, distancePush);
    }

    private void PushEnemy(HeroComponent target, float durationPush, float distancePush)
    {
        Vector2 directionPush = (target.transform.position - transform.position);

        distancePush = ((distancePush * GlobalVariable.cellSize) * durationPush) / GlobalVariable.cellSize;

        if (_isPushTarget)
        {
            target.transform.DOMove((Vector2)target.transform.position + directionPush * distancePush, durationPush).SetEase(Ease.Linear);
            Debug.Log($"PoisonBallProjectile / PushEnemy / if (_isPushTarget = {_isPushTarget})");
        }
        else
        {
            target.transform.DOMove((Vector2)target.transform.position - directionPush * distancePush, durationPush).SetEase(Ease.Linear);
            Debug.Log($"PoisonBallProjectile / PushEnemy / else (_isPushTarget = {_isPushTarget})");
        }
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }

    #endregion

    #region InitializationProjectiles

    public void InitializationProjectileForPoisonBall(Character dad, float energyDad, Skill skill,
        bool isActiveTalentHealingPoisonBall, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies,
        bool isActiveTalentWitheringPoison, bool isPushTarget, bool isActiveVoluminousBall)
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

        Debug.Log($"_isPlayer = {_isPlayer}"); 
        Debug.Log($"_isAllies = {_isAllies}");
        Debug.Log($"_isEnemy = {_isEnemy}");
        Debug.Log($"_isActiveHealingPoisonBall = {_isActiveHealingPoisonBall}");
        Debug.Log($"_isActvieWitheringPoison = {_isActvieWitheringPoison}");
        Debug.Log($"_isActvieWitheringPoison = {_isActvieWitheringPoison}");
        Debug.Log($"_isActiveVoluminousBall = {_isActiveVoluminousBall}");
        Debug.Log($"_isPushTarget = {isPushTarget}");






        #region VoluminousBallTalentIsActvie

        if (_isActiveVoluminousBall)
        {
            _transformBall.localScale = new Vector2(1.2f, 1.2f);
            Debug.Log($"TransformBall == {_transformBall.localScale}");
        }
        else
        {
            _transformBall.localScale = new Vector2(1.0f, 1.0f);
            Debug.Log($"TransformBall == {_transformBall.localScale}");
        }

        #endregion

        InitializationComponentsForCountProjectile();
    }

    #endregion

    #region SetPlayerAndCheckState

    //private void SetPlayer()
    //{
    //    HealingPoisonPerSecond.SetPlayer(_player);
    //    WitheringPoisonState.SetPlayer(_player);
    //    RpcSetPlayer();
    //}

    //private void TargetCheckForState(Character alliesHealth)
    //{
    //    if (alliesHealth.CharacterState.CheckForState(States.RegeneratingPoison))
    //    {
    //        //Debug.Log($"TargetCheckForState.CheckForState == {alliesHealth.CharacterState.CheckForState(States.RegeneratingPoison)}");
    //        RegeneratingPoison.InstantHeal();
    //    }
    //    RpcTargetCheckForState(alliesHealth);
    //}

    //[ClientRpc]
    //private void RpcSetPlayer()
    //{
    //    HealingPoisonPerSecond.SetPlayer(_player);
    //    WitheringPoisonState.SetPlayer(_player);
    //}

    //[ClientRpc]
    //private void RpcTargetCheckForState(Character alliesHealth)
    //{
    //    if (alliesHealth.CharacterState.CheckForState(States.RegeneratingPoison))
    //    {
    //        RegeneratingPoison.InstantHeal();
    //    }
    //}

    #endregion

}