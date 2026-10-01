using System.Collections.Generic;
using UnityEngine;

public class PetrificationStateStacking : StateStacking
{
    private float _duration;

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.PetrificationDebuff;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects { get; }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _duration = durationToExit;

        var attrs = characterState.Character.AttributeSystem;
        var resistMag = attrs[CharacterAttributeName.ResistanceMagical];
        var resistPhys = attrs[CharacterAttributeName.ResistancePhysical];

        resistMag.AddModifier(new AttributeModifier(80f - resistMag.GetValue(), ModifierType.Flat, this));
        resistPhys.AddModifier(new AttributeModifier(80f - resistPhys.GetValue(), ModifierType.Flat, this));

        characterState.Character.Move.SetCanMove(false);

        foreach (var ability in characterState.Character.Abilities.Abilities)
        {
            ability.Disactive = true;
        }
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration < 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);

        foreach (var ability in characterState.Character.Abilities.Abilities)
        {
            ability.Disactive = false;
        }

        characterState.Character.Move.SetCanMove(true);

        var attrs = characterState.Character.AttributeSystem;
        attrs[CharacterAttributeName.ResistanceMagical].RemoveBySource(this);
        attrs[CharacterAttributeName.ResistancePhysical].RemoveBySource(this);
    }

    public override bool Stack(float time)
    {
        return true;
    }
}