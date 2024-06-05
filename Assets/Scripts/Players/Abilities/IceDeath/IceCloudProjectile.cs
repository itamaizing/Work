using UnityEngine;

public class IceCloudProjectile : MonoBehaviour
{
	public float energyDad;
	[HideInInspector]public Character dad;

	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] GameObject _hitEffect;
	[SerializeField] private float _force;
	[SerializeField] private float _distance = 5;
	
	private Vector2 startPos;

	private void Awake()
	{
		startPos = transform.position;		
		_rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
	}

	private void Update()
	{
		if(Vector2.Distance(transform.position, startPos) > _distance * GlobalVariable.cellSize)
		{
			Explode();
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject == dad.gameObject || collision.CompareTag("Ability"))
			return;
		//damage, freez etc
		if(collision.TryGetComponent<Character>(out var target))
		{
			//float duration = 1 + dad.Stamina.Value / 20;
			float duration = 1 + energyDad / 20;
			//target.CharacterState.energy = dad.Stamina;
			target.Health.TakeDamage(10 + energyDad / 4, DamageType.Physical);
			target.CharacterState.AddState(new FrozenState(), duration, 30, States.Frozen);

			//dad.Stamina.Use(duration * 20);
			//damage
			GetComponent<Collider2D>().enabled = false;
		}
		Explode();
	}

	private void Explode()
	{
		if (_hitEffect != null)
		{
			GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
			Destroy(hitEffect, 5f);
		}
		Destroy(gameObject);
	}
}
