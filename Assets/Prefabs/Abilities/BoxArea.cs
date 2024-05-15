using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxArea : MonoBehaviour
{
    [SerializeField] BoxCollider2D _colider;
    [SerializeField] SpriteRenderer _sprite;
    [SerializeField] LayerMask _layer;

    private List<PlayerMove> _enemies = new List<PlayerMove>();
    private bool _isConcernsEnemy;

    public bool IsConcernsEnemy { get => _isConcernsEnemy; set => _isConcernsEnemy = value; }

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
        if(collision.transform != transform.parent && collision.transform.TryGetComponent(out PlayerMove enemy))
        {
            _isConcernsEnemy = true;
            _enemies.Add(enemy);
            enemy.CircleSelect.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform != transform.parent && collision.transform.TryGetComponent(out PlayerMove enemy))
        {
            _enemies.Remove(enemy);
            enemy.CircleSelect.SetActive(false);
            if(_enemies.Count <= 0)
            {
                _isConcernsEnemy = false;
            }
        }
    }
}
