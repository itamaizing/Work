using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class IcePuddle : Ability
{
	[SerializeField] private IcePuddleObject _puddle;
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
		IcePuddleObject puddle = Instantiate(_puddle, gameObject.transform.position, Quaternion.identity);
		puddle.dad = _playerLinks.gameObject;
		puddle.energy = (Energy)Mana;
		puddle.healthComponent = _playerLinks.Health;
	}
}
