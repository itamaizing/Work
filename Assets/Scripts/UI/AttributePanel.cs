using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AttributePanel : MonoBehaviour
{
    [SerializeField] private GameObject _content;
    [SerializeField] private AttributeItem[] _attributes;
    [SerializeField] private TextMeshProUGUI _count;
    //[SerializeField] private HeroComponent _hero;

    private int[] _modif = new int[7];
    private List<int> _changes = new List<int>();

    private int _points = 10;
    private int _bonus = 0;

    public void SwitchVisible(bool visible)
    {
        _content.SetActive(visible);
    }

	public void SwitchVisible()
	{
		_content.SetActive(!_content.activeSelf);
	}

	public void Init(HeroComponent character)
    {
        _attributes[0].Init(null, character.PlayerData.Health);
        _attributes[1].Init(null, character.PlayerData.Stamina);
        _attributes[2].Init(null, character.PlayerData.HealthInfo.DefaultPhysicsDamage);
        _attributes[3].Init(null, character.PlayerData.HealthInfo.DefaultMagicDamage);
        _attributes[4].Init(null, character.PlayerData.HealthInfo.EvadeMeleeDamage);
        _attributes[5].Init(null, character.PlayerData.HealthInfo.EvadeRangeDamage);
        _attributes[6].Init(null, character.PlayerData.HealthInfo.EvadeMagicDamage);

        for(int i = 0; i < _attributes.Length; i++) 
        {
            _modif[i] = 0;
            int id = i;
            _attributes[i].Plus.onClick.AddListener(() => Add(id));
            _attributes[i].Minus.onClick.AddListener(() => Remove(id));
        }
    }

    private void OnValueChange(float value, int id)
    {
        if (value > 0 && _points > 0)//if we adding value and if we have some free points, we can add attribute value
        {
           // _modif[id] =+ value;
            _points--;
            _attributes[id].Add();
        }
        if(value < 0 && _modif[id] > 0) //if we removing value and if value is bigger than default then we removing value and adding points
		{
		//	_modif[id] =+ value;
            _points++;
			_attributes[id].Remove();
		}
        
        if(_points <=0)
        {
            for(int i = 0; i < _attributes.Length ; i++) 
            {
                _attributes[i].mat = Instantiate(_attributes[i].Ico.material);
				_attributes[i].Ico.material = _attributes[i].mat;
				_attributes[i].mat.SetFloat("_GrayscaleAmount", 1);
			}
        }
        else
        {
			for (int i = 0; i < _attributes.Length; i++)
			{
				_attributes[i].mat = Instantiate(_attributes[i].Ico.material);
				_attributes[i].Ico.material = _attributes[i].mat;
				_attributes[i].mat.SetFloat("_GrayscaleAmount", 0);
			}
		}
    }

    private void Add(int id)
    {
        if(_points > 0)
        {
			_modif[id]++;
			_points--;
			_attributes[id].Add();
            _changes.Add(id);
            _count.text = _points.ToString();
		}
        if(_points <=0)
        {
			for (int i = 0; i < _attributes.Length; i++)
			{
				_attributes[i].mat = Instantiate(_attributes[i].Ico.material);
				_attributes[i].Ico.material = _attributes[i].mat;
				_attributes[i].mat.SetFloat("_GrayscaleAmount", 1);
			}
		}
    }    

    private void Remove(int id)
    {
		if (_modif[id] > 0)
		{
			_modif[id]--;
			_points++;
			_attributes[id].Remove();
            _changes.Remove(id);
			_count.text = _points.ToString();
		}
		if (_points > 0)
		{
			for (int i = 0; i < _attributes.Length; i++)
			{
				_attributes[i].mat = Instantiate(_attributes[i].Ico.material);
				_attributes[i].Ico.material = _attributes[i].mat;
				_attributes[i].mat.SetFloat("_GrayscaleAmount", 0);
			}
		}
	}

    public void AddPoints(int value)
    {
        _points += value;
		_count.text = _points.ToString();
	}

    public bool RemovePoints(int value) 
    {
        if (_points >= value)
        {
            _points -= value;
			_count.text = _points.ToString();
			return true;
        }
        else if(_changes.Count > 0)
        {
            Remove(_changes[0]);
            _changes.RemoveAt(0);
			_count.text = _points.ToString();
			return true;
        }
        return false;
    }

    public void SetBonus(int bonus)
    {
        if(_bonus > bonus)
        {
            RemovePoints(_bonus - bonus);
            _bonus = bonus;
        }
        else
        {
            AddPoints(bonus - _bonus);
            _bonus = bonus;
        }
    }
}
