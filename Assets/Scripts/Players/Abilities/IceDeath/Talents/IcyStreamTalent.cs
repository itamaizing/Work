using UnityEngine;

public class IcyStreamTalent : Talent
{
	[SerializeField] private IcyStream _icyStream;
	[SerializeField] private SkillManager _ability;
	public override void Enter()
	{
		_ability.AddSkill(_icyStream);
	}

	public override void Exit()
	{
		_ability.RemoveSkill(_icyStream);
	}
}
