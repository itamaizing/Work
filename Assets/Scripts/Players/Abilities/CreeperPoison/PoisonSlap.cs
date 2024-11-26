using Mirror;
using System.Collections;
using UnityEngine;

public class PoisonSlap : Skill
{
    #region Variables

    private bool _isCanDamageDeal = false;

    [SerializeField] private Character _player;

    [Header("Abilities")]

    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private LightningStrikes _lightningStrikes;
    [SerializeField] private LightningMovement _lightningMovement;

    [Header("Talents")]
    [SerializeField] private RestorationOfGlands _restorationOfGlands;
    [SerializeField] private LightningFastPoisonSlap _lightningFastPoisonSlap;
    [SerializeField] private LightweightSlap _lightweightSlap;
    [SerializeField] private PoisonSlapTalent _poisonSlapTalent;

    #region DisplayArrow

    [SerializeField] private GameObject _arrowPrefab;

    private GameObject[] _arrowRenderers = new GameObject[2]; 
    private bool _colorLockedAfterSecondClick = false;
    private bool _colorLockedAfterThirdClick = false;

    #endregion

    private Character _currentTarget;

    private Vector3 _firstMousePosition = Vector3.positiveInfinity;
    private Vector3 _secondMousePosition;

    private int _poisonBoneStack;

    private float _creeperStrikeCastSpeedMultiplier = 0.5f; // Decrease CastTime on 50%
    private float _lightningStrikesCastSpeedMultiplier = 0.0f;  // Decrease CastTime on 100%
    private float _baseTimeCast = 1.6f;
    private float _baseDamage = 30f;
    private float _distancePush = 3.0f;
    private float _durationPush = 1.0f;
    private float _minTimeCooldown = 0.2f;

    private Coroutine _secondMouseClickCoroutine;

    private bool _isPushTargetAllowed;
    private bool _firstClickDone = false;
    private bool _secondClickDone;
    private bool _isUsedPoisonBallCharger = true;
    private bool _isCanBreak = false;
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    public int PoisonBoneStack { get => _poisonBoneStack; set => _poisonBoneStack = value; }
    public bool IsCanDamageDeal { get => _isCanDamageDeal; }

    protected override bool IsCanCast => CheckCanCast();

    #endregion

    #region PrepareAndStartJob

    private void Update()
    {
        UpdateMouseDetection();
    }

    public void AnimPoisonSlapCast()
    {

    }

    public void AnimPoisonSlapCastEnded()
    {

    }

    public void UsePoisonSlapOfLightningMovement()
    {
        if (RemainingCooldownTime > _minTimeCooldown)
            return;
        
        _currentTarget = _lightningMovement.Target;
        Debug.Log("PoisonSlap / UsePoisonSlapLightning / _currentTarget = " + _currentTarget);
        DamageDealOfLightningMovement();
    }

    protected override void ClearData()
    {
        ClearArrows();

        _firstMousePosition = Vector3.positiveInfinity;
        _secondMousePosition = Vector3.zero;

        _firstClickDone = false;
        _secondClickDone = false;
        _isPushTargetAllowed = false;
        _isUsedPoisonBallCharger = true;
        _isCanBreak = false;

        _currentTarget = null;
        _castDeley = 0;

        if (_secondMouseClickCoroutine != null)
        {
            StopCoroutine(_secondMouseClickCoroutine);
            _secondMouseClickCoroutine = null;
        }
    }

    protected override IEnumerator PrepareJob()
    {
        if (_lightningMovement.IsInMovement)
        {
            _isCanDamageDeal = true;
            Debug.Log("PoisonSlap / if inMovement / isCanDamageDeal = " + IsCanDamageDeal);
            yield break;
        }

        switch (_poisonSlapTalent.Data.IsOpen)
        {
            case true:
                if (_creeperStrike.IsTwoHit)
                {
                    CastSpeedFromCreeperStrike();
                    Debug.Log("PoisonSlap / ActiveTalent / creeperStrike TwoHit");
                    _isUsedPoisonBallCharger = false;
                }
                else if (_lightningStrikes.IsUsedLightningStrikes)
                {
                    CastSpeedFromLightningStrikes();
                    Debug.Log("PoisonSlap / ActiveTalent / LightningStrikes TwoHit");
                    _isUsedPoisonBallCharger = false;
                }
                else
                {
                    _isUsedPoisonBallCharger = true;
                    _castDeley = _baseTimeCast;
                }
                break;

            case false:
                if (_creeperStrike.IsTwoHit)
                {
                    CastSpeedFromCreeperStrike();
                }
                else if (_lightningStrikes.IsUsedLightningStrikes)
                {
                    CastSpeedFromLightningStrikes();
                }
                else
                {
                    _isUsedPoisonBallCharger = true;
                    _castDeley = _baseTimeCast;
                }
                break;
        }

        if (_poisonBall.IsHaveCharge == false && _isUsedPoisonBallCharger)
        {
            yield break;
        }
        else
        {
            while (_currentTarget == null)
            {
                if (GetMouseButton)
                {
                    _currentTarget = GetRaycastTarget();

                    if (_currentTarget != null)
                    {
                        _firstMousePosition = GetMousePoint();

                        CreateArrowsParallelToPlayer();

                        StopAutoDraw();

                        _firstClickDone = true;

                    }

                }
                yield return null;
            }

            yield return _secondMouseClickCoroutine = StartCoroutine(SecondClick());
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_isUsedPoisonBallCharger)
        {
            _poisonBall.PayCostPoisonBall();
        }

        ChooseDirectionPush(_currentTarget);

        DamageDeal(_currentTarget);

        yield return null;
    }

    #endregion

    #region CalculationsDistances

    private bool CheckCanCast()
    {
        if (_currentTarget == null)
            return false;

        return Vector3.Distance(_player.transform.position, _currentTarget.transform.position) <= Radius;
    }

    private void ChooseDirectionPush(Character target)
    {
        _isPushTargetAllowed = Vector3.Distance(_player.transform.position, _secondMousePosition) > Vector3.Distance(_player.transform.position, target.transform.position);
    }

    #endregion

    #region ArrowManagement

    private void CreateArrowsParallelToPlayer()
    {
        if (_currentTarget == null || _arrowPrefab == null)
        {
            Debug.LogError("Arrow Prefab is not assigned or Target is null");
            return;
        }

        Vector3 targetPosition = _currentTarget.transform.position;
        Vector3 playerPosition = _player.transform.position;

        Vector3 directionToTarget = (targetPosition - playerPosition).normalized;

        Vector3 perpendicularDirection = Vector3.Cross(directionToTarget, Vector3.forward).normalized;

        _arrowRenderers[0] = Instantiate(_arrowPrefab, targetPosition + directionToTarget, Quaternion.identity);
        _arrowRenderers[1] = Instantiate(_arrowPrefab, targetPosition - directionToTarget, Quaternion.identity);

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

        Debug.Log("Arrows cleared.");
    }

    #endregion

    #region Update Method for Mouse Movement Detection

    private void UpdateMouseDetection()
    {
        if (_firstClickDone && !_secondClickDone)
        {
            Vector3 currentMousePosition = GetMousePoint();
            if (currentMousePosition.x < _firstMousePosition.x && currentMousePosition.z < _firstMousePosition.z)
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
    }

    #endregion

    #region Coroutines

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

    private void CastSpeedFromCreeperStrike()
    {
        _creeperStrike.IsTwoHit = false;
        Debug.Log("PoisonSlap / CastSpeedFromCreeperStrike / IsTwoHit = " + _creeperStrike.IsTwoHit);

        float _timeCastFromCreeperStrike = _baseTimeCast * _creeperStrikeCastSpeedMultiplier;

        _castDeley = _timeCastFromCreeperStrike;
        Debug.Log("PoisonSlap / CastSpeedFromCreeperStrike / castDeley = " + _castDeley);
    }

    private void CastSpeedFromLightningStrikes()
    {
        float _timeCastFromLightningStrikes = _baseTimeCast * _lightningStrikesCastSpeedMultiplier;

        _castDeley = _timeCastFromLightningStrikes;
        Debug.Log("PoisonSlap / CastSpeedFromLightningStrikes / castDeley = " + _castDeley);
    }

    #endregion

    #region DamageDealAndPushTargetMethods

    private void DamageDeal(Character target)
    {
        if (target != null)
        {
            Damage damage = new Damage
            {
                Value = _baseDamage,
                Type = DamageType.Physical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdApplyDamage(damage, target.gameObject);

            if (target.CharacterState.CheckForState(States.PoisonBone) && _restorationOfGlands && _poisonBoneStack > 0)
            {
                float baseChanceOfRestorationOfGlands = 0.1f;
                float chanceOfRestorationOfGlands = baseChanceOfRestorationOfGlands * _poisonBoneStack;

                if (Random.Range(0f, 1f) <= chanceOfRestorationOfGlands)
                {
                    Debug.Log("CreeperStrike / restorationOfGlands");
                    _restorationOfGlands.ReductionCooldown();
                }
            }

            PushTarget(target, _distancePush, _durationPush, _isPushTargetAllowed);
        }
    }

    public void DamageDealOfLightningMovement()
    {
        if (_currentTarget != null)
        {
            Damage damage = new Damage
            {
                Value = _baseDamage,
                Type = DamageType.Physical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdApplyDamage(damage, _currentTarget.gameObject);

            if (_currentTarget.CharacterState.CheckForState(States.PoisonBone) && _restorationOfGlands && _poisonBoneStack > 0)
            {
                float baseChanceOfRestorationOfGlands = 0.1f;
                float chanceOfRestorationOfGlands = baseChanceOfRestorationOfGlands * _poisonBoneStack;

                if (Random.Range(0f, 1f) <= chanceOfRestorationOfGlands)
                {
                    _restorationOfGlands.ReductionCooldown();
                }
            }

            PushTarget(_currentTarget, _distancePush, _durationPush, _isPushTargetAllowed);
        }
        UseRecharge();
    }

    private void UseRecharge()
    {
        float baseCooldownTime = _cooldownTime;

        if (_lightweightSlap.Data.IsOpen)
        {
            _cooldownTime /= 2;
        }
        
        _isCanDamageDeal = false;
        TryPayCost(true);

        _cooldownTime = baseCooldownTime;
    }

    private void PushTarget(Character target, float distancePush, float durationPush, bool isCanPushTarget)
    {
       if (_lightningMovement.IsInMovement)
       {
            CmdPushEnemyInLightningMovement(target, distancePush, durationPush);
       }
       else
       {
            CmdPushEnemy(target, distancePush, durationPush, isCanPushTarget);
       }
    }

    #endregion

    #region CommandMethods

    [Command]
    private void CmdPushEnemy(Character target, float distancePush, float durationPush, bool isCanPushTarget) 
    {
        MoveComponent targetMoveComponent = target.GetComponent<MoveComponent>();

        Vector2 directionPush = (target.transform.position - transform.position);

        distancePush = ((distancePush * GlobalVariable.cellSize) * durationPush) / GlobalVariable.cellSize;
        if (isCanPushTarget)
        {
            targetMoveComponent.TargetRpcDoMove((Vector2)target.transform.position + directionPush * distancePush, durationPush);
        }
        else
        {
            targetMoveComponent.TargetRpcDoMove((Vector2)target.transform.position - directionPush * distancePush, durationPush);
        }
    }

    [Command]
    private void CmdPushEnemyInLightningMovement(Character target, float distancePush, float durationPush)
    {
        MoveComponent targetMoveComponent = target.GetComponent<MoveComponent>();

        Vector3 directionPush = (target.transform.position - transform.position).normalized;
        Vector3 perpendicularDirection;

        if (directionPush.x < 0)
        {
            perpendicularDirection = new Vector3(directionPush.y, -directionPush.x, 0).normalized;
        }
        else
        {
            perpendicularDirection = new Vector3(-directionPush.y, directionPush.x, 0).normalized;
        }

        distancePush = ((distancePush * GlobalVariable.cellSize) * durationPush) / GlobalVariable.cellSize;

        targetMoveComponent.TargetRpcDoMove(target.transform.position + perpendicularDirection * distancePush, durationPush);
    }


    #endregion
}
