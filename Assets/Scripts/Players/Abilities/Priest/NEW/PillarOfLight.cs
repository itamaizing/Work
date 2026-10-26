using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class PillarOfLight : Skill, IPolaritySwitchable
{
    [Header("Effect Settings")]
    [SerializeField] private VisualEffect _pillarVFX;
    [SerializeField] private float _pillarDuration = 6f;
    [SerializeField] private float _pillarUpOffset = 6f;
    [SerializeField] private float _tickInterval = 1f;
    [SerializeField] private float _damageIncreasePerTick = 5f;
    [SerializeField] private float _aoeRadius = 1f;

    [Header("Mode info")]
    [SerializeField] private AbilityInfo lightInfo;
    [SerializeField] private AbilityInfo darkInfo;

    [Header("VFX Colors")]
    [SerializeField][ColorUsage(true, true)] private Color _lightColor = Color.white;
    [SerializeField][ColorUsage(true, true)] private Color _darkColor  = Color.yellow;

    private const float MinTickInterval = 0.05f;

    //private float _baseDamage = 5f;
    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => CheckCanCast();

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    public bool IsLightMode => isLightMode;
    public event Action OnModeChange;

    private Vector3 _clickPoint;
    
    private readonly List<VisualEffect> _activeVfx = new();

    #region OverhealManaBooster

    private OverhealManaBooster _overhealMana;
    public OverhealManaBooster OverhealManaBooster => _overhealMana;

    #endregion

    private bool CheckCanCast() =>
        Vector3.Distance(_clickPoint, transform.position) <= AreaInfo.Radius;

    private bool IsEnemyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private bool IsAllyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        if (_pillarVFX != null) _pillarVFX.gameObject.SetActive(false);

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
        DestroyActiveVfx();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0)
            _clickPoint = (Vector3)targetInfo.Points[0];
    }

    protected override void ClearData() { }

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
        Vector3 point = _clickPoint;

        float tick = Mathf.Max(_tickInterval, MinTickInterval);
        int ticks = Mathf.Max(1, Mathf.CeilToInt(_pillarDuration / tick));
        float lifetime = ticks * tick;

        CmdSpawnPillarVFX(point, lifetime);
        StartCoroutine(PillarTickJob(point, tick, ticks));

        yield break;
    }

    private IEnumerator PillarTickJob(Vector3 position, float tick, int tickCount)
    {
        var targetTickCounts = new Dictionary<Character, int>();

        for (int i = 0; i < tickCount; i++)
        {
            yield return new WaitForSeconds(tick);

            Collider[] hits = Physics.OverlapSphere(position, _aoeRadius, Targeting.Layer);

            var currentTargets = new HashSet<Character>();
            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<Character>(out var target)) continue;
                if (target.IsDead) continue;
                currentTargets.Add(target);
            }

            var toReset = new List<Character>();
            foreach (var kvp in targetTickCounts)
                if (kvp.Key == null || !currentTargets.Contains(kvp.Key))
                    toReset.Add(kvp.Key);

            foreach (var target in toReset)
                targetTickCounts.Remove(target);

            foreach (var target in currentTargets)
            {
                if (!targetTickCounts.ContainsKey(target))
                    targetTickCounts[target] = 0;

                targetTickCounts[target]++;
                int ticks = targetTickCounts[target];

                _damageValue = _baseDamage.GetValue() + _damageIncreasePerTick * (ticks - 1);

                if (IsEnemyTarget(target))
                {
                    Damage damage = GetDamage();
                    damage.Value = CalculateDamage(_damageValue);
                    CmdApplyDamage(damage, target.gameObject);
                }
                else if (isLightMode && IsAllyTarget(target))
                {
                    Heal heal = new Heal
                    {
                        Value           = _damageValue,
                        DamageableSkill = this,
                    };
                    CmdApplyHeal(heal, target.gameObject, this, nameof(PillarOfLight));
                    _overhealMana.OnAnyHealTaken(target, heal.Value, this);
                }
            }
        }

        _damageValue = _baseDamage.GetValue();
    }

    #region VFX (по копии на столб, каждая живёт и уничтожается сама)

    [Command]
    private void CmdSpawnPillarVFX(Vector3 position, float lifetime) => RpcSpawnPillarVFX(position, lifetime);

    [ClientRpc]
    private void RpcSpawnPillarVFX(Vector3 position, float lifetime)
    {
        if (_pillarVFX == null) return;

        var vfx = Instantiate(_pillarVFX, transform);
        vfx.transform.SetParent(null);
        vfx.transform.position = new Vector3(position.x, _pillarUpOffset, position.z);
        vfx.gameObject.SetActive(true);
        ApplyColor(vfx);
        vfx.Play();

        _activeVfx.Add(vfx);
        Destroy(vfx.gameObject, lifetime);
    }

    private void DestroyActiveVfx()
    {
        foreach (var vfx in _activeVfx)
            if (vfx != null) Destroy(vfx.gameObject);
        _activeVfx.Clear();
    }

    private void ApplyColor(VisualEffect vfx)
    {
        if (vfx == null) return;
        vfx.SetVector4("Color", isLightMode ? _lightColor : _darkColor);
    }

    private void UpdateVFXColors()
    {
        _activeVfx.RemoveAll(v => v == null);
        foreach (var vfx in _activeVfx) ApplyColor(vfx);
    }

    #endregion

    public void SwitchMode() => CmdSwitchMode();

    [Command]
    private void CmdSwitchMode()
    {
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
}