using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ExplosionPoisonCloud : Skill, IColdBloodEmpowerable
{
    [SerializeField] private Character _player;
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;
    [SerializeField] private float _explosionDamage = 6f;
    [SerializeField] private float _healPerStack = 3f;
    [SerializeField] private float _explosionRadius = 4f;

    private List<Character> _enemies = new();

    private int _currentStacksPoisonCloud;
    private int _currentStacksHealingPoisonCloud;

    
    private float _baseDamage_OLD = 6.0f;
    private float _currentDamage;
    private float _chanceApplyBonePoison = 0.9f;
    private float _radiusExplosion = 4f;
    private float _healValue = 3f;
    private float _additionalHealValue = 5f;

    private float _currentHealValue;

    private bool _isExploded = false;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerPrepare => 0;
    protected override bool IsCanCast => _player.CharacterState.CheckForState(States.PoisonCloud) || _player.CharacterState.CheckForState(States.HealingPoisonCloud);

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

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        targetInfo.AddTarget(Hero);

        callbackDataSaved(targetInfo);
        yield break;
    }
    protected override IEnumerator CastJob()
    {
        FindEnemies();
        ExplosionCloud();

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
            Targeting.InteractionMask
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

    private float CalculateHeal(Character target)
    {
        int cloudStacks = _player.CharacterState.CheckStateStacks(States.HealingPoisonCloud);
        if (cloudStacks <= 0) return 0f;

        float perStack = _healValue;

        if (_isRestorativePoison)
        {
            int regenStacks = target.CharacterState.CheckStateStacks(States.RegeneratingPoison);
            perStack += _additionalHealValue * regenStacks;
        }

        return perStack * cloudStacks;
    }

    private void ExplosionCloud()
    {
        _isExploded = true;

        var state = _player.CharacterState;
        bool hasPoison = state.CheckStateStacks(States.PoisonCloud) > 0;

        bool crit = false;
        if (hasPoison)
        {
            FindEnemies();
            crit = _hero.Abilities.GetSkill<ColdBlood>().TryConsumeForPoison(_enemies);
        }

        CmdExplode(crit);
    }
    
    [Command]
    private void CmdExplode(bool crit)
    {
        var state = _player.CharacterState;
        int poisonStacks = state.CheckStateStacks(States.PoisonCloud);
        int healStacks = state.CheckStateStacks(States.HealingPoisonCloud);
        if (poisonStacks <= 0 && healStacks <= 0) return;

        int team = _player.NetworkSettings.TeamIndex;
        float damageValue = Buff.Damage.GetBuffedValue(_explosionDamage) * (crit ? ColdBlood.PoisonCritMultiplier : 1f);
        float healValue = _healPerStack * healStacks;

        var visited = new HashSet<Character>();
        foreach (var col in Physics.OverlapSphere(_player.transform.position, _explosionRadius))
        {
            if (!col.TryGetComponent<Character>(out var target)) continue;
            if (!visited.Add(target) || target.IsDead) continue;

            bool ally = target.NetworkSettings.TeamIndex == team;

            if (!ally && poisonStacks > 0)
            {
                var damage = new Damage { Value = damageValue, Type = DamageType.Physical };
                ApplyDamage(damage, target.gameObject);
            }
            else if (ally && healStacks > 0)
            {
                var heal = new Heal { Value = healValue, DamageableSkill = null };
                ApplyHeal(heal, target.gameObject, this, Name);
            }
        }

        if (_creeperPoisonAura != null) _creeperPoisonAura.ConsumeClouds();
    }


    public void CurrentStacksHealingPoisonCloud(int currentStacks, float radiusExplosion)
    {
        _currentStacksHealingPoisonCloud = currentStacks;
        Debug.Log("ExplosionPoisonCloud / _currentStacksHealingPoisonCloud = " + _currentStacksHealingPoisonCloud);
        _radiusExplosion = radiusExplosion;
    }
}
