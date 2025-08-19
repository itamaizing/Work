using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetInfo
{
    public List<ITargetable> Targets = new();
    public List<Vector3> Points = new();
    public List<Quaternion> Roation = new();
}
