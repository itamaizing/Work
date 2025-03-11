using Mirror;
using UnityEngine;

public class ChainController : NetworkBehaviour
{
    private LineRenderer _line;
    private Transform _startPoint;
    private Transform _endPoint;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
    }

    public void Init(Transform start, Transform end)
    {
        _startPoint = start;
        _endPoint = end;
        UpdatePositions();
    }

    void Update()
    {
        if (!isServer) return;

        UpdatePositions();
    }

    public void UpdatePositions()
    {
        if (_startPoint != null && _endPoint != null)
        {
            _line.SetPosition(0, _startPoint.position + Vector3.up * 1f);
            _line.SetPosition(1, _endPoint.position + Vector3.up * 1f);
        }
    }
}
