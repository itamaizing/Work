using UnityEngine;

public class test1 : MonoBehaviour
{
    public Collider2D obstacleCollider;

    public void UpdateGraphForObstacle()
    {
        Bounds bounds = obstacleCollider.bounds;
    }
}