using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Icecloud : Ability
{
	[SerializeField] private IceCloudProjectile _projectile;
	[SerializeField] private Character _playerLinks;
	//[SerializeField] private RunePlayer _rune;
	//[SerializeField] private Rigidbody2D _rb;

	private Vector2 _mousePos;
	private float _angle;
	private bool _enabled;
	
	private void Update()
	{
		if (!_enabled) return;
		
		if (Input.GetMouseButtonDown(0))
		{
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
		if(Input.GetMouseButtonDown(1)) 
		{
			Cancel();
		}
	}

	protected override void Cast()
	{
		_enabled = true;
		if(_playerLinks.RuneComponent.RemoveRune(1, this)) 
		{
			Shoot();
		}
	}

	protected override void Cancel()
	{
		_enabled = false;
	}

	private void Shoot()
	{
		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		IceCloudProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		projectile.dad = _playerLinks;
		projectile.energyDad = _playerLinks.Stamina.Value;
		_playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
		Cancel();
	}
	//���������� paycost ��� ���� ��� ��� ���� ����� �� �������� ��� �������

}
