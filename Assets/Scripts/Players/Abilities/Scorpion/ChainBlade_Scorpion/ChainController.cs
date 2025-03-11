using Mirror;
using UnityEngine;

public class ChainController : NetworkBehaviour
{
    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _maxDistance = 4f;

    private LineRenderer _line;
    private Transform _playerTransform;
    private Vector3 _direction;
    private Vector3 _startPos;
    private ChainBlade_Scorpion _skill;
    private bool _isFlying;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        gameObject.SetActive(false);
    }

    public void Init(Transform player, Vector3 direction, ChainBlade_Scorpion skill)
    {
        _playerTransform = player;
        _direction = direction;
        _startPos = player.position;
        _skill = skill;
        _isFlying = true;

        UpdatePositions();
    }

    private void Update()
    {
        if (!_isFlying) return;

        transform.position += _direction * _speed * Time.deltaTime;

        if (Vector3.Distance(_startPos, transform.position) >= _maxDistance)
            DeactivateChain();

        UpdatePositions();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Character target) && target != _playerTransform.GetComponent<Character>())
        {
            _isFlying = false;
            _skill.PullTarget(target);
        }
        else if (other.CompareTag("Obstacle"))
        {
            DeactivateChain();
        }
    }

    public void UpdatePositions()
    {
        if (_playerTransform == null || _line == null)
            return;

        _line.SetPosition(0, _playerTransform.position + Vector3.up);
        _line.SetPosition(1, transform.position + Vector3.up);
    }

    private void DeactivateChain()
    {
        _isFlying = false;
        gameObject.SetActive(false);
    }
}
