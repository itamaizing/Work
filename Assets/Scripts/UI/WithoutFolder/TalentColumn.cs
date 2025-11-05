using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class TalentColumn : MonoBehaviour
{
	/*[SerializeField] private GameObject[] _content;
	[SerializeField] private TextMeshProUGUI[] _talentContentName;
	[SerializeField] private TalentButton[] _buttons1;
	[SerializeField] private TextMeshProUGUI _column1;
	[SerializeField] private TalentButton[] _buttons2;
	[SerializeField] private TextMeshProUGUI _column2;
	[SerializeField] private TalentButton[] _buttons3;
	[SerializeField] private TextMeshProUGUI _column3;*/
	[SerializeField] private TalentSystem _system;
	[SerializeField] private AttributePanel _attributePanel;

	[SerializeField] private TalentContent _talentContent;
	[SerializeField] private GroupButton _groupButton;

	private List<TalentContent> _groups = new List<TalentContent>();
	private List <GroupButton> _groupButtons = new List<GroupButton>();

	private int _bonus = 0;
	private int _bonus2 = 0;
	private int _bonus3 = 0;
	//private int _prevCount = 0;
	public void OnContentShow(int id)
	{
		//_content[id].SetActive(!_content[id].activeSelf);
		for (int i = 0; i < _groups.Count; i++)
		{
			if (i == id)
			{
				_groups[i].gameObject.SetActive(!_groups[id].gameObject.activeSelf);
			}
			else
			{
				_groups[i].gameObject.SetActive(false);
			}
		}
	}
	public void Init(TalentSystem system)
	{

	}

	public void Start()
	{
		//_system = system;
		int count = 0;
		/*if (_buttons1.Length != _system.Talents.Count)
		{
			Debug.Log("not equal counts in TalentColumn");
			return;
		}*/
		for(int i = 0; i < _system.TalentsGroups.Count; i++) //(TalentsGroup group in _system.Talents) 
		{
			int row = i;

			var item = Instantiate(_talentContent, transform);
			_groups.Add(item);
			item.Initialize(_system.TalentsGroups[i]);
			for(int j = 0; j < item.Talents.Count; j++)
			{
				int id = j;
				//item.Talents[j].button.onClick.AddListener(() => SwitchTalent(row, id, !_system.TalentsGroups[row].TalentsData[id].Data.IsOpen));
			}

			var button = Instantiate(_groupButton, transform);
			_groupButtons.Add(button);
			
			button.button.onClick.AddListener(() => OnContentShow(row));
		}
		/*for (int i = 0; i < _buttons1.Length; i++)
		{
			_buttons1[i].ico.sprite = _system.Talents[0].TalentsData[i].Data.Icon;
			_buttons1[i].talentName.text = _system.Talents[0].TalentsData[i].Data.Name + i;
			_buttons1[i].talentDescription.text = _system.Talents[0].TalentsData[i].Data.Name + "\n" + _system.Talents[0].TalentsData[i].Data.Description;

			if (_system.Talents[0].TalentsData[i].Data.IsOpen)
			{
				count++;
				_buttons1[i].SwitchBorders(true);
				_buttons1[i].isActive = true;
			}
			else
			{
				_buttons1[i].SwitchBorders(false);
				_buttons1[i].isActive = false;
			}
			int id = i;
			_buttons1[i].button.onClick.AddListener(() => { SwitchTalent(id, 0, !_system.Talents[0].TalentsData[id].Data.IsOpen); });
			//_buttons1[i].SwitchBorders();
		}*/
		//_column1.text = count.ToString();
	}

	private void SwitchTalent(int row, int id, bool value)
	{
		Debug.Log(id);
	//	_system.SetActive(row, id, value);
		_groups[row].Talents[id].SwitchBorders(value);
		Debug.Log("switch");

		//_column1.text = _system.GetActiveTalentCount().ToString();

		if (_attributePanel == null)
		{
			Debug.Log("NO ATTRIBUTE PANEL AT TalentColumn!!!!");
			return;
		}
		if (value)
		{
			_attributePanel.AddPoints(1);
		}
		else
		{
			_attributePanel.RemovePoints(1);
		}
		BonusAttributePoints();
	}

	public void SwitchActiveUI()
	{
		if (gameObject.transform.localScale.x == 0)
		{
			gameObject.transform.DOScale(1, 0.5f);
		}
		else
		{
			gameObject.transform.DOScale(0, 0.5f);
		}
	}

	private void BonusAttributePoints() //its very strange but....  first "for" is for the first row, if we had 2 active talents we get 1 extra point, 
										//if we have 3 active then we get 3 points, for the second row, 2 active-1 point, 3 active-2 points,
										//for third row for 3 active 1 point
	{
		/*int count = 0;
		for (int i = 9; i < 12; i++)
		{
			if (_system.TalentsGroups[0].TalentsData[i].Data.IsOpen)
			{
				count++;
			}
		}
		int count2 = 0;
		for (int i = 6; i < 9; i++)
		{
			if (_system.TalentsGroups[0].TalentsData[i].Data.IsOpen)
			{
				count2++;
			}
		}
		int count3 = 0;
		for (int i = 3; i < 6; i++)
		{
			if (_system.TalentsGroups[0].TalentsData[i].Data.IsOpen)
			{
				count3++;
			}
		}

		if (count <= 1)
		{
			_bonus = 0;
		}
		if (count == 2)
		{
			_bonus = 1;
		}
		if (count == 3)
		{
			_bonus = 3;
		}

		if (count2 <= 1)
		{
			_bonus2 = 0;
		}
		if (count2 == 2)
		{
			_bonus2 = 1;
		}
		if (count2 == 3)
		{
			_bonus2 = 2;
		}

		if (count3 < 3)
		{
			_bonus3 = 0;
		}
		if (count3 == 3)
		{
			_bonus3 = 1;
		}
		_attributePanel.SetBonus(_bonus, _bonus2, _bonus3);*/
	}
}