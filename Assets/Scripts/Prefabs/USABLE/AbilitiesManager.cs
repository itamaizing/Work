using System.Collections.Generic;
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
        panel.gameObject.SetActive(false);
        return panel;
    }
    public void RemovePanel(AbilityPanel panel)
    {
        if (_panels.Contains(panel))
        {
            _panels.Remove(panel);
            Destroy(panel.gameObject);
        }
    }

    public void ActiveCurrentPanel(AbilityPanel currentPanel)
    {
        foreach (var panel in _panels)
        {
            panel.gameObject.SetActive(panel == currentPanel);
        }
    }
}
