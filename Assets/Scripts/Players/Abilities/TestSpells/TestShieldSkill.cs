using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class TestShieldSkill : Skill
{
	[SerializeField] private Character _playerLinks;
	private Character _target;

	protected override bool IsCanCast => true;

	protected override IEnumerator CastJob()
	{
		Shoot(_target.gameObject);
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
	private void Shoot(GameObject targetGm)
	{
		Character target = targetGm.GetComponent<Character>();
		target.CharacterState.AddState(States.LightShield, 20, 100, _playerLinks.gameObject, name);

	}
}
