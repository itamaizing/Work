using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class TalentData
{
	[SerializeField] private List<string> _descriptionsForInfoPanel;
	[SerializeField] private List<TalentStateInfo> _stateInfos = new();

	private string _name;
	private List<TalentData> _dependentTalents = new();
	private bool _isOpen = false;
	private int _level = 0;
	
	public string Description = string.Empty;
    public string ConditionDescription = string.Empty;
    public Sprite Icon;
	public int Group, Row = 0;
	public int MaxLvl = 1;

	public OpenCondition condition;

	public int Level => _level;
	public bool IsOpen => _isOpen;
	public string Name
	{
		get { return _name; }
		set
		{
			_name = value;
		}
	}
	public void SetLevel(int value)
    {
        _level = value;

        /*if (condition != null)
            condition.Validete(this);*/
    }
    public void SetOpen(bool value)
    {
        _isOpen = value;
		if(condition != null && _isOpen)
			condition.Validete(this);
    }

    public List<string> DescriptionsForInfoPanel { get => _descriptionsForInfoPanel; }
	public List<TalentStateInfo> StateInfos => _stateInfos;

    public void Init()
    {
        _dependentTalents = new();
    }

	public TalentData(string name, bool isOpen)
	{
        // Name = name;
        _isOpen = isOpen;
        _dependentTalents = new();
    }

	public void AddDependentTalent(TalentData talent)
    {
		if (talent == null) return;
		/*var talentFind = _dependentTalents.FirstOrDefault(t => t.Name == talent.Name);
        if (talentFind != null)
		{ 
			talentFind = talent;
			return;
        }*/

        //_dependentTalents.Clear();
        if (!_dependentTalents.Contains(talent))
        {
            _dependentTalents.Add(talent);
        }
    }

	public bool CanClose()
    {
        //return true;
		Debug.Log(_dependentTalents.Count);
        foreach (var talent in _dependentTalents)
        {
            Debug.Log(talent);
        }
        if (_dependentTalents.Count <= 0) return true;
        foreach (var talent in _dependentTalents)
        {
			Debug.Log(talent.Name);
            if (talent == null) continue;
            if (talent.IsOpen) return false;
        }
        return true;
    }
}
