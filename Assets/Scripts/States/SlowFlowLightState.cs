using System.Collections.Generic;
using UnityEngine;

public class SlowFlowLightStateStacking : RefreshingStateStacking
{
	private float _speedDebuf = -0.6f;
	private AttributeModifier _modif = new AttributeModifier(0f, ModifierType.Percent);

	private List<StatusEffect> _effects = new List<StatusEffect>();
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.SlowFlowLight;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;


    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_modif = new AttributeModifier(_speedDebuf, ModifierType.Percent);
    
		characterState = character;
		SetMaxStacks(1);

		characterState.Character.Move.AddModifier(_modif);
		characterState.Character.Abilities.Abilities.ForEach(s =>
			s.Buff.CastSpeed.IncreasePercentage(1 - _speedDebuf));
	}

	public override void UpdateState()
	{
	}

	public override void ExitState()
	{
		characterState.Character.Move.RemoveModifier(_modif);
		currentStacksCount = 0;
		_modif = new AttributeModifier(_speedDebuf, ModifierType.Percent);
		characterState.Character.Abilities.Abilities.ForEach(s => s.Buff.CastSpeed.Reset());
		characterState.RemoveState(this);
	}

    public override bool Stack(float time)
    {
        RemainingDuration = time;
        return false;
    }
}
