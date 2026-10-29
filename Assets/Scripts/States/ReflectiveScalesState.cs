using System.Collections.Generic;

public class ReflectiveScalesStateStacking : StateStacking
{
    private static readonly List<StatusEffect> _effects = new() { StatusEffect.Strengthening };

    public override States State => States.ReflectiveScales;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;
    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character sourceCaster, string skillName) { }
}