using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FisuraTail : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BoxCollider2D _collider;

    private void Awake()
    {
        _collider.enabled = false;
    }

    public void Activate(float livetime)
    {
        _collider.enabled = true;
        Destroy(gameObject, livetime);
    }

    public void Rotate()
    {
        Vector3 dir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    public void SetSize(Vector2 vector2)
    {
        _spriteRenderer.size = vector2;
        _collider.size = vector2;

        _spriteRenderer.transform.Translate(new Vector3(0, vector2.y, 0));
    }

    public void AddLength(float value)
    {
        Vector2 newSize = new Vector2(_spriteRenderer.size.x, _spriteRenderer.size.y + value);
        _spriteRenderer.size = newSize;
        _collider.size = newSize;

        _spriteRenderer.transform.Translate(new Vector3(0, value, 0));
    }
}
