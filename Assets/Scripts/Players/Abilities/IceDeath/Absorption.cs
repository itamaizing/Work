using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Absorption : Skill
{
	[SerializeField] private Character _player;
	private IcyCorpse _target;
	private bool _active = false;

	protected override bool IsCanCast
	{
		get { return _target != null; }
	}

	/*private void Update()
	{
		if(!_active) return;

		if (Input.GetMouseButton(0))
		{
			var _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			RaycastHit2D[] hits =
				Physics2D.CircleCastAll(_mousePos, _radius, Vector2.zero);
			for (int i = 0; i < hits.Length; i++)
			{
				if (hits[i].collider.TryGetComponent<IcyCorpse>(out var shadow))
				{
					CmdAction(shadow.gameObject);
					if (shadow.IsAlive)
					{
						CmdAction(shadow.gameObject);
					}
				}
			}
			_active = false;
		}
	}*/


	[Command]
	private void CmdAction(GameObject bodyObj)
	{
		Debug.Log(bodyObj.name);
		Action(bodyObj);
		//RpcAction(bodyObj);
	}

	[ClientRpc]
	private void RpcAction(GameObject bodyObj) 
	{
		Debug.Log(bodyObj.name);
		Action(bodyObj);
	}

	private void Action(GameObject bodyObj)
	{
		Debug.Log(bodyObj.name);
		IcyCorpse body = bodyObj.GetComponent<IcyCorpse>();

		float regen = 0.1f * body.HP + 0.05f * _player.Stamina.CurrentValue / 10;
		_player.Stamina.TryUse(_player.Stamina.CurrentValue);
		_player.Health.Add(regen);
		body.Explode();

	}

	protected override IEnumerator PrepareJob()
	{
		while (_target == null)
		{
			if (Input.GetMouseButton(0))
			{
				_target = (IcyCorpse)GetRaycastTarget();
			}
			yield return null;
		}
	}

	protected override IEnumerator CastJob()
	{
		CmdAction(_target.gameObject);

		yield return null;
	}

	protected override void ClearData()
	{
		_target = null;
		return;
	}
}
