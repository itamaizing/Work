using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Icecloud : Ability
{
	[SerializeField] private IceCloudProjectile _projectile;
	[SerializeField] private PlayerLinks _playerLinks;
	[SerializeField] private GameObject _croosFire;
	//[SerializeField] private RunePlayer _rune;
	//[SerializeField] private Rigidbody2D _rb;

	private Vector2 _mousePos;
	private float _angle;

	private void Awake()
	{
		_croosFire.SetActive(false);
		_isReady = false;

	}

	private void Update()
	{
		if (!_isReady) return;

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
		}
		if(Input.GetMouseButtonDown(1)) 
		{
			Cancel();
		}
	}

	protected override void Cast()
	{
		_isReady = true;
		_croosFire.SetActive(true);
		//if(Input.GetMouseButtonDown(0))
		/*if(_playerLinks.RunePlayer.RemoveRune(1, this)) 
		{
			Shoot();
		}*/
	}

	protected override void Cancel()
	{
		_isReady = false;
		_croosFire.SetActive(false);
	}

	private void Shoot()
	{
		//_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		//Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
		//float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		IceCloudProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, _angle));
		projectile.dad = _playerLinks;
		Cancel();
	}

}
