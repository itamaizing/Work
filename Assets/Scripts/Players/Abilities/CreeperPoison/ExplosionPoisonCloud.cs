using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class ExplosionPoisonCloud : Skill
{
    [SerializeField] private Character _player;
    private List<Character> _enemies = new();

    private int _currentStacksPoisonCloud;

    private float _baseDamage = 6.0f;
    private float _currentDamage;
    private float _chanceApplyBonePoison = 0.9f;
    private float _radiusExplosion = 4f;

    private bool _isExploded = false;

    private Coroutine _searchingEnemiesCoroutine;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => _player.CharacterState.CheckForState(States.PoisonCloud);

    protected override IEnumerator PrepareJob()
    {
        if (_searchingEnemiesCoroutine == null)
        {
            //Debug.Log("ExplosionPoisonCloud / PrepareJob / searchingEnemies == null");
            _searchingEnemiesCoroutine = StartCoroutine(SearchingenemiesJob());
        }

        while (_enemies.Count < 0)
        {
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_player.CharacterState.CheckForState(States.PoisonCloud))
        {
            ExplosionCloud();
        }

        yield return null;
    }

    protected override void ClearData()
    {
        //Debug.Log("ExplosionPoisonCloud / ClearData");
        _isExploded = false;
        _currentDamage = 0;
        _enemies.Clear();
    }

    private void ExplosionCloud()
    {
        //Debug.Log("ExplosionPoisonCloud / ExplosionCloud");
        
        _isExploded = true;

        _currentDamage = _baseDamage * _currentStacksPoisonCloud;

       // Debug.Log("ExplosionPoisonCloud / ExplosionCloud / currentDamage = " + _currentDamage);
        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_currentDamage),
            Type = DamageType.Physical,
        };

        foreach (Character target in _enemies)
        {
           // Debug.Log("ExplosionPoisonCloud / ExplosionCloud / target = " + target);
            if (target != null)
            {
                CmdApplyDamage(damage, target.gameObject);
                target.DamageTracker.AddDamage(damage);

                for (int i = 0; i < _currentStacksPoisonCloud; i++)
                {
                    if (Random.Range(0.0f, 1.0f) <= _chanceApplyBonePoison)
                    {
                        ApplyPoisonBone(target.gameObject);
                    }
                }
            }
        }

        if (_searchingEnemiesCoroutine != null)
        {
            StopCoroutine(_searchingEnemiesCoroutine);
            _searchingEnemiesCoroutine = null;
        }

        _currentStacksPoisonCloud = 0;
    }

    private IEnumerator SearchingenemiesJob()
    {
        while (!_isExploded)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, _radiusExplosion, _targetsLayers);
            foreach (Collider2D enemy in hitEnemies)
            {
                _enemies.Add(enemy.gameObject.GetComponent<Character>());
               // Debug.Log("ExplosionPoisonCloud / _enemies.Count = " + _enemies.Count);
            }
            yield return null;
        }
    }

    public void CurrentStacksPoisonCloud(int currentStacks, float radiusExplosion)
    {
        _currentStacksPoisonCloud = currentStacks;
        //Debug.Log("ExplosionPoisonCloud / _currentStacksPoisonCloud = " + _currentStacksPoisonCloud);
        _radiusExplosion = radiusExplosion;
    }

    private void ApplyPoisonBone(GameObject target)
    {
        CmdApplyPoisonBone(target.gameObject);
    }

    [Command]
    private void CmdApplyPoisonBone(GameObject target)
    {
        target.GetComponent<CharacterState>().AddState(States.PoisonBone, 6f, 0, _player.gameObject, Name);
    }
}
