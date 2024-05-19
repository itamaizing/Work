using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Tentacles : MonoBehaviour
{
    [SerializeField] private LayerMask _layer;
    private List<Collision2D> _collisions = new List<Collision2D>();
    public List<Collision2D> Collisions { get => _collisions; set => _collisions = value; }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemies") && collision.transform != transform.parent)
        {
            _collisions.Add(collision);
            collision.gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        Debug.LogWarning("CollisionEnter2d");
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemies"))
        {
            //_collisions.Remove(collision);
            collision.gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Debug.LogWarning("CollisionExit2d");
        }
    }
}
