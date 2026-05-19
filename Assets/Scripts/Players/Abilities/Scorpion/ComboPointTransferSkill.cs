using System.Collections;
using Mirror;
using UnityEngine;

public class ComboPointTransferSkill : Skill
{
    protected override bool IsCanCast => CheckCanCast();
    
    private bool CheckCanCast()
    {
        Character target = Targeting.GetTarget()?.Character;

        if (target == null)
            return false;

        if (Vector3.Distance(target.transform.position, transform.position) > AreaInfo.Radius)
            return false;

        ConsumeCombo_Scorpion comboScorpion = _hero.Abilities.GetSkill<ConsumeCombo_Scorpion>();
        Character fromCharacter = comboScorpion.LastCharacterNet;

        if (fromCharacter == null)
            return false;

        return Vector3.Distance(fromCharacter.transform.position, transform.position) <= AreaInfo.Radius;
    }
    
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private CharacterState fromCharacter;

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null || Targeting.GetTempTarget().Character == Hero || IsAllyTarget(Targeting.GetTempTarget().Character))
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        Character target = Targeting.GetTarget()?.Character;
        if (target == null) yield break;

        CmdTransferComboPoints(target.gameObject);
        yield return null;
    }

    protected override void ClearData() { }

    [Command]
    private void CmdTransferComboPoints(GameObject target)
    {
        if (target == null) return;
        Character toCharacter = target.GetComponent<Character>();
        if (toCharacter == null) return;

        ConsumeCombo_Scorpion comboScorpion = _hero.Abilities.GetSkill<ConsumeCombo_Scorpion>();

        CharacterState fromCharacter = comboScorpion.LastCharacterState;
        
        if (fromCharacter == null) return;

        //if(Vector3.Distance(fromCharacter.gameObject.transform.position, transform.position) <= AreaInfo.Radius) return;
        
        var comboState = fromCharacter.GetState(States.ComboState) as ComboState;

        if (comboState != null && comboState.CurrentStacksCount > 0)
        {
            int stacksToTransfer = comboState.CurrentStacksCount;

            fromCharacter.RemoveState(States.ComboState);
            for (int i = 0; i < stacksToTransfer; i++)
            {
                toCharacter.CharacterState.AddState(States.ComboState, float.PositiveInfinity, 0f, _hero.gameObject, nameof(ConsumeCombo_Scorpion));
            }

            comboScorpion.LastCharacterState = toCharacter.CharacterState;
        }
    }
}
