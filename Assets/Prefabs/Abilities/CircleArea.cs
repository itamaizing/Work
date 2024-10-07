using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleArea : MonoBehaviour
{
    [SerializeField] CircleCollider2D _colider;
    [SerializeField] SpriteRenderer _sprite;

    private bool _isConcernsEnemy;
    private float _damage;
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

    public void SetSize(float size, float damage)
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
            hpEnemy.PhantomValueShow(_damage);
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
			hpEnemy.PhantomValueShow(0);
		}
	}
}