using System;
using System.Collections.Generic;
using System.Linq;

public class LocalHeroProgressRepository : IHeroProgressRepository
{
    private readonly ISaveData _saveData;
    private readonly SaveSystem _saveSystem;

    public LocalHeroProgressRepository(ISaveData saveData, SaveSystem saveSystem)
    {
        _saveData = saveData;
        _saveSystem = saveSystem;
    }

    public void Load(HeroComponent hero, UIMenuMainAttributesPanel attributesPanel, int saveGroup,
        Func<bool> isStillCurrent, Action onComplete)
    {
        string heroKey = hero.Data.Name;

        int level = _saveData.LoadInt($"{heroKey}_Group{saveGroup}_Level", 1);
        int experience = _saveData.LoadInt($"{heroKey}_Group{saveGroup}_Experience", 0);
        LevelCharacterManager.Instance.ApplyLoadedLevelData(hero, level, experience);

        int talentPoints = _saveData.LoadInt($"{heroKey}_TalentPointsCount", 0);
        hero.TalentManager.SetPoints(talentPoints);

        foreach (var talentGroup in hero.TalentManager.TalentsGroups)
        foreach (var row in talentGroup.TalentRows)
        foreach (var talent in row.Talents)
        {
            int lvl = _saveData.LoadInt($"{heroKey}_Group{saveGroup}_{talentGroup.Name}_{talent.Data.Name}", 0);
            talent.Data.SetOpen(lvl >= 1);
            talent.Data.SetLevel(lvl);
            talentGroup.SetActive(talent.Data, lvl >= 1, lvl);
        }

        if (isStillCurrent != null && !isStillCurrent()) { onComplete?.Invoke(); return; }

        var entries = new List<ServerHeroProgressRepository.AttributeEntry>();
        if (attributesPanel?.AttributeSystem != null)
        {
            foreach (var attribute in attributesPanel.AttributeSystem.Attributes.Values)
            {
                int points = 0;
                _saveSystem.Load<List<AttributeModifier>>(
                    $"{heroKey}_Group{saveGroup}_{attribute.Name}_Points",
                    modifiers => points = modifiers?.Count ?? 0);

                entries.Add(new ServerHeroProgressRepository.AttributeEntry { name = attribute.Name, points = points });
            }
        }

        int freeAttributePoints = _saveSystem.LoadOrDefault($"{heroKey}_Group{saveGroup}_FreeAttributesPoints", 0);
        attributesPanel?.ApplyServerAttributePoints(entries, freeAttributePoints);

        onComplete?.Invoke();
    }

    public void SaveTalent(HeroComponent hero, int idGroup, int row, string idTalent, bool isActive, int lvl, int saveGroup,
        Action<int> onFreeTalentPointsChanged, Action onFailed)
    {
        var talentGroup = hero.TalentManager.TalentsGroups.FirstOrDefault(g => g.ID == idGroup);
        if (talentGroup == null) { onFailed?.Invoke(); return; }

        _saveData.SaveInt($"{hero.Data.Name}_Group{saveGroup}_{talentGroup.Name}_{idTalent}", lvl);
        _saveData.SaveInt($"{hero.Data.Name}_TalentPointsCount", hero.TalentManager.Points);

        onFreeTalentPointsChanged?.Invoke(hero.TalentManager.Points);
    }

    public void SaveAttributePoint(HeroComponent hero, string attributeName, int delta, int saveGroup,
        Action<int> onFreeAttributePointsChanged, Action onFailed)
    {
        var attribute = hero.AttributeSystem.Attributes.Values.FirstOrDefault(a => a.Name == attributeName);
        if (attribute == null) { onFailed?.Invoke(); return; }

        _saveSystem.Save($"{hero.Data.Name}_Group{saveGroup}_{attributeName}_Points", attribute.Modifiers);

        int freePoints = _saveSystem.LoadOrDefault($"{hero.Data.Name}_Group{saveGroup}_FreeAttributesPoints", 0);
        freePoints -= delta;
        _saveSystem.Save($"{hero.Data.Name}_Group{saveGroup}_FreeAttributesPoints", freePoints);

        onFreeAttributePointsChanged?.Invoke(freePoints);
    }

    public void SaveLevel(HeroComponent hero, int saveGroup, int level, int experience, int skillPoints, int attributePoints)
    {
        string heroKey = hero.Data.Name;
        _saveData.SaveInt($"{heroKey}_Group{saveGroup}_Level", level);
        _saveData.SaveInt($"{heroKey}_Group{saveGroup}_Experience", experience);
    }

    public void SaveBottles(string userKey, int bottles, float bottleVolume, Action<int> onSaved, Action onFailed)
    {
        _saveData.SaveInt($"{userKey}_Bottles", bottles);
        _saveData.SaveFloat($"{userKey}_BottleVolume", bottleVolume);
        onSaved?.Invoke(bottles);
    }

    public void LoadBottles(string userKey, Action<int, float> onLoaded, Action onFailed = null)
    {
        int bottles = _saveData.LoadInt($"{userKey}_Bottles", 0);
        float volume = _saveData.LoadFloat($"{userKey}_BottleVolume", 0f);
        onLoaded?.Invoke(bottles, volume);
    }
}