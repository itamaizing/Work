using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class DomeOfLight : Skill, IPolaritySwitchable
{
    [Header("Effect Settings")]
    [SerializeField] private ParticleSystem _effectObject;
    [SerializeField] private float _expandDuration = 0.3f;
    [SerializeField] private float _maxRadius = 2f;

    [Header("Mode info")]
    [SerializeField] private AbilityInfo lightInfo;
    [SerializeField] private AbilityInfo darkInfo;
    
    [Header("VFX Colors")]
    [SerializeField] private Color _lightColor = Color.white;
    [SerializeField] private Color _darkColor  = Color.yellow;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("DomeOfLight");
    protected override bool IsCanCast => true;

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    
    public bool IsLightMode => isLightMode;
    public event Action OnModeChange;
    
    public void AnimCastDome() => AnimStartCastCoroutine();
    public void AnimDomeEnd() { }

    private bool IsEnemyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private bool IsAllyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Allies");
    
    #region OverhealManaBooster

    private OverhealManaBooster _overhealMana;
    public OverhealManaBooster OverhealManaBooster => _overhealMana;

    #endregion
    
    #region Dome Proc Talent
    private DomeProcBooster _domeProcBooster;
    public DomeProcBooster DomeProcBooster => _domeProcBooster;
    #endregion

    public override void LoadTargetData(TargetInfo targetInfo) { }
    
    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        
        UpdateMode();
        
        _overhealMana = new OverhealManaBooster(this, Hero);
        _domeProcBooster = new DomeProcBooster(this, this);
    }
    

    private void OnEnable()
    {
        OnModeChange += UpdateMode;
    }

    private void OnDisable()
    {
        OnModeChange -= UpdateMode;
    }
    
    [Command]
    public void CmdSpawnTemporaryDome(Vector3 centerPosition)
    {
        RpcSpawnTemporaryDome(centerPosition);
    }
    
    [ClientRpc]
    private void RpcSpawnTemporaryDome(Vector3 centerPosition)
    {
        if (_effectObject == null) return;

        ParticleSystem tempEffect = Instantiate(_effectObject, centerPosition, Quaternion.identity);

        var main = tempEffect.main;
        main.startColor = isLightMode ? _lightColor : _darkColor;

        StartCoroutine(RunTemporaryDomeCoroutine(tempEffect));
    }

    private IEnumerator RunTemporaryDomeCoroutine(ParticleSystem tempEffect)
    {
        if (tempEffect == null) yield break;

        tempEffect.gameObject.SetActive(true);

        var hitTargets = new HashSet<Character>();
        float elapsed = 0f;

        while (elapsed < _expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _expandDuration;
            float currentRadius = Mathf.Lerp(0f, _maxRadius, t);
            float currentScale = currentRadius * 2f;

            tempEffect.transform.localScale = Vector3.one * currentScale;

            CheckDomeHitsAtPosition(tempEffect.transform.position, currentRadius, hitTargets);

            yield return null;
        }

        tempEffect.transform.localScale = Vector3.one * (_maxRadius * 2f);
        CheckDomeHitsAtPosition(tempEffect.transform.position, _maxRadius, hitTargets);

        yield return new WaitForSeconds(0.5f);

        Destroy(tempEffect.gameObject);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(_hero);
        callbackDataSaved(targetInfo);
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        CmdSetDomeActive(true);
    
        var hitTargets = new HashSet<Character>();
        float elapsed = 0f;

        while (elapsed < _expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _expandDuration;
            float currentScale = Mathf.Lerp(0f, _maxRadius * 2, t);

            CmdSetDomeScale(currentScale);
            CheckDomeHitsAtPosition(transform.position, Mathf.Lerp(0f, _maxRadius, t), hitTargets);

            yield return new WaitForEndOfFrame();
        }

        CmdSetDomeScale(_maxRadius * 2);
        CheckDomeHitsAtPosition(transform.position, _maxRadius, hitTargets);
        ApplyProcAfterFullExpansion(hitTargets);
        CmdSetDomeActive(false);
        CmdSetDomeScale(0f);
        AnimCastEnded();
        yield return null;
    }

    [Command]
    private void CmdSetDomeActive(bool value) => RpcSetDomeActive(value);

    [ClientRpc]
    private void RpcSetDomeActive(bool value)
    {
        if (_effectObject == null) return;
        _effectObject.gameObject.SetActive(value);
        if (!value) _effectObject.transform.localScale = Vector3.one;
    }

    [Command]
    private void CmdSetDomeScale(float scale) => RpcSetDomeScale(scale);

    [ClientRpc]
    private void RpcSetDomeScale(float scale)
    {
        if (_effectObject == null) return;
        _effectObject.transform.localScale = Vector3.one * scale;
    }

    private void CheckDomeHitsAtPosition(Vector3 position, float radius, HashSet<Character> hitTargets)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius, Targeting.Layer);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (target.IsDead) continue;
            if (hitTargets.Contains(target)) continue;

            hitTargets.Add(target);

            if (IsEnemyTarget(target))
            {
                Damage damage = new Damage
                {
                    Value = Buff.Damage.GetBuffedValue(_damageValue),
                    Type = Info.DamageType,
                    School = Info.School
                };
                CmdApplyDamage(damage, target.gameObject);
            }
            else if (isLightMode && IsAllyTarget(target) && target != Hero)
            {
                var heal = new Heal { Value = _damageValue, DamageableSkill = this };
                CmdApplyHeal(heal, target.gameObject, this, nameof(DomeOfLight));
                _overhealMana.OnAnyHealTaken(target, heal.Value, this);
            }
        }
    }
    
    private void ApplyProcAfterFullExpansion(HashSet<Character> healedTargets)
    {
        if (_domeProcBooster == null || !isLightMode) return;

        foreach (var target in healedTargets)
        {
            if (IsAllyTarget(target) && target != Hero)
            {
                _domeProcBooster.TryProcFromHeal(target);
            }
        }
    }

    protected override void ClearData() { }

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

        UpdateParticleColor();
    }
    
    private void UpdateParticleColor()
    {
        if (_effectObject == null) return;

        var main = _effectObject.main;
        main.startColor = isLightMode ? _lightColor : _darkColor;
    }
}
