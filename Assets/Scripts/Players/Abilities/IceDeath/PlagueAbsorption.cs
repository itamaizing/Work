using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlagueAbsorption : Skill
{
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private HeroComponent _playerLinks;

	private Plague _plagueEnemy;
	private Character _target;
	private int _charges = 0;
	private Energy _energy;
	private RuneComponent _rune;

	protected override bool IsCanCast => IsCanCastCheck();

	private bool IsCanCastCheck()
	{
		if(_target == null) return false;

		if (_rune.CurrentValue >= 1)
		{
			_rune.CmdUse(1);
			return true;
		}
		else
		{
			return false;
		}
	}
	private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
			if (_playerLinks.Resources[i].Type == ResourceType.Rune)
			{
				_rune = (RuneComponent)_playerLinks.Resources[i];
			}
		}

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
			_rune.Add(1);
		}
		else if (_energy.TryUse(70))
		{
			{
				_plagueEnemy = (Plague)_target.CharacterState.GetState(States.Plague);
				if (_plagueEnemy == null) return;


				if (_plagueEnemy.GetStack >= 0)
				{
					Debug.Log("CHECK FOR TEst@@");
					_charges++;
					//_deathSpiral.TalentAddSuperCharge();
					_target.CharacterState.CmdRemoveState(States.Plague);
				}
			}
		}
	}
}
