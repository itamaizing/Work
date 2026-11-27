using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
}

public struct PoisonBallActiveTalentsInfo : NetworkMessage
{
    public bool IsActiveFootInstincts;
    public bool IsActiveRestorationOfGlands;
    public bool IsActiveTransparentPoisons;
    public bool IsActiveWitheringPoison;
    public bool IsActiveContinuationAmbush;
    public bool IsActiveEnlargedGlands;
    public bool IsActiveVoluminousBall;
    public bool IsActiveInertialGlands;
    public bool IsActiveVolatilityOfPoisons;
}

public struct PoisonBallSpawnPointInfo : NetworkMessage
{
    public float SpawnPointX;
    public float SpawnPointY;
    public float SpawnPointZ;
}

public class PoisonBall : Skill, IAltAbility, IColdBloodEmpowerable
{
    #region Variables

    [Header("Ability properties")]
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private PoisonBallProjectile _projectile;
    [SerializeField] private Character _player;
    [SerializeField] private ArrowRender _arrowPrefab;
    [SerializeField] private GameObject _spawnPoint;
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;
    [SerializeField] private ColdBlood _coldBlood;

    #region PoisonCloud
    private float _durationPoisonCloud = 6f;
    #endregion

    private PoisonBallInfo _poisonBallInfo = new PoisonBallInfo();
    private PoisonBallSpawnPointInfo _spawnPointInfo = new PoisonBallSpawnPointInfo();
    private PoisonBallActiveTalentsInfo _activeTalentsInfo = new PoisonBallActiveTalentsInfo();

    private const int PushArrowIndex = 0;
    private const int SpeedArrowIndex = 1;
    private ArrowRender[] _arrowRenderers = new ArrowRender[2];
    private GameObject _pointArrowInstance;

    private Vector3 _firstMousePosition = Vector3.positiveInfinity;
    private Vector3 _secondMousePosition;
    private Vector3 _thirdMousePosition;

    private Vector3 _activeCastPoint = Vector3.positiveInfinity;
    private Character _activeCastTargetCharacter;
    private bool _activeCastIsFast;
    private bool _activeCastIsPushTarget;

    private int _poisonBoneStacks = 0;

    [Header("Poison ball flight and cast settings")]
    [SerializeField, Min(0.01f)] private float _fastMovementSpeed = 18f;
    [SerializeField, Min(0.01f)] private float _slowMovementSpeed = 4f;
    [SerializeField, Min(0f)] private float _fastTimeCast = 0.4f;
    [SerializeField, Min(0f)] private float _slowTimeCast = 1.8f;
    private float _originalChargeCooldown;
    private float _baseCastWidth;
    private float _multiplierForPushDistance;
    private float _animTime;
    private float _baseMultiplierAnimationSpeed = 1f;
    private float _radiusFindTarget = 0.5f;

    private double _serverLastCastTime = -999;

    #region BoolVariables

    private bool _isCanCheckActiveTalents = true;
    private bool _isCanCheckTimerActive = true;
    private bool _isCanApplyInvisible;

    private bool _firstClickDone;
    private bool _secondClickDone;
    private bool _thirdClickDone;

    private bool _isTarget;
    private bool _isPushTarget;

    private bool _isFast;
    private bool _isBallCanBigger;
    private bool _isThreeProjectileOnOneTarget;

    private bool _isAbilityActive;

    #endregion

    private Coroutine _mouseDetectionCoroutine;
    private Coroutine _checkingTalentsCoroutine;
    private Coroutine _checkTimerActiveCoroutine;

    public GameObject LastTarget { get; set; }
    public GameObject CurrentTarget { get; set; }
    public FootInstincts FootInstinctsTalent { get; set; }
    public RestorationOfGlands RestorationOfGlandsTalent { get; set; }
    public int CurrentCountBall { get => _poisonBallInfo.CountProjectiles; }
    public int PoisonBoneStack { get => _poisonBoneStacks; set => _poisonBoneStacks = value; }
    public bool IsAltAbility { get; set; }
    
    private Vector3 _firstClickPlayerPos;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerPrepare => Animator.StringToHash("PoisonBallCastDelayAnimTrigger");
    public override float PreparingDuration
    {
        get => base.PreparingDuration / Mathf.Max(0.1f, GetCastSpeed());
        set => base.PreparingDuration = value;
    }
    //protected override bool IsCanCast => CheckCanCast();

    public event Action ResetAbilityParameters;
    public event Action AbilityChange;

    #region Talent

    private bool _isCanSpawnPoisonCloud = false;
    private bool _isActiveBallEffect = false;
    private bool _isIncreasingPoisonBallCharges = false;
    private bool _isPoisonCloudAddPoisonBone = false;
    private bool _isHealingPoisonBall = false;

    private bool _isBonusChargeApplied = false;

    public bool IsPoisonCloudAddPoisonBone { get => _isPoisonCloudAddPoisonBone; set => _isPoisonCloudAddPoisonBone = value; }

    public void HealingPoisonBall(bool value)
    {
        _isHealingPoisonBall = value;
        if (value == true)
        {
            Targeting.Faction = (TargetFaction.Ally | TargetFaction.Self | TargetFaction.Enemy);
        }
        else
        {
            Targeting.Faction = TargetFaction.Enemy;
        }
    }

    public void IncreasingPoisonBallCharges(bool value)
    {
        _isIncreasingPoisonBallCharges = value;
        
        if (value == _isBonusChargeApplied) return;
        if (!Charges.UsesCharges) return;

        Charges.ModifyMax(value ? 1 : -1);
        _isBonusChargeApplied = value;

        _poisonBallInfo.MaxCountProjectile = Charges.MaxCharges;
    }

    public void ActiveBallEffect(bool value)
    {
        _isActiveBallEffect = value;
    }

    public void SetPoisonCloudEnabled(bool value)
    {
        if(value == _isCanSpawnPoisonCloud) return;
        
        _isCanSpawnPoisonCloud = value;
    }

    public void PoisonCloudAddPoisonBone(bool value)
    {
        if(value == _isPoisonCloudAddPoisonBone) return;
        
        _isPoisonCloudAddPoisonBone = value;
    }

    private bool IsTransparentShot =>
        _player.Abilities.GetSkill<CreeperInvisible>()?.IsTransparentPoisonsActive ?? false;

    #endregion

    #endregion

    public void PayCostPoisonBall(TargetData target)
    {
        CheckWhoTarget(target);
        UseCooldownOrCharges();
    }

    protected override void UseCooldownOrCharges()
    {
        if (_isHealingPoisonBall && (_poisonBallInfo.IsOriginalTargetAllies || _poisonBallInfo.IsOriginalTargetPlayer))
        {
            Charges.TryUse(Charges.BaseCooldown / 2);
            return;
        }
        Charges.TryUse();
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        Charges.AffectedByCooldownReduction = true;
        _baseCastWidth = AreaInfo.CastWidth;
        _originalChargeCooldown = Charges.CooldownTime;

        _poisonBallInfo.StartTimeBetweenAttack = 15.0f;
        _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        _poisonBallInfo.MaxCountProjectile = Charges.MaxCharges;
    }

    private void OnDisable()
    {
        OnSkillCanceled -= ClearData;
    }

    private void OnEnable()
    {
        OnSkillCanceled += ClearData;
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

    private void PostPrepearClear()
    {
        ClearArrows();
        SkillRender.ClearFixedLookPoint();
        _firstMousePosition = Vector3.positiveInfinity;
        _secondMousePosition = Vector3.positiveInfinity;
        _thirdMousePosition = Vector3.positiveInfinity;

        _secondClickDone = false;
        _thirdClickDone = false;
        _firstClickDone = false;

        CancelCoroutine();
    }

    protected override void ClearData()
    {
        ResetAnimatorTriggers();

        if (!IsTargeting)
        {
            Targeting.ClearTarget();
            Targeting.ClearTempTarget();
            PostPrepearClear();
        }

        _isAbilityActive = false;
        base.ClearData();
    }

    private void ResetAnimatorTriggers()
    {
        if (_player != null && _player.Animator != null)
        {
            _player.Animator.ResetTrigger(AnimTriggerPrepare);
            _player.Animator.SetFloat("PoisonBallMultiplierSpeedAnimation", _baseMultiplierAnimationSpeed);
            _player.Animator.SetFloat("CastSpeed", 1f);
        }
    }

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
    {
        _isAbilityActive = true;
        Vector3 targetPoint = Vector3.positiveInfinity;

        StartCoroutine();

        while (Targeting.GetTempTarget()?.Character == null && float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton)
            {
                Vector3 click = Targeting.GetMousePoint();

                Targeting.FindTempTarget(click, _radiusFindTarget, true);
                //CheckWhoTarget(Targeting.GetTempTarget());

                Character tempTarget = Targeting.GetTempTarget()?.Character;
                _isTarget = tempTarget != null;

                Vector3 originPoint = _isTarget ? tempTarget.transform.position : click;
                _firstMousePosition = originPoint;
                _firstClickPlayerPos = _player.transform.position;

                if (_arrowRenderers[0] == null)
                {
                    CreateCastArrows();
                    StartMouseDetectionIfNeeded();
                }

                Vector3 clampedPoint = originPoint;
                if (Vector3.Distance(_player.transform.position, originPoint) > AreaInfo.CastLength)
                {
                    Vector3 dir = (originPoint - _player.transform.position).normalized;
                    clampedPoint = _player.transform.position + dir * AreaInfo.CastLength;
                }

                targetPoint = clampedPoint;
                SkillRender.SetFixedLookPoint(targetPoint);

                _firstClickDone = true;
                UpdateCastArrows(click);
            }

            //CooldownChange();
            yield return null;
        }

        _animTime = GetAnimationClipLength();

        while (!_secondClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 click = Targeting.GetMousePoint();
                _secondMousePosition = click;
                _secondClickDone = true;

                UpdateCastArrows(click);
            }

            yield return null;
        }

        while (!_thirdClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 click = Targeting.GetMousePoint();
                _thirdClickDone = true;
                _thirdMousePosition = click;

                SkillRender.StopDrawLine();
            }

            yield return null;
        }

        Vector3 secondClickPoint = _secondMousePosition;
        Vector3 thirdClickPoint = _thirdMousePosition;
        Vector3 rawFirstClickPoint = _firstMousePosition;
        Vector3 firstClickPlayerPosSnapshot = _firstClickPlayerPos;

        PostPrepearClear();

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(targetPoint);
        targetInfo.Points.Add(secondClickPoint);
        targetInfo.Points.Add(thirdClickPoint);
        targetInfo.Points.Add(rawFirstClickPoint);
        targetInfo.Points.Add(firstClickPlayerPosSnapshot);
        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        SetSpawnPointPosition(_player.transform.position.x, _player.transform.position.y, _player.transform.position.z);
        ChooseWhichProjectileCreate(Targeting.GetTarget().Character);

        ResetAbilityParameters?.Invoke();

        _player.Move.StopLookAt();

        ResetAnimatorTriggers();

        yield return null;
    }

    private void StartCoroutine()
    {
        if (_mouseDetectionCoroutine == null)
        {
            _mouseDetectionCoroutine = StartCoroutine(UpdateMouseDetectionJob());
        }

        if (_checkTimerActiveCoroutine == null)
        {
            _checkTimerActiveCoroutine = StartCoroutine(CheckTimerActiveJob());
        }
    }

    private void CancelCoroutine()
    {
        if (_mouseDetectionCoroutine != null)
        {
            StopCoroutine(_mouseDetectionCoroutine);
            _mouseDetectionCoroutine = null;
        }
    }

    private IEnumerator CheckTimerActiveJob()
    {
        while (_isCanCheckTimerActive)
        {
            if (_poisonBallInfo.IsActiveTimer)
            {
                Timer();
            }

            yield return null;
        }
    }

    #endregion

    #region CheckingMethods

    private void CheckWhoTarget(TargetData target)
    {
        if (target?.Character != null)
        {
            var targetGO = target.Character.gameObject;

            _poisonBallInfo.IsOriginalTargetPlayer = targetGO == _player.gameObject;
            int ownerTeam = _player.NetworkSettings.TeamIndex;
            int targetTeam = target.Character.NetworkSettings.TeamIndex;
            bool isAlly = ownerTeam != 0 && targetTeam != 0
                ? ownerTeam == targetTeam
                : targetGO.layer == LayerMask.NameToLayer("Allies");
            _poisonBallInfo.IsOriginalTargetAllies = !_poisonBallInfo.IsOriginalTargetPlayer && isAlly;
            _poisonBallInfo.IsOriginalTargetEnemy = !_poisonBallInfo.IsOriginalTargetPlayer && !isAlly;
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
            _poisonBallInfo.IsThreeProjectileOnOnetarget = false;
            _poisonBallInfo.IsCanApplyInvisible = false;
        }
    }

    private void CooldownChange()
    {
        if (_isHealingPoisonBall && (_poisonBallInfo.IsOriginalTargetAllies || _poisonBallInfo.IsOriginalTargetPlayer))
        {
            //_chargeCooldown = Charges.CooldownTime / 2;
            _skillAttributes[SkillAttributeName.Cooldown].AddModifier(new(0.5f, ModifierType.Multiplier, source: this));
        }
        else
        {
            _skillAttributes[SkillAttributeName.Cooldown].RemoveBySource(this);
            //_chargeCooldown = Charges.CooldownTime;
        }
    }

    #endregion

    #region ChooseMoveSpeedProjectile

    private void ChooseWhichProjectileCreate(Character character)
    {
        bool transparent = IsTransparentShot;
        bool coldBloodCrit = _coldBlood != null && _coldBlood.TryConsumeForPoison(character);
        bool healCloud = _isHealingPoisonBall &&
                         (_poisonBallInfo.IsOriginalTargetAllies || _poisonBallInfo.IsOriginalTargetPlayer);
        if (_isCanSpawnPoisonCloud) CmdApplyPoisonCloud(healCloud, _durationPoisonCloud);

        Debug.LogError($"is transparent {IsTransparentShot}");
        
        if (_isTarget)
        {
            CmdCreateProjectileForTarget(_activeCastTargetCharacter.gameObject, _activeCastTargetCharacter.transform.position,
                _poisonBallInfo.MaxCountProjectile, _multiplierForPushDistance, PoisonBoneStack,
                _activeCastIsFast, _activeCastIsPushTarget, IsAltAbility,
                _activeTalentsInfo.IsActiveFootInstincts, _activeTalentsInfo.IsActiveRestorationOfGlands,
                _isHealingPoisonBall, _activeTalentsInfo.IsActiveWitheringPoison, _activeTalentsInfo.IsActiveVoluminousBall, _isActiveBallEffect,
                _activeTalentsInfo.IsActiveInertialGlands, _activeTalentsInfo.IsActiveContinuationAmbush,
                _poisonBallInfo.IsOriginalTargetEnemy, _poisonBallInfo.IsOriginalTargetPlayer, _poisonBallInfo.IsOriginalTargetAllies, transparent, coldBloodCrit);

        }
        else
        {
            CmdCreateProjectileForFlyingMaxDistance(_activeCastPoint,
                _poisonBallInfo.MaxCountProjectile, _multiplierForPushDistance, PoisonBoneStack,
                _activeCastIsFast, _activeCastIsPushTarget, IsAltAbility,
                _activeTalentsInfo.IsActiveFootInstincts, _activeTalentsInfo.IsActiveRestorationOfGlands,
                _isHealingPoisonBall, _activeTalentsInfo.IsActiveWitheringPoison, _activeTalentsInfo.IsActiveVoluminousBall, _isActiveBallEffect,
                _activeTalentsInfo.IsActiveInertialGlands, _activeTalentsInfo.IsActiveContinuationAmbush,
                _poisonBallInfo.IsOriginalTargetEnemy, _poisonBallInfo.IsOriginalTargetPlayer, _poisonBallInfo.IsOriginalTargetAllies, transparent,coldBloodCrit);

        }
    }

    #endregion

    #region ArrowManagement

    private void StartMouseDetectionIfNeeded()
    {
        if (_mouseDetectionCoroutine != null)
        {
            StopCoroutine(_mouseDetectionCoroutine);
            _mouseDetectionCoroutine = null;
        }

        _mouseDetectionCoroutine = StartCoroutine(UpdateMouseDetectionJob());
    }

    private void CreateCastArrows()
    {
        if (_arrowPrefab == null) return;

        _pointArrowInstance = new GameObject("PoisonBallArrowCenter");

        for (int i = 0; i < _arrowRenderers.Length; i++)
        {
            _arrowRenderers[i] = Instantiate(_arrowPrefab, _pointArrowInstance.transform);
            RotateArrowChild(_arrowRenderers[i].gameObject, -90);
            _arrowRenderers[i].gameObject.SetActive(true);
        }
    }

    private void RotateArrowChild(GameObject arrow, float zRotation)
    {
        if (arrow == null) return;

        Transform childArrow = arrow.transform.GetChild(0);
        float currentXRotation = childArrow.localEulerAngles.x;

        childArrow.localRotation = Quaternion.Euler(currentXRotation, 0, zRotation);
    }

    private void UpdateCastArrows(Vector3 mousePosition)
    {
        Vector3 direction = GetCastDirection(_firstMousePosition, _firstClickPlayerPos);
        Vector3 pushPoint = _secondClickDone ? _secondMousePosition : mousePosition;
        bool isPush = IsPointAwayFromCaster(pushPoint, _firstMousePosition, _firstClickPlayerPos);
        bool isFast = _secondClickDone &&
            IsPointAwayFromCaster(mousePosition, _firstMousePosition, _firstClickPlayerPos);

        Vector3 pushDirection = isPush ? direction : -direction;
        Vector3 speedDirection = isFast ? direction : -direction;
        UpdateCastArrow(PushArrowIndex, _firstMousePosition + pushDirection * 0.6f,
            pushDirection, !_secondClickDone);
        UpdateCastArrow(SpeedArrowIndex, _player.transform.position + direction * 1.2f,
            speedDirection, _secondClickDone);
        if (_arrowRenderers[SpeedArrowIndex] != null)
            _arrowRenderers[SpeedArrowIndex].gameObject.SetActive(_secondClickDone);
    }

    private void UpdateCastArrow(int index, Vector3 position, Vector3 direction, bool isChoosing)
    {
        var arrow = _arrowRenderers[index];
        if (arrow == null) return;

        position.y = 1.1f;
        arrow.transform.SetPositionAndRotation(position,
            Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 180f, 0f));
        if (isChoosing) arrow.SetDeafaultMaterail();
        else arrow.SetTransparentMaterial();
    }

    private void ClearArrows()
    {
        foreach (var arrow in _arrowRenderers)
        {
            if (arrow != null)
            {
                Destroy(arrow.gameObject);
            }
        }

        if (_pointArrowInstance != null)
        {
            Destroy(_pointArrowInstance);
            _pointArrowInstance = null;
        }

        for (int i = 0; i < _arrowRenderers.Length; i++) _arrowRenderers[i] = null;
    }

    #endregion

    #region Update Method for Mouse Movement Detection

    private IEnumerator UpdateMouseDetectionJob()
    {
        while (_thirdClickDone == false)
        {
            if (_firstClickDone)
                UpdateCastArrows(Targeting.GetMousePoint());

            yield return null;
        }
    }

    #endregion

    #region Command Methods

    [Command]
    private void CmdCreateProjectileForTarget(GameObject target, Vector3 targetPosition,
        int maxCountProjectiles, float multiplierForPushDistance, int poisonBoneStack,
        bool isFast, bool isPushTarget, bool isPlayerInvisible,
        bool isActiveFootInstincts, bool isActiveRestorationOfGlands,
        bool isActiveHealingPoisonBall, bool isActiveWitheringPoison, bool isActiveVoluminousBall,
        bool isActiveBallEffect,
        bool isActiveInertialGlands, bool isActiveContinuationAmbush,
        bool isTargetEnemy, bool isTargetPlayer, bool isTargetAllies, bool isTransparentPoisons, bool isColdBloodCrit)
    {
        int ownerLayer = _player.gameObject.layer;

        bool comboExpired = NetworkTime.time - _serverLastCastTime > _poisonBallInfo.StartTimeBetweenAttack;
        _serverLastCastTime = NetworkTime.time;

        _poisonBallInfo.CountProjectiles = comboExpired ? 0 : _poisonBallInfo.CountProjectiles + 1;
        _poisonBallInfo.IsProjectileCreate = true;
        _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        _poisonBallInfo.IsActiveTimer = true;

        if (_poisonBallInfo.CountProjectiles < maxCountProjectiles && LastTarget == CurrentTarget)
        {
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
            _poisonBallInfo.IsActiveTimer = true;
        }

        Vector3 spawnPosition = new Vector3(_spawnPointInfo.SpawnPointX, _spawnPointInfo.SpawnPointY,
            _spawnPointInfo.SpawnPointZ);

        GameObject item = Instantiate(_projectile.gameObject, spawnPosition, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        poisonBallProjectile.Initialize(this, isTargetPlayer, isTargetEnemy, isTargetAllies,
            isPushTarget, isPlayerInvisible);
        poisonBallProjectile.ApplySettings(_fastMovementSpeed, _slowMovementSpeed, AreaInfo.CastLength);
        poisonBallProjectile.ApplyTalents(new PoisonBallProjectileTalents
        {
            PushDistanceBonus = multiplierForPushDistance,
            PoisonBoneStacks = poisonBoneStack,
            FootInstincts = isActiveFootInstincts,
            RestorationOfGlands = isActiveRestorationOfGlands,
            HealingPoisonBall = isActiveHealingPoisonBall,
            WitheringPoison = isActiveWitheringPoison,
            VoluminousBall = isActiveVoluminousBall,
            BallEffect = isActiveBallEffect,
            TransparentPoisons = isTransparentPoisons,
            ColdBloodCrit = isColdBloodCrit
        });

        poisonBallProjectile.MoveBallToTarget(targetPosition, isFast);

        NetworkServer.Spawn(item);

        poisonBallProjectile.RpcInitTransparent(isTransparentPoisons, ownerLayer);
    }

    [Command]
    private void CmdCreateProjectileForFlyingMaxDistance(Vector3 point,
        int maxCountProjectiles, float multiplierForPushDistance, int poisonBoneStack,
        bool isFast, bool isPushTarget, bool isPlayerInvisible,
        bool isActiveFootInstincts, bool isActiveRestorationOfGlands,
        bool isActiveHealingPoisonBall, bool isActiveWitheringPoison, bool isActiveVoluminousBall,
        bool isActiveBallEffect,
        bool isActiveInertialGlands, bool isActiveContinuationAmbush,
        bool isTargetEnemy, bool isTargetPlayer, bool isTargetAllies, bool isTransparentPoisons, bool isColdBloodCrit)
    {
        int ownerLayer = _player.gameObject.layer;

        bool comboExpired = NetworkTime.time - _serverLastCastTime > _poisonBallInfo.StartTimeBetweenAttack;
        _serverLastCastTime = NetworkTime.time;

        CurrentTarget = LastTarget;

        if (!comboExpired && LastTarget == CurrentTarget)
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
            RpcIsThreeProjectileOnOneTarget(_poisonBallInfo.IsThreeProjectileOnOnetarget);
        }

        if (_poisonBallInfo.CountProjectiles >= 4 && isActiveContinuationAmbush)
        {
            _poisonBallInfo.IsCanApplyInvisible = true;
            RpcIsCanApplyInvisible(_poisonBallInfo.IsCanApplyInvisible);
        }

        if (_poisonBallInfo.CountProjectiles < maxCountProjectiles && LastTarget == CurrentTarget)
        {
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
            _poisonBallInfo.IsActiveTimer = true;
        }

        Vector3 spawnPosition = new Vector3(_spawnPointInfo.SpawnPointX, _spawnPointInfo.SpawnPointY,
            _spawnPointInfo.SpawnPointZ);

        Vector3 direction = point - spawnPosition;
        direction.y = 0;
        direction = direction.normalized;

        Vector3 finalPoint = spawnPosition + direction * AreaInfo.CastLength;
        finalPoint.y = spawnPosition.y;

        GameObject item = Instantiate(_projectile.gameObject, spawnPosition, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>();

        poisonBallProjectile.Initialize(this, isTargetPlayer, isTargetEnemy, isTargetAllies,
            isPushTarget, isPlayerInvisible);
        poisonBallProjectile.ApplySettings(_fastMovementSpeed, _slowMovementSpeed, AreaInfo.CastLength);
        poisonBallProjectile.ApplyTalents(new PoisonBallProjectileTalents
        {
            PushDistanceBonus = multiplierForPushDistance,
            PoisonBoneStacks = poisonBoneStack,
            FootInstincts = isActiveFootInstincts,
            RestorationOfGlands = isActiveRestorationOfGlands,
            HealingPoisonBall = isActiveHealingPoisonBall,
            WitheringPoison = isActiveWitheringPoison,
            VoluminousBall = isActiveVoluminousBall,
            BallEffect = isActiveBallEffect,
            TransparentPoisons = isTransparentPoisons,
            ColdBloodCrit = isColdBloodCrit
        });

        poisonBallProjectile.MoveBallOnMaxDistance(finalPoint, isFast);

        NetworkServer.Spawn(item);

        poisonBallProjectile.RpcInitTransparent(isTransparentPoisons, ownerLayer);
    }
    
    [Command]
    private void CmdApplyPoisonCloud(bool isHealingCloud, float duration)
    {
        _creeperPoisonAura.ApplyCloud(isHealingCloud, duration, this);
    }

    [Command]
    private void SetSpawnPointPosition(float spawnPointX, float spawnPointY, float spawnPointZ)
    {
        _spawnPointInfo.SpawnPointX = spawnPointX;
        _spawnPointInfo.SpawnPointY = spawnPointY;
        _spawnPointInfo.SpawnPointZ = spawnPointZ;
    }

    #endregion

    [TargetRpc]
    private void RpcIsThreeProjectileOnOneTarget(bool isThreePorjectileOnOneTarget)
    {
        _isThreeProjectileOnOneTarget = isThreePorjectileOnOneTarget;
    }

    [TargetRpc]
    private void RpcIsCanApplyInvisible(bool isCanApplyInvisible)
    {
        _isCanApplyInvisible = isCanApplyInvisible;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _activeCastPoint = targetInfo.Points[0];

        Vector3 rawFirstPoint = targetInfo.Points.Count > 3 ? targetInfo.Points[3] : targetInfo.Points[0];
        Vector3 anchorPlayerPos = targetInfo.Points.Count > 4 ? targetInfo.Points[4] : _player.transform.position;
        _firstMousePosition = rawFirstPoint;

        Character targetCharacter = targetInfo.GetTargets().Count > 0 ? (Character)targetInfo.GetTargets()[0] : null;
        _isTarget = targetCharacter != null;
        _activeCastTargetCharacter = targetCharacter;

        if (_isTarget) Targeting.SetTarget(targetCharacter);
        else Targeting.SetTarget(new TargetData(_firstMousePosition));

        Vector3 secondPoint = targetInfo.Points.Count > 1 ? targetInfo.Points[1] : _firstMousePosition;
        Vector3 thirdPoint = targetInfo.Points.Count > 2 ? targetInfo.Points[2] : secondPoint;

        ChooseDirectionPush(secondPoint, anchorPlayerPos);
        ChooseSpeed(thirdPoint, anchorPlayerPos);
        CheckWhoTarget(Targeting.Target);

        _activeCastIsFast = _isFast;
        _activeCastIsPushTarget = _isPushTarget;

        ApplyCastTimingForSpeed();
    }

    private void ChooseSpeed(Vector3 speedPoint, Vector3 anchorPlayerPos)
    {
        _isFast = IsPointAwayFromCaster(speedPoint, _firstMousePosition, anchorPlayerPos);
    }

    private void ChooseDirectionPush(Vector3 pushPoint, Vector3 anchorPlayerPos)
    {
        _isPushTarget = IsPointAwayFromCaster(pushPoint, _firstMousePosition, anchorPlayerPos);
    }

    private static Vector3 GetCastDirection(Vector3 castPoint, Vector3 casterPosition)
    {
        Vector3 direction = castPoint - casterPosition;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    private static bool IsPointAwayFromCaster(Vector3 point, Vector3 castPoint, Vector3 casterPosition)
    {
        Vector3 choiceDirection = point - castPoint;
        choiceDirection.y = 0f;
        return Vector3.Dot(GetCastDirection(castPoint, casterPosition), choiceDirection) > 0f;
    }

    private void ApplyCastTimingForSpeed()
    {
        if (_isFast)
        {
            _preparingDuration = _fastTimeCast;
        }
        else
        {
            _preparingDuration = _slowTimeCast;
        }
    }
}
