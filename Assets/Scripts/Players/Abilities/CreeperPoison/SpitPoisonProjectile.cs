using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpitPoisonProjectile : NetworkBehaviour
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private GameObject _hitEffect;
    [SerializeField] private float _distance = 5f;
    [SerializeField] private int _force = 40;

    private Character _dad;
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
        _startPos = transform.position;
        _rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
        //StartCoroutine(DisableCollider());
    }

    private void FixedUpdate()
    {
        if (Vector2.Distance(transform.position, _startPos) > _distance * GlobalVariable.cellSize)
        {
            Explode();
        }
    }

    //private IEnumerator DisableCollider()
    //{
    //    Collider2D projectileCollider = this.gameObject.GetComponent<Collider2D>();

    //    projectileCollider.enabled = false;
    //    Debug.Log($"Before projectileSpitPoisonCollider enabled == {projectileCollider.enabled}");

    //    yield return new WaitForSeconds(0.2f);
    //    Debug.Log("Two seconds passed");

    //    projectileCollider.enabled = true;
    //    Debug.Log($"After projectileSpitPoisonCollider enabled == {projectileCollider.enabled}");
    //}

    //[Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_talentIsActive)
        {
            if (_isPlayer)
            {
                if (collision.gameObject == _dad.gameObject)
                {
                    SetPlayer();
                    Debug.Log($"if (IsPlayer) // RegeneratingPoison.SetPlayer == {_dad}");
                    _dad.CharacterState.CmdAddState(States.RegeneratingPoison, 6.0f, 0);
                    Destroy(gameObject);
                }
            }
            else if (_isAllies)
            {
                if (collision.gameObject.layer == LayerMask.NameToLayer("Allies") && collision.gameObject != _dad.gameObject)
                {
                    if (collision.TryGetComponent<Character>(out var alliesHealth))
                    {
                        SetPlayer(); 
                        Debug.Log($"if (IsAllies) // RegeneratingPoison.SetPlayer == {_dad}");
                        alliesHealth.CharacterState.CmdAddState(States.RegeneratingPoison, 6.0f, 0);
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
                    if (collision.TryGetComponent<Character>(out var target))
                    {
                        _damage = Random.Range(4.0f, 12.0f);

                        DealDamage(target, _damage, DamageType.Magical, AttackRangeType.RangeAttack);
                    }
                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Allies") && collision.gameObject != _dad.gameObject)
                {
                    return;
                }
            }    
            
        }
        else
        {
            if (collision.gameObject.transform != _dad.transform && collision.gameObject.layer != LayerMask.NameToLayer("Allies"))
            {
                if (collision.TryGetComponent<Character>(out var target))
                {
                    _damage = Random.Range(4.0f, 12.0f);

                    DealDamage(target, _damage, DamageType.Magical, AttackRangeType.RangeAttack);
                }
            }
        }
    }

    private void DealDamage(Character target, float damage, DamageType damageType, AttackRangeType attackRangeType)
    {
        float chanceOfBlindness = 0.3f;
        float numbersForChanceOfBlindness = Random.Range(0.0f, 1.0f);

        Energy _energyLink = (Energy)_dad.Stamina;
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
        _dad = dad;
        _energyDad = energy;
        _isPlayer = isTargetPlayer;
        _isAllies = isTargetAllies;
        _isEnemy = isTargetEnemy;
        _talentIsActive = talentIsActive;
        Debug.Log($"InitializationProjectileSpitPoison / _dad == {_dad}");
        Debug.Log($"_isPlayer == {_isPlayer}; _isAllies == {_isAllies}; _isEnemy == {_isEnemy}; _talentIsActive == {_talentIsActive}");
    }

    private void SetPlayer()
    {
        RegeneratingPoison.SetPlayer(_dad);
        PoisonBone.SetPlayer(_dad);
        Debug.Log($"RegeneratingPoison.SetPlayer == {_dad}");
        RpcSetPlayer();
    }

    [ClientRpc]
    private void RpcSetPlayer()
    {
        RegeneratingPoison.SetPlayer(_dad);
        PoisonBone.SetPlayer(_dad);
        Debug.Log($"Rpc // RegeneratingPoison.SetPlayer == {_dad}");
    }
}
