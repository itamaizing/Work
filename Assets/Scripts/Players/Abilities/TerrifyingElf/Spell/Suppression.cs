using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class Suppression : Skill, IMultiMagicSkill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration = 6f;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    protected override bool IsCanCast => Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    protected override int AnimTriggerCast => 0;

    #region Talent
    private bool _isSuppressionManaAbsorbtion;
    public bool IsSuppressionManaAbsorbtion { get => _isSuppressionManaAbsorbtion; set => _isSuppressionManaAbsorbtion = value; }
    public void SuppressionManaAbsorbtion(bool value) => _isSuppressionManaAbsorbtion = value;
    #endregion

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;
        TargetInfo targetInfo = new TargetInfo();

        while (true)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);
                var temp = Targeting.GetTempTarget()?.Targetable as Character;

                if (temp != null && IsEnemyTarget(temp))
                {
                    targetInfo.AddTarget(temp);
                    if (multiMagic != null) multiMagic.LastTarget = temp;
                    break;
                }
            }
            yield return null;
        }
        
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget() == null) yield break;
        var target = Targeting.GetTarget().Character;
        if (target == null) yield break;
        if (Vector3.Distance(transform.position, target.transform.position) > AreaInfo.Radius) yield break;

        CmdApplyAbsorptionState(target.gameObject);

        AfterCastJob();
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        Targeting.ClearTarget();
    }

    [Command]
    private void CmdApplyAbsorptionState(GameObject targetGameObject)
    {
        var targetCharacter = targetGameObject.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.CharacterState.AddState(States.Suppression, duration, 0, _playerLinks.gameObject, this.name);
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((targetInfo.GetTargets()[0] as Character));
    }

    public void HandleExtraTarget(Character target)
    {
        TryPayCost();
        CmdApplyAbsorptionState(target.gameObject);
    }
}