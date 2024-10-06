using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpitPoisonProjectile : NetworkBehaviour
{
    [SerializeField] private Rigidbody2D _rbBall;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private GameObject _hitEffect;
    [SerializeField] private Collider2D _colliderBall;
    [SerializeField] private float _maxDistance = 5f;
    [SerializeField] private float _speed = 60f;

    private Skill _skill;
    private Character _player;
    private Vector2 _startPos;

    [SyncVar] private int _teamIndex;

    private float _energyDad;
    private float _damage;
    private float _lifeTimePoisonBoneStacks = 6.0f;

    private bool _isPlayer;
    private bool _isAllies;
    private bool _isEnemy;
    private bool _isActiveHealingSpitPoison;
    private bool _isPlayerInvisible;
    private bool _isAlly;

    private void Start()
    {
        if (isServer && _isPlayerInvisible)
        {
            RpcNewTransparencySprite();
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
                    _player.CharacterState.AddState(States.RegeneratingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                    Destroy(gameObject);
                }
            }
            else if (_isAllies)
            {
                if (_isAlly && collision.gameObject != _player.gameObject)
                {
                    if (collision.TryGetComponent<Character>(out var alliesHealth))
                    {
                        alliesHealth.CharacterState.AddState(States.RegeneratingPoison, 6.0f, 0, _player.gameObject, _skill.Name);
                        Destroy(gameObject);
                    }
                }
                else if (!_isAlly && collision.gameObject != _player.gameObject)
                {
                    return;
                }
            }   
            else if (_isEnemy)
            {
                if (collision.gameObject.transform != _player.transform && !_isAlly)
                {
                    if (collision.TryGetComponent<Character>(out var target))
                    {
                        _damage = Random.Range(4.0f, 12.0f);

                        DealDamage(target, _damage, DamageType.Magical, AttackRangeType.RangeAttack);
                    }
                }
                else if (_isAlly && collision.gameObject != _player.gameObject)
                {
                    return;
                }
            }    
            
        }
        else
        {
            if (collision.gameObject.transform != _player.transform && !_isAlly)
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
        float speed = (_speed / 100f) * 5f;
        _rbBall.DOMove(target, speed * _maxDistance / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(Explode);
    }

    public void MoveBallOnMaxDistance(Vector3 point)
    {
        Vector3 direction = (point - transform.position).normalized;

        StartCoroutine(MoveBallOnMaxDistanceCoroutine(direction, _speed));
    }

    private IEnumerator MoveBallOnMaxDistanceCoroutine(Vector3 direction, float speed)
    {
        while (true)
        {
            transform.position += direction * speed * Time.deltaTime;
            if (Vector3.Distance(transform.position, _player.transform.position) > _maxDistance * GlobalVariable.cellSize)
            {
                Explode();
            }
            yield return null;
        }
    }

    private void DealDamage(Character target, float currentDamage, DamageType damageType, AttackRangeType attackRangeType)
    {
        float chanceOfBlindness = 0.3f;
        float numbersForChanceOfBlindness = Random.Range(0.0f, 1.0f);

        Damage damage = new Damage
        {
            Value = _skill.Buff.Damage.GetBuffedValue(currentDamage),
            Type = DamageType.Physical,
            Range = AttackRangeType.RangeAttack,
        };
        target.Health.TryTakeDamage(ref damage, _skill);

        target.CharacterState.AddState(States.PoisonBone, _lifeTimePoisonBoneStacks, 0, _player.gameObject, _skill.Name);

        if (numbersForChanceOfBlindness <= chanceOfBlindness)
        {
            target.CharacterState.AddState(States.Blind, 6f, 0, _player.gameObject, _skill.Name);
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

        int teamIndex = _player.GetComponentInParent<UserNetworkSettings>().TeamIndex;
        _teamIndex = teamIndex;
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
}
