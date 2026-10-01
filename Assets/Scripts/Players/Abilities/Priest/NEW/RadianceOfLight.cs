using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class RadianceOfLight : Skill, IPolaritySwitchable
{
    [Header("Effect Settings")]
    [SerializeField] private ParticleSystem _radianceVFX;
    [SerializeField] private float _aoeRadius = 2f;
    [SerializeField] private float _tickInterval = 1f;

    [Header("VFX Colors")]
    [ColorUsage(true, true)] [SerializeField] private Color _lightColor1 = Color.white;
    [ColorUsage(true, true)] [SerializeField] private Color _darkColor1  = Color.yellow;

    [Header("Mode info")]
    [SerializeField] private AbilityInfo lightInfo;
    [SerializeField] private AbilityInfo darkInfo;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    public bool IsLightMode => isLightMode;
    public event Action OnModeChange;

    private bool IsEnemyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private bool IsAllyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Allies");
    
    #region OverhealManaBooster

    private OverhealManaBooster _overhealMana;
    public OverhealManaBooster OverhealManaBooster => _overhealMana;

    #endregion

    public override void LoadTargetData(TargetInfo targetInfo) { }

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

    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(_hero);
        callbackDataSaved(targetInfo);
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        CmdPlayVFX();

        float elapsed = 0f;
        float tickAccumulator = 0f;

        while (elapsed < Channeling.CastDuration)
        {
            elapsed         += Time.deltaTime;
            tickAccumulator += Time.deltaTime;

            if (tickAccumulator >= _tickInterval)
            {
                tickAccumulator -= _tickInterval;
                ApplyRadianceTick();
            }

            yield return null;
        }

        CmdStopVFX();
    }

    private void ApplyRadianceTick()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _aoeRadius, Targeting.Layer);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (target.IsDead) continue;

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
            else if (isLightMode && IsAllyTarget(target) && target != Hero)
            {
                Heal heal = new Heal
                {
                    Value           = _damageValue,
                    DamageableSkill = this,
                };
                CmdApplyHeal(heal, target.gameObject, this, nameof(RadianceOfLight));
                _overhealMana.OnAnyHealTaken(target,heal.Value,this);
            }
        }
    }

    private void UpdateParticleColors()
    {
        SetParticleColor(_radianceVFX,  isLightMode ? _lightColor1 : _darkColor1);
    }

    private void SetParticleColor(ParticleSystem ps, Color color)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startColor = color;
    }

    [Command]
    private void CmdPlayVFX() => RpcPlayVFX();

    [ClientRpc]
    private void RpcPlayVFX()
    {
        UpdateParticleColors();
        PlayParticle(_radianceVFX);
    }

    [Command]
    private void CmdStopVFX() => RpcStopVFX();

    [ClientRpc]
    private void RpcStopVFX()
    {
        StopParticle(_radianceVFX);
    }

    private void PlayParticle(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.gameObject.SetActive(true);
        ps.Stop();
        ps.Play();
    }

    private void StopParticle(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop();
        ps.gameObject.SetActive(false);
    }

    public void SwitchMode() => CmdSwitchMode();

    [Command]
    private void CmdSwitchMode() => isLightMode = !isLightMode;

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

        UpdateParticleColors();
    }
}
