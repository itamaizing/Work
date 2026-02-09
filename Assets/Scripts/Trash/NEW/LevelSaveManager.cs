using UnityEngine;

public class LevelSaveManager
{
    private readonly ISaveData _saveData;

    public LevelSaveManager(ISaveData saveData)
    {
        _saveData = saveData;
    }

    public void SaveLevelData(HeroComponent character, int saveGroup)
    {
        if (character == null || character.LVL == null) return;

        string prefix = $"{character.Data.Name}_Group{saveGroup}_";

        _saveData.SaveInt(prefix + "Level", character.LVL.Value);
        _saveData.SaveInt(prefix + "Experience", character.LVL.Experience);
        _saveData.SaveInt(prefix + "ExperienceForNextLVL", character.LVL.ExperienceForNextLVL);
    }

    public void LoadLevelData(HeroComponent character, int saveGroup)
    {
        if (character == null || character.LVL == null) return;

        string prefix = $"{character.Data.Name}_Group{saveGroup}_";

        int level = _saveData.LoadInt(prefix + "Level", 1);
        int experience = _saveData.LoadInt(prefix + "Experience", 0);
        int experienceForNextLVL = _saveData.LoadInt(prefix + "ExperienceForNextLVL", 10);

        if (character.LVL.isServer)
        {
            character.LVL.CmdApplyLoadedLevel(level, experience, experienceForNextLVL);
        }
        else
        {
            character.LVL.GetComponent<Level>()?.CmdApplyLoadedLevel(level, experience, experienceForNextLVL);
        }
    }
}
