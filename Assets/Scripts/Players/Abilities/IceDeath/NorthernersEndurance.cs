using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class NorthernersEndurance : Skill
{
	[SerializeField] private Character _playerLinks;
	private Character _target;
	private Energy _energy;

	protected override bool IsCanCast => true;
	private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
		}

	}

	protected override IEnumerator CastJob()
	{
		float boostHp = 0.1f + 0.003f * _energy.CurrentValue;
		_energy.CmdUse(_energy.CurrentValue);
		Shoot(boostHp, _target.gameObject);
		yield return null;
	}

	protected override void ClearData()
	{
		_target = null;
	}

	protected override IEnumerator PrepareJob()
	{
		while (_target == null)
		{
			if (GetMouseButton)
			{
				_target = GetRaycastTarget(true);
			}
			yield return null;
		}
	}

	[Command]
	private void Shoot(float boostHp, GameObject targetGm)
	{
		Character target = targetGm.GetComponent<Character>();

		/*float boostHp = 0.1f + 0.003f * _energy.CurrentValue;
		_energy.CmdUse(_energy.CurrentValue);*/
		target.CharacterState.AddState(States.NorthernerEndurance, 6, boostHp, _playerLinks.gameObject, name);
		
	}
}
