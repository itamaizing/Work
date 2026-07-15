using System.Collections.Generic;
using UnityEngine;

public class ElvenReflexesState : AbstractCharacterState
{
    private const float EvasionBonus = 0.8f;
    private const float TickInterval = 1f;

    private float _tickTimer = 0f;

    private List<StatusEffect> _effects = new() { StatusEffect.Evade };

    /*private float _baseEvade;
    private float _currentEvasionBonus;*/
    
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.ElvenReflexes;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _tickTimer = 0f;

        /*_baseEvade = characterState.Character.Health.EvadeMeleeDamage;
        _currentEvasionBonus = _baseEvade * EvasionBonus;
        characterState.Character.Health.EvadeMeleeDamage += _currentEvasionBonus;*/

        var bonus = character.Character.AttributeSystem[CharacterAttributeName.EvasionPhysical].GetValue() * EvasionBonus;
        
        character.Character.AttributeSystem[CharacterAttributeName.EvasionPhysical]
            .AddModifier(new AttributeModifier(bonus, ModifierType.Flat, source: this));
    }

    public override void UpdateState()
    {
        _tickTimer += Time.deltaTime;
        if (_tickTimer < TickInterval) return;

        _tickTimer = 0f;

        var elvenSkill = characterState.GetState(States.ElvenSkill) as ElvenSkill;
        
        if (elvenSkill == null || elvenSkill.CurrentStacksCount <= 0)
        {
            ExitState();
            return;
        }

        elvenSkill.ReduceStackExternal(true);
    }

    public override void ExitState()
    {
        characterState.Character.AttributeSystem[CharacterAttributeName.EvasionPhysical]
            .RemoveBySource(this, all: true);

        /*characterState.Character.Health.EvadeMeleeDamage -= _currentEvasionBonus;
        _baseEvade = 0;
        _currentEvasionBonus = 0;*/
        characterState.RemoveState(this);
    }
}