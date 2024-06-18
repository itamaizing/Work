using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class IcePuddle : Ability
{
	[SerializeField] private IcePuddleObject _puddle;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private GameObject _preViewPuddle;
	[SerializeField] private GameObject _lowePoint;
	[SerializeField] private FrostingFrozenTalant _frostingFrozenTalant;
	//[SerializeField] private GameObject _spawnPoint;

	private Vector2 _mousePos;
	private float _angle;
	private float _angle2;
	private bool _enabled = false;
	private bool _secondPoind = false;
	private bool _crutch = false; //��� ������ ����� ��� �������

	private void Start()
	{
		_preViewPuddle.SetActive(false);
	}
	private void Update()
	{
		if (!_enabled) return;

		if (!_secondPoind)
		{
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
			_angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			//_preViewPuddle.transform.rotation = Quaternion.Euler(_preViewPuddle.transform.rotation.x, _preViewPuddle.transform.rotation.y, _angle);
			_preViewPuddle.transform.rotation = Quaternion.Euler(0, 0, _angle);
			_preViewPuddle.transform.position = InstantiatePoint();
		}
		else
		{
			Vector3 _mousePos2 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos2 - _preViewPuddle.transform.position;
			_angle2 = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg + 90f;
			_lowePoint.transform.position = InstantiatePoint();
			if (!_crutch)
			{
				//_preViewPuddle.transform.rotation = Quaternion.Euler(_preViewPuddle.transform.rotation.x, _preViewPuddle.transform.rotation.y, _preViewPuddle.transform.rotation.z);
				//_preViewPuddle.transform.rotation = Quaternion.Euler(_preViewPuddle.transform.rotation.x, _preViewPuddle.transform.rotation.y, _angle2);
				_preViewPuddle.transform.rotation = Quaternion.Euler(0, 0, _angle2);
			}
		}
		if (Input.GetMouseButtonDown(0))
		{
			if (_secondPoind)
			{
				_secondPoind = false;
				PayCost();
				if (_playerLinks.RuneComponent.RemoveRune(1, this))
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
			_secondPoind = false;
		}
	}

	protected override void Cast()
	{
		_preViewPuddle.SetActive(true);
		_enabled = true;
	}

	protected override void Cancel()
	{
		_preViewPuddle.SetActive(false);
		_enabled = false;
	}
	private void Shoot()
	{
		//IcePuddleObject puddle = Instantiate(_puddle, _spawnPoint.transform.position, Quaternion.Euler(_croosFire.transform.rotation.x, _croosFire.transform.rotation.y, _croosFire.transform.rotation.z));
		//IcePuddleObject puddle = Instantiate(_puddle, _preViewPuddle.transform.position, Quaternion.Euler(0, 0, _angle2));
		IcePuddleObject puddle = Instantiate(_puddle, _preViewPuddle.transform.position, Quaternion.Euler(0, 0, _preViewPuddle.transform.eulerAngles.z));
		puddle.talant = _frostingFrozenTalant;
		puddle.dad = _playerLinks;
		puddle.energy = (Energy)Mana;
		puddle.healthComponent = _playerLinks.Health;
		_enabled = false;
		_preViewPuddle.SetActive(false);
	}

	private Vector3 InstantiatePoint()
	{
		Vector3 mousePosition = Input.mousePosition;
		Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
		worldPosition.z = 1;
		float distance = Vector2.Distance(gameObject.transform.position, worldPosition);
		if (distance <= _radius)
		{
			_crutch = false;
			return worldPosition;
		}
		else
		{
			_crutch = true;
			Vector3 direction = (worldPosition - gameObject.transform.position).normalized;
			Vector3 spawnPosition = gameObject.transform.position + direction * _radius;
			return spawnPosition;
		}

	}
}
