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

    protected override bool IsCanCast => GetTarget() != null && Vector3.Distance(GetTarget().Transform.position, transform.position) <= Radius;
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == TargetsLayers;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Cast Blade");

    private void OnDisable() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;

    private void HandleSkillCanceled()
    {
        _wasDamageApplied = false;
        ClearTarget();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        SetTarget(targetInfo.GetTargets()[0]);
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

        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                FindTarget(SearchTargetInRadius, GetMousePoint());

                if (GetTempTarget() != null && GetTempTarget() is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) ClearTempTarget();

                    else
                    {
                        _hero.Move.LookAtTransform(GetTempTarget().Transform);
                        if (GetTempTarget() is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        SetTarget(GetTempTarget());

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(GetTarget());
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

        if (GetTarget() != null && Vector2.Distance(transform.position, GetTarget().Transform.position) <= Radius)
        {
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(DamageRange * damageMultiplier),
                Type = DamageType,
            };

            _wasDamageApplied = true;

            if (GetTarget() is IDamageable damageable) CmdAttack(damage, damageable.gameObject, shouldIncreaseCounter);
        }
    }

    [Command]
    private void CmdAttack(Damage damage, GameObject target, bool shouldIncreaseCounter)
    {
        if (_tempTargetForDamage != target.transform)
        {
            _tempTargetForDamage = target.transform;
            _tempForDamage = target.GetComponent<IDamageable>();
        }

        bool result = _tempForDamage.TryTakeDamage(ref damage, this);
        if (result && _tempForDamage is Character character) AttackPassed(shouldIncreaseCounter, character);
    }

    protected override void ClearData()
    {
        _wasDamageApplied = false;
        ClearTarget();
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
