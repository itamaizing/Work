using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Talent : NetworkBehaviour
{
    private bool _isActive;
	public string Name;
	public string Description;
	public Sprite ico;
    public Character character;

    public bool IsActive => _isActive;

	public abstract void Enter();

	public abstract void Exit();

    public void SetActive(bool isActive)
    {
        _isActive = isActive;
    }
}

public class TalentSystem : NetworkBehaviour
{
    [SerializeField] private List<Talent> _talents;
    //private List<Talent> _activeTalents = new List<Talent>();
    private TalentColumn _panel;
    private int _points = 10;

    public TalentColumn Panel => _panel;
	public List<Talent> Talents => _talents;
    //public List<Talent> ActiveTalents => _activeTalents;

	public void Initialize()
	{
		//_panel = TalentManager.Instance.AddPanel(this);
	}
	public void AddPoints(int value)
	{
		_points += value;
	}

   // [Command]
    public void CmdSwitchActive(int id)
    {
        if (id > _talents.Count) return;
        //if (_activeTalents.Contains(_talents[id]))
        if (_talents[id].IsActive)
        {
            Remove(id);
         //   RpcRemove(id);
			Debug.Log("Removes");
		}
        else
        {
            Add(id);
           // RpcAdd(id);
			Debug.Log("Add");
		}
    }
	public void SetActive(int id, bool value)
	{
		if (id > _talents.Count) return;
		//if (_activeTalents.Contains(_talents[id]))
		if (value)
		{
			Add(id);
			//   RpcRemove(id);
		}
		else
		{
            Remove(id);
			// RpcAdd(id);
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
    public void CmdAdd(int id)
    {
        Add(id);
        RpcAdd(id);
    }

	[Command]
	public void CmdRemove(int id) 
    {
        Remove(id); 
        RpcRemove(id);
    }

    [ClientRpc]
    private void RpcAdd(int id)
    {
		Add(id);
	}

	[ClientRpc]
	private void RpcRemove(int id)
    {
		Remove(id);
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

	public void Add(int id)
    {
		Debug.Log("Add");
		if (_talents.Count >= id && _points > GetActiveTalentCount())
        {
			Debug.Log("Add2222");
			//_activeTalents.Add(_talents[id]);
			//_activeTalents[_activeTalents.Count- 1].Enter();
			_talents[id].Enter();
            _talents[id].SetActive(true);
            _points--;
        }
    }

	public void Remove(int id) 
    {
		Debug.Log("Removes");
		if (_talents.Count >= id)
		{
			Debug.Log("Removes22222");
			/*_activeTalents[_activeTalents.Count - 1].Exit();
			_activeTalents.Remove(_talents[id]);
			_talents[id].isActive = false;*/
			_talents[id].Exit();
			_talents[id].SetActive(false);
			_points++;
		}
	}

	public void EnterAll()
    {
        foreach(Talent talent in _talents)
        {
            talent.Enter();
            talent.SetActive(true);
            _points--;
        }
    }

	public void ExitAll()
    {
		foreach (Talent talent in _talents)
		{
            _points++;
			talent.Exit();
            talent.SetActive(false);
            //_activeTalents.Remove(talent);
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
        int count = 0;
        for(int i = 0; i< _talents.Count; i++)
        {
            if (_talents[i].IsActive) 
            {  
                count++;
            }
        }
        return count;
    }
}
