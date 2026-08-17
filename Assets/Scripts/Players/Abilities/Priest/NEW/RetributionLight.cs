using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class RetributionLight : Skill,IPolaritySwitchable
{
    [Header("Effect Settings")]
    [SerializeField] private VisualEffect _lightVFXPrefab;
    [SerializeField] private float _aoeRadius = 1.5f;
    [SerializeField] private float _beamUpOffset = 8f;
    [SerializeField] private float _delayBeforeFirstBeam = 1f;
    [SerializeField] private float _delayBetweenBeams = 1f;
    
    [Header("Mode info")]
    [SerializeField] private AbilityInfo lightInfo;
    [SerializeField] private AbilityInfo darkInfo;

    [Header("AOE Preview")]
    [SerializeField] private GameObject _damageCirclePrefab;
    
    [Header("VFX Colors")]
    [SerializeField][ColorUsage(true, true)] private Color _lightCenterColor  = Color.white;
    [SerializeField][ColorUsage(true, true)] private Color _lightFresnelColor  = Color.white;
    [SerializeField][ColorUsage(true, true)] private Color _lightVoronoiColor  = Color.white;
    [SerializeField][ColorUsage(true, true)] private Color _darkCenterColor  = Color.white;
    [SerializeField][ColorUsage(true, true)] private Color _darkFresnelColor  = Color.white;
    [SerializeField][ColorUsage(true, true)] private Color _darkVoronoiColor  = Color.white;

    protected override bool IsCanCast { get => CheckCanCast(); }
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("RetributionLight");

    private Vector3 _clickPoint;
    private Vector3 _castOriginPoint;

    private GameObject _damageCircleInstance;
    private GameObject _healCircleInstance;

    private float _cachedDamageDealt;
    private bool _isDamagePhase;
    private bool _firstBeamDecalsEnded;
    
    #region OverhealManaBooster

    private OverhealManaBooster _overhealMana;
    public OverhealManaBooster OverhealManaBooster => _overhealMana;

    #endregion
    
    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    public bool IsLightMode => isLightMode;
    public event Action OnModeChange;

    private static readonly int _onFinishedEventId = Shader.PropertyToID("OnBeamHit");
    private static readonly int _onDecalsEventId   = Shader.PropertyToID("OnBeamEnd");

    private bool CheckCanCast() =>
        Vector3.Distance(_clickPoint, transform.position) <= AreaInfo.Radius;

    private bool IsEnemyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    public void AnimCastRetributionLight() => AnimStartCastCoroutine();
    public void AnimRetributionLightEnd()  => AnimCastEnded();

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0)
            _clickPoint = (Vector3)targetInfo.Points[0];
    }

    protected override void ClearData()
    {
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        
        UpdateMode();
        
        _overhealMana = new OverhealManaBooster(this, Hero);
    }
    
    private void OnEnable()
    {
        OnModeChange += UpdateMode;
    }

    private void OnDisable()
    {
        OnModeChange -= UpdateMode;
    }

    private void SpawnOrMoveCircle(ref GameObject instance, GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;

        if (instance == null)
            instance = Instantiate(prefab);

        instance.SetActive(true);
        instance.transform.position = position;
    }

    private void DestroyCirclePreview(ref GameObject instance)
    {
        if (instance == null) return;
        Destroy(instance);
        instance = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (!GetMouseButton)
        {
            yield return null;
        }

        _clickPoint      = Targeting.GetMousePoint();
        _castOriginPoint = transform.position;

        targetInfo.Points.Add(_clickPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        CommitUse();

        Vector3 localClickPoint  = _clickPoint;
        Vector3 localOriginPoint = _castOriginPoint;

        _cachedDamageDealt = 0f;
        _isDamagePhase     = true;

        SpawnOrMoveCircle(ref _damageCircleInstance, _damageCirclePrefab, localClickPoint);
        SpawnOrMoveCircle(ref _healCircleInstance,   _damageCirclePrefab, localOriginPoint);

        yield return new WaitForSeconds(_delayBeforeFirstBeam);

        CmdSpawnAndPlayVFX(localClickPoint, localOriginPoint);

        yield return null;
    }
    
    [Command]
    private void CmdSpawnAndPlayVFX(Vector3 clickPoint, Vector3 originPoint)
    {
        VisualEffect vfxInstance = Instantiate(_lightVFXPrefab);
        NetworkServer.Spawn(vfxInstance.gameObject, connectionToClient);

        TargetRpcSetupVFX(connectionToClient, vfxInstance.gameObject, clickPoint, originPoint);
    }
    
    [TargetRpc]
    private void TargetRpcSetupVFX(NetworkConnectionToClient conn, GameObject vfxGO, Vector3 clickPoint, Vector3 originPoint)
    {
        if (vfxGO == null) return;
    
        var vfxInstance = vfxGO.GetComponent<VisualEffect>();
        if (vfxInstance == null) return;
    
        UpdateVFXColors(vfxInstance);

        vfxInstance.outputEventReceived += (args) => 
            OnVFXOutputEvent(args, vfxInstance, clickPoint, originPoint);
    
        vfxInstance.transform.SetParent(null);
        vfxInstance.transform.position = new Vector3(clickPoint.x, _beamUpOffset, clickPoint.z);
        vfxInstance.Stop();
        vfxInstance.Play();
    }

    private void OnVFXOutputEvent(VFXOutputEventArgs args, VisualEffect vfxInstance, Vector3 clickPoint, Vector3 originPoint)
    {
        if (args.nameId == _onFinishedEventId)
        {
            if (_isDamagePhase) OnDamageVFXFinished(vfxInstance, clickPoint, originPoint);
            else                OnHealVFXFinished(vfxInstance, originPoint);
        }

        if (args.nameId == _onDecalsEventId)
        {
            if (!_isDamagePhase)
            {
                if (!_firstBeamDecalsEnded)
                {
                    _firstBeamDecalsEnded = true;
                    return;
                }
                OnHealVFXDecalsEnded(vfxInstance);
            }
        }
    }

    private void OnDamageVFXFinished(VisualEffect vfxInstance, Vector3 clickPoint, Vector3 originPoint)
    {
        if (!isOwned) return;

        DestroyCirclePreview(ref _damageCircleInstance);
        _cachedDamageDealt    = ApplyAreaDamage(clickPoint, originPoint);
        _firstBeamDecalsEnded = false;
        _isDamagePhase        = false;

        StartCoroutine(DelayedHealBeam(vfxInstance, originPoint));
    }
    
    private IEnumerator DelayedHealBeam(VisualEffect vfxInstance, Vector3 originPoint)
    {
        SpawnOrMoveCircle(ref _healCircleInstance, _damageCirclePrefab, originPoint);
        yield return new WaitForSeconds(_delayBetweenBeams);
        CmdMoveAndPlayVFX(vfxInstance.gameObject, originPoint);
    }

    private void OnHealVFXFinished(VisualEffect vfxInstance, Vector3 originPoint)
    {
        if (!isOwned) return;

        DestroyCirclePreview(ref _healCircleInstance);

        if (isLightMode)
            ApplyAreaHeal(originPoint, _cachedDamageDealt);
        else
            ApplyAreaDarkDamage(originPoint, _cachedDamageDealt);
    }

    private void OnHealVFXDecalsEnded(VisualEffect vfxInstance)
    {
        CmdDisableVFX(vfxInstance.gameObject);
    }

    private float ApplyAreaDamage(Vector3 clickPoint, Vector3 originPoint)
    {
        Collider[] hits = Physics.OverlapSphere(clickPoint, _aoeRadius, Targeting.Layer);

        var targets = new List<GameObject>();
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (!IsEnemyTarget(target)) continue;
            if (target.IsDead) continue;
            targets.Add(target.gameObject);
        }

        CmdApplyDamageAndCalculate(clickPoint, originPoint, targets.ToArray());
        return 0f;
    }
    
    [Command]
    private void CmdApplyDamageAndCalculate(Vector3 position, Vector3 originPoint, GameObject[] targets)
    {
        float totalDamage = 0f;

        foreach (var targetGO in targets)
        {
            if (targetGO == null) continue;
            if (!targetGO.TryGetComponent<Character>(out var target)) continue;
            if (target.IsDead) continue;

            Damage damage = new Damage
            {
                Value  = Buff.Damage.GetBuffedValue(Damage),
                Type   = Info.DamageType,
                School = Info.School
            };

            float healthBefore = target.Health.CurrentValue;
            ApplyDamage(damage, targetGO);
            if (target.IsDead) continue;

            float actualDamage = healthBefore - target.Health.CurrentValue;
            if (actualDamage > 0f) totalDamage += actualDamage;
        }

        TargetRpcOnDamageCalculated(connectionToClient, totalDamage, originPoint);
    }
    
    [TargetRpc]
    private void TargetRpcOnDamageCalculated(NetworkConnectionToClient conn, float totalDamage, Vector3 originPoint)
    {
        _cachedDamageDealt = totalDamage;
        _isDamagePhase     = false;
    }

    private void ApplyAreaHeal(Vector3 position, float totalHeal)
    {
        if (totalHeal <= 0f) return;

        Collider[] hits = Physics.OverlapSphere(position, _aoeRadius, Targeting.Layer);

        var aliveAllies = new List<Character>();
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (IsEnemyTarget(target)) continue;
            if (target.IsDead) continue;

            aliveAllies.Add(target);
        }

        if (aliveAllies.Count == 0) return;

        float healPerTarget = totalHeal / aliveAllies.Count;
        
        var heal = new Heal 
        {
            Value = healPerTarget,
            DamageableSkill = this
        };

        foreach (var ally in aliveAllies)
        {
            CmdApplyHeal(heal,ally.gameObject,this,nameof(RetributionLight));
            _overhealMana.OnAnyHealTaken(ally,heal.Value,this);
        }
    }
    
    private void ApplyAreaDarkDamage(Vector3 position, float totalDamage)
    {
        if (totalDamage <= 0f) return;

        Collider[] hits = Physics.OverlapSphere(position, _aoeRadius, Targeting.Layer);

        var aliveEnemies = new List<Character>();
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (!IsEnemyTarget(target)) continue;
            if (target.IsDead) continue;

            aliveEnemies.Add(target);
        }

        if (aliveEnemies.Count == 0) return;

        float damagePerTarget = totalDamage / aliveEnemies.Count;

        foreach (var enemy in aliveEnemies)
        {
            Damage damage = new Damage
            {
                Value = damagePerTarget,
                Type  = Info.DamageType,
                School = Info.School
            };

            CmdApplyDamage(damage, enemy.gameObject);
        }
    }

    [Command]
    private void CmdMoveAndPlayVFX(GameObject vfxGO, Vector3 position) 
        => RpcMoveAndPlayVFX(vfxGO, position);

    [ClientRpc]
    private void RpcMoveAndPlayVFX(GameObject vfxGO, Vector3 position)
    {
        if (vfxGO == null) return;
        var vfx = vfxGO.GetComponent<VisualEffect>();
        if (vfx == null) return;

        vfx.transform.SetParent(null);
        vfx.gameObject.SetActive(true);
        vfx.transform.position = new Vector3(position.x, _beamUpOffset, position.z);
        vfx.Stop();
        vfx.Play();
    }

    [Command]
    private void CmdDisableVFX(GameObject vfxGO) => RpcDisableVFX(vfxGO);

    [ClientRpc]
    private void RpcDisableVFX(GameObject vfxGO)
    {
        if (vfxGO == null) return;
        var vfx = vfxGO.GetComponent<VisualEffect>();
        if (vfx == null) return;

        vfx.Stop();
        Destroy(vfxGO); // уничтожаем инстанс по завершению
    }

    public void SwitchMode()
    {
        CmdSwitchMode();
    }
    
    [Command]
    private void CmdSwitchMode()
    {
        UpdateMode();
        isLightMode = !isLightMode;
    }
    
    private void OnModeChanged(bool oldValue, bool newValue)
    {
        UpdateMode();
        OnModeChange?.Invoke();
    }
    
    private void UpdateMode()
    {
        Info.School     = isLightMode ? Schools.Light : Schools.Dark;
        AbilityInfoHero = isLightMode ? lightInfo : darkInfo;
        Hero.Abilities.SkillPanelUpdate();

        Cooldown.OnForceRefreshUI();

        UpdateVFXColors();
    }
    
    private void UpdateVFXColors(VisualEffect vfx = null)
    {
        var target = vfx;
        if (target == null) return;

        Color centerColor  = isLightMode ? _lightCenterColor  : _darkCenterColor;
        Color fresnelColor = isLightMode ? _lightFresnelColor : _darkFresnelColor;
        Color voronoiColor = isLightMode ? _lightVoronoiColor : _darkVoronoiColor;

        target.SetVector4("CenterColor",  centerColor);
        target.SetVector4("FresnelColor", fresnelColor);
        target.SetVector4("VoronoiColor", voronoiColor);
    }
}
