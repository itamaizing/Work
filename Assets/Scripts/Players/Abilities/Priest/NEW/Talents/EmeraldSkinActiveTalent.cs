using UnityEngine;

public class EmeraldSkinActiveTalent : Talent
{
	[SerializeField] private EmeraldSkin _emeraldSkin;
	[SerializeField] private SkillManager _ability;

	private void Awake()
	{
		if (Data.IsOpen)
		{
			Enter();
		}
	}

	public override void Enter()
	{
		_ability.AddSkill(_emeraldSkin);
	}

	public override void Exit()
	{
		_ability.RemoveSkill(_emeraldSkin);
	}
}

