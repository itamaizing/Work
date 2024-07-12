using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuPanels : MonoBehaviour
{
	[SerializeField] private TalentColumn _talentPanel;
	[SerializeField] private AbilityMenuPanel _abilityMenuPanel;
	[SerializeField] private AttributePanel _attributePanel;
	public void SetPanel(TalentSystem talentSystem, PlayerAbilities abilities)
	{
		_talentPanel.Init(talentSystem);
		_abilityMenuPanel.Init(abilities);
	}
}
