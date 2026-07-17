using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class ReconnaissanceFire : Skill
{
    [Header("Reconnaissance Fire Settings")]
    [SerializeField] private ReconnaissanceFireAura _fireAura;
    [SerializeField] private ArrowFireProjectile _arrowFireProjectile;
    [SerializeField] private ObjectData _fireData;
    [SerializeField] private float _duration = 10;
    [SerializeField] private float _baseArea = 3f;

    [Header("Raycast settings")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("Arc Fire Arrow Settings")]
    [SerializeField] private LineRenderer _arcRenderer;
    [SerializeField] private float _arcHeight = 6f;

    [Header("Sky Arrow Settings")]
    [SerializeField] private DrawCircle _extendedRadiusCircle;
    [SerializeField] private Color _extendedRadiusColor = new Color(0.8f, 0.3f, 0f);


    private ShotIntoSky _shotIntoSky; 
    private ReconnaissanceFireAura _currentFireAura;
    private ArrowFireProjectile _currentArrowFireAura;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private float _baseDuration;
    private float _baseAnimSpeed;
    private float _baseCastDelay;
    private float _extendedRadius;
    private Coroutine _auraLifeCoroutine;
    private Coroutine _boostWindow;
    private Coroutine _checkExtendedRadiusCoroutine;
    private Coroutine _pendingFireSpawnCoroutine;
    private bool _isSkillEnableBoostLogic;
    private bool _castFromExtendedRadius;
    private WaitForSeconds _waitForElvenBoostDuration;
    private WaitForSeconds _waitForExtendedRadiusInterval = new WaitForSeconds(0.1f);

    #region Const
    private const float AnimSlowdownFactor = 1.8f;
    private const float ElvenBoostDuration = 2f;
    private const float FireAuraBoostedHealth = 65f;
    private const float FireAuraWorshipperBonusDuration = 6f;
    private const float AuraSpawnYOffset = 0.1f;
    private const float AnimationFireMoveMagnitude = 0.0001f;

    private const int ArcResolution = 30;
    private const float ArcMidPointT = 0.5f;
    private const float BezierMidPointMultiplier = 2f;

    private const float DefaultFireAuraHealth = 6f;
    #endregion

    #region Talent
    private bool _fireDarkTalent;
    private bool _fireHealthTalent;
    private bool _fireWorshipperTalent;
    private bool _isSkillEnableBoostLogicActiveTalent;
    private bool _isFireArrowIntoSkyRadiusTalent;
    #endregion

    public ReconnaissanceFireAura CurrentFireAura => _currentFireAura;
    public float BaseArea { get => _baseArea; set => _baseArea = value; }

    protected override bool IsCanCast
    {
        get
        {
            if (float.IsPositiveInfinity(_targetPoint.x)) return false;

            float allowedRadius = _isFireArrowIntoSkyRadiusTalent ? _extendedRadius : AreaInfo.Radius;
            return Targeting.IsPointInRadius(allowedRadius, _targetPoint);
        }
    }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override void SkillEnableBoostLogic()
    {
        CastDeley = 0;
        _isSkillEnableBoostLogic = true;
        SetBoostLogic(true);
    }

    protected override void SkillDisableBoostLogic()
    {
        CastDeley = _baseCastDelay;
        _isSkillEnableBoostLogic = false;
        SetBoostLogic(false);
    }


    private void SetBoostLogic(bool value) => _isSkillEnableBoostLogic = value;

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        _baseAnimSpeed = Hero.Animator.speed;
        _baseDuration = _duration;
        _waitForElvenBoostDuration = new WaitForSeconds(ElvenBoostDuration);

        _extendedRadiusCircle = GetComponentInChildren<DrawCircle>(true);
        if (_extendedRadiusCircle != null)
        {
            _extendedRadiusCircle.Clear();
        }

        _shotIntoSky = _hero.Abilities.GetSkill<ShotIntoSky>();
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
        _baseCastDelay = CastDeley;
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    #region ArcDraw
    private void DrawArc(Vector3 start, Vector3 mid, Vector3 end)
    {
        const int arcResolution = ArcResolution;
        Vector3[] arcPoints = new Vector3[arcResolution + 1];

        for (int i = 0; i <= arcResolution; i++)
        {
            float t = i / (float)arcResolution;
            arcPoints[i] = QuadraticBezierPoint(t, start, mid, end);
        }

        _arcRenderer.positionCount = arcPoints.Length;
        _arcRenderer.SetPositions(arcPoints);
    }

    private Vector3 QuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        return u * u * p0 + BezierMidPointMultiplier * u * t * p1 + t * t * p2;
    }
    #endregion

    protected override void PlayPrepareAnim()
    {
        float dist = Vector3.Distance(transform.position, _targetPoint);
        _castFromExtendedRadius = _isFireArrowIntoSkyRadiusTalent && (dist > AreaInfo.Radius);

        string trigger = _castFromExtendedRadius ? "ShotSkyCastDelay" : "ThrowCastDelay";
        Animation.PlayTrigger(trigger);
    }

    public void AnimationFireMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.SetCanMove(false);

        Vector3 direction = _targetPoint - _hero.transform.position;
        bool badDirection = float.IsInfinity(_targetPoint.x) || direction.sqrMagnitude < AnimationFireMoveMagnitude;

        if (badDirection)
        {
            _hero.Move.StopLookAt();
            return;
        }

        _hero.Move.LookAtPosition(_targetPoint);
    }

    public void TryStartElvenBoostWindow()
    {
        if (!_isSkillEnableBoostLogicActiveTalent) return;
        if (_boostWindow != null) StopCoroutine(_boostWindow);
        _boostWindow = StartCoroutine(ElvenBoostWindow());
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Hero.Animator.speed = Hero.Animator.speed / AnimSlowdownFactor;

        ReconnaissanceFireHealthTalentEnter();

        if (_isFireArrowIntoSkyRadiusTalent)
        {
            ShowExtendedRadius();
            if (_checkExtendedRadiusCoroutine != null) StopCoroutine(_checkExtendedRadiusCoroutine);
            _checkExtendedRadiusCoroutine = StartCoroutine(CheckExtendedRadiusJob());
        }

        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            Vector3 hoverPoint = Targeting.GetMousePoint();

            if (_arcRenderer != null && hoverPoint.IsFinite())
            {
                float dist = Vector3.Distance(transform.position, hoverPoint);
                bool isExtended = _isFireArrowIntoSkyRadiusTalent && (dist > AreaInfo.Radius);

                if (!isExtended)
                {
                    Vector3 start = transform.position;
                    Vector3 mid = Vector3.Lerp(start, hoverPoint, ArcMidPointT);
                    mid.y = Mathf.Max(start.y, hoverPoint.y) + _arcHeight;

                    DrawArc(start, mid, hoverPoint);
                }
                else
                {
                    _arcRenderer.positionCount = 0;
                }
            }

            if (GetMouseButton)
            {
                targetPoint = Targeting.GetMousePoint();
                if (_arcRenderer != null) _arcRenderer.positionCount = 0;
            }

            yield return null;
        }

        if (_checkExtendedRadiusCoroutine != null)
        {
            StopCoroutine(_checkExtendedRadiusCoroutine);
            _checkExtendedRadiusCoroutine = null;
        }
        HideExtendedRadius();

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_targetPoint == Vector3.positiveInfinity && (_fireAura == null)) yield break;

        Hero.Animator.speed = _baseAnimSpeed;
        Hero.Move.StopLookAt();
        Hero.Move.SetCanMove(true);

        CmdSpawnProjectile(_targetPoint, _castFromExtendedRadius, _isFireArrowIntoSkyRadiusTalent);
        
        ResetData();

        yield return null;
    }

    private IEnumerator ElvenBoostWindow()
    {
        EnableSkillBoost();
        yield return _waitForElvenBoostDuration;
        DisableSkillBoost();
    }

    private void HandleSkillCanceled()
    {
        if (_checkExtendedRadiusCoroutine != null)
        {
            StopCoroutine(_checkExtendedRadiusCoroutine);
            _checkExtendedRadiusCoroutine = null;
        }
        HideExtendedRadius();

        CmdCancelPendingFireSpawn();

        if (_hero != null && _hero.Move != null) ReconnaissanceFireHealthTalentExit();
        if (_arcRenderer != null) _arcRenderer.positionCount = 0;
        Hero.Animator.speed = _baseAnimSpeed;
        Hero.Move.SetCanMove(true);
        Hero.Move.StopLookAt();
        _targetPoint = Vector3.positiveInfinity;
        AnimCastEnded();
        if (_auraLifeCoroutine != null) StopCoroutine(_auraLifeCoroutine);
        if (_boostWindow != null) StopCoroutine(_boostWindow);

        ResetData();
    }

    private void HandleProjectilePathEnd(Vector3 point)
    {
        CmdSpawnFireAura(point);
        _currentArrowFireAura.OnProjectilePathEnd -= HandleProjectilePathEnd;
    }

    [Command]
    private void CmdSpawnProjectile(Vector3 targetPoint, bool castFromExtendedRadius, bool isFireArrowIntoSkyRadiusTalent)
    {
        if (float.IsInfinity(targetPoint.x) || float.IsNaN(targetPoint.x)) return;
        
        if (castFromExtendedRadius && isFireArrowIntoSkyRadiusTalent && _shotIntoSky != null)
        {
            _shotIntoSky.SpawnUtilityArrowVisual(targetPoint);

            if (_pendingFireSpawnCoroutine != null) StopCoroutine(_pendingFireSpawnCoroutine);
            _pendingFireSpawnCoroutine = StartCoroutine(SpawnFireAfterArrowDelay(targetPoint, _shotIntoSky.DropDelayTime));
        }
        else
        {
            SpawnNormalProjectile(targetPoint);
        }
    }

    private void SpawnNormalProjectile(Vector3 targetPoint)
    {
        Vector3 start = transform.position;

        var projectile = Instantiate(_arrowFireProjectile, start, Quaternion.identity);
        projectile.Init(targetPoint, _arcHeight);

        NetworkServer.Spawn(projectile.gameObject);

        _currentArrowFireAura = projectile;

        RpcLaunchProjectile(projectile.gameObject, targetPoint);
    }

    private IEnumerator SpawnFireAfterArrowDelay(Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        _pendingFireSpawnCoroutine = null;

        SpawnFireAuraInstant(position); 
    }

    [Command]
    private void CmdCancelPendingFireSpawn()
    {
        if (_pendingFireSpawnCoroutine == null) return;
        StopCoroutine(_pendingFireSpawnCoroutine);
        _pendingFireSpawnCoroutine = null;
    }

    [Command]
    private void CmdSetMaxHealth(float maxHealth)
    {
        _fireData.MaxHealth = maxHealth;
    }

    [Command]
    private void CmdSpawnFireAura(Vector3 position)
    {
        SpawnFireAuraInstant(position);
    }
    
    [Server]
    private void SpawnFireAuraInstant(Vector3 position)
    {
        if (float.IsInfinity(position.x) || float.IsNaN(position.x)) return;

        if (!_isSkillEnableBoostLogic)
        {
            if (_auraLifeCoroutine != null) StopCoroutine(_auraLifeCoroutine);
            if (_currentFireAura != null) NetworkServer.Destroy(_currentFireAura.gameObject);
        }

        position.y += AuraSpawnYOffset;
        var aura = Instantiate(_fireAura, position, Quaternion.identity);
        aura.Init(Hero);

        NetworkConnectionToClient playerConnection = (_hero != null && _hero.netIdentity != null) 
            ? _hero.netIdentity.connectionToClient 
            : connectionToClient;

        NetworkServer.Spawn(aura.gameObject, playerConnection);

        aura.GetComponent<Object>().IndexTeam = _hero.NetworkSettings.TeamIndex;

        _currentFireAura = aura;
        _currentFireAura.FireDarkTalent = _fireDarkTalent;

        RpcSetCurrentFireAura(aura);

        float life = _baseDuration + (_fireWorshipperTalent ? FireAuraWorshipperBonusDuration : 0f);
        _auraLifeCoroutine = StartCoroutine(DestroyAuraAfter(life, aura));
    }
    
    [ClientRpc]
    private void RpcLaunchProjectile(GameObject projectileObj, Vector3 targetPoint)
    {
        if (projectileObj != null && projectileObj.TryGetComponent(out ArrowFireProjectile projectile))
        {
            projectile.Init(targetPoint, _arcHeight);
            _currentArrowFireAura = projectile;
            _currentArrowFireAura.OnProjectilePathEnd += HandleProjectilePathEnd;
        }
    }

    [Server]
    private IEnumerator DestroyAuraAfter(float seconds, ReconnaissanceFireAura aura)
    {
        yield return new WaitForSeconds(seconds);
        if (aura != null) NetworkServer.Destroy(aura.gameObject);
    }

    [ClientRpc]
    private void RpcSetCurrentFireAura(ReconnaissanceFireAura fireAura)
    {
        _currentFireAura = fireAura;
        _currentFireAura.FireDarkTalent = _fireDarkTalent;

        if (_fireWorshipperTalent)
        {
            _currentFireAura.ApplyFireWorshipperTalentEffect(true);
            CmdApplyFireWorshipper();
        }
        _currentFireAura.GetComponent<Object>().IndexTeam = _hero.NetworkSettings.TeamIndex;
    }

    [Command]
    private void CmdApplyFireWorshipper()
    {
        _currentFireAura.ApplyFireWorshipperTalentEffect(true);
    }

    private void ResetData()
    {
        _castFromExtendedRadius = false;
        CastDeley = _baseCastDelay;
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        if (_arcRenderer != null) _arcRenderer.positionCount = 0;
        AnimCastEnded();
        if (_auraLifeCoroutine != null) StopCoroutine(_auraLifeCoroutine);
        
        ResetData();
    }

    private void ShowExtendedRadius()
    {
        if (_extendedRadiusCircle == null) _extendedRadiusCircle = GetComponentInChildren<DrawCircle>(true);
    }

    private void HideExtendedRadius()
    {
        if (_extendedRadiusCircle != null)
        {
            _extendedRadiusCircle.Clear();
        }
    }

    private IEnumerator CheckExtendedRadiusJob()
    {
        float lastCalculatedRadius = -1f;

        while (true)
        {
            if (_extendedRadiusCircle == null)
            {
                yield return null;
                continue;
            }

            _extendedRadius = _shotIntoSky.AreaInfo.Radius;

            Vector3 mousePoint = GetMousePointOnLayer(_groundLayer);
            bool cursorInside = !float.IsPositiveInfinity(mousePoint.x) && Vector3.Distance(mousePoint, transform.position) <= _extendedRadius;

            _extendedRadiusCircle.SetColor(cursorInside ? Color.green : _extendedRadiusColor);

            if (!Mathf.Approximately(lastCalculatedRadius, _extendedRadius))
            {
                lastCalculatedRadius = _extendedRadius;
                _extendedRadiusCircle.Clear();
                _extendedRadiusCircle.Draw(_extendedRadius);
            }

            yield return _waitForExtendedRadiusInterval;
        }
    }

    protected Vector3 GetMousePointOnLayer(LayerMask layer, float y = 0f)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, layer))
        {
            Vector3 point = hit.point;
            point.y = y;
            return point;
        }

        return Vector3.positiveInfinity;
    }

    #region ReconnaissanceFireAuraDarknesTalent
    public void ReconnaissanceFireAuraDarknesActive(bool value)
    {
        _fireDarkTalent = value;

        if (_currentFireAura != null) _currentFireAura.FireDarkTalent = _fireDarkTalent;
    }
    #endregion

    #region ReconnaissanceFireHealthTalent
    public void ReconnaissanceFireHealthTalentActive(bool value)
    {
        _fireHealthTalent = value;
    }

    private void ReconnaissanceFireHealthTalentEnter()
    {
        if (_fireHealthTalent)
        {
            CmdSetMaxHealth(FireAuraBoostedHealth);
            _fireData.MaxHealth = FireAuraBoostedHealth;
        }
    }

    private void ReconnaissanceFireHealthTalentExit()
    {
        CmdSetMaxHealth(DefaultFireAuraHealth);
        _fireData.MaxHealth = DefaultFireAuraHealth;
    }
    #endregion

    #region partialBlindnessTalent
    
    private bool _partialBlindnessTalent;

    public void partialBlindnessTalentActive(bool value)
    {
        if(_partialBlindnessTalent == value) return;
        
        _partialBlindnessTalent = value;
        if (_currentFireAura != null) _currentFireAura.PartialBlindnessTalent = _partialBlindnessTalent;
    }
    #endregion

    #region FireWorshipperTalent
    public void FireWorshipperTalentActive(bool value)
    {
        if(value == _fireWorshipperTalent) return;
        
        _fireWorshipperTalent = value;

        if (_fireWorshipperTalent)
            AreaInfo.Area += 2;
        else
            AreaInfo.Area -= 2;
    }

    #endregion

    #region SkillEnableBoostLogicActiveTalent
    public void SkillEnableBoostLogicActiveTalent(bool value) => _isSkillEnableBoostLogicActiveTalent = value;
    #endregion

    #region SkyArrow Talent
    public void FireArrowIntoSkyRadius(bool value)
    {
        if (value == _isFireArrowIntoSkyRadiusTalent) return;
        _isFireArrowIntoSkyRadiusTalent = value;

        if (!_isFireArrowIntoSkyRadiusTalent)
        {
            if (_checkExtendedRadiusCoroutine != null)
            {
                StopCoroutine(_checkExtendedRadiusCoroutine);
                _checkExtendedRadiusCoroutine = null;
            }
            HideExtendedRadius();
        }

        if (_isFireArrowIntoSkyRadiusTalent && IsPreparing)
        {
            ShowExtendedRadius();
            if (_checkExtendedRadiusCoroutine != null) StopCoroutine(_checkExtendedRadiusCoroutine);
            _checkExtendedRadiusCoroutine = StartCoroutine(CheckExtendedRadiusJob());
        }
    }
    #endregion
}