using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class HealingPoisonCloudState : AbstractCharacterState
{

    private int _maxStacks = 5;
    private float _radiusCloud = 2.5f;

    private float _baseHeal = 0.005f;
    private float _increasedHeal;
    private float _endHeal;

    private ExplosionPoisonCloud _explosion;

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1f;

    private static float _baseDuration;

    private LayerMask _alliesLayer;

    private List<Skill> _skills = new();
    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Healing };

    public override States State => States.HealingPoisonCloud;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _baseDuration = durationToExit;

        MaxStacksCount = _maxStacks;
        
        if (characterState != null)
        {
            _skills = characterState.Character.Abilities.Abilities;

            SearchAbilities();
        }

        if (currentStacksCount < MaxStacksCount)
        {
            AddStacks();
        }
    }

    public override void UpdateState()
    {

        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
            RpcSearchingEnemies(_alliesLayer, characterState.gameObject);
            _timeBetweenHeal = _startTimeBetweenHeal;
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
            return true;
        }

        if (_explosion != null)
        {
            _explosion.CurrentStacksHealingPoisonCloud(currentStacksCount, _radiusCloud);
        }
    }

    public void AddStacks()
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            duration = _baseDuration;
        }
        else
        {
            duration = _baseDuration;
        }

        if (_explosion != null)
        {
            _explosion.CurrentStacksHealingPoisonCloud(currentStacksCount, _radiusCloud);
        }
    }

    private void SearchAbilities()
    {
        foreach (Skill ability in _skills)
        {
            if (ability is CreeperInvisible creeperInvisible)
            {
                if (creeperInvisible != null)
                {
                    _alliesLayer = creeperInvisible.Targeting.Layer;
                }
            }

            if (ability is ExplosionPoisonCloud explosion)
            {
                _explosion = explosion;
            }

        }
    }

    [ClientRpc]
    private void RpcSearchingEnemies(LayerMask alliesLayer, GameObject player)
    {
        Collider[] hitsAllies = Physics.OverlapSphere(player.transform.position, _radiusCloud, alliesLayer);

        foreach (Collider alliesOrPlayer in hitsAllies)
        {
            if (alliesOrPlayer != null)
            {
                if (alliesOrPlayer.TryGetComponent<Character>(out var target))
                {
                    CmdApplyHealing(target.gameObject);

                    _timeBetweenHeal = _startTimeBetweenHeal;
                }
            }
        }
    }

    [Command]
    private void CmdApplyHealing(GameObject target)
    {
        Character targetCharacter = target.GetComponent<Character>();

        _increasedHeal = _baseHeal * currentStacksCount;
        _endHeal = targetCharacter.Health.MaxValue * _increasedHeal;

        Heal heal = new Heal
        {
            Value = _endHeal,
            DamageableSkill = null,
        };

        targetCharacter.Health.Heal(ref heal, null);
        //targetHealth.DamageTracker.AddHeal(heal);
    }

    private void ResetValues()
    {
        currentStacksCount = 0;
        _baseDuration = 0;
        duration = 0;
        _endHeal = 0;
        _increasedHeal = 0;
        _baseHeal = 0.005f;
    }
}
