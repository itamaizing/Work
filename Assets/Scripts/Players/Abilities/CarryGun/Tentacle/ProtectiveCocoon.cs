using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class ProtectiveCocoon : NetworkBehaviour
{
    [SyncVar] private uint _targetNetId;

    private Character _target;
    private List<Skill> _disabledSkills = new();

    public void Init(Character target)
    {
        _target = target;
        _targetNetId = target.netId;

        ApplyControl();
    }

    private void ApplyControl()
    {
        if (_target == null) return;

        _target.Move.SetCanMove(false);

        foreach (var skill in _target.Abilities.Skills)
        {
            if (!skill.Disactive)
            {
                skill.Disactive = true;
                _disabledSkills.Add(skill);
            }
        }
    }

    private void RemoveControl()
    {
        if (_target == null) return;

        _target.Move.SetCanMove(true);

        foreach (var skill in _disabledSkills)
        {
            if (skill != null)
                skill.Disactive = false;
        }

        _disabledSkills.Clear();
    }

    public override void OnStopServer()
    {
        RemoveControl();
    }
}
