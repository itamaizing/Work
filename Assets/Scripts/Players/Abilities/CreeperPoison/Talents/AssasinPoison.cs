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

    public int CurrentChargeAssasinPoison { get => _currentChargePoison; set => _currentChargePoison = value; }

    private void Start()
    {
        Enter();
    }

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
        if (IsActive && _currentChargePoison < 3 && Character.CharacterState.CheckForState(States.CreeperInvisible))
        {
            Debug.Log("AssasinPoison / currentCharge++");
            _timeAccumulateCharge -= Time.deltaTime;
            if (_timeAccumulateCharge <= 0)
            {
                AccumulateChargePoison();
            }
        }
    }
    
    public void CmdSpendCharge(Character target, float lifeTimePoisonBoneStack)
    {
        if (Character.CharacterState.CheckForState(States.CreeperInvisible))
        {
            if (_currentChargePoison > 0)
            {
                target.CharacterState.CmdAddState(States.PoisonBone, lifeTimePoisonBoneStack, 0, Character.gameObject, null);
                _currentChargePoison--;
            }
        }
    }

    private void AccumulateChargePoison()
    {
        if (_currentChargePoison < _maxChargePoison)
        {
            _currentChargePoison++;
            Debug.Log("AssasinPoison / AccumulateChargePoison / CurrentChargePoison == " + _currentChargePoison);
            _timeAccumulateCharge = _startTimeAccumulateCharge;
            Debug.Log("AssasinPoison / AccumulateChargePoison / timeAccumulateCharge == " + _timeAccumulateCharge);
        }
    }

}
