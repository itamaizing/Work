using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TalentRow
{
    public List<Talent> Talents = new();
    public bool isOpen;

    [SerializeReference, SubclassSelector]
    public List<RowOpenCondition> OpenConditions = new();
}
