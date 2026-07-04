using System.Collections.Generic;

public class NorthernerEndurance : AbstractCharacterState
{
	private float _damageToExit;
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.NorthernerEndurance;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => throw new System.NotImplementedException();

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		health = character.Character.Health;
		//_health.BoostHpBonus(damageToExit);
		_damageToExit = damageToExit;
	}

	public override void UpdateState()
	{
	}

	public override void ExitState()
	{
		characterState.RemoveState(this);
		
		//_health.BoostHpReverse(_damageToExit);
	}

	public override bool Stack(float time)
	{
		duration = time;
		return true;
	}
}
