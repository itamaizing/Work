using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawCircle : MonoBehaviour
{
    public int segments = 64;
    public Color lineColor = Color.white;
    public LineRenderer lineRenderer;

    private void Update()
    {
        transform.localPosition = Vector3.zero;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    public void Draw(float radius)
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.useWorldSpace = false;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.positionCount = segments + 1;

        float angle = 0f;
        float angleIncrement = 360f / segments;

        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0f));

            angle += angleIncrement;
        }
    }

    public void Clear()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }

    public void SetColor(Color newColor)
    {
		lineColor = newColor;
	}
}
