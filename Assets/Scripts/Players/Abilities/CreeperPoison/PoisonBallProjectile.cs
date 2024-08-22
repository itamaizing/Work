using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonBallProjectile : NetworkBehaviour
{
    #region Variables
    [SerializeField] protected PoisonBall _poisonBall;

    [SerializeField] protected GameObject _hitEffect;
    [SerializeField] protected SpriteRenderer _spriteRenderer;
    [SerializeField] protected Collider2D _collider;
    [SerializeField] private Rigidbody2D _rbBall;
    [SerializeField] private Character _player;

    private List<Talent> _talents = new();
    private ContinuationAmbush _continuationAmbush;
    private HealingPoisonBall _healingPoisonBall;

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

    #region BoolVaribales

    private bool _isPlayer;
    private bool _isAllies;
    private bool _isEnemy;

    private bool _isActiveHealingPoisonBall;
    private bool _isActvieWitheringPoison;
    private bool _isActiveContinuationAmbush;

    private bool _isPushTarget;

    #endregion

    #endregion
    private void Start()
    {
        _durationPush = 1.0f;
        _startPosition = transform.position;
        InitializationComponentsForCountProjectile();

        _talents = _player.TalentSystem.Talents;
        foreach(Talent talent in _talents)
        {
            if (talent is HealingPoisonBall healBall)
            {
                _healingPoisonBall = healBall;
                Debug.Log($"PoisonBallProjectile / foreach / HealingPoisonBall = {_healingPoisonBall}");
            }

            if (talent is ContinuationAmbush contAmbush)
            {
                _continuationAmbush = contAmbush;
                Debug.Log($"PoisonBallProjectile / foreach / ContinuationAmbush = {_continuationAmbush}");
            }
        }

        bool isAmbush = _continuationAmbush.IsActive;
        bool isHealBall = _healingPoisonBall.IsActive;
        Debug.Log($"PoisonBallProjectile / ContinuationAmbush.isActive = {isAmbush}");
        Debug.Log($"PoisonBallProjectile / HealingPoisonBall.isActive = {isHealBall}");
    }

    private void InitializationComponentsForCountProjectile()
    {
        _poisonBall = _player.GetComponentInChildren<PoisonBall>();

        _footInstincts = _poisonBall.FootInstinctsTalent;
        _countProjectiles = _poisonBall.CountProjectiles;
        _currentTarget = _poisonBall.CurrentTarget;
    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        SetPlayer();
        if (_isActiveHealingPoisonBall)
        {
            if (_isPlayer)
            {
                if (collision.gameObject == _player.gameObject)
                {
                    TargetCheckForState(_player);

                    _player.CharacterState.CmdAddState(States.HealingPoison, 6.0f, 0);

                    Destroy(gameObject);
                }
            }
            else if (_isAllies)
            {
                if (collision.gameObject.layer == LayerMask.NameToLayer("Allies") && collision.gameObject != _player.gameObject)
                {
                    if (collision.TryGetComponent<Character>(out var alliesHealth))
                    {
                        TargetCheckForState(alliesHealth); 

                        alliesHealth.CharacterState.CmdAddState(States.HealingPoison, 6.0f, 0);

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
            if (Vector3.Distance(transform.position, _player.transform.position) > _maxDistance * GlobalVariable.cellSize) 
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
        if (_isActiveContinuationAmbush)
        {
            if (_countProjectiles == 4 && _currentTarget == _poisonBall.LastTarget)
            {
                return; //Другая логика будет
            }
        }

        Energy _energyLink = (Energy)_player.GetComponent<Character>().Stamina;
        _energyLink.SumDamageMake(currentDamage);

        targetHealth.Health.CmdTryTakeDamage(currentDamage, damageType, attackRangeType);

        if (_isActvieWitheringPoison)
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

        //else if (_countProjectiles == 3 && _currentTarget == _poisonBall.LastTarget)


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
        bool isActiveTalentWitheringPoison, bool isPushTarget, bool isActiveContinuationAmbush)
    {
        _player = dad;
        _energyDad = energyDad;
        _isPlayer = isTargetPlayer;
        _isAllies = isTargetAllies;
        _isEnemy = isTargetEnemy;
        _isActiveHealingPoisonBall = isActiveTalentHealingPoisonBall;
        _isActvieWitheringPoison = isActiveTalentWitheringPoison;
        _isActiveContinuationAmbush = isActiveContinuationAmbush;
        _isPushTarget = isPushTarget;

        Debug.Log($"_isPlayer == {_isPlayer}; _isAllies == {_isAllies}; _isEnemy == {_isEnemy}; _talentIsActive == {_isActiveHealingPoisonBall};" +
            $" _talentWitheringPosion = {_isActvieWitheringPoison}; _isPushTarget = {_isPushTarget}; _isActiveContinuationAmbush = {_isActiveContinuationAmbush}");
    }

    #endregion


    #region SetPlayerAndCheckState

    private void SetPlayer()
    {
        HealingPoison.SetPlayer(_player);
        WitheringPoisonState.SetPlayer(_player);
        Debug.Log($" WitheringPoisonState.SetPlayer == {_player}");
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
        HealingPoison.SetPlayer(_player);
        WitheringPoisonState.SetPlayer(_player);
        Debug.Log($"Rpc //  WitheringPoisonState.SetPlayer == {_player}");
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