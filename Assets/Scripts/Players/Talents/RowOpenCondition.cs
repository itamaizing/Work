using System;
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
    public override string ConditionDescription() => _conditionDescription = $"Spend {count} points in this branch";
    protected override bool CanOpenMethod(TalentSystem system, TalentsGroup group, int rowIndex)
        => group.GetSpentPoints() >= count;
}

[Serializable]
public class SpendPointsInPreviousRowCondition : RowOpenCondition
{
    public int count;
    public override string ConditionDescription() => _conditionDescription = $"Spend {count} points in the previous row";
    protected override bool CanOpenMethod(TalentSystem system, TalentsGroup group, int rowIndex)
        => rowIndex <= 0 || group.GetSpentPointsInRow(rowIndex - 1) >= count;
}

[Serializable]
public class OpenTalentsInBranchCondition : RowOpenCondition
{
    public int count;
    public override string ConditionDescription() => _conditionDescription = $"Open {count} talents in this branch";
    protected override bool CanOpenMethod(TalentSystem system, TalentsGroup group, int rowIndex)
        => group.GetOpenTalentsCount() >= count;
}