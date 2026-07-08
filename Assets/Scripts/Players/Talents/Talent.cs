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

/*	private void OnValidate()
	{
		Init();
	}*/

    private void Awake()
    {
		Init();
    }

    private void Init()
    {
        _data.Name = GetType().Name;
        if (OpenCondition == null)
        {
            OpenCondition = new EmptyCondition();
        }
        _data.condition = OpenCondition;
        _data.ConditionDescription = OpenCondition.ConditionDescription();
    }

    public abstract void Enter();

	public abstract void Exit();

	public void SetActive(bool isActive, int lvl = 0)
	{
		Debug.Log(isActive + "Lvl: " + lvl);
		_data.SetOpen(isActive);
		_data.SetLevel(lvl);
		if (isActive && OpenCondition.CanOpen)
		{
			Enter();
		}
		else
		{
			Exit();
		}
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
		//_dependentTalents.Add(data);
		if(data != null)
			_data.AddDependentTalent(data);
    }
}
