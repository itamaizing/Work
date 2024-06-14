using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AbilitiesManager : MonoBehaviour
{
    [SerializeField] private RectTransform panelsParent;
    [SerializeField] private AbilityPanel _panelPrefab;
    
    private List<AbilityPanel> _panels;
    
    private static AbilitiesManager instance;
    public static AbilitiesManager Instance => instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
        
        _panels = new List<AbilityPanel>();
    }

    public AbilityPanel AddPanel(PlayerAbilities abilities)
    {
        var panel = Instantiate(_panelPrefab,panelsParent);
        panel.Fill(abilities);
        _panels.Add(panel);
        return panel;
    }
    public void RemovePanel(AbilityPanel panel)
    {
        if (!_panels.Contains(panel)) return;
        _panels.Remove(panel);
        panel.DestroyAbilityPanel();
        
        ActiveFirstSelectedPanel();
    }

    public void ActiveCurrentPanel(AbilityPanel currentPanel)
    {
        foreach (var panel in _panels)
        {
            panel.IsActive = panel == currentPanel;
        }
    }

    public void ChangeCurrentPanelSelectStatus(AbilityPanel currentPanel,bool isSelect)
    {
        currentPanel.IsSelect = isSelect;
    }

    public void ActiveFirstSelectedPanel()
    {
        var panel = _panels.FirstOrDefault(o => o.IsSelect == true);
        if(panel==null) return;
        panel.IsActive = true;
    }
}
