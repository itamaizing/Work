using System.Collections;
using UnityEngine;

public class TentacleProjectile : MonoBehaviour
{
    private DrawCircle _drawCircle;
    private Character _player;
    private Character _target;
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private bool _isAttackingPsiEnergyActive;
    private float _currentDamage;
    private float _grabDuration = 1.2f; 

    private bool _isCollidedWithOtherCharacter = false;

    private void Awake()
    {
        _drawCircle = GetComponent<DrawCircle>();
    }

    private void Start()
    {
        if (_drawCircle != null)
        {
            _drawCircle.Draw(3f);
        }
    }

    private void OnDestroy()
    {
        if (_drawCircle != null)
        {
            _drawCircle.Clear();
        }
    }

    public void Init(GameObject player, GameObject target, Vector3 startPosition, Vector3 endPosition,
        bool isAttackingPsiEnergyActive, float currentDamage)
    {
        _player = player.GetComponent<Character>();
        _target = target.GetComponent<Character>();
        _startPosition = startPosition;
        _endPosition = endPosition;
        _isAttackingPsiEnergyActive = isAttackingPsiEnergyActive;
        _currentDamage = currentDamage;

        transform.position = startPosition;

        StartTentaclesGrab();
    }

    public void StartTentaclesGrab()
    {
        Debug.Log("TentacleProjectile: StartTentaclesGrab");

        if (_target != null)
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
        Debug.Log("TentacleProjectile: HoldTarget");
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
}
