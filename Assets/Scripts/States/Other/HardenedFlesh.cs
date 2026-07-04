using System;
using System.Collections.Generic;
using UnityEngine;

public class HardenedFlesh : AbstractCharacterState
{
    private List<StatusEffect> _effects = new() { StatusEffect.Destruction };

    public override States State => States.HardenedFlesh;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    private float _buffPercent = 5;
    private int _currentStacks = 0;
    private const int _maxStacks = 5;

    private float _originalDefPhysDamage;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        abilities = character.Character.Abilities;
        base.personWhoMadeBuff = personWhoMadeBuff;

        if (_currentStacks == 0) _originalDefPhysDamage = health.DefPhysDamage;

        duration = durationToExit;

        health.DefPhysDamage = _originalDefPhysDamage + _originalDefPhysDamage * _buffPercent;

        Debug.Log("Def " + health.DefPhysDamage);
    }

    public override void ExitState()
    {
        health.DefPhysDamage = _originalDefPhysDamage;
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            duration = time;
            _currentStacks++;
			health.DefPhysDamage = health.DefPhysDamage + _buffPercent;

			Debug.Log("Def " + health.DefPhysDamage);
			return false;
        }
        return false;
    }

    public override void UpdateState()
    {
        duration -= Time.deltaTime;

        if (duration <= 0)
        {
            ExitState();
            return;
        }
    }
}
