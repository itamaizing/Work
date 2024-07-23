using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PoisonBall : TargetOrAreaAbility
{
    [Header("Talents")]
    [SerializeField] private TalentSystem _talentSystem;
    [SerializeField] private HealingPoisonCloud _healingPoisonCloud;
    [SerializeField] private CapaciousPoisonCloud _capaciousPoisonCloud;

    [SerializeField] private Character _playerLinks;
    [SerializeField] private Vector3 _secondMousePosition;

    [SerializeField] private PoisonCloudBuff _poisonCloudBuffPrefab;
    [SerializeField] private PoisonBallProjectile _projectile;

    private PoisonCloudBuff _poisonCloudBuff;

    private int _countProjectiles = 0;

    private float _fastTimeCast = 1.8f;
    private float _slowTimeCast = 0.4f;

    private bool _secondClickDone = false;
    private bool _isEnemy = false;
    private bool _isFast;

    private Coroutine _clickCoroutine;
    private Coroutine _useCoroutine;

    public int CurrentCharges { get => _currentChargers; set => _currentChargers = value; }
    public int CountProjectiles { get; set; }
    public bool HealingCloudTalentIsActive => _healingPoisonCloud.isActive;
    public bool CapaciousCloudTalentIsActive => _capaciousPoisonCloud.isActive;
    public GameObject LastTarget { get; set; }
    public GameObject CurrentTarget { get; set; }
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

        _countProjectiles++;

        //Debug.Log("PoisonBall Count == " + _countProjectiles);
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

        CmdCreatePoisonCloudBuff(HealingCloudTalentIsActive, CapaciousCloudTalentIsActive);
    }

    private IEnumerator SlowMoveShoot(bool isEnemy, bool isFast)
    {
        _castDelay = _slowTimeCast;
        yield return GetCastDeleyCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);

        CmdCreatePoisonCloudBuff(HealingCloudTalentIsActive, CapaciousCloudTalentIsActive);
    }

    private IEnumerator ThirdProjectileMovement(bool isEnemy, bool isFast)
    {
        _castDelay = 0.4f;
        yield return GetCastDeleyCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);

        CmdCreatePoisonCloudBuff(HealingCloudTalentIsActive, CapaciousCloudTalentIsActive);
    }

    private void ChooseWhichProjectileCreate(bool isEnemy, bool isFast)
    {
        // В зависимости от того выбран таргет или поинт, и какая скорость скорость, запускаем снаряд
        if (isEnemy)
        {
            CmdCreateProjectile(Target.gameObject, Target.transform.position, _isFast);
        }
        else
        {
            CmdCreateProjectile(Point, _isFast);
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
    private void CmdCreateProjectile(GameObject target, Vector3 targetOrPoint, bool isFast)
    {
        CurrentTarget = target;

        if (CountProjectiles < 3)
        {
            CountProjectiles++;
        }
        else if (CountProjectiles == 3)
        {
            CountProjectiles = 1;
        }

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        poisonBallProjectile.InitializationProjectileForPoisonBall(_playerLinks.transform, _playerLinks.Stamina.Value);
        poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdCreateProjectile(Vector3 targetOrPoint, bool isFast)
    {
        if (CountProjectiles < 3)
        {
            CountProjectiles++;
        }
        else if (CountProjectiles == 3)
        {
            CountProjectiles = 1;
        }

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        poisonBallProjectile.InitializationProjectileForPoisonBall(_playerLinks.transform, _playerLinks.Stamina.Value);
        poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdCreatePoisonCloudBuff(bool isActiveHealingCloud, bool isActiveCapaciousCloud)
    {
        RpcCreatePoisonCloudBuff(isActiveHealingCloud, isActiveCapaciousCloud);

        _poisonCloudBuff = _playerLinks.GetComponentInChildren<PoisonCloudBuff>();
        if (_poisonCloudBuff == null)
        {
            _poisonCloudBuff = Instantiate(_poisonCloudBuffPrefab, _playerLinks.transform);
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud);
        }
        else
        {
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud);
        }
    }

    #endregion

    [ClientRpc]
    private void RpcCreatePoisonCloudBuff(bool isActiveHealingCloud, bool isActiveCapaciousCloud)
    {
        _poisonCloudBuff = _playerLinks.GetComponentInChildren<PoisonCloudBuff>();
        if (_poisonCloudBuff == null)
        {
            _poisonCloudBuff = Instantiate(_poisonCloudBuffPrefab, _playerLinks.transform);
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud);
        }
        else
        {
            _poisonCloudBuff.PoisonCloudAddStacks(_playerLinks, isActiveHealingCloud, isActiveCapaciousCloud);
        }
    }

    public void PayCostPoisonBall()
    {
        PayCost();
    }
}
