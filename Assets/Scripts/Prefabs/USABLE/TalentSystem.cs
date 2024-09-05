using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
	    if(SceneManager.GetActiveScene().buildIndex == 1) 
		    _panel = TalentManager.Instance.AddPanel(this);
    }
    public void AddPoints(int value)
    {
        _points += value;
    }
    
	public void SetActive(int id, int row,  bool value)
	{
		if (id > _talents.Count) return;
		//if (_activeTalents.Contains(_talents[id]))
		if (value)
		{

		}
		else
		{

		}
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
