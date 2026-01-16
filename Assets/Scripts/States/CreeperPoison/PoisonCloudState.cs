using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class PoisonCloudState : AbstractCharacterState
{
    private List<Skill> _skills = new();
    private List<Talent> _talents = new();

    private CapaciousPoisonCloud _capaciousPoisonCloud;
    private ToxiqueCloud _toxiqueCloud;
    private ExplosionPoisonCloud _cloudExplosion;

    private Character _player;
    private LayerMask _enemiesLayer;

    private int _maxStacks = 5;

    private float _radiusCloud = 2.5f;

    private float _baseDamage = 0.005f;
    private float _increasedDamage;
    private float _endDamage;

    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1f;

    private float _timeBetweenApplyEmpathicPoisons;
    private float _startTimeBetweenApplyEmpathicPoisons = 2f;

    private float _baseDuration;
    private float _durationEmpathicPoisons = 5f;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Poison };
    public float RadiusCloud { get => _radiusCloud; }
    public override States State => States.PoisonCloud;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        MaxStacksCount = _maxStacks;

        _player = personWhoMadeBuff;

        _baseDuration = durationToExit;

        _timeBetweenAttack = _startTimeBetweenAttack;
        _timeBetweenApplyEmpathicPoisons = _startTimeBetweenApplyEmpathicPoisons;

        if (_player != null)
        {
            _skills = _player.CharacterState.Character.Abilities.Abilities;
            _talents = _player.CharacterState.Character.GetComponent<HeroComponent>().TalentManager.ActiveTalents;

            SearchAbilities();

            SearchTalent();
        }

        if (currentStacksCount < MaxStacksCount)
        {
            AddStacks();
        }
    }

    public override void UpdateState()
    {
        _timeBetweenAttack -= Time.deltaTime;

        _timeBetweenApplyEmpathicPoisons -= Time.deltaTime;

        if (_timeBetweenAttack <= 0)
        {
            RpcSearchingEnemies(_enemiesLayer, characterState.gameObject);

            _timeBetweenAttack = _startTimeBetweenAttack;
        }
    }

    public override void ExitState()
    {
        ResetValues();

        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            AddStacks(); 
            return true;
        }
        else
        {
            duration = _baseDuration;
            if (_cloudExplosion != null)
            {
                _cloudExplosion.CurrentStacksPoisonCloud(currentStacksCount, _radiusCloud);
            }
            return true;
        }
    }

    public void AddStacks()
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            duration = _baseDuration;
            if (_cloudExplosion != null)
            {
                _cloudExplosion.CurrentStacksPoisonCloud(currentStacksCount, _radiusCloud);
            }
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
                }
            }
            if (ability is CreeperStrike creeperStrike)
            {
                _enemiesLayer = creeperStrike.TargetsLayers;
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

                    if (_capaciousPoisonCloud.Data.IsOpen)
                    {
                        float multiplierRadiusCloud = 1.5f;

                        _radiusCloud += multiplierRadiusCloud;
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

    [ClientRpc]
    private void RpcSearchingEnemies(LayerMask enemyLayer, GameObject player)
    {
        Collider[] hitEnemies = Physics.OverlapSphere(player.transform.position, _radiusCloud, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.transform != player.transform)
            {
                Debug.Log("PoisonCloudState / enemy = " + enemy.name);
                CmdDamageDeal(enemy.gameObject);
            }
        }
    }

    [Command]
    private void CmdDamageDeal(GameObject target)
    {
        if (target != null)
        {
            var targetHealth = target.GetComponent<Character>();

            _increasedDamage = _baseDamage * currentStacksCount;
            _endDamage = targetHealth.Health.MaxValue * _increasedDamage;

            Damage damage = new Damage()
            {
                Value = _endDamage,
                Type = DamageType.Physical,
            };

            targetHealth.Health.CmdTryTakeDamage(damage, null);
            //targetHealth.DamageTracker.AddDamage(damage, true);

            if (_toxiqueCloud != null && _toxiqueCloud.Data.IsOpen)
            {
                if (_timeBetweenApplyEmpathicPoisons <= 0)
                {
                    targetHealth.CharacterState.AddState(States.EmpathicPoisons, _durationEmpathicPoisons, 0, _player.gameObject, null);
                    _timeBetweenApplyEmpathicPoisons = _startTimeBetweenApplyEmpathicPoisons;
                }
            }
        }
    }

    private void ResetValues()
    {
        currentStacksCount = 0;
        _baseDuration = 0;
        duration = 0;
        _endDamage = 0;
        _increasedDamage = 0;
        _baseDamage = 0.005f;
    }
}
