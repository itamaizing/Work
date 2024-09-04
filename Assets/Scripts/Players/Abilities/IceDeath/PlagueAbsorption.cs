using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlagueAbsorption : Skill
{
	//[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private Character _character;
	private Plague _plagueEnemy;
	private Character _target;
	private int _charges = 0;

	protected override bool IsCanCast
	{
		get { return _target != null; }
	}


	public bool TryUseCharges(int value)
	{
		if(_charges- value >= 0)
		{
			return true;
		}
		return false;
	}

	public bool UseCharge(int value)
	{
		if (_charges - value >= 0)
		{
			_charges-= value;
			return true;
		}
		return false;
	}

	protected override IEnumerator PrepareJob()
	{
		while (_target == null || _charges <= 0)
		{
			if (Input.GetMouseButton(0))
			{
				_target = GetRaycastTarget();
			}
			yield return null;
		}
	}

	protected override IEnumerator CastJob()
	{
		Absorption();
		yield return null;
	}

	protected override void ClearData()
	{
		_target = null;
	}

	private void Absorption()
	{
		if (_charges > 0)
		{
			_charges--;
			_character.RuneComponent.Add(1);
		}
		else if (_character.Stamina.TryUse(70))
		{
			//if (_character.RuneComponent.RemoveRune(1, this))
			//{
			//	_plagueEnemy = (Plague)_target.CharacterState.GetState(States.Plague);
			//	if (_plagueEnemy == null) return;

				
			//	if (_plagueEnemy.GetStack >= 0)
			//	{
			//		Debug.Log("CHECK FOR TEst@@");
			//		_charges++;
			//		//_deathSpiral.TalentAddSuperCharge();
			//		_target.CharacterState.CmdRemoveState(States.Plague);
			//	}
			//}
			//else
			//{
			//	_character.Stamina.Add(70);
			//}
		}
	}
}
