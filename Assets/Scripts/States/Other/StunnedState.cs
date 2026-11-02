using System.Collections.Generic;
using UnityEngine;

public class StunnedStateStacking : StateStackingRefreshing
{
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Move, StatusEffect.Ability };

    public override States State => States.Stun;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override bool CanExceedMaxDuration => true;

    public override bool TryApply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName)
    {

        float duration = character.CurrentStates.Contains(this) ? Mathf.Max(RemainingDuration, durationToExit) : durationToExit;

        return base.TryApply(character, duration, damageToExit, sourceCaster, skillName);
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        SetMaxStacks(1);
        CurrentStacksCount = 1;

        abilities?.SetAbilitiesDisactive(true);

        characterState.Character.Move.IsMoveBlocked = true;
        characterState.Character.Move.StopMoveAndAnimationMove();
    }

    public override bool Stack(float newDuration)
    {
        if (newDuration > RemainingDuration)
            RemainingDuration = newDuration;

        return true;
    }

    protected override void OnExit()
    {
        CurrentStacksCount = 0;

        bool moveBlocked = false;
        bool abilitiesBlocked = false;

        foreach (var otherState in characterState.CurrentStates)
        {
            if (otherState == this)
                continue;

            if (otherState.Effects.Contains(StatusEffect.Move))
                moveBlocked = true;

            if (otherState.Effects.Contains(StatusEffect.Ability))
                abilitiesBlocked = true;
        }

        characterState.Character.Move.IsMoveBlocked = moveBlocked;
        abilities?.SetAbilitiesDisactive(abilitiesBlocked);
    }
}