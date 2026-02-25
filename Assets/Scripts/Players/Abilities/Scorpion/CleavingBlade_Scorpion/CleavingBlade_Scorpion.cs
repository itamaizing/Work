using Mirror;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class CleavingBlade_Scorpion : Skill
{
    [Header("Ability settings")]
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;
    [SerializeField] private ScorpionPassive _scorpionPassive;
    [SerializeField] [Range(0, 100)] private float _minDamage = 18f;
    [SerializeField] [Range(0, 100)] private float _maxDamage = 26f;
    [SerializeField] private GameObject _blade;

    [SyncVar] private int _counter = 1;

    #region Const
    private const float BleedingDuration = 6f;
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

    private void AttackPassed(bool shouldIncreaseCounter, Character target)
    {
        _comboCounter.AddSkill(target, this);

        if (_comboCounter.IsFinalComboSkill(target, this))
        {
            CharacterState state = target.GetComponent<CharacterState>();

            if (state != null)
            {
                state.AddState(States.Bleeding, BleedingDuration, 0, _hero.gameObject, name);

                int comboStacks = state.CheckStateStacks(States.ComboState);

                for (int i = 0; i < comboStacks; i++) state.AddState(States.Bleeding, BleedingDuration, 0, _hero.gameObject, name);
            }
        }

        if (shouldIncreaseCounter)
        {
            _counter = _counter == MaxComboCounter ? 1 : _counter + 1;
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _wasDamageApplied = false;

        while (Targeting.GetTempTarget().Targetable == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), SearchTargetInRadius);

                if (Targeting.GetTempTarget().Targetable != null && Targeting.GetTempTarget().Targetable is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) Targeting.ClearTempTarget();

                    else
                    {
                        _hero.Move.LookAtTransform(Targeting.GetTempTarget().Targetable.Transform);
                        if (Targeting.GetTempTarget().Targetable is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget().Targetable);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget().Targetable);
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
        if(_wasDamageApplied) return;

        if (Targeting.GetTarget() != null && Vector2.Distance(transform.position, Targeting.GetTarget().Transform.position) <= AreaInfo.Radius)
        {
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(DamageRange * damageMultiplier),
                Type = DamageType,
            };

            _wasDamageApplied = true;

            if (Targeting.GetTarget() is IDamageable damageable) CmdAttack(damage, damageable.gameObject, shouldIncreaseCounter);
        }
    }

    [Command]
    private void CmdAttack(Damage damage, GameObject target, bool shouldIncreaseCounter)
    {
        if (Targeting.ForDamage.Transform != target.transform)
        {
            Targeting.ForDamage = new TargetData(target);
        }

        bool result = Targeting.ForDamage.Damageable.TryTakeDamage(ref damage, this);
        if (result && Targeting.ForDamage.Damageable is Character character) AttackPassed(shouldIncreaseCounter, character);
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
