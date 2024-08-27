using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionPoisonCloud : Skill
{
    [SerializeField] private Character _player;
    private List<HeroComponent> _enemies = new();

    private int _currentStacksPoisonCloud;

    private float _baseDamage = 6.0f;
    private float _currentDamage;
    private float _chanceApplyBonePoison = 0.9f;
    private float _radiusExplosion = 4f;

    public bool Enabled;

    protected override bool IsCanCast => _player.CharacterState.CheckForState(States.PoisonCloud);

    protected override IEnumerator PrepareJob()
    {
        Debug.Log("PrepareJob / Check Nearest Enemies");
        _enemies.Clear();

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, _radiusExplosion, _targetsLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            _enemies.Add(enemy.gameObject.GetComponent<HeroComponent>());
        }
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        Debug.Log("CastJob Coroutine");
        Debug.Log($"Check Player PoisonCloudState == {_player.CharacterState.CheckForState(States.PoisonCloud)}");
        Debug.Log($"Enemies == {_enemies.Count}");
        if (_player.CharacterState.CheckForState(States.PoisonCloud))
        {
            TryPayCost();
            ExplosionCloud();
        }
        else
        {
            ClearData();
        }

        yield return null;
    }

    protected override void ClearData()
    {
        Debug.Log("Cancel");

        _currentDamage = 0;
        _enemies.Clear();
    }

    private void ExplosionCloud()
    {
        Debug.Log("ExplosionCloud");

        _currentDamage = _baseDamage * _currentStacksPoisonCloud;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_currentDamage),
            Type = DamageType.Physical,
            Range = AttackRangeType.MeleeAttack
        };

        Debug.Log($"CurrentDamage ExplosionCloud == {damage.Value}");
        foreach (HeroComponent target in _enemies)
        {
            if (target != null)
            {

                CmdApplyDamage(damage, target.gameObject);

                for (int i = 0; i < _currentStacksPoisonCloud; i++)
                {
                    if (Random.Range(0.0f, 1.0f) <= _chanceApplyBonePoison)
                    {
                        ApplyPoisonBone(target);
                    }
                }
            }
        }
        ClearData();
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
