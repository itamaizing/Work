using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class ServerHeroProgressRepository : IHeroProgressRepository
{
    [Serializable]
    public class TalentEntry
    {
        public int group;
        public int row;
        public string name;
        public int lvl;
    }

    [Serializable]
    public class AttributeEntry
    {
        public string name;
        public int points;
    }

    [Serializable]
    public class TalentAttributeResponse
    {
        public bool success;
        public int freeTalentPoints;
        public int freeAttributePoints;
        public List<TalentEntry> talents;
        public List<AttributeEntry> attributes;
    }

    [Serializable]
    private class TalentSingleResponse
    {
        public bool success;
        public int freeTalentPoints;
    }

    [Serializable]
    private class AttributePointResponse
    {
        public bool success;
        public int freeAttributePoints;
    }

    [Serializable]
    private class SetBottleResponse
    {
        public bool success;
        public int bottles;
    }

    [Serializable]
    private class AbilityPanelResponse
    {
        public bool success;
        public List<SkillPanelSave> layout;
    }

    public void Load(HeroComponent hero, UIMenuMainAttributesPanel attributesPanel, int saveGroup,
        Func<bool> isStillCurrent, Action onComplete)
    {
        int pending = 2;

        void OnPartDone()
        {
            pending--;
            if (pending == 0) onComplete?.Invoke();
        }

        LoadHeroLevel(hero, OnPartDone);
        LoadTalentsAndAttributes(hero, attributesPanel, isStillCurrent, OnPartDone);
    }

    private void LoadHeroLevel(HeroComponent hero, Action onDone)
    {
        var data = new Dictionary<string, string>
        {
            { "id", MPNetworkManager.Instance.UserID.ToString() },
            { "heroName", hero.Data.Name }
        };

        NetworkHTTP.Instance.PostGetHeroData(data,
            success: json =>
            {
                if (string.IsNullOrEmpty(json) || json.Contains("\"success\":false"))
                {
                    Debug.LogWarning($"[ServerHeroProgressRepository] GetHeroData вернул ошибку: {json}");
                    onDone?.Invoke();
                    return;
                }

                var heroData = NetworkHTTP.ConvertInHeroData(json);
                LevelCharacterManager.Instance.ApplyLoadedLevelData(hero, heroData.lvl, heroData.exp);
                onDone?.Invoke();
            },
            error: err =>
            {
                Debug.LogWarning($"[ServerHeroProgressRepository] Сервер недоступен (уровень): {err}");
                onDone?.Invoke();
            });
    }

    private void LoadTalentsAndAttributes(HeroComponent hero, UIMenuMainAttributesPanel attributesPanel,
        Func<bool> isStillCurrent, Action onDone)
    {
        var data = new Dictionary<string, string>
        {
            { "id", MPNetworkManager.Instance.UserID.ToString() },
            { "heroName", hero.Data.Name }
        };

        NetworkHTTP.Instance.PostGetTalentData(data,
            success: json => ApplyTalentAttributeData(hero, attributesPanel, isStillCurrent, json, onDone),
            error: err =>
            {
                Debug.LogWarning("[ServerHeroProgressRepository] Сервер недоступен (таланты): " + err);
                onDone?.Invoke();
            });
    }

    private void ApplyTalentAttributeData(HeroComponent hero, UIMenuMainAttributesPanel attributesPanel,
        Func<bool> isStillCurrent, string json, Action onDone)
    {
        var serverData = JsonConvert.DeserializeObject<TalentAttributeResponse>(json);
        if (serverData == null || !serverData.success)
        {
            onDone?.Invoke();
            return;
        }

        if (isStillCurrent != null && !isStillCurrent())
        {
            onDone?.Invoke();
            return;
        }

        foreach (var group in hero.TalentManager.TalentsGroups)
        foreach (var row in group.TalentRows)
        foreach (var talent in row.Talents)
        {
            talent.Data.SetOpen(false);
            talent.Exit();
        }

        foreach (var entry in serverData.talents)
        {
            var group = hero.TalentManager.TalentsGroups.FirstOrDefault(g => g.ID == entry.group);
            var talent = group?.TalentRows[entry.row].Talents.FirstOrDefault(t => t.Data.Name == entry.name);
            if (talent == null) continue;

            talent.Data.SetOpen(true);
            talent.Data.SetLevel(entry.lvl);
            talent.Enter();
        }

        hero.TalentManager.SetPoints(serverData.freeTalentPoints);

        var attributeEntries = serverData.attributes?
            .Select(a => new AttributeEntry { name = a.name, points = a.points })
            .ToList();

        attributesPanel?.ApplyServerAttributePoints(attributeEntries, serverData.freeAttributePoints);

        onDone?.Invoke();
    }

    public void SaveTalent(HeroComponent hero, int idGroup, int row, string idTalent, bool isActive, int lvl,
        int saveGroup,
        Action<int> onFreeTalentPointsChanged, Action onFailed)
    {
        var payload = new
        {
            id = MPNetworkManager.Instance.UserID,
            heroName = hero.Data.Name,
            group = idGroup,
            row,
            name = idTalent,
            isActive,
            lvl
        };
        string json = JsonConvert.SerializeObject(payload);

        NetworkHTTP.Instance.PostSetTalentSingle(json,
            success: resp =>
            {
                var result = JsonConvert.DeserializeObject<TalentSingleResponse>(resp);
                if (result == null || !result.success)
                {
                    Debug.LogWarning("Талант не сохранён: " + resp);
                    onFailed?.Invoke();
                    return;
                }

                onFreeTalentPointsChanged?.Invoke(result.freeTalentPoints);
            },
            error: err =>
            {
                Debug.LogWarning("Ошибка сохранения таланта: " + err);
                onFailed?.Invoke();
            });
    }

    public void SaveAttributePoint(HeroComponent hero, string attributeName, int delta, int saveGroup,
        Action<int> onFreeAttributePointsChanged, Action onFailed)
    {
        var payload = new
        {
            id = MPNetworkManager.Instance.UserID,
            heroName = hero.Data.Name,
            attribute = attributeName,
            delta
        };
        string json = JsonConvert.SerializeObject(payload);

        NetworkHTTP.Instance.PostSetAttributePoint(json,
            success: resp =>
            {
                var result = JsonConvert.DeserializeObject<AttributePointResponse>(resp);
                if (result == null || !result.success)
                {
                    Debug.LogWarning("Очко атрибута не сохранено: " + resp);
                    onFailed?.Invoke();
                    return;
                }

                onFreeAttributePointsChanged?.Invoke(result.freeAttributePoints);
            },
            error: err =>
            {
                Debug.LogWarning("Ошибка сохранения атрибута: " + err);
                onFailed?.Invoke();
            });
    }

    public void SaveLevel(HeroComponent hero, int saveGroup, int level, int experience, int skillPoints,
        int attributePoints)
    {
        Dictionary<string, string> data = new Dictionary<string, string>()
        {
            { "id", MPNetworkManager.Instance.UserID.ToString() },
            { "heroName", hero.Data.Name },
            { "heroLVL", level.ToString() },
            { "heroExp", experience.ToString() },
            { "heroSkillPoints", skillPoints.ToString() },
            { "attributePoints", attributePoints.ToString() },
        };

        NetworkHTTP.Instance.PostSetHeroData(
            data,
            success: json => Debug.Log("[ServerHeroProgressRepository] уровень героя сохранён на сервере"),
            error: err => Debug.LogWarning($"[ServerHeroProgressRepository] Не удалось сохранить уровень: {err}")
        );
    }

    public void SaveAbilityLayout(string heroName, int saveGroup, List<SkillPanelSave> layout,
        Action<List<SkillPanelSave>> onSaved, Action onFailed)
    {
        var data = new Dictionary<string, string>
        {
            { "id", MPNetworkManager.Instance.UserID.ToString() },
            { "heroName", heroName },
            { "layout", JsonConvert.SerializeObject(layout) }
        };

        NetworkHTTP.Instance.PostSetAbilityPanel(data,
            success: json =>
            {
                var result = JsonConvert.DeserializeObject<AbilityPanelResponse>(json);
                if (result == null || !result.success)
                {
                    Debug.LogWarning("Расстановка способностей не сохранена: " + json);
                    onFailed?.Invoke();
                    return;
                }

                onSaved?.Invoke(result.layout ?? layout);
            },
            error: err =>
            {
                Debug.LogWarning($"[ServerHeroProgressRepository] Не удалось сохранить панель способностей: {err}");
                onFailed?.Invoke();
            });
    }

    public void LoadAbilityLayout(string heroName, int saveGroup, Action<List<SkillPanelSave>> onLoaded,
        Action onFailed = null)
    {
        var data = new Dictionary<string, string>
        {
            { "id", MPNetworkManager.Instance.UserID.ToString() },
            { "heroName", heroName }
        };

        NetworkHTTP.Instance.PostGetAbilityPanel(data,
            success: json =>
            {
                var result = JsonConvert.DeserializeObject<AbilityPanelResponse>(json);
                if (result == null || !result.success)
                {
                    Debug.LogWarning("Не удалось загрузить панель способностей: " + json);
                    onFailed?.Invoke();
                    return;
                }

                onLoaded?.Invoke(result.layout);
            },
            error: err =>
            {
                Debug.LogWarning($"[ServerHeroProgressRepository] Сервер недоступен (панель способностей): {err}");
                onFailed?.Invoke();
            });
    }

    public void SaveBottles(string userKey, int bottles, float bottleVolume, Action<int> onSaved, Action onFailed)
    {
        Dictionary<string, string> data = new Dictionary<string, string>()
        {
            { "id", userKey },
            { "bottle", bottles.ToString() }
        };

        NetworkHTTP.Instance.PostSetBottle(data,
            success: json =>
            {
                var result = JsonConvert.DeserializeObject<SetBottleResponse>(json);
                if (result == null || !result.success)
                {
                    Debug.LogWarning("Бутылки не сохранены: " + json);
                    onFailed?.Invoke();
                    return;
                }

                onSaved?.Invoke(result.bottles);
            },
            error: err =>
            {
                Debug.LogWarning($"[ServerHeroProgressRepository] Не удалось сохранить бутылки: {err}");
                onFailed?.Invoke();
            });
    }

    public void LoadBottles(string userKey, Action<int, float> onLoaded, Action onFailed = null)
    {
        Dictionary<string, string> data = new Dictionary<string, string>() { { "id", userKey } };
        NetworkHTTP.Instance.PostGetBottle(data,
            success: json =>
            {
                if (int.TryParse(json, out int bottles))
                {
                    onLoaded?.Invoke(bottles, 0f);
                }
                else
                {
                    Debug.LogWarning($"[ServerHeroProgressRepository] Не удалось распарсить бутылки: {json}");
                    onFailed?.Invoke();
                }
            },
            error: err =>
            {
                Debug.LogWarning($"[ServerHeroProgressRepository] Сервер недоступен (бутылки): {err}");
                onFailed?.Invoke();
            });
    }
    
    [Serializable]
    private class SetTalentDataRequest
    {
        public int id;
        public string heroName;
        public TalentSnapshotEntry[] talents;
        public AttributeSnapshotEntry[] attributes;
    }

    [Serializable]
    private class SetTalentDataResponse
    {
        public bool success;
        public string error;
    }
    
    public void SaveTalentPage(HeroComponent hero, int saveGroup, TalentSnapshotEntry[] talents,
        AttributeSnapshotEntry[] attributes, Action onSaved, Action onFailed)
    {
        var payload = new SetTalentDataRequest
        {
            id = MPNetworkManager.Instance.UserID,
            heroName = hero.Data.Name,
            talents = talents ?? Array.Empty<TalentSnapshotEntry>(),
            attributes = attributes ?? Array.Empty<AttributeSnapshotEntry>()
        };
        string json = JsonConvert.SerializeObject(payload);

        NetworkHTTP.Instance.PostSetTalentData(json,
            success: resp =>
            {
                var result = JsonConvert.DeserializeObject<SetTalentDataResponse>(resp);
                if (result == null || !result.success)
                {
                    Debug.LogWarning("Расстановка не сохранена: " + resp);
                    onFailed?.Invoke();
                    return;
                }
                onSaved?.Invoke();
            },
            error: err =>
            {
                Debug.LogWarning("Ошибка сохранения расстановки: " + err);
                onFailed?.Invoke();
            });
    }
}