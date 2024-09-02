using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class NorthernersEndurance : Skill
{
	[SerializeField] private Character _character;
	private Character _target;
	protected override bool IsCanCast => throw new System.NotImplementedException();

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
			float boostHp = 0.1f + 0.003f * _character.Stamina.CurrentValue;
			_character.Stamina.TryUse(_character.Stamina.CurrentValue);
			_target.CharacterState.CmdAddState(States.NorthernerEndurance, 6, boostHp, _character.gameObject, name);
		}
	}
}
