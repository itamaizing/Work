using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PoisonBall : TargetOrAreaAbility
{
    [Header("Talents")]
    [SerializeField] private FootInstincts _footInstincts;
    [SerializeField] private HealingPoisonBall _healingPoisonBall;

    [SerializeField] private Character _playerLinks;
    [SerializeField] private Vector3 _secondMousePosition;

    [SerializeField] private PoisonBallProjectile _projectile;

    private int _countProjectiles = 0;

    private float _fastTimeCast = 0.4f;
    private float _slowTimeCast = 1.8f;
    private float _originalChargeCooldown;

    private bool _secondClickDone = false;
    private bool _isTarget = false;
    private bool _isFast;
    private bool _isActiveTalent;
    private bool _isOriginalTargetEnemy;
    private bool _isOriginalTargetAllies;
    private bool _isOriginalTargetPlayer;

    private Coroutine _clickCoroutine;
    private Coroutine _useCoroutine;

    public int CurrentCharges;
    public int CountProjectiles;
    public bool Enabled;

    public bool IsPlayer { get; set; }
    public GameObject LastTarget { get; set; }
    public GameObject CurrentTarget { get; set; }
    public FootInstincts FootInstinctsTalent { get; set; }

    protected override void Start()
    {
        base.Start();
        _originalChargeCooldown = _chargeCooldown;
    }

    protected override IEnumerator UseCoroutine()
    {
        if (_healingPoisonBall.IsActive)
        {
            _isCanTargetHimself = _healingPoisonBall.IsCanTargetHimself;
            _isActiveTalent = _healingPoisonBall.IsActive;
        }
        else
        {
            _isCanTargetHimself = false; 
            _isActiveTalent = _healingPoisonBall.IsActive;
        }
        yield return _chooseTargetJob = StartCoroutine(ChooseTargetCoroutine(Radius));
        CastAction();
    }

    protected override void CastAction()
    {
        _useCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    protected override void Cancel()
    {
        _isTarget = false;
        _secondClickDone = false;
        _secondMousePosition = Vector3.zero;

        if (_clickCoroutine != null)
            StopCoroutine(ClickCoroutine());

        if (_useCoroutine != null)
            StopCoroutine(UseAbilityCoroutine());
    }

    private IEnumerator ClickCoroutine()
    {
        while (!_secondClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _secondClickDone = true;
                _secondMousePosition = GetMousePoint();
                if (_healingPoisonBall.IsActive)
                {
                    ChooseTarget();
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                Cancel();
            }
            yield return null;
        }
    }

    private IEnumerator UseAbilityCoroutine()
    {
        yield return _clickCoroutine = StartCoroutine(ClickCoroutine());
        if (_isActiveTalent)
        {
            //Debug.Log("If Talent is Active == " + _isActiveTalent);

            if (_isOriginalTargetAllies || _isOriginalTargetPlayer)
            {
                if (_chargeCooldown == _originalChargeCooldown)
                {
                    _chargeCooldown /= 2;
                }
                //Debug.Log("if Cooldown == " + _chargeCooldown);
                PayCost();
            }
            else
            {
                _chargeCooldown = _originalChargeCooldown;
                //Debug.Log("else Cooldown == " + _chargeCooldown);
                PayCost();
            }
        }
        else
        {
           // Debug.Log("Else Talent is Active == " + _isActiveTalent);
            PayCost();
        }

        if (_countProjectiles < 3)
        {
            if (Target != null)
            {
                //Debug.Log("Target != null");
                _isTarget = true;
            }
            else
            {
                //Debug.Log("Target == null");
                _isTarget = false;
            }
            ChooseMovementDependingOnCountProjectiles();
        }
        else if (_countProjectiles == 3)
        {
            if (Target != null)
            {
                //Debug.Log("Target != null");
                _isTarget = true;
            }
            else
            {
                //Debug.Log("Target == null");
                _isTarget = false;
            }
            ChooseMovementDependingOnCountProjectiles();
            _countProjectiles = 0;
        }

        Cancel();
    }

    private void ChooseTarget()
    {
        if (Target != null)
        {


            Debug.Log("ChooseTarget");
            if (Target.gameObject == _playerLinks.gameObject)
            {
                Debug.Log("Target == Player");
                _isOriginalTargetPlayer = true;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = false;
            }
            else if (Target.gameObject.layer == LayerMask.NameToLayer("Allies"))
            {
                Debug.Log("Target == Allies");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = true;
                _isOriginalTargetEnemy = false;
            }
            else if (Target.gameObject.layer == LayerMask.NameToLayer("Enemy"))
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

    #region ChooseMoveSpeedProjectile
    private IEnumerator FastMoveShoot(bool isEnemy, bool isFast)
    {
        _castDelay = _fastTimeCast;
        yield return GetCastDeleyCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);
    }

    private IEnumerator SlowMoveShoot(bool isEnemy, bool isFast)
    {
        _castDelay = _slowTimeCast;
        yield return GetCastDeleyCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);
    }

    private IEnumerator ThirdProjectileMovement(bool isEnemy, bool isFast)
    {
        _castDelay = 0.4f;
        yield return GetCastDeleyCoroutine();

        ChooseWhichProjectileCreate(isEnemy, isFast);
    }

    private void ChooseWhichProjectileCreate(bool isEnemy, bool isFast)
    {
        if (isEnemy)
        {
            //Debug.Log("ChooseWhichProj in PoisonBall");
            //Debug.Log("IsOriginaleTargetPlayer == " + _isOriginalTargetPlayer);
            //Debug.Log("IsOriginalTargetAllies == " + _isOriginalTargetAllies);
            //Debug.Log("IsOriginalTargetEnemy == " + _isOriginalTargetEnemy);
            CmdCreateProjectileForTarget(Target.gameObject, Target.transform.position, _isFast, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);

        }
        else
        {
            CmdCreateProjectileForFlyingMaxDistance(Point, _isFast, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);
        }
    }


    private void ChooseSpeed()
    {
        if (_isTarget)
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
            StartCoroutine(_isFast ? FastMoveShoot(_isTarget, _isFast) : SlowMoveShoot(_isTarget, _isFast));
        }
        else if (_countProjectiles == 3)
        {
            ChooseSpeed();
            StartCoroutine(ThirdProjectileMovement(_isTarget, _isFast));
        }
    }

    #endregion

    private void ApplyCloudPoison()
    {
        _playerLinks.CharacterState.CmdAddState(States.PoisonCloud, 6f, 0);
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

        poisonBallProjectile.InitializationProjectileForPoisonBall(_playerLinks, _playerLinks.Stamina.Value, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
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

        poisonBallProjectile.InitializationProjectileForPoisonBall(_playerLinks, _playerLinks.Stamina.Value, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
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
        PayCost();
    }
}
