using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonBall : TargetOrAreaAbility
{
    [SerializeField] private PoisonCloudBuff _poisonCloudBuffPrefab;
    [SerializeField] private PoisonBallProjectile _projectile;
    [SerializeField] private Character _playerLinks;
    [SerializeField] private Vector3 _secondMousePosition;

    private float _fastMovementTimeCast = 1.8f;
    private float _slowMovementTimeCast = 0.4f;

    private bool _secondClickDone = false;
    private bool _isEnemy = false;
    private bool _isFast;

    private PoisonCloudBuff _poisonCloudBuff;

    private Coroutine _clickCoroutine;
    private Coroutine _useCoroutine;

    protected override IEnumerator UseCoroutine()
    {
        yield return _chooseTargetJob = StartCoroutine(ChooseTargetCoroutine(Radius));
        CastAction();
    }

    protected override void CastAction()
    {
        _useCoroutine = StartCoroutine(PoisonBallUseCoroutine());
    }

    protected override void Cancel()
    {
        _isEnemy = false;
        _secondClickDone = false;

        if (_clickCoroutine != null)
            StopCoroutine(ClickCoroutine());

        if (_useCoroutine != null)
            StopCoroutine(PoisonBallUseCoroutine());
    }

    private IEnumerator PoisonBallUseCoroutine()
    {
        yield return _clickCoroutine = StartCoroutine(ClickCoroutine());

        PayCost();
        if (Target != null)
        {
            _isEnemy = true;
            ChooseMovement();
        }
        else
        {
            _isEnemy = false;
            ChooseMovement();
        }
        Cancel();
    }

    private IEnumerator ClickCoroutine()
    {
        while (!_secondClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _secondClickDone = true;
                _secondMousePosition = GetMousePoint();
            }
            yield return null;
        }
    }

    #region ShootSpeed
    private IEnumerator FastMoveShoot(bool isEnemy, bool isFast)
    {
        _castDeley = _fastMovementTimeCast;
        yield return GetCastDeleyCoroutine();

        CmdCreatePoisonCloudBuff();

        if (_isEnemy)
        {
            CmdCreateProjectile(Target.transform.position, _isFast);
        }
        else
        {
            CmdCreateProjectile(Point, _isFast);
        }
    }

    private IEnumerator SlowMoveShoot(bool isEnemy, bool isFast)
    {
        _castDeley = _slowMovementTimeCast;
        yield return GetCastDeleyCoroutine();

        CmdCreatePoisonCloudBuff();

        if (_isEnemy)
        {
            CmdCreateProjectile(Target.transform.position, _isFast);
        }
        else
        {
            CmdCreateProjectile(Point, _isFast);
        }
    }
    #endregion

    private void ChooseMovement()
    {
        if (_isEnemy)
        {
            _isFast = Vector2.Distance(_playerLinks.transform.position, _secondMousePosition) > Vector2.Distance(_playerLinks.transform.position, Target.transform.position);
        }
        else
        {
            _isFast = Vector2.Distance(_playerLinks.transform.position, _secondMousePosition) > Vector2.Distance(_playerLinks.transform.position, Point);
        }
        StartCoroutine(_isFast ? FastMoveShoot(_isEnemy, _isFast) : SlowMoveShoot(_isEnemy, _isFast));
    }   

    #region Command Methods

    [Command]
    private void CmdCreateProjectile(Vector3 targetOrPoint, bool isFast)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        poisonBallProjectile.InitializationProjectile(_playerLinks.transform, _playerLinks.Stamina.Value);
        poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdCreatePoisonCloudBuff()
    {
        _poisonCloudBuff = _playerLinks.GetComponentInChildren<PoisonCloudBuff>();
        if (_poisonCloudBuff == null)
        {
            _poisonCloudBuff = Instantiate(_poisonCloudBuffPrefab, _playerLinks.transform);
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks);
        }
        else
        {
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks);
        }
    }

    #endregion
}
