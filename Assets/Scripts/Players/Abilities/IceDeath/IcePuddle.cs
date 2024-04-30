using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class IcePuddle : Ability
{
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private HealthPlayer _healthPlayer;
	[SerializeField] private IcePuddleObject _puddle;
	[SerializeField] private RunePlayer _rune;

	protected override void Cast()
	{
		PayCost();
		if (_rune.RemoveRune(1, this))
		{
			Shoot();
		}
	}

	protected override void Cancel()
	{
		//вроде не было нужды для отмены каста, пока что....
	}
	private void Shoot()
	{
		IcePuddleObject puddle = Instantiate(_puddle, gameObject.transform.position, Quaternion.identity);
		puddle.dad = _rb.gameObject;
		puddle.energyPlayer = (EnergyPlayer)Mana;
		puddle.healthPlayer = _healthPlayer;
	}
}
