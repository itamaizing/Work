using UnityEngine;
using UnityEngine.UI;

public class MiniMapFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Toggle _miniMapToggle;
    [SerializeField] private bool _isRotation;

    private Quaternion _initialRotation;

    private void Start()
    {
        _initialRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void LateUpdate()
    {
        MiniMapFollowPosition();
        MiniMapFollowRotation();
    }

    private void MiniMapFollowRotation()
    {
        if (_target == null) return;

        if (_isRotation)
        {
            float yRotation = _target.eulerAngles.y;
            float xRotation = _target.eulerAngles.x;

            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
        else
        {
            transform.rotation = _initialRotation;
        }
    }

    private void MiniMapFollowPosition()
    {
        if (_target == null) return;

        Vector3 pos = transform.position;
        pos.x = _target.position.x;
        pos.z = _target.position.z;
        transform.position = pos;
    }

    public void MiniMapFollowRotationToggle(bool value) => _isRotation = value; 
}
