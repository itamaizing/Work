using System;
using System.Collections.Generic;

public interface IHeroProgressRepository
{
    void Load(HeroComponent hero, UIMenuMainAttributesPanel attributesPanel, int saveGroup,
        Func<bool> isStillCurrent, Action onComplete);

    void SaveTalent(HeroComponent hero, int idGroup, int row, string idTalent, bool isActive, int lvl, int saveGroup,
        Action<int> onFreeTalentPointsChanged, Action onFailed);

    void SaveAttributePoint(HeroComponent hero, string attributeName, int delta, int saveGroup,
        Action<int> onFreeAttributePointsChanged, Action onFailed);

    void SaveLevel(HeroComponent hero, int saveGroup, int level, int experience, int skillPoints, int attributePoints);

    void SaveAbilityLayout(string heroName, int saveGroup, List<SkillPanelSave> layout,
        Action<List<SkillPanelSave>> onSaved, Action onFailed);

    void LoadAbilityLayout(string heroName, int saveGroup, Action<List<SkillPanelSave>> onLoaded, Action onFailed = null);
    
    void SaveBottles(string userKey, int bottles, float bottleVolume, Action<int> onSaved, Action onFailed);

    void LoadBottles(string userKey, Action<int, float> onLoaded, Action onFailed = null);
    
    void SaveTalentPage(HeroComponent hero, int saveGroup, TalentSnapshotEntry[] talents,
        AttributeSnapshotEntry[] attributes, Action onSaved, Action onFailed);
    
    void LoadTalentPage(HeroComponent hero, int saveGroup,
        Action<TalentSnapshotEntry[], AttributeSnapshotEntry[]> onLoaded, Action onFailed = null);
}