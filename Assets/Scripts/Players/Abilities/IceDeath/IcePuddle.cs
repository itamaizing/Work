using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class IcePuddle : Ability
{
	[SerializeField] private IcePuddleObject _puddle;
	[SerializeField] private PlayerLinks _playerLinks;
	//[SerializeField] private Rigidbody2D _rb;
	//[SerializeField] private HealthPlayer _healthPlayer;
	//[SerializeField] private RunePlayer _rune;

	protected override void Cast()
	{
		PayCost();
		if (_playerLinks.RunePlayer.RemoveRune(1, this))
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
		puddle.dad = _playerLinks.gameObject;
		puddle.energyPlayer = (EnergyPlayer)Mana;
		puddle.healthPlayer = _playerLinks.HealthPlayer;
	}
}
