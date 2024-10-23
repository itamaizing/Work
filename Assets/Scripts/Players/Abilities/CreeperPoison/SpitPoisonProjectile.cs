using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpitPoisonProjectile : Test_Projectile
{
    private SpitPoison _spitPoison;
    private RestorationOfGlands _restorationOfGlands;
    private Skill _skill;

    private int _playerLayer;
    private int _poisonBoneStack;

    private float _energyDad;
    private float _damage;
    private float _lifeTimePoisonBoneStacks = 6.0f;

    private bool _isPlayer;
    private bool _isAllies;
    private bool _isEnemy;
    private bool _isActiveHealingSpitPoison;
    private bool _isPlayerInvisible;

    private void Start()
    {
        if (isServer)
        {
            InitializationComponents();

            LayerDefinition(_player.gameObject);
        }
        if (isServer && _isPlayerInvisible)
        {
            RpcNewTransparencySprite(_player.gameObject);
        }
    }

    private void InitializationComponents()
    {
        _spitPoison = _player.GetComponentInChildren<SpitPoison>();
        Debug.Log("SpitPoisonProj / SpitPoison = " + _spitPoison);
        _restorationOfGlands = _spitPoison.RestorationOfGlandsTalent;
        Debug.Log("SpitPoisonProj / RestorationOfGlands = " + _restorationOfGlands);
    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
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
    }

    public void MoveBallToTarget(Vector3 target)
    {
        MoveToTarget(target, _speed);
    }

    public void MoveBallOnMaxDistance(Vector3 point)
    {
        MoveToPoint(point, _speed);
    }

    public override void DamageDeal()
    {
        float chanceOfBlindness = 0.3f;
        float numbersForChanceOfBlindness = Random.Range(0.0f, 1.0f);

        Damage _baseDamage = new Damage
        {
            Value = _skill.Buff.Damage.GetBuffedValue(_damage),
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.RangeAttack,
        };
        
        _target.Health.TryTakeDamage(ref _baseDamage, _skill);
        _target.DamageTracker.AddDamage(_baseDamage);

        _target.CharacterState.AddState(States.PoisonBone, _lifeTimePoisonBoneStacks, 0, _player.gameObject, _skill.Name);

        if (_restorationOfGlands.Data.IsOpen && _poisonBoneStack > 0 && _target.CharacterState.CheckForState(States.PoisonBone))
        {
            Debug.Log("SpitPoisProj / if Restoration = true");
            TargetRpcReductionCooldown();
        }

        if (numbersForChanceOfBlindness <= chanceOfBlindness)
        {
            _target.CharacterState.AddState(States.Blind, 6f, 0, _player.gameObject, _skill.Name);
        }

        DestroyProjectile();        
    }

    public void InitializationProjectile(Character dad, Skill skill, float energy,
        bool isActiveHealingSpitPoison, bool isPlayerInvisible, 
        bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies, int poisonBoneStack)
    {
        _player = dad;
        _energyDad = energy;
        _skill = skill;

        _poisonBoneStack = poisonBoneStack;

        _isActiveHealingSpitPoison = isActiveHealingSpitPoison;
        _isPlayerInvisible = isPlayerInvisible;
        _isPlayer = isTargetPlayer;
        _isAllies = isTargetAllies;
        _isEnemy = isTargetEnemy;
    }

    private void TargetRpcReductionCooldown()
    {
        float baseChanceOfRestorationOfGlands = 0.1f;
        float chanceRestorationOfGlands = baseChanceOfRestorationOfGlands * _poisonBoneStack;
        Debug.Log("SpitPoisProj / chanceRestoration = " + chanceRestorationOfGlands);

        if (Random.Range(0f, 1f) <= chanceRestorationOfGlands)
        {
            Debug.Log("SpitPoisonProj / If RestorationOfGlands.IsActive = true");
            _restorationOfGlands.ReductionCooldown();
        }
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
}
