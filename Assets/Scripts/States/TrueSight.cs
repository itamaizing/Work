using System.Collections.Generic;
using UnityEngine;

public class TrueSightAuraState : AuraState
{
    public override States State => States.TestAuraState; // заменишь на States.TrueSight при добавлении в enum
    public override StateType Type => StateType.Aura;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override float Distance => 6f;
    public override float EffectRate => 0.5f;
    public override LayerMask LayerMask => LayerMask.GetMask("Enemy", "Allies"); // или конкретный слой врагов

    public override List<StatusEffect> Effects => new() { StatusEffect.Invisible };

    public override void EffectOnEnter(Character character)
    {
        // не нужен, т.к. эффект постоянный
    }

    public override void EffectOnExit(Character character)
    {
        // не нужен, т.к. эффект постоянный
    }

    public override void EffectOnStay(List<Character> characters)
    {
        foreach (var character in characters)
        {
            if (character == null || character == _self) continue;

            var state = character.CharacterState;

            // Раскрыть любые невидимости
            if (state.CheckForState(States.Invisible))
            {
                state.RemoveState(States.Invisible);
            }

            if (state.CheckForState(States.CreeperInvisible))
            {
                state.RemoveState(States.CreeperInvisible);
            }

            // Добавь любые другие невидимости
        }
    }
}
