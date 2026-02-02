using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class CreeperCombo : MonoBehaviour
{
    [SerializeField] private SneakySpit sneakySpit;
    [SerializeField] private BlockPassiveSkill blockPassiveSkill;
    [SerializeField] private LightningStrikes _lightningStrikes;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private SkillManager _skillManager;

    private Queue<Skill> _comboSkillQueue = new();
    private const int MaxComboSize = 3;

    private void OnEnable()
    {
        _skillManager.SkillCastEnded += RegisterComboSkill;
    }

    private void OnDisable()
    {
        _skillManager.SkillCastEnded -= RegisterComboSkill;
    }

    private void RegisterComboSkill(Skill skill)
    {
        if (skill is not CreeperStrike && skill is not LightningStrikes)
        {
            _comboSkillQueue.Clear();
            return;
        }

        if (_comboSkillQueue.Count >= MaxComboSize)
            _comboSkillQueue.Dequeue();

        _comboSkillQueue.Enqueue(skill);

        TryTriggerComboBoost();
    }

    private void TryTriggerComboBoost()
    {
        var combo = _comboSkillQueue.ToArray();

        if (combo.Length == 3 && combo[0] is CreeperStrike && combo[1] is CreeperStrike && combo[2] is CreeperStrike)
        {
            CmdTriggerSneakySpitFreeWindow(combo[2].GetTargetCharacter());
            _comboSkillQueue.Clear();
        }

        else if (combo.Length >= 2)
        {
            if (combo[0] is CreeperStrike && combo[1] is LightningStrikes ||
                combo[0] is LightningStrikes && combo[1] is CreeperStrike ||
                combo[0] is LightningStrikes && combo[1] is LightningStrikes)
            {
                CmdTriggerSneakySpitFreeWindow(combo[1].GetTargetCharacter());
                _comboSkillQueue.Clear();
            }
            else if (combo.Length == 3 &&
                combo[0] is LightningStrikes &&
                combo[1] is CreeperStrike &&
                combo[2] is CreeperStrike)
            {
                CmdTriggerSneakySpitFreeWindow(combo[1].GetTargetCharacter());
                _comboSkillQueue.Clear();
            }
        }
    }

    [Command] private void CmdTriggerSneakySpitFreeWindow(Character target) => RpcTriggerSneakySpitWindow(target);

    [Command] private void CmdTriggerSneakySpitWindowCancel() => RpcTriggerSneakySpitWindowCancel();

    [Command] private void CmdBlockPassiveSkillFreeWindow(Character target) => RpcBlockPassiveSkillFreeWindow(target);

    [ClientRpc]
    private void RpcTriggerSneakySpitWindow(Character target)
    {
        if (sneakySpit != null) sneakySpit.TryStartSneakySpitBoostWindow(target);
    }

    [ClientRpc]
    private void RpcTriggerSneakySpitWindowCancel()
    {
        if (sneakySpit != null) sneakySpit.CancelBoostWindow();
    }

    [ClientRpc]
    private void RpcBlockPassiveSkillFreeWindow(Character target)
    {
        if (blockPassiveSkill != null) blockPassiveSkill.TryStartBlockPassiveSkillBoostWindow(target);
    }
}
