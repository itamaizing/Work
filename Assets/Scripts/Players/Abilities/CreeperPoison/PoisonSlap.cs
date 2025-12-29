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
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private LightningStrikes _lightningStrikes;
    [SerializeField] private LightningMovement _lightningMovement;
    //[SerializeField] private GameObject _poisonBallObject;
    [SerializeField] private SkillManager _skillManager;

    [Header("Talents")]
    [SerializeField] private RestorationOfGlands _restorationOfGlands;
    [SerializeField] private LightningFastPoisonSlap _lightningFastPoisonSlap;
    [SerializeField] private LightweightSlap _lightweightSlap;
    [SerializeField] private PoisonSlapTalent _poisonSlapTalent;

    #region DisplayArrow

    [SerializeField] private GameObject _arrowPrefab;

    private GameObject[] _arrowRenderers = new GameObject[2];

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

    private void Update()
    {
        UpdateMouseDetection();
    }

    public void PoisonSlapPreparation()
    {
        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;
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
        StopAutoDraw();
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) SetTarget((Character)targetInfo.GetTargets()[0]);
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
        Hero.Move.StopLookAt();
        Hero.Move.CanMove = true;

        ClearTarget();
        ClearTempTarget();

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

        while (GetTempTargetCharacter() == null)
        {
            if (GetMouseButton)
            {
                FindTargetCharacter(_radiusTargetSearch, GetMousePoint());

                if (GetTempTargetCharacter() != null)
                {
                    if (IsAllyTarget(GetTempTargetCharacter()) || GetTempTargetCharacter() == Hero) ClearTempTarget();

                    _firstMousePosition = GetMousePoint();
                    CreateArrowsParallelToPlayer(GetTempTargetCharacter());
                    StopAutoDraw();
                    _firstClickDone = true;

                }
            }

            yield return null;
        }

        yield return _secondMouseClickCoroutine = StartCoroutine(SecondClick());
        SetTarget(GetTempTargetCharacter());

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(GetTargetCharacter());
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_isUsedPoisonBallCharger)
        {
            _poisonBall.PayCostPoisonBall();
        }

        ChooseDirectionPush(GetTargetCharacter());

        DamageDeal(GetTargetCharacter());

        yield return null;
    }

    private void SwitchPayCost()
    {
        var last = _skillManager?.LastCastedSkill;
        var preview = _skillManager?.PreviewCastedSkill;

        bool isDoubleCreeper = last == _creeperStrike && preview == _creeperStrike;
        bool isDoubleLightning = last == _lightningStrikes && preview == _lightningStrikes;

        if (isDoubleCreeper)
        {
            CastSpeedFromCreeperStrike();
            _isUsedPoisonBallCharger = false;
        }
        else if (isDoubleLightning)
        {
            CastSpeedFromLightningStrikes();
            _isUsedPoisonBallCharger = false;
        }

        else
        {
            _isUsedPoisonBallCharger = true;
            Buff.CastSpeed.Reset();
        }
    }
    #endregion

    #region CalculationsDistances

    private bool CheckCanCast()
    {
        if (GetTargetCharacter() == null)
            return false;

        return Vector3.Distance(_player.transform.position, GetTargetCharacter().transform.position) <= Radius;
    }

    private void ChooseDirectionPush(Character target)
    {
        _isPushTargetAllowed = Vector3.Distance(_player.transform.position, _secondMousePosition) > Vector3.Distance(_player.transform.position, target.transform.position);
    }

    #endregion

    #region ArrowManagement

    private void CreateArrowsParallelToPlayer(Character target)
    {
        if (target == null || _arrowPrefab == null)
        {
            Debug.LogError("Arrow Prefab is not assigned or Target is null");
            return;
        }

        Vector3 targetPosition = target.transform.position;
        Vector3 playerPosition = _player.transform.position;

        targetPosition.y = playerPosition.y = 1.1f;

        Vector3 directionToTarget = (targetPosition - playerPosition).normalized;

        Vector3[] spawnPositions = new Vector3[2]
       {
        targetPosition + directionToTarget * 0.5f,
        targetPosition - directionToTarget * 0.5f
       };

        Quaternion[] rotations = new Quaternion[2]
      {
        Quaternion.LookRotation(playerPosition - spawnPositions[0]),
        Quaternion.LookRotation(spawnPositions[1] - playerPosition),
      };

        for (int i = 0; i < _arrowRenderers.Length; i++)
        {
            _arrowRenderers[i] = Instantiate(_arrowPrefab, spawnPositions[i], rotations[i]);
            RotateArrowChild(_arrowRenderers[i], -90);
            _arrowRenderers[i]?.SetActive(false);
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
    }

    private void SetArrowVisibility(int arrowIndex, bool isVisible)
    {
        if (arrowIndex >= 0 && arrowIndex < _arrowRenderers.Length && _arrowRenderers[arrowIndex] != null)
        {
            _arrowRenderers[arrowIndex].SetActive(isVisible);
        }
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
                SetArrowVisibility(0, true);
                SetArrowVisibility(1, false);
            }
            else
            {
                SetArrowVisibility(1, true);
                SetArrowVisibility(0, false);
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

                if (GetTempTargetCharacter() != null)
                {
                    SetArrowVisibility(0, false);
                    SetArrowVisibility(1, false);
                    SwitchPayCost();
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

        if (GetTargetCharacter() != null)
        {
            Damage damage = new Damage
            {
                Value = _baseDamage,
                Type = DamageType.Physical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdApplyDamage(damage, GetTargetCharacter().gameObject);

            if (GetTargetCharacter().CharacterState.CheckForState(States.PoisonBone) && _restorationOfGlands && _poisonBoneStack > 0)
            {
                float baseChanceOfRestorationOfGlands = 0.1f;
                float chanceOfRestorationOfGlands = baseChanceOfRestorationOfGlands * _poisonBoneStack;

                if (Random.Range(0f, 1f) <= chanceOfRestorationOfGlands)
                {
                    _restorationOfGlands.ReductionCooldown();
                }
            }

            PushTarget(GetTargetCharacter(), _distancePush, _durationPush, _isPushTargetAllowed);
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
        Vector3 directionPush = (target.transform.position - transform.position);
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