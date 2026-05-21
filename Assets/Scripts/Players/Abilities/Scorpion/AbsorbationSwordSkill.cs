using System;
using System.Collections;
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
    private float _baseBlockChance;
    
    private int _currentCharges = 2;
    
    private bool _isAbsorbing = false;

    private Coroutine _absorbCoroutine;

    public override string AdditionalDescription =>
        $"Поглощает 1 снарядное заклинание.\n" +
        $"Заряды: {_currentCharges}/{_maxCharges} (накопление за 30 маг. урона)";

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => _currentCharges > 0;

    #region Накопление зарядов
    
    protected override void Awake()
    {
        base.Awake();
        CheckChargers();
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        _baseBlockChance = hero.Health.BlockChance;
        _energy = hero.Resources[ResourceType.Energy];
        Hero.Health.OnBeforeTakeDamage += OnHeroAbsorbed;
        Hero.Health.Block += EndAbsorb;
        _hero.Health.DamageTaken += OnHeroDamageTaken;
    }
    
    private void OnDisable()
    {
        Hero.Health.OnBeforeTakeDamage -= OnHeroAbsorbed;
        Hero.Health.Block -= EndAbsorb;
        _hero.Health.DamageTaken -= OnHeroDamageTaken;
    }

    private void OnHeroDamageTaken(Damage damage, Skill skill)
    {
        if(!skill.IsSkillActive) return;
        if (damage.Type == DamageType.Magical)
        {
            _absorbedDamage += damage.Value;

            while (_absorbedDamage >= _chargeGainPerMagicDamage)
            {
                _absorbedDamage -= _chargeGainPerMagicDamage;
                AddCharge();
                
                if (Chargers > 0)
                {
                    Disactive = false;
                }
            }
        }
    }

    private void OnHeroAbsorbed(Damage damage, Skill skill)
    {
        if(!skill.IsSkillActive) return;
        if (_isAbsorbing && IsProjectileSkill(skill))
        {
            _hero.Health.BlockChance = 100;
            if(isClient)
                _energy.CmdUse(_absorbEnergyReturn);
        }
    }

    private void AddCharge()
    {
        if (_currentChargers < _maxCharges)
            Chargers = _currentChargers + 1;

        CheckChargers();
    }

    private void CheckChargers()
    {
        if (_currentChargers > 0)
        {
            Disactive = false;
        }
        else
        {
            Disactive = true;
        }

        Charges.SendCurrentChange(_currentChargers);
    }

    protected override void UseCooldownOrCharges()
    {
        if (_currentChargers <= 0) return;
        Chargers = _currentChargers - 1;

        CheckChargers();
    }

    #endregion


    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo info = new TargetInfo();
        info.AddTarget(Hero);
        callbackDataSaved(info);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (_currentCharges <= 0) yield break;
        
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
    }

    private void EndAbsorb()
    {
        if (!_isAbsorbing) return;

        _isAbsorbing = false;
        CmdSetAbsorbing(false);
        ControlMovement(true);
        Hero.Abilities?.SetAbilitiesDisactive(false);

        if (_absorbCoroutine != null)
        {
            StopCoroutine(_absorbCoroutine);
            _absorbCoroutine = null;
        }
        
        CheckChargers();
        _hero.Health.BlockChance = _baseBlockChance;
    }

    private bool IsProjectileSkill(Skill skill)
    {
        if (skill == null) return false;

        return skill.Info.SkillType == SkillType.Projectile;
    }

    private void ControlMovement(bool canMove)
    {
        if (Hero?.Move == null) return;

        Hero.Move.SetCanMove(canMove);

        if (!canMove)
            Hero.Move.StopMoveAndAnimationMove();
    }
}
