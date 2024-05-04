using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainController : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Transform _startTarget;
    private Transform target;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void AssignTarget(Transform startTarget, Transform newTarget)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startTarget.position);
        _startTarget = startTarget;
        target = newTarget;
    }

    // Update is called once per frame
    private void Update()
    {
        lineRenderer.SetPosition(0, _startTarget.position);
        lineRenderer.SetPosition(1, target.position);
    }
}
