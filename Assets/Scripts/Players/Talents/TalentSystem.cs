using System;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public struct TalentStateInfo
{
    public string StateName;
    [TextArea] public string Description;
}

public class TalentSystem : NetworkBehaviour
{
    [SerializeField] private List<TalentsGroup> _talents;
    [SerializeField] private List<Talent> _allTalents;

    private Level _lvl;
    private int _points = 1;
    private int _prevValue = 1;
    
    private bool _initialized;
    private bool _pointsLoaded;
    
    public bool IsPreview { get; set; }

   public Level Level { get => _lvl; set => _lvl = value; }

    public List<TalentsGroup> TalentsGroups => _talents;
    public List<Talent> ActiveTalents => GetActiveTalents();

    public List<Talent> GetActiveTalents()
    {
        List<Talent> activeTalents = new();
        foreach(TalentsGroup group in TalentsGroups)
        {
            foreach(TalentRow row in group.TalentRows)
            {
                foreach(Talent talent in row.Talents)
                {
                    if(talent.Data.IsOpen)
                    {
                        activeTalents.Add(talent);
                    }
                }
            }
        }

        return activeTalents;
	}
    
    public int Points => _points;
    public bool CanOpenTalent => _points > 0;

    private void Awake()
    {
        GetComponentsInChildren<Talent>(true, _allTalents);

        foreach (var item in _allTalents)
        {
            item.Owner = this;
            item.Init();
        }
    }

    private void OnDisable()
    {
        if (_lvl != null)
        {
            _lvl.LVLUped -= AddPoint;
            _lvl.LevelLoaded -= OnLevelLoaded;
        }
    }

    public void RefreshEffects() => RefreshTalentVisuals();

    private void ApplyEffect(Talent talent, bool on) => talent?.ApplyEffect(on);

    // [Command]
    public void Initialize(Level level)
    {
        if (_initialized)
        {
            RefreshTalentVisuals();
            return;
        }
        _initialized = true;

        if (level != null)
        {
            _lvl = level;
            _lvl.LVLUped += AddPoint;
            _lvl.LevelLoaded += OnLevelLoaded;
        }

        _prevValue = _lvl.Value;

        if (!_pointsLoaded)
            _points = _lvl.Value;

        RefreshTalentVisuals();
    }
    
    private void OnLevelLoaded(int value)
    {
        _prevValue = value;
    }
    
    private void RefreshTalentVisuals()
    {
        foreach (var talentRow in _talents.SelectMany(g => g.TalentRows))
        foreach (var talent in talentRow.Talents)
        {
            talent.Data.Name = talent.GetType().Name;
            ApplyEffect(talent, talent.Data.IsOpen);
        }
    }

    [ClientRpc]
	public void Initialize2()
	{
		foreach (var talentRow in _talents.SelectMany(talentsGroup => talentsGroup.TalentRows))
		{
            foreach (var talent in talentRow.Talents)
            {
                talent.Data.Name = talent.GetType().Name;
                ApplyEffect(talent, talent.Data.IsOpen);
            }
		}
	}

    public void AddPoint(int value)
    {
        if (_prevValue != value)
        {
            Debug.Log("Add" + value);
            _points++;
            _prevValue = value;
        }
    }

	public void AddPoints(int value)
    {
        _points += value;
    }

    public void SetPoints(int value)
    {
        _points = value;
        _prevValue = _lvl != null ? _lvl.Value : _prevValue;
        _pointsLoaded = true;
    }

    public void SetActive(int group, int row ,int id, bool value)
	{
        _talents[group].TalentRows[row].Talents[id].SetActive(value);
        if (value) _points--;

        else
        {
            int maxPoints = GetMaxTalentPoints();
            if (_points < maxPoints) _points++;
        }
    }

    public int GetMaxTalentPoints()
    {
        return _lvl != null ? _lvl.Value : 1;
    }

    public void SetActive(int group, int row, string name, bool value)
    {
        var talentGroup = _talents?.FirstOrDefault(id => id.ID == group);

        var talent = talentGroup.TalentRows[row].Talents?.FirstOrDefault(o => o.Data.Name == name);
        talent.SetActive(value);
        if (value) _points--;

        else
        {
            int maxPoints = GetMaxTalentPoints();
            if (_points < maxPoints) _points++;
        }
    }

    public void SwitchTalent(int id, int row, string talentName, bool isActive)
    {
        var talentGroup = TalentsGroups?.FirstOrDefault(o => o.ID == id);
        if (talentGroup == null)
        {
            return;
        }

        if (talentGroup.TalentRows == null || row < 0 || row >= talentGroup.TalentRows.Count)
        {
            return;
        }

        var talentRow = talentGroup.TalentRows[row];
        
        var talent = talentRow.Talents?.FirstOrDefault(o => 
            (o.Data != null && o.Data.Name == talentName) || 
            o.GetType().Name == talentName
        );

        if (talent == null)
        {
            return;
        }

        ApplyEffect(talent, isActive);
    }

	[Command]
	public void CmdSwitchTalent(int id, int row, string talentName, bool isActive)
	{
		SwitchTalent(id, row, talentName, isActive);
		ClientSwitchTalent(id, row, talentName, isActive);
	}

	[ClientRpc]
	public void ClientSwitchTalent(int id, int row, string talentName, bool isActive)
	{
		SwitchTalent(id, row, talentName, isActive);
	}

    public void Add(Talent talent)
    {
        talent.SetActive(true);
    }

    public void Remove(Talent talent)
    {
        talent.SetActive(false);
    }

    public int GetActiveTalentCount()
    {
        return ActiveTalents.Count;
    }

}