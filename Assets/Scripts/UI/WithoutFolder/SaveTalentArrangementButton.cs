using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class SaveTalentArrangementButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private UIMenuMainWindow _uiMenuMainWindow;
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;

    private List<object> talents = new();

    private void Awake()
    {
        if (!_button) _button = GetComponent<Button>();
        _button.onClick.AddListener(SaveArrangement);
    }

    private void SaveArrangement()
    {
        talents.Clear();
        var hero = _uiMenuMainWindow.GetHero();
        if (hero == null || MPNetworkManager.Instance.UserID < 0) return;

        var talentSystem = hero.TalentManager;
        var attributeSystem = _attributesPanel.AttributeSystem;

        foreach (var group in talentSystem.TalentsGroups)
        foreach (var row in group.TalentRows)
        foreach (var talent in row.Talents)
            if (talent.Data.IsOpen)
                talents.Add(new { group = group.ID, row = talent.Data.Row, name = talent.Data.Name, lvl = talent.Data.Level });

        var attributes = attributeSystem.Attributes.Values
            .Select(a => new
            {
                name = a.Name,
                points = a.Modifiers.Count(m => (m.Source as string) == "AttributePoint")
            });

        var payload = new
        {
            id = MPNetworkManager.Instance.UserID,
            heroName = hero.Data.Name,
            talents,
            attributes
        };

        string json = JsonConvert.SerializeObject(payload);

        NetworkHTTP.Instance.PostSetTalentData(json,
            success: resp => Debug.Log("Расстановка сохранена: " + resp),
            error: err => Debug.LogWarning("Ошибка сохранения расстановки: " + err));
    }
}