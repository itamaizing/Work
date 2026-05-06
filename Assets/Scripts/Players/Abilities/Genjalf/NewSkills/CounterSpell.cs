using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class CounterSpell : Skill
{
    [SerializeField] private ParticleSystem _particlePref;

    [SerializeField] private SchoolSolvent _schoolSolvent;
    //[SerializeField, Range(0, 100)] private int _debuffChance = 15;

    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;
    
    private float _clickRadius = 0.5f;

    public event Action<Schools> OnSpellDispelled;
    
    #region Talents
    private bool _isApplyDamageTalent;
    private float _manaPercentDamage = 0.3f;
    private float _lastSkillManaCost;
    #endregion
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.GetTarget()?.Character != null;
    }

    public void IsApplyDamageTalent(bool value)
    {
        if (_isApplyDamageTalent == value) return;
        _isApplyDamageTalent = value;
    }
    
    public void AnimCastLight()
    {
        AnimStartCastCoroutine();
    }

    public void AnimLightEnd()
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
            Character currentCharacter = Targeting.GetTarget()?.Character;

            if (currentCharacter)
            {
                CmdState(currentCharacter.gameObject);

                CmdApplyTalent(currentCharacter.gameObject,_isApplyDamageTalent);
            }
        }
        yield return null;
    }

    [Command]
    private void CmdApplyTalent(GameObject target, bool isApplyDamageTalent)
    {
        Character currentCharacter = target.GetComponent<Character>();

        RpcApplyTalent(currentCharacter.connectionToClient, target, isApplyDamageTalent);
    }

    [TargetRpc]
    private void RpcApplyTalent(NetworkConnectionToClient targetConn, GameObject target, bool isApplyDamageTalent)
    {
        Character currentCharacter = target.GetComponent<Character>();
        var cancelledSkill = currentCharacter.Abilities.CurrentCastingSkill;

        if (cancelledSkill == null) return;

        Schools school = cancelledSkill.Info.School;

        if (school != Schools.None)
        {
            CmdReturnSchoolToCaster(school);
        }

        if (!isApplyDamageTalent) return;
        
        _lastSkillManaCost = cancelledSkill.Cost.BaseCost;
        CmdApplyManaDamage(currentCharacter.gameObject, _lastSkillManaCost * _manaPercentDamage);
    }
    
    [Command(requiresAuthority = false)]
    private void CmdReturnSchoolToCaster(Schools school, NetworkConnectionToClient sender = null)
    {
        RpcAddSchoolToSolvent(connectionToClient, school);
    }

    [TargetRpc]
    public void RpcAddSchoolToSolvent(NetworkConnectionToClient targetConn, Schools school)
    {
        _schoolSolvent.AddSchool(school);
        OnSpellDispelled?.Invoke(school);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Targetable == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
                
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);

                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (character != null && !IsEnemyTarget(character))
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
        Targeting.SetTarget(Targeting.GetTempTarget()?.Targetable);
        Targeting.ClearTempTarget();

        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    [Command]
    private void CmdApplyManaDamage(GameObject target, float damage)
    {
        Damage dmg = new Damage();
        dmg.Value = damage;
        ApplyDamage(dmg, target);
    }
    
    [Command]
    private void CmdState(GameObject enemy)
    {
        Character enemyChar = enemy.GetComponent<Character>();
        enemyChar.CharacterState.AddState(States.SchoolDebuff, 5, 0,Schools.None, Hero.gameObject, name);
    }
}
