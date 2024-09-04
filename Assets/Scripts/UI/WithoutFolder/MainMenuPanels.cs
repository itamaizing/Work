using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuPanels : MonoBehaviour
{
	[SerializeField] private TalentColumn _talentPanel;
	[SerializeField] private AbilityMenuPanel _abilityMenuPanel;
	[SerializeField] private AttributePanel _attributePanel;
	public void SetPanel(HeroComponent hero)
	{
		_talentPanel.Init(hero.Talents);
		_abilityMenuPanel.Init(hero.Abilities);
		_attributePanel.Init(hero);
	}
}
