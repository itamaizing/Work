using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabTentaclesObject : NetworkBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _psionicEnergy;
    [SerializeField] private float _lifetime;
    private Character _target;
    private Vector3 _pointInstantiate;
    private Vector3 _endPosition;

    private float _startSpeed = 2f;

    private float _baseSpeed = 0.05f;
    private float _increasedSpeed = 0.05f;
    private float _baseDurationGrabbing = 0.1f;
    private float _reductionSpeed = 2f;

    private bool _isGrabTarget = false;

    private Coroutine _tentaclesToTarget;
    private Coroutine _tentaclesToPlayer;

    public void InitializationProjectile(GameObject player, GameObject target, Vector3 pointInstantiate, Vector3 endPosition)
    {
        Debug.Log("GrabTentaclesProjectile / InitializationProjectile");

        Character playerCharacter = player.GetComponent<Character>();
        Character targetCharacter = target.GetComponent<Character>();

        _player = playerCharacter;
        _target = targetCharacter;
        _pointInstantiate = pointInstantiate;
        _endPosition = endPosition;

        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, _pointInstantiate);
        _lineRenderer.SetPosition(1, _endPosition);
    }

    public void StartTentaclesGrab()
    {
        Debug.Log("GrabTentaclesProjectile / StartTentaclesGrab");

        _tentaclesToTarget = StartCoroutine(TentaclesToTargetJob());
    }

    private void Update()
    {
        _lineRenderer.SetPosition(0, _pointInstantiate);
        _lineRenderer.SetPosition(1, _endPosition);
    }

    private void PullTarget(Vector3 direction, float duration)
    {
        Debug.Log("GrabTentaclesProjectile / PullTarget");
        _target.Move.TargetRpcDoMove((Vector3)_target.transform.position - direction * duration, duration);
    }

    private void DestroyProjectile()
    {
        Debug.Log("GrabTentaclesProjectile / DestroyProjectile");

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
        Debug.Log("GrabTentaclesProjectile / TentaclesToTargetJob");
        while (currentPosition != _endPosition)
        {
            float time = (Time.time - startTime) / _startSpeed;
            Debug.Log("GrabTentaclesProjectile / TentaclesToTargetJob / while / time == " + time);
            currentPosition = Vector3.Lerp(_pointInstantiate, _endPosition, time);
            _lineRenderer.SetPosition(1, currentPosition);
            yield return null;
        }
        Debug.Log("GrabTentaclesProjectile / TentaclesToTargetJob / after while");
        _tentaclesToPlayer = StartCoroutine(TentaclesToPlayerJob());
    }

    private IEnumerator TentaclesToPlayerJob()
    {
        float startTime = Time.time;
        Vector3 currentPosition = _endPosition;
        Debug.Log("GrabTentaclesProjectile / TentaclesToPlayerJob");
        while (currentPosition != _pointInstantiate)
        {
            float time = (Time.time - startTime) / _baseSpeed;
            _lifetime -= Time.deltaTime;

            currentPosition = Vector3.Lerp(_endPosition, _pointInstantiate, time);

            Vector3 direction = (_target.transform.position - _pointInstantiate).normalized;
            PullTarget(direction, time);

            _lineRenderer.SetPosition(1, currentPosition);

            _baseSpeed += _increasedSpeed;

            if (_lifetime <= 0)
            {
                DestroyProjectile();
            }

            yield return null;
        }
    }
}
