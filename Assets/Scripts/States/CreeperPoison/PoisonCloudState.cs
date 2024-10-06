using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonCloudState : AbstractCharacterState
{
    public bool turnOff = false;

    private List<Skill> _skills = new();
    private List<Talent> _talents = new();

    private CapaciousPoisonCloud _capaciousPoisonCloud;
    private ToxiqueCloud _toxiqueCloud;
    private ExplosionPoisonCloud _cloudExplosion;

    private Character _player;
    private LayerMask _enemiesLayer;

    private int _currentStacks = 0;
    private int _maxStacks = 5;

    private float _radiusCloud = 2.5f;

    private float _baseDamage = 0.005f;
    private float _increasedDamage;
    private float _endDamage;

    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1f;

    private float _duration;
    private float _baseDuration;
    private float _durationEmpathicPoisons = 5f;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };

    public float RadiusCloud { get => _radiusCloud; }
    public override States State => States.PoisonCloud;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _player = personWhoMadeBuff;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        _timeBetweenAttack = _startTimeBetweenAttack;

        if (_player != null)
        {
            _skills = _player.CharacterState.Character.Abilities.Abilities;
            _talents = _player.CharacterState.Character.GetComponent<HeroComponent>().Talents.Talents;

            SearchAbilities();

            SearchTalent();
        }

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
        }
    }

    private void SearchAbilities()
    {
        foreach (Skill ability in _skills)
        {
            if (ability is ExplosionPoisonCloud cloudExplosion)
            {
                if (_cloudExplosion == null)
                {
                    _cloudExplosion = cloudExplosion;
                    _enemiesLayer = _cloudExplosion.TargetsLayers;
                }
            }
        }
    }

    private void SearchTalent()
    {
        foreach (Talent talent in _talents)
        {
            if (talent is CapaciousPoisonCloud capaciousCloud)
            {
                if (_capaciousPoisonCloud == null)
                {
                    _capaciousPoisonCloud = capaciousCloud;
                    if (_capaciousPoisonCloud.IsActive)
                    {
                        _radiusCloud += 1.5f;
                    }
                }
            }
            if (talent is ToxiqueCloud toxiqueCloud)
            {
                if (_toxiqueCloud == null)
                {
                    _toxiqueCloud = toxiqueCloud;
                }
            }
        }
    }

    public override void UpdateState()
    {
        _timeBetweenAttack -= Time.deltaTime;
        if (_timeBetweenAttack <= 0)
        {
            SearchingEnemies(_enemiesLayer, _characterState.gameObject);
            _timeBetweenAttack = _startTimeBetweenAttack;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            AddStacks(); 
            return true;
        }
        else
        {
            _duration = _baseDuration;
            if (_cloudExplosion != null)
            {
                _cloudExplosion.CurrentStacksPoisonCloud(_currentStacks, _radiusCloud);
            }
            return true;
        }
    }

    public void AddStacks()
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            _duration = _baseDuration;
            if (_cloudExplosion != null)
            {
                _cloudExplosion.CurrentStacksPoisonCloud(_currentStacks, _radiusCloud);
            }
        }
    }

    private void SearchingEnemies(LayerMask enemyLayer, GameObject player)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(player.transform.position, 4, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.transform != player.transform)
            {
                DamageDeal(enemy.gameObject);
            }
        }
    }

    private void DamageDeal(GameObject target)
    {
        var targetHealth = target.GetComponent<Character>();

        _increasedDamage = _baseDamage * _currentStacks;
        _endDamage = targetHealth.Health.MaxValue * _increasedDamage;

        Damage damage = new Damage()
        {
            Value = _endDamage,
            Type = DamageType.Physical,
            Range = AttackRangeType.MeleeAttack
        };

        targetHealth.Health.CmdTryTakeDamage(damage, null);

        if (_toxiqueCloud.IsActive)
        {
            Debug.Log("PoisonCloud / DamageDeal / toxiqueCloud Active");
            ApplyState(targetHealth);
        }
    }

    private void ApplyState(Character targetHealth)
    {
       // Debug.Log("PoisonCloud / DamageDeal / toxiqueCloud Active");
        targetHealth.CharacterState.CmdAddState(States.EmpathicPoisons, _durationEmpathicPoisons, 0, _player.gameObject, null);
    }

    private void ResetValues()
    {
        _currentStacks = 0;
        _baseDuration = 0;
        _duration = 0;
        _endDamage = 0;
        _increasedDamage = 0;
        _baseDamage = 0.005f;
    }
}
