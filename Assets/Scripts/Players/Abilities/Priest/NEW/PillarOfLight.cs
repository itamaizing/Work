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

    private float _baseDamage = 5f;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => CheckCanCast();

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    public bool IsLightMode => isLightMode;
    public event Action OnModeChange;

    private Vector3 _clickPoint;
    
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
        UpdateMode();
    }

    private void OnEnable()
    {
        OnModeChange += UpdateMode;
        
        _overhealMana = new OverhealManaBooster(this, Hero);
    }

    private void OnDisable()
    {
        OnModeChange -= UpdateMode;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0)
            _clickPoint = (Vector3)targetInfo.Points[0];
    }

    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (!GetMouseButton)
            yield return null;

        _clickPoint = Targeting.GetMousePoint();
        targetInfo.Points.Add(_clickPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        CmdPlayPillarVFX(_clickPoint);
        yield return StartCoroutine(PillarTickJob(_clickPoint));
    }

    [Command]
    private void CmdPlayPillarVFX(Vector3 position) => RpcPlayPillarVFX(position);

    private IEnumerator PillarTickJob(Vector3 position)
    {
        var targetTickCounts = new Dictionary<Character, int>();

        float elapsed = 0f;

        while (elapsed < _pillarDuration)
        {
            yield return new WaitForSeconds(_tickInterval);
            elapsed += _tickInterval;

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
                if (!currentTargets.Contains(kvp.Key))
                    toReset.Add(kvp.Key);

            foreach (var target in toReset)
                targetTickCounts.Remove(target);

            foreach (var target in currentTargets)
            {
                if (!targetTickCounts.ContainsKey(target))
                    targetTickCounts[target] = 0;

                targetTickCounts[target]++;
                int ticks = targetTickCounts[target];

                _damageValue = _baseDamage + _damageIncreasePerTick * (ticks - 1);

                if (IsEnemyTarget(target))
                {
                    Damage damage = new Damage
                    {
                        Value = Buff.Damage.GetBuffedValue(_damageValue),
                        Type  = Info.DamageType,
                        School = Info.School
                    };
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
                    _overhealMana.OnAnyHealTaken(target,heal.Value,this);
                }
            }
        }

        _damageValue = _baseDamage;

        CmdStopPillarVFX();
    }

    [Command]
    private void CmdStopPillarVFX() => RpcStopPillarVFX();

    [ClientRpc]
    private void RpcPlayPillarVFX(Vector3 position)
    {
        if (_pillarVFX == null) return;

        _pillarVFX.transform.SetParent(null);
        _pillarVFX.transform.position = new Vector3(position.x, _pillarUpOffset, position.z);
        _pillarVFX.gameObject.SetActive(true);
        _pillarVFX.Stop();
        _pillarVFX.Play();
    }

    [ClientRpc]
    private void RpcStopPillarVFX()
    {
        if (_pillarVFX == null) return;

        _pillarVFX.Stop();
        _pillarVFX.gameObject.SetActive(false);
        _pillarVFX.transform.SetParent(transform);
    }

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

        UpdateVFXColors();
    }

    private void UpdateVFXColors()
    {
        if (_pillarVFX == null) return;
        Color frontColor = isLightMode ? _lightColor : _darkColor;

        _pillarVFX.SetVector4("Color", frontColor);
    }
}
