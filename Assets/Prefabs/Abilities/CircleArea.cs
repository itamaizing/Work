using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleArea : MonoBehaviour
{
    [SerializeField] CircleCollider2D _colider;
	//[SerializeField] private Collider _collider3d;
	[SerializeField] SpriteRenderer _sprite;

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
        _sprite.size = new Vector2(size, size);
        _colider.radius = size / 2f;
        _damage = damage;
	}

    public void SetColor(Color color)
    {
        _sprite.color = color;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_sprite.size != Vector2.zero && collision.transform != transform.parent && collision.transform.TryGetComponent(out UIPlayerComponents enemy))
        {
            _isConcernsEnemy = true;
            enemy.ChangeSelection(true);
        }
        if(collision.TryGetComponent<Health>(out var hpEnemy) && collision.transform != transform.parent)
        {
            hpEnemy.ShowPhantomValue(_damage);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
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
		}
	}
}