using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

public class SpellThiefSkill : Skill
{
    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => Animator.StringToHash("SpellThief");
    
    private float _clickRadius = 0.5f;
    
    #region Talents
    private bool _isApplyDamageTalent;
    private float _manaPercentDamage = 0.3f;
    private float _lastSkillManaCost;
    #endregion
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private bool CheckCanCast()
    {
        return Targeting.GetTarget()?.Character.CharacterState.CurrentStates.FirstOrDefault(c => c.BaffDebaff == BaffDebaff.Baff) != null && Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.GetTarget()?.Character != null;
    }
    
    public void IsApplyDamageTalent(bool value)
    {
        if (_isApplyDamageTalent == value) return;
        _isApplyDamageTalent = value;
    }

    public void AnimCastThief()
    {
        AnimStartCastCoroutine();
    }

    public void AnimThiefEnd()
    {
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if(targetInfo.GetTargets().Count > 0 && targetInfo.GetTargets()[0] != null)
            Targeting.SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null)
        {
            var targetGO = Targeting.GetTarget()?.Character.gameObject;
            
            CmdState(targetGO);
        }
        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
                
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);

                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (Targeting.GetTempTarget()?.Character != null && !IsEnemyTarget(character))
                    {
                        Targeting.ClearTempTarget();
                    }
                    else
                    {
                        if (character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        callbackDataSaved(targetInfo);
        Targeting.ClearTempTarget();
    }

    [Command]
    private void CmdState(GameObject enemy)
    {
        Character enemyChar = enemy.GetComponent<Character>();
        var baffState = enemyChar.CharacterState.CurrentStates
            .FirstOrDefault(c => c.BaffDebaff == BaffDebaff.Baff);

        if (baffState == null) return;
        if (_isApplyDamageTalent)
        {
            _lastSkillManaCost = baffState.Skill.Cost.BaseCost;
            Damage dmg = new Damage();
            dmg.Value = _lastSkillManaCost * _manaPercentDamage;
            ApplyDamage(dmg,enemy);
        }
        enemyChar.CharacterState.RemoveState(baffState.State);
        
        _hero.CharacterState.AddState(baffState.State, baffState.RemainingDuration, 0, Hero.gameObject, name);
    }
}
