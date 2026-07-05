using System.Collections.Generic;
using UnityEngine;

public class ProtectiveScalesState : StackableState
{
    private float _durationRemaining;
    private float _appliedResist = 0f;

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

    protected override void EnterState(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.personWhoMadeBuff = personWhoMadeBuff;

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
        if (health == null) return;

        health.ResistMagDamage -= _appliedResist;

        _appliedResist = MagicResistValue;
        health.ResistMagDamage += _appliedResist;
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

    protected override void ExitState()
    {
        if (health != null)
        {
            health.ResistMagDamage -= _appliedResist;
        }

        _appliedResist = 0f;
    }
}