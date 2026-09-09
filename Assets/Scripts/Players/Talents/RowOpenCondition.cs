using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class RowOpenCondition
{
    protected string _conditionDescription = string.Empty;
    public bool CanOpen(TalentSystem system, TalentsGroup group, int rowIndex) => CanOpenMethod(system, group, rowIndex);
    protected abstract bool CanOpenMethod(TalentSystem system, TalentsGroup group, int rowIndex);
    public abstract string ConditionDescription();
}

[Serializable]
public class SpendPointsInBranchCondition : RowOpenCondition
{
    public int count;
    public override string ConditionDescription() => _conditionDescription = $"Условия открытия - потратить {count} очков в этой ветке";
    protected override bool CanOpenMethod(TalentSystem system, TalentsGroup group, int rowIndex)
        => group.GetSpentPoints() >= count;
}

[Serializable]
public class SpendPointsInPreviousRowCondition : RowOpenCondition
{
    public int count;
    public override string ConditionDescription() => _conditionDescription = $"Условия открытия - потратить {count} очков в предыдущем ряду";
    protected override bool CanOpenMethod(TalentSystem system, TalentsGroup group, int rowIndex)
        => rowIndex <= 0 || group.GetSpentPointsInRow(rowIndex - 1) >= count;
}

[Serializable]
public class OpenTalentsInBranchCondition : RowOpenCondition
{
    public int count;
    public override string ConditionDescription() => _conditionDescription = $"Условия открытия - открыть {count} талантов в этой ветке";
    protected override bool CanOpenMethod(TalentSystem system, TalentsGroup group, int rowIndex)
        => group.GetOpenTalentsCount() >= count;
}

[Serializable]
public class SpecificTalentOpenRowCondition : RowOpenCondition
{
    [SerializeField] private List<string> _talentsNeededToOpen;
    public IReadOnlyList<string> TalentsNeededToOpen => _talentsNeededToOpen;

    public override string ConditionDescription()
    {
        if (_talentsNeededToOpen == null || _talentsNeededToOpen.Count <= 0) return "";
        _conditionDescription = "Условия открытия - открыть талант: " + string.Join(" ", _talentsNeededToOpen);
        return _conditionDescription;
    }

    protected override bool CanOpenMethod(TalentSystem system, TalentsGroup group, int rowIndex)
    {
        if (_talentsNeededToOpen == null || _talentsNeededToOpen.Count <= 0) return true;

        foreach (var name in _talentsNeededToOpen)
        {
            var talent = system.FindTalentByName(name);
            if (talent == null || !talent.Data.IsOpen) return false;
        }
        return true;
    }
}