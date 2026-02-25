using Mirror;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class PoisonSlap : Skill
{
    #region Variables

    private bool _isCanDamageDeal = false;

    [SerializeField] private Character _player;

    [Header("Abilities")]

    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private LightningMovement _lightningMovement;
    //[SerializeField] private GameObject _poisonBallObject;
    [SerializeField] private SkillManager _skillManager;

    [Header("Talents")]
    [SerializeField] private RestorationOfGlands _restorationOfGlands;
    [SerializeField] private LightningFastPoisonSlap _lightningFastPoisonSlap;
    [SerializeField] private LightweightSlap _lightweightSlap;
    [SerializeField] private PoisonSlapTalent _poisonSlapTalent;

    #region DisplayArrow

    [SerializeField] private ArrowRender _arrowPrefab;

    private GameObject _pointArrowInstance;

    private ArrowRender[] _arrowRenderers = new ArrowRender[2];

    #endregion

    private Vector3 _firstMousePosition = Vector3.positiveInfinity;
    private Vector3 _secondMousePosition;

    private int _poisonBoneStack;

    private float _creeperStrikeCastSpeedMultiplier = 1.5f;
    private float _lightningStrikesCastSpeedMultiplier = 2f;
    private float _baseDamage = 30f;
    private float _distancePush = 3.0f;
    private float _durationPush = 1.0f;

    private Coroutine _secondMouseClickCoroutine;

    private bool _isPushTargetAllowed;
    private bool _firstClickDone = false;
    private bool _secondClickDone;
    private bool _isUsedPoisonBallCharger = true;
    private float _radiusTargetSearch = 0.5f; 

    private static readonly int poisonSlapTrigger = Animator.StringToHash("PoisonSlapCastAnimTrigger");


    protected override int AnimTriggerCast => poisonSlapTrigger;
    protected override int AnimTriggerCastDelay => 0;
    public int PoisonBoneStack { get => _poisonBoneStack; set => _poisonBoneStack = value; }
    public bool IsCanDamageDeal { get => _isCanDamageDeal; set => _isCanDamageDeal = value; }

    protected override bool IsCanCast => CheckCanCast();
    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public event System.Action OnPoisonSlapEnd;

    #endregion

    #region PrepareAndStartJob

    private void OnDisable()
    {
        OnSkillCanceled -= ClearData;
    }

    private void OnEnable()
    {
        OnSkillCanceled += ClearData;
    }

    private void Update()
    {
        UpdateMouseDetection();
    }

    public void PoisonSlapPreparation()
    {
        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.SetCanMove(false);
    }

    public void AnimPoisonSlapCast()
    {
        AnimStartCastCoroutine();
    }

    public void AnimPoisonSlapCastEnded()
    {
        AnimCastEnded();
    }

    public void UsePoisonSlapOfLightningMovement()
    {
        DamageDealOfLightningMovement();
    }

    public void ClearDataPoisonSlap()
    {
        ClearData();
        Renderer.HideSmartIndicator();
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
        SwitchPayCost();
    }

    protected override void ClearData()
    {
        ClearArrows();
        Buff.CastSpeed.Reset();
        Buff.AttackSpeed.Reset();

        _firstMousePosition = Vector3.positiveInfinity;
        _secondMousePosition = Vector3.zero;

        _firstClickDone = false;
        _secondClickDone = false;
        _isPushTargetAllowed = false;
        _isUsedPoisonBallCharger = true;
        Hero.Move.StopLookAt();
        Hero.Move.SetCanMove(true);

        Targeting.ClearTarget();
        Targeting.ClearTempTarget();

        _castDeley = 0;

        if (_secondMouseClickCoroutine != null)
        {
            StopCoroutine(_secondMouseClickCoroutine);
            _secondMouseClickCoroutine = null;
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        //if (_lightningMovement.IsInMovement)
        //{
        //    _isCanDamageDeal = true;
        //    yield break;
        //}

        //if (_poisonBall.IsHaveCharge == false && _isUsedPoisonBallCharger)
        //{
        //    yield break;
        //}

        while (Targeting.GetTempTarget().Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), _radiusTargetSearch);

                if (Targeting.GetTempTarget().Character != null)
                {
                    if (IsAllyTarget(Targeting.GetTempTarget().Character) || Targeting.GetTempTarget().Character == Hero) Targeting.ClearTempTarget();

                    _firstMousePosition = Targeting.GetMousePoint();
                    CreateArrowsParallelToPlayer(Targeting.GetTempTarget().Character);
                    Renderer.HideSmartIndicator();
                    _firstClickDone = true;

                }
            }

            yield return null;
        }

        yield return _secondMouseClickCoroutine = StartCoroutine(SecondClick());
        Targeting.SetTarget(Targeting.GetTempTarget().Character);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget().Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_isUsedPoisonBallCharger)
        {
            _poisonBall.PayCostPoisonBall();
        }

        ChooseDirectionPush(Targeting.GetTarget().Character);
        DamageDeal(Targeting.GetTarget().Character);

        yield return null;
    }

    private void SwitchPayCost()
    {
        var last = _skillManager?.LastCastedSkill;
        var preview = _skillManager?.PreviewCastedSkill;

        bool isDoubleCreeper = last is CreeperStrike && preview is CreeperStrike;
        bool isLightning = last is LightningStrikes;

        if (isDoubleCreeper)
        {
            CastSpeedFromCreeperStrike();
            _isUsedPoisonBallCharger = false;
        }
        else if (isLightning)
        {
            CastSpeedFromLightningStrikes();
            _isUsedPoisonBallCharger = false;
        }

        else
        {
            _isUsedPoisonBallCharger = true;
        }
    }
    #endregion

    #region CalculationsDistances

    private bool CheckCanCast()
    {
        if (Targeting.GetTarget().Character == null)
            return false;

        return Vector3.Distance(_player.transform.position, Targeting.GetTarget().Character.transform.position) <= AreaInfo.Radius;
    }

    private void ChooseDirectionPush(Character target)
    {
        float distanceFromPlayerToClick = Vector3.Distance(_player.transform.position, _secondMousePosition);
        float distanceFromTargetToClick = Vector3.Distance(target.transform.position, _secondMousePosition);
        float playerToTarget = Vector3.Distance(_player.transform.position, target.transform.position);

        _isPushTargetAllowed = distanceFromPlayerToClick > distanceFromTargetToClick;

        if (playerToTarget > distanceFromPlayerToClick && playerToTarget > distanceFromTargetToClick) _isPushTargetAllowed = false;
    }

    #endregion

    #region ArrowManagement

    private void CreateArrowsParallelToPlayer(Character target)
    {
        if (target == null || _arrowPrefab == null) return;

        Vector3 center = target.transform.position;
        Vector3 playerPos = _player.transform.position;

        center.y = 1.1f;
        playerPos.y = 1.1f;

        Vector3 direction = (playerPos - center).normalized;

        _pointArrowInstance = new GameObject("ArrowCenter");
        _pointArrowInstance.transform.position = center;
        _pointArrowInstance.transform.rotation = Quaternion.LookRotation(direction);

        Vector3 offset = direction * 0.6f;

        Vector3[] spawnPositions = new Vector3[2]
        {
        center + offset,
        center - offset
        };

        Quaternion[] rotations = new Quaternion[2]
        {
        Quaternion.LookRotation(playerPos - spawnPositions[0]),
        Quaternion.LookRotation(spawnPositions[1] - playerPos)
        };

        for (int i = 0; i < _arrowRenderers.Length; i++)
        {
            Quaternion flippedRotation = rotations[i] * Quaternion.Euler(0, 180f, 0);
            _arrowRenderers[i] = Instantiate(_arrowPrefab, spawnPositions[i], flippedRotation, _pointArrowInstance.transform);
            RotateArrowChild(_arrowRenderers[i].gameObject, -90);
            _arrowRenderers[i].gameObject?.SetActive(true);
        }
    }


    private void RotateArrowChild(GameObject arrow, float zRotation)
    {
        if (arrow == null) return;

        Transform childArrow = arrow.transform.GetChild(0);
        float currentXRotation = childArrow.localEulerAngles.x;

        childArrow.localRotation = Quaternion.Euler(currentXRotation, 0, zRotation);
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

        if (_pointArrowInstance != null)
        {
            Destroy(_pointArrowInstance);
            _pointArrowInstance = null;
        }
    }

    private void SetArrowVisibility(int arrowIndex, bool isVisible)
    {
        if (arrowIndex >= 0 && arrowIndex < _arrowRenderers.Length && _arrowRenderers[arrowIndex] != null)
        {
            _arrowRenderers[arrowIndex].gameObject.SetActive(isVisible);
        }
    }

    #endregion

    #region Update Method for Mouse Movement Detection

    private void UpdateMouseDetection()
    {
        if (!_firstClickDone || Targeting.GetTempTarget().Character == null) return;

        Vector3 playerPos = _player.transform.position;
        Vector3 targetPos = Targeting.GetTempTarget().Character.transform.position;
        Vector3 mousePos = Targeting.GetMousePoint();

        float playerToClick = Vector3.Distance(playerPos, mousePos);
        float targetToClick = Vector3.Distance(targetPos, mousePos);
        float playerToTarget = Vector3.Distance(playerPos, targetPos);

        bool showPushAway = playerToClick > targetToClick;

        if (!_secondClickDone) if (playerToTarget > playerToClick && playerToTarget > targetToClick) showPushAway = false;

        if (_pointArrowInstance != null)
        {
            Vector3 direction = playerPos - _pointArrowInstance.transform.position;
            direction.y = 0;
            if (direction != Vector3.zero) _pointArrowInstance.transform.rotation = Quaternion.LookRotation(direction);
        }

        SetArrowMaterial(_arrowRenderers[0], !showPushAway);
        SetArrowMaterial(_arrowRenderers[1], showPushAway);
    }

    private void SetArrowMaterial(ArrowRender arrow, bool isActive)
    {
        if (arrow == null) return;

        if (isActive) arrow.SetDeafaultMaterail();
        else arrow.SetTransparentMaterial();
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
                _secondMousePosition = Targeting.GetMousePoint();

                if (Targeting.GetTempTarget().Character != null)
                {
                    SetArrowVisibility(0, false);
                    SetArrowVisibility(1, false);
                }
            }
            yield return null;
        }
    }

    private void CastSpeedFromCreeperStrike()
    {
        Buff.AttackSpeed.ReductionPercentage(_creeperStrikeCastSpeedMultiplier);
        Buff.CastSpeed.IncreasePercentage(_creeperStrikeCastSpeedMultiplier);
    }

    private void CastSpeedFromLightningStrikes()
    {
        Buff.AttackSpeed.ReductionPercentage(_lightningStrikesCastSpeedMultiplier);
        Buff.CastSpeed.IncreasePercentage(_lightningStrikesCastSpeedMultiplier);
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
                    _restorationOfGlands.ReductionCooldown();
                }
            }

            PushTarget(target, _distancePush, _durationPush, _isPushTargetAllowed);
        }

        OnPoisonSlapEnd?.Invoke();
    }

    public void DamageDealOfLightningMovement()
    {
        if (_isUsedPoisonBallCharger)
        {
            _poisonBall.PayCostPoisonBall();
        }

        if (Targeting.GetTarget().Character != null)
        {
            Damage damage = new Damage
            {
                Value = _baseDamage,
                Type = DamageType.Physical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdApplyDamage(damage, Targeting.GetTarget().Character.gameObject);

            if (Targeting.GetTarget().Character.CharacterState.CheckForState(States.PoisonBone) && _restorationOfGlands && _poisonBoneStack > 0)
            {
                float baseChanceOfRestorationOfGlands = 0.1f;
                float chanceOfRestorationOfGlands = baseChanceOfRestorationOfGlands * _poisonBoneStack;

                if (Random.Range(0f, 1f) <= chanceOfRestorationOfGlands)
                {
                    _restorationOfGlands.ReductionCooldown();
                }
            }

            PushTarget(Targeting.GetTarget().Character, _distancePush, _durationPush, _isPushTargetAllowed);
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
        Vector3 directionPush = (target.transform.position - transform.position).normalized;
        directionPush.y = 0f;

        if (targetMoveComponent.connectionToClient != null)
        {
            if (isCanPushTarget) targetMoveComponent.TargetRpcDoMove(target.transform.position + directionPush * distancePush, durationPush);
            else targetMoveComponent.TargetRpcDoMove(target.transform.position - directionPush * distancePush, durationPush);
        }

        else
        {
            if (isCanPushTarget) targetMoveComponent.RpcDoMove(target.transform.position + directionPush * distancePush, durationPush);
            else targetMoveComponent.RpcDoMove(target.transform.position - directionPush * distancePush, durationPush);
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

        if (targetMoveComponent.connectionToClient != null) targetMoveComponent.TargetRpcDoMove(target.transform.position + perpendicularDirection * distancePush, durationPush);
        else targetMoveComponent.RpcDoMove(target.transform.position + perpendicularDirection * distancePush, durationPush);
    }


    #endregion
}
