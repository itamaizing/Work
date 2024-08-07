using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToxiqueCloud : Talent
{
    [SerializeField] private BonePoison _bonePoisonPrefab;
    private BonePoison _bonePoison;

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void ApplyBonePoison(HealthComponent targetHealth)
    {
        Debug.Log("ApplyBonePoison in ToxiqueCloud");
        CmdApplyBonePoison(targetHealth);
    }

    private void CmdApplyBonePoison(HealthComponent targetHealth)
    {
        _bonePoison = targetHealth.GetComponentInChildren<BonePoison>();
        if (_bonePoison == null)
        {
            _bonePoison = Instantiate(_bonePoisonPrefab, targetHealth.transform);
            _bonePoison.AddStacks(targetHealth);
            Debug.Log("if CmdApplyBonePoison == " + _bonePoison.CurrentStacks);

        }
        else
        {
            _bonePoison.AddStacks(targetHealth);
            Debug.Log("else CmdApplyBonePoison == " + _bonePoison.CurrentStacks);
        }
    }
}
