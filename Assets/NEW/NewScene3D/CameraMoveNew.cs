using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMoveNew : MonoBehaviour
{
    public float moveSpeed = 0.1f;

    private Camera _camera;
    private Vector3 lastMousePosition;

    private void Start()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButton(2))
        {
            Vector3 mousePosition = Input.mousePosition;

            if (Input.GetMouseButtonDown(2))
            {
                lastMousePosition = mousePosition;
            }

            Vector3 deltaMousePosition = lastMousePosition - mousePosition;

            _camera.transform.position += new Vector3(deltaMousePosition.x * moveSpeed, 0, deltaMousePosition.y * moveSpeed);

            lastMousePosition = mousePosition;
        }
    }
}
