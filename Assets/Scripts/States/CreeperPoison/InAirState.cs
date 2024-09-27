using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InAirState : AbstractCharacterState
{
    public bool turnOff = false;

    private float _duration;
    private float _baseDuration;
    private float _damageToExit;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };
    public override States State => States.InAir;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("InAirState / EnterState");
        _characterState = character;

        if (character.TryGetComponent<Character>(out var ability))
        {
            _abilities = ability.Abilities;
            _abilities.SetAbilitiesDisabled();
        }
        else
        {
            Debug.Log("no ability at " + character.gameObject.name);
        }

        _characterState.Character.Move.CanMove = false;
        _duration = durationToExit;
        _baseDuration = _duration;
        _baseDuration = durationToExit;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }


    public override void ExitState()
    {
        if (_characterState.Check(StatusEffect.Move))
        {
            _characterState.Character.Move.CanMove = true;
        }
        if (_characterState.Check(StatusEffect.Ability) && _abilities != null)
        {
            _abilities.SetAbilitiesEnabled();
        }
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }
}
