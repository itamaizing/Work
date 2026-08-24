using System;

public interface IHeroProgressRepository
{
    void Load(HeroComponent hero, UIMenuMainAttributesPanel attributesPanel, int saveGroup,
        Func<bool> isStillCurrent, Action onComplete);

    void SaveTalent(HeroComponent hero, int idGroup, int row, string idTalent, bool isActive, int lvl, int saveGroup,
        Action<int> onFreeTalentPointsChanged, Action onFailed);

    void SaveAttributePoint(HeroComponent hero, string attributeName, int delta, int saveGroup,
        Action<int> onFreeAttributePointsChanged, Action onFailed);

    void SaveLevel(HeroComponent hero, int saveGroup, int level, int experience, int skillPoints, int attributePoints);
    
    void SaveBottles(string userKey, int bottles, float bottleVolume, Action<int> onSaved, Action onFailed);

    void LoadBottles(string userKey, Action<int, float> onLoaded, Action onFailed = null);
}