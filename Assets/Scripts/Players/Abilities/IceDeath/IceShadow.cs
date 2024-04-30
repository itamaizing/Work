using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class IceShadow : Ability
{
	[Header("Ability properties")]
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private HealthPlayer _healthPlayer;
	[SerializeField] private IceShadowObject _shadow;
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
		IceShadowObject projectile = Instantiate(_shadow, gameObject.transform.position, Quaternion.identity);
		projectile.dad = _rb.gameObject;
		projectile.energyPlayer = (EnergyPlayer)Mana;
		projectile.healthPlayer = _healthPlayer;
	}
}
