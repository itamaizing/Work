using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class IcePuddle : Ability
{
	[SerializeField] private IcePuddleObject _puddle;
	[SerializeField] private PlayerLinks _playerLinks;
	[SerializeField] private GameObject _croosFire;

	private Vector2 _mousePos;
	private float _angle;
	private bool _enabled = false;

	private void Update()
	{
		if (!_enabled) return;

		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
		_angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		_croosFire.transform.rotation = Quaternion.Euler(_croosFire.transform.rotation.x, _croosFire.transform.rotation.y, _angle);

		if (Input.GetMouseButtonDown(0))
		{
			PayCost();
			if (_playerLinks.RunePlayer.RemoveRune(1, this))
			{
				Shoot();
			}
			else
			{
				Cancel();
			}
		}
		if (Input.GetMouseButtonDown(1))
		{
			Cancel();
		}
	}

	protected override void Cast()
	{
		_croosFire.SetActive(true);
		_enabled = true;
	}

	protected override void Cancel()
	{
		_croosFire.SetActive(false);
	}
	private void Shoot()
	{
		IcePuddleObject puddle = Instantiate(_puddle, InstantiatePoint(), Quaternion.identity);
		puddle.dad = _playerLinks.gameObject;
		puddle.energyPlayer = (EnergyPlayer)Mana;
		puddle.healthPlayer = _playerLinks.HealthPlayer;
		_enabled = false;
	}

	private Vector3 InstantiatePoint()
	{
		Vector3 mousePosition = Input.mousePosition;
		Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
		worldPosition.z = 1;
		float distance = Vector2.Distance(gameObject.transform.position, worldPosition);
		Debug.Log(distance);
		if (distance <= _radius)
		{
			Debug.Log("alright");
			return worldPosition;
		}
		else
		{
			Debug.Log("max pos, downgrading");
			Vector3 direction = (worldPosition - gameObject.transform.position).normalized;
			Vector3 spawnPosition = gameObject.transform.position + direction * _radius;
			return spawnPosition;
		}

	}
}
