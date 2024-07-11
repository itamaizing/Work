using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityMenuPanel : MonoBehaviour
{
	//[SerializeField] private PlayerAbilities _ability;
	//[SerializeField] private Image[] _icos;
	//[SerializeField] private TextMeshProUGUI[] _name;
	[SerializeField] private AbilityUiIco _abilityUiIco;
	[SerializeField] private Transform _parent;

	/*private void Start()
	{
		Init(_ability);
	}*/
	public void Init(PlayerAbilities ability)
	{
		for (int i = 0; i < ability.Abilities.Count; i++) 
		{
			var ico = Instantiate(_abilityUiIco, _parent);
			ico.Init(ability.Abilities[i].Icon, ability.Abilities[i].Name + " \n" + ability.Abilities[i].Description);
		}
	}
}
