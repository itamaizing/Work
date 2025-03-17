using System.Collections;
using UnityEngine;

public class TentacleProjectile : MonoBehaviour
{
    [SerializeField] private bool _isPreview = true;
    [SerializeField] private DrawCircleTentacle _drawCircle;
    [SerializeField] private GameObject tentacle;
    [SerializeField] private LayerMask obstecls;

    private Character _player;
    private Character _target;
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private bool _isAttackingPsiEnergyActive;
    private float _currentDamage;
    private float _grabDuration = 1.2f;
    private float _radius = 3f;

    private bool _radiusView;
    private bool _isCollidedWithOtherCharacter = false;
    private bool _isPullTarget = false;
    private Coroutine _radiusUpdateCoroutine;

    public bool IsPreview { get => _isPreview; set => _isPreview = value; }
    public GameObject Tentacle { get => tentacle; set => tentacle = value; }
    public float Radius => _radius;

    private void Awake()
    {
        _drawCircle = GetComponent<DrawCircleTentacle>();
    }

    private void Start()
    {
        Invoke("drawCircleRadius", 0.1f);
    }

    private void OnDestroy()
    {
        if (_drawCircle != null)
        {
            _drawCircle.Clear();
        }

        if (_radiusUpdateCoroutine != null)
        {
            StopCoroutine(_radiusUpdateCoroutine);
        }
    }

    public void Init(Character player, Character target, Vector3 startPosition, Vector3 endPosition,
        bool isAttackingPsiEnergyActive, float currentDamage)
    {
        _player = player;
        _target = target;
        _startPosition = startPosition;
        _endPosition = endPosition;
        _isAttackingPsiEnergyActive = isAttackingPsiEnergyActive;
        _currentDamage = currentDamage;

        transform.position = startPosition;

        Invoke("ReleaseTarget", _grabDuration);
    }

    public void StartTentaclesGrab()
    {
        if (_target != null && !_isPreview)
        {
            Vector3 direction = (_target.transform.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, _target.transform.position);

            float sphereRadius = 0.2f;

            if (Physics.SphereCast(transform.position, sphereRadius, direction, out RaycastHit hit, distance, obstecls)) return;

            _target.Move.CanMove = false;
            _isPullTarget = true;
            StartCoroutine(PullTarget());
        }
    }

    private void drawCircleRadius()
    {
        if (_drawCircle != null)
        {
            if (_isPreview)
            {
                _drawCircle.Draw(_radius);
                _drawCircle.SetColor(Color.red);
            }
            else _drawCircle.Clear();
        }
    }

    public void SetRadiusColor(Color color)
    {
        if (_drawCircle != null)
        {
            _drawCircle.SetColor(color);
        }
    }

    private IEnumerator PullTarget()
    {
        float elapsedTime = 0f;
        float baseSpeed = 0.05f;
        float speedIncrease = 0.05f;
        float minDistance = 0.5f;

        Vector3 lastTargetPosition = _target.transform.position;
        float targetDistanceAccumulator = 0f;

        while (elapsedTime < _grabDuration)
        {
            Vector3 toTentacle = transform.position - _target.transform.position;
            float distance = toTentacle.magnitude;

            if (distance <= minDistance) break;

            float speed = baseSpeed + (elapsedTime / 0.1f) * speedIncrease;

            if (_isCollidedWithOtherCharacter)
            {
                speed /= 2;
            }

            Vector3 direction = toTentacle.normalized;
            _target.transform.position += direction * speed;

            float traveled = Vector3.Distance(lastTargetPosition, _target.transform.position);
            targetDistanceAccumulator += traveled;

            if (targetDistanceAccumulator >= 0.1f)
            {
                targetDistanceAccumulator -= 0.1f;

                if (_player != null && _player.TryGetComponent<BasePsionicEnergy>(out var psiEnergy))
                {
                    psiEnergy.Add(0.3f);
                }
            }

            lastTargetPosition = _target.transform.position;

            elapsedTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void ReleaseTarget()
    {
        if (_target != null) _target.Move.CanMove = true;

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Character>(out Character character) && character != _target) _isCollidedWithOtherCharacter = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_isPullTarget) StartTentaclesGrab();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<Character>(out Character character) && character != _target) _isCollidedWithOtherCharacter = false;
    }
}
