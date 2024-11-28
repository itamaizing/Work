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

    private float _timeBetweenHeal;
    private float _startTimeBetweenHeal = 1f;

    private static float _duration;
    private static float _baseDuration;

    private LayerMask _alliesLayer;
    private Character _player;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Healing };

    public override States State => States.HealingPoisonCloud;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _player = _characterState.Character;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        MaxStacksCount = _maxStacks;

        _alliesLayer = 9;

        Debug.Log("HealingPoisonCloudState / alliesLayer = " + _alliesLayer.value);
        if (CurrentStacksCount < MaxStacksCount)
        {
            AddStacks();
        }
    }

    public override void UpdateState()
    {

        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal <= 0)
        {
            RpcSearchingEnemies(_alliesLayer, _player.gameObject);
            _timeBetweenHeal = _startTimeBetweenHeal;
        }

        _duration -= Time.deltaTime;
        if (_duration < 0)
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
        if (CurrentStacksCount < MaxStacksCount)
        {
            AddStacks();
            return true;
        }
        else
        {
            _duration = _baseDuration;
            return true;
        }
    }

    public void AddStacks()
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            _duration = _baseDuration;
        }
        else
        {
            _duration = _baseDuration;
        }
    }

    [ClientRpc]
    private void RpcSearchingEnemies(LayerMask alliesLayer, GameObject player)
    {
        Collider[] hitsAllies = Physics.OverlapSphere(player.transform.position, _radiusCloud, alliesLayer);

        foreach (Collider alliesOrPlayer in hitsAllies)
        {
            Debug.Log("HealingPoisonCloud / SearchingEnemies / alliesOrPlayer = " + alliesOrPlayer.name);
            if (alliesOrPlayer != null)
            {
                if (alliesOrPlayer.TryGetComponent<Character>(out var target))
                {
                    Debug.Log("HealingPoisonCloud / SearchingEnemies / if TryGetComponent");
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

        _increasedHeal = _baseHeal * CurrentStacksCount;
        _endHeal = targetCharacter.Health.MaxValue * _increasedHeal;

        Debug.Log("HealingPoisonCloud / CmdApplyHealing / endHeal = " + _endHeal);

        Heal heal = new Heal
        {
            Value = _endHeal,
            DamageableSkill = null,
        };

        Debug.Log("HealingPoisonCloud / CmdApplyHealing / heal = " + heal);

        targetCharacter.Health.Heal(ref heal, null);
        //targetHealth.DamageTracker.AddHeal(heal);
    }

    private void ResetValues()
    {
        CurrentStacksCount = 0;
        _baseDuration = 0;
        _duration = 0;
        _endHeal = 0;
        _increasedHeal = 0;
        _baseHeal = 0.005f;
    }
}
