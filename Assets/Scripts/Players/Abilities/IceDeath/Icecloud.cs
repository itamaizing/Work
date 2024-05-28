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
	protected override void Cast()
	{
		PayCost();
		if(_playerLinks.RunePlayer.RemoveRune(1, this)) 
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
		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		IceCloudProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		projectile.dad = _playerLinks;
	}
	//���������� paycost ��� ���� ��� ��� ���� ����� �� �������� ��� �������

}
