using System.Collections.Generic;
using UnityEngine;

public class ImmortalityState : StateBasic
{
    private float _duration;
    private Character _player;

    private List<StatusEffect> _effects = new();
    public override States State => States.ImmortalityState;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _player = characterState.Character;
        _duration = durationToExit;

        var attrs = _player.AttributeSystem;
        var evadeMelee = attrs[CharacterAttributeName.EvasionPhysicalMelee];
        var evadeRange = attrs[CharacterAttributeName.EvasionPhysicalRange];
        var evadeMagic = attrs[CharacterAttributeName.EvasionMagical];

        evadeMelee.AddModifier(new AttributeModifier(100f - evadeMelee.GetValue(), ModifierType.Flat, this));
        evadeRange.AddModifier(new AttributeModifier(100f - evadeRange.GetValue(), ModifierType.Flat, this));
        evadeMagic.AddModifier(new AttributeModifier(100f - evadeMagic.GetValue(), ModifierType.Flat, this));

        _player.Health.OnTryResist += NegateAllDamage;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
            ExitState();
    }

    public override void ExitState()
    {
        _duration = 0;

        if (_player != null && _player.AttributeSystem != null)
        {
            var attrs = _player.AttributeSystem;
            attrs[CharacterAttributeName.EvasionPhysicalMelee].RemoveBySource(this);
            attrs[CharacterAttributeName.EvasionPhysicalRange].RemoveBySource(this);
            attrs[CharacterAttributeName.EvasionMagical].RemoveBySource(this);

            _player.Health.OnTryResist -= NegateAllDamage;
        }

        characterState.RemoveState(this);
    }

    private bool NegateAllDamage(Damage damage, Skill skill) => true;
}