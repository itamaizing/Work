using System.Collections.Generic;
using UnityEngine;

public class SlowFlowLightState : RefreshingState
{
	private float _speedDebuf = -0.6f;
	private AttributeModifier _modif = new AttributeModifier(0f, ModifierType.Percent);

	private List<StatusEffect> _effects = new List<StatusEffect>();
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.SlowFlowLight;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;


    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_modif = new AttributeModifier(_speedDebuf, ModifierType.Percent);
    
		characterState = character;
		MaxStacksCount = 1;

		characterState.Character.Move.AddModifier(_modif);
		characterState.Character.Abilities.Abilities.ForEach(s =>
			s.Buff.CastSpeed.IncreasePercentage(1 - _speedDebuf));
	}

	public override void UpdateState()
	{
	}

	protected override void ExitState()
	{
		characterState.Character.Move.RemoveModifier(_modif);
		currentStacksCount = 0;
		_modif = new AttributeModifier(_speedDebuf, ModifierType.Percent);
		characterState.Character.Abilities.Abilities.ForEach(s => s.Buff.CastSpeed.Reset());
	}

    public override bool Stack(float time)
    {
        duration = time;
        return false;
    }
}
