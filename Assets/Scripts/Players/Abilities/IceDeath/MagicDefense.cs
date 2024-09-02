using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MagicDefense : Skill
{
	[SerializeField] private PlagueAbsorption _plagueAbsorption;
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private Character _character;
	[SerializeField] private MagicDefenseArea _magDefArea;

	private Vector3 _position;
	private Character _target;
	private float _shieldCapacity = 200;
	private bool _ready = false;
	private bool _hit = false;

	protected override bool IsCanCast => throw new System.NotImplementedException();


	private void Update()
	{
		if (!_ready) return;

		if (Input.GetMouseButtonDown(0))
		{
			_target = null;
			Collider2D hit = Physics2D.OverlapCircle(Camera.main.ScreenToWorldPoint(Input.mousePosition), 2);

			if (hit.transform.TryGetComponent<Character>(out Character enemy) && Vector2.Distance(hit.transform.position, transform.position) <= _radius)
			{
				_target = enemy;
				_hit = true;
			}
			else
			{
				_hit = false;
			}

			//_position = rayHit[0].point;
			_position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			_position.z = 0;
			if (Vector2.Distance(_position, transform.position) > _radius)
			{
				Vector3 direction = (_position - gameObject.transform.position).normalized;
				_position = gameObject.transform.position + direction * _radius;
			}
			Shoot();
		}
	}
	private void Shoot()
	{
		if (_hit)
		{
			//if (_plagueAbsorption.TryUseCharges(1) && _deathSpiral.TryUseCharges(1))
			//{
			//if (_character.RuneComponent.Use(1) && _character.Stamina.Use(70))
			//{
			//_target.CharacterState.CmdAddState(States.MagicBuff, 6, _character.Stamina.UseAll() * 10 + _shieldCapacity);
			ServerAdd(_target.gameObject);
			_ready = false;
			//}
			//}
		}
		else
		{
			//if (_plagueAbsorption.TryUseCharges(2) && _deathSpiral.TryUseCharges(2))
			//{
			//	if (_character.RuneComponent.Use(2) && _character.Stamina.Use(70))
			//	{
			
			SpawnArea(_position);

			_ready = false;
			//	}
			//}
		}
	}

	[Command]
	private void ServerAdd(GameObject obj)
	{
		Character target = obj.GetComponent<Character>();
		target.CharacterState.CmdAddState(States.MagicBuff, 6, _character.Stamina.CurrentValue * 10 + _shieldCapacity, _character.gameObject, name);
	}

	[Command]
	private void SpawnArea(Vector3 position)
	{
		MagicDefenseArea area = Instantiate(_magDefArea, position, Quaternion.identity);
		area.Init(_character, _character.Stamina.CurrentValue, false);
		_character.Stamina.TryUse(_character.Stamina.CurrentValue);
		NetworkServer.Spawn(area.gameObject);

		RpcInit(area.gameObject, position);
	}
	

	[ClientRpc]
	private void RpcInit(GameObject area, Vector3 position)
	{
		MagicDefenseArea magArea = area.GetComponent<MagicDefenseArea>();
		magArea.Init(_character, _character.Stamina.CurrentValue, false);
	}

	protected override IEnumerator PrepareJob()
	{
		throw new System.NotImplementedException();
	}

	protected override IEnumerator CastJob()
	{
		throw new System.NotImplementedException();
	}

	protected override void ClearData()
	{
		throw new System.NotImplementedException();
	}
}
