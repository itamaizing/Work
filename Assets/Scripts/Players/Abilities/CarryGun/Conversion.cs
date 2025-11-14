using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class Conversion : Skill
{
    [SerializeField] private BasePsionicEnergy _psionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;

    private Character target;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        target = (Character)targetInfo.Targets[0];
    }

    protected override void ClearData()
    {
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (_psionicEnergy.CurrentValue <= 0) yield return null;

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(Hero);
        targetDataSavedCallback(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_psionicEnergy != null && _attackingPsionicEnergy != null)
        {
            if (_psionicEnergy.CurrentValue > 0)
            {
                CmdConvertEnergy();
            }
        }

        var lastSkill = Hero?.Abilities?.LastCastedSkill;
        if (lastSkill != null && lastSkill.AutoAttack == AutoAttack.autoAttack)
        {
            lastSkill.TryPreparing();
        }

        yield break;
    }

    [Command]
    private void CmdConvertEnergy()
    {
        _psionicEnergy.ConvertToAttackingEnergy(_attackingPsionicEnergy.MaxAttackingPsiEnergy);
    }
}
