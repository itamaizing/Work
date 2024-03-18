using Pathfinding;
using UnityEngine;

public class test : MonoBehaviour
{
    public Collider2D obstacleCollider;

    public void UpdateGraphForObstacle()
    {
        Bounds bounds = obstacleCollider.bounds;

        GraphUpdateObject guo = new GraphUpdateObject(bounds);
        AstarPath.active.UpdateGraphs(guo);
    }
}