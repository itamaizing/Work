using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

public class PoisonBall : Skill
{
    #region Variables

    [Header("Talents")]
    [SerializeField] private FootInstincts _footInstincts;
    [SerializeField] private HealingPoisonBall _healingPoisonBall;
    [SerializeField] private HealPoisonCloud _healPoisonCloud;
    [SerializeField] private WitheringPoison _witheringPoison;
    [SerializeField] private EnlargedGlands _enlargedGlands;
    [SerializeField] private ContinuationAmbush _continuationAmbush;
    [SerializeField] private VoluminousBall _voluminousBall;

    [SerializeField] private Character _player;
    [SerializeField] private PoisonBallProjectile _projectile;
    [SerializeField] private GameObject arrowPrefab;

    public int CurrentCharges;
    public int CountProjectiles;
    public float TimeBetweenAttack;
    public float StartTimeBetweenAttack = 3.0f;

    public bool IsActiveTimer = false; 

    private GameObject[] arrowRenderers = new GameObject[4];

    private Character _currentTarget;

    private Vector3 _firstMousePosition = Vector3.positiveInfinity;
    private Vector3 _secondMousePosition;
    private Vector3 _thirdMousePosition;

    private int _countProjectiles = 0;

    private float _fastTimeCast = 0.4f;
    private float _slowTimeCast = 1.8f;
    private float _originalChargeCooldown;
    private float _durationPoisonCloud = 6f;

    #region BoolVariables

    private bool _isPushTarget;
    private bool _isTarget;
    private bool _isFast;
    private bool _isOriginalTargetEnemy;
    private bool _isOriginalTargetAllies;
    private bool _isOriginalTargetPlayer;
    private bool _secondClickDone = false;
    private bool _thirdClickDone = false;
    private bool _isActiveHealingPoisonBall;
    private bool _isActiveEnlargedGlands;
    private bool _isActiveContinuationAmbush;
    private bool _isActiveVoluminousBall;
    private bool _isHealingPoisonCloud;
    private bool firstClickCompleted = false;
    private bool colorLockedAfterSecondClick = false;
    private bool colorLockedAfterThirdClick = false;

    #endregion

    private Coroutine _secondClickCoroutine;
    private Coroutine _thirdClickCoroutine;

    public GameObject LastTarget { get; set; }
    public GameObject CurrentTarget { get; set; }
    public FootInstincts FootInstinctsTalent { get; set; }
    public ContinuationAmbush ContinuationAmbushTalent { get; set; }


    #endregion

    #region PrepareAndStartJob

    protected void Start()
    {
        _originalChargeCooldown = _chargeCooldown;
        TimeBetweenAttack = StartTimeBetweenAttack;
    }

    protected override bool IsCanCast => CheckCanCast();

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

        firstClickCompleted = false;
        colorLockedAfterSecondClick = false;
        colorLockedAfterThirdClick = false;

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
            if (Input.GetMouseButtonDown(0))
            {
                _currentTarget = GetRaycastTarget(true);

                if (_currentTarget != null)
                {
                    Debug.Log($"Target Found: {_currentTarget.name}");
                    CheckWhoTarget();
                    _firstMousePosition = GetMousePoint();
                    CreateArrowsParallelToPlayer();
                    StopAutoDraw();
                    firstClickCompleted = true;
                }
                else
                {
                    Debug.Log("Target not found.");
                }
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
        Debug.Log("PoisonBall / CastJob / Called ChooseWhichProjectileCreate");

        ClearArrows();

        yield return null;
    }

    private void UseAbility()
    {
        if (_secondClickDone && _thirdClickDone)
        {
            _isTarget = IsTarget();
            ChooseMovementDependingOnCountProjectiles();
        }
        else
        {
            Debug.Log("Ability not triggered due to incomplete clicks.");
        }
    }

    #endregion

    #region ArrowManagement

    private void CreateArrowsParallelToPlayer()
    {
        if (_currentTarget == null || arrowPrefab == null)
        {
            Debug.LogError("Arrow Prefab is not assigned or Target is null");
            return;
        }

        Vector3 targetPosition = _currentTarget.transform.position;
        Vector3 playerPosition = _player.transform.position;

        Vector3 directionToTarget = (targetPosition - playerPosition).normalized;

        Vector3 perpendicularDirection = Vector3.Cross(directionToTarget, Vector3.forward).normalized;

        arrowRenderers[0] = Instantiate(arrowPrefab, targetPosition + directionToTarget, Quaternion.identity);
        arrowRenderers[1] = Instantiate(arrowPrefab, targetPosition - directionToTarget, Quaternion.identity);
        arrowRenderers[2] = Instantiate(arrowPrefab, targetPosition + directionToTarget * 1.5f, Quaternion.identity);
        arrowRenderers[3] = Instantiate(arrowPrefab, targetPosition - directionToTarget * 1.5f, Quaternion.identity);

        SetArrowDirections(perpendicularDirection);
        SetArrowColors(Color.red);
    }

    private void SetArrowDirections(Vector3 perpendicularDirection)
    {
        for (int i = 0; i < arrowRenderers.Length; i++)
        {
            var arrow = arrowRenderers[i];
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
        foreach (var arrow in arrowRenderers)
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
        if (arrowRenderers[arrowIndex] != null)
        {
            var lineRenderer = arrowRenderers[arrowIndex].GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }
    }

    private void DarkenArrowColor(int arrowIndex, float alpha)
    {
        if (arrowRenderers[arrowIndex] != null)
        {
            var lineRenderer = arrowRenderers[arrowIndex].GetComponent<LineRenderer>();
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
        foreach (var arrow in arrowRenderers)
        {
            if (arrow != null)
            {
                Destroy(arrow);
            }
        }

        Debug.Log("Arrows cleared.");
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

                colorLockedAfterThirdClick = true;
            }
            yield return null;
        }
    }



    #endregion

    #region Update Method for Mouse Movement Detection

    private void Update()
    {
        if (firstClickCompleted && !_secondClickDone)
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

        if (_secondClickDone && !colorLockedAfterSecondClick && !colorLockedAfterThirdClick)
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
        _isActiveHealingPoisonBall = _healingPoisonBall.IsActive;
        _isActiveContinuationAmbush = _continuationAmbush.IsActive;
        _isActiveEnlargedGlands = _enlargedGlands.IsActive;
        _isActiveVoluminousBall = _voluminousBall.IsActive;

        if (_isActiveEnlargedGlands)
        {
            MaxCharges = 4;
        }
    }

    private void CheckWhoTarget()
    {
        if (_currentTarget != null)
        {
            if (_currentTarget.gameObject == _player.gameObject)
            {
                Debug.Log("Target == Player");
                _isOriginalTargetPlayer = true;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = false;
                _isHealingPoisonCloud = _healPoisonCloud.IsActive;
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Allies"))
            {
                Debug.Log("Target == Allies");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = true;
                _isOriginalTargetEnemy = false;
                _isHealingPoisonCloud = _healPoisonCloud.IsActive;
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                Debug.Log("Target == Enemies");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = true;
                _isHealingPoisonCloud = _healPoisonCloud.IsActive;
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
        if (_isActiveHealingPoisonBall && (_isOriginalTargetAllies || _isOriginalTargetPlayer))
        {
            _chargeCooldown = _originalChargeCooldown / 2;
        }
        else
        {
            _chargeCooldown = _originalChargeCooldown;
        }
    }

    #endregion

    #region BooleanMethods

    private bool CheckCanCast()
    {
        return _currentTarget != null &&
               (Vector3.Distance(_firstMousePosition, transform.position) <= Radius ||
                Vector3.Distance(_currentTarget.transform.position, transform.position) <= Radius);
    }

    private bool IsTarget()
    {
        return _currentTarget != null;
    }

    #endregion

    #region ChooseMoveSpeedProjectile

    private void ChooseMovementDependingOnCountProjectiles()
    {
        if (_countProjectiles < 3)
        {
            ChooseSpeed();
            ChooseDirectionPush();
            StartCoroutine(_isFast ? TimeCastForFastMoveProjectile() : TimeCastForSlowMoveProjectile());
        }
        else if (_countProjectiles == 3)
        {
            ChooseSpeed();
            ChooseDirectionPush();
            StartCoroutine(TimeCastForThirdProjectile());
        }
    }

    private void ChooseSpeed()
    {
        _isFast = _isTarget
            ? Vector2.Distance(_player.transform.position, _secondMousePosition) > Vector2.Distance(_player.transform.position, _currentTarget.transform.position)
            : Vector2.Distance(_player.transform.position, _secondMousePosition) > Vector2.Distance(_player.transform.position, _firstMousePosition);
        Debug.Log($"ISFAST?? = {_isFast}");
    }

    private void ChooseDirectionPush()
    {
        _isPushTarget = Vector2.Distance(_player.transform.position, _thirdMousePosition) > Vector2.Distance(_player.transform.position, _secondMousePosition);
    }

    private IEnumerator TimeCastForFastMoveProjectile()
    {
        _castDelay = _slowTimeCast;
        yield return null;
    }

    private IEnumerator TimeCastForSlowMoveProjectile()
    {
        _castDelay = _fastTimeCast;
        yield return null;
    }

    private IEnumerator TimeCastForThirdProjectile()
    {
        _castDelay = 0.4f;
        yield return null;
    }

    private void ChooseWhichProjectileCreate()
    {
        if (_isTarget)
        {
            CmdCreateProjectileForTarget(_currentTarget.gameObject, _currentTarget.transform.position,
                _isFast, _isPushTarget, _isActiveHealingPoisonBall, _isOriginalTargetPlayer, _isOriginalTargetEnemy,
                _isOriginalTargetAllies, _witheringPoison.IsActive, _isActiveContinuationAmbush, _isActiveVoluminousBall);

            CmdApplyCloudPoison(_healPoisonCloud.IsActive, _isHealingPoisonCloud);
        }
        else
        {
            CmdCreateProjectileForFlyingMaxDistance(_firstMousePosition,
                _isFast, _isPushTarget, _isActiveHealingPoisonBall, _isOriginalTargetPlayer, _isOriginalTargetEnemy,
                _isOriginalTargetAllies, _witheringPoison.IsActive, _isActiveContinuationAmbush, _isActiveVoluminousBall);

            CmdApplyCloudPoison(_healPoisonCloud.IsActive, _isHealingPoisonCloud);
        }
    }

    #endregion

    #region Command Methods

    [Command]
    private void CmdCreateProjectileForTarget(GameObject target, Vector3 targetOrPoint,
        bool isFast, bool isPushTarget, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies,
        bool isActiveWitheringPoison, bool isActiveContinuationAmbush, bool isActiveVoluminousBall)
    { 
        CurrentTarget = target;
        FootInstinctsTalent = _footInstincts;

        if (LastTarget == CurrentTarget)
        {
            CountProjectiles++;
            Debug.Log($"PoisonBall / CmdCreateToTarget / first if CountProjectiles == {CountProjectiles}");
        }
        else
        {
            CountProjectiles = 1;
            Debug.Log($"PoisonBall / CmdCreateToTarget / first else CountProjectiles == {CountProjectiles}");
        }

        if (CountProjectiles == 1 && LastTarget == CurrentTarget)
        {
            TimeBetweenAttack = StartTimeBetweenAttack;
            IsActiveTimer = true;
            Debug.Log($"PoisonBall / CmdCreateToTarget / second if CountProjectiles == {CountProjectiles}, IsActiveTimer = {IsActiveTimer}, " +
                $"TimeBetweenAttack = {TimeBetweenAttack}");
        }
        else if (CountProjectiles == 2 && LastTarget == CurrentTarget)
        {
            TimeBetweenAttack = StartTimeBetweenAttack;
            Debug.Log($"PoisonBall / CmdCreateToTarget / first else if CountProjectiles == {CountProjectiles}, IsActiveTimer = {IsActiveTimer}, " +
                      $"TimeBetweenAttack = {TimeBetweenAttack}");
        }
        else if (LastTarget != CurrentTarget || CountProjectiles == 3)
        {
            IsActiveTimer = false;
            CountProjectiles = 0;
            Debug.Log($"PoisonBall / CmdCreateToTarget / second else if CountProjectiles == {CountProjectiles}, IsActiveTimer = {IsActiveTimer}, " +
            $"TimeBetweenAttack = {TimeBetweenAttack}");
        }

        if (IsActiveTimer)
        {
            TimeBetweenAttack -= Time.deltaTime;
            Debug.Log($"PoisonBall / CmdCreateToTarget / Timer = {TimeBetweenAttack}");
            if (TimeBetweenAttack <= 0)
            {
                IsActiveTimer = false;
                CountProjectiles = 0;
            }
        }
        
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>(); 

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.Value, isActiveTalent,
            isTargetPlayer, isTargetEnemy, isTargetAllies, isActiveWitheringPoison, isPushTarget, isActiveContinuationAmbush, isActiveVoluminousBall);

        poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdCreateProjectileForFlyingMaxDistance(Vector3 point, bool isFast, bool isPushTarget,
        bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies,
        bool isActiveWitheringPoison, bool isActiveContinuationAmbush, bool isActiveVoluminousBall)
    {
        FootInstinctsTalent = _footInstincts;

        if (LastTarget == CurrentTarget)
        {
            CountProjectiles++;
        }
        else
        {
            CountProjectiles = 1;
        }

        if (CountProjectiles == 1 && LastTarget == CurrentTarget)
        {
            TimeBetweenAttack = StartTimeBetweenAttack;
            IsActiveTimer = true;
        }
        else if (CountProjectiles == 2 && LastTarget == CurrentTarget)
        {
            TimeBetweenAttack = StartTimeBetweenAttack;
        }
        else if (LastTarget != CurrentTarget || CountProjectiles == 3)
        {
            IsActiveTimer = false;
            CountProjectiles = 0;
        }

        if (IsActiveTimer)
        {
            TimeBetweenAttack -= Time.deltaTime;
            if (TimeBetweenAttack <= 0)
            {
                IsActiveTimer = false;
                CountProjectiles = 0;
            }
        }

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>(); 

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.Value, isActiveTalent,
            isTargetPlayer, isTargetEnemy, isTargetAllies, isActiveWitheringPoison, isPushTarget, isActiveContinuationAmbush, isActiveVoluminousBall);

        poisonBallProjectile.MoveBallOnMaxDistance(point, isFast);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdApplyCloudPoison(bool isActiveTalent, bool isHealingCloud)
    {
        if (isActiveTalent && isHealingCloud)
        {
            _player.CharacterState.CmdAddState(States.HealingPoisonCloud, _durationPoisonCloud, 0);
        }
        else
        {
            _player.CharacterState.CmdAddState(States.PoisonCloud, _durationPoisonCloud, 0);
        }
    }

    public void PayCostPoisonBall()
    {
        TryPayCost();
    }

    #endregion
}
