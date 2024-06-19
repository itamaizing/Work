using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class IceCloudProjectile : MonoBehaviour
{
	public float energyDad;
	//[HideInInspector] public GameObject dadGm;

	private Character _dad;
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private GameObject _hitEffect;
	[SerializeField] private SpriteRenderer _spriteRenderer;
	[SerializeField] private float _force;
	[SerializeField] private float _distance = 5;
	
	private Vector2 startPos;
	private bool _initialized = false;

	public void Init(GameObject dad)
	{
		_dad = dad.GetComponent<Character>();
		Debug.Log("bullet");
		_initialized = true;
	}
	private void Awake()
	{
		//_dad = dadGm.GetComponent<Character>();
		startPos = transform.position;
		_rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
	}

	private void Update()
	{
		//if (!_initialized) return;

        _spriteRenderer.DOFade(0, 1);
		if(Vector2.Distance(transform.position, startPos) > _distance * GlobalVariable.cellSize)
		{
			Explode();
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (_dad == null) return;
		if (collision.gameObject == _dad.gameObject || collision.CompareTag("Ability"))
			return;
		//damage, freez etc
		if(collision.TryGetComponent<Character>(out var target))
		{
			//float duration = 1 + dad.Stamina.Value / 20;
			float duration = 1 + energyDad / 20;
			//target.CharacterState.energy = dad.Stamina;
			float curDamage = 10 + energyDad / 4;
			Energy energyLink = (Energy)_dad.Stamina;
			if (target.CharacterState.CheckForState(States.Frozen))
			{
				curDamage *= 1.4f;
			}
			energyLink.SumDamageMake(curDamage);
			target.Health.TakeDamage(curDamage, DamageType.Physical);
			target.CharacterState.AddState(new FrozenState(), _dad, duration, 30, States.Frozen);

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
