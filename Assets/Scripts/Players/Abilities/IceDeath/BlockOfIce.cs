using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class BlockOfIce : Skill
{
	[SerializeField] private BlockOfIceProjectile _iceArrow;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private float _castTime = 2.5f;
	private bool _canCast = true;
	private Vector2 _mousePos;
	private bool _enabled;
	private Energy _energy;
	private RuneComponent _rune;

	protected override bool IsCanCast => IsCanCastCheck();

	private bool IsCanCastCheck()
	{
		if (_rune.CurrentValue >= 1)
		{
			_rune.CmdUse(1);
			return true;
		}
		else
		{
			return false;
		}
	}
	/*private void Update()
	{
		if (!_enabled) return;

		if (Input.GetMouseButtonDown(0))
		{
			//PayCost();
			StartCoroutine(Casting());
		}
		if(Input.GetMouseButtonDown(1)) 
		{
			//Cancel();
		}
	}*/
	private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
			if (_playerLinks.Resources[i].Type == ResourceType.Rune)
			{
				_rune = (RuneComponent)_playerLinks.Resources[i];
			}
		}

	}

	private void Shoot()
	{
		Debug.Log("shot");
		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - (Vector2)_playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		CmdCreateProjecttile(angle);
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
		//_playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
		//Cancel();
	}
	[Command]
	private void CmdCreateProjecttile(float angle)
	{
		BlockOfIceProjectile projectile = Instantiate(_iceArrow, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		projectile.Init(_playerLinks, _energy.CurrentValue, false, this);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, _energy.CurrentValue);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<BlockOfIceProjectile>().Init(_playerLinks, manaValue, false, this);
	}

	/*private IEnumerator Casting()
	{
		_playerLinks.Move.CanMove = false;
		yield return new WaitForSeconds(_castTime);
		//if (_canCast && _playerLinks.RuneComponent.RemoveRune(1, this))
		{
			_playerLinks.Move.CanMove = true;
			Shoot();
		}
		//else
		{
			_playerLinks.Move.CanMove = true;
			//Cancel();
		}
	}*/

	protected override IEnumerator PrepareJob()
	{
		while (float.IsPositiveInfinity(_mousePos.x))
		{
			if (Input.GetMouseButton(0))
			{
				_mousePos = GetMousePoint();
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
		_mousePos = Vector2.positiveInfinity;
	}
}
