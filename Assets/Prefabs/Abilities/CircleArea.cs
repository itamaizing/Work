using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleArea : MonoBehaviour
{
    [SerializeField] CircleCollider2D _colider;
    [SerializeField] SpriteRenderer _sprite;

    private bool _isConcernsEnemy;

    public bool IsConcernsEnemy { get => _isConcernsEnemy; set => _isConcernsEnemy = value; }

    public void SetSize(float size)
    {
        _sprite.size = new Vector2(size, size);
        _colider.radius = size / 2f;
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
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_sprite.size != Vector2.zero && collision.transform != transform.parent && collision.transform.TryGetComponent(out UIPlayerComponents enemy))
        {
            _isConcernsEnemy = false;
            enemy.ChangeSelection(false);
        }
    }
}