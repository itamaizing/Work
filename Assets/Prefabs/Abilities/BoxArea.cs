using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BoxArea : MonoBehaviour
{
    [SerializeField] BoxCollider2D _colider;
    [SerializeField] SpriteRenderer _sprite;
    [SerializeField] private DecalProjector  _projector;

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

	public void SetSize(float width, float length, Damage damage)
    {
        /*_sprite.size = new Vector2(width, length);
        _colider.size = new Vector2(width, length);
        _colider.offset = new Vector2(0, length / 2);*/
        gameObject.transform.localScale = new Vector3(width, length, 0);
        _projector.size = new Vector2(width, length);
        _projector.pivot = new Vector3(0, length/2, 0.01f);
        _damage = damage;
    }

    public void SetColor(Color color)
    {
        _sprite.color = color;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.transform != transform.parent && collision.transform.TryGetComponent(out Character enemy))
        {
            // deistvie s enemy
        }
		if (collision.TryGetComponent<Health>(out var hpEnemy) && collision.transform != transform.parent)
		{
			hpEnemy.ShowPhantomValue(_damage);
		}
	}

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform != transform.parent && collision.transform.TryGetComponent(out Character enemy))
        {
            // bezdeistvie s enemy
        }
		if (collision.TryGetComponent<Health>(out var hpEnemy) && collision.transform != transform.parent)
		{
            Damage damage = _damage;
            damage.Value = 0;
			hpEnemy.ShowPhantomValue(damage);
		}
	}
}
