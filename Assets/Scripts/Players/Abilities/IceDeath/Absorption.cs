using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Absorption : Ability
{
	[SerializeField] private Character _player;
	private bool _active = false;


	private void Update()
	{
		if(!_active) return;

		if (Input.GetMouseButton(0))
		{
			var _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			RaycastHit2D[] hits =
				Physics2D.CircleCastAll(_mousePos, _radius, Vector2.zero);
			for (int i = 0; i < hits.Length; i++)
			{
				if (hits[i].collider.TryGetComponent<IceShadowObject>(out var shadow))
				{
					if (shadow.IsAlive)
					{
						CmdAction(shadow.gameObject);
					}
				}
			}
			_active = false;
		}
	}

	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{
		_active = true;
	}

	[Command]
	private void CmdAction(GameObject bodyObj)
	{
		Debug.Log(bodyObj.name);
		Action(bodyObj);
		RpcAction(bodyObj);
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
		IceShadowObject body = bodyObj.GetComponent<IceShadowObject>();

		float regen = 0.1f * body.HP + 0.05f * _player.Stamina.CurrentValue / 10;
		_player.Stamina.TryUse(_player.Stamina.CurrentValue);
		_player.Health.Add(regen);
		body.Explode();

	}
}
