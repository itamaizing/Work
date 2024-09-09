using UnityEngine;

public class IceShardTalent : Talent
{
	[SerializeField] private IceShard _iceShard;
	[SerializeField] private SkillManager _ability;
	public override void Enter()
	{
		_ability.AddSkill(_iceShard);
	}

	public override void Exit()
	{
		_ability.RemoveSkill(_iceShard);
	}
}
