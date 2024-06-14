using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeriesOfStrikes : MonoBehaviour
{
    private float _usedRunesValue = 0;
    private float _timer = 0;
	private float _baseTimer = 2f; //time and timer between losing streak
	private float _multiplySpeed = .05f;
	private int _hitCount = 0;
	private bool _isInTheRow;
	private Character _curTarget;

	private bool _list1 = true;
	private bool _list2 = true;
	private List<AbilityForm> _formList = new List<AbilityForm> {AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical };
	private List<AbilityForm> _formList2 = new List<AbilityForm> {AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Magic };
	public void MakeHit(Character target, AbilityForm form, float usedRuneValue)
    {
		if(form == _formList[_hitCount] || form == _formList2[_hitCount]) 
		{
			_hitCount++;
		}

		if (form == _formList[_hitCount] && _list1)
		{
			_list1 = true;
			_isInTheRow = true;
			_usedRunesValue += usedRuneValue;
		}
		else
		{
			_list1 = false;
		}
		if (form == _formList2[_hitCount] && _list2)
		{
			_list2 = true;
			_isInTheRow = true;
			_usedRunesValue += usedRuneValue;
		}
		else
		{
			_list2 = false;
		}
	}

    public void Timer()
    {
		if (_isInTheRow)
		{
			_timer -= Time.deltaTime;
			if (_timer <= 0)
			{
				_curTarget = null;
				_multiplySpeed = 0.05f;
				//_attackSpeed *= (1 - _multiplySpeed);
				Debug.Log("lose streak");
				_timer = _baseTimer;
				_isInTheRow = false;
				_hitCount = 0;
			}
		}
	}

	private void LastHit()
	{

	}
}
