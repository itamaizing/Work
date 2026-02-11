using UnityEngine;

public class CameraInsideBox : MonoBehaviour
{
    [SerializeField] private BoxCollider boundsCollider;
    [SerializeField] private Transform targetCamera;

    private Vector3 minBounds;
    private Vector3 maxBounds;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main.transform;

        Bounds bounds = boundsCollider.bounds;
        minBounds = bounds.min;
        maxBounds = bounds.max;
    }

    private void LateUpdate()
    {
        Vector3 pos = targetCamera.position;

        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        pos.z = Mathf.Clamp(pos.z, minBounds.z, maxBounds.z);

        targetCamera.position = pos;
    }
}
