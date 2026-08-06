using System.Collections.Generic;
using UnityEngine;

public class FocusingOnReflexesState : RefreshingState
{
    private const float EvadeMeleeBonus = 60f;
    private const float EvadeRangeBonus = 100f;
    private Character _character;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Evade };

    public override States State => States.FocusingOnReflexesState;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public FocusingOnReflexesState()
    {
        MaxStacksCount = 1;
        currentStacksCount = 0;
    }
    
    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _character = character.Character;
        this.personWhoMadeBuff = personWhoMadeBuff;

        currentStacksCount = 1;

        if (_character != null && _character.Health != null)
        {
            _character.Health.EvadeMeleeDamage += EvadeMeleeBonus;
            _character.Health.EvadeRangeDamage += EvadeRangeBonus;
            _character.Health.Evaded += OnEvaded;
        }
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float time)
    {
        duration = time;
        return true;
    }
    
    public override void ExitState()
    {
        if (_character != null && _character.Health != null)
        {
            _character.Health.EvadeMeleeDamage -= EvadeMeleeBonus;
            _character.Health.EvadeRangeDamage -= EvadeRangeBonus;
            _character.Health.Evaded -= OnEvaded;
        }

        currentStacksCount = 0;

        if (characterState != null)
        {
            characterState.StateIcons.RemoveItemByState(State);
            characterState.RemoveState(this);
        }
    }

    private void OnEvaded()
    {
        ExitState();
    }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
        {
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        }
        else
        {
            Stack(durationToExit);
        }

        return this;
    }
}