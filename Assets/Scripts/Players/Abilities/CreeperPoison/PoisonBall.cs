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

public class PoisonBall : Skill, IAltAbility
{
    #region Variables

    [Header("Ability properties")]
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private PoisonBallProjectile _projectile;
    [SerializeField] private Character _player;
    [SerializeField] private ArrowRender _arrowPrefab;
    [SerializeField] private GameObject _spawnPoint;
    [SerializeField] private GameObject pointArrowRender;
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;
    [SerializeField] private ColdBlood _coldBlood;

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

    private ArrowRender[] _arrowRenderers = new ArrowRender[4];
    private GameObject _pointArrowInstance;

    private Vector3 _firstMousePosition = Vector3.positiveInfinity;
    private Vector3 _secondMousePosition;
    private Vector3 _thirdMousePosition;

    private Vector3 _activeCastPoint = Vector3.positiveInfinity;
    private Character _activeCastTargetCharacter;
    private bool _activeCastIsFast;
    private bool _activeCastIsPushTarget;

    private int _poisonBoneStacks = 0;

    private float _fastTimeCast = 0.4f;
    private float _slowTimeCast = 1.8f;
    private float _originalChargeCooldown;
    private float _baseCastWidth;
    private float _multiplierForPushDistance;
    private float _animTime;
    private float _baseMultiplierAnimationSpeed = 1f;
    private float _radiusFindTarget = 0.5f;
    private float _increaseManaCostValue = 1.3f;
    private float _baseIncreaseManaCostValue = 1f;
    
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
    protected override int AnimTriggerCastDelay => Animator.StringToHash("PoisonBallCastDelayAnimTrigger");
    //protected override bool IsCanCast => CheckCanCast();

    public event Action ResetAbilityParameters;
    public event Action AbilityChange;

    #region Talent

    private bool _isCanSpawnPoisonCloud = false;
    private bool _isActiveBallEffect = false;
    private bool _isIncreasingPoisonBallCharges = false;
    private bool _isPoisonCloudAddPoisonBone = false;
    private bool _isHealingPoisonBall = false;
    private bool _isColdBloodCrit = false;

    private bool _isBonusChargeApplied = false;

    private bool _isTransparentPoisons = false;

    public bool IsTransparentPoisons
    {
        get => _isTransparentPoisons;
        set
        {
            if (_isTransparentPoisons != value)
            {
                _isTransparentPoisons = value;

                if (_isTransparentPoisons) Buff.ManaCost.IncreasePercentage(_increaseManaCostValue);
                else Buff.ManaCost.IncreasePercentage(_baseIncreaseManaCostValue);
            }
        }
    }

    public bool IsPoisonCloudAddPoisonBone { get => _isPoisonCloudAddPoisonBone; set => _isPoisonCloudAddPoisonBone = value; }

    public bool IsIncreasingPoisonBallCharges
    {
        get => _isIncreasingPoisonBallCharges;

        set
        {
            if (_isIncreasingPoisonBallCharges == value) return;

            _isIncreasingPoisonBallCharges = value;
        }
    }

    public void ColdBloodStrike(bool value) => _isColdBloodCrit = value;

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

    public void TransparentPoisons(bool value) => IsTransparentPoisons = value;

    #endregion

    #endregion

    public void PayCostPoisonBall()
    {
        TryPayCost(true);
    }

    protected override void UseCooldownOrCharges()
    {
        if (_isHealingPoisonBall && (_poisonBallInfo.IsOriginalTargetAllies || _poisonBallInfo.IsOriginalTargetPlayer))
        {
            Debug.Log("Shortened CD");
            Charges.TryUse(Charges.BaseCooldown / 2);
            return;
        }
        Charges.TryUse();
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

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

        if (!IsPreparing)
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
            _player.Animator.ResetTrigger(AnimTriggerCastDelay);
            _player.Animator.SetFloat("PoisonBallMultiplierSpeedAnimation", _baseMultiplierAnimationSpeed);
            _player.Animator.SetFloat("CastSpeed", 1f);
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
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
                CheckWhoTarget();

                Character tempTarget = Targeting.GetTempTarget()?.Character;
                _isTarget = tempTarget != null;

                Vector3 originPoint = _isTarget ? tempTarget.transform.position : click;
                _firstMousePosition = originPoint;
                _firstClickPlayerPos = _player.transform.position;

                if (_arrowRenderers[0] == null)
                {
                    CreateArrowsParallelToPlayer(_firstMousePosition);
                    StartMouseDetectionIfNeeded();
                }

                _arrowRenderers[0]?.gameObject.SetActive(true);
                _arrowRenderers[1]?.gameObject.SetActive(true);
                _arrowRenderers[2]?.gameObject.SetActive(false);
                _arrowRenderers[3]?.gameObject.SetActive(false);

                Vector3 clampedPoint = originPoint;
                if (Vector3.Distance(_player.transform.position, originPoint) > AreaInfo.CastLength)
                {
                    Vector3 dir = (originPoint - _player.transform.position).normalized;
                    clampedPoint = _player.transform.position + dir * AreaInfo.CastLength;
                }

                targetPoint = clampedPoint;
                SkillRender.SetFixedLookPoint(targetPoint);

                _firstClickDone = true;
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

                if (Targeting.GetTempTarget()?.Character != null)
                {
                    if (_secondMousePosition.x < _firstMousePosition.x &&
                        _secondMousePosition.z < _firstMousePosition.z)
                    {
                        SetArrowVisibility(1, true);
                        SetArrowVisibility(3, false);
                    }
                    else
                    {
                        SetArrowVisibility(3, true);
                        SetArrowVisibility(1, false);
                    }
                }
            }

            yield return null;
        }

        while (!_thirdClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_arrowRenderers[0] != null) _arrowRenderers[0].SetDeafaultMaterail();
                if (_arrowRenderers[1] != null) _arrowRenderers[1].SetDeafaultMaterail();

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
        ChooseWhichProjectileCreate();

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

    private void CheckWhoTarget()
    {
        if (Targeting.GetTempTarget()?.Character != null)
        {
            var target = Targeting.GetTempTarget().Character.gameObject;

            _poisonBallInfo.IsOriginalTargetPlayer = target == _player.gameObject;
            _poisonBallInfo.IsOriginalTargetAllies = target.layer == LayerMask.NameToLayer("Allies");
            _poisonBallInfo.IsOriginalTargetEnemy = target.layer == LayerMask.NameToLayer("Enemy");
            Debug.Log($"{_poisonBallInfo.IsOriginalTargetPlayer} {_poisonBallInfo.IsOriginalTargetAllies} {_poisonBallInfo.IsOriginalTargetEnemy}");
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

    private bool CheckCanCast()
    {
        if (Charges != null && Charges.RemainingCharges <= 1 && (IsPreparing || IsCasting))
        {
            return false;
        }

        if (_activeCastTargetCharacter == null)
            return Vector3.Distance(_activeCastPoint, transform.position) <= AreaInfo.CastLength
                   && Targeting.NoObstacles(_activeCastPoint, _obstacle);

        return Vector3.Distance(_activeCastPoint, transform.position) <= AreaInfo.CastLength
               && Targeting.NoObstacles(_activeCastPoint, _obstacle)
               || Vector3.Distance(_activeCastTargetCharacter.transform.position, transform.position) <= AreaInfo.CastLength
               && Targeting.NoObstacles(_activeCastTargetCharacter.transform.position, _obstacle);
    }

    #endregion

    #region ChooseMoveSpeedProjectile

    private void ChooseWhichProjectileCreate()
    {
        if (_isTarget)
        {
            CmdCreateProjectileForTarget(_activeCastTargetCharacter.gameObject, _activeCastTargetCharacter.transform.position,
                _poisonBallInfo.MaxCountProjectile, _multiplierForPushDistance, PoisonBoneStack,
                _activeCastIsFast, _activeCastIsPushTarget, IsAltAbility,
                _activeTalentsInfo.IsActiveFootInstincts, _activeTalentsInfo.IsActiveRestorationOfGlands,
                _isHealingPoisonBall, _activeTalentsInfo.IsActiveWitheringPoison, _activeTalentsInfo.IsActiveVoluminousBall, _isActiveBallEffect,
                _activeTalentsInfo.IsActiveInertialGlands, _activeTalentsInfo.IsActiveContinuationAmbush,
                _poisonBallInfo.IsOriginalTargetEnemy, _poisonBallInfo.IsOriginalTargetPlayer, _poisonBallInfo.IsOriginalTargetAllies, _isTransparentPoisons);

            if (_isCanSpawnPoisonCloud) CmdApplyPoisonCloud(_isHealingPoisonBall, _durationPoisonCloud);
        }
        else
        {
            CmdCreateProjectileForFlyingMaxDistance(_activeCastPoint,
                _poisonBallInfo.MaxCountProjectile, _multiplierForPushDistance, PoisonBoneStack,
                _activeCastIsFast, _activeCastIsPushTarget, IsAltAbility,
                _activeTalentsInfo.IsActiveFootInstincts, _activeTalentsInfo.IsActiveRestorationOfGlands,
                _isHealingPoisonBall, _activeTalentsInfo.IsActiveWitheringPoison, _activeTalentsInfo.IsActiveVoluminousBall, _isActiveBallEffect,
                _activeTalentsInfo.IsActiveInertialGlands, _activeTalentsInfo.IsActiveContinuationAmbush,
                _poisonBallInfo.IsOriginalTargetEnemy, _poisonBallInfo.IsOriginalTargetPlayer, _poisonBallInfo.IsOriginalTargetAllies, _isTransparentPoisons);

            if (_isCanSpawnPoisonCloud) CmdApplyPoisonCloud(_isHealingPoisonBall, _durationPoisonCloud);
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

    private void CreateArrowsParallelToPlayer(Vector3 point)
    {
        if (_arrowPrefab == null || pointArrowRender == null) return;

        Vector3 center = Targeting.GetTarget()?.Character != null ? Targeting.GetTarget().Character.transform.position : point;
        Vector3 playerPos = _player.transform.position;

        center.y = 1.1f;
        playerPos.y = 1.1f;

        Vector3 direction = (playerPos - center).normalized;

        _pointArrowInstance = new GameObject("PoisonBallArrowCenter");
        _pointArrowInstance.transform.position = center;
        _pointArrowInstance.transform.rotation = Quaternion.LookRotation(direction);

        Vector3 offset = direction * 0.6f;
        Vector3 fartherOffset = direction * 1.2f;

        Vector3[] spawnPositions = new Vector3[4]
        {
            center + offset,
            center - offset,
            center + fartherOffset,
            center - fartherOffset
        };

        Quaternion[] rotations = new Quaternion[4]
        {
            Quaternion.LookRotation(playerPos - spawnPositions[0]),
            Quaternion.LookRotation(spawnPositions[1] - playerPos),
            Quaternion.LookRotation(playerPos - spawnPositions[2]),
            Quaternion.LookRotation(spawnPositions[3] - playerPos),
        };

        for (int i = 0; i < _arrowRenderers.Length; i++)
        {
            Quaternion flippedRotation = rotations[i] * Quaternion.Euler(0, 180f, 0);
            _arrowRenderers[i] = Instantiate(_arrowPrefab, spawnPositions[i], flippedRotation, _pointArrowInstance.transform);
            RotateArrowChild(_arrowRenderers[i].gameObject, -90);
            _arrowRenderers[i].gameObject.SetActive(false);
        }
    }

    private void RotateArrowChild(GameObject arrow, float zRotation)
    {
        if (arrow == null) return;

        Transform childArrow = arrow.transform.GetChild(0);
        float currentXRotation = childArrow.localEulerAngles.x;

        childArrow.localRotation = Quaternion.Euler(currentXRotation, 0, zRotation);
    }

    private void SetArrowVisibility(int arrowIndex, bool isVisible)
    {
        if (arrowIndex >= 0 && arrowIndex < _arrowRenderers.Length && _arrowRenderers[arrowIndex] != null)
        {
            _arrowRenderers[arrowIndex].gameObject.SetActive(isVisible);
        }
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
            if (_pointArrowInstance != null)
            {
                Vector3 dir = _player.transform.position - _pointArrowInstance.transform.position;
                dir.y = 0f;
                if (dir != Vector3.zero) _pointArrowInstance.transform.rotation = Quaternion.LookRotation(dir);
            }

            Vector3 currentMousePosition = Targeting.GetMousePoint();

            if (_firstClickDone && !_secondClickDone)
            {
                UpdateArrowHighlight(0, 1, currentMousePosition);
            }

            else if (_secondClickDone && !_thirdClickDone)
            {
                UpdateArrowHighlight(2, 3, currentMousePosition);
            }

            yield return null;
        }
    }

    private void UpdateArrowHighlight(int index1, int index2, Vector3 currentMousePosition)
    {
        if (_arrowRenderers[index1] == null || _arrowRenderers[index2] == null)
            return;

        Vector3 playerPos = _player.transform.position;
        Vector3 arrowPos1 = _arrowRenderers[index1].transform.position;
        Vector3 arrowPos2 = _arrowRenderers[index2].transform.position;

        float dist1 = Vector3.Distance(currentMousePosition, arrowPos1);
        float dist2 = Vector3.Distance(currentMousePosition, arrowPos2);

        if (dist1 < dist2)
        {
            SetArrowVisibility(index1, true);
            SetArrowVisibility(index2, false);

            _arrowRenderers[index1].SetDeafaultMaterail();
            _arrowRenderers[index2].SetTransparentMaterial();
        }
        else
        {
            SetArrowVisibility(index1, false);
            SetArrowVisibility(index2, true);

            _arrowRenderers[index1].SetTransparentMaterial();
            _arrowRenderers[index2].SetDeafaultMaterail();
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
        bool isTargetEnemy, bool isTargetPlayer, bool isTargetAllies, bool isTransparentPoisons)
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

        if (_isColdBloodCrit)
        {
            poisonBallProjectile.InitializationProjectileForPoisonBall(_player, this,
                multiplierForPushDistance, poisonBoneStack,
                isTargetPlayer, isTargetEnemy, isTargetAllies,
                isActiveFootInstincts, isActiveRestorationOfGlands,
                isActiveHealingPoisonBall, isActiveWitheringPoison, isActiveVoluminousBall, isActiveBallEffect,
                isPushTarget, isPlayerInvisible,
                isTransparentPoisons, ownerLayer, _coldBlood.IsCanCrit);
        }
        else
        {
            poisonBallProjectile.InitializationProjectileForPoisonBall(_player, this,
                multiplierForPushDistance, poisonBoneStack,
                isTargetPlayer, isTargetEnemy, isTargetAllies,
                isActiveFootInstincts, isActiveRestorationOfGlands,
                isActiveHealingPoisonBall, isActiveWitheringPoison, isActiveVoluminousBall, isActiveBallEffect,
                isPushTarget, isPlayerInvisible,
                isTransparentPoisons, ownerLayer, false);
        }

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
        bool isTargetEnemy, bool isTargetPlayer, bool isTargetAllies, bool isTransparentPoisons)
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

        if (_isColdBloodCrit)
        {
            poisonBallProjectile.InitializationProjectileForPoisonBall(_player, this,
                multiplierForPushDistance, poisonBoneStack,
                isTargetPlayer, isTargetEnemy, isTargetAllies,
                isActiveFootInstincts, isActiveRestorationOfGlands,
                isActiveHealingPoisonBall, isActiveWitheringPoison, isActiveVoluminousBall, isActiveBallEffect,
                isPushTarget, isPlayerInvisible,
                isTransparentPoisons, ownerLayer, _coldBlood.IsCanCrit);
        }
        else
        {
            poisonBallProjectile.InitializationProjectileForPoisonBall(_player, this,
                multiplierForPushDistance, poisonBoneStack,
                isTargetPlayer, isTargetEnemy, isTargetAllies,
                isActiveFootInstincts, isActiveRestorationOfGlands,
                isActiveHealingPoisonBall, isActiveWitheringPoison, isActiveVoluminousBall, isActiveBallEffect,
                isPushTarget, isPlayerInvisible,
                isTransparentPoisons, ownerLayer, false);
        }

        poisonBallProjectile.MoveBallOnMaxDistance(finalPoint, isFast);

        NetworkServer.Spawn(item);

        poisonBallProjectile.RpcInitTransparent(isTransparentPoisons, ownerLayer);
    }

    [Command]
    private void CmdApplyPoisonCloud(bool isHealingCloud, float duration)
    {
        if (!isHealingCloud)
        {
            if (_poisonDamagingCloud == null && _poisonDamagingCloudPrefab.PoisonDamageCloud == null)
            {
                _poisonDamagingCloud = Instantiate(_poisonDamagingCloudPrefab, _player.transform.position, Quaternion.identity);

                _poisonDamagingCloudPrefab.PoisonDamageCloud = _poisonDamagingCloud;

                _poisonDamagingCloudPrefab.PoisonDamageCloud.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
                _poisonDamagingCloudPrefab.PoisonDamageCloud.AddStack();

                NetworkServer.Spawn(_poisonDamagingCloud.gameObject);
            }
            else
            {
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

                _poisonHealingCloudPrefab.PoisonHealingCloud.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
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

    [Command]
    private void SetSpawnPointPosition(float spawnPointX, float spawnPointY, float spawnPointZ)
    {
        _spawnPointInfo.SpawnPointX = spawnPointX;
        _spawnPointInfo.SpawnPointY = spawnPointY;
        _spawnPointInfo.SpawnPointZ = spawnPointZ;
    }

    #endregion

    [ClientRpc]
    private void RpcApply(PoisonDamagingCloudPrefab poisonDamagingCloud, PoisonHealingCloudPrefab poisonHealingCloud, float duration, bool isHealingCloud)
    {
        if (poisonDamagingCloud != null)
        {
            poisonDamagingCloud.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
            poisonDamagingCloud.AddStack();
        }

        if (poisonHealingCloud != null && isHealingCloud)
        {
            poisonHealingCloud.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
            poisonHealingCloud.AddStack();
        }
    }

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

        ChooseSpeed(secondPoint, anchorPlayerPos);
        ChooseDirectionPush(thirdPoint, anchorPlayerPos);

        _activeCastIsFast = _isFast;
        _activeCastIsPushTarget = _isPushTarget;

        ApplyCastTimingForSpeed();
    }

    private void ChooseSpeed(Vector3 secondPoint, Vector3 anchorPlayerPos)
    {
        Vector3 castDirection = _firstMousePosition - anchorPlayerPos;
        castDirection.y = 0f;

        Vector3 secondClickVector = secondPoint - _firstMousePosition;
        secondClickVector.y = 0f;

        _isFast = Vector3.Dot(castDirection.normalized, secondClickVector.normalized) > 0f;
    }

    private void ChooseDirectionPush(Vector3 thirdPoint, Vector3 anchorPlayerPos)
    {
        Vector3 castDirection = _firstMousePosition - anchorPlayerPos;
        castDirection.y = 0f;

        Vector3 thirdVector = thirdPoint - _firstMousePosition;
        thirdVector.y = 0f;

        _isPushTarget = Vector3.Dot(castDirection.normalized, thirdVector.normalized) > 0f;
    }

    private void ApplyCastTimingForSpeed()
    {
        if (_isFast)
        {
            _castDeley = _fastTimeCast;
        }
        else
        {
            _castDeley = _slowTimeCast;
        }
        _player.Animator.SetFloat("CastSpeed", _castDeley);
    }
}