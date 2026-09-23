using System.Collections.Generic;
using UnityEngine;

public class ProtectiveScalesStateStacking : StateStacking
{
    private float _durationRemaining;

    private const float MagicResistValue = 90f;

    private List<StatusEffect> _effects = new List<StatusEffect>()
    {
        StatusEffect.Strengthening
    };

    public override States State => States.ProtectiveScales;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;
    public override float RemainingDuration => _durationRemaining;

    public override void Apply(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.sourceCaster = personWhoMadeBuff;

        _durationRemaining = durationToExit;

        ApplyMagicResist();
        TryDispelMagicDebuffs();
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float time)
    {
        _durationRemaining = time;
        return true;
    }

    private void ApplyMagicResist()
    {
        if (characterState == null) return;

        var attribute = characterState.Character.AttributeSystem[CharacterAttributeName.ResistanceMagical];
        attribute.RemoveBySource(this);
        attribute.AddModifier(new AttributeModifier(MagicResistValue, ModifierType.Flat, this));
    }

    private void TryDispelMagicDebuffs()
    {
        var states = characterState.CurrentStates;

        for (int i = states.Count - 1; i >= 0; i--)
        {
            var state = states[i];
            if (state == this) continue;

            if (state.Type == StateType.Magic && state.BaffDebaff == BaffDebaff.Debaff)
            {
                float chance = Random.Range(0f, 100f);
                if (chance <= 90f)
                {
                    characterState.RemoveState(state.State);
                }
            }
        }
    }

    public override void ExitState()
    {
        characterState.Character.AttributeSystem[CharacterAttributeName.ResistanceMagical].RemoveBySource(this);
        characterState.RemoveState(this);
    }
}