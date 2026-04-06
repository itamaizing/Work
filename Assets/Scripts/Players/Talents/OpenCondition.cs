
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class OpenCondition
{
    public virtual bool CanOpen => CanOpenMethod();
    protected abstract bool CanOpenMethod();
}

[Serializable]
public class SpecificTalentOpenCondition : OpenCondition
{
    [SerializeField] private List<Talent> _talentsNeededToOpen;

    protected override bool CanOpenMethod()
    {
        if (_talentsNeededToOpen == null) return true;
        if(_talentsNeededToOpen.Count <= 0) return true;

        foreach(var talent in _talentsNeededToOpen)
        {
            if(!talent.Data.IsOpen)
                return false;
        }
        return true;
    }
}

[Serializable]
public class CountTalentsOpenCondition : OpenCondition
{
    public int count;
    [SerializeField] private TalentSystem talentSystem;

    protected override bool CanOpenMethod()
    {
        if (talentSystem == null) return true;

        if(talentSystem.ActiveTalents.Count >= count) 
            return true;
        return false;
    }
}

[Serializable]
public class EmptyCondition : OpenCondition
{
    protected override bool CanOpenMethod()
    {
        return true;
    }
}