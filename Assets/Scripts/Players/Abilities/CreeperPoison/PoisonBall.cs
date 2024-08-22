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
    [SerializeField] private HealPoisonCloud _healPoisonCloud;
    [SerializeField] private WitheringPoison _witheringPoison;

    [SerializeField] private Character _player;
    [SerializeField] private Vector3 _secondMousePosition;

    [SerializeField] private PoisonBallProjectile _projectile;

    private Character _currentTarget;
    private Vector3 _firstMousePosition = Vector3.positiveInfinity;

    private int _countProjectiles = 0;

    private float _fastTimeCast = 0.4f;
    private float _slowTimeCast = 1.8f;
    private float _originalChargeCooldown;
    private float _durationPoisonCloud = 6f;

    private bool _secondClickDone = false;
    private bool _isTarget = false;
    private bool _projectileLaunched = false;

    private bool _isFast;
    private bool _isActiveTalent;
    private bool _isOriginalTargetEnemy;
    private bool _isOriginalTargetAllies;
    private bool _isOriginalTargetPlayer;
    private bool _isHealingPoisonCloud = false;

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

    protected override void ClearData()
    {
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
        }
    }

    protected override IEnumerator PrepareJob()
    {
        if (_healingPoisonBall.IsActive)
        {
            _isActiveTalent = _healingPoisonBall.IsActive;
        }
        else
        {
            _isActiveTalent = _healingPoisonBall.IsActive;
        }

        while (_currentTarget == null && float.IsPositiveInfinity(_firstMousePosition.x))
        {
            if (Input.GetMouseButtonDown(0))
            {
                _currentTarget = GetRaycastTarget(true);
                ChooseTarget();

                _firstMousePosition = GetMousePoint();
            }
            CooldownChange();
            yield return null;
        }
        yield return _secondClickCoroutine = StartCoroutine(SecondClick());
        UseAbility();
    }

    protected override IEnumerator CastJob()
    {
        ChooseWhichProjectileCreate();
        Debug.Log("PoisonBall / CastJob / Called ChooseWhichProjectileCreate");
        yield return null;
    }

    private void ChooseTarget()
    {
        if (_currentTarget != null)
        {
            if (_currentTarget.gameObject == _player.gameObject)
            {
                Debug.Log("Target == Player");
                _isOriginalTargetPlayer = true;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = false;
                if (_healPoisonCloud.IsActive)
                {
                    _isHealingPoisonCloud = true;
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Allies"))
            {
                Debug.Log("Target == Allies");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = true;
                _isOriginalTargetEnemy = false;
                if (_healPoisonCloud.IsActive)
                {
                    _isHealingPoisonCloud = true;
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                Debug.Log("Target == Enemies");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = true;
                if (_healPoisonCloud.IsActive)
                {
                    _isHealingPoisonCloud = true;
                }
            }
        }
        else
        {
            _isOriginalTargetPlayer = false;
            _isOriginalTargetAllies = false;
            _isOriginalTargetEnemy = false;
        }
    }

    private void CooldownChange()
    {
        Debug.Log("CooldownChange PoisonBall");
        if (_isActiveTalent)
        {
            if (_isOriginalTargetAllies || _isOriginalTargetPlayer)
            {
                if (_chargeCooldown == _originalChargeCooldown)
                {
                    _chargeCooldown /= 2; 
                    Debug.Log("if _chargeCooldown == " + _chargeCooldown);
                    Debug.Log("if ChargeCooldown == " + ChargeCooldown);
                }
            }
            else
            {
                _chargeCooldown = _originalChargeCooldown; 
                Debug.Log("else _chargeCooldown == " + _chargeCooldown);
            }
        }
        else
        {
            _chargeCooldown = _originalChargeCooldown;
        }
    }

    private IEnumerator SecondClick()
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

    private void UseAbility()
    {
        switch (_countProjectiles)
        {
            case < 3:
                _isTarget = IsTarget();
                ChooseMovementDependingOnCountProjectiles();
                break;

            case 3:
                _isTarget = IsTarget();
                ChooseMovementDependingOnCountProjectiles();
                break;

            default:
                break;
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

    private bool IsTarget()
    {
        if (_currentTarget != null)
            return true;

        return false;
    }

    #endregion

    #region ChooseMoveSpeedProjectile

    private void ChooseMovementDependingOnCountProjectiles()
    {
        Debug.Log("ChooseMovementDependingOnCountProjectiles");
        if (_countProjectiles < 3)
        {
            Debug.Log("ChooseMovementDependingOnCountProjectiles / if (_countProj < 3)");
            ChooseSpeed();
            StartCoroutine(_isFast ? TimeCastForFastMoveProjectile() : TimeCastForSlowMoveProjectile());
        }
        else if (_countProjectiles == 3)
        {
            Debug.Log("ChooseMovementDependingOnCountProjectiles / else if (_countProj = 3)");
            ChooseSpeed();
            StartCoroutine(TimeCastForThirdProjectile());
        }
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

    private IEnumerator TimeCastForFastMoveProjectile()
    {
        Debug.Log($"FastMoveShoot (isEnemy = {_isTarget}, isFast = {_isFast})");
        _castDelay = _slowTimeCast;
        yield return null;
    }

    private IEnumerator TimeCastForSlowMoveProjectile()
    {
        Debug.Log($"SlowMoveShoot (isEnemy = {_isTarget}, isFast = {_isFast})");
        _castDelay = _fastTimeCast;
        yield return null;
    }

    private IEnumerator TimeCastForThirdProjectile()
    {
        Debug.Log($"ThirdProjectileMovement (isEnemy = {_isTarget}, isFast = {_isFast})");
        _castDelay = 0.4f;
        yield return null;
    }

    private void ChooseWhichProjectileCreate()
    {
        Debug.Log($"ChooseWhichProjectileCreate WitheringPoison IsActive = {_witheringPoison.IsActive}");
        if (_isTarget)
        {
            CmdCreateProjectileForTarget(_currentTarget.gameObject, _currentTarget.transform.position,
                _isFast, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, 
                _isOriginalTargetAllies, _witheringPoison.IsActive);
            CmdApplyCloudPoison(_healPoisonCloud.IsActive, _isHealingPoisonCloud);

        }
        else
        {
            Debug.Log($"ChooseWhichProjectileCreate / else / WitheringPoison IsActive = {_witheringPoison.IsActive})");
            CmdCreateProjectileForFlyingMaxDistance(_firstMousePosition,
                _isFast, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy,
                _isOriginalTargetAllies, _witheringPoison.IsActive);
            CmdApplyCloudPoison(_healPoisonCloud.IsActive, _isHealingPoisonCloud);
        }
        _projectileLaunched = true;
    }

    #endregion

    #region Command Methods

    [Command]
    private void CmdCreateProjectileForTarget(GameObject target, Vector3 targetOrPoint,
        bool isFast, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies,
        bool isWitheringPoisonTalentActive)
    {
        CurrentTarget = target;
        FootInstinctsTalent = _footInstincts;
        Debug.Log("CmdCreateProj");
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

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.Value, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies, isWitheringPoisonTalentActive);
        poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdCreateProjectileForFlyingMaxDistance(Vector3 point, bool isFast, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies, 
        bool isWitheringPoisonTalentActive)
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

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.Value, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies, isWitheringPoisonTalentActive);
        poisonBallProjectile.MoveBallOnMaxDistance(point, isFast);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdApplyCloudPoison(bool isActiveTalent, bool isHealingCloud)
    {
        //Debug.Log($"SpitPoison / CmdApplyCloudPoison");
        //Debug.Log($"SpitPoison / ApplyCloudPoison / if (_healPoisonCloud.IsActive = {isActiveTalent} && _isHealingPoisonCloud = {isHealingCloud})");
        if (isActiveTalent && isHealingCloud)
        {
            _player.CharacterState.CmdAddState(States.HealingPoisonCloud, _durationPoisonCloud, 0);
        }
        else
        {
            _player.CharacterState.CmdAddState(States.PoisonCloud, _durationPoisonCloud, 0);
        }
    }

    #endregion

    #region ClientRpcMethods

    //[ClientRpc]
    //private void RpcCreateProjectileForTaret(GameObject target, Vector3 targetOrPoint, PoisonBallProjectile poisonBallProjectile,
    //    bool isFast, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    //{
    //    FootInstinctsTalent = _footInstincts;
    //    CurrentTarget = target;
    //    Debug.Log("RpcCreateProj");
    //    //Debug.Log("Rpc // IsActiveTalent == " + isActiveTalent);
    //    //Debug.Log("Rpc // isTargetPlayer == " + isTargetPlayer);
    //    //Debug.Log("Rpc // isTargetEnemy == " + isTargetEnemy);
    //    //Debug.Log("Rpc // isTargetAllies == " + isTargetAllies);
    //    if (CurrentTarget == _player.gameObject)
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

    //    poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.Value, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
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

    //    poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.Value, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
    //    poisonBallProjectile.MoveBallOnMaxDistance(point, isFast);
    //}

    #endregion

    public void PayCostPoisonBall()
    {
        TryPayCost();
    }
}
