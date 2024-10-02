using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawArrow : MonoBehaviour
{
    public LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 3;
        lineRenderer.startWidth = 0.125f;
        lineRenderer.endWidth = 0.125f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    public void DrawCurvedArrow(Vector3 startPoint, Vector3 endPoint, bool SetArrowdirectionUp)
    {
        Vector3 midPoint = (startPoint + endPoint) / 2;

        Vector3 direction = (endPoint - startPoint).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
        if (SetArrowdirectionUp) midPoint -= perpendicular * 0.5f;
        else midPoint += perpendicular * 0.5f;

        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, midPoint);
        lineRenderer.SetPosition(2, endPoint);
    }

    public void Clear()
    {
        lineRenderer.positionCount = 0;
    }

    public void SetColor(Color color)
    {
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}
