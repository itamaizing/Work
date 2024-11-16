using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

[Serializable]
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

    public bool IsHealingPoisonCloud;
}

public struct PoisonBallActiveTalentsInfo : NetworkMessage
{
    public bool IsActiveTransparentPoisons;
    public bool IsActiveWitheringPoison;
    public bool IsActiveContinuationAmbush;
    public bool IsActiveHealingPoisonBall;
    public bool IsActiveHealingPoisonCloud;
    public bool IsActiveEnlargedGlands;
    public bool IsActiveVoluminousBall;
    public bool IsActiveInertialGlands;
    public bool IsActiveVolatilityOfPoisons;
    public bool IsActiveBallEffect;
}

public struct PoisonBallSpawnPointInfo : NetworkMessage
{
    public float SpawnPointX;
    public float SpawnPointY;
    public float SpawnPointZ;
}

public class PoisonBall : Skill, IAltAbility
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
    [SerializeField] private BallEffect _ballEffect;

    [Header("Ability properties")]
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private PoisonBallProjectile _projectile;
    [SerializeField] private Character _player;
    [SerializeField] private GameObject _arrowPrefab;
    [SerializeField] private GameObject _spawnPoint;
    
    #region PoisonCloud
    [SerializeField] private PoisonDamagingCloudPrefab _poisonDamagingCloudPrefab;
    [SerializeField] private PoisonHealingCloudPrefab _poisonHealingCloudPrefab;
    private PoisonDamagingCloudPrefab _poisonDamagingCloud;
    private PoisonHealingCloudPrefab _poisonHealingCloud;
    private float _durationPoisonCloud = 6f;
    #endregion

    private PoisonBallInfo _poisonBallInfo = new PoisonBallInfo();
    private PoisonBallSpawnPointInfo _spawnPointInfo = new PoisonBallSpawnPointInfo();
    private PoisonBallActiveTalentsInfo _activeTalentsInfo = new PoisonBallActiveTalentsInfo();

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
    private float _animTime;
    private float _baseAnimTime = 1f;

    #region BoolVariables

    private bool _isPushTarget;
    private bool _isTarget = false;
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
    public bool IsAltAbility { get; set; }

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("PoisonBallCastDelayAnimTrigger");
    protected override bool IsCanCast => CheckCanCast();

    public event Action ResetAbilityParameters;
    public event Action AbilityChange;

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
        #region InertialGlandTalentIsActive
        Debug.Log($"PoisonBall / _activeTalentsInfo.IsActiveInertialGlands = {_activeTalentsInfo.IsActiveInertialGlands} && _poisonBallInfo.IsThreeProjectileOnOnetarget = {_poisonBallInfo.IsThreeProjectileOnOnetarget}");
        if (_activeTalentsInfo.IsActiveInertialGlands && _poisonBallInfo.IsThreeProjectileOnOnetarget)
        {
            Debug.Log("PoisonBall / InertialGlandsIsActive");
            float newRemainingTime = 0.0f;
            _spitPoison.ReductionSetCooldown(newRemainingTime);
        }

        #endregion

        UpdateMouseDetection();
        if (_poisonBallInfo.IsActiveTimer)
        {
            Timer();
        }
    }

    private float GetAnimationClipLength()
    {
        RuntimeAnimatorController animController = _player.Animator.runtimeAnimatorController;
        foreach (var clip in animController.animationClips)
        {
            if (clip.name == "PoisonBallCastAnimation")
            {
                return clip.length;
            }
        }
        return -1f;
    }


    #region PrepareAndStartJob


    public void PayCostPoisonBall()
    {
        TryPayCost(true);
        Debug.Log("PoisonBall Charge = " + Chargers);
    }

    protected override void ClearData()
    {
        if (_animTime > 0)
            _player.Animator.speed = _baseAnimTime;

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

        _animTime = GetAnimationClipLength();

        SetSpawnPointPosition(_spawnPoint.transform.position.x, _spawnPoint.transform.position.y, _spawnPoint.transform.position.z);

        yield return _secondClickCoroutine = StartCoroutine(SecondClick());
        yield return _thirdClickCoroutine = StartCoroutine(ThirdClick());

        UseAbility();
    }

    protected override IEnumerator CastJob()
    {
        ChooseWhichProjectileCreate();

        ClearArrows();

        ResetAbilityParameters?.Invoke();

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
                    if (_secondMousePosition.x < _firstMousePosition.x && _secondMousePosition.z < _firstMousePosition.z)
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
                    if (_thirdMousePosition.x < _secondMousePosition.x && _thirdMousePosition.z < _secondMousePosition.z)
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

    private void SetArrowVisibility(int arrowIndex, bool isVisible)
    {
        if (arrowIndex >= 0 && arrowIndex < _arrowRenderers.Length && _arrowRenderers[arrowIndex] != null)
        {
            var lineRenderer = _arrowRenderers[arrowIndex].GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                Color startColor = lineRenderer.startColor;
                Color endColor = lineRenderer.endColor;

                // ���� ������� ������, ������������� ����� �� 1, ����� �� 0 (������������)
                startColor.a = isVisible ? 1f : 0f;
                endColor.a = isVisible ? 1f : 0f;

                lineRenderer.startColor = startColor;
                lineRenderer.endColor = endColor;
            }
        }
    }

    #endregion

    #region Update Method for Mouse Movement Detection

    private void UpdateMouseDetection()
    {
        // �������� ������� ������� ����
        Vector3 currentMousePosition = GetMousePoint();

        // ����������� ��� ������ ���� �������
        if (_firstClickCompleted && !_secondClickDone)
        {
            UpdateArrowHighlight(0, 1, currentMousePosition);
        }

        // ����������� ��� ������ ���� �������
        if (_secondClickDone && !_colorLockedAfterSecondClick && !_colorLockedAfterThirdClick)
        {
            UpdateArrowHighlight(2, 3, currentMousePosition);
        }
    }

    // ����� ��� ���������� ��������� �������
    private void UpdateArrowHighlight(int index1, int index2, Vector3 currentMousePosition)
    {
        Vector3 arrowPosition1 = _arrowRenderers[index1].transform.position;
        Vector3 arrowPosition2 = _arrowRenderers[index2].transform.position;

        Vector3 direction1 = (arrowPosition1 - currentMousePosition).normalized;

        bool isHorizontal = Mathf.Abs(direction1.x) > Mathf.Abs(direction1.z);

        if (isHorizontal)
        {
            if (Input.GetAxis("Mouse X") > 0)
            {
                SetArrowColor(index1, Color.green);
                SetArrowVisibility(index1, true);
                SetArrowVisibility(index2, false);
            }
            else if (Input.GetAxis("Mouse X") < 0)
            {
                SetArrowColor(index2, Color.green);
                SetArrowVisibility(index2, true);
                SetArrowVisibility(index1, false);
            }
        }
        else
        {
            if (Input.GetAxis("Mouse Z") > 0)
            {
                SetArrowColor(index1, Color.green);
                SetArrowVisibility(index1, true);
                SetArrowVisibility(index2, false);
            }
            else if (Input.GetAxis("Mouse Z") < 0)
            {
                SetArrowColor(index2, Color.green);
                SetArrowVisibility(index2, true);
                SetArrowVisibility(index1, false);
            }
        }
    }

    #endregion

    #region CheckingMethods

    private void CheckingActiveTalents()
    {

        _activeTalentsInfo.IsActiveTransparentPoisons = _transparentPoisons.Data.IsOpen;
        _activeTalentsInfo.IsActiveWitheringPoison = _witheringPoison.Data.IsOpen;
        _activeTalentsInfo.IsActiveContinuationAmbush = _continuationAmbush.Data.IsOpen;
        _activeTalentsInfo.IsActiveHealingPoisonBall = _healingPoisonBall.Data.IsOpen;
        _activeTalentsInfo.IsActiveHealingPoisonCloud = _healPoisonCloud.Data.IsOpen;
        _activeTalentsInfo.IsActiveEnlargedGlands = _enlargedGlands.Data.IsOpen;
        _activeTalentsInfo.IsActiveVoluminousBall = _voluminousBall.Data.IsOpen;
        _activeTalentsInfo.IsActiveInertialGlands = _inertialGlands.Data.IsOpen;
        _activeTalentsInfo.IsActiveVolatilityOfPoisons = _volatilityOfPoisons.Data.IsOpen;
        _activeTalentsInfo.IsActiveBallEffect = _ballEffect.Data.IsOpen;

        #region VolatilityOfPoisonsTalentIsActive

        if (_activeTalentsInfo.IsActiveVolatilityOfPoisons && _poisonBoneStacks > 0)
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

        if (_activeTalentsInfo.IsActiveVoluminousBall && !_isBallCanBigger)
        {
            float multiplier = _baseCastWidth * 0.2f;
            _castWidth += multiplier;
            _isBallCanBigger = true;
        }
        else if (!_activeTalentsInfo.IsActiveVoluminousBall && _isBallCanBigger)
        {
            _castWidth = _baseCastWidth;
            _isBallCanBigger = false;
        }

        #endregion

        #region EnlargedGlandTalentIsActive

        if (_activeTalentsInfo.IsActiveEnlargedGlands && _maxCharges == 3)
        {
            AddMaxChargeCount();
            _poisonBallInfo.MaxCountProjectile = _maxCharges;
        }
        else if (!_activeTalentsInfo.IsActiveEnlargedGlands && _maxCharges >= 4)
        {
            DeductMaxChargeCount();
            _poisonBallInfo.MaxCountProjectile = _maxCharges;
        }

        #endregion

        #region ContinuationAmbushTalentIsActive

        if (_activeTalentsInfo.IsActiveContinuationAmbush && _poisonBallInfo.IsCanApplyInvisible)
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

                if (_activeTalentsInfo.IsActiveHealingPoisonCloud)
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
                Debug.Log("CurrentTarget = allies ");
                _poisonBallInfo.IsOriginalTargetPlayer = false;
                _poisonBallInfo.IsOriginalTargetAllies = true;
                _poisonBallInfo.IsOriginalTargetEnemy = false;

                if (_activeTalentsInfo.IsActiveHealingPoisonCloud)
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
                Debug.Log("CurrentTarget = enemy ");
                _poisonBallInfo.IsOriginalTargetPlayer = false;
                _poisonBallInfo.IsOriginalTargetAllies = false;
                _poisonBallInfo.IsOriginalTargetEnemy = true;

                if (_activeTalentsInfo.IsActiveHealingPoisonCloud)
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
            Debug.Log("PoisonBall / Timer if ");
            _poisonBallInfo.CountProjectiles = 0;
            _poisonBallInfo.IsActiveTimer = false;
            _poisonBallInfo.IsProjectileCreate = false;
        }
    }

    private void CooldownChange()
    {
        if (_activeTalentsInfo.IsActiveHealingPoisonBall && (_poisonBallInfo.IsOriginalTargetAllies || _poisonBallInfo.IsOriginalTargetPlayer))
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

        if (_animTime > 0)
        {
            float multiplierAnimTime = 0.812f;
            float animTimeMultiplier = _animTime / _castDeley - multiplierAnimTime;
            _player.Animator.speed = animTimeMultiplier;
        }

        yield return null;
    }

    private IEnumerator TimeCastForSlowMoveProjectile()
    {
        _castDeley = _fastTimeCast;

        if (_animTime > 0)
        {
            float multiplierAnimTime = 3.7f;
            float animTimeMultiplier = _animTime / _castDeley - multiplierAnimTime;

            _player.Animator.speed = animTimeMultiplier;
        }

        yield return null;
    }

    private void ChooseWhichProjectileCreate()
    {
        if (_isTarget)
        {
            _player.CharacterState.CmdAddState(States.PoisonCloud, _durationPoisonCloud, 0, _player.gameObject, Name);

            CmdCreateProjectileForTarget(_currentTarget.gameObject, _currentTarget.transform.position, 
                _poisonBallInfo.MaxCountProjectile, _multiplierForPushDistance, PoisonBoneStack,
                _isFast, _isPushTarget, IsAltAbility,
                _activeTalentsInfo.IsActiveHealingPoisonBall, _activeTalentsInfo.IsActiveWitheringPoison, _activeTalentsInfo.IsActiveVoluminousBall, _activeTalentsInfo.IsActiveBallEffect,
                _activeTalentsInfo.IsActiveInertialGlands, _activeTalentsInfo.IsActiveContinuationAmbush, 
                _poisonBallInfo.IsOriginalTargetEnemy, _poisonBallInfo.IsOriginalTargetPlayer, _poisonBallInfo.IsOriginalTargetAllies);

            CmdApplyPoisonCloud(_poisonBallInfo.IsHealingPoisonCloud, _durationPoisonCloud);
        }
        else
        {
            _player.CharacterState.CmdAddState(States.PoisonCloud, _durationPoisonCloud, 0, _player.gameObject, Name);

            CmdCreateProjectileForFlyingMaxDistance(_firstMousePosition, 
                _poisonBallInfo.MaxCountProjectile, _multiplierForPushDistance, PoisonBoneStack,
                _isFast, _isPushTarget, IsAltAbility,
                _activeTalentsInfo.IsActiveHealingPoisonBall, _activeTalentsInfo.IsActiveWitheringPoison, _activeTalentsInfo.IsActiveVoluminousBall, _activeTalentsInfo.IsActiveBallEffect,
                _activeTalentsInfo.IsActiveInertialGlands, _activeTalentsInfo.IsActiveContinuationAmbush,
                _poisonBallInfo.IsOriginalTargetEnemy, _poisonBallInfo.IsOriginalTargetPlayer, _poisonBallInfo.IsOriginalTargetAllies);

            CmdApplyPoisonCloud(_poisonBallInfo.IsHealingPoisonCloud, _durationPoisonCloud);
        }
    }

    #endregion

    #region Command Methods

    [Command]
    private void CmdCreateProjectileForTarget(GameObject target, Vector3 targetOrPoint, 
        int maxCountProjectiles, float multiplierForPushDistance, int poisonBoneStack,
        bool isFast, bool isPushTarget, bool isPlayerInvisible,
        bool isActiveHealingPoisonBall, bool isActiveWitheringPoison, bool isActiveVoluminousBall, bool isActiveBallEffect,
        bool isActiveInertialGlands, bool isActiveContinuationAmbush,
        bool isTargetEnemy, bool isTargetPlayer, bool isTargetAllies)

    {
        Debug.Log("CmdCreateProjTarget / IsActiveInertialGlands = " + isActiveInertialGlands);
        Debug.Log("CmdCreateProjTarget / IsActiveContinuationAmbush = " + isActiveContinuationAmbush);
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
        if (_poisonBallInfo.CountProjectiles >= 3 && isActiveInertialGlands)
        {
            _poisonBallInfo.IsThreeProjectileOnOnetarget = true;
            Debug.Log("CmdCreateProjTarget / IsThreeProjectileOnOneTarget = " + _poisonBallInfo.IsThreeProjectileOnOnetarget);
        }

        if (_poisonBallInfo.CountProjectiles >= 4 && isActiveContinuationAmbush)
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

        Vector3 spawnPosition = new Vector3(_spawnPointInfo.SpawnPointX, _spawnPointInfo.SpawnPointY, _spawnPointInfo.SpawnPointZ);

        GameObject item = Instantiate(_projectile.gameObject, spawnPosition, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, this, 
            multiplierForPushDistance, poisonBoneStack,
            isTargetPlayer, isTargetEnemy, isTargetAllies,
            isActiveHealingPoisonBall, isActiveWitheringPoison, isActiveVoluminousBall, isActiveBallEffect,
            isPushTarget, isPlayerInvisible
            );

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
    private void CmdCreateProjectileForFlyingMaxDistance(Vector3 point, 
        int maxCountProjectiles, float multiplierForPushDistance, int poisonBoneStack,
        bool isFast, bool isPushTarget, bool isPlayerInvisible,
        bool isActiveHealingPoisonBall, bool isActiveWitheringPoison, bool isActiveVoluminousBall, bool isActiveBallEffect,
        bool isActiveInertialGlands, bool isActiveContinuationAmbush,
        bool isTargetEnemy, bool isTargetPlayer, bool isTargetAllies)
    {
        Debug.Log("CmdCreateProjPoint / IsActiveInertialGlands = " + isActiveInertialGlands);
        Debug.Log("CmdCreateProjPoint / IsActiveContinuationAmbush = " + isActiveContinuationAmbush);
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

        if (_poisonBallInfo.CountProjectiles >= 3 && isActiveInertialGlands)
        {
            _poisonBallInfo.IsThreeProjectileOnOnetarget = true;
        }

        if (_poisonBallInfo.CountProjectiles >= 4 && isActiveContinuationAmbush)
        {
            _poisonBallInfo.IsCanApplyInvisible = true;
        }

        if (_poisonBallInfo.CountProjectiles < maxCountProjectiles && LastTarget == CurrentTarget)
        {
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
            _poisonBallInfo.IsActiveTimer = true;
        }

        //Debug.Log($"bool isTargetEnemy = {isTargetEnemy}, bool isTargetPlayer = {isTargetPlayer}, bool isTargetAllies = {isTargetAllies}");

        Vector3 spawnPosition = new Vector3(_spawnPointInfo.SpawnPointX, _spawnPointInfo.SpawnPointY, _spawnPointInfo.SpawnPointZ);

        GameObject item = Instantiate(_projectile.gameObject, spawnPosition, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, this,
            multiplierForPushDistance, poisonBoneStack,
            isTargetPlayer, isTargetEnemy, isTargetAllies,
            isActiveHealingPoisonBall, isActiveWitheringPoison, isActiveVoluminousBall, isActiveBallEffect,
            isPushTarget, isPlayerInvisible
            );

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
                //_player.CharacterState.AddState(States.PoisonCloud, duration, 0, _player.gameObject, Name);

                _poisonDamagingCloud = Instantiate(_poisonDamagingCloudPrefab, _player.transform.position, Quaternion.identity);

                _poisonDamagingCloudPrefab.PoisonDamageCloud = _poisonDamagingCloud;
                SceneManager.MoveGameObjectToScene(_poisonDamagingCloudPrefab.PoisonDamageCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonDamagingCloudPrefab.PoisonDamageCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
                _poisonDamagingCloudPrefab.PoisonDamageCloud.AddStack();

                NetworkServer.Spawn(_poisonDamagingCloud.gameObject);

                //Debug.Log("PoisonBall / CmdApplyPoisonCloud / if / _poisonDamagingCloud = " + _poisonDamagingCloud);
                //Debug.Log("PoisonBall / CmdApplyPoisonCloud / if / _poisonDamagingCloudPrefab.PoisonDamageCloud = " + _poisonDamagingCloudPrefab.PoisonDamageCloud);
            }
            else
            {
                //Debug.Log("PoisonBall / CmdApplyPoisonCloud / else / _poisonDamagingCloud = " + _poisonDamagingCloudPrefab.PoisonDamageCloud);
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

    /*
    [Command]
    private void CmdCheckActiveTalents(bool transparentPoisonsActive, bool witheringPoisonActive,
        bool contAmbushActive, bool healingBallActive, bool healingCloudActive,
        bool enlargedGlandsActive, bool voluminousBallActive, bool inertialGlandsActive, bool volatilityPoisonsActive, bool ballEffectActive)
    {
        _activeTalentsInfo.IsActiveTransparentPoisons = transparentPoisonsActive;
        _activeTalentsInfo.IsActiveWitheringPoison = witheringPoisonActive;
        _activeTalentsInfo.IsActiveContinuationAmbush = contAmbushActive;
        _activeTalentsInfo.IsActiveHealingPoisonBall = healingBallActive;
        _activeTalentsInfo.IsActiveHealingPoisonCloud = healingCloudActive;
        _activeTalentsInfo.IsActiveEnlargedGlands = enlargedGlandsActive;
        _activeTalentsInfo.IsActiveVoluminousBall = voluminousBallActive;
        _activeTalentsInfo.IsActiveInertialGlands = inertialGlandsActive;
        _activeTalentsInfo.IsActiveVolatilityOfPoisons = volatilityPoisonsActive;
        _activeTalentsInfo.IsActiveBallEffect = ballEffectActive;

        RpcCheckActiveTalents(transparentPoisonsActive, witheringPoisonActive, contAmbushActive, healingBallActive, healingCloudActive, 
            enlargedGlandsActive, voluminousBallActive, inertialGlandsActive, volatilityPoisonsActive, ballEffectActive);
    }
    */

    [Command]
    private void SetSpawnPointPosition(float spawnPointX, float spawnPointY, float spawnPointZ)
    {
        _spawnPointInfo.SpawnPointX = spawnPointX;
        _spawnPointInfo.SpawnPointY = spawnPointY;
        _spawnPointInfo.SpawnPointZ = spawnPointZ;
    }

    #endregion

    /*
    [ClientRpc]
    private void RpcCheckActiveTalents(bool transparentPoisonsActive, bool witheringPoisonActive,
    bool contAmbushActive, bool healingBallActive, bool healingCloudActive,
    bool enlargedGlandsActive, bool voluminousBallActive, bool inertialGlandsActive, bool volatilityPoisonsActive, bool ballEffectActive)
    {
        _activeTalentsInfo.IsActiveTransparentPoisons = transparentPoisonsActive;
        _activeTalentsInfo.IsActiveWitheringPoison = witheringPoisonActive;
        _activeTalentsInfo.IsActiveContinuationAmbush = contAmbushActive;
        _activeTalentsInfo.IsActiveHealingPoisonBall = healingBallActive;
        _activeTalentsInfo.IsActiveHealingPoisonCloud = healingCloudActive;
        _activeTalentsInfo.IsActiveEnlargedGlands = enlargedGlandsActive;
        _activeTalentsInfo.IsActiveVoluminousBall = voluminousBallActive;
        _activeTalentsInfo.IsActiveInertialGlands = inertialGlandsActive;
        _activeTalentsInfo.IsActiveVolatilityOfPoisons = volatilityPoisonsActive;
        _activeTalentsInfo.IsActiveBallEffect = ballEffectActive;
    }
    */

    [ClientRpc]
    private void RpcApply(PoisonDamagingCloudPrefab poisonDamagingCloud, PoisonHealingCloudPrefab poisonHealingCloud, float duration, bool isHealingCloud)
    {
        //Debug.Log("PoisonBall / RpcApply / if (poisonDamagingCloud != null) = " + poisonDamagingCloud);
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