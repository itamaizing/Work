using System.Linq;
using UnityEngine;

public class TalentSaveManager
{
    private ISaveData _saveData;
    private SaveManager _saveManager;

    public TalentSaveManager(ISaveData saveData, SaveManager saveManager)
    {
        _saveData = saveData;
        _saveManager = saveManager;
    }

    public void SaveTalent(HeroComponent character, int idGroup, string idTalent, bool isActive, int saveGroup)
    {
        var isTalentActive = isActive ? 1 : 0;
        var talentGroup = character.TalentManager.Talents.FirstOrDefault(o => o.ID == idGroup);
        var talent = talentGroup?.TalentsData.FirstOrDefault(o => o.Data.Name == idTalent);

        if (talentGroup == null || talent == null) return;

        var points = talentGroup.BonusAttributePoints(talent.Data.Name, !isActive);
        talent.Data.IsOpen = isActive;
        
        if (isActive)
        {
            _saveManager.SaveAttributePoints(points);
        }
        else
        {
            HandleDeactivation(points);
        }
        Debug.Log("SHOULD " + isActive + " TALENT " + $"{character.Data.Name}_Group{saveGroup}_{talentGroup.Name}_{talent.Data.Name}");

        _saveData.SaveInt($"{character.Data.Name}_Group{saveGroup}_{talentGroup.Name}_{talent.Data.Name}", isTalentActive);
	}

    private void HandleDeactivation(int points)
    {
        int remainingPoints = points;

        remainingPoints = _saveManager.ReduceFreePoints(remainingPoints);

        if (remainingPoints > 0)
        {
            _saveManager.ReduceAttributePoints(remainingPoints);
        }

        if (remainingPoints > 0)
        {
            Debug.LogWarning("Недостаточно очков для деактивации таланта!");
        }
    }

    public void LoadTalent(HeroComponent character, int idGroup, string idTalent, bool needActive, int saveGroup)
    {
        var talentGroup = character.TalentManager.Talents.FirstOrDefault(o => o.ID == idGroup);
        var talent = talentGroup?.TalentsData.FirstOrDefault(o => o.Data.Name == idTalent);

        if (talentGroup == null || talent == null) return;

        int isActive = _saveData.LoadInt($"{character.Data.Name}_Group{saveGroup}_{talentGroup.Name}_{talent.Data.Name}", 0);
       
        talent.Data.IsOpen = isActive == 1;
        talentGroup.SetActive(talent.Data, isActive == 1);

        if(needActive)
        {
            talentGroup.CmdActiveTalent(talent.Data, isActive == 1);
        }
    }

    public void SaveAllTalents(HeroComponent character, int saveGroup)
    {
        foreach (var talentGroup in character.TalentManager.Talents)
        {
            foreach (var talent in talentGroup.TalentsData)
            {
                _saveData.SaveInt($"{character.Data.Name}_Group{saveGroup}_{talentGroup.Name}_{talent.Data.Name}", talent.Data.IsOpen ? 1 : 0);
            }
        }
    }

    public void LoadAllTalents(HeroComponent character, int saveGroup)
    {
        foreach (var talentGroup in character.TalentManager.Talents)
        {
            foreach (var talent in talentGroup.TalentsData)
            {
				int isActive = _saveData.LoadInt($"{character.Data.Name}_Group{saveGroup}_{talentGroup.Name}_{talent.Data.Name}", 0);
				//int isActive = _saveData.LoadInt($"{character.Data.Name}_Group{saveGroup}_{talentGroup.Name}_{talent.Data.Name}", talent.Data.IsOpen ? 1 : 0);
                talent.Data.IsOpen = isActive == 1;
            }
        }
    }
}
