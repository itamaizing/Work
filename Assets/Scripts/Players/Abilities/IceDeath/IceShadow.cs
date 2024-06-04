using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class IceShadow : Ability
{
	[Header("Ability properties")]
	[SerializeField] private IceShadowObject _shadow;
	[SerializeField] private Character _playerLinks;
	//[SerializeField] private Rigidbody2D _rb;
	//[SerializeField] private HealthComponent _healthComponent;
	//[SerializeField] private RuneComponent _rune;

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
		IceShadowObject projectile = Instantiate(_shadow, gameObject.transform.position, Quaternion.identity);
		projectile.dad = _playerLinks.gameObject;
		projectile.energy = (Energy)Mana;
		projectile.healthComponent = _playerLinks.Health;
	}
}
