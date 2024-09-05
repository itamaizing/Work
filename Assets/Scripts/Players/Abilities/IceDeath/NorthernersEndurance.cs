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

	protected override bool IsCanCast => throw new System.NotImplementedException();
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
		Shoot();
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
			if (Input.GetMouseButton(0))
			{
				_target = GetRaycastTarget();
			}
			yield return null;
		}
	}

	//[Command]
	private void Shoot()
	{
		//if (_character.RuneComponent.RemoveRune(1, this))
		{
			float boostHp = 0.1f + 0.003f * _energy.CurrentValue;
			_energy.TryUse(_energy.CurrentValue);
			_target.CharacterState.CmdAddState(States.NorthernerEndurance, 6, boostHp, _playerLinks.gameObject, name);
		}
	}
}
