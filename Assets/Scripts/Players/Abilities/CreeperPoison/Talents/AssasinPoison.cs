using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssasinPoison : Talent
{
    [SerializeField] public CreeperInvisible _invisibleCreeper;

    private int _currentChargePoison;
    private int _maxChargePoison = 3;

    private float _timeAccumulateCharge;
    private float _startTimeAccumulateCharge = 3f;

    public int CurrentChargePoison { get => _currentChargePoison; set => _currentChargePoison = value; }

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    private void Update()
    {
        if (Data.IsOpen && _currentChargePoison < 3)
        {
            if (character.CharacterState.CheckForState(States.CreeperInvisible))
            {
                _timeAccumulateCharge -= Time.deltaTime;
                if (_timeAccumulateCharge <= 0)
                {
                    AccumulateChargePoison();
                    _timeAccumulateCharge = _startTimeAccumulateCharge;
                }
            }
        }
    }
    
    public void CmdSpendCharge(Character target, float lifeTimePoisonBoneStack)
    {
        if (character.CharacterState.CheckForState(States.CreeperInvisible))
        {
            if (CurrentChargePoison > 0)
            {
                target.CharacterState.CmdAddState(States.PoisonBone, lifeTimePoisonBoneStack, 0, character.gameObject, null);
                CurrentChargePoison--;
            }
        }
    }

    private void AccumulateChargePoison()
    {
        if (_currentChargePoison < _maxChargePoison)
        {
            _currentChargePoison++;
        }
    }

}
