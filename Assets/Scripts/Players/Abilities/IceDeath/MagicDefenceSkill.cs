using System;
using System.Collections;
using UnityEngine;

public class MagicDefenceSkill : Skill
{
    private float _baseRuneCost = 2f;
    private float _defenceBaseDuration = 2f;
    private int _plagueCharges = 0;

    private const float AnimSpeedOnAllies = 0.8f;
    private const float AnimSpeedOnEnemy = 2.5f;
    private const float AnimStandartSpeed = 1f;
    private const float RadiusSearchTarget = 0.5f;
    private RuneComponent _rune;
    private Energy _energy;


    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    
    private int MagicDefenceTrigger => Animator.StringToHash("Throw");

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render,hero);
        _rune = (RuneComponent)Hero.Resources[ResourceType.Rune];
        _energy = (Energy)Hero.Resources[ResourceType.Energy];
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
        
        if (target == Hero || IsAllyTarget(target))
        {
            yield return StartCoroutine(CastOnSelfOrAlly());
        }
        else if (!IsAllyTarget(target))
        {
            yield return StartCoroutine(CastOnEnemy(target));
        }

        Targeting.ClearTarget();
    }

    private IEnumerator CastOnSelfOrAlly()
    {
        PlayMagicDefenceAnim(AnimSpeedOnAllies, false);
        yield return new WaitForSeconds(0.8f);
        PlayMagicDefenceAnim(AnimStandartSpeed, true);

        if (!CheckForRuneOrPlague()) yield break;
        ConsumeRuneOrPlague();
    }

    private IEnumerator CastOnEnemy(Character enemy)
    {
        PlayMagicDefenceAnim(AnimSpeedOnEnemy, false);
        yield return new WaitForSeconds(2.5f);
        PlayMagicDefenceAnim(AnimStandartSpeed, true);
    }

    public void AddPlagueCharge(int value)
    {
        _plagueCharges += value;
    }

    private bool CheckForRuneOrPlague()
    {
        int neededRunes = Mathf.Max(0, (int)_baseRuneCost - _plagueCharges);
        return _rune.CurrentValue >= neededRunes;
    }
    
    private void ConsumeRuneOrPlague()
    {
        int runesToConsume = Mathf.Max(0, (int)_baseRuneCost - _plagueCharges);
        int plagueToConsume = Mathf.Min(_plagueCharges, (int)_baseRuneCost);
        
        _plagueCharges -= plagueToConsume;

        if (runesToConsume > 0)
        {
            _rune.CmdUse(runesToConsume);
        }
    }
    
    private void PlayMagicDefenceAnim(float speed,bool isSpeedOnly)
    {
        _hero.Animator.speed = AnimStandartSpeed / speed;
        if(!isSpeedOnly)
            _hero.Animator.SetTrigger(MagicDefenceTrigger);
    }
}