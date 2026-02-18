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
}
