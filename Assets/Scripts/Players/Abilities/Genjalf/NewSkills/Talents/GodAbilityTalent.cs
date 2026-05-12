using UnityEngine;

public class GodAbilityTalent : Talent
{
	[SerializeField] private SkillManager _skillManager;
	private bool _chargesIsAdded = false;

	public override void Enter()
	{
		if (!_chargesIsAdded)
		{
			_skillManager.TalentAddCharges(true);
			_chargesIsAdded = true;
		}
	}

	public override void Exit()
	{
		if (_chargesIsAdded)
		{
			_skillManager.TalentAddCharges(false);
			_chargesIsAdded = false;
		}
	}
}
