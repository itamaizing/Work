using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GrabTentacles : Skill
{
    #region Variables
    [SerializeField] private DrawCircle _circle;

    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _psionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private GrabTentaclesPrefab _tentaclesPrefab;

    private Vector3 _firstTentaclesPoint = Vector3.positiveInfinity;
    private Vector3 _secondTentaclesPoint;
    private Vector3 _pointForSearchingTargets;

    private Character _target;
    private List<Character> _targets = new();

    private float _delayCast = 1.2f;

    private bool _isTargetChoose = false;
    private bool _isTarget = false;
    private bool _isFirstPointDone = false;
    private bool _isSecondPointDone = false;
    private bool _isFirstPointTarget = false;

    private Coroutine _chooseFirstTentaclesPointCoroutine;
    private Coroutine _chooseSecondTentaclesPointCoroutine;
    private Coroutine _searchTargetsCoroutine;
    #endregion

    #region PrepareAndCastJob
    protected override bool IsCanCast => true;

    protected override void ClearData()
    {
        _isTargetChoose = false;
        _isTarget = false;
        _isFirstPointDone = false;
        _isSecondPointDone = false;
        _isFirstPointTarget = false;

        _target = null;
        _firstTentaclesPoint = Vector3.positiveInfinity;
        _secondTentaclesPoint = Vector3.zero;
        _pointForSearchingTargets = Vector3.zero;

        if (_chooseFirstTentaclesPointCoroutine != null)
        {
            StopCoroutine(_chooseFirstTentaclesPointCoroutine);
            _chooseFirstTentaclesPointCoroutine = null;
        }
        if (_chooseSecondTentaclesPointCoroutine != null)
        {
            StopCoroutine(_chooseSecondTentaclesPointCoroutine);
            _chooseSecondTentaclesPointCoroutine = null;
        }
    }

    protected override IEnumerator PrepareJob()
    {
        _castDeley = _delayCast;

        while (_target == null && float.IsPositiveInfinity(_firstTentaclesPoint.x))
        {
            if (GetMouseButton)
            {
                yield return _chooseFirstTentaclesPointCoroutine = StartCoroutine(ChooseFirstTentaclesPointJob());

                if (_isFirstPointDone)
                {
                    yield return _chooseSecondTentaclesPointCoroutine = StartCoroutine(ChooseSecondTentaclesPointJob());
                }
            }

            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_isTarget)
        {
            InstantiateTentacles();
        }
        else
        {
            TryCancel(true);
        }
        yield return null;
    }

    private bool CheckCanCast()
    {
        if (_target == null)
            return Vector3.Distance(_firstTentaclesPoint, transform.position) <= Radius;

        return Vector3.Distance(_firstTentaclesPoint, transform.position) <= Radius ||
               Vector3.Distance(_target.transform.position, transform.position) <= Radius;
    }

    private void DrawRadius(float radius)
    {
        _circle.Draw(radius);
    }

    private IEnumerator SearchTargetsJob()
    {
        while (!_isTargetChoose)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _pointForSearchingTargets = GetMousePoint();
                _targets = GetCloserTargets(_pointForSearchingTargets, Area);
                foreach (var target in _targets)
                {
                    if (target != null)
                    {
                        _target = target;
                    }
                    yield break;
                }
                _isTargetChoose = true;
            }
            yield return null;
        }
    }

    private IEnumerator ChooseFirstTentaclesPointJob()
    {
        while (!_isFirstPointDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                yield return _searchTargetsCoroutine = StartCoroutine(SearchTargetsJob());

                if (_target == null)
                {
                    _firstTentaclesPoint = _pointForSearchingTargets;
                    _isFirstPointTarget = false;
                }
                else if (_target != null)
                {
                    _firstTentaclesPoint = _target.transform.position;
                    _isTarget = true;
                    _isFirstPointTarget = true;
                }

                _isFirstPointDone = true;
                _isTargetChoose = false;

                if (_searchTargetsCoroutine != null)
                {
                    StopCoroutine(_searchTargetsCoroutine);
                    _searchTargetsCoroutine = null;
                }
            }
            yield return null;
        }
    }

    private IEnumerator ChooseSecondTentaclesPointJob()
    {
        while (!_isSecondPointDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_target == null && !_isTarget && !_isFirstPointTarget)
                {
                    yield return _searchTargetsCoroutine = StartCoroutine(SearchTargetsJob());
                    _secondTentaclesPoint = _target.transform.position;
                    _isTarget = true;
                }
                else
                {
                    _secondTentaclesPoint = GetMousePoint();
                }

                _isSecondPointDone = true;
            }
            yield return null;
        }
    }
    #endregion

    private void InstantiateTentacles()
    {
        if (_isFirstPointTarget)
        {
            CmdInstantiateTentacles(_player.gameObject, _target.gameObject, _secondTentaclesPoint, _firstTentaclesPoint);
        }
        else
        {
            CmdInstantiateTentacles(_player.gameObject, _target.gameObject, _firstTentaclesPoint, _secondTentaclesPoint);
        }
    }

    [Command]
    private void CmdInstantiateTentacles(GameObject player, GameObject target, Vector3 pointInstantiate, Vector3 endPoint)
    {
        GameObject item = Instantiate(_tentaclesPrefab.gameObject, pointInstantiate, Quaternion.identity);
        GrabTentaclesPrefab projectile = item.GetComponent<GrabTentaclesPrefab>();

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        projectile.InitializationProjectile(player, target, pointInstantiate, endPoint);

        projectile.StartTentaclesGrab();

        NetworkServer.Spawn(item);

        RpcInstantiateTentacles(projectile.gameObject, player, target, pointInstantiate, endPoint);
    }

    [ClientRpc]
    private void RpcInstantiateTentacles(GameObject projectile, GameObject player, GameObject target, Vector3 instantiatePoint, Vector3 endPoint)
    {
        projectile.GetComponent<GrabTentaclesPrefab>().InitializationProjectile(player, target, instantiatePoint, endPoint);
        projectile.GetComponent<GrabTentaclesPrefab>().StartTentaclesGrab();
    }
}
