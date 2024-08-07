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
        if (IsActive)
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
            Debug.Log("Check State in AssasinPoison == " + character.CharacterState.CheckForState(States.CreeperInvisible));
            if (CurrentChargePoison > 0)
            {
                Debug.Log("CurrentChargePoison == " + CurrentChargePoison);
                target.CharacterState.CmdAddState(States.PoisonBone, 6f, 0);
                CurrentChargePoison--;
                Debug.Log("AddStacks PoisonBone");
                Debug.Log("Before for / _invisibleCreeper.CurrentChargePoison == " + CurrentChargePoison);
            }
        }
    }

    private void AccumulateChargePoison()
    {
        if (_currentChargePoison < _maxChargePoison)
        {
            _currentChargePoison++;
            Debug.Log("CurrentChargePoison == " + _currentChargePoison);
            Debug.Log("TimeAccumulateCharge == " + _timeAccumulateCharge);
        }
    }

    private void CmdAccumulateChargePoison()
    {
        if (_currentChargePoison < _maxChargePoison)
        {
            _currentChargePoison++;
            Debug.Log("CMD CurrentChargePoison == " + _currentChargePoison);
            Debug.Log("CMD TimeAccumulateCharge == " + _timeAccumulateCharge);
        }
    }

}
