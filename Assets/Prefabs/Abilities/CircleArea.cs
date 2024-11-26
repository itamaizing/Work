using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class CircleArea : MonoBehaviour
{
    [SerializeField] SphereCollider _colider;
	//[SerializeField] private Collider _collider3d;
	[SerializeField] SpriteRenderer _sprite;
	[SerializeField] private DecalProjector _projector;

	private List<Health> _enemies = new List<Health>();
	private bool _isConcernsEnemy;
    private Damage _damage;
    /*private Damage _zeroDamage;

	private void Start()
	{
		_zeroDamage = new Damage
		{
			Value = 0,
			Type = DamageType.Physical,
			Range = AttackRangeType.RangeAttack,
		};
	}*/

	public bool IsConcernsEnemy { get => _isConcernsEnemy; set => _isConcernsEnemy = value; }

    public void SetSize(float size, Damage damage)
    {
        /*_sprite.size = new Vector2(size, size);
        _colider.radius = size / 2f;*/
		_damage = damage;

		gameObject.transform.localScale = new Vector3(size, size, 0);
		_projector.size = new Vector2(size, size);
		//_projector.pivot = new Vector3(0, size / 2, 0.01f);
	}

    public void SetColor(Color color)
    {
        _sprite.color = color;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (_sprite.size != Vector2.zero && collision.transform != transform.parent && collision.transform.TryGetComponent(out UIPlayerComponents enemy))
        {
            _isConcernsEnemy = true;
            enemy.ChangeSelection(true);
        }
        if(collision.TryGetComponent<Health>(out var hpEnemy) && collision.transform != transform.parent)
        {
            hpEnemy.ShowPhantomValue(_damage);
			_enemies.Add(hpEnemy);
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (_sprite.size != Vector2.zero && collision.transform != transform.parent && collision.transform.TryGetComponent(out UIPlayerComponents enemy))
        {
            _isConcernsEnemy = false;
            enemy.ChangeSelection(false);
        }
		if (collision.TryGetComponent<Health>(out var hpEnemy) && collision.transform != transform.parent)
		{
			Damage damage = _damage;
			damage.Value = 0;
			hpEnemy.ShowPhantomValue(damage);
			_enemies.Remove(hpEnemy);
		}
	}

	private void OnDestroy()
	{
		if (_enemies.Count > 0)
			for (int i = _enemies.Count - 1; i >= 0; i--)
			{
				Damage damage = _damage;
				damage.Value = 0;
				_enemies[i].ShowPhantomValue(damage);
				_enemies.Remove(_enemies[i]);
			}
	}

	private void OnDisable()
	{
		if (_enemies.Count > 0)
			for (int i = _enemies.Count - 1; i >= 0; i--)
			{
				Damage damage = _damage;
				damage.Value = 0;
				_enemies[i].ShowPhantomValue(damage);
				_enemies.Remove(_enemies[i]);
			}
	}
}