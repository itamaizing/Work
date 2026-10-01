using UnityEngine;

public class CameraBoxLimiter : MonoBehaviour
{
    [SerializeField] private Transform _targetCamera;
    [SerializeField] private BoxCollider _boundsCollider;

    [SerializeField] private float _softZone = 0.5f; 
    [SerializeField] private float _smoothSpeed = 5f;
    
    private BoxCollider _savedBoundsCollider;

    private Vector3 minBounds;
    private Vector3 maxBounds;
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        if (_targetCamera == null)
            _targetCamera = Camera.main.transform;

        Bounds bounds = _boundsCollider.bounds;
        minBounds = bounds.min;
        maxBounds = bounds.max;
    }

    private void LateUpdate()
    {
        CameraLimiter();
    }

    private void CameraLimiter()
    {
        Vector3 desiredPos = _targetCamera.position;
        Vector3 currentPos = _targetCamera.position;

        desiredPos.x = SoftLimitAxis(currentPos.x, minBounds.x, maxBounds.x);
        desiredPos.y = SoftLimitAxis(currentPos.y, minBounds.y, maxBounds.y);
        desiredPos.z = SoftLimitAxis(currentPos.z, minBounds.z, maxBounds.z);

        _targetCamera.position = Vector3.SmoothDamp(currentPos, desiredPos, ref velocity, 1f / _smoothSpeed);
    }

    private float SoftLimitAxis(float value, float min, float max)
    {
        if (value < min + _softZone)
        {
            float t = Mathf.InverseLerp(min, min + _softZone, value);
            return Mathf.Lerp(min, min + _softZone, t * t);
        }

        if (value > max - _softZone)
        {
            float t = Mathf.InverseLerp(max, max - _softZone, value);
            return Mathf.Lerp(max, max - _softZone, t * t);
        }

        return value;
    }
    
    #region Установка новой зоны
    public void SetTemporaryBounds(BoxCollider newBounds)
    {
        if (_savedBoundsCollider == null)
            _savedBoundsCollider = _boundsCollider;

        _boundsCollider = newBounds;
        ApplyBounds();
    }

    public void RestoreOriginalBounds()
    {
        if (_savedBoundsCollider == null) return;
        _boundsCollider = _savedBoundsCollider;
        _savedBoundsCollider = null;
        ApplyBounds();
    }

    private void ApplyBounds()
    {
        if (_boundsCollider == null) return;
        Bounds b = _boundsCollider.bounds;
        minBounds = b.min;
        maxBounds = b.max;
        velocity  = Vector3.zero;
    }
    
    public Bounds GetCurrentBounds()
    {
        return _boundsCollider.bounds;
    }
    #endregion
    
}
