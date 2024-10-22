using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class UIMenuMainAbilitiesPanel : MonoBehaviour
{
    [ReadOnly,ShowInInspector]
    public UIMenuMainWindow Owner;
    
    [SerializeField] private UIMenuMainAbilitiesPanelItem _abilityItem;
    [SerializeField] private AbilityTooltip _abilityInfo;
    [SerializeField] private RectTransform _itemsParent;
    
    private SkillManager _abilitiesComponent;
    private List<UIMenuMainAbilitiesPanelItem> _abilities = new ();
    
    public void Show()
    {
        if(Owner == null) return;

        if (_abilitiesComponent != null)
        {
            _abilitiesComponent.SkillAdded -= UpdatePanel;
            _abilitiesComponent.SkillRemoved -= UpdatePanel;
        }

        _abilitiesComponent = Owner.GetHero().Abilities;

        _abilitiesComponent.SkillAdded += UpdatePanel;
        _abilitiesComponent.SkillRemoved += UpdatePanel;

        ResetPanel();

        foreach (var item in _abilitiesComponent.Abilities)
        {
            var abilityIcon = Instantiate(_abilityItem, _itemsParent);
            abilityIcon.Owner = this;
            abilityIcon.Fill(item);
            _abilities.Add(abilityIcon);
        }
        
        HideTooltip();
    }

    private void ResetPanel()
    {
        if (_abilities.Count > 0)
        {
            foreach (var ability in _abilities)
            {
                ability.Destroy();
            }
            _abilities.Clear();
        }
    }

    public void ShowTooltip(Skill ability , Vector2 position)
    {
        _abilityInfo.gameObject.SetActive(true);
        _abilityInfo.ChangePosition(position);
        _abilityInfo.Fill(ability.Name,ability.Description, ability.CooldownTime,ability.CastDeley);
    }

    public void HideTooltip()
    {
        _abilityInfo.gameObject.SetActive(false);
    }

    private void UpdatePanel(Skill skill)
    {
        Show();
    }
}
