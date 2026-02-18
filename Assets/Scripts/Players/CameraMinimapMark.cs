using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CameraMinimapMark : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float maxDistance = 500f;

    private LineRenderer _line;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 5;
        _line.loop = false;

        if (!targetCamera)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        DrawCameraRect();
    }

    private void DrawCameraRect()
    {
        Vector3[] corners = new Vector3[4];

        corners[0] = GetViewportPointWorld(0, 0);
        corners[1] = GetViewportPointWorld(1, 0);
        corners[2] = GetViewportPointWorld(1, 1);
        corners[3] = GetViewportPointWorld(0, 1);

        for (int i = 0; i < 4; i++)
            _line.SetPosition(i, corners[i]);

        _line.SetPosition(4, corners[0]);
    }

    private Vector3 GetViewportPointWorld(float x, float y)
    {
        Ray ray = targetCamera.ViewportPointToRay(new Vector3(x, y, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask))
        {
            return hit.point;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        return ray.origin + ray.direction * maxDistance;
    }

    #region V2
    /* [SerializeField] private Camera targetCamera;
    [SerializeField] private float groundY = 0f;

    private LineRenderer _line;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 5;
        _line.loop = false;

        if (!targetCamera)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        DrawProjectedRect();
    }

    private void DrawProjectedRect()
    {
        float cameraHeight = targetCamera.transform.position.y - groundY;

        float halfHeight;
        float halfWidth;

        if (targetCamera.orthographic)
        {
            halfHeight = targetCamera.orthographicSize;
            halfWidth = halfHeight * targetCamera.aspect;
        }
        else
        {
            float fovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;
            halfHeight = Mathf.Tan(fovRad * 0.5f) * cameraHeight;
            halfWidth = halfHeight * targetCamera.aspect;
        }

        Vector3 center = new Vector3(
            targetCamera.transform.position.x,
            groundY,
            targetCamera.transform.position.z
        );

        Vector3[] corners = new Vector3[4];

        corners[0] = center + new Vector3(-halfWidth, 0, -halfHeight);
        corners[1] = center + new Vector3(halfWidth, 0, -halfHeight);
        corners[2] = center + new Vector3(halfWidth, 0, halfHeight);
        corners[3] = center + new Vector3(-halfWidth, 0, halfHeight);

        for (int i = 0; i < 4; i++)
            _line.SetPosition(i, corners[i]);

        _line.SetPosition(4, corners[0]);
    } */
    #endregion
}
