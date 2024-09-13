using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public abstract class Talent : NetworkBehaviour
{
    private bool _isActive;
    public string Name;
    public string Description;
    public Sprite Ico;
    public Character Character;

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
    [SerializeField] private List<Talent> _talents2;
    [SerializeField] private List<Talent> _talents3;
    //private List<Talent> _activeTalents = new List<Talent>();
    private TalentColumn _panel;
    private int _points = 10;

    public TalentColumn Panel => _panel;
    public List<Talent> Talents => _talents;
    //public List<Talent> ActiveTalents => _activeTalents;

    public void Initialize()
    {
	    if(SceneManager.GetActiveScene().buildIndex == 1) 
		    _panel = TalentManager.Instance.AddPanel(this);
    }
    public void AddPoints(int value)
    {
        _points += value;
    }

   // [Command]
    public void CmdSwitchActive(int id, int row)
    {
        if (id > _talents.Count) return;
        //if (_activeTalents.Contains(_talents[id]))
        if (_talents[id].IsActive)
        {
            Remove(id, row);
         //   RpcRemove(id);
			Debug.Log("Removes");
		}
        else
        {
            Add(id, row);
           // RpcAdd(id);
			Debug.Log("Add");
		}
    }
	public void SetActive(int id, int row,  bool value)
	{
		if (id > _talents.Count) return;
		//if (_activeTalents.Contains(_talents[id]))
		if (value)
		{
			Add(id, row);
			//   RpcRemove(id);
		}
		else
		{
            Remove(id, row);
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
    public void CmdAdd(int id, int row)
    {
        Add(id, row);
        RpcAdd(id, row);
    }

	[Command]
	public void CmdRemove(int id, int row) 
    {
        Remove(id, row); 
        RpcRemove(id, row);
    }

    [ClientRpc]
    private void RpcAdd(int id, int row)
    {
		Add(id, row);
	}

	[ClientRpc]
	private void RpcRemove(int id, int row)
    {
		Remove(id, row);
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

	public void Add(int id, int row)
    {
		Debug.Log("Add");
        switch (row)
        {
            case 0:
				if (_talents.Count >= id && _points > GetActiveTalentCount())
				{
					Debug.Log("Add2222");
					//_activeTalents.Add(_talents[id]);
					//_activeTalents[_activeTalents.Count- 1].Enter();
					_talents[id].Enter();
					_talents[id].SetActive(true);
					_points--;
				}
				break;
            case 1:
				if (_talents2.Count >= id && _points > GetActiveTalentCount())
				{
					Debug.Log("Add2222");
					//_activeTalents.Add(_talents[id]);
					//_activeTalents[_activeTalents.Count- 1].Enter();
					_talents2[id].Enter();
					_talents2[id].SetActive(true);
					_points--;
				}
				break;
            case 2:
				if (_talents3.Count >= id && _points > GetActiveTalentCount())
				{
					Debug.Log("Add2222");
					//_activeTalents.Add(_talents[id]);
					//_activeTalents[_activeTalents.Count- 1].Enter();
					_talents3[id].Enter();
					_talents3[id].SetActive(true);
					_points--;
				}
				break;
            default:
                break;
        }

		
    }

	public void Remove(int id, int row)
    {
        switch (row)
        {
            case 0:
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
				break;
            case 1:
				if (_talents2.Count >= id)
				{
					Debug.Log("Removes22222");
					/*_activeTalents[_activeTalents.Count - 1].Exit();
					_activeTalents.Remove(_talents[id]);
					_talents[id].isActive = false;*/
					_talents2[id].Exit();
					_talents2[id].SetActive(false);
					_points++;
				}
				break;
            case 2:
				if (_talents3.Count >= id)
				{
					Debug.Log("Removes22222");
					/*_activeTalents[_activeTalents.Count - 1].Exit();
					_activeTalents.Remove(_talents[id]);
					_talents[id].isActive = false;*/
					_talents3[id].Exit();
					_talents3[id].SetActive(false);
					_points++;
				}
				break;
            default:
                break;
        }


       /* Debug.Log("Removes");
		if (_talents.Count >= id)
		{
			Debug.Log("Removes22222");
			/*_activeTalents[_activeTalents.Count - 1].Exit();
			_activeTalents.Remove(_talents[id]);
			_talents[id].isActive = false;
			_talents[id].Exit();
			_talents[id].SetActive(false);
			_points++;
		}*/
	}


        /* Debug.Log("Removes");
         if (_talents.Count >= id)
         {
             Debug.Log("Removes22222");
             /*_activeTalents[_activeTalents.Count - 1].Exit();
             _activeTalents.Remove(_talents[id]);
             _talents[id].isActive = false;
             _talents[id].Exit();
             _talents[id].SetActive(false);
             _points++;
         }*/

    public void EnterAll()
    {
        foreach (Talent talent in _talents)
        {
            talent.Enter();
            talent.SetActive(true);
            _points--;
        }
        foreach (Talent talent in _talents2)
        {
            talent.Enter();
            talent.SetActive(true);
            _points--;
        }
        foreach (Talent talent in _talents3)
        {
            talent.Enter();
            talent.SetActive(true);
            _points--;
        }
		foreach (Talent talent in _talents2)
		{
			talent.Enter();
			talent.SetActive(true);
			_points--;
		}
		foreach (Talent talent in _talents3)
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
		foreach (Talent talent in _talents2)
		{
			_points++;
			talent.Exit();
			talent.SetActive(false);
			//_activeTalents.Remove(talent);
		}
		foreach (Talent talent in _talents3)
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
        for (int i = 0; i < _talents.Count; i++)
        {
            if (_talents[i].IsActive)
            {
                count++;
            }
        }
        return count;
    }
}
