using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpitPoisonProjectile : Test_Projectile
{
    private Skill _skill;

    private int _teamIndex;

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
        if (isServer && _isPlayerInvisible)
        {
            RpcNewTransparencySprite(_player.gameObject);
        }
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
                    Debug.Log("Player");
                    _player.CharacterState.AddState(States.RegeneratingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                    Destroy(gameObject);
                }
            }
            else if (_isAllies)
            {
                if (collision.gameObject != _player.gameObject)
                {
                    if (collision.TryGetComponent<Character>(out var alliesHealth))
                    {
                        Debug.Log("Allies");
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
                if (collision.transform != _player.transform)
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
                if (collision.gameObject != _player.gameObject)
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
            Range = AttackRangeType.RangeAttack,
        };
        _target.Health.TryTakeDamage(ref _baseDamage, _skill);

        _target.CharacterState.AddState(States.PoisonBone, _lifeTimePoisonBoneStacks, 0, _player.gameObject, _skill.Name);

        if (numbersForChanceOfBlindness <= chanceOfBlindness)
        {
            _target.CharacterState.AddState(States.Blind, 6f, 0, _player.gameObject, _skill.Name);
        }

        DestroyProjectile();
    }

    public void InitializationProjectile(Character dad, Skill skill, float energy,
        bool isActiveHealingSpitPoison, bool isPlayerInvisible, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        _player = dad;
        _energyDad = energy;
        _skill = skill;

        _isActiveHealingSpitPoison = isActiveHealingSpitPoison;
        _isPlayerInvisible = isPlayerInvisible;
        _isPlayer = isTargetPlayer;
        _isAllies = isTargetAllies;
        _isEnemy = isTargetEnemy;


    }

    [ClientRpc]
    private void RpcNewTransparencySprite(GameObject player)
    {
        int playerLayer = player.layer;

        Color originalColor = _projectileSprite.color;

        if (_projectileSprite != null)
        {
            if (playerLayer == LayerMask.NameToLayer("Allies"))
            {
                Color newTransparencySprite = originalColor;
                newTransparencySprite.a = 0.5f;
                _projectileSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, newTransparencySprite.a);
            }
            else if (playerLayer == LayerMask.NameToLayer("Enemy"))
            {
                Color newTransparencySprite = originalColor;
                newTransparencySprite.a = 0.0f;
                _projectileSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, newTransparencySprite.a);
            }
        }
    }
}
