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
		if (_talentPanel != null)
		{
			_talentPanel.Init(hero.TalentSystem);
		}
		if (_abilityMenuPanel != null)
		{
			_abilityMenuPanel.Init(hero.Abilities);
		}
		if (_attributePanel != null)
		{
			_attributePanel.Init(hero);
		}
	}
}
