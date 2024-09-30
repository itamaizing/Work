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
        //Debug.Log("EnterState PoisonCloud");
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

        if (_capaciousPoisonCloud != null && _capaciousPoisonCloud.IsActive)
        {
            _radiusCloud += 1.5f;
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
                    //Debug.Log("PoisonCloud / enemiesLayer = " + _enemiesLayer.GetType());
                    //Debug.Log($"PoisonCloud / SearchAbilities / cloudExplosion = {_cloudExplosion}");
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
                }
            }
            if (talent is ToxiqueCloud toxiqueCloud)
            {
                if (_toxiqueCloud == null)
                {
                    _toxiqueCloud = toxiqueCloud;
                   // Debug.Log("ToxiqueCloud = " + _toxiqueCloud);
                }
            }
        }
    }

    public override void UpdateState()
    {
        _timeBetweenAttack -= Time.deltaTime;
        if (_timeBetweenAttack <= 0)
        {
            //Debug.Log("PoisonCloud / timeBetweenAttack <= 0");
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
        //Debug.Log("PoisonCloud / ExitState");
        ResetValues();

        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        //Debug.Log($"PoisonCloud / Stack / currentStacks = {_currentStacks}");

        if (_currentStacks < _maxStacks)
        {
            AddStacks();
            if (_cloudExplosion != null)
            {
                _cloudExplosion.CurrentStacksPoisonCloud(_currentStacks, _radiusCloud);
                //Debug.Log($"PoisonCloud / Stack / if / CurrentStacks/RadiusCloud = {_currentStacks}, {_radiusCloud}");
            }
            return true;
        }
        else
        {
            _duration = _baseDuration;
            if (_cloudExplosion != null)
            {
                _cloudExplosion.CurrentStacksPoisonCloud(_currentStacks, _radiusCloud);
                // Debug.Log($"PoisonCloud / Stack / else / CurrentStacks/RadiusCloud = {_currentStacks}, {_radiusCloud}");
            }
            return false;
        }
    }

    public void AddStacks()
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            _duration = _baseDuration;
            //Debug.Log("PoisonCloud AddStacks = " + _currentStacks);
            //  Debug.Log("if / CurrentStackPoisonCloud in AddStacks == " + _currentStacks); 
        }
        else
        {
            //Debug.Log("else / CurrentStackPoisonCloud in AddStacks == " + _currentStacks);
            _duration = _baseDuration;
        }
    }

    private void SearchingEnemies(LayerMask enemyLayer, GameObject player)
    {
       // Debug.Log($"PoisonCloud / SearchingEnemies");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(player.transform.position, 4, enemyLayer);

        //Debug.Log($"PoisonCloud / SearchingEnemies / hitEnemies = {hitEnemies.Length}");

        foreach (Collider2D enemy in hitEnemies)
        {
            //Debug.Log($"PoisonCloud / SearchingEnemies / enemy = {enemy}");

            if (enemy.transform != player.transform)
            {
                DamageDeal(enemy.gameObject);

                //Debug.Log("After TryGetComponent");
            }
            _timeBetweenAttack = _startTimeBetweenAttack;
        }
    }

    private void DamageDeal(GameObject target)
    {
        var targetHealth = target.GetComponent<Character>();
      //  Debug.Log($"PoisonCloud / DamageDeal");
        //Debug.Log($"PoisonCloud / DamageDeal / targetHealth = {targetHealth}");
        _increasedDamage = _baseDamage * _currentStacks;
        //Debug.Log($"PoisonCloud / DamageDeal / _increasedDamage = {_increasedDamage}");
        _endDamage = targetHealth.Health.MaxValue * _increasedDamage;
        //Debug.Log($"PoisonCloud / DamageDeal / _endDamage = {_endDamage}");

        Damage damage = new Damage()
        {
            Value = _endDamage,
            Type = DamageType.Physical,
            Range = AttackRangeType.MeleeAttack
        };

        //Debug.Log($"PoisonCloud / DamageDeal / damage = {damage}");

        targetHealth.Health.CmdTryTakeDamage(damage, null);

        if (_toxiqueCloud.IsActive)
        {
            //Debug.Log("PoisonCloud / DamageDeal / toxiqueCloud Active");
            ApplyState(targetHealth);
        }
    }

    [Command]
    private void ApplyState(Character targetHealth)
    {
       // Debug.Log("PoisonCloud / DamageDeal / toxiqueCloud Active");
        targetHealth.CharacterState.AddState(States.EmpathicPoisons, _durationEmpathicPoisons, 0, _player.gameObject, null);
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
