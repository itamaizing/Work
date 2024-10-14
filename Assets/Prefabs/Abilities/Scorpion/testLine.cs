using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Transform transform1;
    public Transform transform2;
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer.useWorldSpace = true;
    }

    // Update is called once per frame
    void Update()
    {
        lineRenderer.SetPosition(0, transform1.position);
        lineRenderer.SetPosition(1, transform2.position);
    }
}
