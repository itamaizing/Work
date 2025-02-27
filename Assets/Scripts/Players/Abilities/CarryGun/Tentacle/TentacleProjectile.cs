using System.Collections;
using UnityEngine;

public class TentacleProjectile : MonoBehaviour
{
    [SerializeField] private bool _isPreview;
    [SerializeField] private DrawCircleTentacle _drawCircle;
    [SerializeField] private Character _heroToTentacle;

    private Character _player;
    private Character _target;
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private bool _isAttackingPsiEnergyActive;
    private float _currentDamage;
    private float _grabDuration = 1.2f;
    private float _radius = 3f;

    private bool _isCollidedWithOtherCharacter = false;
    private Coroutine _radiusUpdateCoroutine;

    public Character HeroToTentacle { get => _heroToTentacle; set => _heroToTentacle = value; }

    private void Awake()
    {
        _drawCircle = GetComponent<DrawCircleTentacle>();
    }

    private void Start()
    {
        if (_drawCircle != null)
        {
            _drawCircle.Draw(_radius);
            _drawCircle.SetColor(Color.red);
            _radiusUpdateCoroutine = StartCoroutine(UpdateRadiusColor());
        }
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

        StartTentaclesGrab();
    }

    public void StartTentaclesGrab()
    {
        if (_target != null && !_isPreview)
        {
            _target.Move.CanMove = false;
            StartCoroutine(PullTarget());
        }
    }

    private IEnumerator PullTarget()
    {
        float elapsedTime = 0f;
        float baseSpeed = 0.05f;
        float speedIncrease = 0.05f;

        while (elapsedTime < _grabDuration)
        {
            float speed = baseSpeed + (elapsedTime / 0.1f) * speedIncrease;

            if (_isCollidedWithOtherCharacter)
            {
                speed /= 2;
            }

            Vector3 direction = (transform.position - _target.transform.position).normalized;
            _target.transform.position += direction * speed;

            elapsedTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        HoldTarget();
    }

    private void HoldTarget()
    {
        if (_target != null)
        {
            _target.Move.CanMove = false;
            StartCoroutine(ReleaseTargetAfterDuration());
        }
    }

    private IEnumerator ReleaseTargetAfterDuration()
    {
        yield return new WaitForSeconds(_grabDuration);
        ReleaseTarget();
    }

    private void ReleaseTarget()
    {
        Debug.Log("TentacleProjectile: ReleaseTarget");
        if (_target != null)
        {
            _target.Move.CanMove = true;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Character>(out Character character) && character != _target)
        {
            _isCollidedWithOtherCharacter = true;
        }
    }

    private IEnumerator UpdateRadiusColor()
    {
        while (true)
        {
            bool hasOtherCharacterInRadius = false;
            Collider[] colliders = Physics.OverlapSphere(transform.position, _radius);

            foreach (var collider in colliders)
            {
                if (collider.TryGetComponent<Character>(out Character character) && character != HeroToTentacle)
                {
                    hasOtherCharacterInRadius = true;
                    break;
                }
            }

            _drawCircle.SetColor(hasOtherCharacterInRadius ? Color.green : Color.red);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
