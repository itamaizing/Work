using UnityEngine;

public class CameraMoveNew : MonoBehaviour
{
    [SerializeField] float m_MoveSpeed = 20;
    [SerializeField] float m_BorderDistance = 5;

    [SerializeField] float m_MinX = -64;
    [SerializeField] float m_MaxX = 64;
    [SerializeField] float m_MinY = -64;
    [SerializeField] float m_MaxY = 64;
    [SerializeField] private CameraBoxLimiter _limiter;
    public TestController Target;

    public float moveSpeed = 5f;
    public float fastMoveSpeed = 10;
    public float rotationSpeed = 0.1f;

    private Vector3 lastMousePosition;
    private Vector3 initialPosition;
    private Quaternion initialRotation;


    public Transform player;
    public Vector3 offset;
    public float smoothSpeed = 0.125f; // Скорость плавного перемещения
    public float minDistance = 2f; // Минимальное расстояние от игрока


    private Vector3 _velocity; // Переменная для плавного движения

    public float sensitivity = 10f;
    public float minY = 5f; // Minimum Y position (zoom in limit)
    public float maxY = 30f; // Maximum Y position (zoom out limit)

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
            return;
        HandleMovement();
        HandleRotation();
        HandleResetPosition();
        HandleZoom();
        HandleMovement2();
        if (player == null) return;

        // Расчет расстояния от камеры до игрока
        Vector3 direction = player.position - transform.position;
        float distance = direction.magnitude;

        // Если расстояние меньше минимального, то корректируем положение
        if (distance < minDistance)
        {
            direction = direction.normalized * minDistance;
        }


        // Вычисление новой позиции камеры
        Vector3 targetPosition = player.position + offset;

        // Плавное перемещение камеры
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
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // Calculate the new position based on the scroll input and sensitivity
        // This moves the camera along its local Z-axis (forward/backward)
        Vector3 move = transform.forward * scroll * sensitivity;

        // Apply the movement
        transform.Translate(move, Space.World);

        // Optional: Clamp the camera's vertical position to stay within bounds
        Vector3 currentPosition = transform.position;
        currentPosition.y = Mathf.Clamp(currentPosition.y, minY, maxY);
        transform.position = currentPosition;
    }

    void HandleMovement2()
    {
        Vector2 screenpos = new Vector2(
            Input.mousePosition.x * 100f / Screen.width,
            Input.mousePosition.y * 100f / Screen.height);

        Vector3 movement = Vector3.zero;

        if (screenpos.x < m_BorderDistance) 
            movement.x -= 1f;
    
        if (screenpos.x > 100f - m_BorderDistance) 
            movement.x += 1f;

        if (screenpos.y < m_BorderDistance) 
            movement.y -= 1f;
    
        if (screenpos.y > 100f - m_BorderDistance) 
            movement.y += 1f;

        float currentSpeed = m_MoveSpeed * Time.deltaTime;
        transform.Translate(movement * currentSpeed, Space.Self);

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
