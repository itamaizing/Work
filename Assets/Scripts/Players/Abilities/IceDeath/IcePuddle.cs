using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class IcePuddle : Ability
{
	[SerializeField] private IcePuddleObject _puddle;
	[SerializeField] private PlayerLinks _playerLinks;
	[SerializeField] private GameObject _croosFire;
	//[SerializeField] private GameObject _spawnPoint;

	private Vector2 _mousePos;
	private float _angle;
	private float _angle2;
	private bool _enabled = false;
	private bool _secondPoind = false;

	private void Start()
	{
		_croosFire.SetActive(false);
	}
	private void Update()
	{
		if (!_enabled) return;

		if (!_secondPoind)
		{
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
			_angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			_croosFire.transform.rotation = Quaternion.Euler(_croosFire.transform.rotation.x, _croosFire.transform.rotation.y, _angle);
			_croosFire.transform.position = InstantiatePoint();
		}
		else
		{
			Vector3 _mousePos2 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos2 - _croosFire.transform.position;
			_angle2 = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			_croosFire.transform.rotation = Quaternion.Euler(_croosFire.transform.rotation.x, _croosFire.transform.rotation.y, _angle2);
		}
		if (Input.GetMouseButtonDown(0))
		{
			if (_secondPoind)
			{
				_secondPoind = false;
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
			else 
			{
				_secondPoind = true;
			}
		}
		if (Input.GetMouseButtonDown(1))
		{
			Cancel();
		}
	}

	protected override void Cast()
	{
		PayCost();
		if (_playerLinks.RuneComponent.RemoveRune(1, this))
		{
			Shoot();
		}
	}

	protected override void Cancel()
	{
		//����� �� ���� ����� ��� ������ �����, ���� ���....
	}
	private void Shoot()
	{
		//IcePuddleObject puddle = Instantiate(_puddle, _spawnPoint.transform.position, Quaternion.Euler(_croosFire.transform.rotation.x, _croosFire.transform.rotation.y, _croosFire.transform.rotation.z));
		IcePuddleObject puddle = Instantiate(_puddle, _croosFire.transform.position, Quaternion.Euler(0, 0, _angle2));
		puddle.dad = _playerLinks.gameObject;
		puddle.energyPlayer = (EnergyPlayer)Mana;
		puddle.healthPlayer = _playerLinks.HealthPlayer;
		_enabled = false;
		_croosFire.SetActive(false);
	}

	private Vector3 InstantiatePoint()
	{
		Vector3 mousePosition = Input.mousePosition;
		Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
		worldPosition.z = 1;
		float distance = Vector2.Distance(gameObject.transform.position, worldPosition);
		if (distance <= _radius)
		{
			return worldPosition;
		}
		else
		{
			Vector3 direction = (worldPosition - gameObject.transform.position).normalized;
			Vector3 spawnPosition = gameObject.transform.position + direction * _radius;
			return spawnPosition;
		}

	}
}
