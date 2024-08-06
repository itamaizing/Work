using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PoisonBall : TargetOrAreaAbility
{
    [SerializeField] private FootInstincts _footInstincts;

    [SerializeField] private Character _playerLinks;
    [SerializeField] private Vector3 _secondMousePosition;

    [SerializeField] private PoisonBallProjectile _projectile;

    private int _countProjectiles = 0;

    private float _fastTimeCast = 1.8f;
    private float _slowTimeCast = 0.4f;

    private bool _secondClickDone = false;
    private bool _isEnemy = false;
    private bool _isFast;

    private Coroutine _clickCoroutine;
    private Coroutine _useCoroutine;

    public int CurrentCharges { get => _currentChargers; set => _currentChargers = value; }
    public int CountProjectiles { get => _countProjectiles; set => _countProjectiles = value; }
    public GameObject LastTarget { get; set; }
    public GameObject CurrentTarget { get; set; }
    public FootInstincts FootInstinctsTalent { get; set; }

    public bool Enabled;
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
        _secondMousePosition = Vector3.zero;

        if (_clickCoroutine != null)
            StopCoroutine(ClickCoroutine());

        if (_useCoroutine != null)
            StopCoroutine(PoisonBallUseCoroutine());
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
            else if (Input.GetMouseButtonDown(1))
            {
                Cancel();
            }
            yield return null;
        }
    }

    private IEnumerator PoisonBallUseCoroutine()
    {
        yield return _clickCoroutine = StartCoroutine(ClickCoroutine());
        PayCost();

        if (_countProjectiles < 3)
        {
            if (Target != null)
            {
                _isEnemy = true;
            }
            else
            {
                _isEnemy = false;
            }
            ChooseMovementDependingOnCountProjectiles();
        }
        else if (_countProjectiles == 3)
        {
            if (Target != null)
            {
                _isEnemy = true;
            }
            else
            {
                _isEnemy = false;
            }
            ChooseMovementDependingOnCountProjectiles();
            _countProjectiles = 0;
        }

        Cancel();
    }

    #region ChooseMoveSpeedProjectile
    private IEnumerator FastMoveShoot(bool isEnemy, bool isFast)
    {
        _castDelay = _fastTimeCast;
        yield return GetCastDeleyCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);
        CmdApplyCloudPoison();
    }

    private IEnumerator SlowMoveShoot(bool isEnemy, bool isFast)
    {
        _castDelay = _slowTimeCast;
        yield return GetCastDeleyCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);
        CmdApplyCloudPoison();
    }

    private IEnumerator ThirdProjectileMovement(bool isEnemy, bool isFast)
    {
        _castDelay = 0.4f;
        yield return GetCastDeleyCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);
        CmdApplyCloudPoison();
    }

    private void ChooseWhichProjectileCreate(bool isEnemy, bool isFast)
    {
        if (isEnemy)
        {
            CmdCreateProjectileForTaret(Target.gameObject, Target.transform.position, _isFast);
        }
        else
        {
            CmdCreateProjectileForFlyingMaxDistance(Point, _isFast);
        }
    }

    private void ChooseSpeed()
    {
        if (_isEnemy)
        {
            _isFast = Vector2.Distance(_playerLinks.transform.position, _secondMousePosition) > Vector2.Distance(_playerLinks.transform.position, Target.transform.position);
        }
        else
        {
            _isFast = Vector2.Distance(_playerLinks.transform.position, _secondMousePosition) > Vector2.Distance(_playerLinks.transform.position, Point);
        }
    }

    private void ChooseMovementDependingOnCountProjectiles()
    {
        if (_countProjectiles < 3)
        {
            ChooseSpeed();
            StartCoroutine(_isFast ? FastMoveShoot(_isEnemy, _isFast) : SlowMoveShoot(_isEnemy, _isFast));
        }
        else if (_countProjectiles == 3)
        {
            ChooseSpeed();
            StartCoroutine(ThirdProjectileMovement(_isEnemy, _isFast));
        }
    }

    #endregion

    #region Command Methods

    [Command]
    private void CmdCreateProjectileForTaret(GameObject target, Vector3 targetOrPoint, bool isFast)
    {
        CurrentTarget = target;
        FootInstinctsTalent = _footInstincts;
        Debug.Log("FootInstinctsTalent in Cmd PoisonBall == " + FootInstinctsTalent);

        if (LastTarget == CurrentTarget)
        {
            CountProjectiles++;
        }
        else if (LastTarget != CurrentTarget || CountProjectiles == 3)
        {
            CountProjectiles = 1;
        }

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        poisonBallProjectile.InitializationProjectileForPoisonBall(_playerLinks.transform, _playerLinks.Stamina.Value);
        poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);

        NetworkServer.Spawn(item);

        RpcCreateProjectileForTaret(target, targetOrPoint, isFast);
        RpcInitializationProjectile(item);
    }

    [Command]
    private void CmdCreateProjectileForFlyingMaxDistance(Vector3 point, bool isFast)
    {
        FootInstinctsTalent = _footInstincts;
        Debug.Log("FootInstinctsTalent in Cmd PoisonBall == " + FootInstinctsTalent);
        if (LastTarget == CurrentTarget)
        {
            CountProjectiles++;
        }
        else if (LastTarget != CurrentTarget || CountProjectiles == 3)
        {
            CountProjectiles = 1;
        }

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        poisonBallProjectile.InitializationProjectileForPoisonBall(_playerLinks.transform, _playerLinks.Stamina.Value);
        poisonBallProjectile.MoveBallOnMaxDistance(point, isFast);

        NetworkServer.Spawn(item);

        RpcCreateProjectileForFlyingMaxDistance(point, isFast);
        RpcInitializationProjectile(item);
    }

    [Command]
    private void CmdApplyCloudPoison()
    {
        _playerLinks.CharacterState.AddState(new PoisonCloudState(), 6f, 0, States.PoisonCloud);
        RpcApplyCloudPoison();
    }

    #endregion

    #region ClientRpcMethods

    [ClientRpc]
    private void RpcCreateProjectileForTaret(GameObject target, Vector3 targetOrPoint, bool isFast)
    {
        FootInstinctsTalent = _footInstincts;
        CurrentTarget = target;

        if (LastTarget == CurrentTarget)
        {
            CountProjectiles++;
        }
        else if (LastTarget != CurrentTarget || CountProjectiles == 3)
        {
            CountProjectiles = 1;
        }

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        poisonBallProjectile.InitializationProjectileForPoisonBall(_playerLinks.transform, _playerLinks.Stamina.Value);
        poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);
    }

    [ClientRpc]
    private void RpcCreateProjectileForFlyingMaxDistance(Vector3 point, bool isFast)
    {
        FootInstinctsTalent = _footInstincts;
        if (LastTarget == CurrentTarget)
        {
            CountProjectiles++;
        }
        else if (LastTarget != CurrentTarget || CountProjectiles == 3)
        {
            CountProjectiles = 1;
        }

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        poisonBallProjectile.InitializationProjectileForPoisonBall(_playerLinks.transform, _playerLinks.Stamina.Value);
        poisonBallProjectile.MoveBallOnMaxDistance(point, isFast);
    }

    [ClientRpc]
    private void RpcInitializationProjectile(GameObject projectile)
    {
        projectile.GetComponent<PoisonBallProjectile>().InitializationProjectileForPoisonBall(_playerLinks.transform, _playerLinks.Stamina.Value);
    }

    [ClientRpc]
    private void RpcApplyCloudPoison()
    {
        _playerLinks.CharacterState.AddState(new PoisonCloudState(), 6f, 0, States.PoisonCloud);
    }

    #endregion

    public void PayCostPoisonBall()
    {
        PayCost();
    }
}
