using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionPoisonCloud : Ability
{
    [SerializeField] private Character _player;

    private int _currentStacksPoisonCloud;
    private int _maxStacks;

    private float _baseDamage = 6.0f;
    private float _currentDamage;
    private float _chanceApplyBonePoison = 0.9f;
    private float _radiusExplosion = 4f;

    private DamageType _damageType = DamageType.Magical;
    private AttackRangeType _attackRangeType = AttackRangeType.MeleeAttack;

    private List<HeroComponent> _enemies = new();

    private Coroutine _useAbilityCoroutine;
    private Coroutine _checkEnemyCoroutine;

    public bool Enabled;

    protected override void Cast()
    {
        Debug.Log("Cast");
        _useAbilityCoroutine = StartCoroutine(UseCoroutine());
    }

    protected override void Cancel()
    {
        Debug.Log("Cancel");

        _currentDamage = 0;
        _enemies.Clear();

        if (_useAbilityCoroutine != null)
        {
            StopCoroutine(UseCoroutine());
            _useAbilityCoroutine = null;
        }
        if (_checkEnemyCoroutine != null)
        {
            StopCoroutine(CheckEnemy());
            _checkEnemyCoroutine = null;
        }

    }

    private IEnumerator UseCoroutine()
    {
        Debug.Log("UseCoroutine");
        yield return _checkEnemyCoroutine = StartCoroutine(CheckEnemy());
        Debug.Log($"Check Player PoisonCloudState == {_player.CharacterState.CheckForState(States.PoisonCloud)}");
        Debug.Log($"Enemies == {_enemies.Count}");
        if (_player.CharacterState.CheckForState(States.PoisonCloud))
        {
            PayCost();
            ExplosionCloud();
        }
        else
        {
            Cancel();
        }
    }

    private void ExplosionCloud()
    {
        Debug.Log("ExplosionCloud");
        _currentDamage = _baseDamage * _currentStacksPoisonCloud;
        Debug.Log($"CurrentDamage ExplosionCloud == {_currentDamage}");
        foreach (HeroComponent target in _enemies)
        {
            if (target != null)
            {
                CmdApplyDamage(target.gameObject, _currentDamage, _damageType, _attackRangeType);
                for (int i = 0; i < _currentStacksPoisonCloud; i++)
                {
                    if (Random.Range(0.0f, 1.0f) <= _chanceApplyBonePoison)
                    {
                        ApplyPoisonBone(target);
                    }
                }
            }
        }
        Cancel();
    }

    private IEnumerator CheckEnemy()
    {
        Debug.Log("CheckEnemy");
        _enemies.Clear();
        int combinedLayers = 0;
        foreach (LayerMask layer in _targetsLayers)
        {
            combinedLayers |= layer;
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, _radiusExplosion, combinedLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            _enemies.Add(enemy.gameObject.GetComponent<HeroComponent>());
        }
        yield return null;
    }

    public void CurrentStacksPoisonCloud(int currentStacks, float radiusExplosion)
    {
        Debug.Log("CurrentStacks");
        _currentStacksPoisonCloud = currentStacks;
        _radiusExplosion = radiusExplosion;
        Debug.Log("CloudExplosion currentStacks == " + _currentStacksPoisonCloud);
        Debug.Log("CloudExplosion _radiusExplosion == " + _radiusExplosion);
    }

    private void ApplyPoisonBone(HeroComponent target)
    {
        CmdApplyPoisonBone(target);
    }

    [Command]
    private void CmdApplyPoisonBone(HeroComponent target)
    {
        target.CharacterState.CmdAddState(States.PoisonBone, 6f, 0);
    }
}
