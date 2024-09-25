using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperInvisibleState : AbstractCharacterState
{
    public bool turnOff = false;

    private List<Skill> _skills = new();
    private CreeperInvisible _creeperInvisible;
    private Character _player;

    private float _reductionMoveSpeed = 0.3f;
    private float _originalMoveSpeed;
    private float _increaseStaminaRegen = 0.3f;
    private float _originalStaminaRegen;

    private static bool _isIncreasedManaCost = false;
    private bool _isInvisible;
    private bool _isPlayerInvisability;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };
    public override States State => States.CreeperInvisible;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("EnterState CreeperInvisible");

        _characterState = character;
        _player = _characterState.Character;

        _originalMoveSpeed = _player.Move.CurrentSpeed;
        _originalStaminaRegen = _player.Stamina.RegenerationValue;

        if (_player != null)
        {
            _skills = _player.CharacterState.Character.Abilities.Abilities;
            foreach (Skill ability in _skills)
            {
                if (ability is CreeperInvisible creeperInvisible)
                {
                    if (_creeperInvisible == null)
                    {
                        _creeperInvisible = creeperInvisible;
                    }
                }
            }
        }
    }

    public override void UpdateState()
    {
        _isInvisible = _creeperInvisible.IsInvisible;
        //Debug.Log($"CreeperInvisible / _isInvisible = {_isInvisible}");
        if (_isInvisible)
        {
            if (!_isPlayerInvisability)
            {
                ApplyInvisible();
            }
        }
        else
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        if (_isPlayerInvisability || !_isPlayerInvisability)
        {
            _isPlayerInvisability = false;
            ResetValues();
            _characterState.RemoveState(this);
        }
    }

    public override bool Stack(float time)
    {
        return false;
    }

    private void ApplyInvisible()
    {
        _isPlayerInvisability = true;

        float reductionMoveSpeed = _originalMoveSpeed * _reductionMoveSpeed;
        float endReductionMoveSpeed = _originalMoveSpeed - reductionMoveSpeed;
        _player.Move.SetMoveSpeed(endReductionMoveSpeed);
        Debug.Log("Player MoveSpeed == " + _player.Move.CurrentSpeed);

        _player.Stamina.RegenerationValue *= (1 + _increaseStaminaRegen);
        Debug.Log("Player StaminaRegen == " + _player.Stamina.RegenerationValue);

        if (!_isIncreasedManaCost)
        {
            foreach (Skill ability in _skills)
            {
                ability.Buff.ManaCost.IncreasePercentage(1.3f);
                Debug.Log("Ability manaCost == " + ability.Buff.ManaCost.Multiplier);
                Debug.Log("Modified manaCost at ability: " + ability.name + ", Type: " + ability.GetType() + ", ManaCost Value = " + ability.Buff.ManaCost);
                Debug.Log("IsIncreasedManaCost in Search Abilities== " + _isIncreasedManaCost);
            }
            _isIncreasedManaCost = true;
        }
    }

    private void ResetValues()
    {
        _player.Move.SetDefaultSpeed();
        Debug.Log("Player MoveSpeed == " + _player.Move.CurrentSpeed);

        if (_player.Stamina.RegenerationValue != _originalStaminaRegen)
        {
            _player.Stamina.RegenerationValue /= (1 + _increaseStaminaRegen);
            Debug.Log("Player StaminaRegen == " + _player.Stamina.RegenerationValue);
        }

        if (_isIncreasedManaCost)
        {
            foreach (Skill ability in _skills)
            {
                ability.Buff.ManaCost.ReductionPercentage(1.3f);
                Debug.Log("Ability manaCost == " + ability.Buff.ManaCost.Multiplier);
                Debug.Log("Modified manaCost at ability: " + ability.name + ", Type: " + ability.GetType() + ", ManaCost Value = " + ability.Buff.ManaCost);
            }
            _isIncreasedManaCost = false;
            Debug.Log("IsIncreasedManaCost in ResetValues == " + _isIncreasedManaCost);
        }

        _isPlayerInvisability = false;
    }
}
