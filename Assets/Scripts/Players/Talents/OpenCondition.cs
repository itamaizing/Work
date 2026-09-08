
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IScopedCondition
{
    void SetOwner(TalentSystem owner, int groupId);
}

[Serializable]
public abstract class OpenCondition
{
    protected string _conditionDescription = string.Empty;
    public virtual bool CanOpen => CanOpenMethod();
    protected virtual bool CanOpenMethod() { return true; }
    public virtual void Validete(TalentData data) { }
    public virtual string ConditionDescription() => _conditionDescription;
}

[Serializable]
public class SpecificTalentOpenCondition : OpenCondition, IScopedCondition
{
    [SerializeField] private List<string> _talentsNeededToOpen;

    private TalentSystem _owner;
    public void SetOwner(TalentSystem owner, int groupId) => _owner = owner;

    public override string ConditionDescription()
    {
        if (_talentsNeededToOpen == null || _talentsNeededToOpen.Count <= 0) return "";
        _conditionDescription = "Open this talents: " + string.Join(" ", _talentsNeededToOpen);
        return _conditionDescription;
    }

    protected override bool CanOpenMethod()
    {
        if (_talentsNeededToOpen == null || _talentsNeededToOpen.Count <= 0) return true;
        if (_owner == null) return true;

        foreach (var name in _talentsNeededToOpen)
        {
            var talent = _owner.FindTalentByName(name);
            if (talent == null || !talent.Data.IsOpen) return false;
        }
        return true;
    }

    public override void Validete(TalentData data) { }
}

[Serializable]
public class CountTalentsOpenCondition : OpenCondition, IScopedCondition
{
    public int count;
    private TalentSystem _owner;
    private int _groupId;
    public void SetOwner(TalentSystem owner, int groupId) { _owner = owner; _groupId = groupId; }

    public override string ConditionDescription() => _conditionDescription = $"Open {count} talents in this branch";

    protected override bool CanOpenMethod()
        => _owner == null || _owner.GetGroupOpenTalentsCount(_groupId) >= count;
}

[Serializable]
public class CountPointsCondition : OpenCondition, IScopedCondition
{
    public int count;
    private TalentSystem _owner;
    private int _groupId;
    public void SetOwner(TalentSystem owner, int groupId) { _owner = owner; _groupId = groupId; }

    public override string ConditionDescription() => _conditionDescription = $"Add {count} points in this branch";

    protected override bool CanOpenMethod()
        => _owner == null || _owner.GetGroupSpentPoints(_groupId) >= count;
}

[Serializable]
public class EmptyCondition : OpenCondition
{
    protected override bool CanOpenMethod() => true;
}