using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public float panSpeed = 5f;
    public GameObject Select;
    public Transform spriteObject;

    private Camera _camera;
    private Vector2 spriteSize;
    private Vector2 spritePosition;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        spriteSize = spriteObject.GetComponent<SpriteRenderer>().bounds.size;
        spritePosition = spriteObject.position;
    }

    private void Update()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 cameraPosition = transform.position;

        float activationZoneX = Screen.width * 0.01f; // 1% ширины экрана с краю
        float activationZoneY = Screen.height * 0.01f; // 1% высоты экрана с краю

        float cameraHalfWidth = _camera.orthographicSize * _camera.aspect;
        float cameraHalfHeight = _camera.orthographicSize;

        if (Input.GetMouseButton(0)) // Левая кнопка мыши удерживается
        {
            float deltaX = -Input.GetAxis("Mouse X") * panSpeed * Time.deltaTime * 8;
            float deltaY = -Input.GetAxis("Mouse Y") * panSpeed * Time.deltaTime * 8;

            cameraPosition.x += deltaX;
            cameraPosition.y += deltaY;
        }
        else
        {
            if (mousePosition.x < activationZoneX)
            {
                cameraPosition.x -= panSpeed * Time.deltaTime;
            }
            else if (mousePosition.x > Screen.width - activationZoneX)
            {
                cameraPosition.x += panSpeed * Time.deltaTime;
            }

            if (mousePosition.y < activationZoneY)
            {
                cameraPosition.y -= panSpeed * Time.deltaTime;
            }
            else if (mousePosition.y > Screen.height - activationZoneY)
            {
                cameraPosition.y += panSpeed * Time.deltaTime;
            }
        }

        float minX = spritePosition.x - spriteSize.x / 2 + cameraHalfWidth;
        float maxX = spritePosition.x + spriteSize.x / 2 - cameraHalfWidth;
        float minY = spritePosition.y - spriteSize.y / 2 + cameraHalfHeight;
        float maxY = spritePosition.y + spriteSize.y / 2 - cameraHalfHeight;

        cameraPosition.x = Mathf.Clamp(cameraPosition.x, minX, maxX);
        cameraPosition.y = Mathf.Clamp(cameraPosition.y, minY, maxY);

        transform.position = cameraPosition;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameObject gameObject = Select.GetComponent<SelectObject>().SelectedObject;
            transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, transform.position.z);
        }
    }
}