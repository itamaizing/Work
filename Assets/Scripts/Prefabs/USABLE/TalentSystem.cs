using System;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Talent : NetworkBehaviour
{
	[SerializeField]
	private TalentData _data;
	
    public Character character;

    public TalentData Data => _data;

    public abstract void Enter();

    public abstract void Exit();

    public void SetActive(bool isActive)
    {
	    _data.IsOpen = isActive;
    }
}

[Serializable]
public class TalentData
{
	public int Id;
	public string Name;
	public bool IsOpen;
	public int AttributePoints = 0;
	
	
	public string Description = string.Empty;
	public Sprite Icon;

	public TalentData(int id, string name, bool isOpen)
	{
		Id = id;
		Name = name;
		IsOpen = isOpen;
	}
}

[Serializable]
public class TalentsGroup
{
	[SerializeField] private int _id;
	[SerializeField] private string _name;
	[SerializeField] private List<Talent> _talentGroup;

	public int ID => _id;
	public string Name => _name;
	public List<Talent> TalentsData => _talentGroup;
	public int TalentsCount => TalentsData.Count;
	
	public int BonusAttributePoints()
	{
		int totalBonus = 1;
		int rowLength = 3;
		int numberOfRows = TalentsData.Count / rowLength;

		for (int row = 0; row < numberOfRows; row++)
		{
			int activeCount = 0;
			for (int i = row * rowLength; i < (row + 1) * rowLength && i < TalentsData.Count; i++)
			{
				if (TalentsData[i].Data.IsOpen)
				{
					activeCount++;
				}
			}

			switch (row)
			{
				case 0:
					totalBonus += activeCount == 2 ? 1 : activeCount == 3 ? 3 : 0;
					break;
				case 1:
					totalBonus += activeCount == 2 ? 1 : activeCount == 3 ? 2 : 0;
					break;
				case 2:
					totalBonus += activeCount == 3 ? 1 : 0;
					break;
			}
		}

		return totalBonus;
	}
}

public class TalentSystem : NetworkBehaviour
{
    [SerializeField] private List<TalentsGroup> _talents;
    
    private TalentColumn _panel;
    private int _points = 10;

    public TalentColumn Panel => _panel;
    public List<TalentsGroup> Talents => _talents;

    public List<Talent> ActiveTalents => Talents.SelectMany(o => o.TalentsData).Where(a => a.Data.IsOpen).ToList();

    public void Initialize()
    {
    }
    public void AddPoints(int value)
    {
    }
    
	public void SetActive(int id, int row,  bool value)
	{
	}

    [Command]
    public void CmdAdd(Talent talent)
    {
        Add(talent);
        RpcAdd(talent);
    }

    [Command]
    public void CmdEnterAll()
    {
        EnterAll();
        RpcAddAll();
    }

    [Command]
    public void CmdExitAll()
    {
        ExitAll();
        RpcRemoveAll();
    }

    [Command]
    public void CmdAdd(int id, int row)
    {

        RpcAdd(id, row);
    }

	[Command]
	public void CmdRemove(int id, int row) 
    {

        RpcRemove(id, row);
    }

    [ClientRpc]
    private void RpcAdd(int id, int row)
    {

	}

	[ClientRpc]
	private void RpcRemove(int id, int row)
    {

	}

    [ClientRpc]
    private void RpcAddAll()
    {
        EnterAll();
    }

    [ClientRpc]
    private void RpcRemoveAll()
    {
        ExitAll();
    }

    [ClientRpc]
    public void RpcAdd(Talent talent)
    {
        Add(talent);
    }

    public void EnterAll()
    {
        foreach (TalentsGroup talentGroup in _talents)
        {
	        foreach (var talent in talentGroup.TalentsData)
	        {
		        talent.Enter();
		        talent.SetActive(true);
		        _points--;   
	        }
        }
	}

    public void ExitAll()
    {
	    foreach (TalentsGroup talentGroup in _talents)
	    {
		    foreach (var talent in talentGroup.TalentsData)
		    {
			    talent.Exit();
			    talent.SetActive(false);
			    _points++;   
		    }
	    }
	}

	public void Add(Talent talent)
    {
       // _activeTalents.Add(talent);
       talent.Enter();
       talent.SetActive(true);
    }

    public void Remove(Talent talent)
    {
        talent.Exit();
        talent.SetActive(false);
        // _activeTalents.Remove(talent);
    }

    public int GetActiveTalentCount()
    {
	   return ActiveTalents.Count;
    }
}
