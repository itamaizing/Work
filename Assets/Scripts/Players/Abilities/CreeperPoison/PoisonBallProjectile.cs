using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonBallProjectile : NetworkBehaviour
{
    [SerializeField] protected PoisonBall _poisonBall;

    [SerializeField] protected GameObject _hitEffect;
    [SerializeField] protected SpriteRenderer _spriteRenderer;
    [SerializeField] protected Collider2D _collider;
    [SerializeField] private Rigidbody2D _rbBall;
    [SerializeField] private Character _dad;

    private Vector3 _startPosition;
    private GameObject _currentTarget;
    private FootInstincts _footInstincts;

    private int _countProjectiles;

    private float _energyDad;

    private float _fastMovementSpeed = 0.6f;
    private float _slowMovementSpeed = 1.7f;

    private float _distancePush = 1.0f;
    private float _maxDistance = 6f;
    private float _durationPush = 1.2f;
    private float _durationStun = 1.0f;

    private float _currentDamageForPoisonBall = 35f;

    private bool _isPlayer;
    private bool _isAllies;
    private bool _isEnemy;
    private bool _talentHealingIPoisonBallIsActive;
    private bool _talentWitheringPoisonIsActive;
    private bool _isPushTarget;

    private void Start()
    {
        _durationPush = 1.0f;
        _startPosition = transform.position;
        InitializationComponentsForCountProjectile();
    }

    private void InitializationComponentsForCountProjectile()
    {
        _poisonBall = _dad.GetComponentInChildren<PoisonBall>();

        _footInstincts = _poisonBall.FootInstinctsTalent;
        _countProjectiles = _poisonBall.CountProjectiles;
        _currentTarget = _poisonBall.CurrentTarget;
    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        SetPlayer();
        if (_talentHealingIPoisonBallIsActive)
        {
            if (_isPlayer)
            {
                if (collision.gameObject == _dad.gameObject)
                {
                    Debug.Log($"if (IsPlayer) // HealingPoison.SetPlayer == {_dad}");
                    TargetCheckForState(_dad);
                    _dad.CharacterState.CmdAddState(States.HealingPoison, 6.0f, 0);
                    Destroy(gameObject);
                }
            }
            else if (_isAllies)
            {
                if (collision.gameObject.layer == LayerMask.NameToLayer("Allies") && collision.gameObject != _dad.gameObject)
                {
                    if (collision.TryGetComponent<Character>(out var alliesHealth))
                    {
                        TargetCheckForState(alliesHealth); 
                        Debug.Log($"if (IsAllies) // HealingPoison.SetPlayer == {_dad}");
                        Debug.Log($"Collision gameObject AlliesHealth == {alliesHealth.name}");
                        alliesHealth.CharacterState.CmdAddState(States.HealingPoison, 6.0f, 0);

                        Destroy(gameObject);
                    }
                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") && collision.gameObject != _dad.gameObject)
                {
                    return;
                }
            }
            else if (_isEnemy)
            {
                if (collision.gameObject.transform != _dad.transform && collision.gameObject.layer != LayerMask.NameToLayer("Allies"))
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
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Allies") && collision.gameObject != _dad.gameObject)
                {
                    return;
                }
            }
            else
            {
                if (collision.gameObject.transform != _dad.transform && collision.gameObject.layer != LayerMask.NameToLayer("Allies"))
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
            if (collision.gameObject.transform != _dad.transform && collision.gameObject.layer != LayerMask.NameToLayer("Allies"))
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
        float speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

        _rbBall.DOMove(target, speed * _maxDistance / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(DestroyProjectile);
    }

    public void MoveBallOnMaxDistance(Vector3 point, bool isFast)
    { 
        float speed = isFast ? _fastMovementSpeed : _slowMovementSpeed;

        Vector3 direction = (point - transform.position).normalized;

        StartCoroutine(MoveBallOnMaxDistanceCoroutine(direction, speed));
    }

    private IEnumerator MoveBallOnMaxDistanceCoroutine(Vector3 direction, float speed)
    {
        while (true)
        {
            transform.position += direction * (speed * 30f) * Time.deltaTime;
            if (Vector3.Distance(transform.position, _dad.transform.position) > _maxDistance * GlobalVariable.cellSize) 
            {
                DestroyProjectile();
            }
            yield return null;
        } 
    }

    #endregion

    #region Making Damage

    private void DealDamage(HeroComponent targetHealth, float currentDamage, DamageType damageType, AttackRangeType attackRangeType)
    {
        Energy _energyLink = (Energy)_dad.GetComponent<Character>().Stamina;
        _energyLink.SumDamageMake(currentDamage);

        targetHealth.Health.CmdTryTakeDamage(currentDamage, damageType, attackRangeType);

        if (_talentWitheringPoisonIsActive)
        {
            targetHealth.CharacterState.CmdAddState(States.WitheringPoison, 6f, 0);
        }

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


        //if (_countProjectiles < 3)
        //{
        //    target.transform.DOMove((Vector2)target.transform.position - directionPush * distancePush, durationPush).SetEase(Ease.Linear);
        //}
        //else if (_countProjectiles == 3 && _currentTarget == _poisonBall.LastTarget)
        //{
        //    target.transform.DOMove((Vector2)target.transform.position + directionPush * distancePush, durationPush).SetEase(Ease.Linear);
        //}
        //else if (_countProjectiles == 3 && _currentTarget != _poisonBall.LastTarget)
        //{
        //    target.transform.DOMove((Vector2)target.transform.position - directionPush * distancePush, durationPush).SetEase(Ease.Linear);
        //}

        target.CharacterState.CmdAddState(States.InAir, _durationStun, 0);
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }

    #endregion

    #region InitializationProjectiles

    public void InitializationProjectileForPoisonBall(Character dad, float energyDad, 
        bool isActiveTalentHealingPoisonBall, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies,
        bool isActiveTalentWitheringPoison, bool isPushTarget)
    {
        _dad = dad;
        _energyDad = energyDad;
        _isPlayer = isTargetPlayer;
        _isAllies = isTargetAllies;
        _isEnemy = isTargetEnemy;
        _talentHealingIPoisonBallIsActive = isActiveTalentHealingPoisonBall;
        _talentWitheringPoisonIsActive = isActiveTalentWitheringPoison;
        _isPushTarget = isPushTarget;
        Debug.Log($"_isPlayer == {_isPlayer}; _isAllies == {_isAllies}; _isEnemy == {_isEnemy}; _talentIsActive == {_talentHealingIPoisonBallIsActive};" +
            $" _talentWitheringPosion = {_talentWitheringPoisonIsActive}; _isPushTarget = {_isPushTarget}");
    }

    #endregion


    #region SetPlayerAndCheckState

    private void SetPlayer()
    {
        HealingPoison.SetPlayer(_dad);
        WitheringPoisonState.SetPlayer(_dad);
        Debug.Log($" WitheringPoisonState.SetPlayer == {_dad}");
        RpcSetPlayer();
    }

    private void TargetCheckForState(Character alliesHealth)
    {
        if (alliesHealth.CharacterState.CheckForState(States.RegeneratingPoison))
        {
            //Debug.Log($"TargetCheckForState.CheckForState == {alliesHealth.CharacterState.CheckForState(States.RegeneratingPoison)}");
            RegeneratingPoison.InstantHeal();
        }
        RpcTargetCheckForState(alliesHealth);
    }

    [ClientRpc]
    private void RpcSetPlayer()
    {
        HealingPoison.SetPlayer(_dad);
        WitheringPoisonState.SetPlayer(_dad);
        Debug.Log($"Rpc //  WitheringPoisonState.SetPlayer == {_dad}");
    }

    [ClientRpc]
    private void RpcTargetCheckForState(Character alliesHealth)
    {
        if (alliesHealth.CharacterState.CheckForState(States.RegeneratingPoison))
        {
            //Debug.Log($"RpcTargetCheckForState.CheckForState == {alliesHealth.CharacterState.CheckForState(States.RegeneratingPoison)}");
            RegeneratingPoison.InstantHeal();
        }
    }

    #endregion
}