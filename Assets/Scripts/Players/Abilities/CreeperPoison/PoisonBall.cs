using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PoisonBall : Skill
{
    [Header("Talents")]
    [SerializeField] private FootInstincts _footInstincts;
    [SerializeField] private HealingPoisonBall _healingPoisonBall;

    [SerializeField] private Character _player;
    [SerializeField] private Vector3 _secondMousePosition;

    [SerializeField] private PoisonBallProjectile _projectile;

    private Character _currentTarget;
    private Vector3 _firstMousePosition = Vector3.positiveInfinity;

    private int _countProjectiles = 0;

    private float _fastTimeCast = 0.4f;
    private float _slowTimeCast = 1.8f;
    private float _originalChargeCooldown;

    private bool _secondClickDone = false;
    private bool _isTarget = false;
    private bool _projectileLaunched = false;

    private bool _isFast;
    private bool _isActiveTalent;
    private bool _isOriginalTargetEnemy;
    private bool _isOriginalTargetAllies;
    private bool _isOriginalTargetPlayer;

    private Coroutine _secondClickCoroutine;
    private Coroutine _useAbilityCoroutine;

    public int CurrentCharges;
    public int CountProjectiles;
    public bool Enabled;

    public bool IsPlayer { get; set; }
    public GameObject LastTarget { get; set; }
    public GameObject CurrentTarget { get; set; }
    public FootInstincts FootInstinctsTalent { get; set; }

    protected void Start()
    {
        _originalChargeCooldown = _chargeCooldown;
    }

    protected override bool IsCanCast => CheckCanCast();

    protected override IEnumerator PrepareJob()
    {
        //if (_healingPoisonBall.IsActive)
        //{
        //    _isCanTargetHimself = _healingPoisonBall.IsCanTargetHimself;
        //    _isActiveTalent = _healingPoisonBall.IsActive;
        //}
        //else
        //{
        //    _isCanTargetHimself = false;
        //    _isActiveTalent = _healingPoisonBall.IsActive;
        //}
        Debug.Log("PrepareJob Coroutine work PoisonBall");
        while (_currentTarget == null && float.IsPositiveInfinity(_firstMousePosition.x))
        {
            Debug.Log("PrepareJob Coroutine after while");
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"PrepareJob Coroutine after if Input {Input.GetMouseButtonDown(0)}");
                _currentTarget = GetRaycastTarget();
                ChooseTarget();
                Debug.Log($"PrepareJob Coroutine / currentTarget == {_currentTarget}");
                _firstMousePosition = GetMousePoint();
                Debug.Log($"PrepareJob Coroutine / firstMousePos == {_firstMousePosition}");
            }
            yield return null;
        }
        Debug.Log("PrepareJob Coroutine after while");
        yield return _secondClickCoroutine = StartCoroutine(SecondClick());
    }

    protected override IEnumerator CastJob()
    {
        while (!_projectileLaunched)
        {
            Debug.Log("CastJobCoroutine work PoisonBall / after while");
            if (_secondClickDone)
            {
                Debug.Log($"CastJobCoroutine / secondClickDone == {_secondClickDone}");
                TryPayCost();
                if (_isActiveTalent)
                {
                    IsAlliesOrPlayer();
                }
                if (_useAbilityCoroutine == null)
                {
                    _useAbilityCoroutine = StartCoroutine(UseAbilityCoroutine());
                }
            }
            yield return null;
        }
    }

    protected override void ClearData()
    {
        Debug.Log("ClearData PoisonBall");

        _isTarget = false;
        _secondClickDone = false;
        _projectileLaunched = false;

        _currentTarget = null;

        _firstMousePosition = Vector3.positiveInfinity;
        _secondMousePosition = Vector3.zero;

        if (_secondClickCoroutine != null)
        {
            StopCoroutine(SecondClick());
            _secondClickCoroutine = null;
            Debug.Log($"ClearData / If / _secondClickCoroutine after reset == {_secondClickCoroutine}");
        }
        if (_useAbilityCoroutine != null)
        {
            StopCoroutine(UseAbilityCoroutine());
            _useAbilityCoroutine = null;
            Debug.Log($"ClearData / If / _useAbilityCoroutine after reset == {_useAbilityCoroutine}");
        }
    }

    private IEnumerator SecondClick()
    {
        Debug.Log("SecondClickCoroutine work PoisonBall");
        while (!_secondClickDone)
        {
            Debug.Log("SecondClickCoroutine work PoisonBall after while");
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"SecondClick Coroutine after if Input {Input.GetMouseButtonDown(0)}");
                _secondClickDone = true;
                _secondMousePosition = GetMousePoint();
            }
            yield return null;
        }
    }

    private IEnumerator UseAbilityCoroutine()
    {
        Debug.Log("UseAbilityCoroutine work PoisonBall");
        switch (_countProjectiles)
        {
            case < 3:
                _isTarget = IsTarget();
                Debug.Log($"UseAbilityCoroutine / switch / case < 3 / _isTarget = {_isTarget}");
                ChooseMovementDependingOnCountProjectiles();
                break;

            case 3:
                _isTarget = IsTarget();
                Debug.Log($"UseAbilityCoroutine / switch / case 3 / _isTarget = {_isTarget}");
                ChooseMovementDependingOnCountProjectiles();
                break;

            default:
                break;
        }
        yield return null;
    }
    private void ChooseTarget()
    {
        if (_currentTarget != null)
        {
            Debug.Log("ChooseTarget");
            if (_currentTarget.gameObject == _player.gameObject)
            {
                Debug.Log("Target == Player");
                _isOriginalTargetPlayer = true;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = false;
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Allies"))
            {
                Debug.Log("Target == Allies");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = true;
                _isOriginalTargetEnemy = false;
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                Debug.Log("Target == Enemies");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = true;
            }
        }
        else
        {
            _isOriginalTargetPlayer = false;
            _isOriginalTargetAllies = false;
            _isOriginalTargetEnemy = false;
        }
    }

    #region BooleanMethods

    private bool CheckCanCast()
    {
        Debug.Log("CheckCanCast PoisonBall");

        if (_currentTarget == null)
            return Vector3.Distance(_firstMousePosition, transform.position) <= Radius;

        return Vector3.Distance(_firstMousePosition, transform.position) <= Radius ||
               Vector3.Distance(_currentTarget.transform.position, transform.position) <= Radius;
    }

    private bool IsAlliesOrPlayer()
    {
        if (_isOriginalTargetAllies || _isOriginalTargetPlayer)
        {
            if (_chargeCooldown == _originalChargeCooldown)
            {
                _chargeCooldown /= 2;
            }
            return true;
        }
        else
        {
            _chargeCooldown = _originalChargeCooldown;
        }
        return false;
    }

    private bool IsTarget()
    {
        if (_currentTarget != null)
            return true;

        return false;
    }

    #endregion

    #region ChooseMoveSpeedProjectile

    private IEnumerator FastMoveShoot(bool isEnemy, bool isFast)
    {
        Debug.Log($"FastMoveShoot (isEnemy = {isEnemy}, isFast = {isFast})");
        _castDelay = _fastTimeCast;
        yield return StartCastDelayCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);
    }

    private IEnumerator SlowMoveShoot(bool isEnemy, bool isFast)
    {
        Debug.Log($"SlowMoveShoot (isEnemy = {isEnemy}, isFast = {isFast})");
        _castDelay = _slowTimeCast;
        yield return StartCastDelayCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);
    }

    private IEnumerator ThirdProjectileMovement(bool isEnemy, bool isFast)
    {
        Debug.Log($"ThirdProjectileMovement (isEnemy = {isEnemy}, isFast = {isFast})");
        _castDelay = 0.4f;
        yield return StartCastDelayCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);
    }

    private void ChooseWhichProjectileCreate(bool isEnemy, bool isFast)
    {
        Debug.Log($"ChooseWhichProjectileCreate (isEnemy =  {isEnemy} , isFast =  {isFast} )");
        if (isEnemy)
        {
            Debug.Log($"ChooseWhichProjectileCreate / if / (isEnemy =  {isEnemy})");
            Debug.Log($"ChooseWhichProjectileCreate / if / _currentTarget = {_currentTarget.gameObject} And _currentTarget.Pos = {_currentTarget.transform.position}");
            CmdCreateProjectileForTarget(_currentTarget.gameObject, _currentTarget.transform.position, 
                isFast, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);

        }
        else
        {
            Debug.Log($"ChooseWhichProjectileCreate / else / (isEnemy =  {isEnemy})");
            CmdCreateProjectileForFlyingMaxDistance(_firstMousePosition, 
                isFast, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);
        }
        _projectileLaunched = true;
    }


    private void ChooseSpeed()
    {
        Debug.Log("ChooseSpeed");
        if (_isTarget)
        {
            Debug.Log($"ChooseSpeed / if / _isTarget = {_isTarget}");
            _isFast = Vector2.Distance(_player.transform.position, _secondMousePosition) > Vector2.Distance(_player.transform.position, _currentTarget.transform.position);
        }
        else
        {
            Debug.Log($"ChooseSpeed / else / _isTarget = {_isTarget}");
            _isFast = Vector2.Distance(_player.transform.position, _secondMousePosition) > Vector2.Distance(_player.transform.position, _firstMousePosition);
        }
    }

    private void ChooseMovementDependingOnCountProjectiles()
    {
        Debug.Log("ChooseMovementDependingOnCountProjectiles");
        if (_countProjectiles < 3)
        {
            Debug.Log("ChooseMovementDependingOnCountProjectiles / if (_countProj < 3)");
            ChooseSpeed();
            StartCoroutine(_isFast ? FastMoveShoot(_isTarget, _isFast) : SlowMoveShoot(_isTarget, _isFast));
        }
        else if (_countProjectiles == 3)
        {
            Debug.Log("ChooseMovementDependingOnCountProjectiles / else if (_countProj = 3)");
            ChooseSpeed();
            StartCoroutine(ThirdProjectileMovement(_isTarget, _isFast));
        }
    }

    #endregion

    private void ApplyCloudPoison()
    {
        _player.CharacterState.CmdAddState(States.PoisonCloud, 6f, 0);
    }

    #region Command Methods

    [Command]
    private void CmdCreateProjectileForTarget(GameObject target, Vector3 targetOrPoint,
        bool isFast, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        CurrentTarget = target;
        FootInstinctsTalent = _footInstincts;
        //Debug.Log("CmdCreateProj");
        //Debug.Log("Cmd // IsActiveTalent == " + isActiveTalent);
        //Debug.Log("Cmd // isTargetPlayer == " + isTargetPlayer);
        //Debug.Log("Cmd // isTargetEnemy == " + isTargetEnemy);
        //Debug.Log("Cmd // isTargetAllies == " + isTargetAllies);


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

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.Value, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
        poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);

        NetworkServer.Spawn(item);
        ApplyCloudPoison();
    }

    [Command]
    private void CmdCreateProjectileForFlyingMaxDistance(Vector3 point, bool isFast, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
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

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.Value, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
        poisonBallProjectile.MoveBallOnMaxDistance(point, isFast);

        NetworkServer.Spawn(item);

        ApplyCloudPoison();
    }

    #endregion

    #region ClientRpcMethods

    //[ClientRpc]
    //private void RpcCreateProjectileForTaret(GameObject target, Vector3 targetOrPoint, PoisonBallProjectile poisonBallProjectile,
    //    bool isFast, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    //{
    //    FootInstinctsTalent = _footInstincts;
    //    CurrentTarget = target;
    //    //Debug.Log("RpcCreateProj");
    //    //Debug.Log("Rpc // IsActiveTalent == " + isActiveTalent);
    //    //Debug.Log("Rpc // isTargetPlayer == " + isTargetPlayer);
    //    //Debug.Log("Rpc // isTargetEnemy == " + isTargetEnemy);
    //    //Debug.Log("Rpc // isTargetAllies == " + isTargetAllies);
    //    if (CurrentTarget == _playerLinks.gameObject)
    //    {
    //        IsPlayer = true;
    //    }
    //    else
    //    {
    //        IsPlayer = false;
    //    }

    //    if (LastTarget == CurrentTarget)
    //    {
    //        CountProjectiles++;
    //    }
    //    else if (LastTarget != CurrentTarget || CountProjectiles == 3)
    //    {
    //        CountProjectiles = 1;
    //    }

    //    //GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
    //    //PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

    //    poisonBallProjectile.InitializationProjectileForPoisonBall(_playerLinks, _playerLinks.Stamina.Value, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
    //    poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);
    //}

    //[ClientRpc]
    //private void RpcCreateProjectileForFlyingMaxDistance(Vector3 point, bool isFast, PoisonBallProjectile poisonBallProjectile, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    //{
    //    FootInstinctsTalent = _footInstincts;
    //    if (LastTarget == CurrentTarget)
    //    {
    //        CountProjectiles++;
    //    }
    //    else if (LastTarget != CurrentTarget || CountProjectiles == 3)
    //    {
    //        CountProjectiles = 1;
    //    }

    //    //GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
    //    //PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

    //    poisonBallProjectile.InitializationProjectileForPoisonBall(_playerLinks, _playerLinks.Stamina.Value, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
    //    poisonBallProjectile.MoveBallOnMaxDistance(point, isFast);
    //}

    #endregion

    public void PayCostPoisonBall()
    {
        TryPayCost();
    }
}
