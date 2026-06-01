using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public abstract class Talent : MonoBehaviour
{
	[SerializeField]
	private TalentData _data;

	private List<TalentData> _dependentTalents = new();

	[SerializeReference, SubclassSelector]
	public OpenCondition OpenCondition;

    public Character character;

	public TalentData Data => _data;

	private void OnValidate()
	{
		_data.Name = GetType().Name;
		if(OpenCondition == null)
		{
			OpenCondition = new EmptyCondition();
		}
        _data.condition = OpenCondition;
        _data.ConditionDescription = OpenCondition.ConditionDescription();
		OpenCondition.Validete(Data);
		//Debug.Log("Open condition " + OpenCondition.CanOpen);
	}

	public abstract void Enter();

	public abstract void Exit();

	public void SetActive(bool isActive, int lvl = -1)
	{
		_data.IsOpen = isActive;
		_data.Level = lvl;
		if (isActive && OpenCondition.CanOpen)
		{
			Enter();
		}
		else
		{
			Exit();
		}
	}

	public bool CanClose()
	{
		if(_dependentTalents.Count <= 0) return true;

		foreach(var talent in _dependentTalents)
		{
			if (talent.IsOpen) return false;
		}
		return true;
	}

	public void AddDependendTalent(TalentData data)
	{
		_dependentTalents.Add(data);
	}
}
