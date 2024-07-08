using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TalentColumn : MonoBehaviour
{
	[SerializeField] private GameObject[] _content;
	[SerializeField] private TalentButton[] _buttons1;
	[SerializeField] private TextMeshProUGUI _column1;
	[SerializeField] private TalentButton[] _buttons2;
	[SerializeField] private TextMeshProUGUI _column2;
	[SerializeField] private TalentButton[] _buttons3;
	[SerializeField] private TextMeshProUGUI _column3;
	[SerializeField] private TalentSystem _system;

	public void OnContentShow(int id)
	{
		//_content[id].SetActive(!_content[id].activeSelf);
		for(int i =0; i< _content.Length; i++)
		{
			if(i == id)
			{
				_content[i].SetActive(!_content[id].activeSelf);
			}
			else
			{
				_content[i].SetActive(false);
			}
		}
	}

	private void Start()
	{
		Init();
	}

	public void Init()
	{
		int count = 0;
		for(int i = 0; i < _buttons1.Length; i++)
		{
			_buttons1[i].ico.sprite = _system.Talents[i].ico;
			_buttons1[i].talentName.text = _system.Talents[i].Name + i;
			_buttons1[i].talentDescription.text = _system.Talents[i].Name + "\n" + _system.Talents[i].Description;
			int id = i;
			_buttons1[i].button.onClick.AddListener(() => { SwitchTalent(id, !_system.Talents[id].isActive); });
			if(_system.Talents[id].isActive)
			{
				count++;
			}
			//_buttons1[i].SwitchBorders();
		}
		_column1.text = count.ToString();
	}

	private void SwitchTalent(int id, bool value)
	{
		Debug.Log(id);
		_system.CmdSwitchActive(id);
		_buttons1[id].SwitchBorders(value);
		Debug.Log("switch");

		_column1.text = _system.ActiveTalents.Count.ToString();
	}
}
