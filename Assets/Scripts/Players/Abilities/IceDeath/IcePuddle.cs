using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class IcePuddle : Skill
{
	[SerializeField] private IcePuddleObject _puddle;
	[SerializeField] private GameObject _preViewPuddle;
	[SerializeField] private GameObject _lowePoint;
	[SerializeField] private DecalProjector _puddleProjector;
	//[SerializeField] private FrostingFrozenTalant _frostingFrozenTalant;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private float _timeToDestroy = 3f;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private MoveComponent _move;

	private Vector3 _mousePos;
	private float _angle;
	private float _angle2;
	private float _angle3;
	private bool _lastHit = false;
	private bool _enabled = false;
	private bool _secondPoind = false;
	private bool _crutch = false;
	private float _timer = 2;
	private float _time = 0;
	private bool _talentPuddleSize = false;
	private bool _talentFrostingFrozen = false;
	private bool _talentEvadeDadBoost = false;
	private bool _shooted = false;
	private Energy _energy;

	protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool CheckCanCast()
	{
		if (Vector3.Distance(_preViewPuddle.transform.position, transform.position) <= Radius)
		{
			_enabled = true;
			_lastHit = _seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0, 0);
			//if (_lastHit && _talentPuddleSize)
			//_puddleProjector.size = new Vector2(2 * 1.7f, 1 * 1.7f);
			//_puddleProjector.pivot = new Vector3(0, 1.7f / 2, 0.01f);
			//_preViewPuddle.transform.localScale = Vector3.one * 1.7f;

			_preViewPuddle.SetActive(true);
		}
		return _shooted;
		//return Vector3.Distance(_preViewPuddle.transform.position, transform.position) <= Radius;
	}

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

	private void Update()
	{
		if (!_enabled) return;

		Timer();
		/*if (!_secondPoind)
		{
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
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
			/*float distanceA = Vector2.Distance(gameObject.transform.position, _preViewPuddle.transform.position);
			float distanceB = Vector2.Distance(_lowePoint.transform.position, _preViewPuddle.transform.position);
			float distanceC = Vector2.Distance(gameObject.transform.position, _lowePoint.transform.position);
			if(distanceC > _radius)
			{
				distanceC = _radius;
			}
			float maxAngle = (Mathf.Pow(distanceA, 2) + Mathf.Pow(distanceB, 2) - Mathf.Pow(distanceC, 2) / (2*distanceA*distanceB));
			if(_angle2 > maxAngle) 
			{
				_angle2 = maxAngle;
			}
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
				/*if (_playerLinks.RuneComponent.RemoveRune(1, this))
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
			//Cancel();
			_secondPoind = false;
		}*/
	}

	protected override IEnumerator PrepareJob()
	{
		while (!_shooted)
		{
			PlacePuddle();
			_preViewPuddle.SetActive(true);
			//if (GetMouseButton)
			if (Input.GetMouseButtonDown(0))
			{
				if (_secondPoind)
				{
					_move.LookAtTransform(gameObject.transform);
					//_move.StopLookAt();
					_shooted = true;
					_secondPoind = false;
					//Shoot();
					_enabled = false;
					_preViewPuddle.SetActive(false);
				}
				else
				{
					_move.LookAtTransform(gameObject.transform);
					//_move.StopLookAt();
					_secondPoind = true;
				}
			}
			yield return null;
		}
	}

	protected override IEnumerator CastJob()
	{
		Shoot();
		yield return null;
	}

	protected override void ClearData()
	{
		_move.StopLookAt();
		_secondPoind = false;
		_shooted = false;
		_preViewPuddle.SetActive(false);
	}

	/*protected override void Cast()
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
	}*/

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue, Vector3 position, bool lastHit, bool talentEvade, bool talentFrostingFrozen)
	{
		IcePuddleObject projectile = Instantiate(_puddle, position, Quaternion.Euler(-90, -angle, 0));
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		projectile.Init(_playerLinks, manaValue, lastHit, this);
		projectile.SetTalents(talentEvade, talentFrostingFrozen);
		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, manaValue, lastHit);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue, bool lastHit)
	{
		obj.GetComponent<IcePuddleObject>().Init(_playerLinks, manaValue, lastHit, this);
	}

	private Vector3 InstantiatePoint()
	{
		Vector3 worldPosition = Vector3.zero;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit))
		{
			worldPosition = hit.point;
		}
		//Vector3 mousePosition = Input.mousePosition;
		//Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
		//worldPosition.z = 1;

		float distance = Vector3.Distance(gameObject.transform.position, worldPosition);
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
		/*var length = Mathf.Min(distance, _radius);
		var direction = (worldPosition - gameObject.transform.position).normalized;
		return transform.position + direction * length;*/
	}

	private Vector3 InstantiatePoint2()
	{
		//Vector3 mousePosition = Input.mousePosition;
		//Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
		Vector3 worldPosition = Vector3.zero;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit))
		{
			worldPosition = hit.point;
		}
		//worldPosition.z = 1;

		worldPosition += (worldPosition - _preViewPuddle.transform.position).normalized * 2;
		float distance = Vector3.Distance(gameObject.transform.position, worldPosition);
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
		/*var length = Mathf.Min(distance, _radius);
		var direction = (worldPosition - gameObject.transform.position).normalized;
		return transform.position + direction * length;*/
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

	private void PlacePuddle()
	{
		if (!_secondPoind)
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit))
			{
				_mousePos = hit.point;
			}
			//_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector3 lookDir = _mousePos - _hero.transform.position;
			_angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;
			_preViewPuddle.transform.rotation = Quaternion.Euler(-90, -_angle, 0);
			_preViewPuddle.transform.position = InstantiatePoint();
		}
		else
		{
			Vector3 _mousePos2 = Vector3.zero;
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit))
			{
				_mousePos2 = hit.point;
			}
			//Vector3 _mousePos2 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector3 lookDir = _mousePos2 - _preViewPuddle.transform.position;
			_angle2 = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg + 90f;
			
			_lowePoint.transform.position = InstantiatePoint2();
			if (!_crutch)
			{
				_angle3 = _angle2;
				_preViewPuddle.transform.rotation = Quaternion.Euler(-90, -_angle2, 0);
			}
			//_shooted = true;
		}
	}

	private void Shoot()
	{
		_shooted = true;
		int timeToAdd = (int)_energy.CurrentValue / 5;
		if (timeToAdd > 4)
			timeToAdd = 4;

		_timeToDestroy += timeToAdd;
		_energy.CmdUse(timeToAdd * 5);

		Buff.AttackSpeed.ReductionPercentage(1 + _seriesOfStrikes.GetMultipliedSpeed() / 100);

		//_lastHit = _seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0, 0);


		Buff.AttackSpeed.IncreasePercentage(1 + _seriesOfStrikes.GetMultipliedSpeed() / 100);

		CmdCreateProjecttile(_angle3, _timeToDestroy, _preViewPuddle.transform.position, _lastHit && _talentPuddleSize, _talentEvadeDadBoost, _talentFrostingFrozen);
	}

	public void SetTalentPuddleSize(bool active)
	{
		_talentPuddleSize = active;
	}

	public void SetTalentFrostingFrozen(bool value)
	{
		_talentFrostingFrozen = value;
	}

	public void SetTalentEvadeDadBoost(bool value)
	{
		_talentEvadeDadBoost = value;
	}
}
