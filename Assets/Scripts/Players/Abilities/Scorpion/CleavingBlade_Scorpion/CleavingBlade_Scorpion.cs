using Mirror;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class CleavingBlade_Scorpion : Skill,IComboParticipatingSkill,ISwordSkill
{
    [Header("Ability settings")]
    [SerializeField] private ScorpionPassive _scorpionPassive;
    [SerializeField] [Range(0, 100)] private float _minDamage = 18f;
    [SerializeField] [Range(0, 100)] private float _maxDamage = 26f;
    [SerializeField] private GameObject _blade;

    [SyncVar] private int _counter = 1;
    
    public event Action<GameObject, Skill> OnDamaged;

    private float _pendingFireDamageBonus = 0f;
    private float _pendingScorchedSoulChance = 0f;
    
    #region Const
    private const float BleedingDuration = 9f;
    private const float BaseDamageBaf = 2f;
    private const int MaxComboCounter = 3;
    private const float DefaultAnimSpeed = 1f;
    private const float ReducedAnimSpeed = 0.8f;
    private const float DefaultDamageMultiplier = 1f;
    private const float SearchTargetInRadius = 1f;
    private const float ShouldIncreaseCounter = 2f;
    #endregion

    private bool isCleavingBlade_ScorpionSecondTalent;
    private bool _wasDamageApplied = false;

    public float DamageRange => Random.Range(_minDamage, _maxDamage);

    protected override bool IsCanCast => Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Cast Blade");

    private void OnDisable() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;

    private void HandleSkillCanceled()
    {
        _wasDamageApplied = false;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget(targetInfo.GetTargets()[0]);
    }
    
    public void AddFireBonus(float damagePercent, float scorchedChance)
    {
        _pendingFireDamageBonus += damagePercent;
        _pendingScorchedSoulChance += scorchedChance;
    }

    private void AttackPassed(bool shouldIncreaseCounter, Character target)
    {
        OnDamaged?.Invoke(target.gameObject,this);

        if (shouldIncreaseCounter)
        {
            _counter = _counter == MaxComboCounter ? 1 : _counter + 1;
        }
    }
    
    public void OnFinalComboSkill(GameObject target)
    {
        CharacterState state = target.GetComponent<CharacterState>();

        if (state != null)
        {
            state.AddState(States.Bleeding, BleedingDuration, BaseDamageBaf, _hero.gameObject, name);
        }
    }

    public void OnTargetHasComboPoint(GameObject target, float comboPoints)
    {
        CharacterState state = target.GetComponent<CharacterState>();

        if (state != null)
        {
            for (int i = 0; i < comboPoints; i++) state.AddState(States.Bleeding, BleedingDuration, BaseDamageBaf, _hero.gameObject, name);
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _wasDamageApplied = false;

        while (Targeting.GetTempTarget()?.Targetable == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), SearchTargetInRadius);

                if (Targeting.GetTempTarget()?.Targetable != null && Targeting.GetTempTarget()?.Targetable is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) Targeting.ClearTempTarget();

                    else
                    {
                        _hero.Move.LookAtTransform(Targeting.GetTempTarget()?.Targetable.Transform);
                        if (Targeting.GetTempTarget()?.Targetable is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Targetable);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Targetable);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        TryAttack(true, DefaultDamageMultiplier);
        yield return null;
    }

    private void SpeedAnimBlade_Scorpion()
    {
        float speed = DefaultAnimSpeed;

        if (isCleavingBlade_ScorpionSecondTalent && _counter == ShouldIncreaseCounter) speed = ReducedAnimSpeed;

        _hero.Animator.SetFloat("CastChainBladeSpeed", speed);
    }

    private void TryAttack(bool shouldIncreaseCounter, float damageMultiplier)
    {
        if (_wasDamageApplied) return;

        var targetData = Targeting.GetTarget();
        if (targetData == null) return;

        var target = targetData.Targetable as IDamageable;
        if (target == null) return;

        if (Vector3.Distance(transform.position, targetData.Transform.position) > AreaInfo.Radius) return;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(DamageRange * damageMultiplier),
            Type = Info.DamageType,
        };

        _wasDamageApplied = true;

        CmdAttack(damage, target.gameObject, shouldIncreaseCounter,0);
        
        float bonus = _pendingFireDamageBonus;
        float scorchedChance = _pendingScorchedSoulChance;
        _pendingFireDamageBonus = 0f;
        _pendingScorchedSoulChance = 0f;

        Damage additionalDamage = new Damage
        {
            Value = damage.Value * bonus,
            Type = Info.DamageType,
            School = Schools.Fire
        };

        if (additionalDamage.Value > 0)
        {
            CmdAttack(additionalDamage, target.gameObject, false, scorchedChance);
        }
    }

    [Command]
    private void CmdAttack(Damage damage, GameObject target, bool shouldIncreaseCounter, float scorchedChance)
    {
        if (target == null) return;
        var damageable = target.GetComponent<IDamageable>();
        if (damageable == null) return;

        bool result = damageable.TryTakeDamage(ref damage, this);
        if (result && damageable is Character character)
        {
            AttackPassed(shouldIncreaseCounter, character);
            if (scorchedChance > 0f && Random.Range(0f, 100f) <= scorchedChance)
                character.CharacterState.AddState(States.ScorchedSoul, 5f, 0f, _hero.gameObject, name);
        }
    }

    protected override void ClearData()
    {
        _wasDamageApplied = false;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        AnimCastEnded();
    }

    public void BladeActive()
    {
        _blade.SetActive(true);
        SpeedAnimBlade_Scorpion();
    }

    public void CleavingBlade_ScorpionCast()
    {
        AnimStartCastCoroutine();
    }

    public void CleavingBlade_ScorpionEnd()
    {
        AnimCastEnded();
        _blade.SetActive(false);
    }

    public void CleavingBlade_ScorpionSecondTalent(bool value)
    {
        isCleavingBlade_ScorpionSecondTalent = value;
    }
}
