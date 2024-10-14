using UnityEngine;

public class CameraMoveNew : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float fastMoveSpeed = 10;
    public float rotationSpeed = 0.1f;

    private Vector3 lastMousePosition;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleResetPosition();
    }

    private void HandleMovement()
    {
        float moveSpeedCurrent = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        float moveY = 0f;
        if (Input.GetKey(KeyCode.Q))
        {
            moveY = -1f;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            moveY = 1f;
        }

        Vector3 movement = new Vector3(moveX, moveY, moveZ) * moveSpeedCurrent * Time.deltaTime;
        transform.Translate(movement, Space.Self);
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            transform.Rotate(Vector3.up, mouseDelta.x * rotationSpeed, Space.World);

            transform.Rotate(Vector3.right, -mouseDelta.y * rotationSpeed, Space.Self);
        }

        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        lastMousePosition = Input.mousePosition;
    }

    private void HandleResetPosition()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
    }
}
