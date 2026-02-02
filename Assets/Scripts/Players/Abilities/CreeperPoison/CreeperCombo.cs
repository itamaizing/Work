using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperCombo : NetworkBehaviour
{
    [SerializeField] private SneakySpit sneakySpit;
    [SerializeField] private BlockPassiveSkill blockPassiveSkill;
    [SerializeField] private SkillManager _skillManager;

    private Queue<Skill> _sneakyComboQueue = new();
    private Queue<Skill> _blockComboQueue = new();

    private Coroutine _resetCoroutine;
    private float _comboResetDelay = 1f;

    private const int SneakyComboSize = 3;
    private const int BlockComboSize = 2;

    private void OnEnable()
    {
        _skillManager.SkillCastEnded += HandleSneakyCombo;
        _skillManager.SkillCastEnded += HandleBlockCombo;
    }

    private void OnDisable()
    {
        _skillManager.SkillCastEnded -= HandleSneakyCombo;
        _skillManager.SkillCastEnded -= HandleBlockCombo;
    }

    private IEnumerator ResetTimer()
    {
        yield return new WaitForSeconds(_comboResetDelay);
        ClearComboQueue();
    }

    private void ClearComboQueue()
    {
        _sneakyComboQueue.Clear();
        _blockComboQueue.Clear();

        if (_resetCoroutine != null)
        {
            StopCoroutine(_resetCoroutine);
            _resetCoroutine = null;
        }
    }

    private void HandleSneakyCombo(Skill skill)
    {
        if (skill is not CreeperStrike && skill is not LightningStrikes)
        {
            _sneakyComboQueue.Clear();
            return;
        }

        if (_sneakyComboQueue.Count >= SneakyComboSize)
            _sneakyComboQueue.Dequeue();

        if (_resetCoroutine != null) StopCoroutine(_resetCoroutine);
        _resetCoroutine = StartCoroutine(ResetTimer());

        _sneakyComboQueue.Enqueue(skill);
        TryTriggerSneakyCombo();
    }

    private void TryTriggerSneakyCombo()
    {
        var combo = _sneakyComboQueue.ToArray();
        if (combo.Length < 2) return;

        int creeperCount = 0;
        int lightningCount = 0;

        foreach (var skill in combo)
        {
            if (skill is CreeperStrike) creeperCount++;
            else if (skill is LightningStrikes) lightningCount++;
        }

        bool validCombo =
            lightningCount == 2 ||
            creeperCount == 3 ||
            (lightningCount == 1 && creeperCount == 2);

        if (validCombo)
        {
            Skill lastSkill = combo[^1];
            CmdTriggerSneakySpitFreeWindow(lastSkill.GetTargetCharacter());
            _sneakyComboQueue.Clear();

            if (_resetCoroutine != null)
            {
                StopCoroutine(_resetCoroutine);
                _resetCoroutine = null;
            }
        }
    }

    private void HandleBlockCombo(Skill skill)
    {
        if (skill is not CreeperStrike && skill is not LightningStrikes)
        {
            _blockComboQueue.Clear();
            return;
        }

        if (_blockComboQueue.Count >= BlockComboSize)
            _blockComboQueue.Dequeue();

        if (_resetCoroutine != null) StopCoroutine(_resetCoroutine);
        _resetCoroutine = StartCoroutine(ResetTimer());

        _blockComboQueue.Enqueue(skill);
        TryTriggerBlockCombo();
    }

    private void TryTriggerBlockCombo()
    {
        var combo = _blockComboQueue.ToArray();
        if (combo.Length < 1) return;

        int creeperCount = 0;
        int lightningCount = 0;

        foreach (var skill in combo)
        {
            if (skill is CreeperStrike) creeperCount++;
            else if (skill is LightningStrikes) lightningCount++;
        }

        bool validCombo =
            lightningCount == 1 ||
            creeperCount == 2 ||
            (creeperCount == 1 && lightningCount == 1);

        if (validCombo)
        {
            Skill lastSkill = combo[^1];
            CmdBlockPassiveSkillFreeWindow(lastSkill.GetTargetCharacter());
            _blockComboQueue.Clear();

            if (_resetCoroutine != null)
            {
                StopCoroutine(_resetCoroutine);
                _resetCoroutine = null;
            }
        }
    }

    [Command] private void CmdTriggerSneakySpitFreeWindow(Character target) => RpcTriggerSneakySpitWindow(target);
    [Command] private void CmdBlockPassiveSkillFreeWindow(Character target) => RpcBlockPassiveSkillFreeWindow(target);

    [ClientRpc]
    private void RpcTriggerSneakySpitWindow(Character target)
    {
        sneakySpit?.TryStartSneakySpitBoostWindow(target);
    }

    [ClientRpc]
    private void RpcBlockPassiveSkillFreeWindow(Character target)
    {
        blockPassiveSkill?.TryStartBlockPassiveSkillBoostWindow(target);
    }
}
