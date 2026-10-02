using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
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
	
	private TalentSystem _owner;
	private int _groupId;
	private int _rowIndex;
	
	/*
	private void OnValidate()
	{
		Init();
	}*/

	public void Init(TalentSystem owner, int groupId, int rowIndex)
	{
		_owner = owner;
		_groupId = groupId;
		_rowIndex = rowIndex;

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
	
	public bool IsRowLocked()
	{
		var group = _owner?.TalentsGroups.FirstOrDefault(g => g.ID == _groupId);
		return group != null && !group.CanOpenRow(_owner, _rowIndex);
	}
	
	public string GetRowConditionDescriptionIfLocked()
	{
		if (_owner == null) return string.Empty;
		var group = _owner.TalentsGroups.FirstOrDefault(g => g.ID == _groupId);
		if (group == null) return string.Empty;
		if (group.CanOpenRow(_owner, _rowIndex)) return string.Empty;
		return group.GetRowConditionDescription(_rowIndex);
	}
	
	public string GetLockDescription()
	{
		var parts = new List<string>();

		if (!OpenCondition.CanOpen && !string.IsNullOrEmpty(Data.ConditionDescription))
			parts.Add(Data.ConditionDescription);

		string rowDescription = GetRowConditionDescriptionIfLocked();
		if (!string.IsNullOrEmpty(rowDescription))
			parts.Add(rowDescription);

		return string.Join("\n", parts);
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
