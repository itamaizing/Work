using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class DispelMagic : Skill
{
    private float _clickRadius = 0.5f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("DispelMagic");

    protected override bool IsCanCast =>
        Targeting.GetTarget()?.Character != null &&
        Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;

    private bool IsAllyTarget(Character target) =>
        target != null && target.gameObject.layer == LayerMask.NameToLayer("Allies");

    private bool IsEnemyTarget(Character target) =>
        target != null && target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    public void AnimCastDispelMagic() => AnimStartCastCoroutine();
    public void AnimDispelMagicEnd() => AnimCastEnded();

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    protected override void ClearData() => Targeting.ClearTarget();

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);

                if (Targeting.GetTempTarget()?.Character)
                {
                    _hero.Move.LookAtTransform(Targeting.GetTempTarget().Character.transform);
                }
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character == null) yield break;

        CmdDispel(Targeting.GetTarget().Character.gameObject);
        yield return null;
    }

    [Command]
    private void CmdDispel(GameObject targetGO)
    {
        if (!targetGO.TryGetComponent<Character>(out var target)) return;

        var characterState = target.CharacterState;
        bool isAlly = IsAllyTarget(target);
        
        BaffDebaff typeToRemove = isAlly ? BaffDebaff.Debaff : BaffDebaff.Baff;
        
        AbstractCharacterState stateToDispel = null;
        foreach (var state in characterState.CurrentStates)
        {
            if (state.Type == StateType.Magic && state.BaffDebaff == typeToRemove)
            {
                stateToDispel = state;
                break;
            }
        }

        if (stateToDispel == null) return;

        /*RpcNotifyDispel(targetGO, stateToDispel.State, stateToDispel.CurrentStacksCount);

        if (stateToDispel.CurrentStacksCount > 1)
        {
            RpcRemoveOneStack(targetGO, stateToDispel.State);
        }
        else
        {
            characterState.RemoveState(stateToDispel.State);
        }*/
    }

    [ClientRpc]
    private void RpcNotifyDispel(GameObject targetGO, States state, int stacksCount)
    {
        if (!targetGO.TryGetComponent<CharacterState>(out var characterState)) return;
        
        var stateInstance = characterState.GetState(state);
        if (stateInstance?.PersonWhoMadeBuff == null) return;

        stateInstance.PersonWhoMadeBuff.CharacterState.OnOwnStateDispelled(state, 1);
    }

    [ClientRpc]
    private void RpcRemoveOneStack(GameObject targetGO, States state)
    {
        if (!targetGO.TryGetComponent<CharacterState>(out var characterState)) return;

        var stateInstance = characterState.GetState(state);
        if (stateInstance == null) return;

        //stateInstance.ReduceStack();
        characterState.StateIcons.RemoveIconCount();
    }
}
