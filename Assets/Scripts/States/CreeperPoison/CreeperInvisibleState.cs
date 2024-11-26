using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreeperInvisibleState : AbstractCharacterState
{
    private List<Skill> _skills = new();
    private CreeperInvisible _creeperInvisible;
    private Character _player;
    private SpriteRenderer _playerSprite;

    private float _reductionMoveSpeed = 0.3f;
    private float _originalMoveSpeed;
    private float _increaseStaminaRegen = 0.3f;
    private float _originalStaminaRegen;
    private float _timeBetweenReducingTransparency;
    private float _startTimeBetweenReducingTransparency = 0.5f;

    private static bool _isIncreasedManaCost = false;
    private bool _isInvisible;
    private bool _isPlayerInvisability;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };
    public override float TEST_ChangeableValue { get; set; }
    public override States State => States.CreeperInvisible;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _player = _characterState.Character;

        _playerSprite = _player.GetComponentInChildren<SpriteRenderer>();
        _originalMoveSpeed = _player.Move.DefaultSpeed;
        _originalStaminaRegen = _player.Resources.FirstOrDefault()!.RegenerationValue;

        _timeBetweenReducingTransparency = _startTimeBetweenReducingTransparency;
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

        if (_isInvisible)
        {
            _timeBetweenReducingTransparency -= Time.deltaTime;
            if (_timeBetweenReducingTransparency <= 0f)
            {
                //ReducingTransparencySprite();
                _timeBetweenReducingTransparency = _startTimeBetweenReducingTransparency;
            }

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

    private void ReducingTransparencySprite()
    {
        Color newTransparency = _playerSprite.color;

        newTransparency.a -= 10f * Time.deltaTime;

        newTransparency.a = Mathf.Clamp(newTransparency.a, 0.5f, 1f);

        _playerSprite.color = new Color(1f, 1f, 1f, newTransparency.a);
    }

    private void ApplyInvisible()
    {
        _isPlayerInvisability = true;

        float reductionMoveSpeed = _originalMoveSpeed * _reductionMoveSpeed;

        float endReductionMoveSpeed = _originalMoveSpeed - reductionMoveSpeed;

        _player.Move.SetMoveSpeed(endReductionMoveSpeed);
        // Debug.Log("Player MoveSpeed == " + _player.Move.CurrentSpeed);

       _player.Resources.FirstOrDefault()!.RegenerationValue *= (1 + _increaseStaminaRegen);
        //Debug.Log("Player StaminaRegen == " + _player.Stamina.RegenerationValue);

        if (!_isIncreasedManaCost)
        {
            foreach (Skill ability in _skills)
            {
                ability.Buff.ManaCost.IncreasePercentage(1.3f);
                // Debug.Log("Ability manaCost == " + ability.Buff.ManaCost.Multiplier);
                // Debug.Log("Modified manaCost at ability: " + ability.name + ", Type: " + ability.GetType() + ", ManaCost Value = " + ability.Buff.ManaCost);
                //Debug.Log("IsIncreasedManaCost in Search Abilities== " + _isIncreasedManaCost);
            }
            _isIncreasedManaCost = true;
        }
    }

    private void ResetValues()
    {
        _player.Move.SetDefaultSpeed();
        // Debug.Log("Player MoveSpeed == " + _player.Move.CurrentSpeed);

        if (_player.Resources.FirstOrDefault()!.RegenerationValue != _originalStaminaRegen)
        {
            _player.Resources.FirstOrDefault()!.RegenerationValue /= (1 + _increaseStaminaRegen);
            //Debug.Log("Player StaminaRegen == " + _player.Stamina.RegenerationValue);
        }

        if (_isIncreasedManaCost)
        {
            foreach (Skill ability in _skills)
            {
                ability.Buff.ManaCost.ReductionPercentage(1.3f);
                // Debug.Log("Ability manaCost == " + ability.Buff.ManaCost.Multiplier);
                //Debug.Log("Modified manaCost at ability: " + ability.name + ", Type: " + ability.GetType() + ", ManaCost Value = " + ability.Buff.ManaCost);
            }
            _isIncreasedManaCost = false;
            // Debug.Log("IsIncreasedManaCost in ResetValues == " + _isIncreasedManaCost);
        }

        _isPlayerInvisability = false;
    }
}
