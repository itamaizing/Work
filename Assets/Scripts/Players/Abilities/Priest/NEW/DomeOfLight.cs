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

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("DomeOfLight");
    protected override bool IsCanCast => true;

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    public event Action OnModeChange;
    
    public void AnimCastDome() => AnimStartCastCoroutine();
    public void AnimDomeEnd()  => AnimCastEnded();

    private bool IsEnemyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private bool IsAllyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public override void LoadTargetData(TargetInfo targetInfo) { }

    private void OnEnable()
    {
        OnModeChange += UpdateMode;
        UpdateMode();
    }

    private void OnDisable()
    {
        OnModeChange -= UpdateMode;
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
            CheckDomeHits(Mathf.Lerp(0f, _maxRadius, t), hitTargets);

            yield return new WaitForEndOfFrame();
        }

        CmdSetDomeScale(_maxRadius * 2);
        CheckDomeHits(_maxRadius, hitTargets);

        yield return new WaitForSeconds(0.5f);

        CmdSetDomeActive(false);
        CmdSetDomeScale(0f);
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

    private void CheckDomeHits(float currentRadius, HashSet<Character> hitTargets)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, Targeting.Layer);

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
                    Type  = Info.DamageType,
                };
                CmdApplyDamage(damage, target.gameObject);
            }
            else if (isLightMode && IsAllyTarget(target) && target != Hero)
            {
                var heal = new Heal 
                {
                    Value = _damageValue,
                    DamageableSkill = this
                };
                CmdApplyHeal(heal,target.gameObject,this,nameof(DomeOfLight));
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
        Info.School = isLightMode ? Schools.Light : Schools.Dark;
        AbilityInfoHero = isLightMode ? lightInfo : darkInfo;
        Hero.Abilities.SkillPanelUpdate();
    }
}
