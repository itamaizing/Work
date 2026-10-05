using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class AbsorbationSwordSkill : Skill
{
    [SerializeField] private float _duration = 2f;

    private DamageType _absorbDamageType = DamageType.Magical;
    private Resource _energy;

    private float _absorbEnergyReturn = 10f;
    private float _chargeGainPerMagicDamage = 30f;
    private float _absorbedDamage = 0f;
    
    private bool _isAbsorbing = false;

    private Coroutine _absorbCoroutine;

    #region Доп урон от поглощённых снарядов

    private bool _isAbsorbedDamage;
    private List<Skill> _swordSkills = new(); 
    private List<Damage> _absorbedDamages = new();
    #endregion
    

    public override string AdditionalDescription =>
        $"Поглощает 1 снарядное заклинание.\n" +
        $"Заряды: {Charges.RemainingCharges}/{Charges.MaxCharges} (накопление за 30 маг. урона)";

    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => Charges.HasCharges;

    #region Накопление зарядов
    
    protected override void Awake()
    {
        base.Awake();
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        _energy = hero.Resources[ResourceType.Energy];
        _hero.Health.DamageTaken += OnHeroDamageTaken;
        
        Charges.OnCurrentChange += OnChargesChanged;
        
        _swordSkills = _hero.Abilities.Abilities.Where(s => s is ISwordSkill).ToList();
        foreach (var swordSkill in _swordSkills)
        {
            swordSkill.CastFinished += () => ApplyAbsorbedDamage(swordSkill);
        }
    }
    
    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }
    
    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;

        Charges.OnCurrentChange -= OnChargesChanged;
        
        _hero.Health.DamageTaken -= OnHeroDamageTaken;
        foreach (var swordSkill in _swordSkills)
        {
            swordSkill.CastFinished -= () => ApplyAbsorbedDamage(swordSkill);
        }
    }
    
    private void OnChargesChanged(int remaining) => UpdateDisactiveFromCharges();
    
    private void HandleSkillCanceled()
    {
        _hero.Move.StopLookAt();
        UseCooldownOrCharges();
        EndAbsorb();
    }

    private void OnSkillStarted()
    {
        _hero.Health.OnTryResist += TryAbsorb;
        Hero.Health.Block += EndAbsorb;
    }

    private void OnSkillEnded()
    {
        _hero.Health.OnTryResist -= TryAbsorb;
        Hero.Health.Block -= EndAbsorb;
    }

    public void EnableAbsorbedDamage(bool value)
    {
        if(_isAbsorbedDamage == value) return;
        _isAbsorbedDamage = value;
    }

    private void ApplyAbsorbedDamage(Skill skill)
    {
        if (_isAbsorbedDamage)
        {
            var target = skill.Targeting.GetTarget()?.Character;
            if (target == null) return;
            foreach (var damage in _absorbedDamages)
            {
                var newDamage = new Damage { Value = damage.Value / 2, School = damage.School };
                if(isClient)
                    CmdApplyDamage(newDamage,target.gameObject);
            }
            _absorbedDamages.Clear();
        }
    }

    private void OnHeroDamageTaken(Damage damage, Skill skill)
    {
        if (damage.Type == DamageType.Magical)
        {
            _absorbedDamage += damage.Value;

            while (_absorbedDamage >= _chargeGainPerMagicDamage)
            {
                _absorbedDamage -= _chargeGainPerMagicDamage;
                AddCharge();
                
                if (Charges.RemainingCharges > 0)
                {
                    Disactive = false;
                }
            }
        }
    }


    private bool TryAbsorb(Damage damage, Skill skill)
    {
        if (!_isAbsorbing || !IsProjectileSkill(skill)) return false;

        if (isClient)
            _energy.CmdUse(_absorbEnergyReturn);

        AddAbsorbedDamageToList(_hero.gameObject, damage);

        EndAbsorb();

        return true;
    }

    [TargetRpc]
    private void AddAbsorbedDamageToList(GameObject target,Damage damage)
    {
        if(_isAbsorbedDamage)
            _absorbedDamages.Add(damage);
    }

    private void AddCharge()
    {
        if (Charges.RemainingCharges >= Charges.MaxCharges) return;
        if (Charges.RechargeTimers.Count > 0)
            Charges.RestoreCharge(0);
        UpdateDisactiveFromCharges();
    }

    private void UpdateDisactiveFromCharges()
    {
        Disactive = !Charges.HasCharges;
        Charges.SendCurrentChange(Charges.RemainingCharges);
    }

    protected override void UseCooldownOrCharges()
    {
        if (!Charges.HasCharges) return;
        Charges.TryUse();
        UpdateDisactiveFromCharges();
    }

    #endregion


    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo info = new TargetInfo();
        info.AddTarget(Hero);
        callbackDataSaved(info);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (Charges.RemainingCharges <= 0) yield break;
        _isAbsorbing = true;
        CmdSetAbsorbing(true);
        ControlMovement(false);

        Hero.Abilities?.SetAbilitiesDisactive(true);

        float timer = 0f;

        while (_isAbsorbing && timer < _duration)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        EndAbsorb();
    }

    [Command]
    private void CmdSetAbsorbing(bool value)
    {
        _isAbsorbing = value;
        if(value)
            OnSkillStarted();
    }

    private void EndAbsorb()
    {
        if (!_isAbsorbing) return;

        _isAbsorbing = false;
        if(isClient)
            CmdSetAbsorbing(false);
        ControlMovement(true);
        Hero.Abilities?.SetAbilitiesDisactive(false);

        if (_absorbCoroutine != null)
        {
            StopCoroutine(_absorbCoroutine);
            _absorbCoroutine = null;
        }

        OnSkillEnded();
        UpdateDisactiveFromCharges();
    }

    private bool IsProjectileSkill(Skill skill)
    {
        if (skill == null) return false;

        return skill.Targeting.SkillType == SkillType.Projectile;
    }

    private void ControlMovement(bool canMove)
    {
        if (Hero?.Move == null) return;

        Hero.Move.SetCanMove(canMove);

        if (!canMove)
            Hero.Move.StopMoveAndAnimationMove();
    }
}