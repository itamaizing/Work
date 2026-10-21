using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

public class RetributionLight : Skill, IPolaritySwitchable
{
    [Header("Effect Settings")]
    [SerializeField] private VisualEffect _lightVFXPrefab;
    [SerializeField] private float _aoeRadius = 1.5f;
    [SerializeField] private float _beamUpOffset = 8f;
    [SerializeField] private float _delayBeforeFirstBeam = 1f;
    [SerializeField] private float _delayBetweenBeams = 1f;
    [SerializeField] private float _maxCastLifetime = 15f;

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
    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("RetributionLight");

    private Vector3 _clickPoint;

    private class BeamCast
    {
        public int Id;
        public Vector3 Click;
        public Vector3 Origin;
        public bool IsLight;
        public Schools School;
        public bool DamagePhase = true;
        public bool DamageResultReceived;
        public float DamageDealt;
        public int BeamEnds;
        public bool Finished;
        public VisualEffect Vfx;
        public GameObject DamageCircle;
        public GameObject HealCircle;
    }

    private readonly Dictionary<int, BeamCast> _casts = new();
    private int _nextCastId;

    private readonly Dictionary<int, GameObject> _serverVfx = new();

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

    protected override void ClearData() { }

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

        foreach (var cast in _casts.Values)
        {
            cast.Finished = true;
            DestroyCircle(ref cast.DamageCircle);
            DestroyCircle(ref cast.HealCircle);
        }
        _casts.Clear();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        foreach (var go in _serverVfx.Values)
            if (go != null) NetworkServer.Destroy(go);
        _serverVfx.Clear();
    }

    #region Preview circles

    private GameObject CreateCircle(Vector3 position)
    {
        if (_damageCirclePrefab == null) return null;

        var go = Instantiate(_damageCirclePrefab, position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        return go;
    }

    private static void DestroyCircle(ref GameObject instance)
    {
        if (instance == null) return;
        Destroy(instance);
        instance = null;
    }

    #endregion

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (!GetMouseButton)
            yield return null;

        targetInfo.Points.Add(Targeting.GetMousePoint());
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        var cast = new BeamCast
        {
            Id = ++_nextCastId,
            Click = _clickPoint,
            Origin = transform.position,
            IsLight = isLightMode,
            School = Info.School
        };
        _casts[cast.Id] = cast;

        cast.DamageCircle = CreateCircle(cast.Click);
        cast.HealCircle = CreateCircle(cast.Origin);

        StartCoroutine(BeamSequence(cast));
        yield break;
    }

    private IEnumerator BeamSequence(BeamCast cast)
    {
        yield return new WaitForSeconds(_delayBeforeFirstBeam);
        if (cast.Finished) yield break;

        CmdSpawnAndPlayVFX(cast.Id, cast.Click, cast.IsLight);

        yield return new WaitForSeconds(_maxCastLifetime);
        FinishCast(cast);
    }

    private void FinishCast(BeamCast cast)
    {
        if (cast.Finished) return;
        cast.Finished = true;

        DestroyCircle(ref cast.DamageCircle);
        DestroyCircle(ref cast.HealCircle);
        _casts.Remove(cast.Id);

        CmdDisableVFX(cast.Id);
    }

    #region VFX (server owns the instance, lifetime is controlled per cast)

    [Command]
    private void CmdSpawnAndPlayVFX(int castId, Vector3 clickPoint, bool lightMode)
    {
        if (_lightVFXPrefab == null) return;

        VisualEffect vfxInstance = Instantiate(_lightVFXPrefab);
        SceneManager.MoveGameObjectToScene(vfxInstance.gameObject, gameObject.scene);
        NetworkServer.Spawn(vfxInstance.gameObject, connectionToClient);

        if (_serverVfx.TryGetValue(castId, out var old) && old != null)
            NetworkServer.Destroy(old);
        _serverVfx[castId] = vfxInstance.gameObject;

        StartCoroutine(ServerVfxWatchdog(castId, vfxInstance.gameObject));

        RpcSetupVFX(castId, vfxInstance.gameObject, clickPoint, lightMode);
    }

    private IEnumerator ServerVfxWatchdog(int castId, GameObject go)
    {
        yield return new WaitForSeconds(_maxCastLifetime);
        DestroyServerVfx(castId, go);
    }

    private void DestroyServerVfx(int castId, GameObject go)
    {
        if (go != null) NetworkServer.Destroy(go);
        if (_serverVfx.TryGetValue(castId, out var current) && current == go)
            _serverVfx.Remove(castId);
    }

    [Command]
    private void CmdDisableVFX(int castId)
    {
        if (_serverVfx.TryGetValue(castId, out var go))
            DestroyServerVfx(castId, go);
    }

    [ClientRpc]
    private void RpcSetupVFX(int castId, GameObject vfxGO, Vector3 clickPoint, bool lightMode)
    {
        if (vfxGO == null) return;

        var vfx = vfxGO.GetComponent<VisualEffect>();
        if (vfx == null) return;

        UpdateVFXColors(vfx, lightMode);

        if (isOwned && _casts.TryGetValue(castId, out var cast))
        {
            cast.Vfx = vfx;
            vfx.outputEventReceived += args => OnVFXOutputEvent(args, cast);
        }

        vfx.transform.SetParent(null);
        vfx.transform.position = new Vector3(clickPoint.x, _beamUpOffset, clickPoint.z);
        vfx.Stop();
        vfx.Play();
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

    private void UpdateVFXColors(VisualEffect vfx, bool lightMode)
    {
        if (vfx == null) return;

        vfx.SetVector4("CenterColor",  lightMode ? _lightCenterColor  : _darkCenterColor);
        vfx.SetVector4("FresnelColor", lightMode ? _lightFresnelColor : _darkFresnelColor);
        vfx.SetVector4("VoronoiColor", lightMode ? _lightVoronoiColor : _darkVoronoiColor);
    }

    #endregion

    #region Beam events (owner)

    private void OnVFXOutputEvent(VFXOutputEventArgs args, BeamCast cast)
    {
        if (cast.Finished) return;

        if (args.nameId == _onFinishedEventId)
        {
            if (cast.DamagePhase) OnDamageHit(cast);
            else                  OnHealHit(cast);
        }
        else if (args.nameId == _onDecalsEventId)
        {
            cast.BeamEnds++;
            if (cast.BeamEnds >= 2) FinishCast(cast);
        }
    }

    private void OnDamageHit(BeamCast cast)
    {
        cast.DamagePhase = false;
        DestroyCircle(ref cast.DamageCircle);

        ApplyAreaDamage(cast);
        StartCoroutine(DelayedHealBeam(cast));
    }

    private IEnumerator DelayedHealBeam(BeamCast cast)
    {
        yield return new WaitForSeconds(_delayBetweenBeams);
        if (cast.Finished) yield break;

        if (cast.Vfx == null)
        {
            FinishCast(cast);
            yield break;
        }

        CmdMoveAndPlayVFX(cast.Vfx.gameObject, cast.Origin);
    }

    private void OnHealHit(BeamCast cast)
    {
        DestroyCircle(ref cast.HealCircle);
        StartCoroutine(ApplyReturnBeam(cast));
    }

    private IEnumerator ApplyReturnBeam(BeamCast cast)
    {
        float timeout = 2f;
        while (!cast.DamageResultReceived && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (cast.IsLight) ApplyAreaHeal(cast.Origin, cast.DamageDealt);
        else              ApplyAreaDarkDamage(cast.Origin, cast.DamageDealt, cast.School);
    }

    #endregion

    #region Damage / heal

    private void ApplyAreaDamage(BeamCast cast)
    {
        Collider[] hits = Physics.OverlapSphere(cast.Click, _aoeRadius, Targeting.Layer);

        var targets = new List<GameObject>();
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (!IsEnemyTarget(target)) continue;
            if (target.IsDead) continue;
            targets.Add(target.gameObject);
        }
        
        CmdApplyDamageAndCalculate(cast.Id, targets.ToArray(), cast.School);
    }

    [Command]
    private void CmdApplyDamageAndCalculate(int castId, GameObject[] targets, Schools school)
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
                School = school
            };
            
            float healthBefore = target.Health.CurrentValue;
            ApplyDamage(damage, targetGO);

            if (target.IsDead) continue;

            float dealt = healthBefore - target.Health.CurrentValue;
            if (dealt > 0f) totalDamage += dealt;
        }

        TargetRpcOnDamageCalculated(connectionToClient, castId, totalDamage);
    }

    [TargetRpc]
    private void TargetRpcOnDamageCalculated(NetworkConnectionToClient conn, int castId, float totalDamage)
    {
        if (!_casts.TryGetValue(castId, out var cast)) return;

        cast.DamageDealt = totalDamage;
        cast.DamageResultReceived = true;
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
            CmdApplyHeal(heal, ally.gameObject, this, nameof(RetributionLight));
            _overhealMana.OnAnyHealTaken(ally, heal.Value, this);
        }
    }

    private void ApplyAreaDarkDamage(Vector3 position, float totalDamage, Schools school)
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
                Value  = damagePerTarget,
                Type   = Info.DamageType,
                School = school
            };

            CmdApplyDamage(damage, enemy.gameObject);
        }
    }

    #endregion

    #region Polarity

    public void SwitchMode()
    {
        CmdSwitchMode();
    }

    [Command]
    private void CmdSwitchMode()
    {
        isLightMode = !isLightMode;
        UpdateMode();
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
    }

    #endregion
}