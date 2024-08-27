using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityMenuPanel : MonoBehaviour
{
	[SerializeField] private SkillManager _ability;
	[SerializeField] private Image[] _icos;
	[SerializeField] private TextMeshProUGUI[] _name;

	private void Start()
	{
		for (int i = 0; i < _icos.Length; i++) 
		{
			_icos[i].sprite = _ability.Abilities[i].Icon;
			_name[i].text = _ability.Abilities[i].Name + " \n" + _ability.Abilities[i].Description;
		}
	}
}
