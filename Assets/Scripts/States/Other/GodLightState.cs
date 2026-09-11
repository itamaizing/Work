using System.Collections.Generic;

public class GodLightState : AbstractCharacterState
{
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.GodLight;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        
        characterState.Character.Abilities.SetNextSkillFree();
        characterState.Character.Abilities.SetNextSkillNoCast();
    }

    public override void ExitState()
    {
        
        characterState.RemoveState(this);
    }
    
    public override void UpdateState()
    {
        if (!characterState.Character.Abilities.IsNextSkillNoCast) ExitState();
    }
}
