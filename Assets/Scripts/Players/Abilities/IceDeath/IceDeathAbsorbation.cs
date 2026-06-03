using System;
using Mirror;
using System.Collections;
using System.Linq;
using UnityEngine;

public class IceDeathAbsorbation : Skill
{
    private float _baseRuneCost = 1f;
    private float _maxEnergyCost = 40f;
    private float _enemyPercentPerRune = 1f;
    private float _duration = 12f;
    private float _energyPerHp = 4f;

    private const float AnimSpeedOnCorpseAndSelf = 0.8f;
    private const float AnimSpeedOnEnemy = 2.5f;
    private const float AnimStandartSpeed = 1f;
    private const float RadiusSearchTarget = 0.5f;
    private RuneComponent _rune;
    private Energy _energy;
    private Health _health;

    
    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    
    private int AbsorbationTrigger => Animator.StringToHash("Throw");

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render,hero);
        _rune = (RuneComponent)Hero.Resources[ResourceType.Rune];
        _energy = (Energy)Hero.Resources[ResourceType.Energy];
        _health = (Health)Hero.Resources[ResourceType.Health];
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
        {
            Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
        }
    }

    protected override bool CheckResourcesOnSkill()
    {
        return _rune.CurrentValue >= _baseRuneCost;
    }
    
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), RadiusSearchTarget, true);

                if (Targeting.GetTempTarget()?.Character != null)
                {
                    var target = Targeting.GetTempTarget()?.Character;
                    if (IsAllyTarget(target) && target is not MinionComponent && target != _hero)
                    {
                        Targeting.ClearTempTarget();						
                    }
                    else
                    {
                        _hero.Move.LookAtTransform(Targeting.GetTempTarget().Character.transform);
                        break;
                    }
                }
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        Targeting.ClearTempTarget();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved?.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        Character target = Targeting.GetTarget()?.Character;
        
        if (target == null || target == Hero)
        {
            yield return StartCoroutine(CastOnSelf());
        }
        else if (target is MinionComponent && target.Abilities.GetSkill<DeadStrike>() != null)
        {
            yield return StartCoroutine(CastOnCorpse(target));
        }
        else
        {
            yield return StartCoroutine(CastOnEnemy(target));
        }

        Targeting.ClearTarget();
    }
    
    private IEnumerator CastOnSelf()
    {
        PlayAbsorptionAnim(AnimSpeedOnCorpseAndSelf,false);
        yield return new WaitForSeconds(0.8f);
        PlayAbsorptionAnim(AnimStandartSpeed,true);

        if (!CheckForRune()) yield break;
        ConsumeRune();

        float missingEnergy = _energy.MaxValue - _energy.CurrentValue;
        float hpToSpend = Mathf.Ceil(missingEnergy / _energyPerHp);

        hpToSpend = Mathf.Min(hpToSpend, _health.CurrentValue * 0.95f);

        if (hpToSpend > 0)
        {
            _health.CmdUse(hpToSpend);
            _energy.CmdAdd(hpToSpend * _energyPerHp);
        }

        _energy.CmdRemoveAllRegenModifiers();
    }
    
    private IEnumerator CastOnCorpse(Character corpse)
    {
        PlayAbsorptionAnim(AnimSpeedOnCorpseAndSelf,false);
        yield return new WaitForSeconds(0.8f);
        PlayAbsorptionAnim(AnimStandartSpeed,true);

        if (!CheckForRune()) yield break;
        ConsumeRune();

        float maxHpOnCorpse = corpse.Health.CurrentValue;
        if (maxHpOnCorpse <= 0) 
        {
            yield break;
        }
        const float maxEnergyCost = 40f;

        float paidHpDrained = maxHpOnCorpse >= maxEnergyCost ? maxEnergyCost : maxHpOnCorpse;

        float energyToSpend = paidHpDrained;

        if (maxHpOnCorpse > 0)
        {
            corpse.Health.CmdUse(maxHpOnCorpse);

            _health.CmdAdd(maxHpOnCorpse);

            if (energyToSpend > 0)
            {
                _energy.CmdUse(energyToSpend);
            }
        }
    }

    private IEnumerator CastOnEnemy(Character enemy)
    {
        PlayAbsorptionAnim(AnimSpeedOnEnemy,false);
        yield return new WaitForSeconds(2.5f);
        PlayAbsorptionAnim(AnimStandartSpeed,true);

        if (!CheckForRune()) yield break;
        ConsumeRune();
        float percentToDrain = _enemyPercentPerRune;
        float hpToDamage = enemy.Health.CurrentValue * (percentToDrain / 100f);
        float energyToSpend = Mathf.Min(hpToDamage * 10f, _maxEnergyCost);

        Damage damage = new Damage
        {
            Value = hpToDamage,
            Type = DamageType.Magical,
            School = Schools.Dark
        };

        enemy.Health.CmdTryTakeDamage(damage, Hero.gameObject);
        
        ApplyOtherForces(hpToDamage);

        _energy.CmdUse(energyToSpend);
    }

    [Command]
    private void ApplyOtherForces(float value)
    {
        Hero.CharacterState.AddState(States.OtherForces, _duration, 0, Hero.gameObject, nameof(IceDeathAbsorbation)+"Damage:"+value);
    }

    private bool CheckForRune()
    {
        return _rune.CurrentValue > _baseRuneCost;
    }
    
    private void ConsumeRune()
    {
        _rune.CmdUse(_baseRuneCost);
    }
    
    private void PlayAbsorptionAnim(float speed,bool isSpeedOnly)
    {
        _hero.Animator.speed = AnimStandartSpeed / speed;
        if(!isSpeedOnly)
            _hero.Animator.SetTrigger(AbsorbationTrigger);
    }
}