using UnityEngine;

public class MiniMapFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    private void LateUpdate()
    {
        MiniMapFollowPosition();
    }

    private void MiniMapFollowPosition()
    {
        if (target == null) return;

        Vector3 pos = transform.position;
        pos.x = target.position.x;
        pos.z = target.position.z;
        transform.position = pos;
    }
}
