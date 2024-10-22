using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using System;

public struct PoisonBallInfo : NetworkMessage
{
    public int CountProjectiles;
    public int MaxCountProjectile;

    public float TimeBetweenAttack;
    public float StartTimeBetweenAttack;

    public bool IsProjectileCreate;
    public bool IsActiveTimer;
    public bool IsThreeProjectileOnOnetarget;
    public bool IsCanApplyInvisible;

    public bool IsOriginalTargetEnemy;
    public bool IsOriginalTargetAllies;
    public bool IsOriginalTargetPlayer;

    public bool IsActiveTransparentPoisons;
    public bool IsActiveWitheringPoison;
    public bool IsActiveContinuationAmbush; 
    public bool IsActiveHealingPoisonBall;
    public bool IsActiveHealingPoisonCloud;
    public bool IsActiveEnlargedGlands;
    public bool IsActiveVoluminousBall;
    public bool IsActiveInertialGlands;
    public bool IsActiveVolatilityOfPoisons;
    public bool IsHealingPoisonCloud;
}

public class PoisonBall : Skill
{
    #region Variables

    [Header("Talents")]
    [SerializeField] private RestorationOfGlands _restorationOfGlands;
    [SerializeField] private TransparentPoisons _transparentPoisons;
    [SerializeField] private FootInstincts _footInstincts;
    [SerializeField] private HealingPoisonBall _healingPoisonBall;
    [SerializeField] private HealPoisonCloud _healPoisonCloud;
    [SerializeField] private WitheringPoison _witheringPoison;
    [SerializeField] private EnlargedGlands _enlargedGlands;
    [SerializeField] private ContinuationAmbush _continuationAmbush;
    [SerializeField] private VoluminousBall _voluminousBall;
    [SerializeField] private InertialGlands _inertialGlands;
    [SerializeField] private AssasinPoison _assasinPoison;
    [SerializeField] private FlowOfPoisons _flowOfPoison;
    [SerializeField] private VolatilityOfPoisons _volatilityOfPoisons;
    [SerializeField] private float _projectileSize;

    [Header("Ability properties")]
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private PoisonBallProjectile _projectile;
    [SerializeField] private Character _player;
    [SerializeField] private GameObject _arrowPrefab;

    #region PoisonCloud
    [SerializeField] private PoisonDamagingCloudPrefab _poisonDamagingCloudPrefab;
    [SerializeField] private PoisonHealingCloudPrefab _poisonHealingCloudPrefab;
    private PoisonDamagingCloudPrefab _poisonDamagingCloud;
    private PoisonHealingCloudPrefab _poisonHealingCloud;
    private float _durationPoisonCloud = 6f;
    #endregion

    private PoisonBallInfo _poisonBallInfo = new PoisonBallInfo();

    private GameObject[] _arrowRenderers = new GameObject[4];
    private Character _currentTarget;

    private Vector3 _firstMousePosition = Vector3.positiveInfinity;
    private Vector3 _secondMousePosition;
    private Vector3 _thirdMousePosition;

    private int _poisonBoneStacks = 0;

    private float _fastTimeCast = 0.4f;
    private float _slowTimeCast = 1.8f;
    private float _originalChargeCooldown;
    private float _currentStacksAsssasinPoison = 0;
    private float _baseCastWidth;
    private float _multiplierForPushDistance;

    #region BoolVariables

    private bool _isPushTarget;
    private bool _isTarget;
    private bool _isFast;
    private bool _secondClickDone = false;
    private bool _thirdClickDone = false;
    private bool _firstClickCompleted = false;
    private bool _colorLockedAfterSecondClick = false;
    private bool _colorLockedAfterThirdClick = false;
    private bool _isBallCanBigger = false;
    private bool _isPlayerInvisible = false;

    #endregion

    private Coroutine _secondClickCoroutine;
    private Coroutine _thirdClickCoroutine;
    private Coroutine _mouseDetectionCoroutine;

    public GameObject LastTarget { get; set; }
    public GameObject CurrentTarget { get; set; }
    public FootInstincts FootInstinctsTalent { get; set; }
    public RestorationOfGlands RestorationOfGlandsTalent { get; set; }
    public ContinuationAmbush ContinuationAmbushTalent { get; set; }
    public int CurrentCountBall { get => _poisonBallInfo.CountProjectiles; }
    public int PoisonBoneStack { get => _poisonBoneStacks; set => _poisonBoneStacks = value; }

    #endregion

    private void Start()
    {
        _baseCastWidth = _castWidth;
        _originalChargeCooldown = _chargeCooldown;

        _poisonBallInfo.StartTimeBetweenAttack = 15.0f;
        _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        _poisonBallInfo.MaxCountProjectile = _maxCharges;
    }
    
    private void Update()
    {
        UpdateMouseDetection();
        if (_poisonBallInfo.IsActiveTimer)
        {
            Timer();
        }
    }

    #region PrepareAndStartJob

    protected override bool IsCanCast => CheckCanCast();

    public void PayCostPoisonBall()
    {
        TryPayCost();
    }

    protected override void ClearData()
    {
        _isTarget = false;
        _secondClickDone = false;
        _thirdClickDone = false;

        _currentTarget = null;

        _firstMousePosition = Vector3.positiveInfinity;
        _secondMousePosition = Vector3.zero;
        _thirdMousePosition = Vector3.zero;

        ClearArrows();

        _firstClickCompleted = false;
        _colorLockedAfterSecondClick = false;
        _colorLockedAfterThirdClick = false;

        if (_secondClickCoroutine != null)
        {
            StopCoroutine(_secondClickCoroutine);
            _secondClickCoroutine = null;
        }
        if (_thirdClickCoroutine != null)
        {
            StopCoroutine(_thirdClickCoroutine);
            _thirdClickCoroutine = null;
        }
    }

    protected override IEnumerator PrepareJob()
    {
        CheckingActiveTalents();

        while (_currentTarget == null && float.IsPositiveInfinity(_firstMousePosition.x))
        {
            if (GetMouseButton)
            {
                _currentTarget = GetRaycastTarget(true);
                Debug.Log("PoisonBall / currentTarget = " + _currentTarget);   
                if (_currentTarget != null)
                {
                    _isTarget = true;
                }
                else
                {
                    _isTarget = false;
                }

                CheckWhoTarget();
                _firstMousePosition = GetMousePoint();

                CreateArrowsParallelToPlayer();
                StopAutoDraw();

                _firstClickCompleted = true;

            }
            CooldownChange();
            yield return null;
        }

        yield return _secondClickCoroutine = StartCoroutine(SecondClick());
        yield return _thirdClickCoroutine = StartCoroutine(ThirdClick());

        UseAbility();
    }

    protected override IEnumerator CastJob()
    {
        ChooseWhichProjectileCreate();

        ClearArrows();

        yield return null;
    }

    private void UseAbility()
    {
        if (_secondClickDone && _thirdClickDone)
        {
            ChooseMovementDependingOnCountProjectiles();
        }
    }

    #endregion

    #region ArrowManagement

    private void CreateArrowsParallelToPlayer()
    {
        if (_arrowPrefab == null)
        {
            Debug.LogError("Arrow Prefab is not assigned or Target is null");
            return;
        }

        Vector3 targetPosition;

        if (_isTarget)
        {
            targetPosition = _currentTarget.transform.position;
        }
        else
        {
            targetPosition = _firstMousePosition;
        }

        Vector3 playerPosition = _player.transform.position;

        Vector3 directionToTarget = (targetPosition - playerPosition).normalized;

        Vector3 perpendicularDirection = Vector3.Cross(directionToTarget, Vector3.forward).normalized;

        _arrowRenderers[0] = Instantiate(_arrowPrefab, targetPosition + directionToTarget, Quaternion.identity);
        _arrowRenderers[1] = Instantiate(_arrowPrefab, targetPosition - directionToTarget, Quaternion.identity);
        _arrowRenderers[2] = Instantiate(_arrowPrefab, targetPosition + directionToTarget * 1.5f, Quaternion.identity);
        _arrowRenderers[3] = Instantiate(_arrowPrefab, targetPosition - directionToTarget * 1.5f, Quaternion.identity);

        SetArrowDirections(perpendicularDirection);
        SetArrowColors(Color.red);
    }

    private void SetArrowDirections(Vector3 perpendicularDirection)
    {
        for (int i = 0; i < _arrowRenderers.Length; i++)
        {
            var arrow = _arrowRenderers[i];
            if (arrow != null)
            {
                var drawArrow = arrow.GetComponent<DrawArrow>();
                if (drawArrow != null)
                {
                    Vector3 startPoint = arrow.transform.position - perpendicularDirection * 0.5f;
                    Vector3 endPoint = arrow.transform.position + perpendicularDirection * 0.5f;

                    if (i % 2 == 0)
                    {
                        drawArrow.DrawCurvedArrow(startPoint, endPoint, true);
                    }
                    else
                    {
                        drawArrow.DrawCurvedArrow(startPoint, endPoint, false);
                    }
                }
            }
        }
    }

    private void SetArrowColors(Color color)
    {
        foreach (var arrow in _arrowRenderers)
        {
            if (arrow != null)
            {
                var lineRenderer = arrow.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.startColor = color;
                    lineRenderer.endColor = color;
                }
            }
        }
    }

    private void SetArrowColor(int arrowIndex, Color color)
    {
        if (_arrowRenderers[arrowIndex] != null)
        {
            var lineRenderer = _arrowRenderers[arrowIndex].GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }
    }

    private void DarkenArrowColor(int arrowIndex, float alpha)
    {
        if (_arrowRenderers[arrowIndex] != null)
        {
            var lineRenderer = _arrowRenderers[arrowIndex].GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                Color startColor = lineRenderer.startColor;
                Color endColor = lineRenderer.endColor;

                startColor.a = alpha;
                endColor.a = alpha;

                lineRenderer.startColor = startColor;
                lineRenderer.endColor = endColor;
            }
        }
    }

    private void ClearArrows()
    {
        foreach (var arrow in _arrowRenderers)
        {
            if (arrow != null)
            {
                Destroy(arrow);
            }
        }
    }

    #endregion

    #region MouseClick

    private IEnumerator SecondClick()
    {
        while (!_secondClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _secondClickDone = true;
                _secondMousePosition = GetMousePoint();
                if (_currentTarget != null)
                {
                    if (_secondMousePosition.x < _firstMousePosition.x)
                    {
                        DarkenArrowColor(0, 0.8f);
                        DarkenArrowColor(1, 0f);
                    }
                    else
                    {
                        DarkenArrowColor(0, 0f);
                        DarkenArrowColor(1, 0.8f);
                    }
                }
            }
            yield return null;
        }
    }

    private IEnumerator ThirdClick()
    {
        while (!_thirdClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _thirdClickDone = true;
                _thirdMousePosition = GetMousePoint();

                if (_currentTarget != null)
                {
                    if (_thirdMousePosition.x < _secondMousePosition.x)
                    {
                        DarkenArrowColor(2, 0.8f);
                        DarkenArrowColor(3, 0f);
                    }
                    else
                    {
                        DarkenArrowColor(2, 0f);
                        DarkenArrowColor(3, 0.8f);
                    }
                }
                _colorLockedAfterThirdClick = true;
            }
            yield return null;
        }
    }

    #endregion

    #region Update Method for Mouse Movement Detection

    private void UpdateMouseDetection()
    {
        if (_firstClickCompleted && !_secondClickDone)
        {
            Vector3 currentMousePosition = GetMousePoint();
            if (currentMousePosition.x < _firstMousePosition.x)
            {
                SetArrowColor(0, Color.green);
                SetArrowColor(1, Color.red);
            }
            else
            {
                SetArrowColor(0, Color.red);
                SetArrowColor(1, Color.green);
            }
        }

        if (_secondClickDone && !_colorLockedAfterSecondClick && !_colorLockedAfterThirdClick)
        {
            Vector3 currentMousePosition = GetMousePoint();
            if (currentMousePosition.x < _secondMousePosition.x)
            {
                SetArrowColor(2, Color.green);
                SetArrowColor(3, Color.red);
            }
            else
            {
                SetArrowColor(2, Color.red);
                SetArrowColor(3, Color.green);
            }
        }
    }

    #endregion

    #region CheckingMethods

    private void CheckingActiveTalents()
    {
        _poisonBallInfo.IsActiveTransparentPoisons = _transparentPoisons.Data.IsOpen;
        _poisonBallInfo.IsActiveWitheringPoison = _witheringPoison.Data.IsOpen;
        _poisonBallInfo.IsActiveContinuationAmbush = _continuationAmbush.Data.IsOpen;
        _poisonBallInfo.IsActiveHealingPoisonBall = _healingPoisonBall.Data.IsOpen;
        _poisonBallInfo.IsActiveHealingPoisonCloud = _healPoisonCloud.Data.IsOpen;
        _poisonBallInfo.IsActiveEnlargedGlands = _enlargedGlands.Data.IsOpen;
        _poisonBallInfo.IsActiveVoluminousBall = _voluminousBall.Data.IsOpen;
        _poisonBallInfo.IsActiveInertialGlands = _inertialGlands.Data.IsOpen;
        _poisonBallInfo.IsActiveVolatilityOfPoisons = _volatilityOfPoisons.Data.IsOpen;

        #region TransparentPoisonsTalentIsActive

        if (_transparentPoisons.Data.IsOpen && _player.IsInvisible)
        {
            _isPlayerInvisible = true;
        }
        else
        {
            _isPlayerInvisible = false;
        }

        #endregion

        #region VolatilityOfPoisonsTalentIsActive

        if (_poisonBallInfo.IsActiveVolatilityOfPoisons && _poisonBoneStacks > 0)
        {
            float multiplier = _poisonBoneStacks * 0.1f;
            _multiplierForPushDistance = multiplier;
        }
        else
        {
            _multiplierForPushDistance = 0;
        }

        #endregion

        #region VoluminousBallTalentIsActive

        if (_poisonBallInfo.IsActiveVoluminousBall && !_isBallCanBigger)
        {
            float multiplier = _baseCastWidth * 0.2f;
            _castWidth += multiplier;
            _isBallCanBigger = true;
        }
        else if (!_poisonBallInfo.IsActiveVoluminousBall && _isBallCanBigger)
        {
            _castWidth = _baseCastWidth;
            _isBallCanBigger = false;
        }

        #endregion

        #region EnlargedGlandTalentIsActive

        if (_poisonBallInfo.IsActiveEnlargedGlands && _maxCharges == 3)
        {
            AddMaxChargeCount();
            _poisonBallInfo.MaxCountProjectile = _maxCharges;
        }
        else if (!_poisonBallInfo.IsActiveEnlargedGlands && _maxCharges >= 4)
        {
            DeductMaxChargeCount();
            _poisonBallInfo.MaxCountProjectile = _maxCharges;
        }

        #endregion

        #region InertialGlandTalentIsActive

        if (_poisonBallInfo.IsActiveInertialGlands && _poisonBallInfo.IsThreeProjectileOnOnetarget)
        {
            float newRemainingTime = 0.0f;
            _spitPoison.ReductionSetCooldown(newRemainingTime);
        }

        #endregion

        #region ContinuationAmbushTalentIsActive

        if (_poisonBallInfo.IsActiveContinuationAmbush && _poisonBallInfo.IsCanApplyInvisible)
        {
            _continuationAmbush.CanApplyInvisible(true);
        }

        #endregion

        #region AssasinPoisonTalentIsActive

        if (_assasinPoison.Data.IsOpen && _flowOfPoison.Data.IsOpen)
        {
            _currentStacksAsssasinPoison = _assasinPoison.CurrentChargeAssasinPoison;
            Debug.Log("PoisonBall / CurrentStacksAssasinPoison == " + _currentStacksAsssasinPoison);
            for (int i = 0; i < _currentStacksAsssasinPoison; i++)
            {
                Debug.Log("CycleFor");
                if (Chargers < _maxCharges)
                {
                    _currentStacksAsssasinPoison--;
                    Debug.Log("PoisonBall / CurrentStacksAssasinPoison == " + _currentStacksAsssasinPoison);
                    Debug.Log("PoisonBall / _chargeCooldown == " + _chargeCooldown);
                    float newCooldownTime = _chargeCooldown * 0;
                    Debug.Log("PoisonBall / newCooldownTime == " + newCooldownTime);
                    this.IncreaseSetCooldown(newCooldownTime);
                }
            }
        }

        #endregion
    }

    private void CheckWhoTarget()
    {
        if (_currentTarget != null)
        {
            if (_currentTarget.gameObject == _player.gameObject)
            {
                Debug.Log("CurrentTarget = player ");
                _poisonBallInfo.IsOriginalTargetPlayer = true;
                _poisonBallInfo.IsOriginalTargetAllies = false;
                _poisonBallInfo.IsOriginalTargetEnemy = false;

                if (_poisonBallInfo.IsActiveHealingPoisonCloud)
                {
                    _poisonBallInfo.IsHealingPoisonCloud = true;
                }
                else
                {
                    _poisonBallInfo.IsHealingPoisonCloud = false;
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Allies"))
            {
                _poisonBallInfo.IsOriginalTargetPlayer = false;
                _poisonBallInfo.IsOriginalTargetAllies = true;
                _poisonBallInfo.IsOriginalTargetEnemy = false;

                if (_poisonBallInfo.IsActiveHealingPoisonCloud)
                {
                    _poisonBallInfo.IsHealingPoisonCloud = true;
                }
                else
                {
                    _poisonBallInfo.IsHealingPoisonCloud = false;
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                _poisonBallInfo.IsOriginalTargetPlayer = false;
                _poisonBallInfo.IsOriginalTargetAllies = false;
                _poisonBallInfo.IsOriginalTargetEnemy = true;
                if (_poisonBallInfo.IsActiveHealingPoisonCloud)
                {
                    _poisonBallInfo.IsHealingPoisonCloud = false;
                }
            }
        }
        else
        {
            _poisonBallInfo.IsOriginalTargetPlayer = false;
            _poisonBallInfo.IsOriginalTargetAllies = false;
            _poisonBallInfo.IsOriginalTargetEnemy = false;
        }
    }

    private void Timer()
    {
        _poisonBallInfo.TimeBetweenAttack -= Time.deltaTime;

        if (_poisonBallInfo.TimeBetweenAttack < 0)
        {
            _poisonBallInfo.CountProjectiles = 0;
            _poisonBallInfo.IsActiveTimer = false;
            _poisonBallInfo.IsProjectileCreate = false;
        }
    }

    private void CooldownChange()
    {
        if (_poisonBallInfo.IsActiveHealingPoisonBall && (_poisonBallInfo.IsOriginalTargetAllies || _poisonBallInfo.IsOriginalTargetPlayer))
        {
            _chargeCooldown = _originalChargeCooldown / 2;
        }
        else
        {
            _chargeCooldown = _originalChargeCooldown;
        }
    }

    private bool CheckCanCast()
    {
        //Debug.Log("CheckCanCast PoisonBall");

        if (_currentTarget == null)
            return Vector3.Distance(_firstMousePosition, transform.position) <= Radius && NoObstacles(_firstMousePosition, _obstacle);

        return Vector3.Distance(_firstMousePosition, transform.position) <= Radius &&
            NoObstacles(_firstMousePosition, _obstacle) ||
            Vector3.Distance(_currentTarget.transform.position, transform.position) <= Radius && 
            NoObstacles(_currentTarget.transform.position, _obstacle);

    }

    #endregion

    #region ChooseMoveSpeedProjectile

    private void ChooseMovementDependingOnCountProjectiles()
    {
        ChooseSpeed();
        ChooseDirectionPush();
        StartCoroutine(_isFast ? TimeCastForFastMoveProjectile() : TimeCastForSlowMoveProjectile());
    }

    private void ChooseSpeed()
    {
        _isFast = _isTarget
            ? Vector2.Distance(_player.transform.position, _secondMousePosition) > Vector2.Distance(_player.transform.position, _currentTarget.transform.position)
            : Vector2.Distance(_player.transform.position, _secondMousePosition) > Vector2.Distance(_player.transform.position, _firstMousePosition);
    }

    private void ChooseDirectionPush()
    {
        _isPushTarget = Vector2.Distance(_player.transform.position, _thirdMousePosition) > Vector2.Distance(_player.transform.position, _secondMousePosition);
    }

    private IEnumerator TimeCastForFastMoveProjectile()
    {
        _castDeley = _slowTimeCast;
        yield return null;
    }

    private IEnumerator TimeCastForSlowMoveProjectile()
    {
        _castDeley = _fastTimeCast;
        yield return null;
    }

    private void ChooseWhichProjectileCreate()
    {
        if (_isTarget)
        {
            CmdCreateProjectileForTarget(_currentTarget.gameObject, _currentTarget.transform.position, _poisonBallInfo.MaxCountProjectile, 
                _multiplierForPushDistance, _projectileSize,
                _isFast, _isPushTarget, _isPlayerInvisible,
                _poisonBallInfo.IsActiveHealingPoisonBall, _poisonBallInfo.IsActiveWitheringPoison, _poisonBallInfo.IsActiveVoluminousBall,
                _poisonBallInfo.IsOriginalTargetEnemy, _poisonBallInfo.IsOriginalTargetPlayer, _poisonBallInfo.IsOriginalTargetAllies, PoisonBoneStack);

            //CmdApplyPoisonCloud(_poisonBallInfo.IsHealingPoisonCloud, _durationPoisonCloud);
        }
        else
        {
            CmdCreateProjectileForFlyingMaxDistance(_firstMousePosition, _poisonBallInfo.MaxCountProjectile,
                _multiplierForPushDistance, _projectileSize,
                _isFast, _isPushTarget, _isPlayerInvisible,
                _poisonBallInfo.IsActiveHealingPoisonBall, _poisonBallInfo.IsActiveWitheringPoison, _poisonBallInfo.IsActiveVoluminousBall,
                _poisonBallInfo.IsOriginalTargetEnemy, _poisonBallInfo.IsOriginalTargetPlayer, _poisonBallInfo.IsOriginalTargetAllies, PoisonBoneStack);

            //CmdApplyPoisonCloud(_poisonBallInfo.IsHealingPoisonCloud, _durationPoisonCloud);
        }
    }

    #endregion

    #region Command Methods

    [Command]
    private void CmdCreateProjectileForTarget(GameObject target, Vector3 targetOrPoint, int maxCountProjectiles, 
        float multiplierForPushDistance, float projectileSize,
        bool isFast, bool isPushTarget, bool isPlayerInvisible,
        bool isActiveHealingPoisonBall, bool isActiveWitheringPoison, bool isActiveVoluminousBall,
        bool isTargetEnemy, bool isTargetPlayer, bool isTargetAllies, int poisonBoneStack)

    {
        CurrentTarget = target;
        FootInstinctsTalent = _footInstincts;
        RestorationOfGlandsTalent = _restorationOfGlands;

        if (LastTarget == CurrentTarget)
        {
            _poisonBallInfo.CountProjectiles += 1;
            _poisonBallInfo.IsProjectileCreate = true;
        }
        else
        {
            _poisonBallInfo.IsActiveTimer = false;
            _poisonBallInfo.IsThreeProjectileOnOnetarget = false;
            _poisonBallInfo.IsCanApplyInvisible = false;
            _poisonBallInfo.CountProjectiles = 1;
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        }

        if (_poisonBallInfo.CountProjectiles >= 3 && _poisonBallInfo.IsActiveInertialGlands)
        {
            _poisonBallInfo.IsThreeProjectileOnOnetarget = true;
        }

        if (_poisonBallInfo.CountProjectiles >= 4 && _poisonBallInfo.IsActiveContinuationAmbush)
        {
            _poisonBallInfo.IsCanApplyInvisible = true;
        }

        if (_poisonBallInfo.CountProjectiles < maxCountProjectiles && LastTarget == CurrentTarget)
        {
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
            _poisonBallInfo.IsActiveTimer = true;
        }

        //Debug.Log($"bool isTargetEnemy = {isTargetEnemy}, bool isTargetPlayer = {isTargetPlayer}, bool isTargetAllies = {isTargetAllies}");

       // Debug.Log($"_poisonBallInfo.HealingBall = {_poisonBallInfo.IsActiveHealingPoisonBall}");

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>(); 

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.CurrentValue, multiplierForPushDistance, projectileSize, this, isActiveHealingPoisonBall,
            isTargetPlayer, isTargetEnemy, isTargetAllies, isActiveWitheringPoison, isPushTarget, isActiveVoluminousBall, isPlayerInvisible, poisonBoneStack);

        poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);

        NetworkServer.Spawn(item);

        if (_poisonBallInfo.CountProjectiles > maxCountProjectiles)
        {
            _poisonBallInfo.IsActiveTimer = false;

            _poisonBallInfo.CountProjectiles = 0;
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        }
    }

    [Command]
    private void CmdCreateProjectileForFlyingMaxDistance(Vector3 point, int maxCountProjectiles,
        float multiplierForPushDistance, float projectileSize,
        bool isFast, bool isPushTarget, bool isPlayerInvisible,
        bool isActiveHealingPoisonBall, bool isActiveWitheringPoison, bool isActiveVoluminousBall,
        bool isTargetEnemy, bool isTargetPlayer, bool isTargetAllies, int poisonBoneStack)
    {
        RestorationOfGlandsTalent = _restorationOfGlands;
        FootInstinctsTalent = _footInstincts;
        CurrentTarget = LastTarget;

        if (LastTarget == CurrentTarget)
        {
            _poisonBallInfo.CountProjectiles += 1;
            _poisonBallInfo.IsProjectileCreate = true;
        }
        else
        {
            _poisonBallInfo.IsActiveTimer = false;
            _poisonBallInfo.IsThreeProjectileOnOnetarget = false;
            _poisonBallInfo.IsCanApplyInvisible = false;
            _poisonBallInfo.CountProjectiles = 1;
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        }

        if (_poisonBallInfo.CountProjectiles >= 3 && _poisonBallInfo.IsActiveInertialGlands)
        {
            _poisonBallInfo.IsThreeProjectileOnOnetarget = true;
        }

        if (_poisonBallInfo.CountProjectiles >= 4 && _poisonBallInfo.IsActiveContinuationAmbush)
        {
            _poisonBallInfo.IsCanApplyInvisible = true;
        }

        if (_poisonBallInfo.CountProjectiles < maxCountProjectiles && LastTarget == CurrentTarget)
        {
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
            _poisonBallInfo.IsActiveTimer = true;
        }

        //Debug.Log($"bool isTargetEnemy = {isTargetEnemy}, bool isTargetPlayer = {isTargetPlayer}, bool isTargetAllies = {isTargetAllies}");

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>(); 

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.CurrentValue, multiplierForPushDistance, projectileSize, this, isActiveHealingPoisonBall,
            isTargetPlayer, isTargetEnemy, isTargetAllies, isActiveWitheringPoison, isPushTarget, isActiveVoluminousBall, isPlayerInvisible, poisonBoneStack);

        poisonBallProjectile.MoveBallOnMaxDistance(point, isFast);

        NetworkServer.Spawn(item);

        if (_poisonBallInfo.CountProjectiles >= maxCountProjectiles)
        {
            _poisonBallInfo.IsActiveTimer = false;

            _poisonBallInfo.CountProjectiles = 0;
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        }
    }

    [Command]
    private void CmdApplyPoisonCloud(bool isHealingCloud, float duration)
    {
        if (!isHealingCloud)
        {
            if (_poisonDamagingCloud == null && _poisonDamagingCloudPrefab.PoisonDamageCloud == null)
            {
                _player.CharacterState.AddState(States.PoisonCloud, duration, 0, _player.gameObject, Name);

                _poisonDamagingCloud = Instantiate(_poisonDamagingCloudPrefab, transform.position, Quaternion.identity);

                _poisonDamagingCloudPrefab.PoisonDamageCloud = _poisonDamagingCloud;
                SceneManager.MoveGameObjectToScene(_poisonDamagingCloudPrefab.PoisonDamageCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonDamagingCloudPrefab.PoisonDamageCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
                _poisonDamagingCloudPrefab.PoisonDamageCloud.AddStack();

                NetworkServer.Spawn(_poisonDamagingCloud.gameObject);

                Debug.Log("PoisonBall / CmdApplyPoisonCloud / if / _poisonDamagingCloud = " + _poisonDamagingCloud);
                Debug.Log("PoisonBall / CmdApplyPoisonCloud / if / _poisonDamagingCloudPrefab.PoisonDamageCloud = " + _poisonDamagingCloudPrefab.PoisonDamageCloud);
            }
            else
            {
                Debug.Log("PoisonBall / CmdApplyPoisonCloud / else / _poisonDamagingCloud = " + _poisonDamagingCloudPrefab.PoisonDamageCloud);
                _player.CharacterState.AddState(States.PoisonCloud, duration, 0, _player.gameObject, Name);
                _poisonDamagingCloudPrefab.PoisonDamageCloud.AddStack();
            }
        }
        else
        {
            if (_poisonHealingCloud == null && _poisonHealingCloudPrefab.PoisonHealingCloud == null)
            {
                _player.CharacterState.AddState(States.HealingPoisonCloud, duration, 0, _player.gameObject, Name);

                _poisonHealingCloud = Instantiate(_poisonHealingCloudPrefab, transform.position, Quaternion.identity);
                _poisonHealingCloudPrefab.PoisonHealingCloud = _poisonHealingCloud;
                SceneManager.MoveGameObjectToScene(_poisonHealingCloudPrefab.PoisonHealingCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonHealingCloudPrefab.PoisonHealingCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
                _poisonHealingCloudPrefab.PoisonHealingCloud.AddStack();

                NetworkServer.Spawn(_poisonHealingCloud.gameObject);
            }
            else
            {
                _player.CharacterState.AddState(States.HealingPoisonCloud, duration, 0, _player.gameObject, Name);
                _poisonHealingCloudPrefab.PoisonHealingCloud.AddStack();
            }
        }
        RpcApply(_poisonDamagingCloudPrefab.PoisonDamageCloud, _poisonHealingCloudPrefab.PoisonHealingCloud, duration, isHealingCloud);
    }

    #endregion

    [ClientRpc]
    private void RpcApply(PoisonDamagingCloudPrefab poisonDamagingCloud, PoisonHealingCloudPrefab poisonHealingCloud, float duration, bool isHealingCloud)
    {
            Debug.Log("PoisonBall / RpcApply / if (poisonDamagingCloud != null) = " + poisonDamagingCloud);
        if (poisonDamagingCloud != null)
        {
            poisonDamagingCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
            poisonDamagingCloud.AddStack();
        }

        if (poisonHealingCloud != null && isHealingCloud)
        {
            poisonHealingCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
            poisonHealingCloud.AddStack();
        }
    }
}
