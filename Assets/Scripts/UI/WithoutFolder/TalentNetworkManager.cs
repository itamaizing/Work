using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class TalentNetworkManager : MonoBehaviour
{
    private static TalentNetworkManager _instance;
    public static TalentNetworkManager Instance => _instance;
    
    [SerializeField] private UIMenuMainWindow _uiMenuMainWindow;
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    
    [Serializable] public class ServerTalentEntry { public int group; public int row; public string name; public int lvl; }
    [Serializable] public class ServerAttributeEntry { public string name; public int points; }
    [Serializable] public class ServerTalentData
    {
        public bool success;
        public int freeTalentPoints;
        public int freeAttributePoints;
        public List<ServerTalentEntry> talents;
        public List<ServerAttributeEntry> attributes;
    }
    
    private List<object> talents = new();
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SaveArrangement()
    {
        talents.Clear();
        var hero = _uiMenuMainWindow.GetHero();
        if (hero == null || !MPNetworkManager.Instance.IsServer()) return;

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
    
    public void LoadServerArrangement(HeroComponent character, UIMenuMainAttributesPanel attributesPanel, Func<bool> isStillCurrent, Action onComplete = null)
    {
        if (MPNetworkManager.Instance == null || !MPNetworkManager.Instance.IsServer())
        {
            onComplete?.Invoke();
            return;
        }

        var data = new Dictionary<string, string>
        {
            { "id", MPNetworkManager.Instance.UserID.ToString() },
            { "heroName", character.Data.Name }
        };

        NetworkHTTP.Instance.PostGetTalentData(data,
            success: json => ApplyServerData(character, attributesPanel, isStillCurrent, json, onComplete),
            error: err => { Debug.LogWarning("[Talents] Сервер недоступен: " + err); onComplete?.Invoke(); });
    }

    private void ApplyServerData(HeroComponent character, UIMenuMainAttributesPanel attributesPanel, Func<bool> isStillCurrent, string json, Action onComplete)
    {
        var serverData = JsonConvert.DeserializeObject<ServerTalentData>(json);
        if (serverData == null || !serverData.success) { onComplete?.Invoke(); return; }

        if (isStillCurrent != null && !isStillCurrent())
        {
            onComplete?.Invoke();
            return;
        }

        foreach (var group in character.TalentManager.TalentsGroups)
        foreach (var row in group.TalentRows)
        foreach (var talent in row.Talents)
            group.SetActive(talent.Data, false, 0);

        foreach (var entry in serverData.talents)
        {
            var group = character.TalentManager.TalentsGroups.FirstOrDefault(g => g.ID == entry.group);
            var talent = group?.TalentRows[entry.row].Talents.FirstOrDefault(t => t.Data.Name == entry.name);
            if (talent == null) continue;

            group.SetActive(talent.Data, true, entry.lvl);
        }

        character.TalentManager.SetPoints(serverData.freeTalentPoints);

        attributesPanel?.ApplyServerAttributePoints(serverData.attributes, serverData.freeAttributePoints);
        
        onComplete?.Invoke();
    }
}
