using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RestorationOfGlands : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private PoisonSlap _poisonSlap;

    private float _baseProcentageReduction = 0.1f;

    private bool _isCanTrigger = false;

    public bool IsCanTrigger { get => _isCanTrigger; set => _isCanTrigger = value; }

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void ReductionCooldown()
    {
        //if (isServer)
        //{
            TargetRpcReduction();
       // }
        //else
        //{
            ReductionCooldownNotServer();
        //}
    }

    private void ReductionCooldownNotServer()
    {
        Debug.Log("RestorationOfGlands / ReductionCooldownNotServer");
        float baseCooldownSpitPoison = _spitPoison.RemainingCooldownTime;
        float baseCooldownPoisonBall = _poisonBall.CooldownTime;

        float procentageCoolwonTimeSpitPoison = baseCooldownSpitPoison * _baseProcentageReduction;
        Debug.Log("RestorationOfGlands / ReductionCooldownNotServer / procentageCooldownSpit = " + procentageCoolwonTimeSpitPoison);
        float procentageCoolwonTimePoisonBall = baseCooldownPoisonBall * _baseProcentageReduction;
        Debug.Log("RestorationOfGlands / ReductionCooldownNotServer / procentageCooldownPoisonBall = " + procentageCoolwonTimePoisonBall);

        float reducingCooldownSpitPoison = _spitPoison.CooldownTime - procentageCoolwonTimeSpitPoison;
        Debug.Log("RestorationOfGlands / ReductionCooldownNotServer / reducingCooldownSpitPoison = " + reducingCooldownSpitPoison);
        float reducingCooldownPoisonBall = _poisonBall.CooldownTime - procentageCoolwonTimePoisonBall;
        Debug.Log("RestorationOfGlands / ReductionCooldownNotServer / reducingCooldownPoisonBall = " + reducingCooldownPoisonBall);
        _spitPoison.ReductionSetCooldown(reducingCooldownSpitPoison);

        _poisonBall.ReductionSetCooldown(reducingCooldownPoisonBall);
    }

    [TargetRpc]
    private void TargetRpcReduction()
    {
        Debug.Log("RestorationOfGlands / TargetRpcReduction");
        float baseCooldownSpitPoison = _spitPoison.RemainingCooldownTime;
        float baseCooldownPoisonBall = _poisonBall.CooldownTime;

        float procentageCoolwonTimeSpitPoison = baseCooldownSpitPoison * _baseProcentageReduction;
        Debug.Log("RestorationOfGlands / TargetRpcReduction / procentageCooldownSpit = " + procentageCoolwonTimeSpitPoison);
        float procentageCoolwonTimePoisonBall = baseCooldownPoisonBall * _baseProcentageReduction;
        Debug.Log("RestorationOfGlands / TargetRpcReduction / procentageCooldownPoisonBall = " + procentageCoolwonTimePoisonBall);

        float reducingCooldownSpitPoison = _spitPoison.CooldownTime - procentageCoolwonTimeSpitPoison;
        Debug.Log("RestorationOfGlands / TargetRpcReduction / reducingCooldownSpitPoison = " + reducingCooldownSpitPoison);
        float reducingCooldownPoisonBall = _poisonBall.CooldownTime - procentageCoolwonTimePoisonBall;
        Debug.Log("RestorationOfGlands / TargetRpcReduction / reducingCooldownPoisonBall = " + reducingCooldownPoisonBall);
        _spitPoison.ReductionSetCooldown(reducingCooldownSpitPoison);

        _poisonBall.ReductionSetCooldown(reducingCooldownPoisonBall);

    }
}
