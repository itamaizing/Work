using UnityEngine;

public class CameraMoveNew : MonoBehaviour
{
    [SerializeField] float m_MoveSpeed = 20;
    [SerializeField] int m_EdgePixels = 1;

    [SerializeField] float m_MinX = -64;
    [SerializeField] float m_MaxX = 64;
    [SerializeField] float m_MinY = -64;
    [SerializeField] float m_MaxY = 64;
    [SerializeField] private CameraBoxLimiter _limiter;
    [SerializeField] int m_OutsidePixels = 40;
    public TestController Target;

    public float moveSpeed = 5f;
    public float fastMoveSpeed = 10;
    public float rotationSpeed = 0.1f;

    private Vector3 lastMousePosition;
    private Vector3 initialPosition;
    private Quaternion initialRotation;


    public Transform player;
    public Vector3 offset;
    public float smoothSpeed = 0.125f;
    public float minDistance = 2f;


    private Vector3 _velocity;

    public float sensitivity = 10f;
    public float minY = 5f;
    public float maxY = 30f;

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        //Cursor.lockState = CursorLockMode.Confined;
    }

    private void LateUpdate()
    {
        if (!InputBlocker.IsBlocked(InputLayer.Camera))
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
                return;
            HandleMovement();
            HandleRotation();
            HandleResetPosition();
            HandleZoom();
            HandleMovement2();
        }

        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        float distance = direction.magnitude;

        if (distance < minDistance)
        {
            direction = direction.normalized * minDistance;
        }

        Vector3 targetPosition = player.position + offset;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, smoothSpeed);
    }

    private void HandleMovement()
    {
        float moveSpeedCurrent = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;

        float moveX = 0;
        float moveZ = 0;

        float moveY = 0f;

        /*if (Input.GetKey(KeyCode.Q))
        {
            moveY = -1f;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            moveY = 1f;
        }*/

        if (Input.GetKey(KeyCode.UpArrow))
        {
            //moveZ = 1f;
            moveY = 1f;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            //moveZ = -1f;
            moveY = -1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            moveX = 1f;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveX = -1f;
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

    private void HandleZoom()
    {
        if (InputBlocker.IsPointerOverUI())
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        Vector3 move = transform.forward * scroll * sensitivity;

        transform.Translate(move, Space.World);

        Vector3 currentPosition = transform.position;
        currentPosition.y = Mathf.Clamp(currentPosition.y, minY, maxY);
        transform.position = currentPosition;
    }

    void HandleMovement2()
    {
        Vector3 mp = Input.mousePosition;

        if (mp.x < -m_OutsidePixels || mp.x > Screen.width + m_OutsidePixels ||
            mp.y < -m_OutsidePixels || mp.y > Screen.height + m_OutsidePixels)
            return;

        float dirX = 0f;
        float dirZ = 0f;

        if (mp.x <= m_EdgePixels)                       dirX -= 1f;
        if (mp.x >= Screen.width - 1 - m_EdgePixels)    dirX += 1f;
        if (mp.y <= m_EdgePixels)                       dirZ -= 1f;
        if (mp.y >= Screen.height - 1 - m_EdgePixels)   dirZ += 1f;

        if (dirX != 0f || dirZ != 0f)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = transform.up;
                forward.y = 0f;
            }
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 movement = (right * dirX + forward * dirZ);
            if (movement.sqrMagnitude > 1f) movement.Normalize();

            transform.Translate(movement * m_MoveSpeed * Time.deltaTime, Space.World);
        }

        ApplyCurrentBounds();
    }

    private void ApplyCurrentBounds()
    {
        if (_limiter == null)
        {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, m_MinX, m_MaxX),
                transform.position.y,
                Mathf.Clamp(transform.position.z, m_MinY, m_MaxY));
            return;
        }

        Bounds bounds = _limiter.GetCurrentBounds();
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, bounds.min.x, bounds.max.x);
        pos.z = Mathf.Clamp(pos.z, bounds.min.z, bounds.max.z);

        transform.position = pos;
    }
}