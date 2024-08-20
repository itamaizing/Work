using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpitPoisonProjectile : NetworkBehaviour
{
    [SerializeField] private Rigidbody2D _rbBall;
    [SerializeField] private GameObject _hitEffect;
    [SerializeField] private Collider2D _colliderBall;
    [SerializeField] private float _maxDistance = 5f;
    [SerializeField] private float _speed = 30f;

    private Character _player;
    private Vector2 _startPos;

    private float _energyDad;
    private float _damage;
    private float _lifeTimePoisonBoneStacks = 20.0f;

    private bool _isPlayer;
    private bool _isAllies;
    private bool _isEnemy;

    private bool _talentIsActive;

    private void Awake()
    {
        StartCoroutine(DisableCollider());
    }

    private IEnumerator DisableCollider()
    {
        Collider2D projectileCollider = this.gameObject.GetComponent<Collider2D>();

        projectileCollider.enabled = false;
        Debug.Log($"Before projectileSpitPoisonCollider enabled == {projectileCollider.enabled}");

        yield return new WaitForSeconds(0.2f);
        Debug.Log("Two seconds passed");

        projectileCollider.enabled = true;
        Debug.Log($"After projectileSpitPoisonCollider enabled == {projectileCollider.enabled}");
    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_talentIsActive)
        {
            if (_isPlayer)
            {
                if (collision.gameObject == _player.gameObject)
                {
                    SetPlayer();
                    Debug.Log($"if (IsPlayer) // RegeneratingPoison.SetPlayer == {_player}");
                    _player.CharacterState.CmdAddState(States.RegeneratingPoison, 6.0f, 0);
                    Destroy(gameObject);
                }
            }
            else if (_isAllies)
            {
                if (collision.gameObject.layer == LayerMask.NameToLayer("Allies") && collision.gameObject != _player.gameObject)
                {
                    if (collision.TryGetComponent<Character>(out var alliesHealth))
                    {
                        SetPlayer(); 
                        Debug.Log($"if (IsAllies) // RegeneratingPoison.SetPlayer == {_player}");
                        alliesHealth.CharacterState.CmdAddState(States.RegeneratingPoison, 6.0f, 0);
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
                    if (collision.TryGetComponent<Character>(out var target))
                    {
                        _damage = Random.Range(4.0f, 12.0f);

                        DealDamage(target, _damage, DamageType.Magical, AttackRangeType.RangeAttack);
                    }
                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Allies") && collision.gameObject != _player.gameObject)
                {
                    return;
                }
            }    
            
        }
        else
        {
            if (collision.gameObject.transform != _player.transform && collision.gameObject.layer != LayerMask.NameToLayer("Allies"))
            {
                if (collision.TryGetComponent<Character>(out var target))
                {
                    _damage = Random.Range(4.0f, 12.0f);

                    DealDamage(target, _damage, DamageType.Magical, AttackRangeType.RangeAttack);
                }
            }
        }
    }

    public void MoveBallToTarget(Vector3 target)
    {
        Debug.Log("MoveBallToTarget SpitPoisonBall");
        Debug.Log($"MoveBallToTarget SpitPoisonBall / targetPos = {target}");
        Debug.Log($"MoveBallToTarget SpitPoisonBall / transformPos = {transform.position}");
        _rbBall.DOMove(target, _speed * _maxDistance / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(Explode);
    }

    public void MoveBallOnMaxDistance(Vector3 point)
    {
        Debug.Log("MoveBallOnMaxDistance SpitPoisonBall");
        Vector3 direction = (point - transform.position).normalized;

        StartCoroutine(MoveBallOnMaxDistanceCoroutine(direction, _speed));
    }

    private IEnumerator MoveBallOnMaxDistanceCoroutine(Vector3 direction, float speed)
    {
        Debug.Log("MoveBallOnMaxDistanceCoroutine SpitPoisonBall");
        while (true)
        {
            transform.position += direction * speed * Time.deltaTime;
            if (Vector3.Distance(transform.position, _player.transform.position) > _maxDistance * GlobalVariable.cellSize)
            {
                Explode();
            }
            yield return null;
            Debug.Log($"MoveBallOnMaxDistanceCoroutine SpitPoisonBall / transform.position = {transform.position}");
        }
    }

    private void DealDamage(Character target, float damage, DamageType damageType, AttackRangeType attackRangeType)
    {
        float chanceOfBlindness = 0.3f;
        float numbersForChanceOfBlindness = Random.Range(0.0f, 1.0f);

        Energy _energyLink = (Energy)_player.Stamina;
        _energyLink.SumDamageMake(damage);

        target.Health.TryTakeDamage(damage, damageType, attackRangeType);

        target.CharacterState.CmdAddState(States.PoisonBone, _lifeTimePoisonBoneStacks, 0);

        if (numbersForChanceOfBlindness <= chanceOfBlindness)
        {
            //target.CharacterState.CmdAddState(States.Blind, 6f, 0);
        }

        Explode();
    }

    private void Explode()
    {
        if (_hitEffect != null)
        {
            GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
            Destroy(hitEffect, 5f);
        }
        Destroy(gameObject);
    }

    public void InitializationProjectile(Character dad, float energy, bool talentIsActive, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        _player = dad;
        _energyDad = energy;
        _isPlayer = isTargetPlayer;
        _isAllies = isTargetAllies;
        _isEnemy = isTargetEnemy;
        _talentIsActive = talentIsActive;
        Debug.Log($"InitializationProjectileSpitPoison / _dad == {_player}");
        Debug.Log($"_isPlayer == {_isPlayer}; _isAllies == {_isAllies}; _isEnemy == {_isEnemy}; _talentIsActive == {_talentIsActive}");
    }

    private void SetPlayer()
    {
        RegeneratingPoison.SetPlayer(_player);
        PoisonBone.SetPlayer(_player);
        Debug.Log($"RegeneratingPoison.SetPlayer == {_player}");
        RpcSetPlayer();
    }

    [ClientRpc]
    private void RpcSetPlayer()
    {
        RegeneratingPoison.SetPlayer(_player);
        PoisonBone.SetPlayer(_player);
        Debug.Log($"Rpc // RegeneratingPoison.SetPlayer == {_player}");
    }
}
