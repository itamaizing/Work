using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TalentContent : MonoBehaviour
{
    [SerializeField] private TalentButton _buttons;

    private List<TalentButton> _talentsList = new List<TalentButton>();

    public List<TalentButton> Talents => _talentsList;

    public void Initialize(TalentsGroup talentsGroup)
    {
		/* foreach (Talent talents in talentsGroup.TalentsData)
		 {

			 var item = Instantiate(_buttons, transform);
			 _talentsList.Add(item);
			 item.Init(talents.Data.Icon, talents.Data.Name, talents.Data.Description);
		 }   */
		foreach (var talentsRow in talentsGroup.TalentRows)
		{
			foreach (Talent talents in talentsRow.Talents)
			{
				var item = Instantiate(_buttons, transform);
				_talentsList.Add(item);
				item.Init(talents.Data.Icon, talents.Data.Name, talents.Data.Description);
			}
		}
	}
}
