using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TalentData
{
	[SerializeField] private List<string> _descriptionsForInfoPanel;
	[SerializeField] private List<TalentStateInfo> _stateInfos = new();

	private string _name;
	private List<TalentData> _dependentTalents = new();
	private bool _isOpen = false;
	
	public string Description = string.Empty;
    public string ConditionDescription = string.Empty;
    public Sprite Icon;
	public int Group, Row = 0;
	public int Level = -1;
	public int MaxLvl = -1;

	public OpenCondition condition;

	public bool IsOpen => _isOpen;
	public string Name
	{
		get { return _name; }
		set
		{
			//Debug.Log(value);
			_name = value;
		}
	}

	public void SetOpen(bool value)
    {
        _isOpen = value;
		if(condition != null)
			condition.Validete(this);
    }

    public List<string> DescriptionsForInfoPanel { get => _descriptionsForInfoPanel; }
	public List<TalentStateInfo> StateInfos => _stateInfos;

	public TalentData(string name, bool isOpen)
	{
        // Name = name;
        _isOpen = isOpen;
	}

	public void AddDependentTalent(TalentData talent)
    {
		if (talent == null) return;
		_dependentTalents.Clear();
        if (!_dependentTalents.Contains(talent))
        {
            _dependentTalents.Add(talent);
        }
    }

	public bool CanClose()
    {
        //return true;
        if (_dependentTalents.Count <= 0) return true;
        foreach (var talent in _dependentTalents)
        {
            if (talent.IsOpen) return false;
        }
        return true;
    }
}
