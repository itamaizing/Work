using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsoluteAccuracy : Skill
{
    public float DecreaseCooldownTime = 2f;

    [Header("Talent")]
    [SerializeField] private AbsoluteAccuracyTalent _absoluteAccuracyTalent;

    [Header("AbilityProperties")]
    [SerializeField] private CreeperStrike _creeperStrike;

    private bool _isCanCrit;

    public bool IsCanCrit { get => _isCanCrit; set => _isCanCrit = value; }
    protected override bool IsCanCast => _absoluteAccuracyTalent.IsActive;

    protected override void ClearData()
    {
        Debug.Log("AbsoluteAccuracy / ClearData");
    }

    protected override IEnumerator PrepareJob()
    {
        Debug.Log("AbsoluteAccuracy / PrepareJob");
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        Debug.Log("AbsoluteAccuracy / CastJob");
        IsCanCrit = true;
        yield return null;
    }

}
