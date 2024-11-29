using System.Collections;
using UnityEngine;

public class AssasinPoison : Talent
{
    [SerializeField] public CreeperInvisible _invisibleCreeper;

    private int _currentChargePoison;
    private int _maxChargePoison = 3;

    private float _timeAccumulateCharge;
    private float _startTimeAccumulateCharge = 3f;

    private Coroutine _accumulateChargesCoroutine;

    public int CurrentChargeAssasinPoison { get => _currentChargePoison; set => _currentChargePoison = value; }

    public override void Enter()
    {
        SetActive(true);
        if (_accumulateChargesCoroutine == null)
        {
            _accumulateChargesCoroutine = StartCoroutine(AccumulateCharge());
        }
    }

    public override void Exit()
    {
        SetActive(false);
        if (_accumulateChargesCoroutine != null)
        {
            StopCoroutine(AccumulateCharge());
            _accumulateChargesCoroutine = null;
        }
    }
    
    private IEnumerator AccumulateCharge()
    {
        while (Data.IsOpen)
        {
            if (_currentChargePoison < 3 && character.CharacterState.CheckForState(States.CreeperInvisible))
            {
                _timeAccumulateCharge -= Time.deltaTime;
                if (_timeAccumulateCharge <= 0)
                {
                    AccumulateChargePoison();
                }
            }
            yield return null;
        }
    }

    public void CmdSpendCharge()
    {
        if (character.CharacterState.CheckForState(States.CreeperInvisible))
        {
            if (_currentChargePoison > 0)
            {
                _currentChargePoison--;
            }
        }
    }

    private void AccumulateChargePoison()
    {
        if (_currentChargePoison < _maxChargePoison)
        {
            _currentChargePoison++;
            Debug.Log("Accumulate AssasinPoison Charge = " + _currentChargePoison);
            _timeAccumulateCharge = _startTimeAccumulateCharge;
        }
    }

}
