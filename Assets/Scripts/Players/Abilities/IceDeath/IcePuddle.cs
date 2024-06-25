using Mirror;
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
	//[SerializeField] private FrostingFrozenTalant _frostingFrozenTalant;
	//[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private float _timeToDestroy = 3f;
	//[SerializeField] private GameObject _spawnPoint;

	private Vector2 _mousePos;
	private float _angle;
	private float _angle2;
	private bool _lastHit = false;
	private bool _enabled = false;
	private bool _secondPoind = false;
	private bool _crutch = false;
	private float _timer = 2;
	private float _time = 0;

	/*private void Start()
	{
		_preViewPuddle.SetActive(false);
	}*/
	private void Update()
	{
		if (!_enabled) return;

		Timer();
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
				//PayCost();
				if (_playerLinks.RuneComponent.RemoveRune(1, this))
				{
					Shoot();

					_enabled = false;
					_preViewPuddle.SetActive(false);
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
		//_lastHit = _seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1);
		//_lastHit = true;
		if(_lastHit)
			_preViewPuddle.transform.localScale = Vector3.one * 1.7f;

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
		int timeToAdd = (int)Mana.Value / 5;
		if (timeToAdd > 4)
			timeToAdd = 4;

		_timeToDestroy += timeToAdd;
		//puddle.talant = _frostingFrozenTalant;

		Debug.Log("test spawn");
		CmdCreateProjecttile(_angle2, _timeToDestroy, _preViewPuddle.transform.position, _lastHit);
		Mana.Use(timeToAdd * 5);
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue, Vector3 position, bool lastHit)
	{
		IcePuddleObject projectile = Instantiate(_puddle, position, Quaternion.Euler(0, 0, angle));
		projectile.Init(_playerLinks, manaValue, lastHit);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, manaValue, lastHit);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue, bool lastHit)
	{
		obj.GetComponent<IcePuddleObject>().Init(_playerLinks, manaValue, lastHit);
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

	private void Timer()
	{
		if(_lastHit) 
		{
			_time += Time.deltaTime;
			if(_time >= _timer)
			{
				_lastHit = false;
				_preViewPuddle.transform.localScale = Vector3.one;
			}
		}
	}
}
