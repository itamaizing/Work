using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CloudExplosion : Ability
{
    [SerializeField] private Character _dad;
    [SerializeField] private BonePoison _bonePoisonPrefab;
    [SerializeField] private LayerMask _enemyLayer;
    private PoisonCloudBuff _poisonCloud;
    private BonePoison _bonePoison;

    private float _baseDamage = 6.0f;
    private float _chanceApplyBonePoison = 0.3f;

    private float _currentDamage;
    private float _radiusExplosion;

    private HealthComponent _currentTarget;

    private DamageType _damageType = DamageType.Magical;
    private AttackRangeType _attackRangeType = AttackRangeType.MeleeAttack;

    private Coroutine _useAbilityCoroutine;
    private Coroutine _explosionCloudCoroutine;

    private int _currentStacksPoisonCloud { get; set; }
    private int _maxStacks { get; set; }

    public bool Enabled;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Cast()
    {
        _useAbilityCoroutine = StartCoroutine(UseCoroutine());
    }

    protected override void Cancel()
    {
        _currentTarget = null;
        _currentDamage = _baseDamage;

        if (_useAbilityCoroutine != null)
            StopCoroutine(UseCoroutine());

        if (_explosionCloudCoroutine != null)
            StopCoroutine(ExplosionCoroutine(_currentDamage));
    }

    private IEnumerator UseCoroutine()
    {
        if (_poisonCloud == null)
        {
            _poisonCloud = _dad.GetComponentInChildren<PoisonCloudBuff>();
            _currentStacksPoisonCloud = _poisonCloud.CurrentStacks;
            _radiusExplosion = _poisonCloud.RadiusCloud;
        }
        
        if (_currentStacksPoisonCloud != 0)
        {
            PayCost();
            _currentDamage = _baseDamage * _currentStacksPoisonCloud;
            _explosionCloudCoroutine = StartCoroutine(ExplosionCoroutine(_currentDamage));
        }
        else if (_currentStacksPoisonCloud == 0)
        {
            Cancel();
        }
        yield return null;
    }

    private IEnumerator ExplosionCoroutine(float currentDamage)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, _radiusExplosion, _enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            _currentTarget = enemy.gameObject.GetComponent<HealthComponent>();
            if (_currentTarget != null)
            {
                CmdApplyDamage(_currentTarget.gameObject, currentDamage, _damageType, _attackRangeType);
                for (int i = 1; i <= _currentStacksPoisonCloud; i++)
                {
                    if (Random.Range(0f, 1f) <= _chanceApplyBonePoison)
                    {
                        CmdApplyBonePoison(_currentTarget);
                    }
                }
            }
        }
        yield return null;
        Cancel();
    }

    [Command]
    private void CmdApplyBonePoison(HealthComponent targetHealth)
    {
        RpcApplyBonePoison(targetHealth);

        _bonePoison = targetHealth.GetComponentInChildren<BonePoison>();
        if (_bonePoison == null)
        {
            _bonePoison = Instantiate(_bonePoisonPrefab, targetHealth.transform);
            _bonePoison.AddStacks(targetHealth);
            _bonePoison.CurrentStacks = _currentStacksPoisonCloud;
            if (_bonePoison.CurrentStacks > _bonePoison.MaxStacks)
            {
                _bonePoison.CurrentStacks = _bonePoison.MaxStacks;
            }
        }
        else
        {
            _bonePoison.AddStacks(targetHealth);
            _bonePoison.CurrentStacks = _currentStacksPoisonCloud;
            if (_bonePoison.CurrentStacks > _bonePoison.MaxStacks)
            {
                _bonePoison.CurrentStacks = _bonePoison.MaxStacks;
            }
        }
    }

    [ClientRpc]
    private void RpcApplyBonePoison(HealthComponent targetHealth)
    {
        _bonePoison = targetHealth.GetComponentInChildren<BonePoison>();
        if (_bonePoison == null)
        {
            _bonePoison = Instantiate(_bonePoisonPrefab, targetHealth.transform);
            _bonePoison.AddStacks(targetHealth);
            if (_bonePoison.CurrentStacks > _bonePoison.MaxStacks)
            {
                _bonePoison.CurrentStacks = _bonePoison.MaxStacks;
            }
        }
        else
        {
            _bonePoison.AddStacks(targetHealth);
            _bonePoison.CurrentStacks = _currentStacksPoisonCloud;
            if (_bonePoison.CurrentStacks > _bonePoison.MaxStacks)
            {
                _bonePoison.CurrentStacks = _bonePoison.MaxStacks;
            }
        }
    }
}
