using UnityEngine;

public class MoveScraderAnim : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private MinionMove _minionMove;

    private Vector3 _lastPosition;
    private float _distanceAccumulator = 0f;

    private void Start()
    {
        if (_minionMove == null)
            _minionMove = GetComponent<MinionMove>();

        _lastPosition = _minionMove.transform.position;
    }

    private void Update()
    {
        TrackMovement();
    }

    private void TrackMovement()
    {
        Vector3 currentPos = _minionMove.transform.position;
        float moved = Vector3.Distance(currentPos, _lastPosition);
        _distanceAccumulator += moved;

        if (_distanceAccumulator >= 1f)
        {
            _animator?.SetTrigger("MoveScared");
            _distanceAccumulator -= 1f;
        }

        _lastPosition = currentPos;
    }
}