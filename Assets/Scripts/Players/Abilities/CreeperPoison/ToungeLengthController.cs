using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToungeLengthController : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Transform _startTarget;
    private Transform _endTarget;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }
    public void Clear(Transform pos)
    {
        lineRenderer.SetPosition(0, pos.position);
        lineRenderer.SetPosition(1, pos.position);
    }

    public void AssignTarget(Transform startTarget, Transform newTarget)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startTarget.position);
        lineRenderer.SetPosition(1, newTarget.position);
        _startTarget = startTarget;
        _endTarget = newTarget;
    }

    private void Update()
    {
        lineRenderer.SetPosition(0, _startTarget.position);
        lineRenderer.SetPosition(1, _endTarget.position);
    }
}
