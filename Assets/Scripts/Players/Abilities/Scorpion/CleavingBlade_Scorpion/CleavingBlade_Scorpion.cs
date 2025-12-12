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
    //private Character _target;
    private Character _runtimeTarget;

    private bool isCleavingBlade_ScorpionSecondTalent;

    public float DamageRange => Random.Range(_minDamage, _maxDamage);

    protected override bool IsCanCast => GetTargetCharacter() != null && Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Cast Blade");

    private void OnDisable() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;

    private void HandleSkillCanceled() => ClearTarget();

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        SetTarget((Character)targetInfo.GetTargets()[0]);
        //if (_target is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
    }

    private void AttackPassed(bool shouldIncreaseCounter, Character target)
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
        ClearTarget();
        //_target = null;
    }

    private void AttackMissed()
    {
        Debug.LogWarning("CleavingBlade_Scorpion .AttackMissed - Промах");
        _counter = 1;
        _comboCounter.ResetCounter();

        //_target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (GetTargetCharacter() == null)
        {
            if (GetMouseButton)
            {
                FindTargetCharacter();
                //_target = GetRaycastTarget(true);
            }

            yield return null;
        }

        TargetInfo targetInfo = new();
        targetInfo.AddTarget(GetTargetCharacter());
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        _runtimeTarget = GetTargetCharacter();
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
