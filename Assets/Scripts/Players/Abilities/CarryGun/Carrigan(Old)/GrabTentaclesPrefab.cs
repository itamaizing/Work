using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabTentaclesPrefab : NetworkBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Character _player;
    [SerializeField] private float _lifetime;
    private Character _target;
    private Vector3 _pointInstantiate;
    private Vector3 _endPosition;

    private float _startSpeed = 2f;
    private float _baseSpeed = 0.05f;
    private float _increasedSpeed = 0.05f;
    private float _reductionSpeed = 2f;

    private float _damage;

    private bool _isAttackingPsiEnergyActive = false;
    private bool _isTargetDamageTaked = false;

    private List<Character> _enemiesOnPath = new List<Character>();

    private Coroutine _tentaclesToTarget;
    private Coroutine _tentaclesToPlayer;

    public void InitializationProjectile(GameObject player, GameObject target, Vector3 pointInstantiate, Vector3 endPosition, 
        bool isAttackingPsiEnergy, float currentDamage)
    {

        Character playerCharacter = player.GetComponent<Character>();
        Character targetCharacter = target.GetComponent<Character>();

        _player = playerCharacter;
        _target = targetCharacter;
        _pointInstantiate = pointInstantiate;
        _endPosition = endPosition;
        _isAttackingPsiEnergyActive = isAttackingPsiEnergy;
        _damage = currentDamage;

        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, _pointInstantiate);
        _lineRenderer.SetPosition(1, _endPosition);
    }

    public void StartTentaclesGrab()
    {

        _tentaclesToTarget = StartCoroutine(TentaclesToTargetJob());
    }

    private void Update()
    {
        _lineRenderer.SetPosition(0, _pointInstantiate);
        _lineRenderer.SetPosition(1, _endPosition);
    }

    private void PullTarget(Character target, Vector3 direction, float duration)
    {
        target.Move.TargetRpcDoMove((Vector3)target.transform.position - direction * duration, duration);
    }

    private void DestroyProjectile()
    {

        Destroy(gameObject);

        if (_tentaclesToTarget != null ) 
        {
            StopCoroutine(_tentaclesToTarget);
            _tentaclesToTarget = null;
        }
        if (_tentaclesToPlayer != null )
        {
            StopCoroutine(_tentaclesToPlayer);
            _tentaclesToPlayer = null;
        }
    }

    private IEnumerator TentaclesToTargetJob()
    {
        float startTime = Time.time;
        Vector3 currentPosition = _pointInstantiate;
        while (currentPosition != _endPosition)
        {
            float time = (Time.time - startTime) / _startSpeed;
            currentPosition = Vector3.Lerp(_pointInstantiate, _endPosition, time);
            _lineRenderer.SetPosition(1, currentPosition);
            yield return null;
        }
        _tentaclesToPlayer = StartCoroutine(TentaclesToPlayerJob());
        
    }

    private IEnumerator TentaclesToPlayerJob()
    {
        float baseTime = 0.1f; 
        float startTime = Time.time;

        Vector3 currentPosition = _endPosition;

        List<Character> targetsToPull = new List<Character>(_enemiesOnPath) { _target };

        while (currentPosition != _pointInstantiate)
        {
            if (baseTime < 0)
            {
                _baseSpeed += _increasedSpeed;
                baseTime = 0.1f;
            }

            float time = (Time.time - startTime) / _baseSpeed;

            baseTime -= Time.time;
            _lifetime -= Time.deltaTime;

            currentPosition = Vector3.Lerp(_endPosition, _pointInstantiate, time);

            _lineRenderer.SetPosition(1, currentPosition);

            if (isServer)
            {
                foreach (Character enemy in targetsToPull)
                {
                    Vector3 direction = (enemy.transform.position - _pointInstantiate).normalized;
                    PullTarget(enemy, direction, time);

                    if (_isAttackingPsiEnergyActive && !_isTargetDamageTaked)
                    {
                        _isTargetDamageTaked = true;

                        Damage damage = new Damage
                        {
                            Value = _damage,
                            Type = DamageType.Magical,
                        };

                        enemy.Health.TryTakeDamage(ref damage, null);
                    }
                }
                CheckForObstacles(currentPosition);
            }

            if (_lifetime <= 0)
            {
                DestroyProjectile();
            }

            yield return null;
        }
    }

    private void CheckForObstacles(Vector3 currentPosition)
    {
        RaycastHit2D hit;
        Vector2 direction = (_pointInstantiate - currentPosition).normalized;
        float distance = Vector2.Distance(currentPosition, _pointInstantiate);

        hit = Physics2D.CircleCast(currentPosition, 1.5f, direction, distance);

        if (hit.collider != null)
        {
            Character enemy = hit.collider.GetComponent<Character>();
            if (enemy != null && !_enemiesOnPath.Contains(enemy))
            {
                _enemiesOnPath.Add(enemy);

                _baseSpeed /= _reductionSpeed;
            }
        }
    }
}
