using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttributePanel : MonoBehaviour
{
    [SerializeField] private GameObject _content;
    [SerializeField] private AttributeItem[] _attributes;

    private float[] _modif = new float[7];

    private int _points = 10;
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
            _attributes[i].Plus.onClick.AddListener(() => OnValueChange(1, id));
            _attributes[i].Minus.onClick.AddListener(() => OnValueChange(-1, id));
        }
    }

    private void OnValueChange(float value, int id)
    {
        if (value > 0 && _points > 0)//if we adding value and if we have some free points, we can add attribute value
        {
            _modif[id] =+ value;
            _points--;
            _attributes[id].Add();
        }
        if(value < 0 && _modif[id] > 0) //if we removing value and if value is bigger than default then we removing value and adding points
		{
			_modif[id] =+ value;
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
}
