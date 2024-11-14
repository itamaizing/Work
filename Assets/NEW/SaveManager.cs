using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;

    private HeroComponent _character;
    private int _currentSaveGroup = 0;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }

    public void SetHero(HeroComponent hero)
    {
        _character = hero;
        LoadHeroData();
    }

    public void SetSaveIndex(int index)
    {
        _currentSaveGroup = index;
        LoadHeroData();
    }

    private void SaveAttributePoints(int points)
    {
        var currentPoints = _character.Data.Attributes.FreeAttributePointsCount;
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_FreeAttributesPoints", currentPoints + points);
        PlayerPrefs.Save();
    }

    public int LoadAttributePoints()
    {
        var points = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_FreeAttributesPoints", 0);
        _character.Data.Attributes.FreeAttributePointsCount = points;
        return points;
    }

    public void ChangeAttribute(int index, int points)
    {
        if (LoadAttributePoints() - points < 0) return;

        var attribute = _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == index);
        
        if (attribute == null) return;
        
        var currentPoints = attribute.Points;
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + attribute.Name + "_Points", currentPoints + points);

        var usedAttributes = LoadUsedAttributes();
        for (var i = 0; i < points; i++)
        {
            usedAttributes.Add(index);
        }
        SaveUsedAttributes(usedAttributes);

        PlayerPrefs.Save();
        SaveAttributePoints(-points);
    }

    private void SaveAttribute(int index)
    {
        var attribute = _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == index);
        
        if (attribute == null) return;
        
        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + attribute.Name + "_Points", attribute.Points);
        PlayerPrefs.Save();
    }

    public void LoadAttributes()
    {
        foreach (var attribute in _character.Data.Attributes.AttributeData)
        {
            var savedPoints = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + attribute.Name + "_Points", 0);
            attribute.Points = savedPoints;
        }
    }

    public void LoadAttribute(int index)
    {
        var attribute = _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == index);
        
        if (attribute == null) return;
        
        var savedPoints = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + attribute.Name + "_Points", 0);
        attribute.Points = savedPoints;
    }

    private List<int> LoadUsedAttributes()
    {
        var savedUsedAttributes = PlayerPrefs.GetString(_character.Data.Name + "_Group" + _currentSaveGroup + "_UsedAttributePoints", "");
    
        if (string.IsNullOrWhiteSpace(savedUsedAttributes))
        {
            return new List<int>();
        }
    
        var attributeIndexes = savedUsedAttributes.Split(',')
            .Select(s => 
            {
                if (int.TryParse(s, out var index))
                {
                    return (int?)index;
                }
                return null;
            })
            .Where(i => i.HasValue)
            .Select(i => i.Value)
            .ToList();
        
        return attributeIndexes;
    }

    private void SaveUsedAttributes(List<int> usedAttributes)
    {
        var savedUsedAttributes = string.Join(",", usedAttributes);
        PlayerPrefs.SetString(_character.Data.Name + "_Group" + _currentSaveGroup + "_UsedAttributePoints", savedUsedAttributes);
        PlayerPrefs.Save();
    }

    public void SaveTalent(int idGroup, string idTalent, bool isActive)
    {
        var isTalentActive = isActive ? 1 : 0;
        var talentGroup = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == idGroup);
        var talent = talentGroup?.TalentsData.FirstOrDefault(o => o.Data.Name == idTalent);

        if (talentGroup == null || talent == null) return;

        var points = talentGroup.BonusAttributePoints(talent.Data.Name, !isActive);

        talent.Data.IsOpen = isActive;

        if (isActive)
        {
            SaveAttributePoints(points);
        }
        else
        {
            var freePoints = LoadAttributePoints();
            var remainingPoints = points;

            if (freePoints > 0)
            {
                var deductFromFreePoints = Mathf.Min(freePoints, remainingPoints);
                SaveAttributePoints(-deductFromFreePoints);
                remainingPoints -= deductFromFreePoints;
            }

            if (remainingPoints > 0)
            {
                var usedAttributes = LoadUsedAttributes();
                usedAttributes.Reverse();

                foreach (var attributeIndex in usedAttributes)
                {
                    if (remainingPoints <= 0)
                        break;

                    var attribute = _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == attributeIndex);

                    if (attribute is not { Points: > 0 }) continue;

                    var deductPoints = 1;
                    attribute.Points -= deductPoints;
                    remainingPoints -= deductPoints;

                    SaveAttribute(attributeIndex);
                    LoadAttribute(attributeIndex);
                }

                if (remainingPoints > 0)
                {
                    Debug.LogWarning("Недостаточно очков для деактивации таланта!");
                    return;
                }

                usedAttributes.RemoveAll(attributeIndex => _character.Data.Attributes.AttributeData.FirstOrDefault(o => o.Id == attributeIndex)?.Points <= 0);
                SaveUsedAttributes(usedAttributes);
            }
        }

        PlayerPrefs.SetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + talentGroup.Name + "_" + talent.Data.Name, isTalentActive);
        PlayerPrefs.Save();
	}

    public void LoadTalent(int idGroup, string idTalent)
    {
        var groupTemp = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == idGroup);
        var talentTemp = groupTemp?.TalentsData.FirstOrDefault(o => o.Data.Name == idTalent);

        if (groupTemp == null || talentTemp == null) return;

        var isActive = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + groupTemp.Name + "_" + talentTemp.Data.Name, 0);
        
        talentTemp.Data.IsOpen = isActive == 1;
        groupTemp.SetActive(talentTemp.Data, isActive == 1);
    }

    public void LoadTalents()
    {
        foreach (var talentGroup in _character.TalentManager.Talents)
        {
            var groupTemp = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == talentGroup.ID);

            if (groupTemp == null) return;

            foreach (var talent in talentGroup.TalentsData)
            {
                var talentTemp = groupTemp.TalentsData.FirstOrDefault(o => o.Data.Name == talent.Data.Name);

                if (talentTemp == null) return;

                int isActive = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + groupTemp.Name + "_" + talentTemp.Data.Name, 0);
                
                talent.Data.IsOpen = isActive == 1;
                groupTemp.SetActive(talentTemp.Data, isActive == 1);
            }
        }
    }

    public void ResetAllTalents(HeroComponent hero)
    {
        SetHero(hero);
        foreach (var talentGroup in _character.TalentManager.Talents)
        {
            foreach (var talent in talentGroup.TalentsData)
            {
                SaveTalent(talentGroup.ID, talent.Data.Name, false);
            }
        }
    }

    private void LoadHeroData()
    {
        foreach (var attribute in _character.Data.Attributes.AttributeData)
        {
            var savedPoints = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + attribute.Name + "_Points", 0);
            attribute.Points = savedPoints;
        }

        foreach (var talentGroup in _character.TalentManager.Talents)
        {
            var groupTemp = _character.TalentManager.Talents.FirstOrDefault(o => o.ID == talentGroup.ID);

            if (groupTemp == null) return;

            foreach (var talent in talentGroup.TalentsData)
            {
                var talentTemp = groupTemp.TalentsData.FirstOrDefault(o => o.Data.Name == talent.Data.Name);

                if (talentTemp == null) return;

                var isActive = PlayerPrefs.GetInt(_character.Data.Name + "_Group" + _currentSaveGroup + "_" + groupTemp.Name + "_" + talentTemp.Data.Name, 0);
                
                talent.Data.IsOpen = isActive == 1;
                groupTemp.SetActive(talentTemp.Data, isActive == 1);
            }
        }
    }
}