using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

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

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => _player.CharacterState.CheckForState(States.PoisonCloud);

    #region Talent

    private bool _isRestorativePoison = false;

    public void RestorativePoison(bool value) => _isRestorativePoison = value;

    #endregion

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        foreach (var item in targetInfo.GetTargets())
        {
            _enemies.Add((Character)item);
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        targetInfo.AddTarget(Hero);

        callbackDataSaved(targetInfo);
        yield break;
    }
    protected override IEnumerator CastJob()
    {
        if (_player.CharacterState.CheckForState(States.PoisonCloud))
        {
            FindEnemies();
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

    private void FindEnemies()
    {
        _enemies.Clear();

        Collider[] hitEnemies = Physics.OverlapSphere(
            transform.position,
            _radiusExplosion,
            _targetsLayers
        );

        foreach (Collider enemy in hitEnemies)
        {
            Character character = enemy.GetComponent<Character>();

            if (character != null && !_enemies.Contains(character))
            {
                _enemies.Add(character);
            }
        }
    }

    private void ExplosionCloud()
    {
        Debug.Log("ExplosionPoisonCloud / ExplosionCloud");
        
        _isExploded = true;

        _currentDamage = _baseDamage * _currentStacksPoisonCloud;

       Debug.Log("ExplosionPoisonCloud / ExplosionCloud / currentDamage = " + _currentDamage);

        foreach (Character target in _enemies)
        {
            Debug.Log("ExplosionPoisonCloud / ExplosionCloud / target = " + target);
            if (target != null)
            {
                CmdDamageDeal(target, _currentDamage);
                CmdApplyRestorativeHeal(target, _currentStacksPoisonCloud);

                //for (int i = 0; i < _currentStacksPoisonCloud; i++)
                //{
                //    if (Random.Range(0.0f, 1.0f) <= _chanceApplyBonePoison)
                //    {
                //        ApplyPoisonBone(target.gameObject);
                //    }
                //}
            }
        }

        //_currentStacksPoisonCloud = 0;
    }

    public void CurrentStacksPoisonCloud(int currentStacks, float radiusExplosion)
    {
        _currentStacksPoisonCloud = currentStacks;
        Debug.Log("ExplosionPoisonCloud / _currentStacksPoisonCloud = " + _currentStacksPoisonCloud);
        _radiusExplosion = radiusExplosion;
    }

    private void ApplyPoisonBone(GameObject target)
    {
        CmdApplyPoisonBone(target.gameObject);
    }

    [Command]
    private void CmdDamageDeal(Character target , float currentDamage)
    {
        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(currentDamage),
            Type = DamageType.Physical,
        };
        
        ApplyDamage(damage, target.gameObject);
    }

    [Command]
    private void CmdApplyPoisonBone(GameObject target)
    {
        target.GetComponent<CharacterState>().AddState(States.PoisonBone, 6f, 0, _player.gameObject, Name);
    }

    [Command]
    private void CmdApplyRestorativeHeal(Character target, int cloudStacks)
    {
        if (!_isRestorativePoison) return;

        var state = target.CharacterState.GetState(States.RegeneratingPoison) as RegeneratingPoisonState;

        if (state == null) return;

        int regenStacks = state.CurrentStacksCount;

        float healValue = cloudStacks * 5f * regenStacks;

        Heal heal = new Heal
        {
            Value = healValue,
            DamageableSkill = null
        };

        CmdApplyHeal(heal, target.gameObject, this, Name);
    }
}
