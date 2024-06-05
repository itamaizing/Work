using UnityEngine;

public class BoxArea : MonoBehaviour
{
    [SerializeField] BoxCollider2D _colider;
    [SerializeField] SpriteRenderer _sprite;

    public void SetSize(float width, float length)
    {
        _sprite.size = new Vector2(width, length);
        _colider.size = new Vector2(width, length);
        _colider.offset = new Vector2(0, length / 2);
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
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform != transform.parent && collision.transform.TryGetComponent(out Character enemy))
        {
            // bezdeistvie s enemy
        }
    }
}
