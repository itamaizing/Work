using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeriesOfStrikes : MonoBehaviour
{
	[SerializeField] private Character _player;
    private float _usedRunesValue1 = 0;
    private float _usedRunesValue2 = 0;
    private float _timer = 0;
	private float _baseTimer = 2f; //time and timer between losing streak
	//private float _multiplySpeed = .05f;
	private int _hitCount1 = 0;
	private int _hitCount2 = 0;
	private bool _isInTheRow;
	private Character _curTarget;

	//private bool _list1 = true;
	//private bool _list2 = true;
	private List<AbilityForm> _formList = new List<AbilityForm> {AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical };
	private List<AbilityForm> _formList2 = new List<AbilityForm> {AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Physical, AbilityForm.Magic };

	private void Update()
	{
		Timer();
	}
	public float GetMultipliedSpeed()
	{
		if(_hitCount1 >= _hitCount2)
		{
			return Mathf.Pow(2, _hitCount1);
		}
		else
		{
			return Mathf.Pow(2, _hitCount2);
		}
	}
	public bool MakeHit(Character target, AbilityForm form, float usedRuneValue)
    {
		if(form == _formList[_hitCount1]  && target == _curTarget || target == null) 
		{
			//_list1 = true;
			_isInTheRow = true;
			_curTarget = target;
			_usedRunesValue1 += usedRuneValue;
			_hitCount1++;
			
		}
		else
		{
			_isInTheRow = true;
			_hitCount1 = 0;
			_usedRunesValue1 = usedRuneValue;
			_curTarget = target;
		}
		if(form == _formList2[_hitCount2] && target == _curTarget || target == null)
		{
			//_list2 = true;
			_isInTheRow = true;
			_curTarget = target;
			_usedRunesValue2 += usedRuneValue;
			_hitCount2++;
		}
		else
		{
			_isInTheRow = true;
			_hitCount2 = 0;
			_usedRunesValue2 = usedRuneValue;
			_curTarget = target;
		}
		if(_hitCount1 >=6)
		{
			LastHit(_usedRunesValue1);
			return true;
		}
		if(_hitCount2 >= 6)
		{
			LastHit(_usedRunesValue2);
			return true;
		}
		return false;
	}

	public void Timer()
    {
		if (_isInTheRow)
		{
			_timer -= Time.deltaTime;
			if (_timer <= 0)
			{
				_curTarget = null;
				//_multiplySpeed = 0.05f;
				//_attackSpeed *= (1 - _multiplySpeed);
				Debug.Log("lose streak");
				_timer = _baseTimer;
				_isInTheRow = false;
				_hitCount1 = 0;
				_hitCount2 = 0;
				_curTarget = null;
				//_list1 = true;
				//_list2 = true;
			}
		}
	}

	private void LastHit(float value)
	{
		_player.RuneComponent.Add(value);
	}
}

