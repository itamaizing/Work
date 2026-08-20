using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class TalentNetworkManager : MonoBehaviour
{
    private static TalentNetworkManager _instance;
    public static TalentNetworkManager Instance => _instance;
    
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
    
    public void LoadServerArrangement(HeroComponent character, Action onComplete = null)
    {
        if (MPNetworkManager.Instance == null || MPNetworkManager.Instance.UserID <= 0)
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
            success: json => ApplyServerData(character, json, onComplete),
            error: err => { Debug.LogWarning("[Talents] Сервер недоступен: " + err); onComplete?.Invoke(); });
    }

    private void ApplyServerData(HeroComponent character, string json, Action onComplete)
    {
        var serverData = JsonConvert.DeserializeObject<ServerTalentData>(json);
        if (serverData == null || !serverData.success) { onComplete?.Invoke(); return; }

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

        onComplete?.Invoke();
    }
}
