using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreeperInvisibleState : AbstractCharacterState
{
    private List<Skill> _skills = new();
    private CreeperInvisible _creeperInvisible;
    private Character _player;

    private float _multMoveSpeed = 0.7f; //-30%
    private float _multManaRegen = 1.3f; //+30%
    private float _multSkillCost = 1.3f; //+30%

    private bool _isIncreasedManaCost = false;
    private bool _isCanApplyInvisible;
    private bool _playerInInvisible;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };
    public override States State => States.CreeperInvisible;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;
    
    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _player = characterState.Character;

        if (_player != null)
        {
            ApplyInvisible();
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
        if (_creeperInvisible == null) return;

        _isCanApplyInvisible = _creeperInvisible.IsInvisible;

        if (_isCanApplyInvisible)
        { 
            if (_playerInInvisible == false)
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
        _playerInInvisible = false;
        ResetValues();
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    private void ApplyInvisible()
    {
        _playerInInvisible = true;

        _player.AttributeSystem[CharacterAttributeName.ResourceCost].
            AddModifier(new AttributeModifier(_multSkillCost, ModifierType.Multiplier, source: this));

        _player.Resource.Attr_RegenPeriod.AddModifier
            (new AttributeModifier(_multManaRegen, ModifierType.Multiplier, source: this));

        if (_player?.Move == null) return;
        _player.AttributeSystem[CharacterAttributeName.MoveSpeed].
            AddModifier(new AttributeModifier(_multMoveSpeed, ModifierType.Multiplier, source: this));
    }

    private void ResetValues()
    {
        _player.AttributeSystem[CharacterAttributeName.MoveSpeed].RemoveBySource(this, all: true);
        _player.AttributeSystem[CharacterAttributeName.ResourceCost].RemoveBySource(this, all: true);
        _player.Resource.RemoveModifierBySource(ResourceAttributeName.RegenPeriod, this, all: true);

        _playerInInvisible = false;
    }
}
