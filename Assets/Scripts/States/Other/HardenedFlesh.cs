using System;
using System.Collections.Generic;
using UnityEngine;

public class HardenedFlesh : StackableState
{
    private List<StatusEffect> _effects = new() { StatusEffect.Destruction };

    public override States State => States.HardenedFlesh;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    private float _buffPercent = 5;

    private float _originalDefPhysDamage;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        abilities = character.Character.Abilities;
        base.personWhoMadeBuff = personWhoMadeBuff;

        if (currentStacksCount == 0) _originalDefPhysDamage = health.DefPhysDamage;

        duration = durationToExit;

        health.DefPhysDamage = _originalDefPhysDamage + _originalDefPhysDamage * _buffPercent;

        Debug.Log("Def " + health.DefPhysDamage);
    }

    protected override void ExitState()
    {
        health.DefPhysDamage = _originalDefPhysDamage;
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            duration = time;
            currentStacksCount++;
			health.DefPhysDamage = health.DefPhysDamage + _buffPercent;

			Debug.Log("Def " + health.DefPhysDamage);
			return false;
        }
        return false;
    }

    public override void UpdateState()
    {
    }
}
