using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusingOnReflexesStateStacking : StateBasic
{
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Evade };

    public override States State => States.FocusingOnReflexesState;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    private const float EvadeMeleeBonus = 60f;
    private const float EvadeRangeBonus = 100f;

    private readonly AttributeModifier _evadeMeleeModifier = new(EvadeMeleeBonus, ModifierType.Flat);
    private readonly AttributeModifier _evadeRangeModifier = new(EvadeRangeBonus, ModifierType.Flat);


    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        var attrs = Target.AttributeSystem;
        _evadeMeleeModifier.Source = this;
        _evadeRangeModifier.Source = this;
        attrs[CharacterAttributeName.EvasionPhysicalMelee].AddModifier(_evadeMeleeModifier);
        attrs[CharacterAttributeName.EvasionPhysicalRange].AddModifier(_evadeRangeModifier);

        SubToEvents();
    }

    private void OnEvaded(Skill skill)
    {
        ForceEnd();
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (damage.IsDot) return;
        ForceEnd();
    }

    private void ForceEnd()
    {
        Target.CharacterState.CmdRemoveState(State);
    }

    protected override void OnExit()
    {
        var attrs = Target.AttributeSystem;
        attrs[CharacterAttributeName.EvasionPhysicalMelee].RemoveModifier(_evadeMeleeModifier);
        attrs[CharacterAttributeName.EvasionPhysicalRange].RemoveModifier(_evadeRangeModifier);

        UnsubFromEvents();
    }

    private void SubToEvents()
    {
        if (Target != null && Target.Health != null)
        {
            Target.Health.DamageTaken += OnDamageTaken;
            Target.Health.Evaded += OnEvaded;
        }
    }

    private void UnsubFromEvents()
    {
        if (Target != null && Target.Health != null)
        {
            Target.Health.DamageTaken -= OnDamageTaken;
            Target.Health.Evaded -= OnEvaded;
        }
    }
}