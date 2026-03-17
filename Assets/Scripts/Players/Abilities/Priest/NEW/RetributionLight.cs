using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class RetributionLight : Skill,IPolaritySwitchable
{
    [Header("Effect Settings")]
    [SerializeField] private VisualEffect _lightVFX;
    [SerializeField] private float _aoeRadius = 1.5f;
    [SerializeField] private float _beamUpOffset = 8f;
    [SerializeField] private float _delayBeforeFirstBeam = 1f;
    [SerializeField] private float _delayBetweenBeams = 1f;
    
    [Header("Mode info")]
    [SerializeField] private AbilityInfo lightInfo;
    [SerializeField] private AbilityInfo darkInfo;

    [Header("AOE Preview")]
    [SerializeField] private GameObject _damageCirclePrefab;

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
    
    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    
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

    private void ClearPoints()
    {
        _clickPoint      = Vector3.zero;
        _castOriginPoint = Vector3.zero;
        DestroyCirclePreview(ref _damageCircleInstance);
        DestroyCirclePreview(ref _healCircleInstance);
    }
    
    private void OnEnable()
    {
        OnModeChange += UpdateMode;
        UpdateMode();
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
        if (_lightVFX != null)
            _lightVFX.outputEventReceived -= OnVFXOutputEvent;

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
        _cachedDamageDealt = 0f;
        _isDamagePhase     = true;

        if (_lightVFX != null)
            _lightVFX.outputEventReceived += OnVFXOutputEvent;
        
        SpawnOrMoveCircle(ref _damageCircleInstance, _damageCirclePrefab, _clickPoint);
        SpawnOrMoveCircle(ref _healCircleInstance,   _damageCirclePrefab,   _castOriginPoint);

        yield return new WaitForSeconds(_delayBeforeFirstBeam);

        CmdMoveAndPlayVFX(_clickPoint);

        yield return null;
    }

    private void OnVFXOutputEvent(VFXOutputEventArgs args)
    {
        if (args.nameId == _onFinishedEventId)
        {
            if (_isDamagePhase) OnDamageVFXFinished();
            else                OnHealVFXFinished();
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

                OnHealVFXDecalsEnded();
            }
        }
    }

    private void OnDamageVFXFinished()
    {
        if (!isOwned) return;

        DestroyCirclePreview(ref _damageCircleInstance);

        _cachedDamageDealt  = ApplyAreaDamage(_clickPoint);
        _firstBeamDecalsEnded = false;
        _isDamagePhase      = false;

        StartCoroutine(DelayedHealBeam());
    }
    
    private IEnumerator DelayedHealBeam()
    {
        Vector3 originPoint = _castOriginPoint;
        
        SpawnOrMoveCircle(ref _healCircleInstance, _damageCirclePrefab, originPoint);
    
        yield return new WaitForSeconds(_delayBetweenBeams);

        CmdMoveAndPlayVFX(originPoint);
    }

    private void OnHealVFXFinished()
    {
        if (!isOwned) return;

        DestroyCirclePreview(ref _healCircleInstance);

        if (isLightMode)
            ApplyAreaHeal(_castOriginPoint, _cachedDamageDealt);
        else
            ApplyAreaDarkDamage(_castOriginPoint, _cachedDamageDealt);
    }

    private void OnHealVFXDecalsEnded()
    {
        CmdDisableVFX();
        ClearPoints();
    }

    private float ApplyAreaDamage(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, _aoeRadius, Targeting.Layer);

        var targets = new List<GameObject>();
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (!IsEnemyTarget(target)) continue;
            if (target.IsDead) continue;

            targets.Add(target.gameObject);
        }

        CmdApplyDamageAndCalculate(position, targets.ToArray());

        return 0f;
    }
    
    [Command]
    private void CmdApplyDamageAndCalculate(Vector3 position, GameObject[] targets)
    {
        float totalDamage = 0f;

        foreach (var targetGO in targets)
        {
            if (targetGO == null) continue;
            if (!targetGO.TryGetComponent<Character>(out var target)) continue;
            if (target.IsDead) continue;

            float damageValue = Buff.Damage.GetBuffedValue(Damage);

            Damage damage = new Damage
            {
                Value = damageValue,
                Type  = Info.DamageType,
            };

            float healthBefore = target.Health.CurrentValue;

            ApplyDamage(damage, targetGO);

            if (target.IsDead) continue;

            float actualDamage = healthBefore - target.Health.CurrentValue;
            if (actualDamage > 0f)
                totalDamage += actualDamage;
        }

        TargetRpcOnDamageCalculated(connectionToClient, totalDamage);
    }
    
    [TargetRpc]
    private void TargetRpcOnDamageCalculated(NetworkConnectionToClient target, float totalDamage)
    {
        _cachedDamageDealt = totalDamage;

        _isDamagePhase = false;
        StartCoroutine(DelayedHealBeam());
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
            if (IsEnemyTarget(target)) continue;
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
            };

            CmdApplyDamage(damage, enemy.gameObject);
        }
    }

    [Command]
    private void CmdMoveAndPlayVFX(Vector3 position) => RpcMoveAndPlayVFX(position);

    [ClientRpc]
    private void RpcMoveAndPlayVFX(Vector3 position)
    {
        if (_lightVFX == null) return;

        _lightVFX.transform.SetParent(null);
        _lightVFX.gameObject.SetActive(true);
        _lightVFX.transform.position = new Vector3(position.x, _beamUpOffset, position.z);
        _lightVFX.Stop();
        _lightVFX.Play();
    }

    [Command]
    private void CmdDisableVFX() => RpcDisableVFX();

    [ClientRpc]
    private void RpcDisableVFX()
    {
        if (_lightVFX == null) return;

        _lightVFX.outputEventReceived -= OnVFXOutputEvent;
        _lightVFX.Stop();
        _lightVFX.gameObject.SetActive(false);
        _lightVFX.transform.SetParent(transform);
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
        Info.School = isLightMode ? Schools.Light : Schools.Dark;
        AbilityInfoHero = isLightMode ? lightInfo : darkInfo;
        Hero.Abilities.SkillPanelUpdate();
    }
}
