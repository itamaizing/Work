using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public abstract class Talent : MonoBehaviour
{
	[SerializeField]
	private TalentData _data;

	//private List<TalentData> _dependentTalents = new();

	[SerializeReference, SubclassSelector]
	public OpenCondition OpenCondition = new EmptyCondition();

    public Character character;

	public TalentData Data => _data;
	/*
	private void OnValidate()
	{
		Init();
	}*/

	public void Init(TalentSystem owner, int groupId, int rowIndex)
	{
		_data.Init();
		_data.Name = GetType().Name;
		_data.Group = groupId;
		_data.Row = rowIndex;

		if (OpenCondition == null) OpenCondition = new EmptyCondition();
		_data.condition = OpenCondition;

		if (OpenCondition is IScopedCondition scoped)
			scoped.SetOwner(owner, groupId);

		_data.ConditionDescription = OpenCondition.ConditionDescription();
	}

    public abstract void Enter();

	public abstract void Exit();

	public void SetActive(bool isActive, int lvl = 0)
	{
		_data.SetOpen(isActive);
		_data.SetLevel(isActive ? lvl : 0);
		if (isActive) Enter(); else Exit();
	}

	public bool TrySetActive(bool isActive, int lvl = 0)
	{
		if (isActive && !OpenCondition.CanOpen) return false;
		SetActive(isActive, lvl);
		return true;
	}
	
	/*public bool CanClose()
	{
		if(_dependentTalents.Count <= 0) return true;

		foreach(var talent in _dependentTalents)
		{
			if (talent.IsOpen) return false;
		}
		return true;
	}*/

	public void AddDependendTalent(TalentData data)
	{
		if (data != null) _data.AddDependentTalent(data);
	}
}
