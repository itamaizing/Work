using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Talent : NetworkBehaviour
{
    public bool isActive;
	public string Name;
	public string Description;
	public Sprite ico;
    public Character character;

	public abstract void Enter();

	public abstract void Exit();

}

public class TalentSystem : NetworkBehaviour
{
    [SerializeField] private List<Talent> _talents;
    private List<Talent> _activeTalents = new List<Talent>();
    private int _points = 10;

	public List<Talent> Talents => _talents;
    public List<Talent> ActiveTalents => _activeTalents;

	public void AddPoints(int value)
	{
		_points += value;
	}

   // [Command]
    public void CmdSwitchActive(int id)
    {
        if (id > _talents.Count) return;
        if (_activeTalents.Contains(_talents[id]))
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
    private void RpcAdd(Talent talent)
    {
        Add(talent);
    }

    private void Add(int id)
    {
		Debug.Log("Add");
		if (_talents.Count >= id && _points > _activeTalents.Count)
        {	
			_activeTalents.Add(_talents[id]);
            _activeTalents[_activeTalents.Count- 1].Enter();
            _talents[id].isActive = true;
        }
    }

    private void Remove(int id) 
    {
		Debug.Log("Removes");
		if (_talents.Count >= id)
		{
			_activeTalents[_activeTalents.Count - 1].Exit();
			_activeTalents.Remove(_talents[id]);
			_talents[id].isActive = false;
		}
	}

    private void EnterAll()
    {
        foreach(Talent talent in _activeTalents)
        {
            talent.Enter();
        }
    }

    private void ExitAll()
    {
		foreach (Talent talent in _activeTalents)
		{
			talent.Exit();
            _activeTalents.Remove(talent);
		}
	}

    public void Add(Talent talent)
    {
        _activeTalents.Add(talent);
        talent.Enter();
    }

    public void Remove(Talent talent)
    {
        talent.Exit();
        _activeTalents.Remove(talent);
    }
}
