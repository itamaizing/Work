using Mirror;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class CleavingBlade_Scorpion : Skill
{
    [Header("Ability settings")]
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;
    [SerializeField] private ScorpionPassive scorpionPassive;
    [SerializeField] [Range(0, 100)] private float _minDamage = 18f;
    [SerializeField] [Range(0, 100)] private float _maxDamage = 26f;
    [SerializeField] private GameObject blade;

    [SyncVar] private int _counter = 1;
    private IDamageable _target;

    private bool isCleavingBlade_ScorpionSecondTalent;

    public float DamageRange => Random.Range(_minDamage, _maxDamage);

    protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Cast Blade");

    private void OnDisable() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;

    private void HandleSkillCanceled() => _target = null;
    private void AttackPassed(Character target, bool shouldIncreaseCounter)
    {
        _comboCounter.AddSkill(target, this);

        if (_comboCounter.IsFinalComboSkill(target, this))
        {
            CharacterState state = target.GetComponent<CharacterState>();

            if (state != null)
            {
                state.AddState(States.Bleeding, 6f, 0, _hero.gameObject, name);

                int comboStacks = state.CheckStateStacks(States.ComboState);

                for (int i = 0; i < comboStacks; i++) state.AddState(States.Bleeding, 6f, 0, _hero.gameObject, name);
            }
        }

        if (shouldIncreaseCounter)
        {
            _counter = _counter == 3 ? 1 : _counter + 1;
        }

        _target = null;
    }

    private void AttackMissed()
    {
        Debug.LogWarning("CleavingBlade_Scorpion .AttackMissed - Промах");
        _counter = 1;
        _comboCounter.ResetCounter();

        _target = null;
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as IDamageable;
        if (_target is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        ITargetable target = null;

        while (target == null)
        {
            if (GetMouseButton)
            {
                if (GetRaycastTarget() is ITargetable targetable) target = targetable;
            }

            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(target);
        callbackDataSaved.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        TryAttack(true, 1f);
        yield return null;
    }

    private void SpeedAnimBlade_Scorpion()
    {
        float speed = 1f;

        if (isCleavingBlade_ScorpionSecondTalent && _counter == 2) speed = 0.8f;

        _hero.Animator.SetFloat("CastChainBladeSpeed", speed);
    }

    private void TryAttack(bool shouldIncreaseCounter, float damageMultiplier)
    {
        if (_target != null && Vector2.Distance(transform.position, _target.transform.position) <= Radius)
        {
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(DamageRange * damageMultiplier),
                Type = DamageType,
            };

            CmdAttack(damage, _target.gameObject, shouldIncreaseCounter);
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
        if (result && _tempForDamage is Character character) AttackPassed(character, shouldIncreaseCounter);
    }

    protected override void ClearData()
    {
        _target = null;
    }

    public void BladeActive()
    {
        blade.SetActive(true);
        SpeedAnimBlade_Scorpion();
    }

    public void CleavingBlade_ScorpionCast()
    {
        AnimStartCastCoroutine();
    }

    public void CleavingBlade_ScorpionEnd()
    {
        AnimCastEnded();
        blade.SetActive(false);
    }

    public void CleavingBlade_ScorpionSecondTalent(bool value)
    {
        isCleavingBlade_ScorpionSecondTalent = value;
    }
}
