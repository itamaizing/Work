using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawArrow : MonoBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 3;
        _lineRenderer.startWidth = 0.125f;
        _lineRenderer.endWidth = 0.125f;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    public void DrawCurvedArrow(Vector3 startPoint, Vector3 endPoint, bool SetArrowDirectionUp)
    {
        Vector3 midPoint = (startPoint + endPoint) / 2;

        Vector3 direction = (endPoint - startPoint).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
        if (SetArrowDirectionUp) midPoint -= perpendicular * 0.5f;
        else midPoint += perpendicular * 0.5f;

        _lineRenderer.positionCount = 3;
        _lineRenderer.SetPosition(0, startPoint);
        _lineRenderer.SetPosition(1, midPoint);
        _lineRenderer.SetPosition(2, endPoint);
    }

    public void Clear()
    {
        _lineRenderer.positionCount = 0;
    }

    public void SetColor(Color color)
    {
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
    }
}
