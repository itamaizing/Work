using UnityEngine;

public class CameraBoxLimiter : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;
    [SerializeField] private BoxCollider boundsCollider;

    [SerializeField] private float softZone = 0.5f; 
    [SerializeField] private float smoothSpeed = 5f;

    private Vector3 minBounds;
    private Vector3 maxBounds;
    private Vector3 velocity = Vector3.zero;

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
        CameraLimiter();
    }

    private void CameraLimiter()
    {
        Vector3 desiredPos = targetCamera.position;
        Vector3 currentPos = targetCamera.position;

        desiredPos.x = SoftLimitAxis(currentPos.x, minBounds.x, maxBounds.x);
        desiredPos.y = SoftLimitAxis(currentPos.y, minBounds.y, maxBounds.y);
        desiredPos.z = SoftLimitAxis(currentPos.z, minBounds.z, maxBounds.z);

        targetCamera.position = Vector3.SmoothDamp(currentPos, desiredPos, ref velocity, 1f / smoothSpeed);
    }

    private float SoftLimitAxis(float value, float min, float max)
    {
        if (value < min + softZone)
        {
            float t = Mathf.InverseLerp(min, min + softZone, value);
            return Mathf.Lerp(min, min + softZone, t * t);
        }

        if (value > max - softZone)
        {
            float t = Mathf.InverseLerp(max, max - softZone, value);
            return Mathf.Lerp(max, max - softZone, t * t);
        }

        return value;
    }
}
